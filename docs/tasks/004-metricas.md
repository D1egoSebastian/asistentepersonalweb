# Tareas 004 — Métricas de hábitos

Fuentes: [spec](../specs/004-metricas.md) · [plan](../plans/004-metricas.md) · [constitución](../constitution.md)

**Reglas para todas las tareas:**
- Cada tarea dura como máximo 20-30 minutos. Si se pasa, se divide antes de seguir.
- **Las tareas de lógica se hacen con TDD estricto:** primero el test (falla, rojo), después el código (pasa, verde). Los tests usan solo `node:test` y `node:assert/strict`, con `today` fijo (viernes `2026-10-16`, igual que `streak.test.js`), y se corren con `npm test` (`node --test`) desde `frontend/`.
- Ninguna tarea se marca como hecha si `dotnet test` (desde `backend/`) o `npm test` (desde `frontend/`) fallan (principio 4).
- Si una tarea cambia un comportamiento, la spec se actualiza en el mismo commit (principio 2).
- Ninguna función supera 30 líneas de lógica; en los componentes React el marcado JSX no cuenta, pero sí el estado, los efectos y los manejadores (principio 7).
- Las verificaciones con datos usan `asistente_test`, no `asistente_dev`.
- La entrega va en 4 partes (plan §8): **Parte 1** lógica pura y tests (Fases 1 a 5), **Parte 2** pestaña sin gráfico (Fases 6 y 7), **Parte 3** Chart.js (Fase 8), **Parte 4** cierre (Fase 9).
- **Orden obligatorio (CA-7.8):** la entrada de `docs/decisions.md` para `chart.js@4.5.1` (T32) va antes de cualquier tarea que instale o importe Chart.js (T34 y T36). Ningún archivo de las Partes 1 y 2 importa Chart.js.

---

## Fase 0 — Preparación

- [x] **T00 — Confirmar spec y plan aprobados** · RF: —
  Hecho cuando: la sección 11 de la spec no tiene `[NECESITA ACLARACIÓN]` (D-1 a D-11 resueltas) y el plan §10 no deja dudas que bloqueen.

## Fase 1 — Parte 1: base compartida (`dates.js`, `labels.js`)

- [x] **T01 — `daysBetween` (test primero)** · RF: base de RF-2, RF-8 · Plan: D-T2, §4.1
  Hecho cuando: en `src/logic/dates.test.js` pasan los tests (vistos fallar antes) de días entre dos fechas cruzando mes, año y 29 de febrero, y de resultado negativo si `to` es anterior; `daysBetween(from, to)` está en `src/logic/dates.js`.

- [x] **T02 — Mover `formatDate` a `dates.js` (test primero)** · RF: CA-7.5, RNF-4 · Plan: D-T16
  Hecho cuando: el test de `formatDate` ("27 de octubre de 2026") falla en `dates.test.js`; después `formatDate` vive en `src/logic/dates.js` con un único `Intl.DateTimeFormat("es", …, timeZone: "UTC")` creado una vez; `IdeaCard.jsx` la importa de ahí y ya no tiene su copia; el texto de la fecha en la tarjeta de ideas no cambia; y **todos los tests existentes pasan sin modificarlos**.

- [x] **T03 — Mover etiquetas a `labels.js` (test primero)** · RF: CA-3.3, CA-6.6 · Plan: D-T16
  Hecho cuando: `src/logic/labels.test.js` falla primero y luego pasa con "día/días", "semana/semanas", "1 vez por semana" y "3 veces por semana"; `goalLabel` y `unitLabel(count, isDaily)` viven en `src/logic/labels.js`; `habitView.js` las importa; y `habitView.test.js` y el resto de los tests existentes pasan **sin modificar ninguna línea**.

## Fase 2 — Parte 1: rachas (`streak.js`)

