// Shows one habit. Every value comes from toHabitView: this component does no date math.
export default function HabitCard({ view, busy, onToggleDay, onEdit, onDelete }) {
  return (
    <article className={`habit-card habit-card--${view.status}`}>
      <header className="habit-card__header">
        <h2>{view.name}</h2>
        <span className="habit-card__status">{view.statusLabel}</span>
      </header>

      {view.description && <p className="habit-card__description">{view.description}</p>}

      <p className="habit-card__meta">
        {view.frequencyLabel} · {view.streakLabel}
      </p>
      {view.progressLabel && <p className="habit-card__progress">{view.progressLabel}</p>}

      <div className="habit-card__days">
        {view.days.map((day) => (
          <button
            key={day.date}
            type="button"
            aria-pressed={day.done}
            disabled={busy}
            onClick={() => onToggleDay(view.id, day.date, day.done)}
          >
            {day.label}
            {day.done ? " ✓" : ""}
          </button>
        ))}
      </div>

      <footer className="habit-card__actions">
        <button type="button" onClick={onEdit} disabled={busy}>
          Editar
        </button>
        <button type="button" onClick={onDelete} disabled={busy}>
          Eliminar
        </button>
      </footer>
    </article>
  );
}
