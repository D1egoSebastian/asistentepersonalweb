import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { addDays } from "./dates.js";
import { countByWeek, dailyStreak, weeklyStreak } from "./streak.js";
import { habitStatus } from "./status.js";
import { firstPeriod, completionIn, habitWindows, percentOf, overallPercent, roundPercent } from "./completion.js";

// 2026-10-16 is a Friday. Its week runs from Monday 2026-10-12 to Sunday 2026-10-18.
const TODAY = "2026-10-16";

const daily = (logDates, createdOn = "2026-08-31") => ({
  id: 1,
  frequency: "Daily",
  weeklyGoal: null,
  createdOn,
  logDates,
});

describe("firstPeriod", () => {
  it("is the creation date for a daily habit", () => {
    assert.equal(firstPeriod(daily([], "2026-10-08")), "2026-10-08");
  });

  it("is the Monday of the creation week for a weekly habit", () => {
    const habit = { frequency: "Weekly", weeklyGoal: 3, createdOn: "2026-10-08", logDates: [] };
    assert.equal(firstPeriod(habit), "2026-10-05");
    assert.equal(firstPeriod({ ...habit, createdOn: "2026-10-05" }), "2026-10-05");
    assert.equal(firstPeriod({ ...habit, createdOn: "2026-10-11" }), "2026-10-05");
  });
});

describe("completionIn for daily habits", () => {
  it("counts the marked days over every day of the range", () => {
    const habit = daily(["2026-10-01", "2026-10-02", "2026-10-04", "2026-09-20"]);
    assert.deepEqual(completionIn(habit, null, "2026-10-01", "2026-10-07"), { met: 3, total: 7 });
  });

  it("includes both ends of the range", () => {
    const habit = daily(["2026-10-01", "2026-10-07"]);
    assert.deepEqual(completionIn(habit, null, "2026-10-01", "2026-10-07"), { met: 2, total: 7 });
  });

  it("starts at the creation date when the range starts before it", () => {
    // CA-2.4: created on Thursday 2026-10-01, third week of the window. Thursday to Sunday is 4 days, plus 7.
    const habit = daily(["2026-10-01", "2026-10-05"], "2026-10-01");
    assert.deepEqual(completionIn(habit, null, "2026-09-14", "2026-10-11"), { met: 2, total: 11 });
  });

  it("has no evaluable days when the range ends before the creation date", () => {
    const habit = daily(["2026-10-01"], "2026-10-01");
    assert.deepEqual(completionIn(habit, null, "2026-09-14", "2026-09-30"), { met: 0, total: 0 });
  });

  it("counts a range that is a single day", () => {
    const habit = daily(["2026-10-05"]);
    assert.deepEqual(completionIn(habit, null, "2026-10-05", "2026-10-05"), { met: 1, total: 1 });
    assert.deepEqual(completionIn(habit, null, addDays("2026-10-05", 1), "2026-10-06"), { met: 0, total: 1 });
  });

  it("counts a full range with logs, with no gaps", () => {
    const logs = Array.from({ length: 30 }, (_, i) => addDays("2026-09-01", i));
    assert.deepEqual(completionIn(daily(logs), null, "2026-09-01", "2026-09-30"), { met: 30, total: 30 });
  });
});

const weekly = (logDates, weeklyGoal, createdOn = "2026-08-31") => ({
  id: 2,
  frequency: "Weekly",
  weeklyGoal,
  createdOn,
  logDates,
});

// The weekly functions receive the weekly counts, indexed once per habit.
const weeklyCompletion = (habit, from, to) => completionIn(habit, countByWeek(habit.logDates), from, to);

