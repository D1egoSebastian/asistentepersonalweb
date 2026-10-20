import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { habitStatus } from "./status.js";

// 2026-10-16 is a Friday. Its week starts on Monday 2026-10-12.
const TODAY = "2026-10-16";

const daily = (logDates, createdOn = "2026-10-01") => ({
  frequency: "Daily",
  weeklyGoal: null,
  createdOn,
  logDates,
});

const weekly = (logDates, weeklyGoal, createdOn = "2026-08-31") => ({
  frequency: "Weekly",
  weeklyGoal,
  createdOn,
  logDates,
});

describe("habitStatus for daily habits", () => {
  it("is green when today is marked", () => {
    assert.equal(habitStatus(daily(["2026-10-16"]), TODAY), "green");
  });

  it("is red when yesterday was not marked and the habit existed then", () => {
    assert.equal(habitStatus(daily(["2026-10-13"]), TODAY), "red");
  });

  it("is pending when yesterday was marked and today is not yet", () => {
    assert.equal(habitStatus(daily(["2026-10-15"]), TODAY), "pending");
  });

  it("is never red on the day it was created", () => {
    assert.equal(habitStatus(daily([], "2026-10-16"), TODAY), "pending");
  });

  it("is red when it was created yesterday and not marked", () => {
    assert.equal(habitStatus(daily([], "2026-10-15"), TODAY), "red");
  });

  it("green wins over red", () => {
    // Yesterday is not marked, but today is: the status is green
    assert.equal(habitStatus(daily(["2026-10-16"]), TODAY), "green");
  });
});

describe("habitStatus for weekly habits", () => {
  it("is green when the current week reached the goal", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14"];
    assert.equal(habitStatus(weekly(logs, 3), TODAY), "green");
  });

  it("is red while the current week shows 2 of 3 and last week missed the goal", () => {
    const logs = ["2026-10-05", "2026-10-12", "2026-10-13"];
    assert.equal(habitStatus(weekly(logs, 3), TODAY), "red");
  });

  it("is pending on Monday when last week (Sunday marked) reached the goal", () => {
    const logs = ["2026-10-18"];
    assert.equal(habitStatus(weekly(logs, 1), "2026-10-19"), "pending");
  });

  it("is red the week after a creation week that missed the goal", () => {
    const logs = ["2026-10-12", "2026-10-13"];
    assert.equal(habitStatus(weekly(logs, 3, "2026-10-12"), "2026-10-19"), "red");
  });

  it("is pending during the creation week before reaching the goal", () => {
    const logs = ["2026-10-12", "2026-10-13"];
    assert.equal(habitStatus(weekly(logs, 3, "2026-10-12"), TODAY), "pending");
  });

  it("green wins over red", () => {
    // Last week missed the goal, but the current week reached it
    const logs = ["2026-10-05", "2026-10-12", "2026-10-13", "2026-10-14"];
    assert.equal(habitStatus(weekly(logs, 3), TODAY), "green");
  });
});
