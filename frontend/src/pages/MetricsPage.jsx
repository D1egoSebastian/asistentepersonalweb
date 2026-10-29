import ErrorBanner from "../components/ErrorBanner.jsx";
import HabitMetricsCard from "../components/HabitMetricsCard.jsx";
import MetricsSummary from "../components/MetricsSummary.jsx";
import TrendTable from "../components/TrendTable.jsx";
import { useMetrics } from "../hooks/useMetrics.js";

// Shows the metrics screen. All the logic lives in useMetrics and src/logic (constitution, principle 3).
export default function MetricsPage() {
  const metrics = useMetrics();
  const { view } = metrics;

  return (
    <section className="metrics-page">
      <h1>Métricas</h1>

      {metrics.status === "loading" && <p>Cargando métricas…</p>}

      {metrics.status === "error" && (
        <div className="metrics-page__error">
          <ErrorBanner message={metrics.error} />
          <button type="button" onClick={metrics.retry}>
            Reintentar
          </button>
        </div>
      )}

      {view?.isEmpty && (
        <p className="metrics-page__empty">Todavía no tienes hábitos. Crea el primero en la pestaña Hábitos.</p>
      )}

      {view && !view.isEmpty && (
        <>
          <MetricsSummary overall={view.overall} />

          <section className="metrics-trend" aria-labelledby="metrics-trend-title">
            <h2 id="metrics-trend-title">Últimas 12 semanas</h2>
            <TrendTable trend={view.trend} hasTrend={view.hasTrend} />
          </section>

          <section className="metrics-habits" aria-labelledby="metrics-habits-title">
            <h2 id="metrics-habits-title">Detalle por hábito</h2>
            <ul className="metrics-habits__list">
              {view.habits.map((habit) => (
                <li key={habit.id}>
                  <HabitMetricsCard view={habit} onToggleHistory={metrics.toggleHistory} />
                </li>
              ))}
            </ul>
          </section>
        </>
      )}
    </section>
  );
}
