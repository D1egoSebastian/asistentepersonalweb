# Plan 001 — Seguimiento de hábitos

Plan técnico de [`docs/specs/001-seguimiento-de-habitos.md`](../specs/001-seguimiento-de-habitos.md). Respeta [`docs/constitution.md`](../constitution.md). Cada parte indica entre corchetes los RF que cubre.

## 0. Supuestos provisionales

La spec exige resolver o diferir explícitamente D-1 a D-4 antes del plan. Este plan las **difiere explícitamente** con estos supuestos. Si la respuesta final es distinta, solo cambian las piezas indicadas.

| Duda | Supuesto provisional | Piezas afectadas si cambia |
|---|---|---|
| D-1 | Zona horaria **fija del dueño**, configurada en la variable de entorno `OWNER_TIME_ZONE` (por ejemplo, `America/Bogota`). | `OwnerClock.cs` |
| D-2 (resuelta por el dueño) | La semana de creación **se evalúa como cualquier otra**: necesita la meta completa. Si no se cumple, la semana siguiente se ve roja. | `weeklyStreak`, `habitStatus` |
| D-3 (resuelta por el dueño) | Meta semanal **de 1 a 7**. | `HabitRules.ValidateWeeklyGoal` |
| D-4 (resuelta por el dueño) | **No se permiten** nombres duplicados entre hábitos del mismo dueño. Sin distinguir mayúsculas (D-5 resuelta). | `AppDbContext` (índice único), `HabitService` |

## 1. Arquitectura en una frase

El backend **valida y guarda**: reglas de fechas, límites, unicidad y existencia. El frontend **calcula y pinta**: racha, estado y avance, con funciones puras que reciben `today` como parámetro. El valor de `today` lo envía el backend, porque la spec exige la hora del sistema y no la del dispositivo.

```
[React] HabitsPage (contenedor)
   │  pide GET /api/habits ──────────────► [API] HabitsController ─► HabitService ─► DbContext ─► PostgreSQL
   │  recibe { today, habits[ { ..., logDates[] } ] }                     │
   │                                                                     └─ HabitRules (puras) + OwnerClock (today)
   ▼
src/logic/* (puras: today como parámetro) ─► toHabitView() ─► HabitList / HabitCard (presentacionales)
```

## 2. Archivos

### 2.1 Documentación

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `docs/decisions.md` | Crear | Justificar los paquetes nuevos (principio 1): `Npgsql.EntityFrameworkCore.PostgreSQL` (sin él EF Core no habla con PostgreSQL) y `Microsoft.EntityFrameworkCore.Design` (necesario para generar migraciones). | — |
| `CLAUDE.md` y `AGENTS.md` | Modificar | Mantenerlos iguales. Reflejar los endpoints nuevos (sección 2.2), el modelo `HabitLog` con `Date` en lugar de `completedAt`/`notes`, el styling (CSS plano) y que las rachas se calculan en `src/logic/` (la línea actual dice `Services/`). | — |

### 2.2 Backend (`backend/AsistentePersonal/`)

