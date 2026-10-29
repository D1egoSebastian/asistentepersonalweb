import { addDays, weekStart } from "./dates.js";
import { isWeekMet } from "./streak.js";

// What one calendar day shows in the history grid (CA-8.4):
// "done", "missed" (daily only), "empty" (weekly only), "pending" (today) or
// "out" (the habit did not exist yet, or the day has not come).
// `days` is the Set of logged dates, so the grid shows exactly the logs the API sent (CA-8.7).
export function dayState(date, habit, days, today) {
  if (date < habit.createdOn || date > today) {
    return "out";
  }
  if (days.has(date)) {
    return "done";
  }
  if (date === today) {
    return "pending";
  }
  return habit.frequency === "Daily" ? "missed" : "empty";
}

// The logs of a weekly habit's week over its goal, and whether the week is met, missed or still running.
// The current week is met as soon as it reaches the goal (D-T15).
export function weekSummary(monday, counts, goal, today) {
  const count = counts.get(monday) ?? 0;
  if (isWeekMet(counts, monday, goal)) {
    return { count, goal, state: "met" };
  }
  return { count, goal, state: monday === weekStart(today) ? "current" : "missed" };
}

const RECENT_WEEKS = 12;
const DAYS_BEFORE_RECENT_START = (RECENT_WEEKS - 1) * 7;

// Monday of the first week the grid shows: the last 12 weeks (current one included),
// never earlier than the creation week, or the whole history with `showAll`.
function firstGridWeek(habit, today, showAll) {
  const creationWeek = weekStart(habit.createdOn);
  const recentStart = addDays(weekStart(today), -DAYS_BEFORE_RECENT_START);
  return showAll || creationWeek > recentStart ? creationWeek : recentStart;
}

// True when the habit has more than 12 weeks counting the current one, so "Ver todo el historial" makes sense.
export function hasMoreHistory(habit, today) {
  return weekStart(habit.createdOn) < firstGridWeek(habit, today, false);
}

// The weeks of the grid, from the oldest to the newest. A weekly habit also gets each week's summary.
export function historyWeeks(habit, days, counts, today, showAll) {
  const lastWeek = weekStart(today);
  const weeks = [];
  for (let monday = firstGridWeek(habit, today, showAll); monday <= lastWeek; monday = addDays(monday, 7)) {
    weeks.push({
      monday,
      days: Array.from({ length: 7 }, (_, i) => {
        const date = addDays(monday, i);
        return { date, state: dayState(date, habit, days, today) };
      }),
      summary: habit.frequency === "Daily" ? null : weekSummary(monday, counts, habit.weeklyGoal, today),
    });
  }
  return weeks;
}
