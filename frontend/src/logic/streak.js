import { addDays, weekStart } from "./dates.js";

// Number of logs per week, keyed by the Monday that starts the week.
export function countByWeek(logDates) {
  const counts = new Map();
  for (const date of logDates) {
    const week = weekStart(date);
    counts.set(week, (counts.get(week) ?? 0) + 1);
  }
  return counts;
}

// A week is met when it has at least as many logs as the goal.
export function isWeekMet(counts, week, goal) {
  return (counts.get(week) ?? 0) >= goal;
}

// Consecutive marked days, ending today. If today is not marked yet, the streak ends yesterday.
export function dailyStreak(logDates, today) {
  const days = new Set(logDates);
  let cursor = days.has(today) ? today : addDays(today, -1);
  let streak = 0;
  while (days.has(cursor)) {
    streak += 1;
    cursor = addDays(cursor, -1);
  }
  return streak;
}

// Logs in the current week (Monday to Sunday). It can be above the goal.
export function weekProgress(logDates, today) {
  const start = weekStart(today);
  const end = addDays(start, 6);
  return logDates.filter((date) => date >= start && date <= end).length;
}

// Consecutive met weeks. The current week does not break the streak until it ends,
// and a week before the habit existed never counts.
export function weeklyStreakFromCounts(counts, goal, createdOn, today) {
  const first = weekStart(createdOn);
  const current = weekStart(today);
  let cursor = isWeekMet(counts, current, goal) ? current : addDays(current, -7);
  let streak = 0;
  while (cursor >= first && isWeekMet(counts, cursor, goal)) {
    streak += 1;
    cursor = addDays(cursor, -7);
  }
  return streak;
}

// Same as weeklyStreakFromCounts, for callers that only have the log dates.
export function weeklyStreak(logDates, goal, createdOn, today) {
  return weeklyStreakFromCounts(countByWeek(logDates), goal, createdOn, today);
}

// Longest chain of dates in `keys` where each one is `step` days after the previous.
// A chain starts at a date whose predecessor is missing, so every chain is walked once.
function longestRun(keys, step) {
  let best = 0;
  for (const start of keys) {
    if (keys.has(addDays(start, -step))) {
      continue;
    }
    let length = 1;
    for (let next = addDays(start, step); keys.has(next); next = addDays(next, step)) {
      length += 1;
    }
    best = Math.max(best, length);
  }
  return best;
}

// Longest run of consecutive marked days in the whole history.
// Logs are never in the future, so today counts when marked and does not break anything when not.
export function bestDailyStreak(logDates) {
  return longestRun(new Set(logDates), 1);
}

// Longest run of consecutive met weeks in the whole history.
// Weeks that did not reach the goal are simply left out, so the current week never breaks a run.
export function bestWeeklyStreak(counts, goal) {
  const metWeeks = new Set([...counts.keys()].filter((week) => isWeekMet(counts, week, goal)));
  return longestRun(metWeeks, 7);
}
