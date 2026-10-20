import { describe, it, beforeEach, afterEach } from "node:test";
import assert from "node:assert/strict";
import { errorMessageFor, listHabits, setUnauthorizedHandler } from "./habitsApi.js";
import { clearToken, readToken, saveToken } from "./session.js";

describe("errorMessageFor", () => {
  it("shows the message sent by the API for a 400", () => {
    assert.equal(
      errorMessageFor(400, { message: "El nombre es obligatorio." }),
      "El nombre es obligatorio.",
    );
  });

  it("shows a generic message for a 400 without a message", () => {
    assert.equal(errorMessageFor(400, { title: "Validation failed" }), "Los datos no son válidos.");
  });

  it("says the habit no longer exists for a 404", () => {
    assert.equal(errorMessageFor(404, null), "Este hábito ya no existe.");
  });

  it("says the session expired for a 401", () => {
    assert.equal(errorMessageFor(401, null), "Tu sesión expiró. Vuelve a iniciar sesión.");
  });

  it("shows a generic message for any other status", () => {
    assert.equal(errorMessageFor(500, null), "No se pudo completar la acción. Vuelve a intentarlo.");
  });
});

describe("habits requests and the session", () => {
  const originalFetch = globalThis.fetch;
  const listBody = JSON.stringify({ today: "2026-10-16", habits: [] });

  beforeEach(() => {
    clearToken();
    setUnauthorizedHandler(null);
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    setUnauthorizedHandler(null);
  });

  it("sends the stored token as a Bearer header", async () => {
    saveToken("token-actual");
    let sentHeaders;
    globalThis.fetch = async (_url, options) => {
      sentHeaders = options.headers;
      return new Response(listBody, { status: 200 });
    };

    await listHabits();

    assert.equal(sentHeaders.Authorization, "Bearer token-actual");
  });

  it("saves the renewed token from X-Refreshed-Token", async () => {
    saveToken("token-viejo");
    globalThis.fetch = async () =>
      new Response(listBody, { status: 200, headers: { "X-Refreshed-Token": "token-renovado" } });

    await listHabits();

    assert.equal(readToken(), "token-renovado");
  });

  it("ignores a renewed token that arrives after the user signed out", async () => {
    saveToken("token-viejo");
    let respond;
    globalThis.fetch = () => new Promise((resolve) => {
      respond = resolve;
    });

    const pending = listHabits();
    clearToken(); // "Salir" while the request is still in flight
    respond(new Response(listBody, { status: 200, headers: { "X-Refreshed-Token": "token-renovado" } }));
    await pending;

    assert.equal(readToken(), null);
  });

  it("clears the token and notifies the app on a 401", async () => {
    saveToken("token-vencido");
    let notified = false;
    setUnauthorizedHandler(() => {
      notified = true;
    });
    globalThis.fetch = async () => new Response(null, { status: 401 });

    await assert.rejects(listHabits());

    assert.equal(readToken(), null);
    assert.equal(notified, true);
  });
});
