import { clearToken, readToken, saveToken, sessionGeneration } from "./session.js";

// Calls to the habits API. Paths are relative: in development, Vite forwards /api to the ASP.NET Core server.
const BASE_PATH = "/api/habits";

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

// Spanish message for each HTTP status (plan §5.3).
export function errorMessageFor(status, body) {
  if (status === 400) {
    return body?.message ?? "Los datos no son válidos.";
  }
  if (status === 401) {
    return "Tu sesión expiró. Vuelve a iniciar sesión.";
  }
  if (status === 404) {
    return "Este hábito ya no existe.";
  }
  return "No se pudo completar la acción. Vuelve a intentarlo.";
}

function buildHeaders(body) {
  const headers = {};
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }
  const token = readToken();
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

async function request(method, path, body) {
  const sessionAtStart = sessionGeneration();
  let response;
  try {
    response = await fetch(`${BASE_PATH}${path}`, {
      method,
      headers: buildHeaders(body),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, "No hay conexión con el servidor.");
  }

  updateSession(response, sessionAtStart);

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    throw new ApiError(response.status, errorMessageFor(response.status, errorBody));
  }

  // Some successful responses have no body (for example, an unlog).
  return response.json().catch(() => null);
}

export const listHabits = () => request("GET", "");

export const createHabit = (data) => request("POST", "", data);

export const updateHabit = (id, data) => request("PUT", `/${id}`, data);

export const deleteHabit = (id) => request("DELETE", `/${id}`);

export const logHabit = (id, date) => request("POST", `/${id}/log`, { date });

export const unlogHabit = (id, date) => request("DELETE", `/${id}/log/${date}`);
