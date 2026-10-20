import { describe, it, beforeEach } from "node:test";
import assert from "node:assert/strict";
import { clearToken, readToken, saveToken } from "./session.js";

describe("session token", () => {
  beforeEach(() => {
    clearToken();
  });

  it("reads back the token that was saved", () => {
    saveToken("token-de-prueba");
    assert.equal(readToken(), "token-de-prueba");
  });

  it("replaces the previous token when a new one is saved", () => {
    saveToken("primero");
    saveToken("renovado");
    assert.equal(readToken(), "renovado");
  });

  it("returns null after the token is cleared", () => {
    saveToken("token-de-prueba");
    clearToken();
    assert.equal(readToken(), null);
  });
});
