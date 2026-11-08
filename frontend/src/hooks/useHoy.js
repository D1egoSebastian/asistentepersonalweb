import { useCallback, useEffect, useState } from "react";
import { logHabit, unlogHabit } from "../api/habitsApi.js";
import { createIdea, listIdeas } from "../api/ideasApi.js";
import { applyTodayOverrides, habitDoneToday, todayProgress, staleIdeas, latestIdeas } from "../logic/hoy.js";
import { toHabitView } from "../logic/habitView.js";
import { extractHashtags } from "../logic/tags.js";
import { useHabitList } from "./useHabitList.js";

// Everything the "Hoy" screen does, so the page only shows it (constitution, principle 3).
// The shell runs it, so the side rail can show the same counts.
export function useHoy() {
  const { today, habits: serverHabits, error: habitsError, run, reload: reloadHabits } = useHabitList();
  const [ideas, setIdeas] = useState([]);
  const [ideasError, setIdeasError] = useState(null);
  // Toggles still waiting for the server, by habit id. The screen shows them right away.
  const [overrides, setOverrides] = useState({});

  const reloadIdeas = useCallback(async () => {
    try {
      const list = await listIdeas({ archived: false });
      setIdeas(list.ideas);
      setIdeasError(null);
    } catch (failure) {
      setIdeasError(failure.message);
    }
  }, []);

  useEffect(() => {
    reloadIdeas();
  }, [reloadIdeas]);

  // Reloads both lists, so the page and the side rail agree with the server.
  const reload = useCallback(async () => {
    await Promise.all([reloadHabits(), reloadIdeas()]);
  }, [reloadHabits, reloadIdeas]);

  const ready = today !== null;
  const habits = ready ? applyTodayOverrides(serverHabits, overrides, today) : [];

  // Marks or unmarks a habit for today. The check changes at once; when the server answers,
  // the list reloads and the override is dropped, so a failed call goes back to the real state.
  async function toggleToday(habit) {
    if (Object.hasOwn(overrides, habit.id)) {
      return;
    }
    const done = habitDoneToday(habit, today);
    setOverrides((current) => ({ ...current, [habit.id]: !done }));
    await run(() => (done ? unlogHabit(habit.id, today) : logHabit(habit.id, today)));
    setOverrides(({ [habit.id]: _settled, ...rest }) => rest);
  }

  // "#word" in the text becomes a tag (spec 006, CA-3.2). Returns true when the idea was saved.
  async function saveIdea(input) {
    try {
      await createIdea(extractHashtags(input));
    } catch (failure) {
      setIdeasError(failure.message);
      return false;
    }
    await reloadIdeas();
    return true;
  }

  return {
    ready,
    today,
    error: habitsError ?? ideasError,
    items: habits.map((habit) => ({ habit, view: toHabitView(habit, today), done: habitDoneToday(habit, today) })),
    progress: ready ? todayProgress(habits, today) : { done: 0, total: 0 },
    ideaCount: ideas.length,
    stale: ready ? staleIdeas(ideas, today) : [],
    latest: latestIdeas(ideas),
    toggleToday,
    saveIdea,
    reload,
  };
}
