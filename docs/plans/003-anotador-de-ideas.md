# Plan 003 — Anotador de ideas

Plan técnico de [`docs/specs/003-anotador-de-ideas.md`](../specs/003-anotador-de-ideas.md). Respeta [`docs/constitution.md`](../constitution.md). Cada parte indica entre corchetes los RF que cubre.

## 1. Arquitectura en una frase

El backend **guarda, valida, busca y filtra**: las ideas pueden ser miles y largas, así que la búsqueda no se hace en el navegador. El frontend **muestra y sugiere**: con la lista de etiquetas que le da la API, calcula las sugerencias y las opciones del filtro con funciones puras.

```
[React] IdeasPage ─ useIdeas ─ ideasApi ─ http.js (sesión, X-Refreshed-Token, 401)
   │                    └─ src/logic/tags.js (sugerencias, opciones de filtro: puras)
   ▼
[API] IdeasController ─ IdeaService ─ AppDbContext ─ PostgreSQL (unaccent + ILIKE)
                            └─ IdeaRules (puras: texto, resumen, etiquetas, patrón de búsqueda)
```

## 2. Decisiones técnicas

| ID | Decisión | Por qué | Alternativa descartada |
|---|---|---|---|
| D-T1 | Las etiquetas se guardan como una **lista de texto dentro de cada idea** (columna `text[]` de PostgreSQL). | Una etiqueta existe mientras alguna idea la usa (CA-2.9) sin escribir limpieza de huérfanas, y no hay tablas intermedias que entender. | Tablas `Tags` e `IdeaTags`: más normalizado, pero obliga a borrar etiquetas huérfanas y a hacer joins para cada lista. Con 200 etiquetas no compensa. |
| D-T2 | La **búsqueda corre en la base**, con `unaccent` + `ILIKE` sobre texto escapado. | Ignora mayúsculas y acentos (CA-4.1, D-2) y trata `%` y `_` como texto (CA-4.5). Con 5.000 ideas no hace falta índice para cumplir 1 segundo (RNF-3). | Buscar en el navegador: obliga a bajar el texto completo de todas las ideas (hasta 25 MB en el peor caso). |
| D-T3 | La lista devuelve el **resumen**, no el texto completo. El texto se pide al abrir una idea. | Cumple CA-3.2 y CA-3.3 sin bajar miles de textos largos. El resumen es una regla pura en el backend, con tests. | Devolver el texto completo y resumir en el navegador: más simple, pero pesado con ideas largas. |
| D-T4 | Archivar es una **fecha `ArchivedAt` opcional**: vacía = activa. | Un solo campo distingue activa de archivada y dice cuándo se archivó. Restaurar la vacía (CA-6.3). | Un campo de estado de texto: abre la puerta a más estados, y la spec los excluye. |
| D-T5 | Las etiquetas se **escriben con la ortografía existente** (CA-2.2): al guardar, el servicio busca si ya hay una igual sin mayúsculas y usa esa. | La regla queda en un solo lugar (el servicio) y se testea contra la base. Como dos guardados simultáneos igual podrían dejar "App" y "app", el filtro y el conteo comparan siempre en minúsculas, y el conteo devuelve la escritura que usan más ideas (CA-2.11). | Corregir la ortografía en el navegador: dos pestañas podrían crear "App" y "app" a la vez, y el filtro por escritura exacta partiría el tema en dos. |
| D-T6 | **Un solo `request()` compartido** en `src/api/http.js`, usado por hábitos, ideas y sesión. | Hoy está duplicado en `authApi.js` y `habitsApi.js`; las ideas serían la tercera copia. La lógica de sesión (token, renovación, 401, "Salir") queda en un solo lugar. **Los mensajes de error salen del cuerpo de la respuesta** (`{ message }`), que el backend ya escribe en español para 400, 404 y 409. `http.js` solo fija el mensaje de 401 y uno genérico de respaldo. Así un 404 de ideas dice "Esta idea ya no existe." y uno de hábitos "Este hábito ya no existe.", sin que `http.js` conozca las pantallas (principio 3). | Copiar `request()` otra vez: tres lugares para corregir el mismo error. Dejar los mensajes fijos en `http.js`: el 404 de ideas diría "hábito". |
| D-T7 | **Navegación con pestañas en `App.jsx`** (Hábitos e Ideas), sin librería de rutas. | Son dos pantallas; un estado en `App` alcanza (principio 1). | `react-router`: una dependencia nueva para dos pestañas. |
| D-T8 | El doble clic (CA-8.2) se evita **en el formulario**, que bloquea "Guardar" mientras envía (igual que en hábitos). | Dos ideas con el mismo texto pueden ser legítimas, así que la base no puede rechazar duplicados. | Rechazar ideas idénticas en el backend: impediría anotar dos veces lo mismo a propósito. |
| D-T9 | La búsqueda en el navegador **espera 300 ms** después de la última tecla antes de pedir resultados. | Evita una petición por cada letra sin hacer lenta la búsqueda. | Buscar en cada tecla: muchas peticiones inútiles. |
| D-T10 | La **fecha mostrada** la calcula el backend en la zona del dueño (`OwnerClock.TodayAt` sobre `CreatedAt`) y la envía como `createdDate` (`yyyy-MM-dd`). | El navegador no conoce la zona del dueño; así la fecha coincide con el "hoy" de los hábitos. | Formatear en el navegador con su propia zona: una idea de las 23:30 podría mostrar otro día. |
| D-T11 | Los **instantes** (`CreatedAt`, `UpdatedAt`, `ArchivedAt`) los lee el controller con `DateTimeOffset.UtcNow` y los pasa al servicio como `now`, igual que `AuthController`. `OwnerClock` sigue siendo lo único que decide qué día es "hoy", y su comentario se corrige para decir exactamente eso. | El servicio no lee el reloj y se testea con un `now` fijo. | Leer el reloj dentro del servicio: los tests dependerían de la hora real. |
| D-T12 | La edición simultánea (CA-5.5) se detecta con la **versión de fila `xmin` de PostgreSQL**, mapeada como `uint Version` con `IsRowVersion()` de Npgsql. `GET /api/ideas/{id:int}` devuelve `version` y `PUT` la recibe; si no coincide, la edición se rechaza con 409. | PostgreSQL cambia `xmin` en cada escritura, así que nadie la mantiene a mano, y EF Core la usa como token de concurrencia, lo que también cubre la carrera entre leer y guardar. No suma librerías: ya usamos Npgsql. | Comparar `UpdatedAt`: PostgreSQL guarda microsegundos y .NET ticks de 100 ns, así que el valor devuelto al crear no coincide con el guardado y daría falsos "la idea cambió". |

