// Dates are "yyyy-MM-dd" text, the same format the API sends.
// Arithmetic uses UTC, so every day is exactly 24 hours long (no daylight saving changes).
const DAY_MS = 86_400_000;

// Metrics convert the same few thousand dates about 100,000 times (spec 004, RNF-3), so each
// conversion is computed once and remembered. The caches only grow with the distinct dates seen.
const msByDate = new Map();
const dateByMs = new Map();

function toUtcMs(date) {
  let ms = msByDate.get(date);
  if (ms === undefined) {
    const [year, month, day] = date.split("-").map(Number);
    ms = Date.UTC(year, month - 1, day);
    msByDate.set(date, ms);
  }
  return ms;
}

function fromUtcMs(ms) {
  let date = dateByMs.get(ms);
  if (date === undefined) {
    date = new Date(ms).toISOString().slice(0, 10);
    dateByMs.set(ms, date);
  }
  return date;
}

export function addDays(date, days) {
  return fromUtcMs(toUtcMs(date) + days * DAY_MS);
}

// Monday of the week that contains the date. Weeks run Monday to Sunday.
export function weekStart(date) {
  const weekday = new Date(toUtcMs(date)).getUTCDay(); // 0 = Sunday
  const daysSinceMonday = (weekday + 6) % 7;
  return addDays(date, -daysSinceMonday);
}

// Days from `from` to `to`. Negative when `to` is earlier.
export function daysBetween(from, to) {
  return (toUtcMs(to) - toUtcMs(from)) / DAY_MS;
}

// One formatter for the whole app: creating it once is cheaper than one per call.
const DATE_FORMAT = new Intl.DateTimeFormat("es", {
  day: "numeric",
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

// "2026-10-27" becomes "27 de octubre de 2026". The date is read as a calendar day.
export function formatDate(date) {
  return DATE_FORMAT.format(toUtcMs(date));
}

const SHORT_DATE_FORMAT = new Intl.DateTimeFormat("es", { day: "numeric", month: "short", timeZone: "UTC" });

// "2026-08-03" becomes "3 ago". Used where the full date does not fit, like the chart axis.
export function formatShortDate(date) {
  return SHORT_DATE_FORMAT.format(toUtcMs(date));
}