> Nota de fase 1: el proyecto se llama `AsistentePersonal` (no `Api`) y los tests `AsistentePersonal.Tests`; ambos están en la solución `backend/AsistentePersonal.slnx`. Donde este plan dice `backend/Api/`, leer `backend/AsistentePersonal/`.

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `Models/Habit.cs` | Crear | Entidad: `Id`, `UserId`, `Name`, `Description` (nullable), `Frequency` (`Daily`/`Weekly`), `WeeklyGoal` (nullable), `CreatedOn` (`DateOnly`). | RF-1, RF-8 |
| `Models/HabitLog.cs` | Crear | Entidad: `Id`, `HabitId`, `Date` (`DateOnly`). Sin notas (fuera de alcance). | RF-3, RF-4 |
| `Data/AppDbContext.cs` | Crear o modificar | `DbSet`s, **índice único `(HabitId, Date)`**, **índice único `(UserId, lower(Name))`** (D-4 y D-5; EF Core lo crea con SQL en la migración) y **borrado en cascada** Habit → HabitLog. | RF-3, RF-9, RF-10 |
| `Data/Migrations/*` | Generar | Migración `AddHabits` (generada con la herramienta, no se escribe a mano). | — |
| `DTOs/CreateHabitRequest.cs` | Crear | `Name`, `Description`, `Frequency`, `WeeklyGoal`. | RF-1 |
| `DTOs/UpdateHabitRequest.cs` | Crear | Solo `Name` y `Description`. Al no tener frecuencia ni meta, es imposible cambiarlas. | RF-8 |
| `DTOs/LogRequest.cs` | Crear | `Date` (`yyyy-MM-dd`): la fecha que vio el usuario. | RF-3 |
| `DTOs/HabitResponse.cs` | Crear | Datos del hábito más `LogDates` (lista de `yyyy-MM-dd`). | RF-2 |
| `DTOs/HabitListResponse.cs` | Crear | `{ Today, Habits }`. | RF-2, RF-5 a RF-7 |
| `Services/OwnerClock.cs` | Crear | Una sola función: `Today()` → `DateOnly` en la zona `OWNER_TIME_ZONE`, con `TimeZoneInfo` (integrado en .NET). Es el único lugar que lee el reloj. | RNF-3, sección 4 de la spec |
| `Services/HabitRules.cs` | Crear | Funciones **puras y estáticas** de validación (sección 3.1). Reciben `today` cuando lo necesitan. | RF-1, RF-3, RF-4, RF-8 |
| `Services/HabitService.cs` | Crear | Casos de uso con `AppDbContext`: listar, crear, editar, eliminar, marcar y desmarcar. Recibe `today` como parámetro y llama a `HabitRules`. | RF-1 a RF-4, RF-8 a RF-10 |
| `Controllers/HabitsController.cs` | Crear | `[Authorize]` en toda la clase. Obtiene `today` de `OwnerClock`, delega en `HabitService` y traduce los resultados a HTTP (sección 5.3). | RF-1 a RF-4, RF-8 a RF-10, RNF-1 |
| `Program.cs` | Modificar | Registrar `AppDbContext` y `HabitService`. La autenticación llega con la spec de login (dependencia). | RNF-1 |

**Endpoints** (todos con autenticación):

| Método y ruta | Uso | RF |
|---|---|---|
| `GET /api/habits` | `{ today, habits }`, ordenados por `CreatedOn` y luego por `Id`. | RF-2 |
| `POST /api/habits` | Crear. | RF-1 |
| `PUT /api/habits/{id}` | Editar el nombre y la descripción. | RF-8 |
| `DELETE /api/habits/{id}` | Eliminar con cascada. | RF-9 |
| `POST /api/habits/{id}/log` | Marcar `{ date }`. | RF-3 |
| `DELETE /api/habits/{id}/log/{date}` | Desmarcar. | RF-4 |

`GET /api/habits/{id}/logs` (historial) no se implementa: está fuera de alcance.

### 2.3 Tests del backend (`backend/AsistentePersonal.Tests/`, plantilla `dotnet new xunit` sin agregados)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `HabitRulesTests.cs` | Crear | Un test por cada regla de `HabitRules`, con `today` fijo. | RF-1, RF-3, RF-4, RF-8 |
| `HabitServiceTests.cs` | Crear | Contra una base PostgreSQL local de tests (`ConnectionStrings__Test`): límite de 50, idempotencia, cascada, índice único y hábito inexistente. Usa el proveedor Npgsql que ya trae el proyecto Api, sin paquetes extra. | RF-1, RF-3, RF-4, RF-9, RF-10 |

