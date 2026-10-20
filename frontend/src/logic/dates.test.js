import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { addDays, weekStart } from "./dates.js";

describe("addDays", () => {
  it("crosses a month boundary", () => {
    assert.equal(addDays("2026-01-31", 1), "2026-02-01");
  });

  it("crosses a year boundary", () => {
    assert.equal(addDays("2025-12-31", 1), "2026-01-01");
  });

  it("handles February 29 in a leap year", () => {
    assert.equal(addDays("2028-02-28", 1), "2028-02-29");
    assert.equal(addDays("2028-02-29", 1), "2028-03-01");
  });

  it("skips February 29 in a common year", () => {
    assert.equal(addDays("2027-02-28", 1), "2027-03-01");
  });

  it("goes backwards with a negative number of days", () => {
    assert.equal(addDays("2026-03-01", -1), "2026-02-28");
    assert.equal(addDays("2026-10-16", -7), "2026-10-09");
  });

  it("returns the same date for zero days", () => {
    assert.equal(addDays("2026-10-16", 0), "2026-10-16");
  });
});

describe("weekStart", () => {
  it("returns the same Monday for all seven days of the week", () => {
    const monday = "2026-10-12";
    for (let offset = 0; offset < 7; offset += 1) {
      assert.equal(weekStart(addDays(monday, offset)), monday);
    }
  });

  it("returns the same day when it is already a Monday", () => {
    assert.equal(weekStart("2026-10-12"), "2026-10-12");
  });

  it("handles a week that crosses the change of year", () => {
    // 2025-12-29 is a Monday; the week runs into January 2026
    for (let offset = 0; offset < 7; offset += 1) {
      assert.equal(weekStart(addDays("2025-12-29", offset)), "2025-12-29");
    }
  });
});
