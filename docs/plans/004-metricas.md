# Plan 004 — Métricas de hábitos

Plan técnico de [`docs/specs/004-metricas.md`](../specs/004-metricas.md). Respeta [`docs/constitution.md`](../constitution.md). Cada parte indica entre corchetes los RF que cubre.

## 1. Arquitectura en una frase

El backend **no cambia**: `GET /api/habits` ya envía `today`, `createdOn`, `frequency`, `weeklyGoal` y `logDates` ordenadas de cada hábito (RNF-6). El frontend **calcula y pinta**: porcentajes, rachas, tendencia e historial salen de funciones puras de `src/logic/` que reciben `today` como parámetro (plan 001, D-T1), y la pantalla solo muestra el modelo que arman.

```
[React] App (pestaña "Métricas") ─ MetricsPage ─ useMetrics ─ habitsApi.listHabits ─ http.js (sesión, 401)
                                        │                │
                                        │                └─ toMetricsView(habits, today, expandedIds)   (puro)
                                        │                     ├─ completion.js  (ventanas, %, general)
                                        │                     ├─ trend.js       (12 semanas cerradas)
                                        │                     ├─ history.js     (grilla día por día)
                                        │                     └─ streak.js      (racha actual y mejor racha)
                                        ▼
        MetricsSummary · TrendChart (Chart.js) + TrendTable · HabitMetricsCard ─ HistoryGrid   (presentacionales)
```

## 2. Decisiones técnicas