### 2.4 Frontend (`frontend/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `src/logic/dates.js` | Crear | Aritmética de fechas `yyyy-MM-dd` (sección 3.2). | Base de RF-3 a RF-7 |
| `src/logic/streak.js` | Crear | `dailyStreak`, `weeklyStreak`, `weekProgress`. | RF-5, RF-6, RF-2 |
| `src/logic/status.js` | Crear | `habitStatus`. | RF-7 |
| `src/logic/logWindow.js` | Crear | `loggableDays`: qué botones (hoy y ayer) se habilitan. | RF-3, RF-4 |
| `src/logic/habitView.js` | Crear | `toHabitView`: arma el modelo que pinta la tarjeta. | RF-2 |
| `src/logic/*.test.js` | Crear | Tests con `node --test` (sección 6). | Todos los de `logic` |
| `src/api/habitsApi.js` | Crear | Llamadas `fetch` y traducción de errores HTTP a mensajes en español. | RF-10 |
| `src/pages/HabitsPage.jsx` | Crear | **Contenedor**: carga datos, guarda el estado, llama a la API y vuelve a cargar tras cada acción. | RF-2 a RF-4, RF-8 a RF-10 |
| `src/components/HabitList.jsx` | Crear | Presentacional: lista o estado vacío. | RF-2 |
| `src/components/HabitCard.jsx` | Crear | Presentacional: una tarjeta (sección 4). | RF-2 a RF-7 |
| `src/components/HabitForm.jsx` | Crear | Presentacional: crear o editar. En modo edición oculta la frecuencia y la meta. | RF-1, RF-8 |
| `src/components/ErrorBanner.jsx` | Crear | Muestra el mensaje de error actual. | RF-1, RF-3, RF-4, RF-10 |
| `src/styles/habits.css` | Crear | Estilos de la lista y de los estados. | RF-2, RF-7 |
| `package.json` | Modificar | Script `"test": "node --test"`. | — |

## 3. Funciones puras

### 3.1 Backend: `HabitRules` (C#)

Cada función devuelve `null` si la entrada es válida, o un mensaje de error en español.

| Firma | Regla | RF |
|---|---|---|
| `ValidateName(string? name)` | Texto vacío → obligatorio; más de 100 caracteres → límite. | CA-1.3, CA-1.4, CA-8.3 |
| `ValidateDescription(string? description)` | Más de 500 caracteres → límite. | CA-1.4, CA-8.3 |
| `ValidateWeeklyGoal(Frequency f, int? goal)` | Si es diaria, la meta se ignora y el hábito se guarda sin meta (decisión del dueño). Si es semanal: falta, o está fuera de 1 a 7 (D-3 resuelta). | CA-1.2, CA-1.5 |
| `ValidateHabitCount(int current)` | 50 o más → máximo alcanzado. | CA-1.7 |
| `ValidateLogDate(DateOnly date, DateOnly today, DateOnly createdOn)` | Futura → CA-3.3; anterior a ayer → CA-3.2; anterior a la creación → CA-3.5. | CA-3.2, CA-3.3, CA-3.5 |
| `ValidateUnlogDate(DateOnly date, DateOnly today)` | Anterior a ayer o futura → no modificable. | CA-4.2 |
| `Normalize(string? text)` | Recorta espacios; si queda texto vacío, devuelve `null`. | CA-1.6, CA-8.4 |
| `IsBlank(string? text)` | Sin caracteres visibles: espacios o caracteres invisibles (categoría Unicode `Format`). | CA-1.3 |
| `CountChars(string text)` | Caracteres tal como los ve el usuario, con `StringInfo.LengthInTextElements` (integrado en .NET). | CA-1.4 |

### 3.2 Frontend: `src/logic/` (JavaScript, siempre con `today` como parámetro)

> Nota de fase 7: la frecuencia llega de la API como `"Daily"` o `"Weekly"` (la API usa `JsonStringEnumConverter`), no en minúsculas. Las funciones comparan con esos valores. `streak.js` también exporta `countByWeek` e `isWeekMet`, que usa `status.js`.

Todas las fechas son textos `yyyy-MM-dd`. Ninguna función lee el reloj.

| Archivo | Firma | Devuelve | RF |
|---|---|---|---|
| `dates.js` | `addDays(date, n)` | Fecha desplazada `n` días. | Base |
| `dates.js` | `weekStart(date)` | Lunes de esa semana. | RF-6 |
| `streak.js` | `dailyStreak(logDates, today)` | Número. | RF-5 |
| `streak.js` | `weeklyStreak(logDates, goal, createdOn, today)` | Número. | RF-6 |
| `streak.js` | `weekProgress(logDates, today)` | Registros de la semana actual (puede superar la meta). | CA-2.1, CA-3.6 |
| `status.js` | `habitStatus(habit, today)` | `"green"`, `"red"` o `"pending"`. | RF-7 |
| `logWindow.js` | `loggableDays(createdOn, today)` | `[{ date, label: "Hoy" }, { date, label: "Ayer" }?]`. Ayer solo aparece si es igual o posterior a `createdOn`. | CA-3.2, CA-3.5, CA-4.2 |
| `habitView.js` | `toHabitView(habit, today)` | `{ id, name, description, frequencyLabel, streakLabel, status, statusLabel, progressLabel?, days: [{ date, label, done }] }`. | RF-2 |

