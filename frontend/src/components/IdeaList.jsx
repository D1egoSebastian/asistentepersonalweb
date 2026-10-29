import IdeaCard from "./IdeaCard.jsx";
import IdeaComposer from "./IdeaComposer.jsx";

// Lists the ideas of the section in the order the API sends them. One idea at a time can show its editor instead of its card.
export default function IdeaList({
  items,
  archived,
  filtersActive,
  allTags,
  opened,
  editing,
  conflict,
  formKey,
  onOpen,
  onCloseOpened,
  onEdit,
  onSaveEdit,
  onCancelEdit,
  onUseLatest,
  onToggleArchive,
  onDelete,
  onClearFilters,
}) {
  if (items === null) {
    return null;
  }

  if (items.length === 0) {
    return <EmptyState archived={archived} filtersActive={filtersActive} onClearFilters={onClearFilters} />;
  }

  return (
    <ul className="idea-list">
      {items.map((idea) => {
        const detail = opened?.id === idea.id ? opened : null;
        // An idea archived in another tab shows as archived even while it is listed as active (CA-5.4).
        const isArchived = archived || detail?.archived === true;

        return (
          <li key={idea.id}>
            {editing?.id === idea.id ? (
              <IdeaComposer
                key={formKey}
                mode="edit"
                initialText={editing.text}
                initialTags={editing.tags}
                allTags={allTags}
                conflict={conflict}
                onSubmit={onSaveEdit}
                onCancel={onCancelEdit}
                onUseLatest={onUseLatest}
              />
            ) : (
              <IdeaCard
                idea={idea}
                archived={isArchived}
                detail={detail}
                onOpen={() => onOpen(idea.id)}
                onClose={onCloseOpened}
                onEdit={() => onEdit(idea.id)}
                onToggleArchive={() => onToggleArchive(idea.id, isArchived)}
                onDelete={() => onDelete(idea.id)}
              />
            )}
          </li>
        );
      })}
    </ul>
  );
}

function EmptyState({ archived, filtersActive, onClearFilters }) {
  if (filtersActive) {
    return (
      <div className="idea-list__empty">
        <p>No hay ideas que coincidan.</p>
        <button type="button" onClick={onClearFilters}>
          Quitar filtros
        </button>
      </div>
    );
  }

  const message = archived ? "No tienes ideas archivadas." : "Todavía no tienes ideas. Anota la primera arriba.";
  return <p className="idea-list__empty">{message}</p>;
}