| ID | Decisión | Por qué | Alternativa descartada |
|---|---|---|---|
| D-T1 | Todo se calcula en el **frontend**, en `src/logic/`, con funciones puras que reciben `today`. Sin endpoints nuevos ni cambios en la base. | Lo fija la spec (RNF-6, D-7) y sigue el plan 001 (D-T1, D-T10): el historial completo ya llega en `GET /api/habits`. Las reglas se testean con `node --test` y `today` fijo (RNF-2). | `/api/metrics/*` en el servidor: fuera de alcance (spec §9) y duplicaría en C# las reglas de semana que ya viven en `streak.js`. |
| D-T2 | **Una sola función de conteo por rango**, `completionIn(habit, counts, from, to)`, para las dos ventanas y para cada semana de la tendencia. Recorta `from` al primer período del hábito (fecha de creación o semana de creación). | La regla de existencia del hábito y de semana de creación queda en un solo lugar, así que porcentajes y tendencia no pueden contradecirse (CA-9.3). El ejemplo de CA-2.4 sale solo del recorte. | Una función por ventana y otra para la tendencia: tres copias de la misma regla de creación. |
| D-T3 | Se **reutilizan** las reglas de la spec 001: `weekStart`, `countByWeek`, `isWeekMet`, `dailyStreak`, `weeklyStreak` y `weekProgress`. La mejor racha se agrega a `streak.js` con un único ayudante `longestRun(keys, step)` (paso 1 para días, 7 para semanas). | La racha actual de "Métricas" es literalmente la misma función que la de "Hábitos" (CA-6.1), y la mejor racha usa el mismo criterio de semana cumplida (CA-6.2, CA-9.3). | Un módulo nuevo de rachas: duplica las reglas y abre la puerta a que las dos pestañas difieran. |
| D-T4 | `countByWeek` se calcula **una sola vez por hábito y por carga**, y se pasa a las funciones semanales. `weeklyStreak` se parte en `weeklyStreakFromCounts(counts, …)` y un envoltorio de una línea con la firma actual. | Cada `weekStart` crea fechas; con 50 hábitos semanales y 5 años de registros, recalcular el mapa en cada ventana y en cada semana de la tendencia serían unas 16 pasadas de 1.826 fechas por hábito (RNF-3). | Llamar a `countByWeek` dentro de cada función: más simple de leer, pero multiplica el costo por 16. |
| D-T5 | Un porcentaje es un **número exacto** `met * 100 / total`, o `null` si `total` es 0 ("sin datos"). Se redondea con `Math.round` **solo al armar el texto**. El general promedia los exactos y descarta los `null`. | Cumple CA-2.5, CA-3.4 y CA-4.1 a CA-4.5. Multiplicar antes de dividir hace exactos los casos ",5" por hábito (133 de 200 da 66,5 y se muestra 67). | Redondear cada hábito antes de promediar: contradice CA-4.5. Usar 0 para "sin datos": contradice CA-2.5. |
| D-T6 | Un **modelo de vista** `toMetricsView(habits, today, expandedIds)` arma todos los textos: porcentajes, bases ("20 de 30 días"), "sin datos", período en curso, rachas con unidad, estados de la grilla y puntos de la tendencia. | Igual que `toHabitView` en hábitos: los componentes no hacen cuentas (principio 3) y los textos se testean. El gráfico y la tabla leen los **mismos puntos ya redondeados**, así que no pueden diferir (CA-7.5). | Calcular textos en los componentes: lógica sin tests y repartida en JSX. |
| D-T7 | Hook propio **`useMetrics`**: carga una vez al montar, con estado `loading`, `error` o `ready`, y `retry`. No reutiliza `useHabitList`. | CA-1.4 exige reintentar y no mostrar cifras parciales. `useHabitList` no tiene reintento y conserva la lista anterior tras un error; cambiarlo para limpiar el error en cada carga borraría los mensajes que `run` muestra en "Hábitos". | Modificar `useHabitList`: toca una pantalla que ya funciona y cambia su comportamiento de errores. |
| D-T8 | **Tercera pestaña en `App.jsx`**, con renderizado condicional que **desmonta** la página al cambiar de pestaña. | Al abrir "Métricas" siempre se pide la lista nueva (CA-1.3, CA-9.4) y `today` queda fijo mientras la pestaña está abierta (CA-9.5). Sigue la navegación sin librería del plan 003 (D-T7). | Compartir la lista entre pestañas o refrescar con un temporizador: más estado y `today` podría cambiar a mitad de la visualización. |
| D-T9 | **Chart.js usado directamente**, sin envoltorio para React, con **registro selectivo**: `LineController`, `LineElement`, `PointElement`, `LinearScale`, `CategoryScale` y `Tooltip`. Versión **exacta** (`--save-exact`), sin animación. La configuración la arma una función pura, `trendChartConfig(points, colors)`. | RNF-7 permite una sola biblioteca nueva; el registro selectivo deja fuera del paquete lo que no se usa. Sin animación, el gráfico se ve en el primer cuadro (RNF-3) y no hay movimiento innecesario. La función pura se testea sin importar Chart.js. | `react-chartjs-2`: segunda dependencia (RNF-7). `chart.js/auto`: registra todos los tipos de gráfico y agranda el paquete. |
| D-T10 | Chart.js se importa **de forma estática**, sin carga diferida. | RNF-3 no cuenta la primera carga de la aplicación, pero sí el momento de abrir la pestaña: un bloque cargado al abrir "Métricas" sumaría una descarga a ese segundo. | `React.lazy` para la página: aligera la primera carga, que no se mide, a costa de la que sí se mide. |
| D-T11 | Los **colores del gráfico** salen de variables CSS (`--color-chart-line`, `--color-chart-grid`, `--color-text`) leídas con `getComputedStyle` al crear el gráfico. Un hook `useColorScheme` escucha `matchMedia("(prefers-color-scheme: dark)")` y el gráfico se vuelve a crear cuando cambia. | Los colores de modo claro y oscuro viven en un solo lugar, el CSS, junto a los de hábitos (CA-7.6). | Colores escritos en JavaScript: dos lugares que mantener iguales. |
| D-T12 | **Alternativa en texto siempre visible**: `TrendTable` (12 filas: semana por la fecha de su lunes y porcentaje o "sin datos") debajo del gráfico. El `canvas` lleva `role="img"` y una `aria-label` que remite a la tabla. | Un lector de pantalla no lee un `canvas` (RNF-4, CA-7.5). Doce filas no justifican esconderlas. | Tabla dentro de `<details>`: un paso más para acceder a los datos. |
| D-T13 | La **grilla** es una `<table>` por hábito: filas = semanas (de la más antigua a la más nueva, como un calendario), columnas = lunes a domingo con encabezado. Cada celda tiene un símbolo visible y un texto oculto visualmente con su fecha y estado. Sin botones: es de solo lectura. | RNF-4 (fecha y estado como texto, no solo color) y CA-8.6 (no se puede marcar desde la grilla). | Celdas solo de color o un mapa de calor en `canvas`: no accesibles y fuera de alcance (spec §9). |
| D-T14 | El **historial completo** se calcula solo para los hábitos expandidos. `useMetrics` guarda `expandedIds` (un `Set`) y `toggleHistory(id)`. | Con 50 hábitos de 5 años, armar todas las grillas completas son unas 91.000 celdas que nadie mira. Por defecto se arman 12 semanas por hábito (CA-8.2, CA-8.3). | Calcular todo al cargar: gasta el presupuesto de RNF-3 en datos ocultos. |
| D-T15 | En la grilla semanal, la **semana en curso** dice "Cumplida" si ya alcanzó la meta y "En curso" si no. Las cerradas dicen "Cumplida" o "No cumplida". | Sigue la prioridad del estado de la spec 001 (verde gana) y CA-6.3 (el período en curso suma si ya está cumplido). | Mostrar siempre "En curso": contradiría la racha, que ya cuenta esa semana. |
| D-T16 | **Se comparten, no se copian**: `formatDate` pasa de `IdeaCard.jsx` a `src/logic/dates.js` (con un único `Intl.DateTimeFormat("es", …, timeZone: "UTC")` creado una vez), y `goalLabel` y la unidad "día/días/semana/semanas" pasan de `habitView.js` a `src/logic/labels.js`. | Las métricas necesitan los mismos textos. Crear el formateador una vez importa: `toLocaleDateString` crea uno por llamada, y la grilla formatea unas 4.200 fechas (RNF-3). | Copiar las funciones: dos lugares para corregir el mismo texto. |

