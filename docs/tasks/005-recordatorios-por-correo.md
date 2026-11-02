# Tareas 005 — Resumen diario por correo y publicación

Fuentes: [spec](../specs/005-recordatorios-por-correo.md) · [plan](../plans/005-recordatorios-por-correo.md) · [constitución](../constitution.md)

**Reglas para todas las tareas:**
- Cada tarea dura como máximo 20-30 minutos. Si se pasa, se divide antes de seguir.
- **TDD estricto:** en cada tarea marcada "(test primero)" el test se escribe y se ve fallar (rojo, incluso por no compilar) antes de escribir el código (verde). Backend: xunit de la plantilla, sin Moq ni paquetes extra. Frontend: `node:test` y `node:assert/strict`. Fechas fijas: hoy es el viernes `2026-10-16` y el lunes `2026-10-19` para CA-3.3; zona `America/La_Paz`.
- Ninguna tarea se marca como hecha si `dotnet test` (desde `backend/`) o `npm test` (desde `frontend/`) fallan (principio 4).
- Si una tarea cambia un comportamiento o una regla del proyecto, la spec o el documento afectado se actualiza **en el mismo commit** (principio 2). Esto cubre S-3 (T19) y la enmienda S-4 (T36 a T38).
- Ninguna función supera 30 líneas de lógica (principio 7). Los tests con datos usan `asistente_test`; las verificaciones manuales usan `asistente_dev`.
- Las claves (`Resend__ApiKey`, `Reminders__Secret`) solo viven en variables de entorno. Ninguna tarea las pega en un archivo, un commit, una captura ni un log (RNF-2).
- Las verificaciones externas (Fase 0) las ejecuta el dueño con ayuda del agente; el resultado se anota como "Nota (fecha)" en la tarea. Si un resultado contradice el plan, se detiene el trabajo y se avisa al dueño: el agente no cambia las decisiones del plan.
- Las tareas de la Fase 6 (C5) están **BLOQUEADAS** hasta que el dueño decida sobre el límite de intentos de login y el token en `localStorage` (T40).

## Review Workload Forecast

| Campo | Valor |
|---|---|
| Líneas cambiadas estimadas | Código y tests ≈ 1.900; documentación ≈ 700; migración generada ≈ 400 (se revisa aparte) |
| Riesgo frente al presupuesto de 400 líneas | Alto en conjunto; cada commit propuesto queda por debajo (ver "Entrega") |
| Estrategia de entrega recibida | No indicada; se asume `ask-on-risk` |
| Estrategia de cadena | feature-branch-chain: rama tracker `feat/recordatorios-por-correo`, PR hijos encadenados, solo el tracker a main |

Decision needed before apply: No
delivery_strategy: auto-chain (decidido por el dueño, según recomendación)
chain_strategy: feature-branch-chain (rama tracker `feat/recordatorios-por-correo`; los PR hijos apuntan al anterior; solo el tracker se fusiona a main)
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain (decidido)
400-line budget risk: High

---

## Fase 0 — Preparación y verificaciones previas (sin código de producción)

- [ ] **T00 — Confirmar spec y plan aprobados** · RF: —
  Hecho cuando: el dueño confirma por escrito las dos lecturas del plan §14 (D-T9 "período existió completo" y D-T4 `ReminderLog` con `IdeaId`) y la spec no tiene dudas abiertas distintas de D-1, D-2 y D-3. Si el dueño cambia alguna, se vuelve al plan antes de seguir.

- [ ] **T01 — RNF-1: ningún servicio pide tarjeta** · RF: RNF-1, CA-5.2
  Hecho cuando: se comprobó en el alta del plan gratuito de Neon, Render, Resend y GitHub Pages (repositorio público) que ninguno exige tarjeta, y el resultado por servicio queda anotado en la tarea. Si alguno la exige, se descarta ese servicio y se avisa al dueño antes de implementar; el agente no elige reemplazo.

- [ ] **T02 — D-1: verificar `unaccent` en Neon** · RF: D-1, plan §13
  Hecho cuando: con un proyecto Neon creado (sin publicar la app), `CREATE EXTENSION IF NOT EXISTS unaccent;` termina sin error y `SELECT unaccent('canción');` devuelve `cancion`. También queda anotado el formato de cadena para Npgsql que funciona (`Host=…;Database=…;Username=…;Password=…;SSL Mode=Require`) y cuál conexión (directa o agrupada) se usa para migraciones y cuál para la app. Si falla, se detiene todo y se avisa: la migración `AddIdeas` no corre en Neon.

