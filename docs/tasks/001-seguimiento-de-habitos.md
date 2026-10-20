# Tareas 001 — Seguimiento de hábitos

Fuentes: [spec](../specs/001-seguimiento-de-habitos.md) · [plan](../plans/001-seguimiento-de-habitos.md) · [constitución](../constitution.md)

**Reglas para todas las tareas:**
- Cada tarea dura como máximo 20-30 minutos. Si se pasa, se divide antes de seguir.
- **Las tareas de lógica se hacen con TDD:** primero el test (falla), después el código (pasa).
- Ninguna tarea se marca como hecha si `dotnet test` (desde `backend/`) o `npm test` (desde `frontend/`) fallan (principio 4).
- Si una tarea cambia un comportamiento, la spec se actualiza en el mismo commit (principio 2).

---

## Fase 0 — Preparación

- [x] **T01 — Cerrar las dudas D-1 a D-4** · RF: RF-1, RF-3, RF-6, RF-7
  Hecho cuando: la sección 11 de la spec ya no tiene `[NECESITA ACLARACIÓN]` (cada duda está resuelta o dice "Diferida: supuesto del plan §0") y la tabla §0 del plan coincide con esas respuestas.

- [x] **T02 — Inicializar el repositorio** · RF: —
  Hecho cuando: `git status` funciona y un `.gitignore` excluye `bin/`, `obj/`, `node_modules/` y `.env`.

- [x] **T03 — Crear `docs/decisions.md`** · RF: —
  Hecho cuando: el archivo tiene una entrada para `Npgsql.EntityFrameworkCore.PostgreSQL` y otra para `Microsoft.EntityFrameworkCore.Design`, cada una con "qué problema resuelve" y "por qué no alcanza lo que ya hay".

- [x] **T04 — Actualizar `CLAUDE.md` y `AGENTS.md`** · RF: —
  Hecho cuando: ambos reflejan los endpoints de log y unlog, `HabitLog` con `Date`, CSS plano y las rachas en `src/logic/`, y `diff CLAUDE.md AGENTS.md` solo muestra las diferencias de nombre de agente.

## Fase 1 — Esqueleto del backend

- [x] **T05 — Crear los proyectos `backend/AsistentePersonal` y `backend/AsistentePersonal.Tests`** · RF: —
  Hecho cuando: `dotnet build` y `dotnet test` pasan desde `backend/`, y `Tests` referencia a `Api`.
  Nota: el proyecto se llama `AsistentePersonal` (no `Api`) y la solución es `backend/AsistentePersonal.slnx`. Verificado con `dotnet build` y `dotnet test` desde `backend/`.

- [x] **T06 — Agregar EF Core con PostgreSQL** · RF: —
  Hecho cuando: los dos paquetes de T03 están instalados, `dotnet build` pasa y la cadena de conexión se lee de una variable de entorno (ningún `appsettings*.json` versionado tiene contraseñas).
  Nota: paquetes instalados y sin secretos en `appsettings`. La cadena se lee de `ConnectionStrings__Default`: sin la variable, `dotnet ef dbcontext info` falla con un mensaje claro; con la variable, el proveedor es Npgsql (verificado en T14).

- [x] **T07 — `OwnerClock` (test primero)** · RF: RNF-3
  Hecho cuando: un test verifica que el instante UTC `2026-01-01T03:00Z` con la zona `America/Bogota` da `2025-12-31` y pasa. `Today()` lee `OWNER_TIME_ZONE`.
  Nota: se vio fallar primero (error de compilación, `OwnerClock` no existía) y después pasar: `Passed! Failed: 0, Passed: 1`.

## Fase 2 — Reglas puras del backend (`HabitRules`, TDD)

- [x] **T08 — `Normalize`, `IsBlank` y `CountChars`** · RF: RF-1, RF-8 (CA-1.3, CA-1.6, CA-8.4)
  Hecho cuando: hay tests que pasan para estos casos: espacios al inicio y al final, texto de solo espacios → `null`, solo caracteres invisibles → vacío, y un emoji cuenta como 1.
  Nota: 9 tests en `HabitRulesTests.cs`. Se vio fallar primero (`HabitRules` no existía) y después pasar. Total de la solución: 10 de 10.

- [x] **T09 — `ValidateName` y `ValidateDescription`** · RF: RF-1, RF-8 (CA-1.3, CA-1.4, CA-8.3)
  Hecho cuando: pasan los tests de nombre vacío, 100 contra 101 caracteres y descripción de 500 contra 501, cada uno con su mensaje en español.

