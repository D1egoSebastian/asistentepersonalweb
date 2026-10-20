import { addDays } from "./dates.js";

// The days the user may mark: today always, and yesterday only if the habit already existed then.
export function loggableDays(createdOn, today) {
  const days = [{ date: today, label: "Hoy" }];

  const yesterday = addDays(today, -1);
  if (yesterday >= createdOn) {
    days.push({ date: yesterday, label: "Ayer" });
  }

  return days;
}