## 3. Archivos

### 3.1 Documentación

| Archivo | Acción | Responsabilidad |
|---|---|---|
| `docs/decisions.md` | Modificar | Registrar la extensión `unaccent` de PostgreSQL (no es un paquete, pero agrega una dependencia a la base). |
| `CLAUDE.md` y `AGENTS.md` | Modificar | Actualizar el modelo `Idea` (etiquetas y `archivedAt`), los endpoints de ideas y la navegación por pestañas. Mantenerlos iguales. |

### 3.2 Backend (`backend/AsistentePersonal/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `Models/Idea.cs` | Crear | `Id`, `UserId`, `Text`, `Tags` (`List<string>`), `CreatedAt`, `UpdatedAt`, `ArchivedAt` (opcional) y `Version` (`xmin`, D-T12). | RF-1, RF-2, RF-5, RF-6 |
| `Data/AppDbContext.cs` | Modificar | `DbSet<Idea> Ideas`. | RF-1 |
| `Data/Migrations/*AddIdeas*` | Generar y editar | Tabla `Ideas`, índice `(UserId, ArchivedAt, CreatedAt)` y `CREATE EXTENSION IF NOT EXISTS unaccent` escrito a mano. | RF-1, RF-4 |
| `Services/TextRules.cs` | Crear (mover) | `IsBlank`, `Normalize` y `CountChars`, que hoy están en `HabitRules` y usan hábitos, login e ideas. `HabitRules`, `AuthRules` y `IdeaRules` los llaman desde acá. | RF-1, RF-2 |
| `Services/OwnerClock.cs` | Modificar | Solo el comentario: es lo único que decide qué día es "hoy" (D-T11). | — |
| `Services/IdeaRules.cs` | Crear | Funciones puras (sección 4.1). | RF-1, RF-2, RF-3, RF-4 |
| `Services/IdeaService.cs` | Crear | Casos de uso (sección 4.2). Recibe `userId` y `now`. | RF-1 a RF-8 |
| `DTOs/IdeaDtos.cs` | Crear | `IdeaRequest` (`Text`, `Tags`, y `Version` solo al editar), `IdeaSummaryResponse`, `IdeaResponse`, `IdeaListResponse`, `TagCountResponse`. | RF-1 a RF-6 |
| `Controllers/IdeasController.cs` | Crear | `[Authorize]`; dueño desde el token; traduce resultados a HTTP con el mismo mapeo que hábitos. | RF-1 a RF-8 |
| `Program.cs` | Modificar | Registrar `IdeaService`. | — |

