import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { addDays, weekStart } from "./dates.js";
import { countByWeek } from "./streak.js";
import { closedWeeks, weekCompletion, trend } from "./trend.js";

// 2026-10-16 is a Friday. Its week runs from Monday 2026-10-12 to Sunday 2026-10-18.
const TODAY = "2026-10-16";

describe("closedWeeks", () => {
  it("returns 12 Mondays, from the oldest to the newest", () => {
    const weeks = closedWeeks(TODAY, 12);
    assert.equal(weeks.length, 12);
    assert.equal(weeks[0], "2026-07-20");
    assert.equal(weeks[11], "2026-10-05");
    for (const week of weeks) {
      assert.equal(weekStart(week), week);
    }
    assert.deepEqual(weeks, [...weeks].sort());
  });

  it("leaves out the current week", () => {
    assert.ok(!closedWeeks(TODAY, 12).includes("2026-10-12"));
    assert.ok(closedWeeks(TODAY, 12).every((week) => week < "2026-10-12"));
  });

  it("ends at the Monday of the previous week when today is a Monday", () => {
    const weeks = closedWeeks("2026-10-19", 12);
    assert.equal(weeks[11], "2026-10-12");
    assert.equal(weeks[0], "2026-07-27");
  });

  it("ends at the Monday of the previous week when today is a Sunday", () => {
    assert.equal(closedWeeks("2026-10-18", 3)[2], "2026-10-05");
  });

  it("returns as many weeks as asked", () => {
    assert.deepEqual(closedWeeks(TODAY, 3), ["2026-09-21", "2026-09-28", "2026-10-05"]);
  });
});

const daily = (logDates, createdOn = "2026-01-05") => ({
  frequency: "Daily",
  weeklyGoal: null,
  createdOn,
  logDates,
});

const weekly = (logDates, weeklyGoal, createdOn = "2026-01-05") => ({
  frequency: "Weekly",
  weeklyGoal,
  createdOn,
  logDates,
});

// The habits arrive already indexed: the weekly counts are computed once per habit.
const indexed = (habit) => ({ ...habit, counts: countByWeek(habit.logDates) });

// Every day of the week that starts on `monday`.
const wholeWeek = (monday) => Array.from({ length: 7 }, (_, i) => addDays(monday, i));

describe("weekCompletion", () => {
  it("counts the logged days over the 7 days of a daily habit", () => {
    const habit = indexed(daily(["2026-09-28", "2026-09-30", "2026-10-03"]));
    assert.deepEqual(weekCompletion(habit, habit.counts, "2026-09-28"), { met: 3, total: 7 });
  });

  it("counts only the days since creation in the creation week of a daily habit", () => {
    // Created on Thursday 2026-10-01: Thursday to Sunday are 4 days.
    const habit = indexed(daily(["2026-10-01", "2026-10-02"], "2026-10-01"));
    assert.deepEqual(weekCompletion(habit, habit.counts, "2026-09-28"), { met: 2, total: 4 });
  });

  it("has no data for a daily habit created after the week", () => {
    const habit = indexed(daily([], "2026-10-08"));
    assert.deepEqual(weekCompletion(habit, habit.counts, "2026-09-28"), { met: 0, total: 0 });
  });

  it("is a full week met or missed for a weekly habit (CA-7.9)", () => {
    const habit = indexed(weekly(["2026-09-28", "2026-09-29", "2026-10-05"], 2));
    assert.deepEqual(weekCompletion(habit, habit.counts, "2026-09-28"), { met: 1, total: 1 });
    assert.deepEqual(weekCompletion(habit, habit.counts, "2026-10-05"), { met: 0, total: 1 });
  });

  it("has no data for a weekly habit created after the week", () => {
    const habit = indexed(weekly([], 2, "2026-10-08"));
    assert.deepEqual(weekCompletion(habit, habit.counts, "2026-09-28"), { met: 0, total: 0 });
  });
});

describe("trend", () => {
  it("has one point per closed week, from the oldest to the newest, and never the current week", () => {
    const points = trend([indexed(daily([]))], TODAY);
    assert.equal(points.length, 12);
    assert.deepEqual(points.map((point) => point.monday), closedWeeks(TODAY, 12));
    assert.ok(points.every((point) => point.monday !== "2026-10-12"));
  });

  it("gives the percent of the week for a daily habit", () => {
    const logs = [...wholeWeek("2026-10-05"), "2026-09-28", "2026-09-29", "2026-09-30"];
    const points = trend([indexed(daily(logs))], TODAY);
    assert.equal(points[11].percent, 100);
    assert.equal(points[10].percent, (3 * 100) / 7);
    assert.equal(points[9].percent, 0);
  });

  it("ignores the logs of the current week (CA-5.3)", () => {
    const withCurrentWeek = trend([indexed(daily([...wholeWeek("2026-10-12")]))], TODAY);
    assert.deepEqual(withCurrentWeek, trend([indexed(daily([]))], TODAY));
  });

  it("uses the days since creation in the creation week", () => {
    // Created on Thursday 2026-10-01, with two logs: 2 of 4 days. Earlier weeks have no data.
    const points = trend([indexed(daily(["2026-10-01", "2026-10-02"], "2026-10-01"))], TODAY);
    assert.equal(points[10].monday, "2026-09-28");
    assert.equal(points[10].percent, 50);
    assert.equal(points[9].percent, null);
    assert.equal(points[0].percent, null);
  });

  it("is 0 or 100 for a weekly habit in each week (CA-7.9)", () => {
    const habit = indexed(weekly(["2026-10-05", "2026-10-06", "2026-09-28"], 2));
    const points = trend([habit], TODAY);
    assert.equal(points[11].percent, 100);
    assert.equal(points[10].percent, 0);
  });

  it("leaves a week without habits as null, not 0 (CA-7.4)", () => {
    const points = trend([indexed(daily([], "2026-10-12"))], TODAY);
    assert.ok(points.every((point) => point.percent === null));
    assert.deepEqual(trend([], TODAY).map((point) => point.percent), Array(12).fill(null));
  });

  it("does not count a habit in the weeks before it existed", () => {
    const old = indexed(daily(wholeWeek("2026-09-21")));
    const recent = indexed(daily([], "2026-10-01"));
    const points = trend([old, recent], TODAY);
    // Week of 2026-09-21: only the old habit existed (100 %). Week of 2026-09-28: old 0 %, recent 0 % of 4 days.
    assert.equal(points[9].percent, 100);
    assert.equal(points[10].percent, 0);
  });

  it("averages the habits with the same weight, whatever their frequency", () => {
    const dailyHabit = indexed(daily(wholeWeek("2026-10-05")));
    const weeklyHabit = indexed(weekly([], 3));
    assert.equal(trend([dailyHabit, weeklyHabit], TODAY)[11].percent, 50);
    const partial = indexed(daily(["2026-10-05", "2026-10-06", "2026-10-07"]));
    assert.equal(trend([dailyHabit, partial], TODAY)[11].percent, (100 + (3 * 100) / 7) / 2);
  });
});
