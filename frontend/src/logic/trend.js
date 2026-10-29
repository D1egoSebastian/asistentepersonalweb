import { addDays, weekStart } from "./dates.js";
import { completionIn, overallPercent, percentOf } from "./completion.js";

// The Mondays of the last `count` closed weeks, from the oldest to the newest.
// The current week is not closed yet, so it is never included.
export function closedWeeks(today, count) {
  const lastClosed = addDays(weekStart(today), -7);
  return Array.from({ length: count }, (_, i) => addDays(lastClosed, -7 * (count - 1 - i)));
}

const TREND_WEEKS = 12;

// Met and total periods of a habit in the week that starts on `monday`.
// A daily habit counts its days; a weekly habit counts the week as a whole (0 % or 100 %).
export function weekCompletion(habit, counts, monday) {
  const end = habit.frequency === "Daily" ? addDays(monday, 6) : monday;
  return completionIn(habit, counts, monday, end);
}

// One point per closed week: the overall percent of that week, or null when no habit existed.
// Each habit comes indexed with its weekly `counts`, computed once.
export function trend(indexedHabits, today) {
  return closedWeeks(today, TREND_WEEKS).map((monday) => {
    const percents = indexedHabits.map((habit) => percentOf(weekCompletion(habit, habit.counts, monday)));
    return { monday, percent: overallPercent(percents) };
  });
}