## 3. Archivos

### 3.1 Documentación

| Archivo | Acción | Responsabilidad | Cuándo |
|---|---|---|---|
| `docs/decisions.md` | Modificar | Entrada de **Chart.js** con su versión exacta: qué problema resuelve (gráfico de líneas con huecos, ejes y escalado en `canvas`) y por qué no alcanza lo que ya hay (CSS plano no dibuja una línea con huecos y ejes sin escribir a mano un motor de gráficos; el dueño eligió Chart.js en D-5). | **Antes** de cualquier código que lo importe (CA-7.8). |
| `CLAUDE.md` y `AGENTS.md` | Modificar | Mantenerlos iguales. Quitar `GET /api/metrics/streaks`, `GET /api/metrics/completion` y `GET /api/dashboard` de los endpoints (spec §9); cerrar "Chart.js vs. Recharts" en "Próximas decisiones" y en el stack; mencionar la pestaña "Métricas" y los módulos nuevos de `src/logic/`. | Al cerrar la implementación. |

### 3.2 Backend

Sin cambios (RNF-6). Los tests del backend tienen que seguir pasando.

### 3.3 Frontend (`frontend/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `package.json`, `package-lock.json` | Modificar | `chart.js` con versión exacta, verificada con `npm view chart.js version` en la tarea (última conocida: rama 4.5). | RF-7 |
| `src/logic/dates.js` | Modificar | Agregar `daysBetween(from, to)` y `formatDate(date)` (D-T16). | Base |
| `src/logic/labels.js` | Crear | `goalLabel(goal)` y `unitLabel(count, isDaily)`, movidas de `habitView.js`. | CA-3.3, CA-6.6 |
| `src/logic/habitView.js` | Modificar | Usar `labels.js`. Sin cambios de comportamiento: sus tests pasan igual. | — |
| `src/logic/streak.js` | Modificar | `weeklyStreakFromCounts` (D-T4), `bestDailyStreak`, `bestWeeklyStreak` y el ayudante privado `longestRun`. | RF-6 |
| `src/logic/completion.js` | Crear | `firstPeriod`, `completionIn`, `habitWindows`, `percentOf`, `overallPercent`. | RF-2, RF-3, RF-4 |
| `src/logic/trend.js` | Crear | `closedWeeks`, `weekCompletion`, `trend`. | RF-7 |
| `src/logic/history.js` | Crear | `dayState`, `weekSummary`, `historyWeeks`, `hasMoreHistory`. | RF-8 |
| `src/logic/metricsView.js` | Crear | `toMetricsView` y sus ayudantes de texto (D-T6). | RF-1 a RF-9 |
| `src/logic/trendChart.js` | Crear | `trendChartConfig(points, colors)`: datos y opciones de Chart.js como objeto plano (D-T9). | CA-7.1, CA-7.4 |
| `src/logic/*.test.js` | Crear o ampliar | Sección 6. | Todos |
| `src/hooks/useMetrics.js` | Crear | Carga, estados, reintento, `expandedIds` y `view` memorizada con `useMemo` (D-T7, D-T14). | RF-1, CA-9.5 |
| `src/hooks/useColorScheme.js` | Crear | Devuelve `"light"` o `"dark"` y escucha los cambios (D-T11). | CA-7.6 |
| `src/pages/MetricsPage.jsx` | Crear | Solo marcado: cargando, error con "Reintentar", sin hábitos, o el contenido. | RF-1 |
| `src/components/MetricsSummary.jsx` | Crear | Porcentaje general de las dos ventanas. | RF-4 |
| `src/components/TrendChart.jsx` | Crear | Registra los componentes de Chart.js, crea el gráfico en un `canvas` y lo destruye al desmontar o al cambiar datos o modo. | RF-7 |
| `src/components/TrendTable.jsx` | Crear | La tendencia como tabla (D-T12). | CA-7.5, RNF-4 |
| `src/components/HabitMetricsCard.jsx` | Crear | Nombre, dos ventanas con su base, período en curso, racha actual y mejor racha, y la grilla. | RF-3, RF-5, RF-6 |
| `src/components/HistoryGrid.jsx` | Crear | Tabla de semanas, resumen semanal y botón "Ver todo el historial" / "Ver las últimas 12 semanas". | RF-8 |
| `src/components/IdeaCard.jsx` | Modificar | Importar `formatDate` de `dates.js` y quitar su copia (D-T16). | — |
| `src/App.jsx` | Modificar | Pestaña "Métricas" y renderizado condicional de las tres páginas (D-T8). | CA-1.1 |
| `src/main.jsx` | Modificar | Importar `styles/metrics.css`. | — |
| `src/styles/metrics.css` | Crear | Tarjetas, grilla, estados de celda, `.visually-hidden` y variables de color del gráfico en modo claro y oscuro, con el mismo selector que `habits.css`. | CA-7.6, RNF-4 |