- [x] **T10 — `ValidateWeeklyGoal` y `ValidateHabitCount`** · RF: RF-1 (CA-1.2, CA-1.5, CA-1.7)
  Hecho cuando: pasan los tests de meta ausente, 0, 1, 7 y 8, de frecuencia diaria sin meta, y de 49 contra 50 hábitos.

- [x] **T11 — `ValidateLogDate`** · RF: RF-3 (CA-3.2, CA-3.3, CA-3.5)
  Hecho cuando: pasan los tests con `today` fijo para fecha futura (error), hoy (válida), ayer (válida), anteayer (error) y ayer con el hábito creado hoy (error).

- [x] **T12 — `ValidateUnlogDate`** · RF: RF-4 (CA-4.2)
  Hecho cuando: pasan los tests de hoy y ayer (válidas) y de anteayer y una fecha futura (error).

## Fase 3 — Datos

- [x] **T13 — Entidades `Habit` y `HabitLog`** · RF: RF-1, RF-3
  Nota: corregí `id`, `userId` y `Frecuency` a `Id`, `UserId` y `Frequency`, según el plan. `dotnet build` pasa.
  Hecho cuando: las entidades tienen exactamente los campos de la sección 2.2 del plan y `dotnet build` pasa.

- [x] **T14 — `AppDbContext` con índice único y cascada** · RF: RF-3, RF-9, RF-10
  Nota: 3 tests en `AppDbContextModelTests.cs` (índice único, cascada, frecuencia como texto), vistos fallar primero y después pasar. El `DbContext` también se registra en `Program.cs` con la cadena de configuración.
  Hecho cuando: el `DbContext` declara el índice único `(HabitId, Date)` y el borrado en cascada Habit → HabitLog, y `dotnet build` pasa.

- [x] **T15 — Migración `AddHabits`** · RF: RF-3, RF-9, RF-10
  Nota: migración aplicada a `asistente_dev`. Verificado en PostgreSQL: tablas `Habits` y `HabitLogs`, índice único `IX_HabitLogs_HabitId_Date`, índice `IX_Habits_UserId_LowerName` (`lower("Name")`, para D-4 y D-5, escrito a mano porque EF no lo modela) y clave foránea `ON DELETE CASCADE`.
  Hecho cuando: `dotnet ef database update` crea las tablas en la base local, y `\d` en `psql` muestra el índice único y la FK con `ON DELETE CASCADE`.

- [x] **T16 — Base de datos de tests** · RF: —
  Nota: `TestDatabase` lee `ConnectionStrings__Test`, aplica la migración y limpia las tablas antes de cada test. `DatabaseSmokeTests` inserta y lee un hábito. Sin la variable, los tests de base fallan con un mensaje claro. Los tests de base no corren en paralelo.
  Hecho cuando: los tests leen `ConnectionStrings__Test`, limpian las tablas antes de cada test, y un test de humo que inserta y lee un hábito pasa.

## Fase 4 — `HabitService` (TDD contra la base de tests)

- [x] **T17 — `Create`** · RF: RF-1 (CA-1.1 a CA-1.8)
  Nota: 8 tests en `HabitServiceTests.cs`, escritos antes que el servicio.
  Hecho cuando: pasan los tests de que crear normaliza el texto, guarda `CreatedOn = today`, un hábito diario guarda meta nula aunque llegue una, rechaza las entradas inválidas y rechaza el hábito 51 y un nombre ya usado, sin distinguir mayúsculas (CA-1.8).

- [x] **T18 — `List`** · RF: RF-2 (CA-2.2)
  Nota: 2 tests, incluido el que comprueba que un dueño no ve los hábitos de otro.
  Hecho cuando: pasa el test de que devuelve `today` y los hábitos con sus `logDates`, ordenados por `CreatedOn` y luego por `Id`.

- [x] **T19 — `Update`** · RF: RF-8 (CA-8.1 a CA-8.5)
  Nota: 6 tests. Editar no toca la frecuencia ni la meta, porque el método no las recibe.
  Hecho cuando: pasan los tests de que editar cambia solo el nombre y la descripción, conserva los registros, acepta guardar sin cambios, guarda la descripción vacía como `null` y rechaza un nombre inválido o ya usado por otro hábito, sin modificar nada.

- [x] **T20 — `Delete`** · RF: RF-9, RF-10 (CA-9.2, CA-10.1)
  Nota: 2 tests. Los registros se borran por la cascada de la base, no por código.
  Hecho cuando: pasan los tests de que eliminar borra el hábito y sus registros, y de que eliminar un id inexistente devuelve NotFound.

