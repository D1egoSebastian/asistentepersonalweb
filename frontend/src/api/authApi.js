import { saveToken } from "./session.js";

// Public calls to start a session. They never send a token, and their errors carry the API's own message
// (for example, "Email o contraseña incorrectos."), not the "session expired" message of habitsApi.
const BASE_PATH = "/api/auth";

async function request(method, path, body) {
  let response;
  try {
    response = await fetch(`${BASE_PATH}${path}`, {
      method,
      headers: body === undefined ? {} : { "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new Error("No hay conexión con el servidor.");
  }

  const data = await response.json().catch(() => null);
  if (!response.ok) {
    // The status lets the screens react to specific cases, like 409 (the account already exists).
    const error = new Error(data?.message ?? "No se pudo completar la acción. Vuelve a intentarlo.");
    error.status = response.status;
    throw error;
  }
  return data;
}

export function getStatus() {
  return request("GET", "/status");
}

export async function setup(email, password, passwordConfirmation) {
  const session = await request("POST", "/setup", { email, password, passwordConfirmation });
  saveToken(session.token);
}

export async function login(email, password) {
  const session = await request("POST", "/login", { email, password });
  saveToken(session.token);
}