## 4. Algoritmo del mapa (pseudocódigo)

Interpreto "el mapa" como el **mapa de registros**: convertir la lista de fechas registradas en estructuras de búsqueda rápida (un conjunto de días y un mapa de semana → cantidad). Sobre ellas se calculan la racha y el estado.

### 4.1 Aritmética de fechas sin librerías

```
addDays(date, n):
    convertir date a milisegundos UTC (Date.UTC con año, mes-1, día)
    sumar n * 86 400 000
    devolver en formato yyyy-MM-dd usando getters UTC
    # En UTC no hay horario de verano: cada día dura exactamente 24 h

weekStart(date):
    dia = día de la semana UTC de date      # 0 = domingo ... 6 = sábado
    retroceso = (dia + 6) mod 7             # lunes → 0, domingo → 6
    devolver addDays(date, -retroceso)
```

### 4.2 Construir el mapa

```
buildDaySet(logDates):
    devolver Set(logDates)

buildWeekMap(logDates):
    mapa = Map vacío
    para cada fecha en logDates:
        semana = weekStart(fecha)
        mapa[semana] = (mapa[semana] o 0) + 1
    devolver mapa
```

### 4.3 Racha diaria [RF-5]

```
dailyStreak(logDates, today):
    dias = buildDaySet(logDates)
    cursor = si dias tiene today: today, si no: addDays(today, -1)   # CA-5.1 / CA-5.2
    racha = 0
    mientras dias tenga cursor:
        racha += 1
        cursor = addDays(cursor, -1)
    devolver racha                                                    # 0 si ni hoy ni ayer (CA-5.3)
```

### 4.4 Racha semanal [RF-6]

```
weeklyStreak(logDates, goal, createdOn, today):
    semanas = buildWeekMap(logDates)
    actual = weekStart(today)
    primera = weekStart(createdOn)
    cumplida(s) = (semanas[s] o 0) >= goal
    cursor = si cumplida(actual): actual, si no: addDays(actual, -7)  # CA-6.1 / CA-6.2
    racha = 0
    mientras cursor >= primera y cumplida(cursor):
        racha += 1
        cursor = addDays(cursor, -7)
    devolver racha                                                    # CA-6.3
    # D-2 resuelta: la semana de creación se evalúa igual que las demás; si no se cumplió, el bucle termina
```

### 4.5 Estado [RF-7]

```
habitStatus(habit, today):
    si habit.frequency == "daily":
        ayer = addDays(today, -1)
        dias = buildDaySet(habit.logDates)
        si dias tiene today:                          devolver "green"     # CA-7.1
        si ayer >= habit.createdOn y no dias tiene ayer: devolver "red"   # CA-7.2, CA-7.4
        devolver "pending"                                                 # CA-7.3
    si no:
        semanas = buildWeekMap(habit.logDates)
        actual = weekStart(today)
        anterior = addDays(actual, -7)
        primera = weekStart(habit.createdOn)
        si (semanas[actual] o 0) >= goal:             devolver "green"
        si anterior >= primera y (semanas[anterior] o 0) < goal: devolver "red"   # D-2 resuelta
        # anterior < primera  → el hábito no existía (CA-7.4)
        devolver "pending"
```

Las fechas `yyyy-MM-dd` se comparan como texto: el orden alfabético coincide con el orden cronológico.

## 5. Flujos y comportamiento

### 5.1 Marcar y desmarcar [RF-3, RF-4, RF-10]

> Nota de fase 4: todos los métodos de `HabitService` reciben `userId` como primer parámetro (RNF-1). El plan original lo omitía, pero cada hábito tiene `UserId`. Los métodos devuelven `ServiceResult` (Ok, Invalid o NotFound) en lugar de lanzar excepciones.

```
HabitService.Log(habitId, date, today):
    habit = buscar por id             → si no existe: NotFound              (CA-10.1)
    error = HabitRules.ValidateLogDate(date, today, habit.CreatedOn)
                                      → si hay error: Invalid(error)        (CA-3.2, 3.3, 3.5)
    si ya existe (habitId, date): devolver Ok                                (CA-3.4)
    insertar; si el índice único rechaza por concurrencia: devolver Ok       (CA-10.2)

HabitService.Unlog(habitId, date, today):
    habit = buscar por id             → si no existe: NotFound              (CA-10.1)
    error = HabitRules.ValidateUnlogDate(date, today) → si hay error: Invalid(error)  (CA-4.2)
    borrar si existe; si no existe, no hacer nada; devolver Ok              (CA-4.1, CA-4.3)
```