- [ ] **T03 — D-2: prueba de entrega a Gmail** · RF: D-2, CA-5.3, plan §13
  Hecho cuando: con la cuenta de Resend creada con el **mismo Gmail del dueño** y una clave solo de envío en una variable de entorno de la sesión, un `curl` a `https://api.resend.com/emails` (remitente `onboarding@resend.dev`, texto de prueba sin datos reales) llega a la **bandeja principal** de Gmail y no a spam. Se anota la ubicación (principal, promociones o spam). Si cae en spam, se anota y el dueño decide; el agente no cambia el remitente. T52 reconfirma con el envío real.

- [ ] **T04 — Resend: límites, retención e idempotencia** · RF: RNF-6, CA-1.4, plan §13
  Hecho cuando: con la documentación vigente de Resend queda anotado: si la cabecera `Idempotency-Key` está soportada (decide si T33 la envía), el límite de envíos del plan gratuito frente a 1 correo diario, la política de retención del contenido enviado (alimenta la entrada de T37) y si el remitente de prueba solo entrega a la dirección de la cuenta.

## Fase 1 — C2a: hábitos puros y casos compartidos

- [ ] **T05 — `test-cases/daily-streak.json` y `streakCases.test.js`** · RF: D-3 · Plan: D-T11
  Hecho cuando: existe `test-cases/daily-streak.json` en la raíz con casos `{ today, logDates, dailyStreak }` (sin registros, hoy marcado, hoy sin marcar, racha cortada ayer contada desde anteayer, cruce de mes y de año) y casos de `weekStart` (lunes, domingo, cruce de año); `frontend/src/logic/streakCases.test.js` los lee con `fs.readFileSync` y pasa contra `dailyStreak` y `weekStart` **sin modificar** `streak.js` ni `dates.js`. Si un caso falla, se corrige el JSON, no el JS existente.

- [ ] **T06 — `WeekStart` y `DailyStreak` en C# (test primero)** · RF: D-3 (CA-3.2, CA-3.4) · Plan: D-T11, §4.1
  Hecho cuando: en `backend/AsistentePersonal.Tests/ReminderRulesTests.cs` fallan primero y luego pasan los tests `SharedCases_*` que leen el mismo JSON (el `.csproj` de tests lo copia con `<None … CopyToOutputDirectory>`, sin paquetes nuevos); `ReminderRules.WeekStart` y `ReminderRules.DailyStreak` están en `Services/ReminderRules.cs` junto a las constantes `StaleIdeaDays = 14`, `MinLostStreak = 3`, `IdeaPreviewChars = 80` y `DigestTime = 08:00` (S-8).

- [ ] **T07 — Alerta `MissedYesterday` (test primero)** · RF: CA-3.1 · Plan: D-T9, §4.2
  Hecho cuando: pasan en `ReminderRulesTests.cs` `MissedYesterday_*`: diario sin registro de ayer → alerta; con registro de ayer → no; **creado ayer → no** (`createdOn < ayer`); creado anteayer → sí; semanal → no. Los tipos `HabitAlert` y `AlertKind` y `HabitAlerts(habit, logs, today)` están en `ReminderRules.cs`.

- [ ] **T08 — Alerta `StreakLost` y dos alertas por hábito (test primero)** · RF: CA-3.4, CA-3.5 · Plan: §4.2, S-1
  Hecho cuando: pasan `StreakLost_*`: racha de 3 días cortada ayer → sí (con el largo de la racha, por ejemplo 5); racha de 2 → no; ayer marcado → no; semanal → no; y `TwoAlerts_SameHabit` (CA-3.1 y CA-3.4 juntas devuelven dos alertas del mismo hábito).

- [ ] **T09 — Alerta `WeeklyAtRisk` (test primero)** · RF: CA-3.2 · Plan: §4.2, S-2
  Hecho cuando: pasan `WeeklyAtRisk_*` con meta 3: lunes 0 de 3 → no (quedan 7); viernes 0 de 3 → sí (quedan 3); viernes 1 de 3 → no; domingo con 0 de 1 → sí; meta cumplida → no; hoy ya marcado cuenta como hecho; y un hábito diario → no.

- [ ] **T10 — Alerta `LastWeekMissed` (test primero)** · RF: CA-3.3 · Plan: D-T9, §4.2
  Hecho cuando: pasan `LastWeekMissed_*` con hoy lunes 2026-10-19: un día que no es lunes → no; meta cumplida la semana pasada → no; hábito creado esta semana → no; **creado el miércoles de la semana pasada → no** (D-T9); creado el lunes pasado → no (`createdOn < lunes pasado`); creado antes del lunes pasado con menos registros que la meta → sí.

