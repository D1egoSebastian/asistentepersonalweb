# CLAUDE.md

# Asistente Personal - Proyecto

> Los principios innegociables del proyecto están en `docs/constitution.md`. Ante cualquier conflicto, la constitución manda.

## Descripción General

Aplicación web para gestionar un asistente personal integral que vaya más allá de un simple to-do list. Será un espacio centralizado para almacenar ideas, rutinas, hábitos y hacer seguimiento de progreso con métricas y gráficos.

## Objetivo

Crear una plataforma donde pueda:
- Anotar ideas que se me ocurran en el momento
- Registrar rutinas y hábitos que quiero desarrollar (ej: daily coding challenge)
- Hacer auditoría y seguimiento de lo completado
- Ver métricas, gráficos y progreso de manera ordenada
- Acceder desde cualquier lugar (web)
- Que cuente con notificacion o estados de tareas cuando no las cumpla (tal vez rojo cuando no se cumpla algo, verde cuando si)
- Que se integre con mi gmail para poder mandarme correos a mi mismo sobre objetivos que quiero lograr, rutinas que me faltan, etc.

## Stack Tecnológico

### Frontend
- **Framework:** React
- **Build tool:** Vite
- **Gráficos:** Chart.js 4.5.1, versión exacta (justificada en `docs/decisions.md`, spec 004)
- **Styling:** CSS plano, sin Tailwind (decisión D-T8 de `docs/plans/001-seguimiento-de-habitos.md`)

### Backend
- **Framework:** ASP.NET Core
- **Lenguaje:** C#
- **Base de datos:** PostgreSQL
- **Autenticación:** JWT firmado con `Jwt__SigningKey`; contraseña con `PasswordHasher` de ASP.NET Core Identity (sin la librería completa de Identity). Ver `docs/specs/002-login.md`.

### Deployment
- **Frontend:** Vercel
- **Backend:** (A definir - Azure, Railway, Render, etc.)
- **Base de datos:** PostgreSQL alojada

## Funcionalidades Principales

### 1. Gestión de Ideas
- Crear, editar, eliminar ideas
- Almacenarlas de forma ordenada
- Buscar/filtrar ideas

### 2. Gestión de Hábitos y Rutinas
- Crear hábitos (nombre, descripción, frecuencia diaria o semanal con meta)
- Registrar cuando completo un hábito (hoy o ayer, un registro por día)
- Historial de ejecución

### 3. Métricas y Tracking
- **Streaks:** Cuántos días seguidos he completado un hábito
- **Porcentaje de cumplimiento:** % de veces que completé vs. veces que debería
- **Gráficos:** Visualización de progreso a lo largo del tiempo
- Dashboard con resumen general

### 4. Autenticación
- App de un solo usuario (el dueño), sin registro público
- La cuenta se crea una sola vez desde la pantalla de primer uso; después esa pantalla se desactiva
- Todo endpoint salvo `/api/auth/*` exige sesión, excepto `POST /api/reminders/daily`, que se protege con un secreto compartido en `X-Reminder-Secret` (spec 005, S-4)
- La sesión dura 7 días desde el último uso: cada respuesta con sesión trae un token renovado en el encabezado `X-Refreshed-Token`
- "Salir" solo borra el token del navegador (no hay revocación en el servidor)

### 5. Resumen diario por correo (spec 005)
- Un flujo programado llama cada hora a `POST /api/reminders/daily`; desde las 8:00 del dueño, y una sola vez por día, el servidor envía un correo de texto plano a la cuenta del dueño con las ideas que llevan 14 días sin editarse y los hábitos en riesgo
- Si no hay nada que reportar no se envía correo, y si el envío falla el día no queda marcado para que la siguiente ejecución lo reintente
- El envío usa Resend por `HttpClient`, sin paquete (ver `docs/decisions.md`)
- La publicación gratuita (GitHub Pages, Render, Neon) está pendiente: sigue en la spec 005

## Estructura del Backend (ASP.NET Core)

```
Controllers/        - Endpoints HTTP
Models/            - Entidades de BD (Habit, HabitLog, Idea, User, etc.)
DTOs/              - Data Transfer Objects para las respuestas
Services/          - Casos de uso y validaciones de escritura (HabitService, HabitRules, IdeaService, IdeaRules, TextRules), reloj del dueño (OwnerClock), resumen diario (ReminderService, ReminderRules, ReminderText) y envío de correo (IEmailSender, ResendEmailSender)
Data/              - DbContext, migrations
```

Sin capa `Repositories/`: EF Core ya cumple ese rol, así que los Services usan el `DbContext` directamente.

Las rachas, el estado y el avance semanal se calculan en el frontend, en `src/logic/`, con funciones puras que reciben `today` como parámetro (decisión D-T1 del plan de la spec 001). La lógica de cada pantalla vive en `src/hooks/`; los componentes de `src/pages/` y `src/components/` solo muestran. Las sugerencias y las opciones de filtro de etiquetas son funciones puras en `src/logic/tags.js`, y las llamadas a la API salen de `src/api/` (una función por endpoint, sobre `http.js`).

La app tiene tres pestañas tras iniciar sesión: "Hábitos", "Ideas" y "Métricas" (la barra superior también tiene "Salir"). En ideas, la búsqueda, el filtro y el orden los hace el backend: el frontend pide la sección, espera 300 ms tras la última tecla de la búsqueda y muestra el resultado.