La fecha que viaja es la que vio el usuario. Por eso un "hoy" enviado a las 23:59:59 llega como "ayer" y sigue siendo válido.

### 5.2 Crear, editar y eliminar [RF-1, RF-8, RF-9]

- **Crear:** `Normalize` → `ValidateName`, `ValidateDescription`, `ValidateWeeklyGoal`, `ValidateHabitCount` → comprobar que el nombre no exista (CA-1.8) → guardar con `CreatedOn = today` (si es diario, la meta se guarda como `null`).
- **Editar:** `Normalize` → `ValidateName`, `ValidateDescription` → comprobar que el nombre no esté en uso por otro hábito (CA-8.3) → guardar. Sin cambios también devuelve Ok (CA-8.5).
- **Eliminar:** la confirmación ocurre en la interfaz (CA-9.1, CA-9.3). El backend borra en cascada (CA-9.2) o devuelve NotFound (CA-10.1).

### 5.3 Traducción a HTTP (en el controller)

| Resultado del servicio | HTTP | Qué hace la interfaz |
|---|---|---|
| Ok | 200 / 204 | Vuelve a cargar la lista. |
| Invalid(mensaje) | 400 `{ message }` | Muestra el mensaje en `ErrorBanner`. |
| NotFound | 404 | "Este hábito ya no existe." y vuelve a cargar la lista (CA-10.1). |
| Sin sesión | 401 (lo emite ASP.NET) | "Tu sesión expiró. Vuelve a iniciar sesión." Sin cambios (CA-10.3). |

## 6. Cómo se pinta en la interfaz [RF-2 a RF-9]

**Lista (`HabitList`)**
- Sin hábitos: "Todavía no tienes hábitos. Crea el primero para empezar." y el botón "Nuevo hábito" (CA-2.3).
- Con hábitos: tarjetas en el orden que llega del backend (CA-2.2).

**Tarjeta (`HabitCard`)**, alimentada solo por `toHabitView`:

```
┌─▌ verde/rojo/gris ──────────────────────────────────┐
│  Daily coding challenge                 ● Cumplido   │  ← statusLabel: Cumplido / No cumplido / Pendiente
│  Diario · Racha: 5 días                              │  ← frequencyLabel · streakLabel
│  [✓ Hoy]  [  Ayer]                                   │  ← botones de loggableDays (aria-pressed)
│                                    Editar  Eliminar  │
└──────────────────────────────────────────────────────┘
Semanal:  "3 veces por semana · Racha: 2 semanas · Esta semana: 2 de 3"
```

- **Estado (RF-7):** borde izquierdo de color **y** texto ("Cumplido", "No cumplido", "Pendiente"). El color nunca es la única señal (accesibilidad).
- **Hoy y ayer (RF-3, RF-4):** botones que alternan entre marcar y desmarcar. "Ayer" no aparece si el hábito se creó hoy (CA-3.5). Mientras una acción está en curso, los botones se deshabilitan: eso evita el doble envío, y el backend igual es idempotente (CA-3.4).
- **Formulario (RF-1, RF-8):** nombre, descripción, frecuencia (diaria o semanal) y meta (visible solo si es semanal). En edición, la frecuencia y la meta se muestran como texto fijo, no como campos.
- **Eliminar (RF-9):** `window.confirm("Se borrará el hábito y todo su historial. Esta acción no se puede deshacer. ¿Continuar?")`.
- **Después de cada acción:** `HabitsPage` vuelve a pedir `GET /api/habits` y re-renderiza (sección 7, D-T4).

## 7. Decisiones técnicas