- [ ] **T11 — Cierre de C2a** · RF: CA-3.1 a CA-3.5, D-3
  Hecho cuando: `dotnet test` y `npm test` pasan; cada alerta tiene su test en positivo y en su borde negativo; ninguna función supera 30 líneas; no hay cambios en `frontend/src` salvo `streakCases.test.js`; y el commit se mide antes de cerrarlo: si supera 400 líneas, T10 pasa a un commit propio.

## Fase 2 — C2b: ideas, texto, secreto y reloj puros

- [ ] **T12 — `OwnerClock.LocalTimeAt` e `IsDigestTime` (test primero)** · RF: CA-1.1 · Plan: D-T2, §4.1
  Hecho cuando: en `OwnerClockTests.cs` pasan los tests de la hora local con `America/La_Paz` y con `America/Santiago`, incluido el día de un cambio de horario (fecha comprobada con `TimeZoneInfo`); `OwnerClock.LocalTimeAt(utcNow, zone)` devuelve `TimeOnly`; en `ReminderRulesTests.cs` pasan `IsDigestTime_*` con 07:59 → falso, 08:00 → verdadero y 23:59 → verdadero; los tests existentes de `OwnerClock` pasan sin modificarlos.

- [ ] **T13 — `IsValidSecret` (test primero)** · RF: CA-4.2 · Plan: D-T7
  Hecho cuando: pasan `IsValidSecret_*`: igual → verdadero; distinto → falso; de otra longitud → falso; `null` → falso; vacío → falso. La implementación usa `CryptographicOperations.FixedTimeEquals` sobre los SHA-256 de ambos valores (revisión de código, no medición de tiempos).

- [ ] **T14 — `IsIdeaDue` y `DaysSince` (test primero)** · RF: CA-2.1, CA-2.2, CA-2.3, CA-2.4 · Plan: §4.1
  Hecho cuando: pasan `IdeaDue_At14Days_NotAt13`, `IdeaDue_14DaysAfterLastReminder` (recordada hace 13 → no, hace 14 → sí), `IdeaEditedAfterReminder_CountsFromEdit` y `DaysSince` con 15 y 28 días.

- [ ] **T15 — `IdeaRules.Cut` y `Summary` (test primero)** · RF: CA-2.4 · Plan: D-T12
  Hecho cuando: en `IdeaRulesTests.cs` pasan `Cut_*` (primera línea con texto, corte a 80 con "…", emoji y tildes sin partirse, saltos de línea iniciales ignorados, texto corto sin "…"); `Summary` llama a `Cut(text, 120)`; y **los tests existentes de `Summary` pasan sin modificarlos**.

- [ ] **T16 — `ReminderText.Subject` (test primero)** · RF: CA-1.6 · Plan: §4.1
  Hecho cuando: en `ReminderTextTests.cs` pasan "Tu resumen de hoy: 2 ideas y 1 hábito", "…: 1 idea", "…: 3 hábitos" y la omisión de la parte vacía.

- [ ] **T17 — `ReminderText.Body` (test primero)** · RF: CA-1.6, CA-2.4, CA-3.5 · Plan: §4.4
  Hecho cuando: pasan `IdeaLine_*` (texto recortado a 80 y días sin editar), `OneLinePerAlert` y el cuerpo exacto con las secciones "Ideas sin editar" y "Hábitos en riesgo" (los textos de cada alerta quedan fijados en el test, en español neutro y texto plano); el tipo `StaleIdea` está en `ReminderRules.cs`.

- [ ] **T18 — Cierre de C2b** · RF: CA-1.1, CA-1.6, CA-2.1 a CA-2.4, CA-4.2
  Hecho cuando: `dotnet test` y `npm test` pasan; los tests existentes de `IdeaRules` y `OwnerClock` no se modificaron; ninguna función supera 30 líneas; no cambia ningún endpoint ni pantalla.

## Fase 3 — C3: datos y reinicio del reloj al restaurar (S-3)

- [ ] **T19 — `RestoreAsync` reinicia `UpdatedAt` (test primero)** · RF: S-3 · Plan: D-T13
  Hecho cuando: en `IdeaServiceTests.cs` el test `Restore_KeepsTheTextTagsAndDates` se renombra a `Restore_KeepsTextTagsAndCreatedAt_AndResetsUpdatedAt`, falla y luego pasa; `IdeaService.RestoreAsync(userId, id, now)` pone `UpdatedAt = now` sin cambiar `CreatedAt` ni el orden de la lista; `IdeasController` pasa `now`; el resto de los tests de restaurar pasa. **En el mismo commit** (principio 2), `docs/specs/003-anotador-de-ideas.md` CA-6.3 queda: restaurar conserva texto, etiquetas y `CreatedAt`, y reinicia `UpdatedAt`.

