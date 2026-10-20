import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { loggableDays } from "./logWindow.js";

describe("loggableDays", () => {
  it("offers only today for a habit created today", () => {
    assert.deepEqual(loggableDays("2026-10-16", "2026-10-16"), [
      { date: "2026-10-16", label: "Hoy" },
    ]);
  });

  it("offers today and yesterday for a habit created earlier", () => {
    assert.deepEqual(loggableDays("2026-10-01", "2026-10-16"), [
      { date: "2026-10-16", label: "Hoy" },
      { date: "2026-10-15", label: "Ayer" },
    ]);
  });

  it("offers yesterday when the habit was created yesterday", () => {
    assert.deepEqual(loggableDays("2026-10-15", "2026-10-16"), [
      { date: "2026-10-16", label: "Hoy" },
      { date: "2026-10-15", label: "Ayer" },
    ]);
  });

  it("moves the window when today changes (midnight)", () => {
    assert.deepEqual(loggableDays("2026-10-01", "2026-10-17"), [
      { date: "2026-10-17", label: "Hoy" },
      { date: "2026-10-16", label: "Ayer" },
    ]);
  });

  it("drops yesterday after midnight when the habit was created the day before", () => {
    // Created on 16th: on the 17th, the 16th is still yesterday and is allowed
    assert.deepEqual(loggableDays("2026-10-16", "2026-10-17"), [
      { date: "2026-10-17", label: "Hoy" },
      { date: "2026-10-16", label: "Ayer" },
    ]);
  });
});