## 4. Reglas y algoritmos

Todas las fechas son textos `yyyy-MM-dd` y se comparan como texto (plan 001, §4). Ninguna función lee el reloj. En las funciones semanales, los períodos son los lunes de cada semana.

### 4.1 Funciones puras

| Archivo | Firma | Devuelve | RF |
|---|---|---|---|
| `dates.js` | `daysBetween(from, to)` | Días de `from` a `to` (negativo si `to` es anterior). | Base |
| `dates.js` | `formatDate(date)` | "27 de octubre de 2026". | CA-7.5, RNF-4 |
| `labels.js` | `unitLabel(count, isDaily)` | "día", "días", "semana" o "semanas". | CA-3.3, CA-6.6 |
| `streak.js` | `bestDailyStreak(logDates)` | Racha más larga de días seguidos con registro. | CA-6.2, CA-6.5 |
| `streak.js` | `bestWeeklyStreak(counts, goal)` | Racha más larga de semanas cumplidas seguidas. | CA-6.2, CA-6.3 |
| `completion.js` | `firstPeriod(habit)` | `createdOn` (diario) o `weekStart(createdOn)` (semanal). | CA-2.2 |
| `completion.js` | `completionIn(habit, counts, from, to)` | `{ met, total }` en el rango, recortado al primer período. | CA-2.4, CA-3.1, CA-3.2, CA-3.5 |
| `completion.js` | `habitWindows(habit, counts, today)` | `{ sinceCreation, lastFourWeeks }`, cada una `{ met, total }`. | CA-2.1 a CA-2.3, CA-5.3 |
| `completion.js` | `percentOf({ met, total })` | `met * 100 / total`, o `null` si `total` es 0. | CA-2.5, CA-3.4 |
| `completion.js` | `overallPercent(percents)` | Promedio de los que no son `null`; `null` si no queda ninguno. | CA-4.1 a CA-4.5 |
| `trend.js` | `closedWeeks(today, count)` | Los lunes de las `count` semanas cerradas, del más antiguo al más nuevo. | CA-7.1 |
| `trend.js` | `trend(indexedHabits, today)` | `[{ monday, percent }]` de las 12 semanas cerradas. | CA-7.1 a CA-7.4 |
| `history.js` | `dayState(date, habit, days, today)` | `"done"`, `"missed"`, `"empty"`, `"pending"` o `"out"`. | CA-8.4, CA-8.7 |
| `history.js` | `weekSummary(monday, counts, goal, today)` | `{ count, goal, state: "met" \| "missed" \| "current" }`. | CA-8.5 |
| `history.js` | `historyWeeks(habit, days, counts, today, showAll)` | Semanas con sus 7 días y, si es semanal, su resumen. | CA-8.1 a CA-8.3 |
| `history.js` | `hasMoreHistory(habit, today)` | `true` si el hábito tiene más de 12 semanas contando la actual. | CA-8.3 |
| `metricsView.js` | `toMetricsView(habits, today, expandedIds)` | El modelo de la sección 4.3. | RF-1 a RF-9 |
| `trendChart.js` | `trendChartConfig(points, colors)` | Objeto `{ type: "line", data, options }` para Chart.js. | CA-7.1, CA-7.4, CA-7.6 |