- [x] **T04 — `weeklyStreakFromCounts` (test primero)** · RF: CA-6.1, CA-9.3, RNF-3 · Plan: D-T3, D-T4
  Hecho cuando: pasa un test nuevo en `streak.test.js` que verifica que `weeklyStreakFromCounts(counts, …)` da lo mismo que `weeklyStreak` en varios historiales; `weeklyStreak` queda como envoltorio de una línea con su firma actual; y los tests existentes de `streak.test.js`, `status.test.js` y `habitView.test.js` pasan sin modificarlos.

- [x] **T05 — `longestRun` y `bestDailyStreak` (test primero)** · RF: RF-6 (CA-6.2, CA-6.3, CA-6.4, CA-6.5) · Plan: D-T3
  Hecho cuando: pasan en `streak.test.js` los tests de sin registros → 0, racha vieja más larga que la actual, hoy sin marcar que no corta, hoy marcado que suma, nunca menor que `dailyStreak` (varios historiales) y racha de más de un año; `longestRun(keys, step)` es privado.

- [x] **T06 — `bestWeeklyStreak` (test primero)** · RF: RF-6 (CA-6.2, CA-6.3, CA-6.4, CA-6.5), CA-3.5 · Plan: D-T3
  Hecho cuando: pasan los tests de sin registros → 0, racha vieja más larga que la actual, semana actual cumplida que suma, semana actual sin cumplir que no corta, "4 de 3" que cuenta una semana, y nunca menor que `weeklyStreak`.

## Fase 3 — Parte 1: porcentajes (`completion.js`)

- [x] **T07 — `firstPeriod` y `completionIn` diario (test primero)** · RF: RF-2, RF-3 (CA-2.2, CA-2.4, CA-2.5, CA-3.1) · Plan: D-T2
  Hecho cuando: en `src/logic/completion.test.js` pasan los tests de `firstPeriod` (fecha de creación en diarios, lunes de su semana en semanales), de rango recortado a la creación (ejemplo de CA-2.4: 11 días), de rango anterior a la creación → `{ met: 0, total: 0 }` y de rango completo con registros.

- [x] **T08 — `completionIn` semanal (test primero)** · RF: RF-2, RF-3 (CA-2.4, CA-3.2, CA-3.5) · Plan: D-T2, D-T4
  Hecho cuando: pasan los tests del ejemplo de CA-2.4 (2 semanas), de semana de creación con la meta completa (fallada si la meta es mayor que 1, también creada un domingo), de semana con más registros que la meta (cuenta 1) y de rango sin semanas evaluables.

- [x] **T09 — `habitWindows` (test primero)** · RF: RF-2, RF-5 (CA-2.1, CA-2.3, CA-2.6, CA-5.3) · Plan: D-T2
  Hecho cuando: pasan los tests de las dos ventanas en diarios y semanales, creado hoy → `total` 0 en las dos, creado el lunes con hoy miércoles → 2 días desde la creación y `total` 0 en 4 semanas, hoy lunes, y marcar hoy (o la semana actual) sin cambiar ninguna ventana.

- [x] **T10 — `percentOf` y `overallPercent` (test primero)** · RF: RF-4, RF-3 (CA-2.5, CA-3.4, CA-4.1 a CA-4.5) · Plan: D-T5
  Hecho cuando: pasan los tests de `percentOf` con `total` 0 → `null`, 133 de 200 → 66,5 exacto, y de `overallPercent` que excluye los `null`, da `null` si todos lo son, da el mismo peso a cada hábito y promedia exactos sin redondear antes.

- [x] **T11 — Consistencia con estado y racha (test primero)** · RF: RF-9 (CA-9.1, CA-9.2, CA-9.3) · Plan: D-T2, D-T3
  Hecho cuando: pasan en `completion.test.js` los tests de que, si `habitStatus` da rojo, el período anterior cuenta como fallado en "desde la creación" (diario y semanal), y de que con racha actual de n períodos cerrados `met` de "desde la creación" es al menos n.