- [ ] **T20 — Modelos y restricciones (test primero)** · RF: RNF-3, CA-2.2 · Plan: D-T3, D-T4, §3
  Hecho cuando: `ReminderModelTests.cs` (no figura en la tabla del plan §10; se agrega porque RNF-3 y D-T4 dependen de estas restricciones) falla primero por compilación y luego se escribe lo mínimo para que compile: `Models/DailyDigestLog.cs` (`ForDate` clave primaria no generada, `SentAt`), `Models/ReminderLog.cs` (`Id`, `IdeaId`, `SentOn`), su configuración en `Data/AppDbContext.cs` (FK a `Ideas` con cascada, índice único `(IdeaId, SentOn)`) y el borrado de `ReminderLogs` y `DailyDigestLogs` en `TestDatabase.CreateEmptyContextAsync`. Los tests cubren: segundo `DailyDigestLog` con la misma fecha falla, segundo `ReminderLog` con el mismo `(IdeaId, SentOn)` falla, y borrar la idea borra sus `ReminderLog`. Siguen en rojo hasta T21.

- [ ] **T21 — Migración `AddReminders`** · RF: RNF-3 · Plan: §3
  Hecho cuando: `dotnet ef migrations add AddReminders` genera la migración y se revisa que solo crea `DailyDigestLogs` y `ReminderLogs` (sin tocar `Ideas`, `Habits` ni `Users`); se aplica a `asistente_dev`; los tests de T20 pasan; y `dotnet test` completo pasa.

- [ ] **T22 — Cierre de C3** · RF: RNF-3, S-3
  Hecho cuando: `dotnet test` y `npm test` pasan; `git diff` muestra la spec 003 cambiada en el mismo commit que `RestoreAsync`; la migración generada va marcada para revisarse aparte; y la lista de ideas se ve igual en el navegador (el frontend no muestra `UpdatedAt`).

## Fase 4 — C4a: servicio con remitente falso

Se entrega en dos commits (ver "Entrega"): **C4a-1** (T23 a T25) y **C4a-2** (T26 a T30).

- [ ] **T23 — Contratos y `FakeEmailSender`** · RF: CA-1.4 · Plan: D-T6, §5
  Hecho cuando: existen `Services/IEmailSender.cs` (con `EmailMessage(To, Subject, Text, IdempotencyKey)` y `SendAsync` que devuelve `bool`), los tipos `ReminderResult` (`Sent`, `Skipped`, `Failed`) y `ReminderOutcome(Result, Items)`, y `FakeEmailSender` en el proyecto de tests (escrito a mano, guarda los mensajes y puede configurarse para devolver `false`); todo compila y `dotnet test` sigue en verde.

- [ ] **T24 — Envío a las 8:00 al correo del dueño (test primero)** · RF: CA-1.1, CA-1.5, RNF-3 · Plan: §4.3
  Hecho cuando: en `ReminderServiceTests.cs` (base `asistente_test`) pasan `SendsAt8_SkipsAt759` (7:59 no envía y no toca la base; 8:00 con una idea estancada envía) y `SendsToOwnerEmail`; `ReminderService.SendDailyAsync(utcNow, zone)` consulta ideas activas, reserva `DailyDigestLog` y `ReminderLog` dentro de una transacción, envía y confirma.

- [ ] **T25 — Un solo resumen por día y nada que reportar (test primero)** · RF: CA-1.2, CA-1.3 · Plan: §4.3
  Hecho cuando: pasan `SecondRunSameDay_Skips` (la segunda ejecución del día devuelve `Skipped` y no envía), `NothingToReport_SkipsWithoutEmailOrDigestRow` (no se envía y **no se crea** `DailyDigestLog`) y un caso sin fila de dueño → `Skipped`. Cierra C4a-1: `dotnet test` y `npm test` pasan.

- [ ] **T26 — Memoria de ideas en la base (test primero)** · RF: CA-2.2, CA-2.5, CA-2.6 · Plan: D-T3
  Hecho cuando: pasan `ArchivedIdea_IsNotIncluded`, `RemindedIdea_ReturnsAfter14Days` (recordada hoy no vuelve a los 13 días, sí a los 14), `Sending_KeepsUpdatedAtAndVersion` (tras enviar, `UpdatedAt` y `Version` de la idea no cambian) y el orden con la idea más antigua primero.

- [ ] **T27 — Alertas de hábitos desde la base (test primero)** · RF: CA-3.1 a CA-3.6 · Plan: D-T10
  Hecho cuando: pasan `HabitAlert_ArrivesInDigest` (un diario con ayer sin registro entra), `DeletedHabit_IsNotIncluded` (CA-3.6) y `OnlyLast14DaysOfLogs_GiveSameAlertsAsFullHistory` (la consulta carga solo registros desde `today − 14` y el resultado es igual al del historial completo); los `items` cuentan ideas más hábitos distintos.

