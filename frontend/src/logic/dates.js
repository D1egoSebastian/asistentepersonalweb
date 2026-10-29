// Dates are "yyyy-MM-dd" text, the same format the API sends.
// Arithmetic uses UTC, so every day is exactly 24 hours long (no daylight saving changes).
const DAY_MS = 86_400_000;

function toUtcMs(date) {
  const [year, month, day] = date.split("-").map(Number);
  return Date.UTC(year, month - 1, day);
}

function fromUtcMs(ms) {
  return new Date(ms).toISOString().slice(0, 10);
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
