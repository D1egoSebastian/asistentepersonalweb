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

import { daysBetween } from "./dates.js";

describe("daysBetween", () => {
  it("counts the days inside one month", () => {
    assert.equal(daysBetween("2026-10-12", "2026-10-16"), 4);
  });

  it("crosses a month boundary", () => {
    assert.equal(daysBetween("2026-01-30", "2026-02-02"), 3);
  });

  it("crosses a year boundary", () => {
    assert.equal(daysBetween("2025-12-30", "2026-01-02"), 3);
  });

  it("counts February 29 in a leap year but not in a common year", () => {
    assert.equal(daysBetween("2028-02-28", "2028-03-01"), 2);
    assert.equal(daysBetween("2027-02-28", "2027-03-01"), 1);
  });

  it("is 0 for the same date", () => {
    assert.equal(daysBetween("2026-10-16", "2026-10-16"), 0);
  });

  it("is negative when the second date is earlier", () => {
    assert.equal(daysBetween("2026-10-16", "2026-10-12"), -4);
  });

  it("counts a whole year", () => {
    assert.equal(daysBetween("2026-01-01", "2027-01-01"), 365);
  });
});

import { formatDate } from "./dates.js";

describe("formatDate", () => {
  it("writes the day, the month name and the year in Spanish", () => {
    assert.equal(formatDate("2026-10-27"), "27 de octubre de 2026");
  });

  it("does not pad the day and keeps the calendar day (no UTC shift)", () => {
    assert.equal(formatDate("2026-01-01"), "1 de enero de 2026");
    assert.equal(formatDate("2028-02-29"), "29 de febrero de 2028");
  });
});

import { formatShortDate } from "./dates.js";

describe("formatShortDate", () => {
  it("shows day and short month for the chart axis", () => {
    assert.equal(formatShortDate("2026-08-03"), "3 ago");
    assert.equal(formatShortDate("2026-09-28"), "28 sept");
  });

  it("reads the date as a calendar day, not shifted by time zone", () => {
    assert.equal(formatShortDate("2026-01-01"), "1 ene");
  });
});