## Fase 4 — Parte 1: tendencia e historial (`trend.js`, `history.js`)

- [x] **T12 — `closedWeeks` (test primero)** · RF: RF-7 (CA-7.1) · Plan: D-T2
  Hecho cuando: en `src/logic/trend.test.js` pasan los tests de que devuelve 12 lunes cerrados del más antiguo al más nuevo, de que excluye la semana actual y de que con hoy lunes el último es el lunes de la semana anterior.

- [x] **T13 — `weekCompletion` y `trend` (test primero)** · RF: RF-7 (CA-7.1 a CA-7.4, CA-7.9), RF-4 · Plan: D-T2, D-T5
  Hecho cuando: pasan los tests de diario parcial en su semana de creación, semanal 0 % o 100 %, hábito creado después de una semana que no cuenta en ella, semana sin hábitos → `null` (no 0), y promedio simple entre hábitos.

- [x] **T14 — `dayState` (test primero)** · RF: RF-8 (CA-8.4, CA-8.7) · Plan: D-T13
  Hecho cuando: en `src/logic/history.test.js` pasan los tests de `done`, `missed` (diario), `empty` (semanal), `pending` (hoy sin registro) y `out` (antes de la creación y después de hoy).

- [x] **T15 — `weekSummary` (test primero)** · RF: RF-8 (CA-8.5) · Plan: D-T15
  Hecho cuando: pasan los tests del resumen "2 de 3" con estados `met`, `missed` (semana cerrada sin meta) y `current` (semana en curso sin meta), y de la semana en curso ya cumplida → `met`.

- [x] **T16 — `historyWeeks` y `hasMoreHistory` (test primero)** · RF: RF-8 (CA-8.1 a CA-8.3, CA-8.7) · Plan: D-T13, D-T14
  Hecho cuando: pasan los tests de 12 semanas contando la actual, hábito con menos de 12 semanas que empieza en la semana de creación y `hasMoreHistory` falso, `showAll` que empieza en la semana de creación, y los días `done` de la grilla completa que son exactamente `logDates`.

## Fase 5 — Parte 1: modelo de vista (`metricsView.js`)

- [x] **T17 — `toMetricsView`: vacío, resumen y orden (test primero)** · RF: RF-1, RF-4 (CA-1.2, CA-1.6, CA-4.4, CA-4.5), RNF-5 · Plan: D-T6
  Hecho cuando: en `src/logic/metricsView.test.js` pasan los tests de lista vacía → `isEmpty`, textos "72 %" y "sin datos" del general en las dos ventanas, general calculado con exactos (puede diferir en 1 del promedio mostrado), y hábitos en el orden recibido de la API.

- [x] **T18 — `toMetricsView`: tarjeta por hábito (test primero)** · RF: RF-3, RF-5, RF-6 (CA-3.3, CA-3.4, CA-5.1, CA-5.2, CA-6.1, CA-6.6), RNF-5 · Plan: D-T6
  Hecho cuando: pasan los tests de los textos "67 %", "sin datos", "20 de 30 días", "5 de 8 semanas", redondeo de 66,5 a 67 solo en el texto, "Hoy: hecho", "Hoy: pendiente", "Esta semana: 2 de 3" (y "4 de 3"), y rachas con unidad ("5 días", "2 semanas") con mejor racha nunca menor que la actual.

- [x] **T19 — `toMetricsView`: tendencia e historial (test primero)** · RF: RF-7, RF-8, RF-9 (CA-7.5, CA-7.7, CA-8.2, CA-9.4), RNF-4 · Plan: D-T6, D-T14
  Hecho cuando: pasan los tests de que la tabla y el gráfico leen los mismos valores ya redondeados, `hasTrend` falso si las 12 semanas son `null`, un hábito eliminado que no aparece y cambia el general y la tendencia, solo los hábitos de `expandedIds` con la grilla completa, y cada celda con fecha y estado como texto (`label`).

