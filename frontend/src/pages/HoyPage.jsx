import { useState } from "react";
import ErrorBanner from "../components/ErrorBanner.jsx";
import { daysBetween } from "../logic/dates.js";
import { relativeDay, todayHeading } from "../logic/hoy.js";

const RING_CIRCUMFERENCE = 251.3;

// Shows the day: progress, today's habits, quick capture and the ideas that need attention.
// All the logic lives in useHoy (constitution, principle 3).
export default function HoyPage({ hoy }) {
  const [text, setText] = useState("");

  if (!hoy.ready) {
    return <p className="hoy-loading">Cargando…</p>;
  }

  const { today, items, progress, stale, latest } = hoy;
  const heading = todayHeading(today);
  const remaining = progress.total - progress.done;
  const ringOffset = progress.total === 0 ? RING_CIRCUMFERENCE : RING_CIRCUMFERENCE * (1 - progress.done / progress.total);

  async function handleSubmit(event) {
    event.preventDefault();
    const clean = text.trim();
    if (clean === "") {
      return;
    }
    if (await hoy.saveIdea(clean)) {
      setText("");
    }
  }

  return (
    <div className="hoy">
      <section className="hoy-day" aria-labelledby="hoy-title">
        <p className="hoy-eyebrow">{heading.weekday}</p>
        <h1 className="hoy-date" id="hoy-title">
          {heading.dayOfMonth}
        </h1>

        <div className="hoy-summary">
          <svg className="hoy-ring" viewBox="0 0 96 96" aria-hidden="true">
            <circle className="hoy-ring__track" cx="48" cy="48" r="40" />
            <circle
              className="hoy-ring__value"
              cx="48"
              cy="48"
              r="40"
              strokeDasharray={RING_CIRCUMFERENCE}
              strokeDashoffset={ringOffset}
              transform="rotate(-90 48 48)"
            />
          </svg>
          <div>
            <strong>
              {progress.done} de {progress.total} hechos
            </strong>
            <p>
              {progress.total === 0
                ? "Todavía no tienes hábitos. Crea el primero en Hábitos."
                : remaining === 0
                  ? "Todo hecho por hoy."
                  : `Te quedan ${remaining}. Marca cada hábito cuando lo cumplas.`}
            </p>
          </div>
        </div>

        <ErrorBanner message={hoy.error} />

        <ul className="hoy-habits">
          {items.map(({ habit, view, done }) => (
            <li className="hoy-habit" key={view.id}>
              <button
                type="button"
                className="hoy-check"
                aria-pressed={done}
                aria-label={`Marcar ${view.name}`}
                onClick={() => hoy.toggleToday(habit)}
              >
                <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <path d="m3 8.5 3 3 7-7" />
                </svg>
              </button>
              <div>
                <h3>{view.name}</h3>
                <p>{view.progressLabel ? `${view.frequencyLabel} · ${view.progressLabel}` : view.frequencyLabel}</p>
              </div>
              {view.status === "red" ? (
                <span className="hoy-streak hoy-streak--risk">en riesgo</span>
              ) : (
                <span className="hoy-streak">{view.streakLabel}</span>
              )}
            </li>
          ))}
        </ul>
      </section>

      <aside className="hoy-panel" aria-label="Captura y pendientes">
        <form className="hoy-capture" onSubmit={handleSubmit}>
          <h2>Captura rápida</h2>
          <textarea
            value={text}
            onChange={(event) => setText(event.target.value)}
            placeholder="Anota lo que se te ocurra…"
            aria-label="Nueva idea"
          />
          <button type="submit" disabled={text.trim() === ""}>
            Guardar idea
          </button>
        </form>

        {stale.length > 0 && (
          <section className="hoy-block" aria-labelledby="hoy-review">
            <h2 id="hoy-review">Para revisar</h2>
            <ul className="hoy-alerts">
              {stale.map((idea) => (
                <li className="hoy-alert" key={idea.id}>
                  <i aria-hidden="true" />
                  <span>
                    «{idea.summary}» lleva {daysBetween(idea.updatedDate, today)} días sin editarse.
                  </span>
                </li>
              ))}
            </ul>
          </section>
        )}

        <section className="hoy-block" aria-labelledby="hoy-latest">
          <h2 id="hoy-latest">Últimas ideas</h2>
          {latest.length === 0 ? (
            <p className="hoy-empty">Todavía no anotaste ideas.</p>
          ) : (
            <ul className="hoy-ideas">
              {latest.map((idea) => (
                <li key={idea.id}>
                  {idea.summary}
                  <small>
                    {relativeDay(idea.createdDate, today)}
                    {idea.tags.length > 0 && ` · ${idea.tags.map((tag) => `#${tag}`).join(" ")}`}
                  </small>
                </li>
              ))}
            </ul>
          )}
        </section>
      </aside>
    </div>
  );
}