**Endpoints** (todos con sesión):

| Método y ruta | Uso | Respuesta | RF |
|---|---|---|---|
| `GET /api/ideas?archived=false&search=&tag=` | Lista de una sección, con búsqueda y filtro opcionales | 200 `{ ideas: [{ id, summary, tags, createdDate }] }` | RF-3, RF-4, RF-6 |
| `GET /api/ideas/{id:int}` | Abrir una idea, activa o archivada | 200 `{ id, text, tags, createdDate, archived, version }`; 404 | CA-3.3, CA-5.5 |
| `POST /api/ideas` | Anotar | 200 idea; 400 | RF-1, RF-2 |
| `PUT /api/ideas/{id:int}` | Editar texto y etiquetas | 200 idea con su `version` nueva; 400; 404; 409 si está archivada o si `version` no coincide, cada uno con su `message` | RF-5, CA-5.5 |
| `POST /api/ideas/{id:int}/archive` | Archivar | 200; 404 | CA-6.1 |
| `POST /api/ideas/{id:int}/restore` | Restaurar | 200; 404 | CA-6.3 |
| `DELETE /api/ideas/{id:int}` | Eliminar | 200; 404 | RF-7 |
| `GET /api/ideas/tags` | Todas las etiquetas del dueño con cuántas ideas activas y archivadas tiene cada una, agrupadas sin distinguir mayúsculas | 200 `[{ name, activeCount, archivedCount }]` | CA-2.1, CA-2.9, CA-2.11, CA-4.6 |

Las rutas con `{id:int}` solo aceptan números, así que `/api/ideas/tags` nunca se confunde con una idea.

### 3.3 Tests del backend (`backend/AsistentePersonal.Tests/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `TextRulesTests.cs` | Crear (mover) | Los tests de `IsBlank`, `Normalize` y `CountChars` que hoy están en `HabitRulesTests`. | RF-1 |
| `IdeaRulesTests.cs` | Crear | Un test por regla de `IdeaRules`. | RF-1 a RF-4 |
| `IdeaServiceTests.cs` | Crear | Contra la base de tests (sección 6). | RF-1 a RF-8 |

### 3.4 Frontend (`frontend/src/`)