- [ ] **T28 — El envío falla y no deja rastro (test primero)** · RF: CA-1.4 · Plan: D-T5
  Hecho cuando: pasa `SendFails_LeavesNoRows_AndNextRunSends`: con `FakeEmailSender` devolviendo `false` el resultado es `Failed`, no queda ninguna fila en `DailyDigestLogs` ni en `ReminderLogs`, y la ejecución siguiente del mismo día envía.

- [ ] **T29 — Ejecución simultánea (test primero)** · RF: CA-1.3, RNF-3 · Plan: D-T5
  Hecho cuando: pasa `ConcurrentRun_UniqueViolation_Skips`: con `BeforeSaveInterceptor` (ya existe en el proyecto de tests) que inserta `DailyDigestLog` del mismo día antes del guardado, el resultado es `Skipped` y no se envía correo.

- [ ] **T30 — REFACTOR y cierre de C4a** · RF: RF-1, RNF-9 · Plan: §4.3
  Hecho cuando: `ReminderService` se parte en `LoadStaleIdeasAsync`, `LoadHabitAlertsAsync` y `ReserveAndSendAsync`, cada uno de 30 líneas o menos; ningún test cambia; `dotnet test` y `npm test` pasan; y ningún endpoint ni migración nueva (el servicio aún no se expone).

## Fase 5 — C4b: Resend, endpoint, configuración y enmiendas documentales

- [ ] **T31 — Controlador: rechazo sin secreto (test primero)** · RF: CA-4.2, CA-4.3 · Plan: §6
  Hecho cuando: en `RemindersControllerTests.cs` (controlador instanciado con `DefaultHttpContext`, sin sesión) pasa `MissingOrWrongSecret_Returns401_AndSendsNothing`: sin cabecera y con cabecera incorrecta responde 401 con cuerpo vacío y el remitente falso no se llamó. Existen `Controllers/RemindersController.cs` con `[AllowAnonymous]` explícito y `Services/ReminderSecret.cs`.

- [ ] **T32 — Controlador: respuestas 200 y 502 (test primero)** · RF: CA-4.1, CA-4.4 · Plan: D-T8
  Hecho cuando: pasan `ValidSecret_Returns200`, `Body_HasOnlyResultAndItems` (el cuerpo tiene solo `result` e `items`, sin texto de ideas ni nombres de hábitos; `result` es `sent`, `skipped` o `failed`) y `Failed_Returns502`; el controlador solo valida el secreto y delega en `ReminderService` (principio 3).

- [ ] **T33 — `ResendEmailSender` (test primero)** · RF: CA-5.3, CA-1.4, RNF-2, RNF-7 · Plan: D-T6, §5
  Hecho cuando: en `ResendEmailSenderTests.cs` (agregado al plan §10: se usa un `HttpMessageHandler` escrito a mano, sin paquetes, porque D-T6 promete "nunca lanza" y sin test esa promesa no se verifica) pasan: el cuerpo JSON es `{ from, to: [to], subject, text }` con remitente `onboarding@resend.dev`; respuesta 2xx → `true`; respuesta 4xx/5xx → `false`; `HttpRequestException` → `false`; tiempo agotado → `false`. La cabecera `Idempotency-Key` se envía solo si T04 confirmó soporte.

- [ ] **T34 — Configuración y DI en `Program.cs`** · RF: RNF-2, CA-4.3 · Plan: §5, §7.1
  Hecho cuando: `Program.cs` valida `Resend__ApiKey` (obligatoria) y `Reminders__Secret` (obligatoria, 32 caracteres o más), registra `ReminderService`, `ReminderSecret` (singleton) y `AddHttpClient<IEmailSender, ResendEmailSender>` (`BaseAddress` `https://api.resend.com/`, `Bearer`, `Timeout` 15 s); `appsettings.json` sube `System.Net.Http.HttpClient` a `Warning`. Verificación manual: la API no arranca sin cada variable ni con un secreto corto, y arranca con ambas; `dotnet test` pasa.

- [ ] **T35 — Verificación local de punta a punta** · RF: CA-1.1 a CA-1.5, CA-4.1 a CA-4.4, RNF-2 · Plan: §7.4 paso 7
  Hecho cuando: en `asistente_dev`, con una idea de 14 días o más y la hora local del dueño en 8:00 o más: `curl -X POST` sin cabecera → 401 con cuerpo vacío y sin `X-Refreshed-Token`; con la cabecera correcta → `200 {"result":"sent","items":1}` y el correo llega al Gmail; una segunda llamada → `skipped`; con una clave de Resend inválida → 502 `failed`, `DailyDigestLogs` vacía, y ni la consola ni los logs muestran la clave ni el cuerpo de la respuesta.