- [x] **T21 — `Log`** · RF: RF-3, RF-10 (CA-3.1 a CA-3.5, CA-10.1)
  Nota: 7 tests, incluido ayer como fecha válida y la fecha anterior a la creación.
  Hecho cuando: pasan los tests de marcar hoy y ayer, de que marcar dos veces deja un solo registro y devuelve Ok, de fechas inválidas → Invalid y de un id inexistente → NotFound.

- [x] **T22 — `Log` concurrente** · RF: RF-10 (CA-10.2)
  Nota: el test usa dos contextos y `Task.WhenAll`. No garantiza que las dos escrituras se crucen en el tiempo; por eso el manejo del índice único (`IsUniqueViolation`) queda verificado por código, no por este test solo.
  Hecho cuando: pasa un test que lanza dos `Log` iguales con `Task.WhenAll` y deja exactamente un registro, sin excepciones.

- [x] **T23 — `Unlog`** · RF: RF-4, RF-10 (CA-4.1 a CA-4.3, CA-10.1)
  Nota: 4 tests.
  Hecho cuando: pasan los tests de que desmarcar borra el registro, de que desmarcar un día sin registro devuelve Ok, de que anteayer → Invalid y de un id inexistente → NotFound.

## Fase 5 — API

- [x] **T24 — DTOs** · RF: RF-1, RF-2, RF-3, RF-8
  Nota: 5 DTOs con los nombres del plan. `CreateHabitRequest` no recibe el dueño ni la fecha de creación; `UpdateHabitRequest` solo tiene nombre y descripción. `dotnet build` pasa.
  Hecho cuando: existen los 5 DTOs del plan, `UpdateHabitRequest` no tiene frecuencia ni meta, y `dotnet build` pasa.

- [x] **T25 — `HabitsController`: CRUD de hábitos** · RF: RF-1, RF-2, RF-8, RF-9, RF-10
  Nota: reescrito tras la revisión. Ver los cambios en la nota de T26.
  Hecho cuando: los endpoints `GET`, `POST`, `PUT` y `DELETE /api/habits` delegan en el servicio, traducen Ok/Invalid/NotFound a 200/400/404 según la tabla 5.3 del plan, y una llamada sin autenticación devuelve 401.

- [x] **T26 — `HabitsController`: marcar y desmarcar** · RF: RF-3, RF-4, RF-10
  Nota: en la revisión se corrigieron estos errores: `Update` pasaba el `UserId` como `habitId` (editaba el hábito equivocado); las rutas no coincidían con el plan; el dueño venía del cliente en vez del token; la fecha de hoy salía del reloj del sistema en vez de `OwnerClock`; todos los resultados devolvían 200 aunque fueran Invalid o NotFound; el servicio se creaba a mano en vez de inyectarse.
  Hecho cuando: `POST /api/habits/{id}/log` y `DELETE /api/habits/{id}/log/{date}` delegan en el servicio con `today` de `OwnerClock`, y una llamada sin autenticación devuelve 401.

- [x] **T27 — Registrar los servicios en `Program.cs`** · RF: RNF-1
  Nota: verificado con `dotnet run --launch-profile https`: `GET`, `POST`, `PUT` y `DELETE` de hábitos y de registros responden 401 sin token, y el log no muestra errores. Esto usa JwtBearer, sin emitir tokens todavía: el camino con sesión válida se prueba cuando exista el login.
  Hecho cuando: la API arranca con `dotnet run`, sin errores de inyección de dependencias, y `GET /api/habits` responde 401.

## Fase 6 — Esqueleto del frontend

- [x] **T28 — Crear `frontend/` con Vite + React** · RF: —
  Nota: verificado: `npm run build` pasa, `npm test` sale con código 0 (0 tests), y `/api/habits` pedido a Vite llega a la API y responde 401. `@vitejs/plugin-react` está justificado en `docs/decisions.md`.
  Hecho cuando: `npm run dev` muestra la página, `npm test` ejecuta `node --test` sin errores (aunque haya 0 tests) y `vite.config.js` redirige `/api` al backend local.

## Fase 7 — Lógica pura del frontend (`src/logic/`, TDD con `node --test`)

- [x] **T29 — `addDays`** · RF: base de RF-3 a RF-7
  Nota: tests de `dates.test.js` (cruce de mes, de año, 29 de febrero y días negativos), vistos fallar primero.
  Hecho cuando: pasan los tests de cruce de mes, de año y de 29 de febrero, y de `n` negativo.

