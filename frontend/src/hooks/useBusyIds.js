import { useCallback, useState } from "react";

// Ids of the habits with an action in progress. Their buttons stay disabled meanwhile.
export function useBusyIds() {
  const [busyIds, setBusyIds] = useState(() => new Set());

  const setBusy = useCallback((id, busy) => {
    setBusyIds((previous) => {
      const next = new Set(previous);
      if (busy) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }, []);

  return { busyIds, setBusy };
}
