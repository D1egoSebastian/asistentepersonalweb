# Plan 002 — Cuenta del dueño, inicio y cierre de sesión

Plan técnico de [`docs/specs/002-login.md`](../specs/002-login.md). Respeta [`docs/constitution.md`](../constitution.md). Cada parte indica entre corchetes los RF que cubre.

## 0. Decisiones principales

| Tema | Decisión | Alternativa descartada |
|---|---|---|
| Dueño único | Una sola fila en `Users` con `Id = 1`. La creación inserta ese `Id` fijo, así que dos intentos a la vez no pueden crear dos cuentas (CA-1.7). | Contar filas antes de insertar: tiene una carrera entre dos peticiones. |
| Hash de la contraseña | `PasswordHasher<User>` de ASP.NET Core Identity (PBKDF2 con sal). | Escribir un hash propio: es código de seguridad que no conviene mantener a mano. |
| Sesión | JWT firmado con `Jwt__SigningKey` (variable de entorno, RNF-2). Dura 7 días desde el último uso. | Sesión guardada en el servidor: exige una tabla de sesiones activas, que la spec deja fuera. |
| Renovación (CA-2.5) | Cada respuesta autenticada trae un token nuevo en el encabezado `X-Refreshed-Token`. El cliente lo guarda y reemplaza el anterior. | Renovar solo una vez por día: un uso dentro de ese día no alargaría la sesión, y la spec dice que cada uso sí. |
| Dónde guarda el cliente el token | `localStorage`, bajo una clave fija. Sobrevive a cerrar el navegador (CA-2.1). | `sessionStorage`: se borra al cerrar la pestaña. Cookie `HttpOnly`: más segura frente a XSS, pero cambia el modelo de autenticación que fija `CLAUDE.md`; se revisa si la app se publica. |
| Límite de intentos (RNF-3) | No se implementa. Queda como riesgo aceptado, con una nota para revisarlo antes de publicar. | Bloqueo temporal: la spec lo descarta explícitamente. |
| Cerrar sesión (D-2) | Solo en el cliente: borra el token de `localStorage`. No hay endpoint de logout. | Lista de tokens revocados: exige guardar estado en el servidor. |
| Paquetes nuevos | Ninguno, si `PasswordHasher` viene con el framework. Si no, se agrega `Microsoft.Extensions.Identity.Core` y se justifica en `decisions.md`. Se verifica al compilar. | — |

## 1. Archivos

### 1.1 Documentación

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `docs/decisions.md` | Modificar | Justificar `PasswordHasher` solo si requiere paquete nuevo. Registrar la decisión de `localStorage` y su riesgo frente a XSS. | RF-2, RNF-1 |
| `CLAUDE.md` y `AGENTS.md` | Modificar | Agregar el modelo `User`, los endpoints `/api/auth/*`, la variable `Jwt__SigningKey` y la renovación de sesión. Mantenerlos idénticos. | — |

