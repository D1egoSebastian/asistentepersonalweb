# Decisiones de dependencias

Registro de cada paquete externo que se agrega al proyecto, según el principio 1 de la [constitución](constitution.md). Cada entrada explica qué problema resuelve y por qué no alcanza lo que ya hay en el stack.

## Token de sesión en `localStorage` (no es un paquete, pero es un riesgo a registrar)

- **Qué se decidió:** el frontend guarda el token de sesión en `localStorage` (spec 002, plan §0).
- **Riesgo:** si alguien logra ejecutar JavaScript en la página (XSS), puede leer el token y usarlo hasta que venza.
- **Por qué se acepta por ahora:** es una app personal, sin contenido de terceros ni HTML generado por usuarios, y React escapa el texto por defecto.
- **Alternativa descartada por ahora:** cookie `HttpOnly`, que JavaScript no puede leer. Se revisa antes de publicar la app.
- **Decisión del dueño (2026-11-02):** se acepta tal como está, también al publicar la app. La cookie `HttpOnly` queda como alternativa si cambia el riesgo.
- **Paquetes:** no hizo falta ninguno nuevo para el login. `PasswordHasher` viene con el framework de ASP.NET Core, y los tokens usan las librerías que ya trae `Microsoft.AspNetCore.Authentication.JwtBearer`.

## Npgsql.EntityFrameworkCore.PostgreSQL

- **Problema que resuelve:** permite que Entity Framework Core hable con PostgreSQL. EF Core es el ORM que usa el backend (ver `CLAUDE.md`, sección de estructura del backend).
- **Por qué no alcanza lo que ya hay:** EF Core por sí solo no incluye ningún proveedor de base de datos. Sin este paquete, el `DbContext` no puede conectarse a PostgreSQL.
- **Alternativa descartada:** escribir las consultas SQL a mano con `Npgsql` directamente, sin EF Core. Se descarta porque obliga a escribir a mano el mapeo de cada entidad y las migraciones, y eso es más código para mantener.

## @vitejs/plugin-react 6.x

- **Problema que resuelve:** permite que Vite compile JSX (la sintaxis de React) y que el desarrollo use recarga en caliente.
- **Por qué no alcanza lo que ya hay:** Vite sabe servir archivos, pero no transforma JSX por sí solo. React y Vite están en el stack; este plugin es el puente entre los dos.
- **Alternativa descartada:** escribir componentes sin JSX (con `React.createElement`). Se descarta porque el código es más largo y más difícil de leer para alguien que empieza.
- **Nota:** el proyecto no usa ESLint ni otras herramientas de lint, porque el plan no las pide. Si después se necesitan, se justifican aquí.

## Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10

- **Problema que resuelve:** da a los endpoints `[Authorize]` un esquema de autenticación real. Sin él, ASP.NET responde 500 ("no authentication handler is registered") en vez de 401, y la spec exige 401 sin sesión (T27).
- **Por qué no alcanza lo que ya hay:** la autenticación de ASP.NET Core está en este paquete; no viene en el framework base. La spec ya elige JWT (ver `CLAUDE.md`, sección de autenticación).
- **Alternativa descartada:** un esquema propio que siempre responde 401. Se descarta porque obliga a escribir y mantener código de autenticación que el login de la spec va a reemplazar.
- **Nota:** todavía no emite tokens. Eso llega con la spec de login.

## Microsoft.EntityFrameworkCore 10.0.13 (referencia directa)

- **Problema que resuelve:** fija la versión de EF Core que usa el proyecto. Sin esta referencia, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 trae EF Core 10.0.4 para el proyecto de tests y `Microsoft.EntityFrameworkCore.Design` trae 10.0.13 para el proyecto principal. Las dos versiones no pueden compilar juntas.
- **Por qué no alcanza lo que ya hay:** no es un paquete nuevo en el stack: EF Core ya era parte del stack. Solo alinea las versiones.
- **Alternativa descartada:** cambiar la versión de Npgsql hasta encontrar una que dependa de 10.0.13. Se descarta porque depende de la disponibilidad de versiones y no deja explícita la decisión.

