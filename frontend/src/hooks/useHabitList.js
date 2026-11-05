import { useCallback, useEffect, useState } from "react";
import { listHabits } from "../api/habitsApi.js";

// Loads the habit list and runs the actions that change it.
// The server sends "today", so the screen never decides the date on its own.
export function useHabitList() {
  const [today, setToday] = useState(null);
  const [habits, setHabits] = useState([]);
  const [error, setError] = useState(null);

  const reload = useCallback(async () => {
    try {
      const list = await listHabits();
      setToday(list.today);
      setHabits(list.habits);
    } catch (err) {
      setError(err.message);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  // Runs one API call, shows its error if it fails, and always reloads the list afterwards.
  // Returns the error, or null when the call succeeded.
  const run = useCallback(
    async (call) => {
      setError(null);
      let failure = null;
      try {
        await call();
      } catch (err) {
        failure = err;
        setError(err.message);
      }
      await reload();
      return failure;
    },
    [reload],
  );

  return { today, habits, error, run, reload };
}