describe("completionIn for weekly habits", () => {
  it("counts the met weeks over the weeks of the range", () => {
    const logs = ["2026-09-14", "2026-09-15", "2026-09-21", "2026-10-05", "2026-10-06"];
    assert.deepEqual(weeklyCompletion(weekly(logs, 2), "2026-09-14", "2026-10-05"), { met: 2, total: 4 });
  });

  it("starts at the creation week with the full goal when the range starts before it", () => {
    // CA-2.4: created on Thursday 2026-10-01, in the week of 2026-09-28. That week and the next one: 2 weeks.
    const logs = ["2026-10-01", "2026-10-02", "2026-10-05"];
    assert.deepEqual(
      weeklyCompletion(weekly(logs, 2, "2026-10-01"), "2026-09-14", "2026-10-05"),
      { met: 1, total: 2 },
    );
  });

  it("fails the creation week when the goal is above 1 and the habit existed only a day", () => {
    // Created on Sunday 2026-10-11: that week has one day, but the goal is still 3.
    const habit = weekly(["2026-10-11"], 3, "2026-10-11");
    assert.deepEqual(weeklyCompletion(habit, "2026-10-05", "2026-10-05"), { met: 0, total: 1 });
  });

  it("meets the creation week with one log when the goal is 1", () => {
    const habit = weekly(["2026-10-11"], 1, "2026-10-11");
    assert.deepEqual(weeklyCompletion(habit, "2026-10-05", "2026-10-05"), { met: 1, total: 1 });
  });

  it("counts a week with more logs than the goal as one met week (4 of 3)", () => {
    const logs = ["2026-10-05", "2026-10-06", "2026-10-07", "2026-10-08"];
    assert.deepEqual(weeklyCompletion(weekly(logs, 3), "2026-10-05", "2026-10-05"), { met: 1, total: 1 });
  });

  it("does not count met weeks outside the range", () => {
    const logs = ["2026-09-07", "2026-09-08", "2026-10-12", "2026-10-13"];
    assert.deepEqual(weeklyCompletion(weekly(logs, 2), "2026-09-14", "2026-10-05"), { met: 0, total: 4 });
  });

  it("has no evaluable weeks when the range ends before the creation week", () => {
    const habit = weekly([], 2, "2026-10-08");
    assert.deepEqual(weeklyCompletion(habit, "2026-09-14", "2026-09-28"), { met: 0, total: 0 });
  });
});

const windowsOf = (habit, today) => habitWindows(habit, countByWeek(habit.logDates), today);

describe("habitWindows for daily habits", () => {
  it("ends 'since creation' yesterday and covers the 28 days before this week in 'last four weeks'", () => {
    // Created 2026-08-31. Closed days: 2026-08-31 to 2026-10-15 are 46. Four weeks: 2026-09-14 to 2026-10-11.
    const logs = ["2026-08-31", "2026-09-20", "2026-10-11", "2026-10-13"];
    assert.deepEqual(windowsOf(daily(logs), TODAY), {
      sinceCreation: { met: 4, total: 46 },
      lastFourWeeks: { met: 2, total: 28 },
    });
  });

  it("keeps the closed days of the current week out of 'last four weeks' (CA-2.3)", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14"];
    const windows = windowsOf(daily(logs), TODAY);
    assert.equal(windows.sinceCreation.met, 3);
    assert.equal(windows.lastFourWeeks.met, 0);
  });

  it("has no data in either window for a habit created today (CA-2.6)", () => {
    assert.deepEqual(windowsOf(daily([], TODAY), TODAY), {
      sinceCreation: { met: 0, total: 0 },
      lastFourWeeks: { met: 0, total: 0 },
    });
  });

  it("has data since creation but not in the last four weeks when created on Monday and today is Wednesday", () => {
    const habit = daily(["2026-10-12"], "2026-10-12");
    assert.deepEqual(windowsOf(habit, "2026-10-14"), {
      sinceCreation: { met: 1, total: 2 },
      lastFourWeeks: { met: 0, total: 0 },
    });
  });

  it("on a Monday, counts yesterday (Sunday) since creation and the four weeks that ended yesterday", () => {
    const habit = daily(["2026-10-18", "2026-10-11", "2026-09-20"]);
    const windows = windowsOf(habit, "2026-10-19");
    assert.deepEqual(windows.sinceCreation, { met: 3, total: 49 });
    assert.deepEqual(windows.lastFourWeeks, { met: 2, total: 28 });
  });

  it("does not change when today gets marked (CA-5.3)", () => {
    const before = windowsOf(daily(["2026-10-15"]), TODAY);
    const after = windowsOf(daily(["2026-10-15", TODAY]), TODAY);
    assert.deepEqual(after, before);
  });
});

