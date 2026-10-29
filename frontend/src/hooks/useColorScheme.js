import { useSyncExternalStore } from "react";

const DARK_QUERY = "(prefers-color-scheme: dark)";

// Calls `onChange` when the system switches between light and dark, and returns the cleanup.
function subscribe(onChange) {
  const query = window.matchMedia(DARK_QUERY);
  query.addEventListener("change", onChange);
  return () => query.removeEventListener("change", onChange);
}

function getScheme() {
  return window.matchMedia(DARK_QUERY).matches ? "dark" : "light";
}

// Returns "light" or "dark" and re-renders when the system setting changes (CA-7.6).
export function useColorScheme() {
  return useSyncExternalStore(subscribe, getScheme);
}
