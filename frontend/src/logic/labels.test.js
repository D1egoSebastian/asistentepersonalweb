import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { goalLabel, unitLabel } from "./labels.js";

describe("goalLabel", () => {
  it("uses the singular for one time per week", () => {
    assert.equal(goalLabel(1), "1 vez por semana");
  });

  it("uses the plural for more than one time per week", () => {
    assert.equal(goalLabel(3), "3 veces por semana");
    assert.equal(goalLabel(7), "7 veces por semana");
  });
});

describe("unitLabel", () => {
  it("uses day and days for daily habits", () => {
    assert.equal(unitLabel(1, true), "día");
    assert.equal(unitLabel(5, true), "días");
  });

  it("uses week and weeks for weekly habits", () => {
    assert.equal(unitLabel(1, false), "semana");
    assert.equal(unitLabel(2, false), "semanas");
  });

  it("uses the plural for zero", () => {
    assert.equal(unitLabel(0, true), "días");
    assert.equal(unitLabel(0, false), "semanas");
  });
});
