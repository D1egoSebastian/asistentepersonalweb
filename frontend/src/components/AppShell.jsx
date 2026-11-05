import { useState } from "react";
import { useHoy } from "../hooks/useHoy.js";
import HabitsPage from "../pages/HabitsPage.jsx";
import HoyPage from "../pages/HoyPage.jsx";
import IdeasPage from "../pages/IdeasPage.jsx";
import MetricsPage from "../pages/MetricsPage.jsx";

const DESTINATIONS = [
  { id: "hoy", label: "Hoy" },
  { id: "habits", label: "Hábitos" },
  { id: "ideas", label: "Ideas" },
  { id: "metrics", label: "Métricas" },
];

// The signed-in shell: the side rail (a bottom bar on narrow screens) and the open screen (spec 006, RF-1).
// It loads "Hoy" data only after sign-in, so the sign-in screens never call the API.
export default function AppShell({ onLogout }) {
  const [tab, setTab] = useState("hoy");
  const hoy = useHoy();

  // Opening "Hoy" reloads its data, because the other screens may have changed habits or ideas.
  function openTab(id) {
    setTab(id);
    if (id === "hoy") {
      hoy.reload();
    }
  }

  // Counts shown next to each destination. "Métricas" shows none in this version (spec 006, section 9).
  const counts = {
    hoy: hoy.ready ? `${hoy.progress.done}/${hoy.progress.total}` : "",
    habits: hoy.ready ? String(hoy.items.length) : "",
    ideas: hoy.ready ? String(hoy.ideaCount) : "",
    metrics: "",
  };

  return (
    <div className="shell">
      <aside className="rail">
        <p className="rail-brand">Asistente</p>
        <nav className="rail-nav" aria-label="Secciones">
          {DESTINATIONS.map((destination) => (
            <button
              key={destination.id}
              type="button"
              className="rail-link"
              aria-current={tab === destination.id ? "page" : undefined}
              onClick={() => openTab(destination.id)}
            >
              <span>{destination.label}</span>
              {counts[destination.id] !== "" && <span className="rail-count">{counts[destination.id]}</span>}
            </button>
          ))}
        </nav>
        <button type="button" className="rail-logout" onClick={onLogout}>
          Salir
        </button>
        <p className="rail-footnote">Resumen por correo: 08:00</p>
      </aside>

      <div className="shell-content">
        {tab === "hoy" && <HoyPage hoy={hoy} />}
        {tab === "habits" && <HabitsPage />}
        {tab === "ideas" && <IdeasPage />}
        {tab === "metrics" && <MetricsPage />}
      </div>
    </div>
  );
}
