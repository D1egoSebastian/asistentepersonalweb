import { addDays, weekStart } from "./dates.js";
import { countByWeek, isWeekMet } from "./streak.js";

// Returns "green" (done), "red" (missed) or "pending" (still time).
// Green wins over red, and red wins over pending.
export function habitStatus(habit, today) {
  return habit.frequency === "Daily" ? dailyStatus(habit, today) : weeklyStatus(habit, today);
}

function dailyStatus({ logDates, createdOn }, today) {
  const days = new Set(logDates);
  if (days.has(today)) {
    return "green";
  }

  // Red only when yesterday ended without a mark, and the habit existed yesterday.
  const yesterday = addDays(today, -1);
  if (yesterday >= createdOn && !days.has(yesterday)) {
    return "red";
  }

  return "pending";
}

function weeklyStatus({ logDates, weeklyGoal, createdOn }, today) {
  const counts = countByWeek(logDates);
  const current = weekStart(today);
  const previous = addDays(current, -7);

  if (isWeekMet(counts, current, weeklyGoal)) {
    return "green";
  }

  // The creation week counts with the full goal (spec D-2).
  if (previous >= weekStart(createdOn) && !isWeekMet(counts, previous, weeklyGoal)) {
    return "red";
  }

  return "pending";
}
