# Plan 005 — Resumen diario por correo y publicación

Plan técnico de [`docs/specs/005-recordatorios-por-correo.md`](../specs/005-recordatorios-por-correo.md). Respeta [`docs/constitution.md`](../constitution.md), con la enmienda S-4 del principio 5 (sección 9). Cada parte indica entre corchetes los RF o CA que cubre.

## 1. Arquitectura en una frase

Un flujo de GitHub Actions llama cada hora a `POST /api/reminders/daily` con un secreto. El controlador solo valida el secreto y delega en `ReminderService`, que obtiene "hoy" con `OwnerClock`, arma el resumen con reglas puras (`ReminderRules`, `ReminderText`), reserva el día en `DailyDigestLog` dentro de una transacción, envía con `IEmailSender` (Resend por `HttpClient`) y confirma o deshace según el resultado del envío.

```
GitHub Actions (cron horario, curl con 2 reintentos)
   │  POST /api/reminders/daily   X-Reminder-Secret
   ▼
RemindersController ── ReminderRules.IsValidSecret (tiempo constante) ── 401 si falla
   │
   ▼
ReminderService.SendDailyAsync(utcNow, zone)
   ├─ OwnerClock.LocalTimeAt / TodayAt        (¿ya son las 8:00? ¿qué día es?)
   ├─ AppDbContext: DailyDigestLogs, Users, Ideas + ReminderLogs, Habits + HabitLogs (14 días)
   ├─ ReminderRules  (puro: ideas estancadas, alertas de hábitos, racha, semana)
   ├─ ReminderText   (puro: asunto y cuerpo en texto plano)
   ├─ BEGIN → INSERT DailyDigestLog + ReminderLog
   ├─ IEmailSender.SendAsync ── ResendEmailSender ── HttpClient → https://api.resend.com/emails
   └─ COMMIT si se envió │ ROLLBACK si falló
   ▼
200 { result: "sent" | "skipped", items } · 502 { result: "failed", items }
```

El frontend no cambia de comportamiento: solo aprende a apuntar a una API en otro origen (`VITE_API_URL`) y a vivir bajo `/<repo>/` en GitHub Pages [RF-5].

## 2. Decisiones técnicas

