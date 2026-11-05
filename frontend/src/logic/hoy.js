import { daysBetween } from "./dates.js";

// Days without an edit before an idea shows up in "Para revisar" (spec 006, CA-4.1).
const STALE_AFTER_DAYS = 14;
const LATEST_IDEAS_COUNT = 5;

const WEEKDAYS = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const MONTHS = [
  "enero", "febrero", "marzo", "abril", "mayo", "junio",
  "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
];

// A habit is done today when today's date is among its logged dates.
export const habitDoneToday = (habit, today) => habit.logDates.includes(today);

// The ring and its counter: how many habits are done today, out of all of them.
export function todayProgress(habits, today) {
  const done = habits.filter((habit) => habitDoneToday(habit, today)).length;
  return { done, total: habits.length };
}

// Ideas whose last edit is at least STALE_AFTER_DAYS old. The list keeps its order.
export function staleIdeas(ideas, today) {
  return ideas.filter((idea) => daysBetween(idea.updatedDate, today) >= STALE_AFTER_DAYS);
}

// The newest ideas by creation date. Ties go to the higher id. The input is not modified.
export function latestIdeas(ideas) {
  return [...ideas]
    .sort((a, b) => (a.createdDate === b.createdDate ? b.id - a.id : a.createdDate < b.createdDate ? 1 : -1))
    .slice(0, LATEST_IDEAS_COUNT);
}

// "hoy", "ayer" or "hace N días", for the date shown under an idea.
export function relativeDay(date, today) {
  const days = daysBetween(date, today);
  if (days === 0) return "hoy";
  if (days === 1) return "ayer";
  return `hace ${days} días`;
}

// The heading of the day, in Spanish. Built from the parts of the date so the time zone does not shift it.
export function todayHeading(today) {
  const [year, month, day] = today.split("-").map(Number);
  const weekday = WEEKDAYS[new Date(year, month - 1, day).getDay()];
  return { weekday, dayOfMonth: `${day} de ${MONTHS[month - 1]}` };
}