### 1.2 Backend (`backend/AsistentePersonal/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `Models/User.cs` | Crear | Entidad con `Id` (fijo 1), `Email`, `PasswordHash`, `CreatedAt`. | RF-1, RNF-1 |
| `Data/AppDbContext.cs` | Modificar | `DbSet<User> Users`. | RF-1 |
| `Data/Migrations/*AddUsers*` | Generar y editar | Tabla `Users` y el índice único `lower("Email")` escrito a mano (como en la migración de hábitos). | RF-1, RNF-5 |
| `Services/AuthRules.cs` | Crear | Funciones puras que devuelven `null` o un mensaje en español: `ValidateEmail`, `ValidatePassword`, `ValidatePasswordConfirmation`. | RF-1 |
| `Services/SessionTokens.cs` | Crear | Emite y lee tokens JWT: `Issue(ownerId, now)`, con firma y expiración de 7 días. Es el único lugar que conoce la clave. | RF-2, RF-3, RNF-2 |
| `Services/AuthService.cs` | Crear | Casos de uso: `GetStatusAsync`, `SetupAsync`, `LoginAsync`. Usa `AppDbContext`, `PasswordHasher<User>` y `SessionTokens`. | RF-1, RF-2 |
| `Services/ServiceResult.cs` | Modificar | Agregar los estados `Conflict` (409) y `Unauthorized` (401) a `ResultStatus`. `ServiceResult<T>` ya sirve para devolver la sesión. | RF-1, RF-2 |
| `DTOs/SetupRequest.cs` | Crear | `Email`, `Password`, `PasswordConfirmation`. | RF-1 |
| `DTOs/LoginRequest.cs` | Crear | `Email`, `Password`. | RF-2 |
| `DTOs/AuthStatusResponse.cs` | Crear | `HasOwner` (bool). No revela email ni nada más. | RF-1 |
| `DTOs/SessionResponse.cs` | Crear | `Token`, `ExpiresAt`. | RF-2 |
| `Controllers/AuthController.cs` | Crear | `GET /api/auth/status` (público), `POST /api/auth/setup` (público, solo si no hay cuenta), `POST /api/auth/login` (público). | RF-1, RF-2 |
| `Controllers/HabitsController.cs` | Modificar | Ninguno: ya usa `[Authorize]` y `ClaimTypes.NameIdentifier`. | RF-3 |
| `Program.cs` | Modificar | Configurar JwtBearer (emisor, audiencia, clave, sin tolerancia de reloj). Registrar `AuthService`, `SessionTokens` y `PasswordHasher<User>`. Agregar el middleware de renovación. Leer `Jwt__SigningKey` y fallar si falta. | RF-2, RF-3, RNF-2 |

**Endpoints:**

| Método y ruta | Acceso | Respuesta | RF |
|---|---|---|---|
| `GET /api/auth/status` | Público | 200 `{ hasOwner }` | CA-1.1, CA-1.6 |
| `POST /api/auth/setup` | Público, solo sin cuenta | 200 `SessionResponse`; 400 si la validación falla; 409 si ya existe la cuenta | CA-1.2 a CA-1.7 |
| `POST /api/auth/login` | Público | 200 `SessionResponse`; 401 `{ message: "Email o contraseña incorrectos." }` | CA-2.1, CA-2.2, CA-2.4 |

Todas las demás rutas de hábitos ya exigen sesión (`[Authorize]`), y ya devuelven 401 sin token (CA-3.1, CA-3.2).

**Renovación de sesión (CA-2.5):** un middleware registrado con `Response.OnStarting` agrega `X-Refreshed-Token` a cualquier respuesta cuya petición tuvo una sesión válida. El token nuevo expira 7 días desde ahora. El encabezado es visible para el navegador solo si se expone con CORS; como la app va por el proxy de Vite (mismo origen), no hace falta configurar CORS.

### 1.3 Tests del backend (`backend/AsistentePersonal.Tests/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `AuthRulesTests.cs` | Crear | Un test por regla de `AuthRules`: email vacío, sin arroba, sin punto después de la arroba; contraseña de 11 y 12 caracteres; confirmación distinta. | RF-1 |
| `SessionTokensTests.cs` | Crear | Un token emitido se valida con la misma clave; con otra clave falla; un token vencido falla. | RF-2, RF-3 |
| `AuthServiceTests.cs` | Crear | Contra la base de tests: crear la cuenta; segundo intento rechazado; dos intentos a la vez dejan una sola cuenta; login correcto; login con email en mayúsculas; login incorrecto con el mismo mensaje; login sin cuenta. | RF-1, RF-2 |
| `TestDatabase.cs` | Modificar | Limpiar también la tabla `Users` antes de cada test. | RF-1 |

## 2. Modelo de datos

```
Users
  Id            int          PK, siempre 1 (un solo dueño)
  Email         text         not null; índice único sobre lower("Email")
  PasswordHash  text         not null
  CreatedAt     timestamptz  not null
```

`Habit.UserId` no tiene clave foránea hacia `Users`. Así los tests de hábitos pueden usar `UserId = 1` sin crear una cuenta antes. La relación queda por convención (`UserId = 1`) y se documenta aquí.

## 3. Algoritmos clave

### 3.1 Crear la cuenta [RF-1]