## Microsoft.EntityFrameworkCore.Design

- **Problema que resuelve:** provee las herramientas que usa `dotnet ef` para generar y aplicar migraciones de la base de datos.
- **Por qué no alcanza lo que ya hay:** sin este paquete, el comando `dotnet ef migrations add` no existe, y el esquema de PostgreSQL tendría que escribirse y mantenerse a mano.
- **Alternativa descartada:** mantener scripts SQL de creación de tablas a mano. Se descarta porque cada cambio de modelo exigiría editar el script y el modelo por separado, y es fácil que queden desincronizados.
- **Nota:** este paquete se usa solo en tiempo de desarrollo, no en la aplicación publicada.

## Extensión `unaccent` de PostgreSQL

- **Problema que resuelve:** la búsqueda de ideas ignora acentos: "cancion" encuentra "canción" y al revés (spec 003, CA-4.1, D-2).
- **Por qué no alcanza lo que ya hay:** `ILIKE` y `lower()` ignoran mayúsculas, pero no tildes. PostgreSQL no trae otra función integrada que quite acentos. `unaccent` no es un paquete externo: viene con la instalación de PostgreSQL (módulo `contrib`), y la búsqueda sigue corriendo en la base (plan D-T2).
- **Alternativa descartada 1:** quitar tildes en C# y filtrar en memoria. Obliga a traer todas las ideas del dueño en cada búsqueda y no cumple RNF-3 (5.000 ideas en menos de 1 segundo).
- **Alternativa descartada 2:** guardar una columna aparte con el texto sin tildes. Duplica el dato y hay que mantenerla en cada escritura.
- **Riesgo:** el hosting de PostgreSQL tiene que permitir `CREATE EXTENSION unaccent`. Se verifica al elegir hosting (plan §8).
- **Verificado:** el archivo `unaccent.control` existe en la instalación local de PostgreSQL 18. Todavía no está creada en `asistente_dev` ni en `asistente_test`: eso ocurre en la migración `AddIdeas` (T07).

## Microsoft.AspNetCore.OpenApi 10.0.10

- **Problema que resuelve:** publica el documento OpenAPI de la API (`AddOpenApi` y `MapOpenApi` en `Program.cs`), para revisar los endpoints sin leer el código.
- **Por qué no alcanza lo que ya hay:** el framework no genera ese documento por sí solo; sin este paquete no existen `AddOpenApi` ni `MapOpenApi`.
- **Alternativa descartada:** escribir el JSON de OpenAPI a mano. Se descarta porque habría que mantenerlo igual que los endpoints, y es fácil que se desincronicen.
- **Nota:** `MapOpenApi` solo se activa en desarrollo (`Program.cs`, dentro de `IsDevelopment()`).

## Microsoft.OpenApi 2.7.5 (referencia directa)

- **Problema que resuelve:** es la librería de modelo OpenAPI que usa `Microsoft.AspNetCore.OpenApi`. El código del proyecto no la usa directamente.
- **Por qué no alcanza lo que ya hay:** la referencia explícita fija la versión 2.7.5, la misma que ya trae `Microsoft.AspNetCore.OpenApi` 10.0.10 como dependencia.
- **Pendiente:** confirmar si la referencia explícita hace falta o si puede quitarse sin cambiar la versión resuelta.

## chart.js 4.5.1

