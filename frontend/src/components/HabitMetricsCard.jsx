import HistoryGrid from "./HistoryGrid.jsx";

// One window of the habit: "Desde la creación: 67 % (20 de 30 días)", or just "sin datos".
function WindowLine({ label, data }) {
  return (
    <p className="metric-card__window">
      {label}: {data.percent}
      {data.base && ` (${data.base})`}
    </p>
  );
}

// Shows one habit's metrics. Every value comes from toMetricsView: this component does no math.
export default function HabitMetricsCard({ view, onToggleHistory }) {
  return (
    <article className="metric-card">
      <header className="metric-card__header">
        <h3>{view.name}</h3>
        <span className="metric-card__frequency">{view.frequencyLabel}</span>
      </header>

      <WindowLine label="Desde la creación" data={view.sinceCreation} />
      <WindowLine label="Últimas 4 semanas" data={view.lastFourWeeks} />

      <p className="metric-card__current">{view.currentLabel}</p>
      <p className="metric-card__streaks">
        Racha actual: {view.streakLabel} · Mejor racha: {view.bestStreakLabel}
      </p>

      <HistoryGrid name={view.name} history={view.history} onToggle={() => onToggleHistory(view.id)} />
    </article>
  );
}
