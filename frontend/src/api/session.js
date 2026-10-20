// Keeps the session token in this browser so it survives closing the tab (spec 002, CA-2.1).
// The browser can block storage (private mode, blocked site data), so every access is in try/catch,
// and a copy in memory keeps the session alive while the page stays open.
const STORAGE_KEY = "asistente-personal.session";

let memoryToken = null;

// Changes every time the session is cleared. A request remembers the value it started with,
// so a response that arrives after "Salir" can be recognized as belonging to the old session.
let generation = 0;

export function sessionGeneration() {
  return generation;
}

export function readToken() {
  try {
    const stored = globalThis.localStorage?.getItem(STORAGE_KEY);
    if (stored) {
      return stored;
    }
  } catch {
    // Storage blocked: fall back to the copy in memory.
  }
  return memoryToken;
}

export function saveToken(token) {
  memoryToken = token;
  try {
    globalThis.localStorage?.setItem(STORAGE_KEY, token);
  } catch {
    // Storage blocked: the session lasts while the page is open.
  }
}

export function clearToken() {
  generation += 1;
  memoryToken = null;
  try {
    globalThis.localStorage?.removeItem(STORAGE_KEY);
  } catch {
    // Storage blocked: nothing stored to remove.
  }
}
