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

import { countByWeek, weeklyStreakFromCounts } from "./streak.js";

describe("weeklyStreakFromCounts", () => {
  const histories = [
    { logs: ["2026-10-12", "2026-10-13", "2026-10-05", "2026-10-06"], goal: 2, created: CREATED_LONG_AGO, expected: 2 },
    { logs: ["2026-10-05", "2026-10-06", "2026-10-12"], goal: 2, created: CREATED_LONG_AGO, expected: 1 },
    { logs: ["2026-10-05", "2026-10-12"], goal: 2, created: CREATED_LONG_AGO, expected: 0 },
    { logs: ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"], goal: 3, created: CREATED_LONG_AGO, expected: 1 },
    { logs: ["2026-10-12", "2026-10-13"], goal: 3, created: "2026-10-12", expected: 0 },
    { logs: ["2026-10-12", "2026-10-13"], goal: 2, created: "2026-10-12", expected: 1 },
  ];

  it("gives the same streak as weeklyStreak for the same history", () => {
    for (const { logs, goal, created, expected } of histories) {
      const fromCounts = weeklyStreakFromCounts(countByWeek(logs), goal, created, TODAY);
      assert.equal(fromCounts, expected);
      assert.equal(fromCounts, weeklyStreak(logs, goal, created, TODAY));
    }
  });
});

import { bestDailyStreak } from "./streak.js";

describe("bestDailyStreak", () => {
  it("is 0 with no logs", () => {
    assert.equal(bestDailyStreak([]), 0);
  });

  it("is 1 for a single log", () => {
    assert.equal(bestDailyStreak(["2026-10-01"]), 1);
  });

  it("finds an old run that is longer than the current one", () => {
    const logs = ["2026-09-01", "2026-09-02", "2026-09-03", "2026-09-04", "2026-10-15", "2026-10-16"];
    assert.equal(bestDailyStreak(logs), 4);
    assert.equal(dailyStreak(logs, TODAY), 2);
  });

  it("does not break the run when today is not marked yet", () => {
    const logs = ["2026-10-13", "2026-10-14", "2026-10-15"];
    assert.equal(bestDailyStreak(logs), 3);
  });

  it("adds today when it is already marked", () => {
    const logs = ["2026-10-13", "2026-10-14", "2026-10-15", "2026-10-16"];
    assert.equal(bestDailyStreak(logs), 4);
  });

  it("does not depend on the order of the dates", () => {
    assert.equal(bestDailyStreak(["2026-10-16", "2026-10-14", "2026-10-15", "2026-10-01"]), 3);
  });

  it("counts runs that cross a month and a year", () => {
    assert.equal(bestDailyStreak(["2025-12-30", "2025-12-31", "2026-01-01"]), 3);
  });

  it("counts a run longer than a year", () => {
    const logs = Array.from({ length: 400 }, (_, i) => addDays(TODAY, -i));
    assert.equal(bestDailyStreak(logs), 400);
  });

  it("is never smaller than the current streak", () => {
    const histories = [
      [],
      ["2026-10-16"],
      ["2026-10-15"],
      ["2026-10-13"],
      ["2026-10-16", "2026-10-15", "2026-10-10"],
      ["2026-10-15", "2026-10-14", "2026-09-01", "2026-09-02", "2026-09-03"],
    ];
    for (const logs of histories) {
      assert.ok(bestDailyStreak(logs) >= dailyStreak(logs, TODAY), `history ${logs.join(",")}`);
    }
  });
});

import { bestWeeklyStreak } from "./streak.js";

describe("bestWeeklyStreak", () => {
  const best = (logs, goal) => bestWeeklyStreak(countByWeek(logs), goal);

  it("is 0 with no logs", () => {
    assert.equal(best([], 2), 0);
  });

  it("is 0 when no week reached the goal", () => {
    assert.equal(best(["2026-10-05", "2026-10-12"], 2), 0);
  });

  it("finds an old run that is longer than the current one", () => {
    const logs = [
      "2026-09-07", "2026-09-08", "2026-09-14", "2026-09-15", "2026-09-21", "2026-09-22",
      "2026-10-12", "2026-10-13",
    ];
    assert.equal(best(logs, 2), 3);
    assert.equal(weeklyStreak(logs, 2, CREATED_LONG_AGO, TODAY), 1);
  });

  it("counts the current week when it already reached the goal", () => {
    const logs = ["2026-10-05", "2026-10-06", "2026-10-12", "2026-10-13"];
    assert.equal(best(logs, 2), 2);
  });

  it("does not break the run when the current week has not reached the goal", () => {
    const logs = ["2026-09-28", "2026-09-29", "2026-10-05", "2026-10-06", "2026-10-12"];
    assert.equal(best(logs, 2), 2);
  });

  it("counts a week with more logs than the goal as one week (4 of 3)", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"];
    assert.equal(best(logs, 3), 1);
  });

  it("breaks the run at a week that missed the goal", () => {
    const logs = ["2026-09-28", "2026-09-29", "2026-10-05", "2026-10-12", "2026-10-13"];
    assert.equal(best(logs, 2), 1);
  });

  it("is never smaller than the current streak", () => {
    const histories = [
      { logs: [], goal: 2 },
      { logs: ["2026-10-12", "2026-10-13"], goal: 2 },
      { logs: ["2026-10-05", "2026-10-06", "2026-10-12"], goal: 2 },
      { logs: ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"], goal: 3 },
      { logs: ["2026-09-07", "2026-09-08", "2026-10-05", "2026-10-06", "2026-10-12", "2026-10-13"], goal: 2 },
    ];
    for (const { logs, goal } of histories) {
      const current = weeklyStreak(logs, goal, CREATED_LONG_AGO, TODAY);
      assert.ok(best(logs, goal) >= current, `history ${logs.join(",")}`);
    }
  });
});