Las métricas (spec 004) no tienen endpoints propios: salen de `GET /api/habits` y se calculan en el frontend con funciones puras de `src/logic/`: `completion.js` (ventanas y porcentajes), `trend.js` (tendencia de 12 semanas), `history.js` (grilla día por día), `metricsView.js` (todos los textos de la pestaña) y `trendChart.js` (configuración de Chart.js, sin importarlo). Las rachas, incluida la mejor, están en `streak.js`. Los porcentajes se redondean solo al mostrarlos, con `roundPercent`.

## Variables de entorno

| Variable | Uso |
|---|---|
| `ConnectionStrings__Default` | Base de la app (`asistente_dev` en desarrollo). |
| `ConnectionStrings__Test` | Base de los tests de backend (`asistente_test`). El nombre debe terminar en `_test`: los tests borran todas las filas y se niegan a correr contra otra base. |
| `Jwt__SigningKey` | Clave para firmar las sesiones; al menos 32 caracteres. Sin ella la API no arranca. |
| `Resend__ApiKey` | Clave de envío de Resend. Sin ella la API no arranca. |
| `Reminders__Secret` | Secreto que el flujo programado manda en `X-Reminder-Secret`; al menos 32 caracteres. Sin él la API no arranca. |
| `Cors__AllowedOrigin` | Origen del frontend publicado (`https://<usuario>.github.io`). Lo usará la publicación (spec 005, CA-5.5); todavía no tiene efecto. |
| `OWNER_TIME_ZONE` | Zona horaria del dueño (por ejemplo, `America/La_Paz`). Define "hoy" en la API. Si falta o no existe, la API no arranca. |

Ninguna va en `appsettings*.json` ni en el repositorio.

## Endpoints Básicos (API REST)

- `GET /api/auth/status` - Indica si ya existe la cuenta del dueño (público)
- `POST /api/auth/setup` - Crea la cuenta del dueño, solo si no existe (público)
- `POST /api/auth/login` - Inicia sesión y devuelve el token (público)
- `GET /api/ideas?archived=false&search=&tag=` - Traer una sección (activas o archivadas), con búsqueda y filtro opcionales. Devuelve resumen, no el texto completo
- `GET /api/ideas/{id}` - Abrir una idea completa, activa o archivada, con su `version`
- `POST /api/ideas` - Crear idea
- `PUT /api/ideas/{id}` - Editar texto y etiquetas; exige la `version` que el dueño abrió (si no coincide, 409)
- `POST /api/ideas/{id}/archive` - Archivar idea
- `POST /api/ideas/{id}/restore` - Restaurar idea (conserva el texto, las etiquetas y `createdAt`, y reinicia `updatedAt`)
- `DELETE /api/ideas/{id}` - Eliminar idea
- `GET /api/ideas/tags` - Etiquetas del dueño con cuántas ideas activas y archivadas tiene cada una

- `POST /api/habits` - Crear hábito
- `GET /api/habits` - Traer todos los hábitos, con `today` y el historial de fechas de cada uno
- `PUT /api/habits/{id}` - Editar nombre y descripción (frecuencia y meta no se cambian)
- `DELETE /api/habits/{id}` - Eliminar hábito y todo su historial

- `POST /api/habits/{id}/log` - Marcar hábito como cumplido para una fecha (hoy o ayer)
- `DELETE /api/habits/{id}/log/{date}` - Desmarcar hábito para una fecha (hoy o ayer)

- `POST /api/reminders/daily` - Calcula y envía el resumen del día si corresponde. Sin sesión: exige `X-Reminder-Secret` (401 sin cuerpo si falta o no coincide). Responde 200 `{ result: "sent" | "skipped", items }` o 502 `{ result: "failed", items }`; nunca devuelve texto de ideas ni nombres de hábitos

## Modelos de BD (Inicial)

### User
- id (siempre 1: un solo dueño; la clave primaria fija impide crear dos cuentas)
- email (único sin distinguir mayúsculas)
- passwordHash
- createdAt

### Habit
- id
- userId (FK)
- name
- description (opcional)
- frequency (daily | weekly)
- weeklyGoal (solo si weekly; entre 1 y 7)
- createdOn (fecha, sin hora)

### HabitLog
- id
- habitId (FK)
- date (fecha, sin hora; único por habitId)

### Idea
- id
- userId (FK)
- text (sin límite de líneas; máximo 5.000 caracteres)
- tags (`text[]`; hasta 10 por idea, máximo 30 caracteres cada una, sin comas)
- createdAt
- updatedAt
- archivedAt (opcional: si tiene fecha, la idea está archivada)
- version (`xmin` de PostgreSQL, sin columna propia: cambia en cada escritura y detecta ediciones simultáneas)

### DailyDigestLog
- forDate (fecha, sin hora; clave primaria: un resumen por día)
- sentAt

### ReminderLog
- id
- ideaId (FK a Idea, con borrado en cascada)
- sentOn (fecha, sin hora; único por ideaId y sentOn)

La base necesita la extensión `unaccent`: la migración `AddIdeas` la crea con `CREATE EXTENSION IF NOT EXISTS unaccent`. La búsqueda ignora tildes con ella (decisión en `docs/decisions.md`).

## Próximas Decisiones

- [ ] Elegir plataforma de deployment para backend
- [x] Definir librería de gráficos: Chart.js 4.5.1 (spec 004)
- [x] Antes de publicar: revisar el límite de intentos de login y el token en `localStorage` (riesgos aceptados en la spec 002; decisión del dueño el 2026-11-02: se mantienen tal como están)

## Notas

- No se integra IA (Claude API) — solo almacenamiento y visualización de datos
- Aplicación personal de un solo usuario (por ahora); no requiere escala masiva
- Prioridad: funcionalidad y experiencia de usuario clara
