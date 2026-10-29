import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { countByWeek } from "./streak.js";
import { addDays } from "./dates.js";
import { dayState, weekSummary, historyWeeks, hasMoreHistory } from "./history.js";

// 2026-10-16 is a Friday. Its week runs from Monday 2026-10-12 to Sunday 2026-10-18.
const TODAY = "2026-10-16";

const daily = (logDates, createdOn = "2026-10-05") => ({
  frequency: "Daily",
  weeklyGoal: null,
  createdOn,
  logDates,
});

const weekly = (logDates, weeklyGoal, createdOn = "2026-10-05") => ({
  frequency: "Weekly",
  weeklyGoal,
  createdOn,
  logDates,
});

const stateOf = (date, habit) => dayState(date, habit, new Set(habit.logDates), TODAY);

describe("dayState for daily habits", () => {
  const habit = daily(["2026-10-06", "2026-10-16"]);

  it("is done for a day with a log, today included", () => {
    assert.equal(stateOf("2026-10-06", habit), "done");
    assert.equal(stateOf("2026-10-16", habit), "done");
  });

  it("is missed for a closed day without a log", () => {
    assert.equal(stateOf("2026-10-07", habit), "missed");
    assert.equal(stateOf("2026-10-15", habit), "missed");
  });

  it("is missed on the creation day when it has no log", () => {
    assert.equal(stateOf("2026-10-05", habit), "missed");
  });

  it("is pending for today without a log", () => {
    assert.equal(stateOf("2026-10-16", daily([])), "pending");
  });

  it("is out before the creation date and after today", () => {
    assert.equal(stateOf("2026-10-04", habit), "out");
    assert.equal(stateOf("2026-10-17", habit), "out");
    assert.equal(stateOf("2026-10-18", habit), "out");
  });
});

describe("dayState for weekly habits", () => {
  const habit = weekly(["2026-10-06", "2026-10-16"], 3);

  it("is done for a day with a log", () => {
    assert.equal(stateOf("2026-10-06", habit), "done");
    assert.equal(stateOf("2026-10-16", habit), "done");
  });

  it("is empty, not missed, for a day without a log (CA-8.4)", () => {
    assert.equal(stateOf("2026-10-07", habit), "empty");
    assert.equal(stateOf("2026-10-14", habit), "empty");
  });

  it("is pending for today without a log", () => {
    assert.equal(stateOf("2026-10-16", weekly([], 3)), "pending");
  });

  it("is out before the creation date and after today", () => {
    assert.equal(stateOf("2026-10-04", habit), "out");
    assert.equal(stateOf("2026-10-17", habit), "out");
  });

  it("is out for the days of the creation week before the creation date", () => {
    const created = weekly([], 2, "2026-10-08");
    assert.equal(stateOf("2026-10-07", created), "out");
    assert.equal(stateOf("2026-10-08", created), "empty");
  });
});

describe("weekSummary", () => {
  const summaryOf = (logs, goal, monday) => weekSummary(monday, countByWeek(logs), goal, TODAY);

  it("shows the logs over the goal and a met state when the closed week reached it", () => {
    const logs = ["2026-10-05", "2026-10-06", "2026-10-07"];
    assert.deepEqual(summaryOf(logs, 3, "2026-10-05"), { count: 3, goal: 3, state: "met" });
  });

  it("is missed when a closed week did not reach the goal", () => {
    assert.deepEqual(summaryOf(["2026-10-05", "2026-10-06"], 3, "2026-10-05"), { count: 2, goal: 3, state: "missed" });
  });

  it("is missed for a closed week with no logs", () => {
    assert.deepEqual(summaryOf([], 2, "2026-09-28"), { count: 0, goal: 2, state: "missed" });
  });

  it("is current when the current week has not reached the goal yet", () => {
    assert.deepEqual(summaryOf(["2026-10-12"], 3, "2026-10-12"), { count: 1, goal: 3, state: "current" });
  });

  it("is met when the current week already reached the goal (D-T15)", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14"];
    assert.deepEqual(summaryOf(logs, 3, "2026-10-12"), { count: 3, goal: 3, state: "met" });
  });

  it("counts every log of a week with more logs than the goal (4 of 3)", () => {
    const logs = ["2026-10-05", "2026-10-06", "2026-10-07", "2026-10-08"];
    assert.deepEqual(summaryOf(logs, 3, "2026-10-05"), { count: 4, goal: 3, state: "met" });
  });
});