describe("habitWindows for weekly habits", () => {
  it("ends both windows at the previous week", () => {
    // Created 2026-08-31: weeks from 08-31 to 10-05 are 6. The four weeks start on 09-14.
    const logs = ["2026-08-31", "2026-09-01", "2026-09-14", "2026-09-15", "2026-10-05", "2026-10-06"];
    assert.deepEqual(windowsOf(weekly(logs, 2), TODAY), {
      sinceCreation: { met: 3, total: 6 },
      lastFourWeeks: { met: 2, total: 4 },
    });
  });

  it("has no data in either window for a habit created this week (CA-2.6)", () => {
    assert.deepEqual(windowsOf(weekly([], 2, "2026-10-12"), TODAY), {
      sinceCreation: { met: 0, total: 0 },
      lastFourWeeks: { met: 0, total: 0 },
    });
  });

  it("on a Monday, the four weeks are the ones that ended yesterday", () => {
    const logs = ["2026-09-21", "2026-09-22", "2026-10-12", "2026-10-13"];
    assert.deepEqual(windowsOf(weekly(logs, 2), "2026-10-19"), {
      sinceCreation: { met: 2, total: 7 },
      lastFourWeeks: { met: 2, total: 4 },
    });
  });

  it("does not change when the current week reaches the goal (CA-5.3)", () => {
    const before = windowsOf(weekly(["2026-10-05", "2026-10-06"], 2), TODAY);
    const after = windowsOf(weekly(["2026-10-05", "2026-10-06", "2026-10-12", "2026-10-13"], 2), TODAY);
    assert.deepEqual(after, before);
  });
});

describe("percentOf", () => {
  it("is null when there are no evaluable periods, never 0 (CA-2.5)", () => {
    assert.equal(percentOf({ met: 0, total: 0 }), null);
  });

  it("is 0 when there are periods and none was met", () => {
    assert.equal(percentOf({ met: 0, total: 5 }), 0);
  });

  it("is 100 when every period was met", () => {
    assert.equal(percentOf({ met: 8, total: 8 }), 100);
  });

  it("keeps the exact value, without rounding (133 of 200 is 66.5)", () => {
    assert.equal(percentOf({ met: 133, total: 200 }), 66.5);
    assert.equal(percentOf({ met: 1, total: 3 }), 100 / 3);
  });
});

describe("overallPercent", () => {
  it("averages the percents with the same weight for each habit", () => {
    assert.equal(overallPercent([100, 0]), 50);
    assert.equal(overallPercent([90, 60, 30]), 60);
  });

  it("leaves out the habits without data (null)", () => {
    assert.equal(overallPercent([100, null, 50]), 75);
  });

  it("is null when every habit has no data, and when there are no habits", () => {
    assert.equal(overallPercent([null, null]), null);
    assert.equal(overallPercent([]), null);
  });

  it("is 0, not null, when the habits with data have 0 %", () => {
    assert.equal(overallPercent([0, null]), 0);
  });

  it("averages the exact percents, without rounding them first (CA-4.5)", () => {
    // Rounding first would give (1 + 1 + 0) / 3 = 0.67 and show 1 %.
    assert.equal(overallPercent([0.5, 0.5, 0]), 1 / 3);
    assert.equal(Math.round(overallPercent([0.5, 0.5, 0])), 0);
  });
});

