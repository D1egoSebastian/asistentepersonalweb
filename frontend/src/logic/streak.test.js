import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { addDays } from "./dates.js";
import { dailyStreak, weeklyStreak, weekProgress } from "./streak.js";

// 2026-10-16 is a Friday. Its week runs from Monday 2026-10-12 to Sunday 2026-10-18.
const TODAY = "2026-10-16";
const CREATED_LONG_AGO = "2026-08-31";

describe("dailyStreak", () => {
  it("counts today and the days before it", () => {
    assert.equal(dailyStreak(["2026-10-16", "2026-10-15"], TODAY), 2);
  });

  it("counts from yesterday when today is not marked yet", () => {
    assert.equal(dailyStreak(["2026-10-15"], TODAY), 1);
  });

  it("is 0 when neither today nor yesterday is marked", () => {
    assert.equal(dailyStreak(["2026-10-13"], TODAY), 0);
  });

  it("stops at a gap in the middle", () => {
    assert.equal(dailyStreak(["2026-10-16", "2026-10-14"], TODAY), 1);
  });

  it("counts streaks longer than a year", () => {
    const logs = Array.from({ length: 400 }, (_, i) => addDays(TODAY, -i));
    assert.equal(dailyStreak(logs, TODAY), 400);
  });

  it("is 0 with no logs", () => {
    assert.equal(dailyStreak([], TODAY), 0);
  });
});

describe("weekProgress", () => {
  it("counts only the logs of the current week (Monday to Sunday)", () => {
    assert.equal(weekProgress(["2026-10-12", "2026-10-13", "2026-10-05"], TODAY), 2);
  });

  it("can go above the goal", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"];
    assert.equal(weekProgress(logs, TODAY), 4);
  });
});

describe("weeklyStreak", () => {
  it("counts the current week when it reached the goal, plus earlier met weeks", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-05", "2026-10-06"];
    assert.equal(weeklyStreak(logs, 2, CREATED_LONG_AGO, TODAY), 2);
  });

  it("counts from last week while the current week has not reached the goal", () => {
    const logs = ["2026-10-05", "2026-10-06", "2026-10-12"];
    assert.equal(weeklyStreak(logs, 2, CREATED_LONG_AGO, TODAY), 1);
  });

  it("is 0 when last week and the current week both missed the goal", () => {
    const logs = ["2026-10-05", "2026-10-12"];
    assert.equal(weeklyStreak(logs, 2, CREATED_LONG_AGO, TODAY), 0);
  });

  it("counts a week with more logs than the goal (4 of 3)", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"];
    assert.equal(weeklyStreak(logs, 3, CREATED_LONG_AGO, TODAY), 1);
  });

  it("is 0 when the creation week did not reach the goal", () => {
    const logs = ["2026-10-12", "2026-10-13"];
    assert.equal(weeklyStreak(logs, 3, "2026-10-12", TODAY), 0);
  });

  it("counts the creation week when it reached the goal", () => {
    const logs = ["2026-10-12", "2026-10-13"];
    assert.equal(weeklyStreak(logs, 2, "2026-10-12", TODAY), 1);
  });

  it("is 0 after weeks without use", () => {
    assert.equal(weeklyStreak(["2026-08-31"], 1, "2026-08-31", TODAY), 0);
  });
});
