// The weekly trend as a table, so it can be read without the chart (CA-7.5, RNF-4).
// Each row shows the week by the date of its Monday and the same rounded text the chart uses.
export default function TrendTable({ trend, hasTrend }) {
  if (!hasTrend) {
    return <p className="trend-table__empty">Sin datos todavía.</p>;
  }

  return (
    <table className="trend-table">
      <caption className="visually-hidden">Cumplimiento general por semana</caption>
      <thead>
        <tr>
          <th scope="col">Semana</th>
          <th scope="col">Cumplimiento</th>
        </tr>
      </thead>
      <tbody>
        {trend.map((point) => (
          <tr key={point.monday}>
            <th scope="row">Semana del {point.label}</th>
            <td>{point.text}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
