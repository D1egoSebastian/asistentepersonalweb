const WEEKDAYS = [
  { short: "L", full: "Lunes" },
  { short: "M", full: "Martes" },
  { short: "X", full: "Miércoles" },
  { short: "J", full: "Jueves" },
  { short: "V", full: "Viernes" },
  { short: "S", full: "Sábado" },
  { short: "D", full: "Domingo" },
];

// One day. The symbol is for the eyes and the hidden text gives the date and state to screen readers (RNF-4).
// It has no button: marking days is only done in "Hábitos" (CA-8.6).
function DayCell({ day }) {
  return (
    <td className={`history-cell history-cell--${day.state}`}>
      <span aria-hidden="true">{day.symbol}</span>
      <span className="visually-hidden">{day.label}</span>
    </td>
  );
}

// The day-by-day history of one habit: one row per week, from Monday to Sunday, oldest week first.
export default function HistoryGrid({ name, history, onToggle }) {
  return (
    <div className="history-grid">
      <table>
        <caption className="visually-hidden">Historial de {name}</caption>
        <thead>
          <tr>
            {WEEKDAYS.map((weekday) => (
              <th key={weekday.short} scope="col">
                <span aria-hidden="true">{weekday.short}</span>
                <span className="visually-hidden">{weekday.full}</span>
              </th>
            ))}
            {history.hasSummary && (
              <th scope="col" className="history-grid__summary-head">
                Semana
              </th>
            )}
          </tr>
        </thead>
        <tbody>
          {history.weeks.map((week) => (
            <tr key={week.monday}>
              {week.days.map((day) => (
                <DayCell key={day.date} day={day} />
              ))}
              {week.summary && (
                <td className={`history-grid__summary history-grid__summary--${week.summary.state}`}>
                  {week.summary.label}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>

      {history.hasMore && (
        <button type="button" onClick={onToggle}>
          {history.expanded ? "Ver las últimas 12 semanas" : "Ver todo el historial"}
        </button>
      )}
    </div>
  );
}