- [ ] **T36 — Enmienda S-4: constitución y spec 002** · RF: CA-4.3, S-4 · Plan: §9
  Hecho cuando: `docs/constitution.md` principio 5 dice que todo endpoint salvo el login exige autenticación, **excepto `POST /api/reminders/daily`, protegido por un secreto compartido en `X-Reminder-Secret`** (spec 005, S-4); `docs/specs/002-login.md` tiene la misma excepción con enlace a la spec 005. Va **en el mismo commit** que el endpoint (T31, T32).

- [ ] **T37 — `docs/decisions.md`: entrada de Resend** · RF: RNF-6, RNF-7, CA-5.3 · Plan: §9
  Hecho cuando: la entrada explica qué resuelve Resend (servicio por `HttpClient`, sin paquete), por qué no Gmail SMTP (bloqueado en hosting gratuito, contraseña de aplicación) ni la API de Gmail (OAuth y renovación de tokens), el tratamiento de datos (el texto de las ideas pasa por Resend; retención según T04), los límites del plan gratuito y las alternativas descartadas.

- [ ] **T38 — `CLAUDE.md` y `AGENTS.md`: endpoint, variables, servicios y modelos** · RF: CA-4.3, RNF-2 · Plan: §9
  Hecho cuando: ambos agregan `POST /api/reminders/daily` y su excepción, las variables `Resend__ApiKey`, `Reminders__Secret` y `Cors__AllowedOrigin` (las de frontend `VITE_*` esperan a T53), los servicios `ReminderService`, `ReminderRules`, `ReminderText`, `ResendEmailSender` y los modelos `DailyDigestLog` y `ReminderLog`; y `diff CLAUDE.md AGENTS.md` solo muestra las diferencias de nombre de agente. No se toca la sección "Deployment".

- [ ] **T39 — Cierre de C4b** · RF: RF-1, RF-4
  Hecho cuando: `dotnet test` y `npm test` pasan; cada fila de CA-1.x a CA-4.x de la matriz del plan §11 tiene su test o su verificación (T35); la constitución, la spec 002 y el endpoint están en el mismo commit; y no hay publicación todavía.

## Fase 6 — C5: publicación (BLOQUEADA)

**Bloqueo:** `CLAUDE.md` ("Próximas decisiones") exige revisar el límite de intentos de login y el token en `localStorage` antes de publicar, y la spec 005 no los trata. Ninguna tarea de esta fase empieza hasta cerrar T40. Esta tarea documenta el bloqueo; **no lo resuelve**.

- [ ] **T40 — BLOQUEO: decisión del dueño sobre login y `localStorage`** · RF: plan §13, spec 002
  Hecho cuando: el dueño decide por escrito si acepta los dos riesgos tal como están o los resuelve antes de publicar, y la decisión queda registrada en esta tarea. Si decide resolverlos, eso es una spec nueva y no entra en la 005. El agente no decide ni implementa nada aquí.

- [ ] **T41 — BLOQUEADA · Verificar backend y contenedor** · RF: CA-5.2, CA-5.5 · Plan: §13
  Hecho cuando: se anotan los datos vigentes de: etiquetas `mcr.microsoft.com/dotnet/sdk:10.0` y `aspnet:10.0` y si la imagen trae datos de zonas horarias; el nombre vigente en ASP.NET Core 10 para vaciar las redes conocidas de `ForwardedHeadersOptions` (`KnownNetworks` podría estar obsoleto); la variable de puerto que usa Render (`PORT`) y su mapa a `ASPNETCORE_HTTP_PORTS`; y las horas gratuitas de Render y de cómputo de Neon frente a ~24 despertares diarios.

- [ ] **T42 — BLOQUEADA · Verificar frontend y Actions** · RF: CA-5.1, CA-5.4, CA-5.6 · Plan: §13
  Hecho cuando: se anotan las versiones vigentes de `checkout`, `setup-node`, `upload-pages-artifact` y `deploy-pages`, y que Pages gratuito exige repositorio público para la cuenta del dueño.

