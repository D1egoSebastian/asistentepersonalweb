// Shows the overall completion of both windows. Every value comes from toMetricsView.
export default function MetricsSummary({ overall }) {
  return (
    <section className="metrics-summary" aria-labelledby="metrics-summary-title">
      <h2 id="metrics-summary-title">Cumplimiento general</h2>
      <p>Desde la creación: {overall.sinceCreation}</p>
      <p>Últimas 4 semanas: {overall.lastFourWeeks}</p>
    </section>
  );
}