- [x] **T20 — Cierre de la Parte 1** · RF: RNF-2, RNF-6
  Hecho cuando: `npm test`, `npm run build` y `dotnet test` pasan; `habitView.test.js`, `streak.test.js` (casos previos) y `status.test.js` siguen sin modificar; cada función de §4.1 del plan tiene su test; ninguna función supera 30 líneas; y `git diff --stat backend/` está vacío.

## Fase 6 — Parte 2: hook, página y componentes (sin gráfico)

- [x] **T21 — `useMetrics`** · RF: RF-1 (CA-1.3, CA-1.4), CA-9.5 · Plan: D-T7, D-T14
  Hecho cuando: `src/hooks/useMetrics.js` carga `listHabits` una vez al montar con estados `loading`, `error` y `ready`, expone `retry`, `expandedIds`, `toggleHistory(id)` y una `view` memorizada con `useMemo`; en un error no deja cifras; ninguna función supera 30 líneas; y `npm run build` pasa.

- [x] **T22 — `MetricsPage` y `MetricsSummary`** · RF: RF-1, RF-4 (CA-1.2, CA-1.4, CA-4.4, CA-7.7), RNF-5 · Plan: §3.3, §5
  Hecho cuando: `src/pages/MetricsPage.jsx` solo tiene marcado y muestra "Cargando métricas…", el error con "Reintentar" y sin cifras, "Todavía no tienes hábitos. Crea el primero en la pestaña Hábitos." y el contenido; `MetricsSummary.jsx` muestra "Cumplimiento general" con las dos ventanas; y `npm run build` pasa.

- [x] **T23 — `TrendTable`** · RF: RF-7 (CA-7.5, CA-7.7), RNF-4 · Plan: D-T12
  Hecho cuando: `src/components/TrendTable.jsx` muestra las 12 semanas ("Semana del 12 de octubre de 2026 — 67 %" o "sin datos") con los valores de `view.trend`, o "Sin datos todavía." si `hasTrend` es falso; y `npm run build` pasa.

- [x] **T24 — `HabitMetricsCard`** · RF: RF-3, RF-5, RF-6 (CA-3.3, CA-5.1, CA-5.2, CA-6.1, CA-6.6) · Plan: §5
  Hecho cuando: `src/components/HabitMetricsCard.jsx` muestra nombre, frecuencia, las dos ventanas con su base, el período en curso, racha actual y mejor racha, todo tomado de `view` sin cálculos dentro del componente; y `npm run build` pasa.

- [x] **T25 — `HistoryGrid`** · RF: RF-8 (CA-8.1, CA-8.3, CA-8.4, CA-8.5, CA-8.6), RNF-4 · Plan: D-T13, D-T15
  Hecho cuando: `src/components/HistoryGrid.jsx` es una `<table>` de semanas (de la más antigua a la más nueva) con columnas lunes a domingo, cada celda con símbolo visible y texto oculto con fecha y estado, el resumen semanal ("2 de 3 · Cumplida"), y el botón "Ver todo el historial" / "Ver las últimas 12 semanas" solo si `hasMore`; sin ningún botón para marcar días; y `npm run build` pasa.

- [x] **T26 — `metrics.css` e importación** · RF: RNF-4, CA-7.6 · Plan: D-T13, D-T11
  Hecho cuando: `src/styles/metrics.css` define tarjetas, grilla, estados de celda, `.visually-hidden` y las variables de color del gráfico (`--color-chart-line`, `--color-chart-grid`) en modo claro y oscuro con el mismo selector que `habits.css`; `src/main.jsx` lo importa; y `npm run build` pasa.

- [x] **T27 — Tercera pestaña en `App.jsx`** · RF: RF-1 (CA-1.1, CA-1.3), CA-9.5 · Plan: D-T8
  Hecho cuando: con sesión se ven "Hábitos", "Ideas" y "Métricas" junto a "Salir"; el renderizado condicional desmonta la página al cambiar de pestaña; y `npm test` y `npm run build` pasan.

