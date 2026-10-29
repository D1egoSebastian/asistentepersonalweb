import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { addDays } from "./dates.js";
import { toMetricsView } from "./metricsView.js";

// 2026-10-16 is a Friday. Its week runs from Monday 2026-10-12 to Sunday 2026-10-18.
const TODAY = "2026-10-16";
const NONE = new Set();

const daily = (logDates, createdOn = "2026-08-31", id = 1) => ({
  id,
  name: `Daily ${id}`,
  description: null,
  frequency: "Daily",
  weeklyGoal: null,
  createdOn,
  logDates,
});

const weekly = (logDates, weeklyGoal, createdOn = "2026-08-31", id = 2) => ({
  id,
  name: `Weekly ${id}`,
  description: null,
  frequency: "Weekly",
  weeklyGoal,
  createdOn,
  logDates,
});

// `count` consecutive days starting on `from`.
const run = (from, count) => Array.from({ length: count }, (_, i) => addDays(from, i));

describe("toMetricsView: empty list and overall percent", () => {
  it("is empty without habits, with 'sin datos' everywhere and no habit cards", () => {
    const view = toMetricsView([], TODAY, NONE);
    assert.equal(view.isEmpty, true);
    assert.deepEqual(view.habits, []);
    assert.deepEqual(view.overall, { sinceCreation: "sin datos", lastFourWeeks: "sin datos" });
  });

  it("is not empty with one habit", () => {
    assert.equal(toMetricsView([daily([])], TODAY, NONE).isEmpty, false);
  });

  it("shows the overall percent of both windows as rounded text", () => {
    // Created 2026-09-21: 25 closed days since creation, 21 of them inside the four weeks.
    // 18 logged days (09-21 to 10-08): 18 of 25 = 72 %, and 18 of 21 = 85.7 %.
    const habit = daily(run("2026-09-21", 18), "2026-09-21");
    const view = toMetricsView([habit], TODAY, NONE);
    assert.deepEqual(view.overall, { sinceCreation: "72 %", lastFourWeeks: "86 %" });
  });

  it("shows 'sin datos' when every habit is without data in a window (CA-4.4)", () => {
    const view = toMetricsView([daily([], TODAY), weekly([], 2, TODAY, 2)], TODAY, NONE);
    assert.deepEqual(view.overall, { sinceCreation: "sin datos", lastFourWeeks: "sin datos" });
  });

  it("leaves a habit without data out of the average of that window (CA-4.3)", () => {
    // The young habit only has data since creation (2 days, none logged): 0 %. The old one has both windows.
    const old = daily(run("2026-08-31", 46), "2026-08-31", 1);
    const young = daily([], "2026-10-14", 2);
    const view = toMetricsView([old, young], TODAY, NONE);
    assert.deepEqual(view.overall, { sinceCreation: "50 %", lastFourWeeks: "100 %" });
  });

  it("averages the exact percents, not the rounded ones (CA-4.5)", () => {
    // Three habits created on 2026-10-13 (3 closed days): 2 of 3, 2 of 3 and 0 of 3.
    // Exact average 44.4 % -> "44 %". The average of the shown values (67, 67, 0) would be 45.
    const habits = [
      daily(["2026-10-13", "2026-10-14"], "2026-10-13", 1),
      daily(["2026-10-14", "2026-10-15"], "2026-10-13", 2),
      daily([], "2026-10-13", 3),
    ];
    const view = toMetricsView(habits, TODAY, NONE);
    assert.equal(view.overall.sinceCreation, "44 %");
    assert.equal(view.overall.lastFourWeeks, "sin datos");
  });

  it("weighs every habit the same, whatever its frequency (CA-4.2)", () => {
    // Daily at 100 % (46 of 46) and weekly at 0 % (6 weeks, none met): 50 %.
    const habits = [daily(run("2026-08-31", 46), "2026-08-31", 1), weekly([], 3, "2026-08-31", 2)];
    assert.equal(toMetricsView(habits, TODAY, NONE).overall.sinceCreation, "50 %");
  });

  it("keeps the order in which the habits arrive (CA-1.6)", () => {
    const habits = [daily([], "2026-08-31", 3), weekly([], 2, "2026-09-01", 1), daily([], "2026-09-02", 2)];
    const view = toMetricsView(habits, TODAY, NONE);
    assert.deepEqual(view.habits.map((habit) => habit.id), [3, 1, 2]);
    assert.deepEqual(view.habits.map((habit) => habit.name), ["Daily 3", "Weekly 1", "Daily 2"]);
  });
});