| ID | Decisión | Por qué | Alternativa descartada |
|---|---|---|---|
| D-T1 | La racha, el estado y el avance se calculan en el **frontend** (`src/logic/`). | Son funciones puras que se testean con `node --test` y la interfaz las necesita para pintar. El backend queda simple. | Calcularlas en `Services/`: duplica las reglas o deja al frontend sin lógica que testear. Se retoma en la spec de métricas si hace falta. |
| D-T2 | El backend **envía `today`**. | La spec exige la hora de referencia del sistema, no la del dispositivo. Además vuelve deterministas las funciones puras. | `new Date()` en el navegador: falla si el reloj o la zona del dispositivo están mal. |
| D-T3 | Las fechas se manejan como texto `yyyy-MM-dd` con aritmética UTC y `DateOnly` en el backend. | Sin horas no hay problemas de horario de verano ni de zona, y no hace falta ninguna librería (principio 1). | date-fns o Luxon: suman una dependencia para pocas funciones. |
| D-T4 | Se vuelve a cargar la lista tras cada acción. | Es una sola fuente de verdad, cubre la concurrencia (CA-10.2) y es lo más simple de entender. Cabe en el segundo de "inmediatamente". | Actualización optimista: más rápida, pero exige revertir los errores, y eso es complejo para un junior. |
| D-T5 | La unicidad de los registros se garantiza con un **índice único** en la base. | Es lo único que resiste de verdad las acciones simultáneas (CA-10.2). | Comprobar antes de insertar: tiene una carrera entre dos peticiones. |
| D-T6 | Las validaciones de formulario existen **solo en el backend**; el frontend muestra su mensaje. | Una sola copia de cada regla (principio 7). | Validar también en el frontend: mejor experiencia, pero duplica las reglas en dos lenguajes. |
| D-T7 | `HabitRules` y `src/logic/` reciben `today` como parámetro, sin una interfaz `IClock`. | Se testean pasándoles una fecha; el reloj se lee en un único lugar (`OwnerClock`). | `IClock` con inyección: una abstracción "por si acaso" (principio 7). |
| D-T8 | **CSS plano** en `src/styles/`. | No suma dependencias (principio 1) y es suficiente para una lista. | Tailwind: requiere instalación, configuración y aprender su sintaxis. |
| D-T9 | La confirmación de eliminación usa `window.confirm`. | Viene con el navegador y no requiere ningún componente. | Un diálogo propio: más lindo, pero es código y estado extra. |
| D-T10 | Se envía todo el historial de fechas en `GET /api/habits`. | La racha diaria puede abarcar años, y en el peor caso de RNF-4 (unas 91 000 fechas) son alrededor de 1 MB sin comprimir, menos con gzip. | Enviar la racha ya calculada desde el backend: contradice D-T1. Se revisa si RNF-4 falla en la medición. |

> **Medición de RNF-4 (T46), en `localhost`:** 50 hábitos y 91 300 registros (5 años). La API responde en 0,20 a 0,35 s con 1,19 MB sin comprimir. En el navegador, desde que se pide la lista hasta que el cambio es visible: 428, 432 y 681 ms (la red sola, unos 120 ms; el resto es cálculo de rachas y render). **Cumple RNF-4**, pero con poco margen en una conexión real: la respuesta no está comprimida, y 1,19 MB en una conexión lenta pueden sumar varios cientos de milisegundos. Mejora pendiente: activar la compresión de respuestas de ASP.NET Core, que viene con el framework y no necesita paquetes.

## 8. Estrategia de tests

### 8.1 Frontend con `node --test` (principio 4, RNF-2)

- **Comando:** `npm test`, que ejecuta `node --test` desde `frontend/`. Node descubre los `*.test.js` y ignora `node_modules`. Requiere `"type": "module"` (Vite ya lo trae).
- **Sin dependencias:** solo se usan `node:test` (`describe`, `it`) y `node:assert/strict`.
- **Solo `src/logic/`:** los componentes JSX no se testean porque solo pintan (principio 3).
- **`today` siempre fijo** en cada test. Ningún test depende del día en que se ejecuta.