### 4.2 Pseudocódigo

```
completionIn(habit, counts, from, to):                      # D-T2
    desde = max(from, firstPeriod(habit))                   # el hábito no existía antes
    si desde > to: devolver { met: 0, total: 0 }            # "sin datos" (CA-2.5)
    si diario:
        total = daysBetween(desde, to) + 1
        met = cantidad de logDates con desde <= fecha <= to                    # CA-3.1
    si semanal:
        total = daysBetween(desde, to) / 7 + 1
        met = cantidad de lunes s en counts con desde <= s <= to e isWeekMet(counts, s, goal)
                                                            # CA-3.2; más registros que la meta = 1 semana (CA-3.5)
    devolver { met, total }

habitWindows(habit, counts, today):
    actual = weekStart(today)
    finDesdeCreacion = diario ? addDays(today, -1) : addDays(actual, -7)      # último período cerrado (CA-2.1)
    finCuatro        = diario ? addDays(actual, -1) : addDays(actual, -7)     # último domingo (CA-2.3)
    sinceCreation = completionIn(habit, counts, firstPeriod(habit), finDesdeCreacion)
    lastFourWeeks = completionIn(habit, counts, addDays(actual, -28), finCuatro)

weekCompletion(habit, counts, lunes):                        # CA-7.2
    diario:  completionIn(habit, counts, lunes, addDays(lunes, 6))
    semanal: completionIn(habit, counts, lunes, lunes)      # 100 % o 0 % (CA-7.9)

trend(indexados, today):
    para cada lunes de closedWeeks(today, 12):
        percent = overallPercent(indexados.map(h → percentOf(weekCompletion(h, h.counts, lunes))))
                                                            # null = semana sin datos: hueco en la línea (CA-7.4)

longestRun(keys, step):                                     # keys: Set de fechas; step: 1 o 7
    mejor = 0
    para cada k en keys que no tenga a addDays(k, -step) en keys:   # k empieza una racha
        largo = 1; cursor = addDays(k, step)
        mientras keys tenga cursor: largo += 1; cursor = addDays(cursor, step)
        mejor = max(mejor, largo)
    devolver mejor                                          # 0 sin registros (CA-6.5)

bestDailyStreak(logDates)    = longestRun(Set(logDates), 1)
bestWeeklyStreak(counts, g)  = longestRun(Set(lunes de counts con isWeekMet), 7)
    # Los registros nunca son futuros: hoy o la semana actual suman si están cumplidos y,
    # si no, no aparecen en el conjunto y no cortan nada (CA-6.3). La racha actual es una
    # de esas rachas, así que la mejor nunca es menor (CA-6.4).

dayState(date, habit, days, today):                          # CA-8.4
    si date < habit.createdOn o date > today: "out"
    si days tiene date: "done"                               # CA-8.7: exactamente los registros de la API
    si date == today:   "pending"
    diario: "missed"; semanal: "empty"

historyWeeks(habit, days, counts, today, showAll):
    primera = weekStart(createdOn); actual = weekStart(today)
    inicio = showAll ? primera : max(primera, addDays(actual, -77))         # 12 semanas con la actual (CA-8.2)
    para cada lunes de inicio a actual (paso 7): 7 días con su estado y, si es semanal, weekSummary
```

`completionIn` recorre `logDates` (diario) o las entradas de `counts` (semanal, a lo sumo unas 260). Cada hábito se indexa una vez por carga: `days = Set(logDates)` y, si es semanal, `counts = countByWeek(logDates)` (D-T4).

### 4.3 Modelo de vista