const cardOf = (habit, today = TODAY) => toMetricsView([habit], today, NONE).habits[0];

describe("toMetricsView: habit card", () => {
  it("shows the frequency", () => {
    assert.equal(cardOf(daily([])).frequencyLabel, "Diario");
    assert.equal(cardOf(weekly([], 3)).frequencyLabel, "3 veces por semana");
    assert.equal(cardOf(weekly([], 1)).frequencyLabel, "1 vez por semana");
  });

  it("shows the percent and the base of both windows for a daily habit", () => {
    // 21 logs inside the four weeks (09-14 to 10-04), none after: 21 of 28 and 21 of 46 closed days.
    const card = cardOf(daily(run("2026-09-14", 21)));
    assert.deepEqual(card.lastFourWeeks, { percent: "75 %", base: "21 de 28 días" });
    assert.deepEqual(card.sinceCreation, { percent: "46 %", base: "21 de 46 días" });
  });

  it("shows 20 of 30 days as 67 %", () => {
    const card = cardOf(daily(run("2026-09-16", 20), "2026-09-16"));
    assert.deepEqual(card.sinceCreation, { percent: "67 %", base: "20 de 30 días" });
  });

  it("shows the percent and the base in weeks for a weekly habit", () => {
    // Created on Monday 2026-08-17: 8 closed weeks. Met (1 log, goal 1): the first five.
    const logs = ["2026-08-17", "2026-08-24", "2026-08-31", "2026-09-07", "2026-09-14"];
    const card = cardOf(weekly(logs, 1, "2026-08-17"));
    assert.deepEqual(card.sinceCreation, { percent: "63 %", base: "5 de 8 semanas" });
    assert.deepEqual(card.lastFourWeeks, { percent: "25 %", base: "1 de 4 semanas" });
  });

  it("rounds 66.5 up to 67 only in the text", () => {
    // Created 2026-03-30: 200 closed days, 133 logged.
    const card = cardOf(daily(run("2026-03-30", 133), "2026-03-30"));
    assert.deepEqual(card.sinceCreation, { percent: "67 %", base: "133 de 200 días" });
  });

  it("uses the singular unit for a base of one", () => {
    assert.equal(cardOf(daily(["2026-10-15"], "2026-10-15")).sinceCreation.base, "1 de 1 día");
    assert.equal(cardOf(weekly(["2026-10-05"], 1, "2026-10-05")).sinceCreation.base, "1 de 1 semana");
  });

  it("shows 'sin datos' and no base in a window without evaluable periods (CA-2.5)", () => {
    const card = cardOf(daily([], TODAY));
    assert.deepEqual(card.sinceCreation, { percent: "sin datos", base: null });
    assert.deepEqual(card.lastFourWeeks, { percent: "sin datos", base: null });
  });

  it("shows 0 % with its base, not 'sin datos', when there are periods and none was met", () => {
    assert.deepEqual(cardOf(weekly([], 2, "2026-10-05")).sinceCreation, { percent: "0 %", base: "0 de 1 semana" });
  });

  it("shows whether a daily habit is done today", () => {
    assert.equal(cardOf(daily([TODAY])).currentLabel, "Hoy: hecho");
    assert.equal(cardOf(daily(["2026-10-15"])).currentLabel, "Hoy: pendiente");
  });

  it("shows the progress of the current week for a weekly habit, even above the goal", () => {
    assert.equal(cardOf(weekly(["2026-10-12", "2026-10-13"], 3)).currentLabel, "Esta semana: 2 de 3");
    const above = weekly(["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"], 3);
    assert.equal(cardOf(above).currentLabel, "Esta semana: 4 de 3");
  });

  it("shows the streaks with their unit for a daily habit", () => {
    // Current run: 5 days ending today. Older run: 9 days.
    const logs = [...run("2026-10-12", 5), ...run("2026-09-01", 9)];
    const card = cardOf(daily(logs));
    assert.equal(card.streakLabel, "5 días");
    assert.equal(card.bestStreakLabel, "9 días");
  });

  it("shows the same value in both streaks when the best one is the current one", () => {
    const card = cardOf(daily(["2026-10-16"]));
    assert.equal(card.streakLabel, "1 día");
    assert.equal(card.bestStreakLabel, "1 día");
  });

  it("shows 0 as the best streak when the habit has no logs (CA-6.5)", () => {
    const card = cardOf(daily([]));
    assert.equal(card.streakLabel, "0 días");
    assert.equal(card.bestStreakLabel, "0 días");
  });

  it("shows the streaks in weeks for a weekly habit, and the best one is never below the current one", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-05", "2026-10-06", "2026-09-07", "2026-09-08"];
    const card = cardOf(weekly(logs, 2));
    assert.equal(card.streakLabel, "2 semanas");
    assert.equal(card.bestStreakLabel, "2 semanas");
    const older = weekly([...logs, "2026-09-14", "2026-09-15", "2026-09-21", "2026-09-22"], 2);
    assert.equal(cardOf(older).streakLabel, "2 semanas");
    assert.equal(cardOf(older).bestStreakLabel, "3 semanas");
  });
});