```
AuthService.SetupAsync(email, password, confirmation):
    si ya hay cuenta (Users.Id = 1 existe):    devolver Conflict("Ya existe una cuenta.")       (CA-1.6)
    error = AuthRules.ValidateEmail(email)
         ?? AuthRules.ValidatePassword(password)
         ?? AuthRules.ValidatePasswordConfirmation(password, confirmation)
    si hay error:                              devolver Invalid(error)                          (CA-1.3 a 1.5)
    user = new User { Id = 1, Email = email.Trim(), PasswordHash = hasher.HashPassword(...), CreatedAt = now }
    insertar
    si la base rechaza por clave duplicada:    devolver Conflict("Ya existe una cuenta.")       (CA-1.7)
    devolver Ok(SessionTokens.Issue(1, now))                                                    (CA-1.2)
```

### 3.2 Iniciar sesión [RF-2]

```
AuthService.LoginAsync(email, password):
    user = Users donde Id = 1 y lower(Email) = lower(email)
    si no existe user:                         devolver Unauthorized(mensaje genérico)          (CA-2.2, CA-2.4)
    resultado = hasher.VerifyHashedPassword(user, user.PasswordHash, password)
    si resultado es Failed:                    devolver Unauthorized(mensaje genérico)          (CA-2.2)
    devolver Ok(SessionTokens.Issue(1, now))                                                    (CA-2.1)
```

El mensaje es el mismo en los dos casos de fallo, para no revelar si el email existe.

### 3.3 Renovar la sesión [RF-2, CA-2.5]

```
Middleware (después de autenticar):
    si la petición tenía un token válido:
        Response.OnStarting(() => Response.Headers["X-Refreshed-Token"] = SessionTokens.Issue(1, now))
```

El cliente lee ese encabezado en cada respuesta y, si existe, reemplaza el token guardado.

## 4. Cómo se ve en la interfaz [RF-1 a RF-4]

- **Al abrir la app:** el cliente pide `GET /api/auth/status`.
  - Si no hay cuenta: muestra la pantalla de primer uso (CA-1.1).
  - Si hay cuenta y no hay token guardado: muestra la pantalla de inicio de sesión.
  - Si hay token: muestra la lista de hábitos.
- **Pantalla de primer uso:** email, contraseña y repetir contraseña. Los errores de la API se muestran arriba del formulario; el formulario no se borra si hay error.
- **Pantalla de inicio de sesión:** email y contraseña. Muestra "Email o contraseña incorrectos." cuando corresponde.
- **Botón "Salir"** en el encabezado de la lista: borra el token de `localStorage` y vuelve a la pantalla de inicio de sesión (CA-4.1).
- **401 en cualquier llamada** (CA-3.3): borra el token y muestra la pantalla de inicio de sesión con "Tu sesión expiró. Vuelve a iniciar sesión.".
- **Renovación:** `habitsApi.js` lee `X-Refreshed-Token` de cada respuesta y lo guarda.

**Archivos de la interfaz:**

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `src/api/session.js` | Crear | Guardar, leer y borrar el token en `localStorage`. Cada acceso va dentro de `try/catch`: el navegador puede bloquearlo. | RF-2, RF-4 |
| `src/api/authApi.js` | Crear | `getStatus`, `setup`, `login`. | RF-1, RF-2 |
| `src/api/habitsApi.js` | Modificar | Agregar el token guardado a cada petición, guardar `X-Refreshed-Token` y avisar con un callback cuando llega un 401. | RF-3, CA-2.5 |
| `src/pages/SetupPage.jsx` | Crear | Pantalla de primer uso. | RF-1 |
| `src/pages/LoginPage.jsx` | Crear | Pantalla de inicio de sesión. | RF-2 |
| `src/App.jsx` | Modificar | Decide qué pantalla mostrar al abrir la app y maneja el 401 y el cierre de sesión. | RF-1 a RF-4 |
| `src/pages/HabitsPage.jsx` | Modificar | Agregar el botón "Salir" en el encabezado. | RF-4 |
| `src/styles/habits.css` | Modificar | Estilos del formulario de sesión (reutilizar `.habit-form`). | — |

