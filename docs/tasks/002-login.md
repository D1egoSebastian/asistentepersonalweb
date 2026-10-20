# Tareas 002 — Cuenta del dueño, inicio y cierre de sesión

Fuentes: [spec](../specs/002-login.md) · [plan](../plans/002-login.md) · [constitución](../constitution.md)

**Reglas para todas las tareas:**
- Cada tarea dura como máximo 20-30 minutos. Si se pasa, se divide antes de seguir.
- **Las tareas de lógica se hacen con TDD:** primero el test (falla), después el código (pasa).
- Ninguna tarea se marca como hecha si `dotnet test` (desde `backend/`) o `npm test` (desde `frontend/`) fallan (principio 4).
- Si una tarea cambia un comportamiento, la spec se actualiza en el mismo commit (principio 2).
- Los comandos que levantan la API necesitan `ConnectionStrings__Default` y `Jwt__SigningKey`. Las verificaciones de esta lista usan la base `asistente_test`, para no ocupar la cuenta del dueño en `asistente_dev`.

---

## Fase 0 — Preparación

- [x] **T01 — Estados `Conflict` y `Unauthorized`** · RF: RF-1, RF-2
  Hecho cuando: `ResultStatus` tiene los dos estados nuevos, `HabitsController` los traduce a 409 y 401, y `dotnet build` pasa.

- [x] **T02 — `PasswordHasher` disponible** · RF: RNF-1
  Hecho cuando: un test usa `PasswordHasher<User>` y compila. Si hace falta un paquete nuevo, está agregado y justificado en `docs/decisions.md`.

## Fase 1 — Reglas y tokens (TDD)

- [x] **T03 — `AuthRules`** · RF: RF-1 (CA-1.3, CA-1.4, CA-1.5)
  Hecho cuando: pasan los tests de email vacío, sin arroba y sin punto después de la arroba; contraseña de 11 y de 12 caracteres; y confirmación distinta, cada uno con su mensaje en español.

- [x] **T04 — `SessionTokens`** · RF: RF-2, RF-3 (CA-2.1, CA-3.2), RNF-2
  Hecho cuando: pasan los tests de que un token emitido se valida con la misma clave, falla con otra clave, falla vencido, y contiene el id del dueño; y una clave de menos de 32 caracteres se rechaza.

## Fase 2 — Datos

- [x] **T05 — Entidad `User` y migración `AddUsers`** · RF: RF-1 (CA-1.6, CA-1.7), RNF-5
  Hecho cuando: la migración crea `Users` con el índice único `lower("Email")`, está aplicada en `asistente_dev` y `asistente_test`, y `TestDatabase` limpia `Users` antes de cada test.

## Fase 3 — Servicio (TDD contra la base de tests)

- [x] **T06 — `AuthService.GetStatusAsync` y `SetupAsync`** · RF: RF-1 (CA-1.1 a CA-1.7), RNF-1
  Hecho cuando: pasan los tests de que sin cuenta el estado es "sin dueño"; crear la cuenta guarda un hash distinto de la contraseña y devuelve una sesión; un segundo intento devuelve Conflict; dos intentos a la vez dejan una sola cuenta; y los datos inválidos devuelven Invalid.

- [x] **T07 — `AuthService.LoginAsync`** · RF: RF-2 (CA-2.1, CA-2.2, CA-2.4), RNF-5
  Hecho cuando: pasan los tests de login correcto (devuelve sesión), login con el email en mayúsculas, contraseña incorrecta, email desconocido y login sin cuenta, y los tres fallos devuelven el mismo mensaje.

## Fase 4 — API

- [x] **T08 — DTOs y `AuthController`** · RF: RF-1, RF-2
  Hecho cuando: existen los 4 DTOs del plan, `GET /api/auth/status`, `POST /api/auth/setup` y `POST /api/auth/login` delegan en `AuthService`, y `dotnet build` pasa.

- [x] **T09 — JWT en `Program.cs` y renovación** · RF: RF-2, RF-3 (CA-2.5, CA-3.1, CA-3.2), RNF-2
  Hecho cuando: verificado con `curl` contra `asistente_test`: sin `Jwt__SigningKey` la API no arranca con un mensaje claro; con la clave, el estado, la creación (una vez 200, otra vez 409), el login (200 y 401), `GET /api/habits` con token (200 y encabezado `X-Refreshed-Token`) y sin token (401).

## Fase 5 — Interfaz

- [x] **T10 — `session.js`, `authApi.js` y token en `habitsApi.js`** · RF: RF-2, RF-3, RF-4 (CA-2.5, CA-3.3)
  Hecho cuando: cada petición de hábitos lleva el token guardado, el token de `X-Refreshed-Token` reemplaza al anterior, un 401 avisa a la app, y `npm test` y `npm run build` pasan.

- [x] **T11 — `SetupPage` y `LoginPage`** · RF: RF-1, RF-2
  Hecho cuando: las dos pantallas envían a la API, muestran sus errores sin borrar lo escrito, y `npm run build` pasa.

- [x] **T12 — Flujo en `App.jsx` y botón "Salir"** · RF: RF-1 a RF-4
  Hecho cuando: la app elige primer uso, inicio de sesión o hábitos según el estado; "Salir" borra el token y vuelve al inicio de sesión; un 401 vuelve al inicio de sesión con el mensaje de sesión expirada.

## Fase 6 — Cierre

- [x] **T13 — Verificación en el navegador** · RF: RF-1 a RF-4
  Hecho cuando: contra `asistente_test`, en el navegador: se crea la cuenta desde la pantalla de primer uso, se ven los hábitos, "Salir" vuelve al inicio de sesión, se vuelve a entrar con la misma cuenta, y un token alterado lleva al inicio de sesión con el mensaje de sesión expirada.

- [x] **T14 — Documentación** · RF: —
  Hecho cuando: `CLAUDE.md` y `AGENTS.md` (iguales salvo la cabecera) incluyen `User`, `/api/auth/*`, `Jwt__SigningKey` y la renovación; `docs/decisions.md` registra el riesgo del token en `localStorage`.
