import { describe, it, beforeEach, afterEach } from "node:test";
import assert from "node:assert/strict";
import {
  archiveIdea,
  createIdea,
  deleteIdea,
  getIdea,
  listIdeas,
  listTags,
  restoreIdea,
  updateIdea,
} from "./ideasApi.js";
import { ApiError } from "./http.js";
import { clearToken, saveToken } from "./session.js";

// The token and session rules are tested in http.test.js; here only the endpoints matter.
describe("ideas endpoints", () => {
  const originalFetch = globalThis.fetch;
  let sent;

  const respondWith = (status, body) => {
    globalThis.fetch = async (url, options) => {
      sent = { url, method: options.method, body: options.body && JSON.parse(options.body) };
      return new Response(body === undefined ? null : JSON.stringify(body), { status });
    };
  };

  beforeEach(() => {
    clearToken();
    saveToken("token-actual");
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("lists the active section by default, without empty search or tag", async () => {
    respondWith(200, { ideas: [] });

    await listIdeas();

    assert.equal(sent.method, "GET");
    assert.equal(sent.url, "/api/ideas?archived=false");
  });

  it("lists the archived section with search and tag encoded", async () => {
    respondWith(200, { ideas: [] });

    await listIdeas({ archived: true, search: "sueño app", tag: "Salud & más" });

    const params = new URL(sent.url, "http://localhost").searchParams;
    assert.equal(params.get("archived"), "true");
    assert.equal(params.get("search"), "sueño app");
    assert.equal(params.get("tag"), "Salud & más");
  });

  it("leaves out a search that is only spaces", async () => {
    respondWith(200, { ideas: [] });

    await listIdeas({ search: "   " });

    assert.equal(new URL(sent.url, "http://localhost").searchParams.has("search"), false);
  });

  it("gets one idea at /api/ideas/{id}", async () => {
    respondWith(200, { id: 7 });

    await getIdea(7);

    assert.equal(sent.method, "GET");
    assert.equal(sent.url, "/api/ideas/7");
  });

  it("creates an idea with its text and tags", async () => {
    respondWith(200, { id: 8 });

    await createIdea({ text: "Una idea", tags: ["app"] });

    assert.equal(sent.method, "POST");
    assert.equal(sent.url, "/api/ideas");
    assert.deepEqual(sent.body, { text: "Una idea", tags: ["app"] });
  });

  it("sends the version the owner opened when updating", async () => {
    respondWith(200, { id: 7, version: 12 });

    await updateIdea(7, { text: "Nuevo texto", tags: [], version: 11 });

    assert.equal(sent.method, "PUT");
    assert.equal(sent.url, "/api/ideas/7");
    assert.deepEqual(sent.body, { text: "Nuevo texto", tags: [], version: 11 });
  });

  it("rejects an update with the API message and status, so the screen can warn about changes", async () => {
    respondWith(409, { message: "Esta idea cambió mientras la editabas. Esta es la versión nueva." });

    await assert.rejects(updateIdea(7, { text: "x", tags: [], version: 1 }), (error) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.status, 409);
      assert.equal(error.message, "Esta idea cambió mientras la editabas. Esta es la versión nueva.");
      return true;
    });
  });

  it("archives and restores one idea", async () => {
    respondWith(200);
    await archiveIdea(7);
    assert.equal(sent.method, "POST");
    assert.equal(sent.url, "/api/ideas/7/archive");

    respondWith(200);
    await restoreIdea(7);
    assert.equal(sent.method, "POST");
    assert.equal(sent.url, "/api/ideas/7/restore");
  });

  it("deletes one idea at /api/ideas/{id}", async () => {
    respondWith(200);

    await deleteIdea(7);

    assert.equal(sent.method, "DELETE");
    assert.equal(sent.url, "/api/ideas/7");
  });

  it("lists the owner's tags with their counts", async () => {
    respondWith(200, [{ name: "app", activeCount: 1, archivedCount: 0 }]);

    const tags = await listTags();

    assert.equal(sent.url, "/api/ideas/tags");
    assert.deepEqual(tags, [{ name: "app", activeCount: 1, archivedCount: 0 }]);
  });
});
