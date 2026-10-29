import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { addTag, filterOptions, normalizeTag, splitTags, suggestTags, MAX_TAGS_PER_IDEA } from "./tags.js";

describe("normalizeTag", () => {
  it("trims the ends and removes one leading #", () => {
    assert.equal(normalizeTag("  #Compras "), "Compras");
  });

  it("keeps the rest of the text as written", () => {
    assert.equal(normalizeTag("lluvia de ideas"), "lluvia de ideas");
  });
});

describe("splitTags", () => {
  it("splits a comma-separated text into normalized tags", () => {
    assert.deepEqual(splitTags("libros, cine"), ["libros", "cine"]);
  });

  it("drops empty parts and normalizes each one", () => {
    assert.deepEqual(splitTags(" #App ,, ,b"), ["App", "b"]);
  });
});

describe("suggestTags", () => {
  const allTags = ["Compras", "compras-casa", "Cine", "cafe", "Cocina"];

  it("suggests tags that start with the typed text, ignoring case", () => {
    assert.deepEqual(suggestTags(allTags, "co"), ["Compras", "compras-casa", "Cocina"]);
  });

  it("does not suggest tags already chosen, ignoring case", () => {
    assert.deepEqual(suggestTags(allTags, "co", ["COMPRAS"]), ["compras-casa", "Cocina"]);
  });

  it("suggests nothing for an empty or blank text", () => {
    assert.deepEqual(suggestTags(allTags, "   "), []);
  });

  it("suggests at most 8 tags", () => {
    const many = Array.from({ length: 12 }, (_, index) => `tag${index}`);
    assert.equal(suggestTags(many, "tag").length, 8);
  });
});

describe("addTag", () => {
  it("adds the tag at the end", () => {
    assert.deepEqual(addTag(["cine"], "libros", []), ["cine", "libros"]);
  });

  it("uses the spelling that already exists", () => {
    assert.deepEqual(addTag([], "APP", ["App"]), ["App"]);
  });

  it("does not add a tag twice, ignoring case", () => {
    const chosen = ["App"];
    assert.equal(addTag(chosen, "app", []), chosen);
  });

  it("rejects a blank tag", () => {
    const chosen = ["App"];
    assert.equal(addTag(chosen, "  # ", []), chosen);
  });

  it("rejects a tag when the idea already has the maximum", () => {
    const full = Array.from({ length: MAX_TAGS_PER_IDEA }, (_, index) => `t${index}`);
    assert.equal(addTag(full, "extra", []), full);
  });

  it("does not change the list it receives", () => {
    const chosen = ["cine"];
    addTag(chosen, "libros", []);
    assert.deepEqual(chosen, ["cine"]);
  });
});

describe("filterOptions", () => {
  const tagCounts = [
    { name: "Cine", activeCount: 0, archivedCount: 2 },
    { name: "app", activeCount: 3, archivedCount: 0 },
    { name: "Libros", activeCount: 1, archivedCount: 1 },
  ];

  it("offers only tags with an active idea in the active section, sorted", () => {
    assert.deepEqual(filterOptions(tagCounts, false), ["app", "Libros"]);
  });

  it("offers only tags with an archived idea in the archived section", () => {
    assert.deepEqual(filterOptions(tagCounts, true), ["Cine", "Libros"]);
  });
});
