import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { toHabitView } from "./habitView.js";

// 2026-10-16 is a Friday. Its week starts on Monday 2026-10-12.
const TODAY = "2026-10-16";

const daily = (logDates, createdOn = "2026-10-01") => ({
  id: 1,
  name: "Daily coding challenge",
  description: "Resolver un ejercicio",
  frequency: "Daily",
  weeklyGoal: null,
  createdOn,
  logDates,
});

const weekly = (logDates, weeklyGoal, createdOn = "2026-08-31") => ({
  id: 2,
  name: "Gimnasio",
  description: null,
  frequency: "Weekly",
  weeklyGoal,
  createdOn,
  logDates,
});

describe("toHabitView for daily habits", () => {
  it("shows the identity, frequency and status of the habit", () => {
    const view = toHabitView(daily(["2026-10-16"]), TODAY);

    assert.equal(view.id, 1);
    assert.equal(view.name, "Daily coding challenge");
    assert.equal(view.description, "Resolver un ejercicio");
    assert.equal(view.frequencyLabel, "Diario");
    assert.equal(view.status, "green");
    assert.equal(view.statusLabel, "Cumplido");
    assert.equal(view.progressLabel, null);
  });

  it("uses the singular for a streak of 1 day", () => {
    assert.equal(toHabitView(daily(["2026-10-16"]), TODAY).streakLabel, "Racha: 1 día");
  });

  it("uses the plural for a streak of 5 days", () => {
    const logs = ["2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15", "2026-10-16"];
    assert.equal(toHabitView(daily(logs), TODAY).streakLabel, "Racha: 5 días");
  });

  it("marks each button as done only when that day is logged", () => {
    const view = toHabitView(daily(["2026-10-16"]), TODAY);

    assert.deepEqual(view.days, [
      { date: "2026-10-16", label: "Hoy", done: true },
      { date: "2026-10-15", label: "Ayer", done: false },
    ]);
  });

  it("shows pending and red labels for the other statuses", () => {
    assert.equal(toHabitView(daily(["2026-10-15"]), TODAY).statusLabel, "Pendiente");
    assert.equal(toHabitView(daily(["2026-10-13"]), TODAY).statusLabel, "No cumplido");
  });
});

describe("toHabitView for weekly habits", () => {
  it("shows the goal in the frequency label", () => {
    assert.equal(toHabitView(weekly([], 3), TODAY).frequencyLabel, "3 veces por semana");
  });

  it("uses the singular for a goal of 1", () => {
    assert.equal(toHabitView(weekly([], 1), TODAY).frequencyLabel, "1 vez por semana");
  });

  it("shows the current week's progress against the goal", () => {
    const view = toHabitView(weekly(["2026-10-12", "2026-10-13"], 3), TODAY);
    assert.equal(view.progressLabel, "Esta semana: 2 de 3");
  });

  it("shows the streak in weeks", () => {
    const logs = ["2026-10-05", "2026-10-06", "2026-10-12", "2026-10-13"];
    const view = toHabitView(weekly(logs, 2), TODAY);

    assert.equal(view.streakLabel, "Racha: 2 semanas");
    assert.equal(view.progressLabel, "Esta semana: 2 de 2");
  });

  it("uses the singular for a streak of 1 week", () => {
    const logs = ["2026-10-12"];
    assert.equal(toHabitView(weekly(logs, 1), TODAY).streakLabel, "Racha: 1 semana");
  });

  it("marks the buttons of today and yesterday for a weekly habit too", () => {
    const view = toHabitView(weekly(["2026-10-15"], 3), TODAY);

    assert.deepEqual(view.days, [
      { date: "2026-10-16", label: "Hoy", done: false },
      { date: "2026-10-15", label: "Ayer", done: true },
    ]);
  });
});