## Fase 7 — Parte 2: verificaciones en el navegador (sin gráfico)

- [x] **T28 — Pestaña, vacío, error y sesión** · RF: RF-1 (CA-1.1, CA-1.2, CA-1.4, CA-1.5), RNF-1, RNF-5
  Hecho cuando: en el navegador contra `asistente_test`, la pestaña "Métricas" abre y se vuelve a "Hábitos" sin problemas; sin hábitos se ve el mensaje de invitación y ninguna cifra; con la red cortada se ve el error con "Reintentar" y ninguna cifra, y al reintentar con red aparece el contenido; con token vencido se vuelve al inicio con "Tu sesión expiró."; y todos los textos están en español.

- [x] **T29 — Período en curso, eliminación y recarga** · RF: RF-5, RF-9 (CA-1.3, CA-5.4, CA-9.4, CA-9.5)
  Hecho cuando: en el navegador, marcar hoy en "Hábitos" y volver a "Métricas" cambia solo el período en curso, la racha y el historial (los porcentajes no); marcar el domingo un lunes recalcula la semana anterior al reabrir; eliminar un hábito y volver lo quita de las métricas y recalcula el general; y con la pestaña abierta "hoy" no cambia hasta reabrirla.
  Nota (28-10-2026): verificado en el navegador salvo "marcar el domingo un lunes", que no se puede reproducir un miércoles sin cambiar el reloj; lo cubre el test de CA-9.1 en `completion.test.js`. Que "hoy" no cambie con la pestaña abierta se cumple por diseño: `today` viene de la respuesta cargada al abrir (D-T8).

- [x] **T30 — Grilla, solo lectura y accesibilidad** · RF: RF-8 (CA-8.2, CA-8.3, CA-8.4, CA-8.6), RNF-4
  Hecho cuando: en el navegador, un hábito con más de 12 semanas muestra 12 y ofrece "Ver todo el historial" y volver a 12, uno con menos de 12 no ofrece el botón; ninguna celda es clicable, tampoco hoy ni ayer; con un lector de pantalla (o inspeccionando el texto oculto) cada celda dice fecha y estado, y la tabla de tendencia se lee completa; y en 375 px y en escritorio, en modo claro y oscuro, no hay desbordamiento horizontal.

- [x] **T31 — Cierre de la Parte 2** · RF: RF-1, RF-3 a RF-9
  Hecho cuando: `npm test`, `npm run build` y `dotnet test` pasan, T21 a T30 están hechas, y `grep` no encuentra ninguna importación de `chart.js` en `frontend/src`.

## Fase 8 — Parte 3: Chart.js

- [x] **T32 — Justificar Chart.js en `docs/decisions.md`** · RF: CA-7.8, RNF-7 · Plan: D-T9, §9 · **Va antes de T33 a T36**
  Hecho cuando: `docs/decisions.md` tiene la entrada de **`chart.js@4.5.1`** (versión exacta, sin `^`) con qué problema resuelve (gráfico de líneas con huecos, ejes y escalado en `canvas`), por qué no alcanza lo que hay (CSS plano no dibuja una línea con huecos y ejes sin escribir un motor de gráficos a mano; el dueño eligió Chart.js en D-5), la licencia MIT y la compatibilidad con Vite 8 confirmadas, el motivo de no usar `react-chartjs-2` ni `chart.js/auto`, y la decisión de fijar 4.5.1 en lugar de la 4.6.1 (demasiado reciente); y la entrada está guardada antes de instalar o importar nada.

- [x] **T33 — `trendChartConfig` (test primero)** · RF: RF-7 (CA-7.1, CA-7.4, CA-7.6) · Plan: D-T9
  Hecho cuando: en `src/logic/trendChart.test.js` pasan los tests de 12 puntos, `null` conservado con `spanGaps` falso, eje Y de 0 a 100, animación desactivada y colores recibidos aplicados; `trendChartConfig(points, colors)` está en `src/logic/trendChart.js` **sin importar Chart.js**.