```js
toMetricsView(habits, today, expandedIds) → {
  isEmpty,                                   // CA-1.2
  overall: { sinceCreation: "72 %" | "sin datos", lastFourWeeks: "…" },
  trend: [{ monday, label: "12 de octubre de 2026", value: 67 | null, text: "67 %" | "sin datos" }],
  hasTrend,                                  // false si las 12 semanas son null (CA-7.7)
  habits: [{                                 // en el orden de la API: por creación (CA-1.6)
    id, name, frequencyLabel,
    sinceCreation: { percent: "67 %" | "sin datos", base: "20 de 30 días" | null },
    lastFourWeeks: { percent, base },        // CA-3.3
    currentLabel: "Hoy: hecho" | "Hoy: pendiente" | "Esta semana: 2 de 3",   // CA-5.1, CA-5.2
    streakLabel: "5 días", bestStreakLabel: "9 días",                       // CA-6.1, CA-6.6
    history: { weeks, expanded, hasMore, hasSummary },   // hasSummary: weekly habits draw the "2 de 3 · Cumplida" column; weeks: [{ monday, days: [{ date, state, symbol, label }], summary? }]
  }],
}
```

El período en curso usa `dailyStreak`/`weeklyStreakFromCounts` y `weekProgress` tal como "Hábitos" (CA-5.2, CA-6.1). Ningún porcentaje ni punto de la tendencia incluye hoy ni la semana actual, porque todas las ventanas terminan antes (CA-5.3).

## 5. Cómo se ve en la interfaz

- **Pestañas:** "Hábitos", "Ideas" y "Métricas", junto a "Salir" (CA-1.1).
- **Cargando:** "Cargando métricas…". **Error:** el mensaje de `http.js` en `ErrorBanner` y el botón "Reintentar", sin ninguna cifra (CA-1.4). **401:** `http.js` ya lleva al inicio de sesión con "Tu sesión expiró." (CA-1.5).
- **Sin hábitos:** "Todavía no tienes hábitos. Crea el primero en la pestaña Hábitos." Sin porcentajes ni gráfico (CA-1.2).
- **Resumen:** "Cumplimiento general" con "Desde la creación: 72 %" y "Últimas 4 semanas: sin datos".
- **Tendencia:** título "Últimas 12 semanas", el gráfico (eje Y de 0 % a 100 %, una etiqueta corta por lunes) y debajo la tabla "Semana del … — 67 %". Si no hay ninguna semana con datos, solo "Sin datos todavía." (CA-7.7).
- **Tarjeta por hábito**, en el orden de la API:

```
┌──────────────────────────────────────────────────────────┐
│  Daily coding challenge                       Diario     │
│  Desde la creación: 67 % (20 de 30 días)                 │
│  Últimas 4 semanas: 75 % (21 de 28 días)                 │
│  Hoy: pendiente                                          │
│  Racha actual: 5 días · Mejor racha: 9 días              │
│        L  M  X  J  V  S  D                               │
│  ...   ✓  ✓  ✗  ✓  ✓  ·  …     (12 semanas)              │
│  [Ver todo el historial]                                 │
└──────────────────────────────────────────────────────────┘
Semanal: cada fila termina con "2 de 3 · Cumplida" (o "No cumplida", "En curso").
```

- **Celdas:** símbolo visible (✓ cumplido, ✗ no cumplido, · sin registro, … pendiente, vacío fuera de rango) y color, más un texto oculto visualmente: "12 de octubre de 2026: cumplido" (RNF-4). Sin botones (CA-8.6).

## 6. Estrategia de tests

### 6.1 Frontend con `node --test` (principio 4, RNF-2)

TDD estricto: cada función de la sección 4.1 empieza por su test en rojo. Solo `node:test` y `node:assert/strict`, con `today` fijo. Fecha base: hoy viernes `2026-10-16` (semana del lunes 12), igual que `streak.test.js`.