describe("toMetricsView: trend", () => {
  it("has 12 points, from the oldest closed week to the last one, with date, rounded value and text", () => {
    // Week of 2026-10-05: 7 of 7 days. Week of 2026-09-28: 3 of 7 days (42.9 %). Earlier weeks: 0 %.
    const logs = [...run("2026-10-05", 7), ...run("2026-09-28", 3)];
    const view = toMetricsView([daily(logs, "2026-01-05")], TODAY, NONE);
    assert.equal(view.trend.length, 12);
    assert.deepEqual(view.trend[11], { monday: "2026-10-05", label: "5 de octubre de 2026", value: 100, text: "100 %" });
    assert.deepEqual(view.trend[10], { monday: "2026-09-28", label: "28 de septiembre de 2026", value: 43, text: "43 %" });
    assert.deepEqual(view.trend[0], { monday: "2026-07-20", label: "20 de julio de 2026", value: 0, text: "0 %" });
  });

  it("gives the table and the chart the same rounded values (CA-7.5)", () => {
    const logs = [...run("2026-09-28", 3), ...run("2026-10-05", 5)];
    const view = toMetricsView([daily(logs, "2026-01-05")], TODAY, NONE);
    for (const point of view.trend) {
      assert.equal(point.text, point.value === null ? "sin datos" : `${point.value} %`);
    }
    assert.deepEqual(view.trend.slice(10).map((point) => point.value), [43, 71]);
  });

  it("leaves the weeks before any habit existed as null with 'sin datos', and keeps them in the series (CA-7.4)", () => {
    const view = toMetricsView([daily(["2026-10-01"], "2026-10-01")], TODAY, NONE);
    assert.deepEqual(view.trend[9], { monday: "2026-09-21", label: "21 de septiembre de 2026", value: null, text: "sin datos" });
    assert.equal(view.trend[10].value, 25);
  });

  it("has a trend when at least one of the 12 weeks has data", () => {
    assert.equal(toMetricsView([daily([], "2026-10-01")], TODAY, NONE).hasTrend, true);
  });

  it("has no trend when the 12 weeks are without data (CA-7.7)", () => {
    const view = toMetricsView([daily([], TODAY)], TODAY, NONE);
    assert.equal(view.hasTrend, false);
    assert.ok(view.trend.every((point) => point.value === null));
    assert.equal(toMetricsView([], TODAY, NONE).hasTrend, false);
  });

  it("recalculates the overall percent and the trend without a deleted habit (CA-9.4)", () => {
    const kept = daily(run("2026-08-31", 46), "2026-08-31", 1);
    const deleted = daily([], "2026-08-31", 2);
    const before = toMetricsView([kept, deleted], TODAY, NONE);
    const after = toMetricsView([kept], TODAY, NONE);
    assert.equal(before.overall.sinceCreation, "50 %");
    assert.equal(after.overall.sinceCreation, "100 %");
    assert.equal(before.trend[11].value, 50);
    assert.equal(after.trend[11].value, 100);
    assert.deepEqual(after.habits.map((habit) => habit.id), [1]);
  });
});