- [x] **T34 — Instalar `chart.js` con versión exacta** · RF: RF-7, RNF-7 · Plan: D-T9
  Hecho cuando: T32 está hecha; `npm view chart.js version` se consultó y se instaló con `npm install --save-exact chart.js@4.5.1` desde `frontend/`; `package.json` muestra `"chart.js": "4.5.1"` sin `^`; es la única dependencia nueva; y `npm test` y `npm run build` pasan.

- [x] **T35 — `useColorScheme` y colores del gráfico** · RF: CA-7.6 · Plan: D-T11
  Hecho cuando: `src/hooks/useColorScheme.js` devuelve `"light"` o `"dark"` desde `matchMedia("(prefers-color-scheme: dark)")` y escucha los cambios (y limpia el listener al desmontar), y `metrics.css` tiene las tres variables de color en los dos modos; y `npm run build` pasa.

- [x] **T36 — `TrendChart` y montaje en `MetricsPage`** · RF: RF-7 (CA-7.1, CA-7.4, CA-7.6, CA-7.7), RNF-3, RNF-4 · Plan: D-T9, D-T10, D-T12
  Hecho cuando: `src/components/TrendChart.jsx` registra solo `LineController`, `LineElement`, `PointElement`, `LinearScale`, `CategoryScale` y `Tooltip`; importa Chart.js de forma estática; crea el gráfico en un `canvas` con `role="img"` y `aria-label` que remite a la tabla; lo destruye y recrea al desmontar o al cambiar datos o modo; `MetricsPage` lo muestra sobre `TrendTable` solo si `hasTrend`; y `npm test` y `npm run build` pasan.

- [x] **T37 — Verificar el gráfico en el navegador** · RF: RF-7 (CA-7.1, CA-7.4, CA-7.6, CA-7.7, CA-7.9), RNF-4
  Hecho cuando: en el navegador, el gráfico muestra 12 puntos que coinciden con la tabla; con una semana sin hábitos la línea se interrumpe y no cae a 0 %; con todas las semanas sin datos se ve "Sin datos todavía." y no el gráfico; con solo hábitos semanales la línea es escalonada; y al cambiar del modo claro al oscuro con la pestaña abierta (cambio del sistema o de las DevTools) los colores cambian y siguen siendo legibles.
  Nota (28-10-2026): en el navegador aparecieron dos fallos a 375 px y se corrigieron. (1) El canvas estiraba la página a 734 px: `.metrics-page` ahora usa `grid-template-columns: minmax(0, 1fr)`. (2) Las fechas largas del eje X aplastaban el gráfico: el eje usa `formatShortDate` ("3 ago"), el tooltip conserva la fecha completa y el contenedor tiene 220 px de alto con `maintainAspectRatio: false`. Las dos cosas tienen test en `dates.test.js` y `trendChart.test.js`.

- [x] **T38 — Cierre de la Parte 3** · RF: RF-7, RNF-7
  Hecho cuando: `npm test`, `npm run build` y `dotnet test` pasan, y el historial de commits muestra la entrada de `decisions.md` (T32) antes del que instala o importa Chart.js.
  Estado (28-10-2026): `npm test` (270), `npm run build` y `dotnet test` (163) pasan, y `chart.js` solo se importa en `TrendChart.jsx`. El commit de la entrada de `decisions.md` ("docs: justify chart.js 4.5.1 in decisions") va antes del que instala e importa Chart.js.

## Fase 9 — Parte 4: cierre