| ID | Decisión | Por qué | Alternativa descartada |
|---|---|---|---|
| D-T1 | Las reglas viven en **`ReminderRules`** (estáticas, sin base ni reloj) y los textos en **`ReminderText`**. `ReminderService` solo consulta, llama a las reglas y persiste. | RNF-9 y principio 3. Mismo patrón que `HabitRules`/`IdeaRules`: las reglas se testean sin base de datos y con `today` fijo. | Reglas dentro del servicio: cada test de regla necesitaría PostgreSQL. |
| D-T2 | **"Hoy" y "las 8:00" salen de `OwnerClock`**: se agrega `OwnerClock.LocalTimeAt(utcNow, zone)` (devuelve `TimeOnly`). El controlador pasa `DateTimeOffset.UtcNow` y `OwnerClock.Zone()`; el servicio nunca lee el reloj. | `OwnerClock` es "el único código que decide qué día es hoy" (plan 001, D-T11). Con `utcNow` como parámetro, los tests fijan la hora y cubren el cambio de horario (spec §7). | Leer `DateTime.Now` en el servicio: depende de la zona del servidor (UTC en Render). |
| D-T3 | **La idea recordada se guarda en una tabla aparte (`ReminderLog`) y no en una columna de `Idea`**. | CA-2.5: no se toca `UpdatedAt`. Además, `Idea.Version` es el `xmin` de PostgreSQL y cambia con **cualquier** escritura de la fila: una columna `LastRemindedAt` en `Idea` cambiaría la versión y el dueño que tuviera la idea abierta recibiría un 409 falso ("Esta idea cambió mientras la editabas", spec 003). | Columna `Idea.LastRemindedAt`: rompe la detección de ediciones simultáneas. |
| D-T4 | `ReminderLog` tiene **`IdeaId` con clave foránea y borrado en cascada**, no `Kind` + `SubjectId`. | Solo las ideas necesitan memoria: las alertas de hábitos se recalculan cada día con los registros. Un `Kind` con un único valor es una abstracción "por si acaso" (principio 7), y un `SubjectId` sin clave foránea deja filas huérfanas al borrar ideas. | `ReminderLog(Kind, SubjectId, SentOn)` genérico: más flexible, pero sin integridad referencial y sin un segundo uso real. |
| D-T5 | **Una transacción explícita** alrededor de insertar `DailyDigestLog` (+ `ReminderLog`), enviar y confirmar. Si el envío falla, `ROLLBACK`. | Cumple el orden pedido por RNF-3 y CA-1.4 (la fila existe antes de enviar y desaparece si falla) sin un `DELETE` de compensación: si el proceso muere durante el envío, PostgreSQL deshace solo. Una segunda ejecución simultánea queda bloqueada en el índice único hasta que la primera confirme, y entonces recibe violación de unicidad → `skipped`. | Confirmar la fila y borrarla si falla: si el proceso muere entre el envío fallido y el borrado, el día queda marcado sin correo y no se reintenta. |
| D-T6 | **`IEmailSender`** con una sola implementación, **`ResendEmailSender`**, registrada con `AddHttpClient`. Devuelve `bool` (enviado o no) y nunca lanza por errores de red o HTTP. | RNF-7: `HttpClient` y `System.Text.Json` vienen con el framework. La interfaz existe por una razón concreta: los tests usan un `FakeEmailSender` escrito a mano (sin Moq, principio 4). | Librería de Resend o SMTP (`System.Net.Mail` con Gmail): dependencia nueva o SMTP bloqueado en hosting gratuito (CA-5.3). |
| D-T7 | **Comparación del secreto con `CryptographicOperations.FixedTimeEquals` sobre los SHA-256** del valor recibido y del configurado. | CA-4.2: tiempo constante. Comparar los hashes (32 bytes siempre) evita además filtrar la longitud del secreto. | `==` sobre `string`: corta en el primer carácter distinto. |
| D-T8 | El endpoint responde **200** con `sent` o `skipped` y **502** con `failed`. Cuerpo: `{ "result", "items" }` y nada más. | CA-4.4 y RNF-5: un fallo de envío debe terminar el flujo en error para que GitHub avise. `skipped` es un resultado normal (antes de las 8:00, ya enviado, nada que reportar). | 200 siempre con el resultado en el cuerpo: el flujo nunca fallaría y RNF-5 no avisaría. |
| D-T9 | **"El período existió completo"**: una alerta sobre ayer exige `createdOn < ayer` (CA-3.1) y una sobre la semana pasada exige `createdOn < lunes pasado` (CA-3.3). | Es la lectura literal de los casos límite de la spec ("hábito diario creado ayer: no aplica"; "no existía la semana anterior completa"). Una sola regla para los dos casos. **Difiere de `streak.js`/métricas**, donde la semana de creación sí cuenta: ver §14. | `createdOn <= ayer` / semana de creación incluida: contradice el caso límite de la spec para hábitos diarios. |
| D-T10 | El servicio carga **solo los registros de los últimos 14 días** con una consulta propia, en lugar de reutilizar `HabitService.ListAsync`. | Las reglas miran como mucho el lunes de la semana pasada (≤ 13 días). `ListAsync` trae el historial completo (hasta ~91.000 filas) y el endpoint corre cada hora hasta que haya algo que enviar; con Neon gratuito el tiempo de cómputo cuenta. | Reutilizar `ListAsync`: menos código, pero el costo crece con los años de historial. |
| D-T11 | **Casos de racha compartidos en `test-cases/daily-streak.json`** en la raíz del repo, leídos por xunit (copiado a la salida con `<None … CopyToOutputDirectory>`) y por `node --test` (`fs.readFileSync`). Cada caso: `today`, `logDates`, `dailyStreak` esperado; y casos de `weekStart`. | D-3. C# porta `dailyStreak` y `weekStart`; si una de las dos versiones cambia, el mismo JSON rompe el test de la otra. No es un paquete: no viola el principio 4. | Copiar los casos a mano en dos archivos de test: divergen en silencio. |
| D-T12 | El texto de una idea en el correo es **la primera línea con texto, cortada a 80 caracteres visibles con "…"**. `IdeaRules.Summary` se generaliza a `IdeaRules.Cut(text, maxChars)` y `Summary` la llama con 120. | CA-2.4. Reutiliza el corte por elementos de texto (no rompe emojis ni tildes) que ya tiene la lista de ideas. | Cortar `Text` con `Substring(0, 80)`: puede partir un emoji y mete saltos de línea en el correo. |
| D-T13 | **Restaurar pone `UpdatedAt = now`** (`IdeaService.RestoreAsync(userId, id, now)`). `CreatedAt` no cambia, así que el orden de la lista tampoco. | S-3. El frontend no muestra `UpdatedAt` (verificado con búsqueda en `frontend/`), así que el cambio no se ve en pantalla. | Calcular el reloj desde `ArchivedAt`: `ArchivedAt` vuelve a `null` al restaurar y la fecha se pierde. |
| D-T14 | **CORS con una sola política** cuyo origen sale de `Cors__AllowedOrigin` (`https://<usuario>.github.io`). Sin la variable, no se admite ningún origen cruzado (en desarrollo no hace falta: Vite usa proxy). | CA-5.5 sin escribir el usuario en el código. Mismo patrón de configuración por entorno que el resto. | Origen escrito en `Program.cs`: ata el código a una cuenta de GitHub. |
| D-T15 | **Cabeceras reenviadas en código** (`UseForwardedHeaders` con `X-Forwarded-For` y `X-Forwarded-Proto`, redes conocidas vaciadas), antes de `UseHttpsRedirection`. | Render termina TLS en su proxy; sin esto, la app ve `http` y `UseHttpsRedirection` redirige en bucle o avisa. En código queda visible y revisable. | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` en Render: mismo efecto, pero escondido en la configuración del servicio. |
| D-T16 | **Backend en Render con `backend/Dockerfile`** (multietapa: SDK para publicar, `aspnet` para correr). | Render no ofrece un runtime nativo de .NET; Docker es la vía gratuita. | Otro proveedor: la spec fija Render (CA-5.2). |
| D-T17 | **Migraciones aplicadas a mano** desde la máquina del dueño (`dotnet ef database update --connection …`) contra la conexión directa de Neon, antes de desplegar el commit que las trae. | El arranque en frío de Render no suma la migración, y la cadena de la app no necesita permisos de DDL. | `Database.Migrate()` al arrancar: más simple de operar, pero cada arranque en frío consulta el historial y la app corre con permisos de esquema. |
| D-T18 | **Constantes fijas en `ReminderRules`**: `StaleIdeaDays = 14`, `MinLostStreak = 3`, `IdeaPreviewChars = 80`, `DigestTime = 08:00`. El remitente `onboarding@resend.dev` es constante en `ResendEmailSender`. | S-8. | Variables de entorno: configuración sin un caso de uso. |

## 3. Modelo de datos [RNF-3, CA-2.2, CA-2.5]

| Entidad | Columnas | Restricciones |
|---|---|---|
| `DailyDigestLog` | `ForDate` (`date`), `SentAt` (`timestamptz`) | `ForDate` es la **clave primaria** (no se genera): garantiza un resumen por día sin índice extra. |
| `ReminderLog` | `Id`, `IdeaId` (FK → `Ideas`, cascada), `SentOn` (`date`) | Índice único `(IdeaId, SentOn)`. |

- Migración `AddReminders` (generada con `dotnet ef migrations add`). No toca `Ideas`, `Habits` ni `Users`.
- `TestDatabase.CreateEmptyContextAsync` borra también `ReminderLogs` y `DailyDigestLogs`.
- "Último recordatorio" de una idea = `MAX(SentOn)` de sus filas. Se guarda una fila por envío: queda el historial y la consulta es trivial.

## 4. Reglas y algoritmos [RF-1, RF-2, RF-3]

Fechas como `DateOnly` en el día del dueño. Lunes = inicio de semana (igual que `weekStart` de `dates.js`). `ayer = today.AddDays(-1)`.

### 4.1 Funciones puras

| Clase | Firma | Devuelve | CA |
|---|---|---|---|
| `OwnerClock` | `LocalTimeAt(utcNow, zone)` | Hora local del dueño (`TimeOnly`). | CA-1.1 |
| `ReminderRules` | `IsDigestTime(TimeOnly local)` | `local >= 08:00`. | CA-1.1 |
| `ReminderRules` | `IsValidSecret(string? received, string expected)` | `false` si falta; si no, `FixedTimeEquals(SHA256(received), SHA256(expected))`. | CA-4.2 |
| `ReminderRules` | `IsIdeaDue(DateOnly updatedOn, DateOnly? lastRemindedOn, DateOnly today)` | `today − max(updatedOn, lastRemindedOn) >= 14`. | CA-2.1 a CA-2.3 |
| `ReminderRules` | `DaysSince(DateOnly from, DateOnly today)` | Días sin editar. | CA-2.4 |
| `ReminderRules` | `WeekStart(DateOnly date)` | Lunes de esa semana (porta `weekStart`). | CA-3.2, CA-3.3 |
| `ReminderRules` | `DailyStreak(IReadOnlySet<DateOnly> logs, DateOnly today)` | Porta `dailyStreak` de `streak.js`. | CA-3.4, D-3 |
| `ReminderRules` | `HabitAlerts(Habit habit, IReadOnlySet<DateOnly> logs, DateOnly today)` | Lista de `HabitAlert` (0 a 2 por hábito). | CA-3.1 a CA-3.5 |
| `ReminderText` | `Subject(int ideas, int habits)` | "Tu resumen de hoy: 2 ideas y 1 hábito" (singular/plural; omite la parte vacía). | CA-1.6 |
| `ReminderText` | `Body(IReadOnlyList<StaleIdea>, IReadOnlyList<HabitAlert>)` | Texto plano en español neutro, una línea por idea y por alerta. | CA-1.6, CA-2.4, CA-3.5 |
| `IdeaRules` | `Cut(string? text, int maxChars)` | Primera línea con texto, cortada con "…". | CA-2.4 |

### 4.2 Pseudocódigo de las alertas

```
HabitAlerts(habit, logs, today):
    alerts = []
    si diario:
        si habit.CreatedOn < ayer y ayer ∉ logs:                               # CA-3.1, D-T9
            alerts += MissedYesterday
        si ayer ∉ logs:
            perdida = DailyStreak(logs, ayer)       # ayer sin registro → cuenta desde anteayer hacia atrás
            si perdida >= 3: alerts += StreakLost(perdida)                      # CA-3.4, S-1
    si semanal:
        lunes = WeekStart(today)
        hechos = |{ d ∈ logs : lunes <= d <= today }|                           # hoy cuenta si ya está marcado
        faltan = goal − hechos
        quedan = 7 − índice(today)                  # lunes = 0 → quedan 7; domingo = 6 → queda 1
        si faltan > 0 y quedan <= faltan: alerts += WeeklyAtRisk(hechos, goal, quedan)   # CA-3.2, S-2
        si today es lunes:
            pasado = lunes − 7
            si habit.CreatedOn < pasado y |logs en [pasado, pasado+6]| < goal:
                alerts += LastWeekMissed(cuenta, goal)                          # CA-3.3, D-T9
    devolver alerts                                 # CA-3.5: una línea por alerta