const gridOf = (habit, showAll = false) =>
  historyWeeks(habit, new Set(habit.logDates), countByWeek(habit.logDates), TODAY, showAll);

describe("historyWeeks", () => {
  it("shows the last 12 weeks, counting the current one", () => {
    const weeks = gridOf(daily([], "2025-01-06"));
    assert.equal(weeks.length, 12);
    assert.equal(weeks[0].monday, "2026-07-27");
    assert.equal(weeks[11].monday, "2026-10-12");
  });

  it("orders the weeks from the oldest to the newest, 7 days from Monday to Sunday each", () => {
    const weeks = gridOf(daily([], "2025-01-06"));
    for (const week of weeks) {
      assert.deepEqual(week.days.map((day) => day.date), Array.from({ length: 7 }, (_, i) => addDays(week.monday, i)));
    }
    assert.ok(weeks.every((week, i) => i === 0 || week.monday > weeks[i - 1].monday));
  });

  it("starts at the creation week when the habit has less than 12 weeks", () => {
    const weeks = gridOf(daily([], "2026-10-01"));
    assert.deepEqual(weeks.map((week) => week.monday), ["2026-09-28", "2026-10-05", "2026-10-12"]);
  });

  it("marks the days before the creation date and after today as out", () => {
    const weeks = gridOf(daily(["2026-10-01"], "2026-10-01"));
    assert.deepEqual(
      weeks[0].days.map((day) => day.state),
      ["out", "out", "out", "done", "missed", "missed", "missed"],
    );
    assert.deepEqual(
      weeks[2].days.map((day) => day.state),
      ["missed", "missed", "missed", "missed", "pending", "out", "out"],
    );
  });

  it("starts at the creation week with showAll", () => {
    const habit = daily([], "2026-06-10");
    assert.equal(gridOf(habit)[0].monday, "2026-07-27");
    const all = gridOf(habit, true);
    assert.equal(all[0].monday, "2026-06-08");
    assert.equal(all.length, 19);
    assert.equal(all[all.length - 1].monday, "2026-10-12");
  });

  it("shows as done exactly the days with a log in the whole history (CA-8.7)", () => {
    const logs = ["2025-12-31", "2026-01-01", "2026-03-15", "2026-10-15", "2026-10-16"];
    const weeks = gridOf(daily(logs, "2025-12-01"), true);
    const doneDates = weeks.flatMap((week) => week.days.filter((day) => day.state === "done")).map((day) => day.date);
    assert.deepEqual(doneDates, logs);
  });

  it("has no weekly summary for a daily habit", () => {
    assert.ok(gridOf(daily([], "2026-10-01")).every((week) => week.summary === null));
  });

  it("adds the weekly summary to each week of a weekly habit", () => {
    const habit = weekly(["2026-09-28", "2026-09-29", "2026-10-12"], 2, "2026-09-28");
    const weeks = gridOf(habit);
    assert.deepEqual(weeks.map((week) => week.summary), [
      { count: 2, goal: 2, state: "met" },
      { count: 0, goal: 2, state: "missed" },
      { count: 1, goal: 2, state: "current" },
    ]);
  });
});

describe("hasMoreHistory", () => {
  it("is false when the habit has less than 12 weeks", () => {
    assert.equal(hasMoreHistory(daily([], "2026-10-01"), TODAY), false);
  });

  it("is false with exactly 12 weeks, counting the current one", () => {
    assert.equal(hasMoreHistory(daily([], "2026-07-27"), TODAY), false);
    assert.equal(hasMoreHistory(daily([], "2026-07-31"), TODAY), false);
  });

  it("is true with 13 weeks or more", () => {
    assert.equal(hasMoreHistory(daily([], "2026-07-26"), TODAY), true);
    assert.equal(hasMoreHistory(weekly([], 2, "2025-01-06"), TODAY), true);
  });
});
