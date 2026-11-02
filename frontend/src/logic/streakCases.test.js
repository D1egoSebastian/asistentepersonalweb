import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { weekStart } from "./dates.js";
import { dailyStreak } from "./streak.js";

// The same file is read by the backend tests (ReminderRulesTests), so both versions stay in step (spec 005, D-3).
const CASES_URL = new URL("../../../test-cases/daily-streak.json", import.meta.url);
const cases = JSON.parse(readFileSync(CASES_URL, "utf8"));

describe("shared dailyStreak cases", () => {
  it("has cases to run", () => {
    assert.ok(cases.dailyStreakCases.length >= 8);
  });

  for (const { name, today, logDates, dailyStreak: expected } of cases.dailyStreakCases) {
    it(name, () => {
      assert.equal(dailyStreak(logDates, today), expected);
    });
  }
});

describe("shared weekStart cases", () => {
  it("has cases to run", () => {
    assert.ok(cases.weekStartCases.length >= 5);
  });

  for (const { name, date, weekStart: expected } of cases.weekStartCases) {
    it(name, () => {
      assert.equal(weekStart(date), expected);
    });
  }
});
