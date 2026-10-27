import { request } from "./http.js";
import { saveToken } from "./session.js";

// Public calls to start a session. They send no token and never end one (see http.js).
// Their errors carry the API's own message, for example "Email o contraseña incorrectos.".
const BASE_PATH = "/api/auth";

const publicRequest = (method, path, body) =>
  request(method, `${BASE_PATH}${path}`, body, { session: false });

export function getStatus() {
  return publicRequest("GET", "/status");
}

export async function setup(email, password, passwordConfirmation) {
  const session = await publicRequest("POST", "/setup", { email, password, passwordConfirmation });
  saveToken(session.token);
}

export async function login(email, password) {
  const session = await publicRequest("POST", "/login", { email, password });
  saveToken(session.token);
}
