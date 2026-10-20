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