- [x] **T30 — `weekStart`** · RF: RF-6
  Nota: incluye una semana que cruza el cambio de año (2025-12-29 a 2026-01-04).
  Hecho cuando: pasan los tests de que los 7 días de una semana devuelven el mismo lunes, incluida una semana que cruza el cambio de año.

- [x] **T31 — `dailyStreak`** · RF: RF-5 (CA-5.1 a CA-5.3)
  Nota: 6 tests en `streak.test.js`, incluido el de racha de 400 días.
  Hecho cuando: pasan los tests de hoy marcado, solo ayer, ninguno → 0, hueco en el medio y racha de más de 365 días.

- [x] **T32 — `weeklyStreak` y `weekProgress`** · RF: RF-6, RF-2, RF-3 (CA-6.1 a CA-6.3, CA-2.1, CA-3.6)
  Nota: 9 tests entre `streak.test.js` y `habitView.test.js`. Incluye "4 de 3" y la semana de creación sin cumplir.
  Hecho cuando: pasan los tests de semana actual cumplida, solo la anterior cumplida, anterior fallida → 0, "4 de 3" y semana de creación sin cumplir → racha 0 (D-2 resuelta).

- [x] **T33 — `habitStatus` para hábitos diarios** · RF: RF-7 (CA-7.1 a CA-7.4)
  Nota: 6 tests en `status.test.js`.
  Hecho cuando: pasan los tests de verde, rojo, pendiente, creado hoy → nunca rojo, creado ayer sin marcar → rojo, y verde gana sobre rojo.

- [x] **T34 — `habitStatus` para hábitos semanales** · RF: RF-7 (CA-7.1 a CA-7.4)
  Nota: 6 tests en `status.test.js`.
  Hecho cuando: pasan los tests de verde, rojo durante "2 de 3", el lunes con domingo marcado → pendiente, y semana de creación sin cumplir → rojo la semana siguiente (D-2 resuelta).

- [x] **T35 — `loggableDays`** · RF: RF-3, RF-4 (CA-3.2, CA-3.5, CA-4.2)
  Nota: 5 tests en `logWindow.test.js`.
  Hecho cuando: pasan los tests de que un hábito creado hoy solo ofrece "Hoy", uno anterior ofrece "Hoy" y "Ayer", y al cambiar `today` se desplaza la ventana.

- [x] **T36 — `toHabitView`** · RF: RF-2 (CA-2.1)
  Nota: 11 tests en `habitView.test.js`. Frecuencia en la API: `"Daily"`/`"Weekly"`, no en minúsculas.
  Hecho cuando: pasan los tests de las etiquetas "Diario", "3 veces por semana", "Racha: 1 día", "Racha: 5 días", "Racha: 2 semanas" y "Esta semana: 2 de 3", y de `done` correcto en cada botón.

## Fase 8 — Interfaz

> Verificación con sesión real (después de la spec 002, contra `asistente_test`): lista vacía y orden del backend (T38); etiquetas, racha, avance y estado tomados de `toHabitView` (T39); crear un hábito diario y uno semanal, con la meta visible solo en el semanal (T41); editar nombre y descripción con frecuencia y meta fijas y registros conservados (T42); eliminar con el texto del plan, cancelar sin cambios y aceptar quitando el hábito (T43, con `window.confirm` reemplazado para la prueba automática); 404 al marcar un hábito borrado desde fuera, con el mensaje y la lista actualizada, y 401 con el mensaje de sesión expirada (T44); errores de la API mapeados (T37). T40 y T45 también quedaron verificadas después (ver sus notas).
>
> Estado anterior, al cierre de la fase 8: código de T37 a T45 escrito, y `npm test` pasa (57 de 57) junto con `npm run build`. Las casillas seguían sin marcar porque sus "Hecho cuando" piden ver hábitos reales, y eso necesitaba el login (dependencia, ver T47). Verificado en el navegador sin login: la página muestra "Tu sesión expiró. Vuelve a iniciar sesión." con un 401 (T37, T44 parcial); el formulario de creación muestra el campo de meta solo si es semanal, y el envío sin sesión muestra el mensaje sin perder lo escrito (T41 parcial); en 375 px no hay desbordamiento horizontal y una tarjeta de prueba mide 343 px (T45 parcial). Falta verificar con sesión válida: lista vacía (T38), marcar y desmarcar (T40), editar (T42), eliminar (T43), 404 entre pestañas (T44) y colores de estado con datos reales.

- [x] **T37 — `habitsApi.js`** · RF: RF-10 (CA-10.1, CA-10.3)
  Hecho cuando: cada función llama al endpoint del plan, y las respuestas 400, 404 y 401 se convierten en el mensaje en español de la tabla 5.3 (verificado a mano contra la API).