```

CA-3.1 y CA-3.4 pueden salir juntas para el mismo hábito ("ayer no lo cumpliste" y "perdiste una racha de 5 días"); la spec lo admite (CA-3.5). CA-3.6 sale sola: la consulta parte de la tabla `Habits`.

### 4.3 Flujo del servicio (orden de la transacción, RNF-3)

```
SendDailyAsync(utcNow, zone) → ReminderOutcome(Result, Items)
    si !IsDigestTime(OwnerClock.LocalTimeAt(utcNow, zone)): Skipped            # sin tocar la base
    today = OwnerClock.TodayAt(utcNow, zone)
    si existe DailyDigestLog(today): Skipped                                     # CA-1.3
    owner = Users.Find(User.OwnerId); si null: Skipped
    ideas  = activas del dueño + MAX(ReminderLog.SentOn) → filtrar con IsIdeaDue   # CA-2.1..2.3, 2.6
             orden: la más antigua primero
    habits = hábitos del dueño + registros desde today−14 (D-T10) → HabitAlerts
    si no hay ideas ni alertas: Skipped                                          # CA-1.2 (el día no se marca)
    BEGIN
      INSERT DailyDigestLog(today, utcNow); INSERT ReminderLog(idea, today) por idea
      SaveChanges → violación de unicidad: ROLLBACK, Skipped                    # ejecución simultánea
      ok = await email.SendAsync(mensaje a owner.Email)                          # CA-1.5
      si !ok: ROLLBACK, Failed(items)                                           # CA-1.4
    COMMIT, Sent(items)                         # items = ideas + hábitos distintos
