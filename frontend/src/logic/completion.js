import { addDays, daysBetween, weekStart } from "./dates.js";
import { isWeekMet } from "./streak.js";

// The first period the habit existed in: its creation day (daily) or the Monday of its creation week (weekly).
export function firstPeriod(habit) {
  return habit.frequency === "Daily" ? habit.createdOn : weekStart(habit.createdOn);
}

// How many periods from `from` to `to` (both included) were met, out of how many existed.
// The range is cut at the first period of the habit, so nothing before it counts.
// `counts` (logs per week) is only used by weekly habits, whose ranges go from Monday to Monday.
export function completionIn(habit, counts, from, to) {
  const start = from > firstPeriod(habit) ? from : firstPeriod(habit);
  if (start > to) {
    return { met: 0, total: 0 };
  }
  return habit.frequency === "Daily"
    ? dailyCompletion(habit.logDates, start, to)
    : weeklyCompletion(counts, habit.weeklyGoal, start, to);
}

function dailyCompletion(logDates, start, to) {
  const met = logDates.filter((date) => date >= start && date <= to).length;
  return { met, total: daysBetween(start, to) + 1 };
}

// A week is met with at least `goal` logs; more logs than the goal still count as one week.
function weeklyCompletion(counts, goal, start, to) {
  const metWeeks = [...counts.keys()].filter(
    (week) => week >= start && week <= to && isWeekMet(counts, week, goal),
  );
  return { met: metWeeks.length, total: daysBetween(start, to) / 7 + 1 };
}

const DAYS_IN_FOUR_WEEKS = 28;

// The two windows of a habit. Today and the current week are never inside them (CA-2.1, CA-5.3).
export function habitWindows(habit, counts, today) {
  const currentWeek = weekStart(today);
  const lastClosedWeek = addDays(currentWeek, -7);
  const isDaily = habit.frequency === "Daily";
  const sinceCreationEnd = isDaily ? addDays(today, -1) : lastClosedWeek;
  const lastFourWeeksEnd = isDaily ? addDays(currentWeek, -1) : lastClosedWeek;

  return {
    sinceCreation: completionIn(habit, counts, firstPeriod(habit), sinceCreationEnd),
    lastFourWeeks: completionIn(habit, counts, addDays(currentWeek, -DAYS_IN_FOUR_WEEKS), lastFourWeeksEnd),
  };
}

// The exact percent of a window, or null when it has no evaluable periods ("sin datos").
// Rounding is left to whoever shows the number.
export function percentOf({ met, total }) {
  return total === 0 ? null : (met * 100) / total;
}

// Simple average of the habits that have data. Every habit weighs the same.
export function overallPercent(percents) {
  const withData = percents.filter((percent) => percent !== null);
  if (withData.length === 0) {
    return null;
  }
  return withData.reduce((sum, percent) => sum + percent, 0) / withData.length;
}

// Rounds half up for display. Averaging exact fractions can leave float noise
// (37.5 becomes 37.49999999999999), so the value is trimmed to 9 decimals first.
export function roundPercent(percent) {
  return Math.round(Number(percent.toFixed(9)));
}