| Archivo | Casos | Cubre |
|---|---|---|
| `dates.test.js` | `daysBetween` cruzando mes, año y 29 de febrero, y negativo; `formatDate` ("27 de octubre de 2026"). | Base |
| `labels.test.js` | Singular y plural de días y semanas; "1 vez por semana" y "3 veces por semana". | CA-3.3, CA-6.6 |
| `habitView.test.js` | Sin cambios: tiene que seguir pasando tras mover las etiquetas. | — |
| `streak.test.js` | `weeklyStreakFromCounts` igual a `weeklyStreak`. Mejor racha: sin registros → 0; racha vieja más larga que la actual; hoy sin marcar no corta; hoy marcado suma; semana actual cumplida suma; "4 de 3" cuenta una semana; nunca menor que la actual (varios historiales); racha de más de un año. | CA-6.1 a CA-6.5 |
| `completion.test.js` | Diario y semanal en las dos ventanas; ejemplo de CA-2.4 (11 días y 2 semanas); creado hoy → `null` en las dos; creado el lunes con hoy miércoles → 2 días y `null` en 4 semanas; hoy lunes; creado un domingo (diario 1 día; semanal con meta completa, fallada si la meta es mayor que 1); semana con más registros que la meta; marcar hoy no cambia nada; `percentOf` 133 de 200 → 66,5; `overallPercent` excluye `null`, todos `null` → `null`, pesos iguales. **Consistencia:** con el estado rojo de `habitStatus`, el período anterior cuenta como fallado (CA-9.1); con racha actual de n períodos cerrados, `met` de "desde la creación" es al menos n (CA-9.2). | RF-2 a RF-5, RF-9 |
| `trend.test.js` | 12 lunes cerrados en orden; semana actual excluida; diario parcial en la semana de creación; semanal 0 o 100; hábito creado después de una semana no cuenta en ella; semana sin hábitos → `null`; promedio simple. | CA-7.1 a CA-7.4, CA-7.9 |
| `history.test.js` | Cada estado de `dayState` en diario y semanal; 12 semanas con la actual; hábito con menos de 12 semanas empieza en la de creación y `hasMoreHistory` es falso; `showAll` empieza en la semana de creación; resumen "2 de 3" y estados `met`, `missed`, `current` (D-T15); los días `done` de la grilla completa son exactamente `logDates` (CA-8.7). | RF-8 |
| `metricsView.test.js` | Lista vacía → `isEmpty`; textos "67 %", "sin datos", "20 de 30 días", "5 de 8 semanas"; redondeo de 66,5 a 67 solo en el texto; general de exactos (puede diferir en 1 del promedio mostrado, CA-4.5); orden de la API; hábito eliminado no aparece y cambia el general; la tabla y el gráfico usan los mismos valores; solo los hábitos de `expandedIds` traen la grilla completa. | RF-1 a RF-9 |
| `trendChart.test.js` | 12 puntos; `null` se conserva y `spanGaps` es falso (CA-7.4); eje de 0 a 100; animación desactivada; colores recibidos aplicados. | CA-7.1, CA-7.4, CA-7.6 |

### 6.2 En el navegador

Lo que es solo interfaz, cada punto con su tarea en `docs/tasks/004-metricas.md`: pestaña nueva y vuelta a "Hábitos"; marcar hoy en "Hábitos" y volver a "Métricas" (solo cambian período en curso, racha e historial, CA-5.4); eliminar un hábito y volver (CA-9.4); error de red con "Reintentar"; sesión vencida; sin hábitos; línea interrumpida; cambio de modo claro a oscuro con la pestaña abierta (CA-7.6); lector de pantalla en la tabla y en las celdas; "Ver todo el historial" y vuelta a 12 semanas; ningún día editable desde la grilla.

### 6.3 Rendimiento (RNF-3)

- **Datos:** 50 hábitos (25 diarios y 25 semanales con meta 3), cada uno con un registro por día durante 5 años (unos 91.300), generados con SQL (`generate_series`) en `asistente_test`, como en las specs 001 y 003. Los semanales con registros diarios son el peor caso para `countByWeek`.
- **Medición:** compilación de producción (`vite build` + `vite preview`), navegador de escritorio, panel Performance: desde el clic en "Métricas" hasta el cuadro donde se ven las tarjetas y el gráfico. Tres corridas; se anota el peor caso en este plan.
- **Aparte, el cálculo solo:** `performance.now()` temporal alrededor de `toMetricsView` (no se versiona), para separar red, cálculo y render. Referencia: la red y la API ya suman entre 120 y 350 ms con este volumen (plan 001, D-T10).
- **Si falla:** primero el cálculo (perfilar y evitar pasadas repetidas); si el problema es la red, la mejora prevista es comprimir la respuesta en ASP.NET Core (plan 001), que es un cambio de servidor y necesita el visto bueno del dueño, porque la spec dice que no hay cambios en el servidor.

### 6.4 Regla de commit

`npm test` y `dotnet test` pasan antes de cada commit.

## 7. Matriz de cobertura

