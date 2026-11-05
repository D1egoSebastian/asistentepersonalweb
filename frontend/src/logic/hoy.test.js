import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { habitDoneToday, todayProgress, staleIdeas, latestIdeas, relativeDay, todayHeading } from "./hoy.js";

// 2026-11-03 is a Tuesday.
const TODAY = "2026-11-03";

const habit = (id, logDates) => ({ id, name: `Hábito ${id}`, frequency: "Daily", logDates });

const idea = (id, createdDate, updatedDate = createdDate) => ({
  id,
  summary: `Idea ${id}`,
  tags: [],
  createdDate,
  updatedDate,
});

describe("habitDoneToday", () => {
  it("is true only when today is logged", () => {
    assert.equal(habitDoneToday(habit(1, ["2026-11-02", TODAY]), TODAY), true);
    assert.equal(habitDoneToday(habit(1, ["2026-11-02"]), TODAY), false);
  });
});

describe("todayProgress", () => {
  it("counts the habits logged today over the total", () => {
    const habits = [habit(1, [TODAY]), habit(2, []), habit(3, [TODAY])];

    assert.deepEqual(todayProgress(habits, TODAY), { done: 2, total: 3 });
  });

  it("returns zero of zero when there are no habits", () => {
    assert.deepEqual(todayProgress([], TODAY), { done: 0, total: 0 });
  });
});

describe("staleIdeas", () => {
  it("keeps the ideas last edited 14 or more days ago", () => {
    const ideas = [
      idea(1, "2026-09-01", "2026-10-20"), // 14 days before today
      idea(2, "2026-09-01", "2026-10-21"), // 13 days before today
      idea(3, "2026-08-01", "2026-10-01"), // 33 days before today
    ];

    assert.deepEqual(staleIdeas(ideas, TODAY).map((item) => item.id), [1, 3]);
  });

  it("returns an empty list when nothing is stale", () => {
    assert.deepEqual(staleIdeas([idea(1, TODAY)], TODAY), []);
  });
});

describe("latestIdeas", () => {
  it("returns the five newest ideas by creation date, newest first", () => {
    const ideas = [
      idea(1, "2026-10-01"),
      idea(2, "2026-10-05"),
      idea(3, "2026-10-03"),
      idea(4, "2026-10-07"),
      idea(5, "2026-10-02"),
      idea(6, "2026-10-06"),
      idea(7, "2026-10-04"),
    ];

    assert.deepEqual(latestIdeas(ideas).map((item) => item.id), [4, 6, 2, 7, 3]);
  });

  it("breaks ties on the same day by the higher id", () => {
    const ideas = [idea(1, TODAY), idea(9, TODAY)];

    assert.deepEqual(latestIdeas(ideas).map((item) => item.id), [9, 1]);
  });

  it("does not modify the list it receives", () => {
    const ideas = [idea(1, "2026-10-01"), idea(2, "2026-10-09")];

    latestIdeas(ideas);

    assert.deepEqual(ideas.map((item) => item.id), [1, 2]);
  });
});

describe("relativeDay", () => {
  it("names today, yesterday and older days", () => {
    assert.equal(relativeDay(TODAY, TODAY), "hoy");
    assert.equal(relativeDay("2026-11-02", TODAY), "ayer");
    assert.equal(relativeDay("2026-10-31", TODAY), "hace 3 días");
  });
});

describe("todayHeading", () => {
  it("gives the weekday and the day in Spanish", () => {
    assert.deepEqual(todayHeading(TODAY), { weekday: "Martes", dayOfMonth: "3 de noviembre" });
  });

  it("uses the real weekday of the date", () => {
    assert.equal(todayHeading("2026-11-08").weekday, "Domingo");
  });
});
