import { useCallback, useEffect, useMemo, useState } from "react";
import { listHabits } from "../api/habitsApi.js";
import { toMetricsView } from "../logic/metricsView.js";
import { toggleId } from "../logic/toggleId.js";

const LOADING = { status: "loading", data: null, error: null };

// Loads the habit list once when the tab opens and turns it into the metrics view.
// The tab is unmounted when the owner leaves it, so every visit gets a fresh list and a fixed "today" (CA-1.3, CA-9.5).
// On error there is no data, so the screen can never show partial figures (CA-1.4).
export function useMetrics() {
  const [state, setState] = useState(LOADING);
  const [attempt, setAttempt] = useState(0);
  const [expandedIds, setExpandedIds] = useState(() => new Set());

  useEffect(() => {
    let cancelled = false;
    listHabits().then(
      (data) => !cancelled && setState({ status: "ready", data, error: null }),
      (failure) => !cancelled && setState({ status: "error", data: null, error: failure.message }),
    );
    return () => {
      cancelled = true;
    };
  }, [attempt]);

  const retry = useCallback(() => {
    setState(LOADING);
    setAttempt((count) => count + 1);
  }, []);

  const toggleHistory = useCallback((id) => setExpandedIds((current) => toggleId(current, id)), []);

  const view = useMemo(
    () => (state.data ? toMetricsView(state.data.habits, state.data.today, expandedIds) : null),
    [state.data, expandedIds],
  );

  return { status: state.status, error: state.error, view, retry, expandedIds, toggleHistory };
}
