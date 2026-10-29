import { useCallback, useEffect, useRef, useState } from "react";
import {
  archiveIdea,
  createIdea,
  deleteIdea,
  getIdea,
  listIdeas,
  listTags,
  restoreIdea,
  updateIdea,
} from "../api/ideasApi.js";
import { filterOptions } from "../logic/tags.js";

const SEARCH_DELAY_MS = 300;
const DELETE_CONFIRMATION = "Se borrará la idea para siempre. Si puede volver a servirte, archívala. ¿Eliminar?";

// What the owner edits: the version they opened, so the API can detect changes made in another tab (CA-5.5).
const toEditing = (idea) => ({ id: idea.id, version: idea.version, text: idea.text, tags: idea.tags });

// Everything the ideas screen does, so the page only shows it (constitution, principle 3).
export function useIdeas() {
  const [archived, setArchived] = useState(false);
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [tag, setTag] = useState("");
  const [items, setItems] = useState(null);
  const [tagCounts, setTagCounts] = useState([]);
  const [error, setError] = useState(null);
  const [opened, setOpened] = useState(null);
  const [editing, setEditing] = useState(null);
  const [conflict, setConflict] = useState(null);
  const [formKey, setFormKey] = useState(0);
  const latestRequest = useRef(0);

  // The search waits 300 ms after the last key, so typing does not send one request per letter (RF-4).
  useEffect(() => {
    const timer = setTimeout(() => setAppliedSearch(search), SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [search]);

  // Only the newest request may update the screen, so a slow answer cannot overwrite a newer filter.
  const refresh = useCallback(async () => {
    const requestId = ++latestRequest.current;
    try {
      const [list, counts] = await Promise.all([listIdeas({ archived, search: appliedSearch, tag }), listTags()]);
      if (requestId === latestRequest.current) {
        setItems(list.ideas);
        setTagCounts(counts);
        setError(null);
      }
    } catch (failure) {
      if (requestId === latestRequest.current) {
        setError(failure.message);
      }
    }
  }, [archived, appliedSearch, tag]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  // Runs one API call and reports the outcome; the caller decides what to show.
  async function attempt(call) {
    try {
      return { ok: true, value: await call() };
    } catch (failure) {
      return { ok: false, failure };
    }
  }

  // A 404 means the idea is gone (deleted elsewhere), so the list is refreshed and it disappears.
  function reportFailure(failure) {
    setError(failure.message);
    if (failure.status === 404) {
      refresh();
    }
  }

  function clearFilters() {
    setSearch("");
    setAppliedSearch("");
    setTag("");
  }

  async function openIdea(id) {
    setEditing(null);
    setConflict(null);
    const result = await attempt(() => getIdea(id));
    if (result.ok) {
      setError(null);
      setOpened(result.value);
    } else {
      reportFailure(result.failure);
    }
  }

  async function startEdit(id) {
    const result = await attempt(() => getIdea(id));
    if (!result.ok) {
      reportFailure(result.failure);
      return;
    }
    setError(null);
    setOpened(null);
    setConflict(null);
    setEditing(toEditing(result.value));
    setFormKey((key) => key + 1);
  }

  function cancelEdit() {
    setEditing(null);
    setConflict(null);
  }

  // The idea changed while it was being edited: fetch its new version and show it next to the form,
  // keeping the text the owner typed (CA-5.5). If it was archived meanwhile, it opens read-only (CA-5.4).
  async function showLatest(id, message) {
    const result = await attempt(() => getIdea(id));
    if (!result.ok) {
      reportFailure(result.failure);
      return;
    }
    const latest = result.value;
    if (latest.archived) {
      setEditing(null);
      setConflict(null);
      setOpened(latest);
      setError(message);
      return;
    }
    setConflict({ message, latest });
    setEditing((current) => current && { ...current, version: latest.version });
  }

  // Returns true when the idea was saved, so the form knows it can leave the text behind.
  async function saveEdit(values) {
    const result = await attempt(() => updateIdea(editing.id, { ...values, version: editing.version }));
    if (result.ok) {
      setEditing(null);
      setConflict(null);
      await refresh();
      return true;
    }
    if (result.failure.status === 409) {
      await showLatest(editing.id, result.failure.message);
      return false;
    }
    if (result.failure.status === 404) {
      setEditing(null);
    }
    reportFailure(result.failure);
    return false;
  }

  // "Usar la versión nueva": the form loads the new text and tags, and the next save uses their version.
  function loadLatestVersion() {
    setEditing(toEditing(conflict.latest));
    setConflict(null);
    setFormKey((key) => key + 1);
  }

  async function create(values) {
    const result = await attempt(() => createIdea(values));
    if (!result.ok) {
      reportFailure(result.failure);
      return false;
    }
    await refresh();
    return true;
  }

  // Archiving moves the idea to the other section; restoring moves it back.
  async function toggleArchive(id, isArchived) {
    const result = await attempt(() => (isArchived ? restoreIdea(id) : archiveIdea(id)));
    if (!result.ok) {
      reportFailure(result.failure);
      return;
    }
    setOpened(null);
    await refresh();
  }

  async function remove(id) {
    if (!window.confirm(DELETE_CONFIRMATION)) {
      return;
    }
    const result = await attempt(() => deleteIdea(id));
    if (!result.ok) {
      reportFailure(result.failure);
      return;
    }
    setOpened(null);
    await refresh();
  }

  return {
    archived,
    setArchived,
    search,
    setSearch,
    tag,
    setTag,
    tagOptions: filterOptions(tagCounts, archived),
    allTags: tagCounts.map((item) => item.name),
    filtersActive: search.trim() !== "" || tag !== "",
    clearFilters,
    items,
    error,
    opened,
    openIdea,
    closeOpened: () => setOpened(null),
    editing,
    conflict,
    formKey,
    startEdit,
    cancelEdit,
    saveEdit,
    loadLatestVersion,
    create,
    toggleArchive,
    remove,
  };
}
