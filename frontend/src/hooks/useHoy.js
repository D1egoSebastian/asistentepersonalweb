import { useCallback, useEffect, useState } from "react";
import { logHabit, unlogHabit } from "../api/habitsApi.js";
import { createIdea, listIdeas } from "../api/ideasApi.js";
import { habitDoneToday, todayProgress, staleIdeas, latestIdeas } from "../logic/hoy.js";
import { toHabitView } from "../logic/habitView.js";
import { useHabitList } from "./useHabitList.js";

// Everything the "Hoy" screen does, so the page only shows it (constitution, principle 3).
// The shell runs it, so the side rail can show the same counts.
export function useHoy() {
  const habitList = useHabitList();
  const [ideas, setIdeas] = useState([]);
  const [ideasError, setIdeasError] = useState(null);

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

  const { today, habits, error: habitsError, run, reload: reloadHabits } = habitList;

  // Reloads both lists, so the page and the side rail agree with the server.
  const reload = useCallback(async () => {
    await Promise.all([reloadHabits(), reloadIdeas()]);
  }, [reloadHabits, reloadIdeas]);

  // Marks or unmarks a habit for today. The shared list reloads after the call.
  function toggleToday(habit) {
    const call = habitDoneToday(habit, today) ? () => unlogHabit(habit.id, today) : () => logHabit(habit.id, today);
    return run(call);
  }

  // Returns true when the idea was saved, so the form knows it can clear its text.
  async function saveIdea(text) {
    try {
      await createIdea({ text, tags: [] });
    } catch (failure) {
      setIdeasError(failure.message);
      return false;
    }
    await reloadIdeas();
    return true;
  }

  const ready = today !== null;

  return {
    ready,
    today,
    error: habitsError ?? ideasError,
    items: ready
      ? habits.map((habit) => ({ habit, view: toHabitView(habit, today), done: habitDoneToday(habit, today) }))
      : [],
    progress: ready ? todayProgress(habits, today) : { done: 0, total: 0 },
    ideaCount: ideas.length,
    stale: ready ? staleIdeas(ideas, today) : [],
    latest: latestIdeas(ideas),
    toggleToday,
    saveIdea,
    reload,
  };
}