- [ ] **T39 — Medir RNF-3** · RF: RNF-3 · Plan: §6.3, D-T4, D-T10, D-T14
  Hecho cuando: con 50 hábitos (25 diarios y 25 semanales con meta 3) y un registro por día durante 5 años (unos 91.300), generados con `generate_series` en `asistente_test`, y con `vite build` + `vite preview`, la pestaña "Métricas" se ve en menos de 1 segundo en tres corridas (desde el clic hasta ver tarjetas y gráfico); el peor tiempo queda anotado en el plan; y el tiempo de `toMetricsView` se midió aparte con un `performance.now()` temporal que no se versiona. Si no cumple, se perfila primero el cálculo y cualquier cambio de servidor espera el visto bueno del dueño.

- [ ] **T40 — Actualizar `CLAUDE.md` y `AGENTS.md`** · RF: RNF-6 · Plan: §3.1
  Hecho cuando: ambos (iguales salvo la cabecera) quitan `GET /api/metrics/streaks`, `GET /api/metrics/completion` y `GET /api/dashboard` de los endpoints, cierran "Chart.js vs. Recharts" en "Próximas decisiones" y en el stack (queda Chart.js 4.5.1), mencionan la tercera pestaña "Métricas" y los módulos nuevos de `src/logic/`, y `diff CLAUDE.md AGENTS.md` solo muestra las diferencias de nombre de agente.

- [ ] **T41 — Casos límite y spec al día** · RF: RF-1 a RF-9
  Hecho cuando: cada caso límite de la sección 8 de la spec tiene su verificación (test automático si es una regla, tarea del navegador T28 a T37 si es de interfaz), y la spec refleja cualquier cambio de comportamiento hecho durante la implementación, en el mismo commit que el cambio.

- [ ] **T42 — Verificación final** · RF: todos
  Hecho cuando: `dotnet test` y `npm test` pasan; cada fila de la matriz del plan (§7) tiene su verificación; `git diff --stat backend/` está vacío y no hay endpoints ni migraciones nuevas (RNF-1, RNF-6); `chart.js` es la única dependencia nueva (RNF-7); y los textos visibles están en español neutro (RNF-5).

- [ ] **T43 — Recorrido del dueño** · RF: RF-1 a RF-9
  Hecho cuando: el dueño, sin ayuda y en `asistente_dev`, abre "Métricas", lee el porcentaje de cada hábito y el general en las dos ventanas, entiende el gráfico y su tabla, ve la mejor racha de cada hábito, recorre su historial (con "Ver todo el historial") y confirma los dos detalles de interfaz del plan §10: la grilla de la semana más antigua a la más nueva y la semana en curso ya cumplida como "Cumplida".

---

## Entrega

El repositorio no tiene remoto ni PRs: el trabajo se integra con commits directos en `master`, solo cuando el dueño lo pide. Como el cambio suma entre 2.300 y 2.800 líneas (código y tests, sin `package-lock.json`), se entrega en siete commits (el 5 se parte en dos) que se revisan uno a uno. Cada commit deja `npm test`, `npm run build` y `dotnet test` en verde, y se puede revertir solo.

| Commit | Tareas | Líneas estimadas | Notas |
|---|---|---|---|
| 1 | Base compartida y rachas (T01 a T06) | Unas 350 | Los tests existentes pasan sin modificarse. |
| 2 | Porcentajes, tendencia e historial (T07 a T16) | Unas 500 | Si cuesta revisarlo, se separan la tendencia y el historial. |
| 3 | Modelo de vista y cierre de la Parte 1 (T17 a T20) | Unas 450 | |
| 4 | Pestaña sin gráfico y verificaciones (T21 a T31) | Unas 650 | Casi todo marcado y CSS. |
| 5 | Chart.js (T32 a T38) | Unas 300, más unas 40 en `package-lock.json` | Dos commits: primero T32 solo (entrada de `decisions.md`), después T33 a T38. Así el historial muestra la justificación antes de instalar o importar Chart.js (CA-7.8). |
| 6 | Cierre (T39 a T43) | Unas 60 | |
