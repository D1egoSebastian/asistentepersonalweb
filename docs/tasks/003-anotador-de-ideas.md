# Tareas 003 — Anotador de ideas

Fuentes: [spec](../specs/003-anotador-de-ideas.md) · [plan](../plans/003-anotador-de-ideas.md) · [constitución](../constitution.md)

**Reglas para todas las tareas:**
- Cada tarea dura como máximo 20-30 minutos. Si se pasa, se divide antes de seguir.
- **Las tareas de lógica se hacen con TDD:** primero el test (falla), después el código (pasa).
- Ninguna tarea se marca como hecha si `dotnet test` (desde `backend/`) o `npm test` (desde `frontend/`) fallan (principio 4).
- Si una tarea cambia un comportamiento, la spec se actualiza en el mismo commit (principio 2).
- Las verificaciones con datos usan `asistente_test`, no `asistente_dev`.

---

## Fase 0 — Preparación

- [x] **T00 — Cerrar las dudas D-4 a D-6** · RF: RF-4, RF-5
  Hecho cuando: la sección 11 de la spec no tiene `[NECESITA ACLARACIÓN]`, y el plan (§4.2 y §6.1) y las tareas T10 y T11 reflejan las respuestas.

- [x] **T01 — Registrar `unaccent`** · RF: RF-4
  Hecho cuando: `docs/decisions.md` tiene una entrada para la extensión `unaccent`, con qué resuelve, por qué no alcanza lo que hay y el riesgo de que el hosting no la permita.

- [x] **T02 — Mover las reglas de texto a `TextRules`** · RF: RF-1
  Hecho cuando: `IsBlank`, `Normalize` y `CountChars` viven en `TextRules`, sus tests están en `TextRulesTests`, `HabitRules` y `AuthRules` los usan desde ahí, el comentario de `OwnerClock` dice que es lo único que decide qué día es "hoy" (plan D-T11), y `dotnet test` pasa sin cambiar ningún test de hábitos ni de login.

- [x] **T03 — `request()` único en `http.js`** · RF: RF-8 (CA-8.1, CA-8.3)
  Hecho cuando: `habitsApi.js` y `authApi.js` usan `http.js` y no tienen su propio `request()`; los tests de sesión (token, renovación, carrera de "Salir", 401) están en `http.test.js`; un 404 o un 409 muestran el `message` que manda la API, y sin `message` el genérico; los mensajes de hábitos siguen iguales en pantalla; y `npm test` y `npm run build` pasan.

## Fase 1 — Reglas puras del backend (`IdeaRules`, TDD)

- [ ] **T04 — Texto de la idea** · RF: RF-1 (CA-1.2, CA-1.3, CA-1.4)
  Hecho cuando: pasan los tests de texto vacío, de solo saltos de línea, de 5.000 contra 5.001 caracteres, y de recorte de extremos conservando los saltos internos.

- [ ] **T05 — Etiquetas** · RF: RF-2 (CA-2.3 a CA-2.7, CA-2.10)
  Hecho cuando: pasan los tests de `#` y espacios en los extremos, etiqueta vacía, etiqueta con coma, lista nula como "sin etiquetas", 30 contra 31 caracteres, 10 contra 11 etiquetas, y repetidas con distinta mayúscula (queda la primera escritura).

