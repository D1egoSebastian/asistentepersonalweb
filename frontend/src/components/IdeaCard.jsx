import { formatDate } from "../logic/dates.js";

// Shows one idea: its summary, tags and date. Opening it shows the whole text.
// Archived ideas are read-only: they have no "Editar" (CA-5.4).
export default function IdeaCard({ idea, archived, detail, onOpen, onClose, onEdit, onToggleArchive, onDelete }) {
  return (
    <article className={`idea-card${archived ? " idea-card--archived" : ""}`}>
      <button
        type="button"
        className="idea-card__summary"
        aria-expanded={detail !== null}
        onClick={detail ? onClose : onOpen}
      >
        {idea.summary}
      </button>

      {detail && <p className="idea-card__text">{detail.text}</p>}

      {idea.tags.length > 0 && (
        <ul className="idea-card__tags">
          {idea.tags.map((tag) => (
            <li key={tag} className="tag-chip">
              {tag}
            </li>
          ))}
        </ul>
      )}

      <p className="idea-card__meta">
        <time dateTime={idea.createdDate}>{formatDate(idea.createdDate)}</time>
        {archived && " · Archivada"}
      </p>

      <div className="idea-card__actions">
        {!archived && <button type="button" onClick={onEdit}>Editar</button>}
        <button type="button" onClick={onToggleArchive}>
          {archived ? "Restaurar" : "Archivar"}
        </button>
        <button type="button" onClick={onDelete}>
          Eliminar
        </button>
      </div>
    </article>
  );
}