| Archivo | Casos | Cubre |
|---|---|---|
| `dates.test.js` | `addDays` cruzando mes, año y 29 de febrero; `weekStart` para cada día de la semana y para una semana que cruza el año. | Sección 4 de la spec, casos límite de semana |
| `streak.test.js` | Diaria: hoy marcado; solo ayer; ni hoy ni ayer → 0; hueco en el medio; racha larga (más de 365 días). Semanal: semana actual cumplida; no cumplida pero la anterior sí; anterior fallida → 0; "4 de 3"; semana de creación sin cumplir → racha 0 (D-2 resuelta); vuelta tras semanas sin uso. `weekProgress` incluso por encima de la meta. | CA-5.1 a 5.3, CA-6.1 a 6.3, CA-3.6 |
| `status.test.js` | Diario: verde; rojo (ayer sin marcar); pendiente; creado hoy → nunca rojo; creado ayer sin marcar → rojo. Semanal: verde; rojo toda la semana "2 de 3"; lunes después de marcar el domingo pasa de rojo a pendiente; semana de creación sin cumplir → rojo la semana siguiente (D-2 resuelta). Prioridad verde > rojo. | CA-7.1 a 7.4, casos límite |
| `logWindow.test.js` | Hábito creado hoy → solo "Hoy". Creado antes → "Hoy" y "Ayer". El cambio de `today` (medianoche) desplaza la ventana. | CA-3.2, CA-3.5, CA-4.2 |
| `habitView.test.js` | Etiquetas: "Diario", "3 veces por semana", "Racha: 1 día" / "Racha: 5 días", "Esta semana: 2 de 3", `done` correcto en cada botón. | CA-2.1 |

### 8.2 Backend con xUnit (principio 4)

- **`HabitRulesTests`** (unitarios, sin base): nombre vacío, de solo espacios o de solo caracteres invisibles; 100 contra 101 caracteres contando emojis como 1; descripción de 500 contra 501; meta ausente, 0, 1, 7 y 8; 49 contra 50 hábitos; fecha futura, de hoy, de ayer, de anteayer y anterior a la creación; desmarcar anteayer. Cubre CA-1.3 a 1.7, CA-3.2, 3.3, 3.5, CA-4.2 y CA-8.3.
- **`HabitServiceTests`** (contra la base PostgreSQL de tests): marcar dos veces → un registro; marcar en paralelo (`Task.WhenAll`) → un registro; desmarcar sin registro → Ok; eliminar → sus registros desaparecen; acciones sobre un id inexistente → NotFound; editar sin cambios → Ok; editar no toca la frecuencia; orden de la lista; nombre ya usado al crear y al editar, sin distinguir mayúsculas. Cubre CA-3.4, CA-4.3, CA-8.1, 8.2, 8.5, CA-9.2, CA-10.1, 10.2 y CA-2.2.
- **Fuera de los tests automatizados:** RNF-4 (medición manual con 50 hábitos y 5 años de datos generados) y CA-10.3 (depende de la spec de login).

### 8.3 Regla de commit

`npm test` (frontend) y `dotnet test` (backend) tienen que pasar antes de cada commit.

## 9. Matriz de cobertura

| RF | Backend | Frontend | Tests |
|---|---|---|---|
| RF-1 Crear | `HabitRules`, `HabitService.Create`, `CreateHabitRequest` | `HabitForm`, `ErrorBanner` | `HabitRulesTests` |
| RF-2 Ver | `GET /api/habits` (orden) | `toHabitView`, `HabitList`, `HabitCard` | `habitView.test`, `HabitServiceTests` |
| RF-3 Marcar | `ValidateLogDate`, `HabitService.Log`, índice único | `loggableDays`, `HabitCard` | `HabitRulesTests`, `HabitServiceTests`, `logWindow.test` |
| RF-4 Desmarcar | `ValidateUnlogDate`, `HabitService.Unlog` | `loggableDays`, `HabitCard` | `HabitRulesTests`, `HabitServiceTests`, `logWindow.test` |
| RF-5 Racha diaria | (envía `today` y `logDates`) | `dailyStreak` | `streak.test` |
| RF-6 Racha semanal | (envía `today` y `logDates`) | `weeklyStreak`, `weekProgress` | `streak.test` |
| RF-7 Estado | (envía `today` y `createdOn`) | `habitStatus`, CSS de estados | `status.test` |
| RF-8 Editar | `UpdateHabitRequest`, `HabitService.Update` | `HabitForm` (modo edición) | `HabitRulesTests`, `HabitServiceTests` |
| RF-9 Eliminar | Cascada, `HabitService.Delete` | `window.confirm` en `HabitsPage` | `HabitServiceTests` |
| RF-10 Consistencia | NotFound, índice único, `[Authorize]` | `habitsApi.js` (404 y 401) | `HabitServiceTests` (CA-10.3 queda pendiente del login) |