- [ ] **T06 — Resumen y patrón de búsqueda** · RF: RF-3 (CA-3.2), RF-4 (CA-4.5)
  Hecho cuando: pasan los tests de resumen con la primera línea vacía, de 120 contra 121 caracteres (con "…"), de un emoji justo en el corte (no se parte), de patrón que escapa `%`, `_` y `\`, y de palabras de búsqueda separadas por uno o más espacios (solo espacios da una lista vacía).

## Fase 2 — Datos

- [ ] **T07 — Entidad `Idea` y migración `AddIdeas`** · RF: RF-1, RF-4, RF-6
  Hecho cuando: la migración crea `Ideas` con etiquetas como `text[]`, `ArchivedAt` opcional, `Version` mapeada a `xmin` sin columna nueva (plan D-T12), el índice `(UserId, ArchivedAt, CreatedAt)` y `CREATE EXTENSION IF NOT EXISTS unaccent`; está aplicada en `asistente_dev` y `asistente_test`; y `TestDatabase` limpia `Ideas` antes de cada test.

## Fase 3 — `IdeaService` (TDD contra la base de tests)

- [ ] **T08 — `CreateAsync` y ortografía de etiquetas** · RF: RF-1, RF-2 (CA-1.1, CA-2.2)
  Hecho cuando: pasan los tests de crear con fechas iguales y estado activo, rechazo de datos inválidos, y "App" después de "app" guardado como "app". Queda anotado si la lectura de etiquetas distintas se tradujo con EF Core o con SQL parametrizado (plan §8).

- [ ] **T09 — `ListAsync`: sección, orden y resumen** · RF: RF-3, RF-6 (CA-3.1, CA-3.2, CA-6.1)
  Hecho cuando: pasan los tests de orden por creación (la más nueva primero) en las dos secciones, de que editar o archivar no cambia el orden, de que la lista devuelve el resumen y no el texto, de `createdDate` en la zona del dueño (una idea de las 03:00 UTC sale con el día anterior en `America/La_Paz`), y de que las archivadas no aparecen en la sección activa.

- [ ] **T10 — `ListAsync`: búsqueda y filtro** · RF: RF-4 (CA-4.1 a CA-4.7), RF-2 (CA-2.11)
  Hecho cuando: pasan los tests de "cancion" encuentra "Canción" y al revés, `%` buscado como texto, búsqueda de solo espacios ignorada, varias palabras en cualquier orden ("sueño app" encuentra "Una app para medir el sueño", "sueño gato" no) (D-4), búsqueda que encuentra por etiqueta ("salud") y que mezcla texto y etiqueta ("sueño salud") (D-5), filtro por etiqueta, filtro que trae "App" y "app" juntas, y búsqueda más filtro a la vez.

- [ ] **T11 — `UpdateAsync`** · RF: RF-5, RF-8 (CA-5.1 a CA-5.5, CA-8.1)
  Hecho cuando: pasan los tests de edición que actualiza `UpdatedAt` y conserva `CreatedAt`, guardar sin cambios sin tocar `UpdatedAt`, datos inválidos sin modificar nada, idea archivada → Conflict, versión vieja → Conflict sin modificar la idea (D-6), otra pestaña que guarda entre la lectura y el guardado → Conflict, archivar y restaurar cambian la versión y guardar sin cambios no, e idea borrada entre la lectura y el guardado → NotFound.

- [ ] **T12 — Archivar, restaurar y eliminar** · RF: RF-6, RF-7, RF-8 (CA-6.1, CA-6.3, CA-6.4, CA-7.2, CA-8.1)
  Hecho cuando: pasan los tests de archivar, restaurar conservando texto, etiquetas y fechas, eliminar, y las tres acciones sobre una idea inexistente → NotFound.

- [ ] **T13 — Conteo de etiquetas y dueño** · RF: RF-2, RF-4, RNF-1 (CA-2.9, CA-2.11, CA-4.6)
  Hecho cuando: pasan los tests de conteo de ideas activas y archivadas por etiqueta, de que "App" y "app" cuentan como una sola con la escritura más usada, de que una etiqueta sin ideas deja de aparecer, y de que otro dueño no ve ni modifica las ideas.

## Fase 4 — API

- [ ] **T14 — DTOs, `IdeasController` y registro** · RF: RF-1 a RF-8
  Hecho cuando: los 8 endpoints del plan delegan en `IdeaService`, las rutas con id usan `{id:int}`, el controller pasa `DateTimeOffset.UtcNow` como `now`, y verificado con `curl` contra `asistente_test`: sin token responden 401; con token, crear (200 y 400), listar con búsqueda y filtro, abrir una activa y una archivada (con `version`), editar (200 con `version` nueva, 400, 404, 409 por archivada y 409 por versión vieja, cada uno con su `message`), archivar, restaurar, eliminar, conteo de etiquetas, y `/api/ideas/tags` no se confunde con una idea.

## Fase 5 — Frontend: lógica y API

- [ ] **T15 — `logic/tags.js`** · RF: RF-2, RF-4 (CA-2.1, CA-2.3, CA-2.6, CA-2.7, CA-2.10, CA-4.6)
  Hecho cuando: pasan los tests de sugerencias por prefijo sin mayúsculas y sin las elegidas, máximo 8 sugerencias, agregar con la ortografía existente, no repetir, tope de 10, "libros, cine" separado en dos etiquetas, y opciones del filtro por sección.

- [ ] **T16 — `ideasApi.js`** · RF: RF-1 a RF-7
  Hecho cuando: hay una función por endpoint usando `http.js`, y `npm test` y `npm run build` pasan.

## Fase 6 — Interfaz

- [ ] **T17 — `useIdeas`** · RF: RF-3 a RF-8
  Hecho cuando: el hook maneja la sección, la búsqueda con espera de 300 ms, el filtro, la lista, la idea abierta y las acciones; ninguna función supera 30 líneas de lógica; y `npm run build` pasa.

- [ ] **T18 — `IdeasPage` e `IdeaFilters`** · RF: RF-3, RF-4, RF-6 (CA-3.4, CA-4.4, CA-6.2)
  Hecho cuando: en el navegador se ven el mensaje de lista vacía, la búsqueda, el filtro, las pestañas Activas y Archivadas, y "No hay ideas que coincidan." con "Quitar filtros", que limpia búsqueda y etiqueta sin cambiar de sección; y la búsqueda pide resultados recién 300 ms después de la última tecla (verificado en las peticiones de red).

- [ ] **T19 — `IdeaComposer` y `TagInput`** · RF: RF-1, RF-2, RF-5 (CA-1.5, CA-2.1, CA-8.2), RNF-2
  Hecho cuando: en el navegador, al abrir la pantalla el cuadro de texto tiene el foco; una idea de una línea se anota escribiendo y guardando con el botón o con Ctrl+Enter; las etiquetas se agregan como chips con Enter, con coma o eligiendo una sugerencia, y se quitan; editar carga el texto y las etiquetas; un doble clic envía una sola vez; y si el guardado falla, el formulario conserva lo escrito.

- [ ] **T20 — Aviso de idea cambiada** · RF: RF-5 (CA-5.4, CA-5.5)
  Hecho cuando: en el navegador, con la misma idea abierta para editar en dos pestañas, la primera guarda; la segunda, al guardar, muestra "Esta idea cambió mientras la editabas. Esta es la versión nueva." y la versión nueva junto al formulario, que conserva lo escrito; "Usar la versión nueva" la carga en el formulario; "Guardar" de nuevo aplica lo escrito; y si la otra pestaña archivó la idea, se ve archivada y sin "Editar".

- [ ] **T21 — `IdeaList` e `IdeaCard`** · RF: RF-3, RF-6, RF-7 (CA-3.2, CA-3.3, CA-6.1, CA-6.3, CA-7.1, CA-7.3)
  Hecho cuando: en el navegador, cada tarjeta muestra el resumen, las etiquetas y la fecha; el clic abre el texto completo, también en una archivada, que no muestra "Editar"; archivar y restaurar mueven la idea de sección; y eliminar muestra la confirmación del plan, cancelar no cambia nada y aceptar la borra.

- [ ] **T22 — Pestañas en `App` e `ideas.css`** · RF: —
  Hecho cuando: con sesión se ven las pestañas Hábitos e Ideas y "Salir"; la pantalla de ideas se ve bien en 375 px y en escritorio, en modo claro y oscuro, sin desbordamiento horizontal.

## Fase 7 — Cierre

- [ ] **T23 — Medir RNF-3** · RF: RNF-3
  Hecho cuando: con 5.000 ideas y 200 etiquetas generadas en `asistente_test`, la lista, la búsqueda y el filtro tardan menos de 1 segundo, y el tiempo queda anotado en el plan.

- [ ] **T24 — Documentación** · RF: —
  Hecho cuando: `CLAUDE.md` y `AGENTS.md` (iguales salvo la cabecera) reflejan el modelo `Idea`, los endpoints de ideas y las pestañas.

- [ ] **T25 — Verificación final** · RF: todos
  Hecho cuando: `dotnet test` y `npm test` pasan, cada fila de la matriz del plan (§7) tiene su verificación, y la spec refleja cualquier cambio de comportamiento.

- [ ] **T26 — Recorrido del dueño** · RF: RF-1 a RF-8
  Hecho cuando: el dueño, sin ayuda y en `asistente_dev`, anota una idea en dos acciones, le pone etiquetas usando sugerencias, la busca sin tildes, la filtra, la edita, la archiva, la restaura y la elimina.
