import { habitStatus } from "./status.js";
import { dailyStreak, weeklyStreak, weekProgress } from "./streak.js";
import { loggableDays } from "./logWindow.js";
import { goalLabel, unitLabel } from "./labels.js";

const STATUS_LABELS = {
  green: "Cumplido",
  red: "No cumplido",
  pending: "Pendiente",
};

// Everything the habit card shows, computed from the habit and "today". The card does no math itself.
export function toHabitView(habit, today) {
  const isDaily = habit.frequency === "Daily";
  const streak = isDaily
    ? dailyStreak(habit.logDates, today)
    : weeklyStreak(habit.logDates, habit.weeklyGoal, habit.createdOn, today);
  const status = habitStatus(habit, today);

  return {
    id: habit.id,
    name: habit.name,
    description: habit.description,
    frequencyLabel: isDaily ? "Diario" : goalLabel(habit.weeklyGoal),
    streakLabel: streakLabel(streak, isDaily),
    progressLabel: isDaily ? null : `Esta semana: ${weekProgress(habit.logDates, today)} de ${habit.weeklyGoal}`,
    status,
    statusLabel: STATUS_LABELS[status],
    days: loggableDays(habit.createdOn, today).map((day) => ({
      ...day,
      done: habit.logDates.includes(day.date),
    })),
  };
}

function streakLabel(streak, isDaily) {
  return `Racha: ${streak} ${unitLabel(streak, isDaily)}`;
}
