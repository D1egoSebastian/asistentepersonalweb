import { useRef, useState } from "react";
import { addTag } from "../logic/tags.js";
import TagInput from "./TagInput.jsx";

// Captures an idea (mode "create") or edits one (mode "edit"). The text and tags stay when a save fails (CA-1.5).
export default function IdeaComposer({
  mode,
  initialText = "",
  initialTags = [],
  allTags,
  conflict,
  onSubmit,
  onCancel,
  onUseLatest,
}) {
  const [text, setText] = useState(initialText);
  const [tags, setTags] = useState(initialTags);
  const [sending, setSending] = useState(false);
  const sendingRef = useRef(false);

  // The ref blocks a second save before React re-renders the disabled button, so a double click sends one request.
  async function save() {
    if (sendingRef.current) {
      return;
    }
    sendingRef.current = true;
    setSending(true);
    const sent = { text, tags };
    if (mode === "create") {
      setText("");
      setTags([]);
    }
    const saved = await onSubmit(sent);
    sendingRef.current = false;
    setSending(false);
    if (!saved && mode === "create") {
      restore(sent);
    }
  }

  // A failed create gives back what was sent, placed before anything typed meanwhile (CA-1.5).
  function restore(sent) {
    setText((current) => (current === "" ? sent.text : `${sent.text}\n${current}`));
    setTags((current) => sent.tags.reduce((list, tag) => addTag(list, tag, allTags), current));
  }

  // Ctrl+Enter (or Cmd+Enter) saves from the text box, so an idea of one line is typed and saved without the mouse (RNF-2).
  function handleKeyDown(event) {
    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      save();
    }
  }

  function handleSubmit(event) {
    event.preventDefault();
    save();
  }

  return (
    <form className="idea-composer" onSubmit={handleSubmit}>
      <label>
        {mode === "edit" ? "Idea" : "Nueva idea"}
        <textarea
          value={text}
          rows={mode === "edit" ? 6 : 3}
          autoFocus
          onChange={(event) => setText(event.target.value)}
          onKeyDown={handleKeyDown}
        />
      </label>

      <TagInput chosen={tags} allTags={allTags} onChange={setTags} />

      {conflict && (
        <div className="idea-conflict" role="alert">
          <p>{conflict.message}</p>
          <p className="idea-conflict__label">Versión nueva:</p>
          <p className="idea-conflict__text">{conflict.latest.text}</p>
          <button type="button" onClick={onUseLatest}>
            Usar la versión nueva
          </button>
        </div>
      )}

      <div className="idea-composer__actions">
        <button type="submit" disabled={sending}>
          Guardar
        </button>
        {mode === "edit" && (
          <button type="button" onClick={onCancel}>
            Cancelar
          </button>
        )}
      </div>
    </form>
  );
}
