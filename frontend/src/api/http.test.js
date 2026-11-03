import { describe, it, beforeEach, afterEach } from "node:test";
import assert from "node:assert/strict";
import { ApiError, apiUrl, errorMessageFor, request, setUnauthorizedHandler } from "./http.js";
import { clearToken, readToken, saveToken } from "./session.js";

const API_URL = "/api/habits";

describe("apiUrl", () => {
  it("keeps a relative path when there is no base (development proxy)", () => {
    assert.equal(apiUrl("/api/habits", ""), "/api/habits");
  });

  it("puts the path after the base", () => {
    assert.equal(apiUrl("/api/habits", "https://api.example.com"), "https://api.example.com/api/habits");
  });

  it("does not double the slash when the base ends with one", () => {
    assert.equal(apiUrl("/api/habits", "https://api.example.com/"), "https://api.example.com/api/habits");
  });
});

describe("errorMessageFor", () => {
  it("shows the message sent by the API", () => {
    assert.equal(
      errorMessageFor(400, { message: "El nombre es obligatorio." }),
      "El nombre es obligatorio.",
    );
  });

  it("shows the API message for a 404, not a fixed one", () => {
    assert.equal(
      errorMessageFor(404, { message: "Esta idea ya no existe." }),
      "Esta idea ya no existe.",
    );
  });

  it("shows the API message for a 409", () => {
    assert.equal(
      errorMessageFor(409, { message: "Ya existe una cuenta." }),
      "Ya existe una cuenta.",
    );
  });

  it("shows a generic message for a 400 without a message", () => {
    assert.equal(errorMessageFor(400, { title: "Validation failed" }), "Los datos no son válidos.");
  });

  it("shows a generic message for a 404 without a message", () => {
    assert.equal(errorMessageFor(404, null), "No se pudo completar la acción. Vuelve a intentarlo.");
  });

  it("says the session expired for a 401 without a message", () => {
    assert.equal(errorMessageFor(401, null), "Tu sesión expiró. Vuelve a iniciar sesión.");
  });

  it("shows a generic message for any other status", () => {
    assert.equal(errorMessageFor(500, null), "No se pudo completar la acción. Vuelve a intentarlo.");
  });
});

describe("request", () => {
  const originalFetch = globalThis.fetch;
  const okBody = JSON.stringify({ ok: true });

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
      return new Response(okBody, { status: 200 });
    };

    await request("GET", API_URL);

    assert.equal(sentHeaders.Authorization, "Bearer token-actual");
  });

  it("sends no token for a public call", async () => {
    saveToken("token-actual");
    let sentHeaders;
    globalThis.fetch = async (_url, options) => {
      sentHeaders = options.headers;
      return new Response(okBody, { status: 200 });
    };

    await request("POST", "/api/auth/login", { email: "a@b.c" }, { session: false });

    assert.equal(sentHeaders.Authorization, undefined);
  });

  it("sends JSON with Content-Type only when there is a body", async () => {
    let sentHeaders;
    globalThis.fetch = async (_url, options) => {
      sentHeaders = options.headers;
      return new Response(okBody, { status: 200 });
    };

    await request("GET", API_URL);
    assert.equal(sentHeaders["Content-Type"], undefined);

    await request("POST", API_URL, { name: "Leer" });
    assert.equal(sentHeaders["Content-Type"], "application/json");
  });

  it("saves the renewed token from X-Refreshed-Token", async () => {
    saveToken("token-viejo");
    globalThis.fetch = async () =>
      new Response(okBody, { status: 200, headers: { "X-Refreshed-Token": "token-renovado" } });

    await request("GET", API_URL);

    assert.equal(readToken(), "token-renovado");
  });

  it("ignores a renewed token that arrives after the user signed out", async () => {
    saveToken("token-viejo");
    let respond;
    globalThis.fetch = () => new Promise((resolve) => {
      respond = resolve;
    });

    const pending = request("GET", API_URL);
    clearToken(); // "Salir" while the request is still in flight
    respond(new Response(okBody, { status: 200, headers: { "X-Refreshed-Token": "token-renovado" } }));
    await pending;

    assert.equal(readToken(), null);
  });

  it("clears the token and notifies the app on a 401 of a session call", async () => {
    saveToken("token-vencido");
    let notified = false;
    setUnauthorizedHandler(() => {
      notified = true;
    });
    globalThis.fetch = async () => new Response(null, { status: 401 });

    await assert.rejects(request("GET", API_URL));

    assert.equal(readToken(), null);
    assert.equal(notified, true);
  });

  it("keeps the session and shows the API message when a public call gets a 401", async () => {
    saveToken("token-vigente");
    let notified = false;
    setUnauthorizedHandler(() => {
      notified = true;
    });
    globalThis.fetch = async () =>
      new Response(JSON.stringify({ message: "Email o contraseña incorrectos." }), { status: 401 });

    await assert.rejects(
      request("POST", "/api/auth/login", {}, { session: false }),
      (error) => error instanceof ApiError && error.message === "Email o contraseña incorrectos.",
    );

    assert.equal(readToken(), "token-vigente");
    assert.equal(notified, false);
  });

  it("throws an ApiError with the status and the API message", async () => {
    globalThis.fetch = async () =>
      new Response(JSON.stringify({ message: "Este hábito ya no existe." }), { status: 404 });

    await assert.rejects(request("DELETE", `${API_URL}/7`), (error) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.status, 404);
      assert.equal(error.message, "Este hábito ya no existe.");
      return true;
    });
  });

  it("throws a connection error with status 0 when the server cannot be reached", async () => {
    globalThis.fetch = async () => {
      throw new TypeError("fetch failed");
    };

    await assert.rejects(request("GET", API_URL), (error) => {
      assert.equal(error.status, 0);
      assert.equal(error.message, "No hay conexión con el servidor.");
      return true;
    });
  });

  it("returns null for a successful response without a body", async () => {
    globalThis.fetch = async () => new Response(null, { status: 200 });

    assert.equal(await request("DELETE", `${API_URL}/1/log/2026-10-16`), null);
  });
});