- [x] **T38 — `HabitsPage` y `HabitList`** · RF: RF-2 (CA-2.2, CA-2.3)
  Hecho cuando: con la base vacía se ve el mensaje de lista vacía, y con hábitos cargados se ven en el orden del backend.

- [x] **T39 — `HabitCard`: lectura** · RF: RF-2, RF-5, RF-6, RF-7
  Hecho cuando: la tarjeta muestra el nombre, la frecuencia, la racha, el avance semanal y el estado con color **y** texto, todo tomado de `toHabitView` y sin cálculos dentro del componente.

- [x] **T40 — `HabitCard`: botones Hoy y Ayer** · RF: RF-3, RF-4 (CA-3.1, CA-3.4, CA-4.1)
  Nota: medido en el navegador: marcar 66 ms y desmarcar 34 ms hasta ver el cambio de estado. Los botones se deshabilitan durante la acción. Un doble clic deja un solo registro en la base.
  Hecho cuando: hacer clic marca o desmarca, se vuelve a cargar la lista y la racha y el estado cambian en menos de 1 segundo; los botones se deshabilitan mientras la acción está en curso y un doble clic no genera errores.

- [x] **T41 — `HabitForm`: crear** · RF: RF-1
  Hecho cuando: se puede crear un hábito diario y uno semanal, la meta solo aparece si es semanal, y los errores del backend se ven en pantalla.

- [x] **T42 — `HabitForm`: editar** · RF: RF-8
  Hecho cuando: se pueden editar el nombre y la descripción, la frecuencia y la meta se ven como texto fijo, y los registros se conservan.

- [x] **T43 — Eliminar con confirmación** · RF: RF-9 (CA-9.1 a CA-9.3)
  Hecho cuando: aparece el texto de confirmación del plan, cancelar no cambia nada y aceptar quita el hábito de la lista.

- [x] **T44 — `ErrorBanner` y errores 404 y 401** · RF: RF-10 (CA-10.1, CA-10.3)
  Hecho cuando: eliminar un hábito en una pestaña y marcarlo en otra muestra "Este hábito ya no existe." y actualiza la lista; un 401 muestra el mensaje de sesión expirada.

- [x] **T45 — `habits.css`** · RF: RF-2, RF-7
  Nota: con datos reales, en 1280 px (modo oscuro) y en 375 px (modo claro): cada estado tiene su color de borde, el contraste del texto de estado va de 6,57 a 17,4 (mínimo AA: 4,5) y no hay desbordamiento horizontal.
  Hecho cuando: los tres estados tienen el borde de color correspondiente, el texto de estado es legible y la lista se ve bien en móvil (375 px) y en escritorio.

## Fase 9 — Cierre

- [x] **T46 — Medir RNF-4** · RF: RNF-4
  Nota: 50 hábitos y 91 300 registros: de 428 a 681 ms desde el pedido hasta ver el cambio. Anotado en el plan (D-T10), con la mejora pendiente de comprimir la respuesta.
  Hecho cuando: con 50 hábitos y 5 años de registros generados en la base local, la lista aparece en menos de 1 segundo, y el tiempo medido queda anotado en el plan (D-T10).

- [ ] **T47 — Recorrido completo del dueño** · RF: RF-1 a RF-10
  Nota: desbloqueada por la spec 002. Claude hizo el recorrido completo contra `asistente_test` (crear diario y semanal, marcar hoy y ayer, desmarcar, ver racha y estado, editar y eliminar). Queda abierta porque el criterio pide que lo haga **el dueño, sin ayuda**, en `asistente_dev`.
  Hecho cuando: el dueño autenticado cumple, sin ayuda, cada punto de la sección 10 de la spec (crear diario y semanal, marcar hoy y ayer, desmarcar, ver racha y estado, editar y eliminar).

- [x] **T48 — Verificación final** · RF: todos
  Nota: `dotnet test` 88 de 88 y `npm test` 60 de 60. Todos los tests que lista la matriz del plan (§9) existen y pasan. Los criterios puramente visuales (confirmación al eliminar, CA-9.1 y CA-9.3; sesión expirada en pantalla, CA-10.3) no tienen test automático: se verificaron en el navegador. La spec refleja los cambios de comportamiento (D-1 a D-5 y la meta ignorada en hábitos diarios).
  Hecho cuando: `dotnet test` y `npm test` pasan, cada CA de la matriz del plan (§9) tiene su test, y la spec refleja cualquier cambio de comportamiento hecho durante la implementación.