| Archivo | Acción | Responsabilidad | RF |
|---|---|---|---|
| `api/http.js` | Crear | El `request()` único: token, `X-Refreshed-Token`, 401, generación de sesión, errores con `status` y con el `message` que manda la API (D-T6). | CA-8.1, CA-8.3 |
| `api/habitsApi.js`, `api/authApi.js` | Modificar | Usar `http.js`; quitar sus copias de `request()`. | — |
| `api/ideasApi.js` | Crear | Una función por endpoint. | RF-1 a RF-7 |
| `logic/tags.js` | Crear | Funciones puras (sección 4.3). | CA-2.1, CA-2.3, CA-2.6, CA-4.6 |
| `logic/tags.test.js` | Crear | Tests con `node --test`. | Ídem |
| `hooks/useIdeas.js` | Crear | Sección (activas o archivadas), búsqueda con espera de 300 ms, filtro, lista, idea abierta y acciones. Toda la lógica de la pantalla. | RF-3 a RF-8 |
| `pages/IdeasPage.jsx` | Crear | Solo marcado: filtros, formulario de captura arriba, lista. | RF-3 |
| `components/IdeaComposer.jsx` | Crear | Texto + etiquetas; "Guardar" se bloquea mientras envía. Sirve para anotar y para editar. | RF-1, RF-2, RF-5, CA-8.2 |
| `components/TagInput.jsx` | Crear | Etiquetas elegidas como "chips" con botón para quitar, y sugerencias mientras se escribe (de `logic/tags.js`). | RF-2 |
| `components/IdeaFilters.jsx` | Crear | Búsqueda, selector de etiqueta, pestañas Activas/Archivadas y "Quitar filtros". | RF-4, CA-6.2 |
| `components/IdeaList.jsx`, `components/IdeaCard.jsx` | Crear | Resumen, etiquetas, fecha; acciones Abrir, Editar, Archivar/Restaurar, Eliminar. | RF-3, RF-6, RF-7 |
| `App.jsx` | Modificar | Pestañas Hábitos e Ideas cuando hay sesión (D-T7). | — |
| `styles/ideas.css` | Crear | Estilos de chips, filtros y tarjetas, con las variables de tema existentes. | RF-3 |

## 4. Reglas y algoritmos

### 4.1 `IdeaRules` (C#, puras)

Cada `Validate*` devuelve `null` si es válido o un mensaje en español.

| Función | Regla | RF |
|---|---|---|
| `NormalizeText(text)` | `Trim()` de espacios y saltos de línea en los extremos; conserva los del medio. | CA-1.4 |
| `ValidateText(text)` | Vacío → obligatorio; más de 5.000 caracteres → límite. | CA-1.2, CA-1.3 |
| `NormalizeTag(tag)` | Recorta extremos y un `#` inicial. | CA-2.3 |
| `ValidateTags(tags)` | Lista nula = sin etiquetas; etiqueta vacía o con coma → rechazo; más de 30 caracteres → límite; más de 10 etiquetas → límite. | CA-2.4, CA-2.5, CA-2.7, CA-2.10 |
| `DistinctTags(tags)` | Quita repetidas sin distinguir mayúsculas; conserva la primera escritura. | CA-2.6 |
| `Summary(text)` | Primera línea con texto; si supera 120 caracteres, los primeros 120 + "…". Cuenta y corta por caracteres visibles (`StringInfo`), así nunca parte un emoji. | CA-3.2 |
| `SearchWords(search)` | Separa la búsqueda por espacios y descarta las partes vacías. Una búsqueda de solo espacios da una lista vacía, que significa "sin búsqueda". | CA-4.1, CA-4.7 |
| `LikePattern(word)` | Escapa `\`, `%` y `_`, y rodea con `%`. Se aplica a cada palabra. | CA-4.5 |

### 4.2 `IdeaService` (casos de uso)

```
CreateAsync(userId, text, tags, now):
    texto = NormalizeText(text); etiquetas = DistinctTags(tags.Select(NormalizeTag))
    error = ValidateText(texto) ?? ValidateTags(etiquetas)        → Invalid
    etiquetas = UseExistingSpelling(userId, etiquetas)             (D-T5, CA-2.2)
    guardar con CreatedAt = UpdatedAt = now, ArchivedAt = null     (CA-1.1)