```

Ninguna línea escribe en `Ideas`: `UpdatedAt` y `Version` no cambian (CA-2.5). El servicio se parte en métodos de ≤ 30 líneas (`LoadStaleIdeasAsync`, `LoadHabitAlertsAsync`, `ReserveAndSendAsync`), principio 7.

### 4.4 Ejemplo de correo

```
Asunto: Tu resumen de hoy: 2 ideas y 2 hábitos

Ideas sin editar
- "Aprender Rust para el backend de la app de música, empezando por…" (15 días)
- "Regalo para mamá" (28 días)

Hábitos en riesgo
- Daily coding challenge: ayer no lo cumpliste.
- Daily coding challenge: perdiste una racha de 5 días.
- Gimnasio: llevas 0 de 3 esta semana y quedan 3 días.
```

Los textos exactos se fijan en `ReminderTextTests`.

## 5. Envío [CA-1.4, CA-5.3, RNF-2, RNF-7]

```csharp
public sealed record EmailMessage(string To, string Subject, string Text, string IdempotencyKey);

public interface IEmailSender
{
    // True when the provider accepted the message. Never throws for network or HTTP errors.
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
```

- `ResendEmailSender(HttpClient http)`: `POST emails` con `{ from, to: [to], subject, text }` en JSON. El `HttpClient` se configura en `Program.cs` con `AddHttpClient<IEmailSender, ResendEmailSender>`: `BaseAddress = https://api.resend.com/`, `Authorization: Bearer <Resend__ApiKey>`, `Timeout = 15 s`.
- **Errores:** respuesta no 2xx, `HttpRequestException` o `TaskCanceledException` (tiempo agotado) → `false`. Se registra solo el código de estado; nunca el cuerpo, las cabeceras ni la clave (RNF-2). `appsettings.json` sube `System.Net.Http.HttpClient` a nivel `Warning` como defensa adicional.
- **Clave de idempotencia:** `digest-{yyyy-MM-dd}`, enviada en la cabecera `Idempotency-Key` **solo si** se confirma que Resend la admite (§13). Cubre el caso "Resend aceptó pero la respuesta no llegó".
- **Configuración (variables de entorno, nunca en el repo):**

| Variable | Uso | Validación al arrancar |
|---|---|---|
| `Resend__ApiKey` | Clave de envío de Resend. | Obligatoria; sin ella la API no arranca. |
| `Reminders__Secret` | Valor esperado en `X-Reminder-Secret`. | Obligatoria, al menos 32 caracteres (como `Jwt__SigningKey`). |
| `Cors__AllowedOrigin` | `https://<usuario>.github.io`. | Opcional (D-T14). |

En desarrollo, el dueño define las dos obligatorias igual que hoy define `Jwt__SigningKey`.

## 6. Endpoint [RF-4]

`RemindersController` (`api/reminders`), **sin `[Authorize]`** y con `[AllowAnonymous]` explícito para que la excepción S-4 se lea en el código:

```
POST daily  ([FromHeader(Name = "X-Reminder-Secret")] string? secret)
    si !ReminderRules.IsValidSecret(secret, reminderSecret.Value): return Unauthorized()   # cuerpo vacío
    outcome = await reminders.SendDailyAsync(DateTimeOffset.UtcNow, OwnerClock.Zone())
    return outcome.Result == Failed ? StatusCode(502, body) : Ok(body)    # body = { result, items }
```

- El secreto llega por inyección como `ReminderSecret(string Value)`, registrado como singleton en `Program.cs` (mismo patrón que `SessionTokens`).
- Sin usuario autenticado, el middleware de sesión deslizante no emite `X-Refreshed-Token` (CA-4.3).
- `result` usa los valores fijos de CA-4.4 (`sent`, `skipped`, `failed`); no hay más texto en la respuesta, así que RNF-8 no agrega mensajes.

## 7. Publicación [RF-5]

### 7.1 `Program.cs`

Orden del pipeline: `UseForwardedHeaders` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → sesión deslizante → `UseAuthorization` → `MapControllers`.

- `AddCors`: política con `WithOrigins(Cors__AllowedOrigin)`, métodos `GET, POST, PUT, DELETE`, cabeceras `Authorization`, `Content-Type` y `X-Reminder-Secret`, y `WithExposedHeaders("X-Refreshed-Token")`. `Content-Type` no figura en CA-5.5 pero es imprescindible: un `POST` con JSON dispara la verificación previa del navegador. `X-Reminder-Secret` se incluye porque lo pide CA-5.5, aunque el flujo de GitHub no es un navegador y no pasa por CORS.
- `Configure<ForwardedHeadersOptions>` (D-T15).
- Validación de `Resend__ApiKey` y `Reminders__Secret`, registro de `ReminderService`, `ReminderSecret` y el `HttpClient` de Resend.

### 7.2 Frontend

- `vite.config.js`: `base: process.env.VITE_BASE_PATH ?? "/"`. El flujo de Pages lo define como `/${{ github.event.repository.name }}/`. La app no usa enrutador (navegación por pestañas), así que no necesita `404.html`.
- `src/api/http.js`: `const API_URL = (import.meta.env?.VITE_API_URL ?? "").replace(/\/$/, "")` y una función exportada `apiUrl(path, base = API_URL)` que usa `request`. En desarrollo y en `node --test`, `API_URL` es `""` y las rutas siguen siendo relativas (proxy de Vite). `authApi`, `habitsApi` e `ideasApi` no cambian.

### 7.3 Flujos de GitHub Actions

| Archivo | Disparo | Pasos |
|---|---|---|
| `.github/workflows/pages.yml` | `push` a `main` y manual | `checkout` → `setup-node` → `npm ci` → `npm test` → `npm run build` con `VITE_API_URL: ${{ vars.API_URL }}` y `VITE_BASE_PATH` → `upload-pages-artifact` (`frontend/dist`) → `deploy-pages`. Permisos `pages: write`, `id-token: write`. |
| `.github/workflows/daily-reminder.yml` | `schedule: cron "17 * * * *"` y `workflow_dispatch` | Un paso: `curl --fail-with-body --retry 2 --retry-delay 30 --retry-all-errors --max-time 90 -X POST -H "X-Reminder-Secret: ${{ secrets.REMINDERS_SECRET }}" "${{ vars.API_URL }}/api/reminders/daily"`. Sin `set -x`. |

- Minuto 17 y no 0: el cron de GitHub corre en UTC y se demora más en las horas en punto (§13). La regla de las 8:00 se evalúa en el servidor con la zona del dueño, así que el horario de verano no afecta el cron.
- `--retry 2` cubre el arranque en frío de Render (CA-5.4). Reintentar un `502` es seguro: la transacción garantiza que no hay doble envío.
- `API_URL` es una **variable** del repositorio (no es secreta: queda dentro del JS publicado); `REMINDERS_SECRET` es un **secreto**.

### 7.4 Pasos de despliegue (orden importante)

1. **Neon:** crear el proyecto y la base. Ejecutar `CREATE EXTENSION IF NOT EXISTS unaccent;` para resolver D-1 antes de seguir. Convertir la URL que entrega Neon al formato de Npgsql (`Host=…;Database=…;Username=…;Password=…;SSL Mode=Require`).
2. **Migraciones:** desde la máquina del dueño, contra la conexión **directa** (no la agrupada) de Neon (D-T17).
3. **Resend:** cuenta creada **con el mismo Gmail del dueño** (el remitente de prueba solo entrega a esa dirección, §13) y clave con permiso solo de envío.
4. **Render:** servicio web gratuito desde el repo, entorno Docker, raíz `backend/`. Variables: `ConnectionStrings__Default`, `Jwt__SigningKey`, `OWNER_TIME_ZONE`, `Resend__ApiKey`, `Reminders__Secret`, `Cors__AllowedOrigin`, `ASPNETCORE_HTTP_PORTS` (puerto que espera Render, §13).
5. **Crear la cuenta del dueño de inmediato** con `curl` a `POST /api/auth/setup` y comprobar `GET /api/auth/status`. Mientras la cuenta no exista, cualquiera que conozca la URL puede reclamarla (§13).
6. **GitHub:** variable `API_URL`, secreto `REMINDERS_SECRET`, Pages con origen "GitHub Actions"; correr `pages.yml`.
7. **Verificación final (criterios §9 de la spec):** `curl -X POST …/api/reminders/daily` sin cabecera → 401; `workflow_dispatch` de `daily-reminder.yml` → correo real en Gmail (D-2); inicio de sesión desde la URL de Pages (CORS y `X-Refreshed-Token`).

## 8. Archivos

| Archivo | Acción | Responsabilidad | Spec |
|---|---|---|---|
| `backend/AsistentePersonal/Services/OwnerClock.cs` | Modificar | `LocalTimeAt` (D-T2). | CA-1.1 |
| `Services/ReminderRules.cs` | Crear | Constantes y reglas de §4.1; tipos `HabitAlert`, `StaleIdea`, `AlertKind`. | RF-2, RF-3, CA-4.2 |
| `Services/ReminderText.cs` | Crear | Asunto y cuerpo. | CA-1.6, CA-2.4 |
| `Services/ReminderService.cs` | Crear | Flujo de §4.3; `ReminderOutcome`, `ReminderResult`. | RF-1 |
| `Services/IEmailSender.cs`, `Services/ResendEmailSender.cs` | Crear | §5. | CA-1.4, CA-5.3 |
| `Services/IdeaRules.cs` | Modificar | `Cut` (D-T12). | CA-2.4 |
| `Services/IdeaService.cs`, `Controllers/IdeasController.cs` | Modificar | `RestoreAsync(…, now)` (D-T13). | S-3 |
| `Models/DailyDigestLog.cs`, `Models/ReminderLog.cs` | Crear | §3. | RNF-3, CA-2.2 |
| `Data/AppDbContext.cs`, `Data/Migrations/*_AddReminders*` | Modificar / Crear | Configuración y migración. | RNF-3 |
| `Controllers/RemindersController.cs`, `Services/ReminderSecret.cs` | Crear | §6. | RF-4 |
| `Program.cs`, `appsettings.json` | Modificar | §5 y §7.1. | RF-4, RF-5 |
| `backend/Dockerfile`, `backend/.dockerignore` | Crear | D-T16. | CA-5.2 |
| `backend/AsistentePersonal.Tests/*` | Crear o modificar | §10; `.csproj` copia `test-cases/daily-streak.json`. | Todos |
| `test-cases/daily-streak.json` | Crear | D-T11. | D-3 |
| `frontend/src/logic/streakCases.test.js` | Crear | Lee el JSON y prueba `dailyStreak` y `weekStart`. | D-3 |
| `frontend/src/api/http.js`, `http.test.js` | Modificar | `apiUrl` (§7.2). | CA-5.6 |
| `frontend/vite.config.js` | Modificar | `base`. | CA-5.1 |
| `.github/workflows/pages.yml`, `daily-reminder.yml` | Crear | §7.3. | CA-5.1, CA-5.4 |

## 9. Enmiendas documentales (en el commit del código que las vuelve ciertas)

| Documento | Cambio | Commit |
|---|---|---|
| `docs/constitution.md` | Principio 5: "Todo endpoint salvo el login exige autenticación, **excepto `POST /api/reminders/daily`, protegido por un secreto compartido en `X-Reminder-Secret`** (spec 005, S-4)". | Endpoint (C4) |
| `docs/specs/002-login.md` | Nota en CA-3.1/alcance con la misma excepción y el enlace a la spec 005. | Endpoint (C4) |
| `docs/specs/003-anotador-de-ideas.md` | CA-6.3: restaurar conserva texto, etiquetas y `CreatedAt`, y **reinicia `UpdatedAt`** (S-3). | Datos (C3) |
| `docs/decisions.md` | Entrada **Resend (servicio, sin paquete)**: qué resuelve; por qué no Gmail SMTP (bloqueado en hosting gratuito, contraseña de aplicación) ni la API de Gmail (OAuth y renovación de tokens); tratamiento de datos (RNF-6: el texto de las ideas pasa por Resend; retención según su política, §13); límites del plan gratuito; alternativas descartadas. | Endpoint (C4) |
| `CLAUDE.md` y `AGENTS.md` (iguales) | Endpoints: agregar `POST /api/reminders/daily` y la excepción. Variables de entorno: `Resend__ApiKey`, `Reminders__Secret`, `Cors__AllowedOrigin`, `VITE_API_URL`, `VITE_BASE_PATH`. Servicios: `ReminderService`, `ReminderRules`, `ReminderText`, `ResendEmailSender`. Modelos: `DailyDigestLog`, `ReminderLog`. | Endpoint (C4) |
| `CLAUDE.md` y `AGENTS.md` | Deployment: GitHub Pages (reemplaza Vercel, S-5), Render con Docker, Neon, Resend, GitHub Actions; cerrar "Elegir plataforma de deployment para backend". | Publicación (C5) |

## 10. Estrategia de tests (principio 4, TDD estricto)

Cada función de §4.1 y cada paso de §4.3 empieza con su test en rojo. Solo xunit de la plantilla y `node:test`. Fechas fijas: hoy **viernes 2026-10-16** (como `streak.test.js`) y **lunes 2026-10-19** para CA-3.3. Zona `America/La_Paz` y, para el cambio de horario, `America/Santiago`.

| Archivo | Tipo | Casos |
|---|---|---|
| `OwnerClockTests.cs` | Puro | `LocalTimeAt` en dos zonas y en el día del cambio de horario. |
| `ReminderRulesTests.cs` | Puro | `IsDigestTime` 07:59/08:00; `IsValidSecret` igual, distinto, otra longitud, `null`, vacío; `IsIdeaDue` 13/14 días, recordada hace 13/14, editada después del recordatorio; `WeekStart` y `DailyStreak` contra el JSON; cada alerta de §4.2 en positivo y en su borde negativo; CA-3.1 + CA-3.4 juntas. |
| `ReminderTextTests.cs` | Puro | Asunto: "2 ideas y 1 hábito", "1 idea", "3 hábitos"; cuerpo exacto; corte de 80 con emoji y con saltos de línea. |
| `IdeaRulesTests.cs` | Puro | `Cut`; `Summary` sigue igual. |
| `ReminderServiceTests.cs` | Base `asistente_test` + `FakeEmailSender` | §11. Usa `BeforeSaveInterceptor` para simular otra ejecución que inserta `DailyDigestLog` primero. |
| `RemindersControllerTests.cs` | Controlador instanciado con `DefaultHttpContext` | Sin cabecera → 401 y el remitente falso no se llamó; cabecera incorrecta → 401; correcta → 200 con solo `result` e `items`; `failed` → 502. |
| `IdeaServiceTests.cs` | Base | `Restore_KeepsTheTextTagsAndDates` pasa a `Restore_KeepsTextTagsAndCreatedAt_AndResetsUpdatedAt`. |
| `frontend/src/logic/streakCases.test.js` | `node --test` | Mismo JSON contra `dailyStreak` y `weekStart`. |
| `frontend/src/api/http.test.js` | `node --test` | `apiUrl` con base vacía, con base y con barra final. |

**Fuera de los tests automáticos** (verificación manual, criterios §9 de la spec): envío real a Gmail, 401 en la URL publicada, una corrida exitosa del flujo y CORS desde Pages. La comparación en tiempo constante se garantiza por construcción (`FixedTimeEquals`), no se mide con tiempos.

`dotnet test` y `npm test` pasan antes de cada commit.

## 11. Matriz de cobertura CA → test

| CA | Implementación | Test |
|---|---|---|
| CA-1.1 | `IsDigestTime`, `LocalTimeAt`, §4.3 | `ReminderRulesTests.IsDigestTime_*`, `ReminderServiceTests.SendsAt8_SkipsAt759` |
| CA-1.2 | §4.3 "nada que reportar" | `ReminderServiceTests.NothingToReport_SkipsWithoutEmailOrDigestRow` |
| CA-1.3 | PK `ForDate`, transacción | `ReminderServiceTests.SecondRunSameDay_Skips`, `ConcurrentRun_UniqueViolation_Skips` |
| CA-1.4 | `ROLLBACK` si `SendAsync` es `false` | `ReminderServiceTests.SendFails_LeavesNoRows_AndNextRunSends` |
| CA-1.5 | `owner.Email` | `ReminderServiceTests.SendsToOwnerEmail` |
| CA-1.6 | `ReminderText` | `ReminderTextTests.*` |
| CA-2.1 | `IsIdeaDue` | `ReminderRulesTests.IdeaDue_At14Days_NotAt13` |
| CA-2.2 | `IsIdeaDue`, `ReminderLog` | `ReminderRulesTests.IdeaDue_14DaysAfterLastReminder`, `ReminderServiceTests.RemindedIdea_ReturnsAfter14Days` |
| CA-2.3 | `IsIdeaDue` (máximo de fechas) | `ReminderRulesTests.IdeaEditedAfterReminder_CountsFromEdit` |
| CA-2.4 | `Cut`, `DaysSince`, `ReminderText.Body` | `IdeaRulesTests.Cut_*`, `ReminderTextTests.IdeaLine_*` |
| CA-2.5 | Tabla aparte (D-T3) | `ReminderServiceTests.Sending_KeepsUpdatedAtAndVersion` |
| CA-2.6 | Filtro `ArchivedAt == null` | `ReminderServiceTests.ArchivedIdea_IsNotIncluded` |
| CA-3.1 | `HabitAlerts` | `ReminderRulesTests.MissedYesterday_*` (incluye "creado ayer: no") |
| CA-3.2 | `HabitAlerts` | `ReminderRulesTests.WeeklyAtRisk_*` (lunes, viernes 0/3 y 1/3, domingo 0/1, meta cumplida, hoy marcado) |
| CA-3.3 | `HabitAlerts` | `ReminderRulesTests.LastWeekMissed_*` (no lunes, meta cumplida, creado esta semana, creado la semana pasada) |
| CA-3.4 | `DailyStreak`, `HabitAlerts` | `ReminderRulesTests.StreakLost_*` (3 sí, 2 no, ayer marcado no, semanal no), JSON compartido |
| CA-3.5 | Lista de alertas | `ReminderRulesTests.TwoAlerts_SameHabit`, `ReminderTextTests.OneLinePerAlert` |
| CA-3.6 | Consulta desde `Habits` | `ReminderServiceTests.DeletedHabit_IsNotIncluded` |
| CA-4.1 | `RemindersController` | `RemindersControllerTests.ValidSecret_Returns200` |
| CA-4.2 | `IsValidSecret` | `ReminderRulesTests.IsValidSecret_*`, `RemindersControllerTests.MissingOrWrongSecret_Returns401_AndSendsNothing` |
| CA-4.3 | `[AllowAnonymous]`, constitución | `RemindersControllerTests` (sin sesión), revisión de §9 |
| CA-4.4 | Cuerpo `{ result, items }` | `RemindersControllerTests.Body_HasOnlyResultAndItems` |
| CA-5.1 | `base`, `pages.yml` | Manual (URL publicada) |
| CA-5.2 | Dockerfile, Render, Neon | Manual |
| CA-5.3 | `ResendEmailSender` | Manual (correo real); `ReminderServiceTests` con el falso |
| CA-5.4 | `daily-reminder.yml` | Manual (una corrida exitosa) |
| CA-5.5 | CORS en `Program.cs` | Manual (login desde Pages, token renovado) |
| CA-5.6 | `apiUrl`, `VITE_API_URL` | `http.test.js`, manual (build sin URL en el repo) |
| S-3 | `RestoreAsync(…, now)` | `IdeaServiceTests.Restore_*_ResetsUpdatedAt` |
| D-3 | `test-cases/daily-streak.json` | `ReminderRulesTests.SharedCases_*`, `streakCases.test.js` |

## 12. Entrega

Presupuesto de revisión: 400 líneas cambiadas por commit/PR. Estimaciones a mano alzada, sin contar el código generado de la migración.

| # | Commit | Contenido | Líneas aprox. | ¿> 400? |
|---|---|---|---|---|
| C1 | `docs: add spec and plan 005 for daily email digest` | Spec 005 (≈140) y este plan (≈300). | ≈ 440 | **Sí** (solo documentación; puede partirse en spec y plan). |
| C2 | `feat(reminders): add pure reminder rules and shared streak cases` | `ReminderRules`, `ReminderText`, `IdeaRules.Cut`, `OwnerClock.LocalTimeAt`, JSON compartido, tests de reglas y de texto, `streakCases.test.js`, `.csproj`. | ≈ 650 | **Sí.** Partir en **C2a** hábitos (alertas, racha, semana, JSON, ≈ 380) y **C2b** ideas y texto (≈ 270). |
| C3 | `feat(reminders): store digests and reminders, reset idea clock on restore` | Modelos, `AppDbContext`, migración `AddReminders`, `TestDatabase`, `RestoreAsync` (S-3) con su test, spec 003. | ≈ 130 + ≈ 400 generadas | No (las generadas se revisan aparte). |
| C4 | `feat(reminders): send the daily digest through Resend` | `ReminderService` y sus tests, `IEmailSender`, `ResendEmailSender`, `FakeEmailSender`, controlador y tests, `Program.cs` (configuración y DI), `appsettings.json`, constitución, spec 002, `decisions.md`, `CLAUDE.md`/`AGENTS.md` (endpoints y variables). | ≈ 750 | **Sí.** Partir en **C4a** servicio con remitente falso y tests (≈ 430, al límite) y **C4b** Resend, endpoint, `Program.cs` y enmiendas (≈ 320). |
| C5 | `ci: publish frontend to GitHub Pages and schedule daily digest` | CORS y cabeceras reenviadas, Dockerfile, `vite.config.js`, `http.js` y su test, dos flujos, `CLAUDE.md`/`AGENTS.md` (deployment), resultado del despliegue en este plan. | ≈ 230 | No. |

Cada commit deja `dotnet test` y `npm test` en verde y no cambia el comportamiento visible hasta C4b (endpoint) y C5 (publicación).

## 13. Riesgos y verificaciones pendientes

Nada de esta sección está verificado; cada punto tiene su tarea antes del commit que lo necesita.

- **Publicar antes de cerrar los riesgos de la spec 002 (bloquea C5):** `CLAUDE.md` pide revisar el límite de intentos de login y el token en `localStorage` **antes de publicar**. La spec 005 no los trata. El dueño debe decidir si se aceptan o se resuelven antes de C5.
- **Cuenta reclamada por un tercero:** con la base vacía, `POST /api/auth/setup` está abierto. El paso 5 de §7.4 minimiza la ventana; no la elimina.
- **D-1 `unaccent` en Neon:** por verificar en el paso 1 de §7.4. Si no está disponible, la migración `AddIdeas` falla y hay que replantear el proveedor o la búsqueda.
- **D-2 spam:** el primer correo desde `onboarding@resend.dev` puede caer en spam. Por verificar en el paso 7.
- **S-7 flujos desactivados:** GitHub desactiva los flujos programados de repos públicos tras 60 días sin actividad; el único aviso es que el correo deja de llegar. **S-6:** no hay otro monitoreo que el correo de fallo de GitHub; un `skipped` permanente (por ejemplo, reglas mal calibradas) no avisa a nadie.
- **Repositorio público obligatorio:** GitHub Pages gratuito exige repo público (por verificar para la cuenta del dueño). El código no tiene secretos, pero las specs y docs quedan públicas.
- **Resend (por verificar):** que el remitente de prueba solo entregue a la dirección de la cuenta de Resend; límites del plan gratuito; que no pida tarjeta (RNF-1); retención de los correos enviados (RNF-6); soporte de la cabecera `Idempotency-Key`. Sin idempotencia, un envío aceptado cuya respuesta se pierde se repite en la hora siguiente.
- **Render (por verificar):** que el plan gratuito no pida tarjeta; la variable de puerto que espera (`PORT`) y cómo mapearla a `ASPNETCORE_HTTP_PORTS`; tiempo real de arranque en frío; horas gratuitas al mes frente a ~24 despertares diarios.
- **Neon (por verificar):** horas de cómputo del plan gratuito con una consulta por hora; formato de la cadena para Npgsql (SSL, SNI); usar la conexión directa para migraciones y cuál para la app.
- **Imagen Docker (por verificar):** etiquetas `mcr.microsoft.com/dotnet/sdk:10.0` y `aspnet:10.0`, y que la imagen incluya los datos de zonas horarias. Si faltan, `OWNER_TIME_ZONE` falla y la API no arranca (falla visible, no silenciosa).
- **ASP.NET Core 10 (por verificar):** el nombre vigente de las propiedades para vaciar las redes conocidas en `ForwardedHeadersOptions` (`KnownNetworks` podría estar marcado como obsoleto) y que `AddHttpClient` no redacte o sí las cabeceras en los registros.
- **Vite (por verificar):** que `import.meta.env?.VITE_API_URL` se reemplace en la compilación igual que `import.meta.env.VITE_API_URL`; se comprueba buscando la URL en `dist/` dentro del flujo de Pages. Rutas absolutas a `public/` dentro de JSX no se reescriben con `base`.
- **Acciones de GitHub (por verificar):** versiones actuales de `checkout`, `setup-node`, `upload-pages-artifact` y `deploy-pages`; demora real del cron horario (RNF-4 admite hasta ~9:30).
- **Primer correo largo:** al publicar, todas las ideas con más de 14 días entran juntas. La spec no pone tope; si son muchas, el dueño decidirá si agregar uno (cambio de spec).
- **Transacción abierta durante el envío:** hasta 15 s con una conexión ocupada. Aceptable para un solo usuario; con el agrupador de conexiones de Neon conviene confirmarlo en el primer despliegue.

## 14. Dudas abiertas

Ninguna bloquea el diseño; conviene que el dueño confirme dos lecturas antes de las tareas:

- **D-T9:** "existir durante el período" se lee como "creado antes de que empiece". Así, CA-3.3 no avisa de un hábito semanal creado el miércoles de la semana pasada, aunque la pestaña "Métricas" sí cuente esa semana.
- **D-T4:** `ReminderLog` guarda `IdeaId` con clave foránea, no `Kind` + `SubjectId`. Si el dueño prevé recordatorios de otro tipo, se revisa cuando exista ese caso.