- [ ] **T43 — BLOQUEADA · CORS y cabeceras reenviadas en `Program.cs`** · RF: CA-5.5 · Plan: D-T14, D-T15, §7.1
  Hecho cuando: el orden del pipeline es `UseForwardedHeaders` → `UseHttpsRedirection` → `UseCors` → autenticación → sesión → autorización → controladores; la política CORS usa `Cors__AllowedOrigin`, métodos `GET, POST, PUT, DELETE`, cabeceras `Authorization`, `Content-Type` y `X-Reminder-Secret`, y expone `X-Refreshed-Token`. No hay prueba automática (`WebApplicationFactory` exige un paquete nuevo, principio 4); verificación manual con `curl` y `Origin`: el origen configurado recibe `Access-Control-Allow-Origin`, otro origen no, y la verificación previa (`OPTIONS`) pasa; `dotnet test` pasa.

- [ ] **T44 — BLOQUEADA · `apiUrl` en `http.js` (test primero)** · RF: CA-5.6 · Plan: §7.2
  Hecho cuando: en `frontend/src/api/http.test.js` fallan y luego pasan los tests de `apiUrl(path, base)` con base vacía (ruta relativa), con base y con barra final; `http.js` lee `import.meta.env?.VITE_API_URL` y `request` usa `apiUrl`; `authApi`, `habitsApi` e `ideasApi` no cambian; los tests existentes pasan sin modificarlos.

- [ ] **T45 — BLOQUEADA · `vite.config.js` con `base`** · RF: CA-5.1, CA-5.6 · Plan: §7.2
  Hecho cuando: `base` es `process.env.VITE_BASE_PATH ?? "/"`; con `VITE_API_URL=https://ejemplo.invalid VITE_BASE_PATH=/repo/ npm run build` la URL aparece en `dist/` y los recursos cuelgan de `/repo/`; sin variables, la URL no aparece y la app sigue funcionando en desarrollo con el proxy; `npm test` pasa.

- [ ] **T46 — BLOQUEADA · `backend/Dockerfile` y `.dockerignore`** · RF: CA-5.2 · Plan: D-T16
  Hecho cuando: el Dockerfile multietapa (SDK para publicar, `aspnet` para correr) construye; si Docker está disponible en la máquina del dueño, el contenedor arranca con las variables de entorno y `OWNER_TIME_ZONE` no falla; si no, esta comprobación pasa al primer despliegue (T49) y se anota.

- [ ] **T47 — BLOQUEADA · Flujos `pages.yml` y `daily-reminder.yml`** · RF: CA-5.1, CA-5.4, RNF-5 · Plan: §7.3
  Hecho cuando: `pages.yml` (push a `main` y manual: `npm ci`, `npm test`, `npm run build` con `VITE_API_URL` desde `vars.API_URL` y `VITE_BASE_PATH`, subida y despliegue de `frontend/dist`) y `daily-reminder.yml` (cron `17 * * * *` y manual; un `curl --fail-with-body --retry 2 --retry-delay 30 --retry-all-errors --max-time 90` con `X-Reminder-Secret` desde `secrets.REMINDERS_SECRET`, sin `set -x`) existen con las versiones de T42; el secreto no aparece escrito en ningún archivo.

- [ ] **T48 — BLOQUEADA · Migraciones a Neon** · RF: CA-5.2 · Plan: D-T17, §7.4 pasos 1 y 2
  Hecho cuando: con la conexión directa de Neon (T02) se corre `dotnet ef database update --connection …` desde la máquina del dueño y las tablas de `AddIdeas`, `AddReminders` y las anteriores existen; el comando se corre **antes** de desplegar el código que las usa; la cadena no queda en el historial de comandos compartido ni en el repositorio.

- [ ] **T49 — BLOQUEADA · Servicio en Render** · RF: CA-5.2, RNF-2 · Plan: §7.4 paso 4
  Hecho cuando: el servicio web gratuito desde el repositorio (entorno Docker, raíz `backend/`) tiene las variables `ConnectionStrings__Default`, `Jwt__SigningKey`, `OWNER_TIME_ZONE`, `Resend__ApiKey`, `Reminders__Secret`, `Cors__AllowedOrigin` y la del puerto (T41); `GET /api/auth/status` responde; y queda anotado el tiempo real de arranque en frío.

- [ ] **T50 — BLOQUEADA · Crear la cuenta del dueño de inmediato** · RF: spec 002 · Plan: §7.4 paso 5, §13
  Hecho cuando: justo después de T49, `curl` a `POST /api/auth/setup` crea la cuenta y `GET /api/auth/status` indica que existe; mientras no se haga, cualquiera con la URL puede reclamarla.

- [ ] **T51 — BLOQUEADA · Configuración de GitHub y primer despliegue de Pages** · RF: CA-5.1, CA-5.4 · Plan: §7.4 paso 6
  Hecho cuando: existen la variable `API_URL` y el secreto `REMINDERS_SECRET`, Pages usa el origen "GitHub Actions" y `pages.yml` corre con éxito.

