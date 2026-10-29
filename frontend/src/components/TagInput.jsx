import { useState } from "react";
import { MAX_TAGS_PER_IDEA, addTag, splitTags, suggestTags } from "../logic/tags.js";

// Chosen tags as chips, with suggestions from the existing tags of the owner while typing (CA-2.1, CA-2.10).
export default function TagInput({ chosen, allTags, onChange }) {
  const [draft, setDraft] = useState("");
  const full = chosen.length >= MAX_TAGS_PER_IDEA;
  const suggestions = suggestTags(allTags, draft, chosen);

  // Enter, a comma or a picked suggestion adds the text; "libros, cine" adds both tags.
  function addDraft(text) {
    onChange(splitTags(text).reduce((list, tag) => addTag(list, tag, allTags), chosen));
    setDraft("");
  }

  function handleKeyDown(event) {
    if ((event.key === "Enter" || event.key === ",") && !event.ctrlKey && !event.metaKey) {
      event.preventDefault();
      addDraft(draft);
    }
  }

  return (
    <div className="tag-input">
      <ul className="tag-input__chips">
        {chosen.map((tag) => (
          <li key={tag} className="tag-chip">
            {tag}
            <button
              type="button"
              aria-label={`Quitar la etiqueta ${tag}`}
              onClick={() => onChange(chosen.filter((picked) => picked !== tag))}
            >
              ×
            </button>
          </li>
        ))}
      </ul>

      <label>
        Etiquetas
        <input
          value={draft}
          disabled={full}
          placeholder="Escribe y pulsa Enter"
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
        />
      </label>

      {full && <p className="tag-input__hint">Una idea puede tener hasta {MAX_TAGS_PER_IDEA} etiquetas.</p>}

      {suggestions.length > 0 && (
        <ul className="tag-input__suggestions" aria-label="Sugerencias de etiquetas">
          {suggestions.map((tag) => (
            <li key={tag}>
              <button type="button" onClick={() => addDraft(tag)}>
                {tag}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
