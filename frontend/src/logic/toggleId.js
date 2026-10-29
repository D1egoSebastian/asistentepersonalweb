// A copy of the set with `id` added, or removed if it was there. Never changes the original set.
export function toggleId(ids, id) {
  const next = new Set(ids);
  if (!next.delete(id)) {
    next.add(id);
  }
  return next;
}