**Lógica pura del frontend:** `session.js` no tiene lógica de fechas ni de estado; solo guarda y lee. No se agrega lógica de negocio en el cliente: las reglas de contraseña y email viven en el backend (decisión D-T6 del plan 001).

## 5. Configuración

| Variable | Obligatoria | Uso |
|---|---|---|
| `ConnectionStrings__Default` | Sí (ya existe) | Base de datos de desarrollo. |
| `Jwt__SigningKey` | Sí (nueva) | Clave para firmar las sesiones. Debe tener al menos 32 caracteres. Si falta o es corta, la app no arranca. |

La clave se configura en las variables de usuario de Windows, igual que la cadena de conexión. Nunca va en `appsettings*.json`.

## 6. Estrategia de tests

- **Backend, `xUnit`:** `AuthRulesTests`, `SessionTokensTests` y `AuthServiceTests`, escritos antes del código (TDD). La concurrencia de CA-1.7 se prueba con dos contextos y `Task.WhenAll`, como en T22.
- **Advertencia sobre T22:** ese test no garantiza que las dos escrituras se crucen en el tiempo. La clave primaria fija (`Id = 1`) es la que impide el doble registro; se verifica también leyendo el código del manejo de la excepción.
- **Frontend:** no se agregan tests nuevos (la lógica de negocio está en el backend). Se verifica en el navegador: primer uso, inicio de sesión, cierre de sesión, expiración simulada con un token alterado (debe dar 401 y volver al inicio de sesión).
- **API, con `curl`:** `GET /api/auth/status`, `POST /api/auth/setup` (una vez y otra vez), `POST /api/auth/login` (correcto e incorrecto), y `GET /api/habits` con el token (200 y encabezado `X-Refreshed-Token`), y sin token (401).

## 7. Matriz de cobertura

| RF / CA | Código | Verificación |
|---|---|---|
| CA-1.1, CA-1.6 | `AuthController.Status`, `AuthService.GetStatusAsync` | `curl` antes y después de crear la cuenta |
| CA-1.2 | `AuthService.SetupAsync` | `AuthServiceTests` |
| CA-1.3 a CA-1.5 | `AuthRules` | `AuthRulesTests` |
| CA-1.7 | Clave primaria fija `Id = 1` | `AuthServiceTests` (concurrencia) |
| CA-2.1 | `AuthService.LoginAsync`, `SessionTokens.Issue` | `AuthServiceTests`, `SessionTokensTests` |
| CA-2.2, CA-2.4 | Mensaje genérico en `LoginAsync` | `AuthServiceTests` |
| CA-2.3 | Ausencia de bloqueo (riesgo aceptado) | Revisión de código |
| CA-2.5 | Middleware `X-Refreshed-Token` | `curl` con el token: el encabezado aparece |
| CA-3.1, CA-3.2 | `[Authorize]` en `HabitsController` | `curl` sin token (401) |
| CA-3.3 | `habitsApi.js` y `App.jsx` | Navegador con token alterado |
| CA-4.1 | `App.jsx` y `session.js` | Navegador: "Salir" |
| CA-4.2 | Cierre de sesión solo en el cliente | Revisión de código |
| RNF-1 | `PasswordHasher<User>` | `AuthServiceTests` (el hash no es la contraseña) |
| RNF-2 | `Jwt__SigningKey` obligatoria | Arranque sin la variable falla con mensaje claro |
| RNF-5 | Índice `lower("Email")` | SQL verificado en la base |

## 8. Riesgos y pendientes

- **Token en `localStorage`:** si alguien logra ejecutar JavaScript en la página, puede leer el token. Riesgo aceptado para una app personal sin contenido de terceros. Se revisa antes de publicar.
- **Sin límite de intentos (RNF-3):** se revisa antes de publicar.
- **Dependencia de la spec 001:** T47 queda desbloqueada cuando esta spec esté verificada. Las tareas de la fase 8 se verifican con datos reales después de este plan.
- **Migración sin clave foránea:** `Habits.UserId` no referencia `Users`. Si más adelante hay más de un dueño, hace falta esa clave foránea y migrar los datos.