- **Problema que resuelve:** dibuja el gráfico de tendencia de la pestaña "Métricas" (spec 004, RF-7): una línea con 12 puntos semanales, ejes de 0 a 100 %, huecos donde una semana no tiene datos y escalado en un `canvas`.
- **Por qué no alcanza lo que ya hay:** el stack no tiene librería de gráficos. Con CSS plano no se dibuja una línea con huecos y ejes sin escribir a mano un pequeño motor de gráficos (escalas, trazado, puntos, tooltip). El dueño eligió Chart.js en la spec 004 (D-5).
- **Versión:** se fija `4.5.1` exacta (sin `^`, instalada con `--save-exact`). La última, 4.6.1, se publicó el 22-10-2026 y en npm no figura una 4.6.0; es demasiado reciente para adoptarla sin uso previo. La 4.5.1 se publicó el 13-10-2025.
- **Licencia y dependencias:** MIT. Trae una sola dependencia transitiva (`@kurkle/color`, para mezclar colores) y no declara `peerDependencies`. Es un paquete ESM, compatible con Vite 8; `npm run build` lo confirma al instalarla.
- **Cómo se usa (plan 004, D-T9):** se importan y registran solo `LineController`, `LineElement`, `PointElement`, `LinearScale`, `CategoryScale` y `Tooltip`, para que Vite deje fuera el resto. La configuración sale de una función pura y testeada (`trendChartConfig`), sin animación.
- **Alternativa descartada 1:** `react-chartjs-2`. Es una segunda dependencia para envolver un solo gráfico; un componente propio con `useEffect` crea y destruye el gráfico en pocas líneas.
- **Alternativa descartada 2:** `chart.js/auto`. Registra todos los tipos de gráfico y plugins, y suma al bundle código que la app no usa.
- **Alternativa descartada 3:** Recharts. Depende de React y de varias librerías de D3, y es más pesada para un solo gráfico de líneas. Cierra la decisión "Chart.js vs. Recharts" de `CLAUDE.md`.
- **Accesibilidad:** el `canvas` no se lee con lector de pantalla, así que la tabla de tendencia (`TrendTable`) queda siempre visible como alternativa en texto (RNF-4).

## Resend (servicio de correo, sin paquete)

- **Problema que resuelve:** envía el resumen diario por correo al Gmail del dueño (spec 005, CA-5.3). Se usa desde `ResendEmailSender` con `HttpClient` contra la API REST de Resend (`POST https://api.resend.com/emails`).
- **Por qué no alcanza lo que ya hay:** el framework puede enviar por SMTP, pero el hosting gratuito suele bloquear los puertos SMTP. Una API por HTTPS pasa por el puerto 443, que siempre está abierto.
- **Paquetes:** ninguno (RNF-7). `HttpClient` y `System.Text.Json` vienen con el framework, así que no se agrega la librería de Resend.
- **Alternativa descartada 1:** Gmail SMTP. El hosting gratuito bloquea el SMTP saliente y exige una contraseña de aplicación de Google guardada como secreto.
- **Alternativa descartada 2:** la API de Gmail. Pide OAuth, una pantalla de consentimiento y renovar tokens, mucho más de lo que necesita un correo al propio dueño.
- **Alternativa descartada 3:** una librería cliente de Resend. Es una dependencia nueva para una sola llamada HTTP.
- **Tratamiento de datos (RNF-6):** el correo contiene el texto de las ideas del dueño (recortado a 80 caracteres) y los nombres de sus hábitos, y se envía a su propia cuenta. Resend los procesa como proveedor. La política de retención del contenido enviado **está por verificar** en la documentación vigente (tarea T04); esta entrada se completa con ese dato.
- **Límites del plan gratuito:** **por verificar** (tarea T04): el límite de envíos frente a un correo diario, que no pida tarjeta (RNF-1) y que el remitente de prueba `onboarding@resend.dev` solo entregue a la dirección de la cuenta de Resend.
- **Cabecera `Idempotency-Key`:** el código la envía con la clave `digest-{fecha}`. Resend la admite con una ventana de 24 horas (verificado en su documentación el 2026-11-02, T04). Así, un envío aceptado cuya respuesta se pierde no se repite en la ejecución de la hora siguiente.
- **Secretos:** la clave vive solo en `Resend__ApiKey`. El cliente registra únicamente el código de estado de la respuesta, nunca el cuerpo, las cabeceras ni la clave (RNF-2).
