import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { toggleId } from "./toggleId.js";

describe("toggleId", () => {
  it("adds an id that is not in the set", () => {
    assert.deepEqual(toggleId(new Set([1]), 2), new Set([1, 2]));
  });

  it("removes an id that is already in the set", () => {
    assert.deepEqual(toggleId(new Set([1, 2]), 1), new Set([2]));
  });

  it("returns a new set and leaves the original untouched, so React sees the change", () => {
    const original = new Set([1]);
    const next = toggleId(original, 2);
    assert.notEqual(next, original);
    assert.deepEqual(original, new Set([1]));
  });
});