| RF | Frontend | Tests |
|---|---|---|
| RF-1 Pestaña y vacíos | `App.jsx`, `useMetrics`, `MetricsPage`, `toMetricsView` (`isEmpty`) | `metricsView.test`, navegador |
| RF-2 Períodos y ventanas | `firstPeriod`, `completionIn`, `habitWindows` | `completion.test` |
| RF-3 % por hábito | `completionIn`, `percentOf`, textos de `metricsView` | `completion.test`, `metricsView.test` |
| RF-4 % general | `overallPercent` | `completion.test`, `metricsView.test` |
| RF-5 Período en curso | `weekProgress`, `currentLabel`, ventanas que terminan antes de hoy | `completion.test`, `metricsView.test`, navegador (CA-5.4) |
| RF-6 Rachas | `dailyStreak`, `weeklyStreakFromCounts`, `bestDailyStreak`, `bestWeeklyStreak` | `streak.test` |
| RF-7 Tendencia | `trend`, `trendChartConfig`, `TrendChart`, `TrendTable`, `useColorScheme`, `decisions.md` | `trend.test`, `trendChart.test`, navegador (CA-7.6) |
| RF-8 Historial | `history.js`, `HistoryGrid`, `expandedIds` | `history.test`, navegador (CA-8.6) |
| RF-9 Consistencia | Reglas compartidas de `streak.js` y `completionIn` (D-T2, D-T3), recarga al abrir (D-T8) | `completion.test`, navegador (CA-9.4) |
| RNF-1 Privacidad | Sin endpoints nuevos: los datos salen de `GET /api/habits`, que ya exige sesión (`[Authorize]`) | Tests existentes del backend |
| RNF-3 Rendimiento | D-T4, D-T10, D-T14, D-T16 | Medición de 6.3 |
| RNF-4 Accesibilidad | `TrendTable`, celdas con texto oculto | `metricsView.test` (etiquetas), navegador |
| RNF-5 Idioma | Textos en `metricsView`, `labels.js` y los componentes, incluidos errores y vacíos | `metricsView.test` (textos exactos), navegador |

## 8. Entrega sugerida

El cambio supera holgadamente las 400 líneas, así que conviene en partes que funcionen solas:

1. **Lógica pura y tests** (`dates`, `labels`, `streak`, `completion`, `trend`, `history`, `metricsView`). Sin interfaz; `habitView` e `IdeaCard` siguen iguales por fuera.
2. **Pestaña sin gráfico:** `useMetrics`, `MetricsPage`, resumen, tarjetas, grilla, `TrendTable` y CSS. Ya cumple RF-7 en texto.
3. **Chart.js:** entrada en `decisions.md` (primero), dependencia, `trendChart.js`, `useColorScheme` y `TrendChart`.
4. **Cierre:** medición de RNF-3, verificaciones en el navegador y actualización de `CLAUDE.md` y `AGENTS.md`.

## 9. Riesgos

- **Versión de Chart.js:** se fija `chart.js@4.5.1` (publicada el 13-10-2025). La última, 4.6.1, salió el 22-10-2026 y en npm no figura una 4.6.0; es demasiado reciente para adoptarla sin uso previo. La tarea de la parte 3 instala la versión exacta (sin `^`), confirma la licencia (MIT) y la compatibilidad con Vite 8, y la anota en `decisions.md`.
- **Presupuesto de RNF-3:** la red ya consume entre un tercio y la mitad del segundo. Si el cálculo o el render del gráfico suman demasiado, la siguiente palanca toca el servidor (sección 6.3).
- **Coma flotante en el general (resuelto):** un promedio exacto de ",5" podía quedar en ",4999…" (por ejemplo 37,5 → 37). `roundPercent` en `completion.js` recorta a 9 decimales antes de redondear, y las dos vistas lo usan; un test fija el caso 37,5 → 38.
- **Tema forzado:** `habits.css` admite `data-theme="light"`, pero nada lo usa hoy. `useColorScheme` solo escucha la preferencia del sistema; si algún día se agrega un selector de tema, el gráfico tiene que escucharlo también.
- **Mover `formatDate` y las etiquetas:** toca `IdeaCard.jsx` y `habitView.js`, que ya funcionan. Sus tests y su texto en pantalla no deben cambiar.
- **`node --test` y `Intl`:** `formatDate` depende de los datos de idioma de Node (ICU completo desde Node 13). Si el test da otro texto, se revisa la versión de Node antes de cambiar la función.

## 10. Dudas abiertas

Ninguna bloquea. Dos detalles de interfaz quedan decididos en este plan y conviene que el dueño los confirme al verlos: la grilla va de la semana más antigua a la más nueva (D-T13), y la semana en curso ya cumplida dice "Cumplida" (D-T15).