- [ ] **T52 — BLOQUEADA · D-2 y criterios de finalización con la URL publicada** · RF: CA-4.2, CA-5.1 a CA-5.5, D-2, spec §9 · Plan: §7.4 paso 7
  Hecho cuando: `curl -X POST …/api/reminders/daily` sin cabecera responde 401; `workflow_dispatch` de `daily-reminder.yml` termina con éxito y llega un correo real a la bandeja principal del Gmail del dueño (reconfirma D-2 de T03); el inicio de sesión desde la URL de Pages funciona con el token renovado (CORS y `X-Refreshed-Token`); y la URL de la API no está escrita en el repositorio.

- [ ] **T53 — BLOQUEADA · Documentar la publicación** · RF: CA-5.1, CA-5.2, S-5, S-7 · Plan: §9
  Hecho cuando: `CLAUDE.md` y `AGENTS.md` (iguales salvo la cabecera) describen el deployment real (GitHub Pages en lugar de Vercel, Render con Docker, Neon, Resend, GitHub Actions), agregan `VITE_API_URL` y `VITE_BASE_PATH`, cierran "Elegir plataforma de deployment para backend" y avisan de S-7 (flujos programados desactivados tras 60 días sin actividad en repositorio público); el resultado del despliegue queda anotado en el plan. Va en el mismo commit que C5.

## Fase 7 — Cierre

- [ ] **T54 — Verificación final** · RF: todos
  Hecho cuando: `dotnet test` y `npm test` pasan; cada fila de la matriz del plan §11 tiene su test o su verificación; los criterios de finalización de la spec §9 están cumplidos (las filas de publicación dependen de T40); no hay librerías nuevas en `backend/*.csproj` ni en `frontend/package.json` (RNF-7); y los textos del correo y del endpoint están en español neutro (RNF-8).

- [ ] **T55 — Recorrido del dueño** · RF: RF-1 a RF-5
  Hecho cuando: el dueño recibe el resumen real en su Gmail, lo lee sin ayuda, confirma que las ideas y las alertas de hábitos coinciden con lo que ve en la app, y que el correo no llegó a spam.

---

## Entrega

Presupuesto de revisión: 400 líneas cambiadas por commit (estimaciones a mano alzada, sin la migración generada). Cada commit deja `dotnet test` y `npm test` en verde y se puede revertir solo. No cambia el comportamiento visible hasta C4b (endpoint) y C5 (publicación).

| Commit | Tareas | Líneas aprox. | ¿> 400? |
|---|---|---|---|
| C1a `docs: add spec 005 for daily email digest` | Spec 005 | ≈ 140 | No |
| C1b `docs: add plan 005 for daily email digest` | Plan 005 | ≈ 300 | No |
| C1c `docs: add tasks 005 for daily email digest` | Este documento | ≈ 230 | No |
| C2a `feat(reminders): add habit alert rules and shared streak cases` | T05 a T11 | ≈ 380 | **Al límite.** Si pasa de 400, T10 (`LastWeekMissed`) va en su propio commit |
| C2b `feat(reminders): add idea, text, secret and clock rules` | T12 a T18 | ≈ 270 | No |
| C3 `feat(reminders): store digests and reminders, reset idea clock on restore` | T19 a T22 | ≈ 190 + ≈ 400 generadas | No (las generadas se revisan aparte) |
| C4a-1 `feat(reminders): send the digest at 8:00 with a fake sender` | T23 a T25 | ≈ 230 | No |
| C4a-2 `feat(reminders): remember ideas, add habit alerts and survive failures` | T26 a T30 | ≈ 200 | No |
| C4b `feat(reminders): send the daily digest through Resend` | T31 a T39 | ≈ 380 (código, tests, 4 documentos) | **Al límite.** Si pasa de 400, las enmiendas documentales (T36 a T38) siguen en este commit por el principio 2 y se parte `ResendEmailSender` con sus tests (T33) en un commit anterior |
| C5 `ci: publish frontend to GitHub Pages and schedule daily digest` | T40 a T53 | ≈ 230 | No. **BLOQUEADO por T40** |
| C6 `docs: close spec 005 tasks` | T54, T55 y notas | ≈ 60 | No |

**Commits que pasan de 400 líneas sin partir:** ninguno. Sí pasaban en el plan: C1 (≈ 440 + tareas), C2 (≈ 650) y C4 (≈ 750); aquí están partidos en C1a/b/c, C2a/b y C4a-1/C4a-2/C4b.

**Dependencias entre commits:** C2a → C2b → C3 → C4a-1 → C4a-2 → C4b → C5. La Fase 0 (T00 a T04) va antes de C2a y no produce commits de código; sus notas se guardan en C6.
