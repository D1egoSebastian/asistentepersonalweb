import { clearToken, readToken, saveToken, sessionGeneration } from "./session.js";

// The one place that sends requests to the API (plan D-T6). Habits, ideas and sign-in use it.
const GENERIC_MESSAGE = "No se pudo completar la acción. Vuelve a intentarlo.";
const SESSION_EXPIRED_MESSAGE = "Tu sesión expiró. Vuelve a iniciar sesión.";
const NO_CONNECTION_MESSAGE = "No hay conexión con el servidor.";
const FALLBACK_MESSAGES = {
  400: "Los datos no son válidos.",
  401: SESSION_EXPIRED_MESSAGE,
};

// In production the API runs on its own host (VITE_API_URL). In development it is empty, and the Vite proxy serves /api.
const API_BASE = import.meta.env?.VITE_API_URL ?? "";

export function apiUrl(path, base = API_BASE) {
  if (!base) {
    return path;
  }
  return `${base.replace(/\/$/, "")}${path}`;
}

let unauthorizedHandler = null;

// The app registers what to do when the session is missing or expired (spec 002, CA-3.3).
export function setUnauthorizedHandler(handler) {
  unauthorizedHandler = handler;
}

export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

// The API's own Spanish message wins (plan D-T6). Without one, a fixed message for the status.
export function errorMessageFor(status, body) {
  if (body?.message) {
    return body.message;
  }
  return FALLBACK_MESSAGES[status] ?? GENERIC_MESSAGE;
}

function buildHeaders(body, session) {
  const headers = {};
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }
  const token = session ? readToken() : null;
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }
  return headers;
}

// Keeps the stored session in step with the response: saves the renewed token (spec 002, CA-2.5)
// and reacts to a 401 (CA-3.3). If the user signed out while the request was in flight,
// the response belongs to the old session and is ignored, so "Salir" cannot be undone (CA-4.1).
function updateSession(response, sessionAtStart) {
  if (sessionGeneration() !== sessionAtStart) {
    return;
  }

  const refreshed = response.headers.get("X-Refreshed-Token");
  if (refreshed) {
    saveToken(refreshed);
  }

  if (response.status === 401) {
    clearToken();
    unauthorizedHandler?.();
  }
}

// Sends one request. Session calls (habits, ideas) send the stored token and react to the session.
// Public calls (sign-in, first use) send no token and never end the session: a wrong password
// is a 401 with its own message, not an expired session.
export async function request(method, url, body, { session = true } = {}) {
  const sessionAtStart = sessionGeneration();
  let response;
  try {
    response = await fetch(apiUrl(url), {
      method,
      headers: buildHeaders(body, session),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, NO_CONNECTION_MESSAGE);
  }

  if (session) {
    updateSession(response, sessionAtStart);
  }

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    throw new ApiError(response.status, errorMessageFor(response.status, errorBody));
  }

  // Some successful responses have no body (for example, an unlog).
  return response.json().catch(() => null);
}
