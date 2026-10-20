import { useState } from "react";
import { createHabit, deleteHabit, logHabit, unlogHabit, updateHabit } from "../api/habitsApi.js";
import { toHabitView } from "../logic/habitView.js";
import { useBusyIds } from "./useBusyIds.js";
import { useHabitList } from "./useHabitList.js";

const DELETE_CONFIRMATION =
  "Se borrará el hábito y todo su historial. Esta acción no se puede deshacer. ¿Continuar?";

// Everything the habits screen does, so the page only shows it (constitution, principle 3).
export function useHabits() {
  const { today, habits, error, run } = useHabitList();
  const { busyIds, setBusy } = useBusyIds();
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState(null);

  async function runOnHabit(habitId, call) {
    setBusy(habitId, true);
    await run(call);
    setBusy(habitId, false);
  }

  const toggleDay = (habitId, date, done) =>
    runOnHabit(habitId, () => (done ? unlogHabit(habitId, date) : logHabit(habitId, date)));

  function remove(habit) {
    if (window.confirm(DELETE_CONFIRMATION)) {
      runOnHabit(habit.id, () => deleteHabit(habit.id));
    }
  }

  async function create(values) {
    const failure = await run(() => createHabit(values));
    if (!failure) {
      setCreating(false);
    }
  }

  // A 404 means the habit was deleted elsewhere: the form closes, and the list is already refreshed.
  async function saveEdit(habitId, values) {
    const failure = await run(() => updateHabit(habitId, values));
    if (!failure || failure.status === 404) {
      setEditingId(null);
    }
  }

  const items = today === null ? [] : habits.map((habit) => ({ habit, view: toHabitView(habit, today) }));

  return { today, error, items, busyIds, creating, setCreating, editingId, setEditingId, toggleDay, remove, create, saveEdit };
}
