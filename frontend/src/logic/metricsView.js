import { countByWeek, dailyStreak, weeklyStreakFromCounts, weekProgress, bestDailyStreak, bestWeeklyStreak } from "./streak.js";
import { habitWindows, overallPercent, percentOf, roundPercent } from "./completion.js";
import { goalLabel, unitLabel } from "./labels.js";
import { formatDate } from "./dates.js";
import { trend } from "./trend.js";
import { historyWeeks, hasMoreHistory } from "./history.js";

const NO_DATA = "sin datos";

// What each day of the history grid shows: a visible symbol and the text read by screen readers.
const DAY_CELLS = {
  done: { symbol: "✓", text: "cumplido" },
  missed: { symbol: "✗", text: "no cumplido" },
  empty: { symbol: "·", text: "sin registro" },
  pending: { symbol: "…", text: "pendiente" },
  out: { symbol: "", text: "fuera de rango" },
};

const WEEK_STATE_TEXT = {
  met: "Cumplida",
  missed: "No cumplida",
  current: "En curso",
};

// "72 %", or "sin datos" when the window has no evaluable period. Rounding happens only here (CA-3.4).
function percentText(percent) {
  return percent === null ? NO_DATA : `${roundPercent(percent)} %`;
}

// Weekly habits get their logs-per-week map once, so no function has to count the logs again.
function indexHabit(habit) {
  const counts = habit.frequency === "Daily" ? null : countByWeek(habit.logDates);
  return { ...habit, counts, days: new Set(habit.logDates) };
}

// The overall percent of each window: the exact average of the habits that have data.
function overallView(windows) {
  const overallOf = (name) => overallPercent(windows.map((habitWindow) => percentOf(habitWindow[name])));
  return {
    sinceCreation: percentText(overallOf("sinceCreation")),
    lastFourWeeks: percentText(overallOf("lastFourWeeks")),
  };
}

// One window of a habit: "67 %" and "20 de 30 días", or "sin datos" with no base.
function windowView({ met, total }, isDaily) {
  return {
    percent: percentText(percentOf({ met, total })),
    base: total === 0 ? null : `${met} de ${total} ${unitLabel(total, isDaily)}`,
  };
}

// "Hoy: hecho" for daily habits, "Esta semana: 2 de 3" for weekly ones.
function currentLabel(habit, today) {
  if (habit.frequency === "Daily") {
    return habit.days.has(today) ? "Hoy: hecho" : "Hoy: pendiente";
  }
  return `Esta semana: ${weekProgress(habit.logDates, today)} de ${habit.weeklyGoal}`;
}

// The current streak and the best one, with their unit: "5 días", "2 semanas".
function streakLabels(habit, today) {
  const isDaily = habit.frequency === "Daily";
  const current = isDaily
    ? dailyStreak(habit.logDates, today)
    : weeklyStreakFromCounts(habit.counts, habit.weeklyGoal, habit.createdOn, today);
  const best = isDaily ? bestDailyStreak(habit.logDates) : bestWeeklyStreak(habit.counts, habit.weeklyGoal);
  return {
    streakLabel: `${current} ${unitLabel(current, isDaily)}`,
    bestStreakLabel: `${best} ${unitLabel(best, isDaily)}`,
  };
}

// One point of the trend. The chart and the table both read `value` and `text`, so they cannot differ.
function trendPoint({ monday, percent }) {
  return {
    monday,
    label: formatDate(monday),
    value: percent === null ? null : roundPercent(percent),
    text: percentText(percent),
  };
}

function cellView({ date, state }) {
  const { symbol, text } = DAY_CELLS[state];
  return { date, state, symbol, label: `${formatDate(date)}: ${text}` };
}

// "2 de 3 · Cumplida".
function summaryView(summary) {
  if (summary === null) {
    return null;
  }
  return { ...summary, label: `${summary.count} de ${summary.goal} · ${WEEK_STATE_TEXT[summary.state]}` };
}

// The history grid: the last 12 weeks, or the whole history when the habit is expanded.
function historyView(habit, today, expanded) {
  const weeks = historyWeeks(habit, habit.days, habit.counts, today, expanded);
  return {
    weeks: weeks.map((week) => ({
      monday: week.monday,
      days: week.days.map(cellView),
      summary: summaryView(week.summary),
    })),
    expanded,
    hasMore: hasMoreHistory(habit, today),
  };
}

function habitCard(habit, windows, today, expanded) {
  const isDaily = habit.frequency === "Daily";
  return {
    id: habit.id,
    name: habit.name,
    frequencyLabel: isDaily ? "Diario" : goalLabel(habit.weeklyGoal),
    sinceCreation: windowView(windows.sinceCreation, isDaily),
    lastFourWeeks: windowView(windows.lastFourWeeks, isDaily),
    currentLabel: currentLabel(habit, today),
    ...streakLabels(habit, today),
    history: historyView(habit, today, expanded),
  };
}

// Everything the metrics screen shows, computed from the habits and "today". The components do no math.
export function toMetricsView(habits, today, expandedIds) {
  const indexed = habits.map(indexHabit);
  const windows = indexed.map((habit) => habitWindows(habit, habit.counts, today));

  const points = trend(indexed, today).map(trendPoint);

  return {
    isEmpty: habits.length === 0,
    overall: overallView(windows),
    trend: points,
    hasTrend: points.some((point) => point.value !== null),
    habits: indexed.map((habit, i) => habitCard(habit, windows[i], today, expandedIds.has(habit.id))),
  };
}