UpdateAsync(userId, id, text, tags, version, now):
    idea = FindOwned(userId, id)                                   → NotFound (CA-8.1)
    si idea.ArchivedAt != null                                     → Conflict (CA-5.4)
    si idea.Version != version                                     → Conflict IdeaChanged (CA-5.5)
    normalizar y validar como en Create                            → Invalid (CA-5.2)
    si el texto y las etiquetas no cambiaron: Ok sin guardar       (CA-5.3)
    guardar con UpdatedAt = now, usando version como valor original del token (D-T12)
    si EF Core lanza DbUpdateConcurrencyException: releer la idea
        ya no existe                                               → NotFound (CA-8.1)
        existe (otra pestaña guardó en el medio)                   → Conflict IdeaChanged (CA-5.5)

ListAsync(userId, archived, search, tag):
    consulta del dueño, filtrada por sección (ArchivedAt null o no)
    si search vacía o de solo espacios: ignorarla                  (CA-4.7)
    si search: unaccent(Text) ILIKE unaccent(LikePattern(search))  (CA-4.1, CA-4.5)
               con varias palabras: una condición por palabra de SearchWords, unidas con Y (D-4)
               cada palabra vale si está en el texto o en alguna etiqueta (D-5):
                 unaccent(Text) ILIKE p  O  alguna etiqueta con unaccent(etiqueta) ILIKE p
    si tag:    alguna etiqueta de la idea es igual en minúsculas   (CA-4.2, CA-4.3, CA-2.11)
    ordenar por CreatedAt desc, luego Id desc, en las dos secciones (CA-3.1)
    devolver { id, Summary(Text), tags, createdDate }              (CA-3.2, D-T3, D-T10)

ArchiveAsync / RestoreAsync(userId, id, now):  poner o quitar ArchivedAt  → NotFound si no existe
DeleteAsync(userId, id):                       borrar                       → NotFound si no existe
TagCountsAsync(userId):                        por etiqueta, ideas activas y archivadas (CA-2.9, CA-4.6)
```

`UseExistingSpelling` lee las etiquetas distintas del dueño (son pocas: hasta 200) y reemplaza cada etiqueta nueva por la existente con las mismas letras sin distinguir mayúsculas.

### 4.3 `logic/tags.js` (JavaScript, puras)

| Función | Devuelve | RF |
|---|---|---|
| `normalizeTag(text)` | Sin espacios en los extremos y sin `#` inicial (misma regla que el backend, para mostrar el chip ya limpio). | CA-2.3 |
| `splitTags(text)` | Lo escrito separado por comas, ya normalizado y sin partes vacías: "libros, cine" da `["libros", "cine"]`. | CA-2.10 |
| `suggestTags(allTags, prefix, chosen)` | Etiquetas que empiezan con `prefix` sin distinguir mayúsculas, sin las ya elegidas, como máximo 8. | CA-2.1 |
| `addTag(chosen, tag, allTags)` | La lista con la etiqueta agregada en su ortografía existente, sin repetir; rechaza si ya hay 10. | CA-2.2, CA-2.6, CA-2.7 |
| `filterOptions(tagCounts, archived)` | Etiquetas con al menos una idea en la sección que se ve. | CA-4.6 |

La regla final la aplica siempre el backend: estas funciones solo mejoran lo que se ve mientras se escribe.

## 5. Cómo se ve en la interfaz

- **Pestañas** arriba: "Hábitos" e "Ideas", junto al botón "Salir".
- **Pantalla de ideas:**
  - Arriba, el formulario de captura: un cuadro de texto grande con foco al abrir la pantalla, el campo de etiquetas y "Guardar". Anotar una idea de una línea es escribir y guardar, con el botón o con Ctrl+Enter (RNF-2). Una etiqueta se agrega con Enter, con una coma o eligiendo una sugerencia (CA-2.10).
  - Debajo, los filtros: búsqueda, selector de etiqueta, pestañas "Activas" y "Archivadas".
  - Después, la lista: cada tarjeta muestra el resumen, las etiquetas como chips y la fecha, con las acciones "Editar", "Archivar" (o "Restaurar") y "Eliminar". Al hacer clic en el resumen se abre el texto completo. En la sección de archivadas no aparece "Editar": la idea se abre solo para leer (CA-3.3, CA-5.4).