describe("consistency with the status and the streak (RF-9)", () => {
  it("counts yesterday as missed when a daily habit is red (CA-9.1)", () => {
    const habit = daily(["2026-10-10", "2026-10-11", "2026-10-12", "2026-10-13", "2026-10-14"], "2026-10-10");
    assert.equal(habitStatus(habit, TODAY), "red");
    const yesterday = completionIn(habit, null, "2026-10-15", "2026-10-15");
    assert.deepEqual(yesterday, { met: 0, total: 1 });
    // Six closed days (10-10 to 10-15), five of them met: the missed one is in the total.
    assert.deepEqual(windowsOf(habit, TODAY).sinceCreation, { met: 5, total: 6 });
  });

  it("counts the previous week as missed when a weekly habit is red (CA-9.1)", () => {
    const habit = weekly(["2026-09-28", "2026-09-29", "2026-10-06"], 2, "2026-09-28");
    assert.equal(habitStatus(habit, TODAY), "red");
    assert.deepEqual(weeklyCompletion(habit, "2026-10-05", "2026-10-05"), { met: 0, total: 1 });
    // Two closed weeks (09-28 and 10-05), only the first one met.
    assert.deepEqual(windowsOf(habit, TODAY).sinceCreation, { met: 1, total: 2 });
  });

  it("counts the creation week as missed in a red weekly habit created on a Sunday (CA-9.1)", () => {
    const habit = weekly(["2026-10-11"], 2, "2026-10-11");
    assert.equal(habitStatus(habit, TODAY), "red");
    assert.deepEqual(windowsOf(habit, TODAY).sinceCreation, { met: 0, total: 1 });
  });

  it("has at least as many met days since creation as the closed days of a daily streak (CA-9.2)", () => {
    const histories = [
      ["2026-10-15", "2026-10-14", "2026-10-13"],
      ["2026-10-16", "2026-10-15", "2026-10-14", "2026-10-13"],
      ["2026-10-16"],
      ["2026-10-14", "2026-10-13", "2026-09-01"],
    ];
    for (const logs of histories) {
      const habit = daily(logs);
      const closedDays = dailyStreak(logs, TODAY) - (logs.includes(TODAY) ? 1 : 0);
      assert.ok(closedDays >= 0);
      assert.ok(windowsOf(habit, TODAY).sinceCreation.met >= closedDays, `history ${logs.join(",")}`);
    }
  });

  it("has at least as many met weeks since creation as the closed weeks of a weekly streak (CA-9.2)", () => {
    const histories = [
      { logs: ["2026-10-05", "2026-10-06", "2026-09-28", "2026-09-29", "2026-09-21", "2026-09-22"], goal: 2 },
      { logs: ["2026-10-12", "2026-10-13", "2026-10-05", "2026-10-06"], goal: 2 },
      { logs: ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15"], goal: 3 },
      { logs: ["2026-10-06", "2026-09-29"], goal: 1 },
    ];
    for (const { logs, goal } of histories) {
      const habit = weekly(logs, goal);
      const streak = weeklyStreak(logs, goal, habit.createdOn, TODAY);
      const currentMet = countByWeek(logs).get("2026-10-12") >= goal;
      const closedWeeks = streak - (currentMet ? 1 : 0);
      assert.ok(closedWeeks >= 0);
      assert.ok(windowsOf(habit, TODAY).sinceCreation.met >= closedWeeks, `history ${logs.join(",")}`);
    }
  });
});

describe("roundPercent", () => {
  it("rounds half up", () => {
    assert.equal(roundPercent(66.5), 67);
    assert.equal(roundPercent(37.4), 37);
    assert.equal(roundPercent(0), 0);
  });

  it("rounds an exact x.5 average half up despite floating-point noise", () => {
    const percents = [
      percentOf({ met: 86, total: 336 }),
      percentOf({ met: 11, total: 14 }),
      percentOf({ met: 1, total: 12 }),
    ];
    assert.equal(roundPercent(overallPercent(percents)), 38);
  });
});
