import { describe, it, beforeEach, afterEach } from "node:test";
import assert from "node:assert/strict";
import { deleteHabit, listHabits } from "./habitsApi.js";
import { clearToken, saveToken } from "./session.js";

// The token and session rules are tested in http.test.js; here only the endpoints matter.
describe("habits endpoints", () => {
  const originalFetch = globalThis.fetch;
  const listBody = JSON.stringify({ today: "2026-10-16", habits: [] });

  beforeEach(() => {
    clearToken();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("lists habits from /api/habits with the session token", async () => {
    saveToken("token-actual");
    let sent;
    globalThis.fetch = async (url, options) => {
      sent = { url, headers: options.headers };
      return new Response(listBody, { status: 200 });
    };

    await listHabits();

    assert.equal(sent.url, "/api/habits");
    assert.equal(sent.headers.Authorization, "Bearer token-actual");
  });

  it("deletes one habit at /api/habits/{id}", async () => {
    let sent;
    globalThis.fetch = async (url, options) => {
      sent = { url, method: options.method };
      return new Response(null, { status: 200 });
    };

    await deleteHabit(7);

    assert.deepEqual(sent, { url: "/api/habits/7", method: "DELETE" });
  });
});