- **Sin ideas activas:** "Todavía no tienes ideas. Anota la primera arriba."
- **Sin resultados:** "No hay ideas que coincidan." con el botón "Quitar filtros", que limpia la búsqueda y la etiqueta y deja la sección como está (CA-4.4).
- **Eliminar:** `window.confirm("Se borrará la idea para siempre. Si puede volver a servirte, archívala. ¿Eliminar?")`.
- **Si el guardado falla:** el formulario no se limpia y el mensaje aparece arriba (CA-1.5).
- **Si la idea cambió mientras se editaba (409):** aparece el `message` de la API y, junto al formulario, la versión nueva en modo lectura, pedida de nuevo con `GET /api/ideas/{id:int}`. El formulario conserva lo escrito. "Usar la versión nueva" carga esa versión en el formulario; "Guardar" envía lo escrito con la `version` nueva (CA-5.5). Si la idea quedó archivada, se muestra archivada y sin "Editar" (CA-5.4).

## 6. Estrategia de tests

### 6.1 Backend (xUnit, plantilla sin agregados)

- **`IdeaRulesTests`:**
  - texto vacío, de solo saltos de línea y de 5.000 contra 5.001 caracteres;
  - recorte de extremos conservando saltos internos;
  - etiqueta con `#` y espacios, vacía, con coma, de 30 contra 31 caracteres, 10 contra 11 etiquetas, y lista nula;
  - repetidas con distinta mayúscula;
  - resumen con primera línea vacía, línea de 120 contra 121 caracteres, y un emoji justo en el corte;
  - patrón con `%`, `_` y `\`;
  - palabras de búsqueda: varias separadas por uno o más espacios, y solo espacios da una lista vacía.
- **`IdeaServiceTests`** (base de tests):
  - crear guarda las fechas y queda activa;
  - "App" después de "app" usa "app";
  - orden por creación en las dos secciones, y editar o archivar no cambia el orden;
  - `createdDate` en la zona del dueño (una idea creada a las 03:00 UTC sale con el día anterior en `America/La_Paz`);
  - búsqueda sin mayúsculas ni acentos ("cancion" encuentra "Canción");
  - búsqueda de `%` literal, y búsqueda de solo espacios ignorada;
  - búsqueda con varias palabras en cualquier orden ("sueño app" encuentra "Una app para medir el sueño", y "sueño gato" no la encuentra);
  - búsqueda que encuentra por etiqueta ("salud" encuentra una idea etiquetada "salud" que no lo dice en el texto), y mezcla de texto y etiqueta ("sueño salud");
  - filtro por etiqueta, y búsqueda más filtro;
  - filtro y conteo con "App" y "app" guardadas en ideas distintas: una sola etiqueta, con la escritura más usada;
  - editar con una versión vieja da Conflict y no modifica la idea (CA-5.5);
  - otra pestaña guarda entre la lectura y el guardado: Conflict, no NotFound (con `BeforeSaveInterceptor`);
  - archivar y restaurar cambian la versión, y guardar sin cambios no la cambia;
  - archivar saca de la lista activa, y restaurar conserva texto, etiquetas y fechas;
  - editar una archivada da Conflict;
  - guardar sin cambios no toca `UpdatedAt`;
  - eliminar, y acciones sobre una idea inexistente dan NotFound;
  - una idea borrada entre la lectura y el guardado da NotFound (con `BeforeSaveInterceptor`);
  - conteo de etiquetas por sección, y una etiqueta sin ideas deja de aparecer;
  - otro dueño no ve ni modifica las ideas.

### 6.2 Frontend (`node --test`)

- **`tags.test.js`:** sugerencias por prefijo sin mayúsculas y sin las elegidas; límite de 8 sugerencias; agregar con ortografía existente; no repetir; tope de 10; separar por comas; opciones de filtro por sección.
- **`http.test.js`:** se mueven los tests de sesión que hoy están en `habitsApi.test.js` (token, renovación, carrera de "Salir", 401), porque la lógica pasa a `http.js`. Nuevos: un 404 o un 409 muestran el `message` que manda la API, y sin `message` se usa el genérico.

### 6.3 En el navegador

Lo que es solo interfaz: foco en el cuadro al abrir, Ctrl+Enter para guardar, chips con Enter y coma, sugerencias, "Quitar filtros" que mantiene la sección, aviso y versión nueva al editar en dos pestañas, espera de 300 ms en la búsqueda, archivada abierta sin "Editar", confirmación al eliminar, formulario que no se limpia si falla, y doble clic en "Guardar". Cada punto tiene su tarea en `docs/tasks/003-anotador-de-ideas.md`.

### 6.4 Rendimiento (RNF-3)

Con 5.000 ideas y 200 etiquetas generadas en `asistente_test`, medir lista, búsqueda y filtro como en la spec 001. Si la búsqueda supera 1 segundo, la mejora prevista es un índice trigram sobre el texto sin acentos (requiere otra extensión, se registraría en `decisions.md`).

## 7. Matriz de cobertura

| RF | Backend | Frontend | Tests |
|---|---|---|---|
| RF-1 Anotar | `IdeaRules` (texto), `IdeaService.Create` | `IdeaComposer`, `useIdeas` | `IdeaRulesTests`, `IdeaServiceTests`, navegador (CA-1.5) |
| RF-2 Etiquetas | `IdeaRules` (etiquetas), `UseExistingSpelling` | `TagInput`, `logic/tags.js` | `IdeaRulesTests`, `IdeaServiceTests`, `tags.test.js` |
| RF-3 Ver | `List` (orden, resumen), `GET /{id}` | `IdeaList`, `IdeaCard` | `IdeaServiceTests`, `IdeaRulesTests` (resumen) |
| RF-4 Buscar y filtrar | `List` (unaccent, ILIKE, etiqueta), `LikePattern` | `IdeaFilters`, `filterOptions` | `IdeaServiceTests`, `IdeaRulesTests`, `tags.test.js` |
| RF-5 Editar | `IdeaService.Update`, versión `xmin` (D-T12) | `IdeaComposer` (modo edición y aviso de versión nueva) | `IdeaServiceTests`, navegador (CA-5.5) |
| RF-6 Archivar y restaurar | `Archive`, `Restore`, `List` por sección | Pestañas Activas/Archivadas | `IdeaServiceTests` |
| RF-7 Eliminar | `IdeaService.Delete` | `window.confirm` en `useIdeas` | `IdeaServiceTests`, navegador |
| RF-8 Consistencia | NotFound, interceptor de carrera | `http.js` (401), bloqueo de "Guardar" | `IdeaServiceTests`, `http.test.js`, navegador |

## 8. Riesgos

- **`unaccent` en otra base:** está disponible en la instalación local de PostgreSQL 18, pero cada base donde se publique la app tiene que permitir `CREATE EXTENSION unaccent`. Se verifica cuando se elija el hosting.
- **Traducción de consultas sobre `text[]`:** la lectura de etiquetas distintas, el filtro en minúsculas y la búsqueda dentro de las etiquetas se verifican en sus tareas (T08 y T10). Si EF Core no traduce alguna, se usa una consulta SQL parametrizada con `unnest`.
- **`xmin` como versión:** hay que confirmar que Npgsql 10 lo mapea con `IsRowVersion()` sin crear una columna nueva en la migración, y que el `BeforeSaveInterceptor` puede simular otra pestaña guardando en el medio. Se verifica en T07 y T11.
- **Mover `IsBlank`, `Normalize` y `CountChars` a `TextRules`:** toca código de hábitos que ya funciona. Los tests de hábitos tienen que seguir pasando sin cambios en su comportamiento.
