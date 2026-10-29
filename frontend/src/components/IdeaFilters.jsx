// Shows the search, the tag filter and the Activas/Archivadas tabs. The page owns the values (constitution, principle 3).
export default function IdeaFilters({ archived, onArchivedChange, search, onSearchChange, tag, tagOptions, onTagChange }) {
  // A tag can stay selected after its last idea leaves this section; keep it listed so the filter still shows it.
  const options = tag !== "" && !tagOptions.includes(tag) ? [...tagOptions, tag] : tagOptions;

  return (
    <div className="idea-filters">
      <div className="idea-filters__tabs">
        <button type="button" aria-pressed={!archived} onClick={() => onArchivedChange(false)}>
          Activas
        </button>
        <button type="button" aria-pressed={archived} onClick={() => onArchivedChange(true)}>
          Archivadas
        </button>
      </div>

      <label>
        Buscar
        <input type="search" value={search} onChange={(event) => onSearchChange(event.target.value)} />
      </label>

      <label>
        Etiqueta
        <select value={tag} onChange={(event) => onTagChange(event.target.value)}>
          <option value="">Todas las etiquetas</option>
          {options.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