describe("toMetricsView: history", () => {
  const old = (logDates, id = 1) => daily(logDates, "2026-01-05", id);

  it("builds the last 12 weeks for a habit that is not expanded, and offers the full history", () => {
    const { history } = cardOf(old([]));
    assert.equal(history.weeks.length, 12);
    assert.equal(history.expanded, false);
    assert.equal(history.hasMore, true);
  });

  it("builds the whole history only for the expanded habits (D-T14)", () => {
    const view = toMetricsView([old([], 1), old([], 2)], TODAY, new Set([2]));
    assert.equal(view.habits[0].history.weeks.length, 12);
    assert.equal(view.habits[0].history.expanded, false);
    assert.equal(view.habits[1].history.weeks.length, 41);
    assert.equal(view.habits[1].history.expanded, true);
    assert.equal(view.habits[1].history.weeks[0].monday, "2026-01-05");
  });

  it("does not offer the full history to a habit with less than 12 weeks", () => {
    const { history } = cardOf(daily([], "2026-10-01"));
    assert.equal(history.hasMore, false);
    assert.equal(history.weeks.length, 3);
  });

  it("shows exactly the logged days of the API as done in the full history (CA-8.7)", () => {
    const logs = ["2026-02-03", "2026-05-30", "2026-10-14", "2026-10-16"];
    const view = toMetricsView([old(logs)], TODAY, new Set([1]));
    const done = view.habits[0].history.weeks
      .flatMap((week) => week.days)
      .filter((day) => day.state === "done")
      .map((day) => day.date);
    assert.deepEqual(done, logs);
  });

  it("gives every cell a symbol and a text with its date and state (RNF-4)", () => {
    const { history } = cardOf(daily(["2026-10-01"], "2026-10-01"));
    const [first, , last] = history.weeks;
    assert.deepEqual(first.days[2], { date: "2026-09-30", state: "out", symbol: "", label: "30 de septiembre de 2026: fuera de rango" });
    assert.deepEqual(first.days[3], { date: "2026-10-01", state: "done", symbol: "✓", label: "1 de octubre de 2026: cumplido" });
    assert.deepEqual(first.days[4], { date: "2026-10-02", state: "missed", symbol: "✗", label: "2 de octubre de 2026: no cumplido" });
    assert.deepEqual(last.days[4], { date: "2026-10-16", state: "pending", symbol: "…", label: "16 de octubre de 2026: pendiente" });
  });

  it("labels a day without a log of a weekly habit as 'sin registro', not as a failure", () => {
    const { history } = cardOf(weekly([], 3, "2026-10-12"));
    assert.deepEqual(history.weeks[0].days[1], { date: "2026-10-13", state: "empty", symbol: "·", label: "13 de octubre de 2026: sin registro" });
  });

  it("has no weekly summary for a daily habit", () => {
    assert.ok(cardOf(old([])).history.weeks.every((week) => week.summary === null));
  });

  it("tells the grid whether to draw the weekly summary column: only for weekly habits", () => {
    assert.equal(cardOf(old([])).history.hasSummary, false);
    assert.equal(cardOf(weekly([], 3, "2026-09-28")).history.hasSummary, true);
  });

  it("writes the weekly summary with the logs over the goal and the state of the week (CA-8.5)", () => {
    const logs = ["2026-09-28", "2026-09-29", "2026-09-30", "2026-10-05", "2026-10-06", "2026-10-12"];
    const { history } = cardOf(weekly(logs, 3, "2026-09-28"));
    assert.deepEqual(history.weeks.map((week) => week.summary.label), [
      "3 de 3 · Cumplida",
      "2 de 3 · No cumplida",
      "1 de 3 · En curso",
    ]);
  });
});
