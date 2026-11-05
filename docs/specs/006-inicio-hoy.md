# Spec 006 — Inicio "Hoy" y navegación

## 1. Contexto y objetivo

La app tiene tres pestañas ("Hábitos", "Ideas", "Métricas") y cada una muestra una sola cosa. Al abrirla no hay un lugar que diga qué toca hoy, la pantalla se ve vacía y, para capturar una idea, hay que cambiar de pestaña.

**Objetivo:** que al iniciar sesión el dueño aterrice en una pantalla "Hoy" que responda tres preguntas sin hacer clic de más: qué hábitos me faltan hoy, qué ideas necesitan atención y dónde anoto algo nuevo. La navegación pasa a ser una barra lateral en escritorio y una barra inferior en móvil, con cuatro destinos.

**Qué no es:** no cambia las reglas de rachas, de cumplimiento ni de correo. No agrega endpoints. No es un rediseño de "Hábitos", "Ideas" ni "Métricas": esas pantallas conservan su contenido y solo cambian de lugar en la navegación.

**Referencia:** prototipo A de `prototipos/a-hoy.html`, aprobado por el dueño. El prototipo es visual; esta spec manda sobre el comportamiento.

## 2. Usuarios

- **Dueño (único usuario):** la persona con la cuenta de la spec 002.

## 3. Dependencias

- **Spec 001 (hábitos):** `GET /api/habits` (con `today` y el historial), `POST` y `DELETE` de `/api/habits/{id}/log/{date}`. Las reglas de estado y de semana cumplida (RF-6, RF-7) no cambian.
- **Spec 003 (ideas):** `GET /api/ideas`, `POST /api/ideas`. La regla de 14 días sin editar viene de la spec 005.
- **Spec 004 (métricas):** la pestaña "Métricas" se mueve al nuevo menú; su contenido no cambia.
- **Spec 005 (correo):** el resumen sale a las 8:00 (hora del dueño). Esta spec solo lo muestra como texto informativo.

## 4. Definiciones

- **Hoy:** la fecha `today` que devuelve la API, en la zona horaria del dueño.
- **Pendiente hoy:** hábito diario sin registro para hoy, o hábito semanal cuya semana actual aún no llegó a su meta. Un hábito pendiente no es un fallo: hoy todavía puede cumplirse.
- **Idea sin editar:** idea activa cuyo `updatedAt` es de hace 14 días o más (mismo criterio que la spec 005).

## 5. Historias de usuario

- **HU-1:** Como dueño, quiero ver al abrir la app qué hábitos me faltan hoy, para decidir qué hacer ahora.
- **HU-2:** Como dueño, quiero marcar un hábito desde "Hoy", para no tener que ir a "Hábitos".
- **HU-3:** Como dueño, quiero anotar una idea desde "Hoy", para no perderla.
- **HU-4:** Como dueño, quiero ver las ideas que llevan 14 días sin tocar, para decidir si las retomo o las archivo.
- **HU-5:** Como dueño, quiero moverme entre secciones con una navegación que siempre esté a la vista, para no perderme en la app.

## 6. Requisitos funcionales

Notación EARS, igual que en las specs anteriores.

### RF-1 — Pestaña inicial y navegación (HU-5)

- **CA-1.1:** El sistema deberá ofrecer cuatro destinos: "Hoy", "Hábitos", "Ideas" y "Métricas", disponibles solo con sesión válida.
- **CA-1.2:** Cuando el dueño inicie sesión, el sistema deberá abrir "Hoy".
- **CA-1.3:** En pantallas de 860 px o más, el sistema deberá mostrar la navegación como barra lateral fija con el nombre de la app, el destino actual marcado y, junto a cada destino, su número clave (hábitos pendientes en "Hoy", ideas activas en "Ideas", cumplimiento de la semana en "Métricas").
- **CA-1.4:** En pantallas de menos de 860 px, el sistema deberá mostrar la navegación como barra inferior fija con los cuatro destinos. Ningún elemento de la barra deberá quedar tapado por el contenido.
- **CA-1.5:** El destino actual deberá indicarse con `aria-current="page"`, y la navegación deberá ser operable con teclado.
- **CA-1.6:** Si la sesión venció, entonces el sistema deberá llevar al inicio de sesión con el mensaje de sesión expirada (spec 002, CA-3.3).
- **CA-1.7:** El botón "Salir" deberá seguir visible en ambas navegaciones.

### RF-2 — Resumen del día (HU-1, HU-2)

- **CA-2.1:** El sistema deberá mostrar la fecha de hoy con el día de la semana en español.
- **CA-2.2:** El sistema deberá mostrar cuántos hábitos están hechos hoy sobre el total de hábitos, con un anillo de progreso. Si no hay hábitos, deberá mostrar un mensaje que invite a crear el primero en "Hábitos", sin anillo.
- **CA-2.3:** El sistema deberá listar los hábitos en el mismo orden que la spec 001 (CA-2.2), cada uno con su estado de hoy.
- **CA-2.4:** Cada hábito deberá mostrar una línea con su meta: "Diario" para hábitos diarios, o "N veces por semana · X de N esta semana" para semanales. Si la racha actual es de 2 días o más, deberá mostrarla a la derecha.
- **CA-2.5:** Cuando el dueño marque o desmarque un hábito, el sistema deberá actualizar el anillo y el contador sin recargar la pantalla, y llamar a la API del registro de hoy.
- **CA-2.6:** Si la llamada a la API falla, entonces el sistema deberá deshacer el cambio visual y mostrar un mensaje de error breve.
- **CA-2.7:** Un hábito semanal que ya cumplió su meta deberá mostrarse como hecho aunque no se haya marcado hoy, y no deberá ofrecer marcarse de nuevo.

### RF-3 — Captura rápida (HU-3)

- **CA-3.1:** El sistema deberá mostrar un área de texto "Anota lo que se te ocurra" con un botón "Guardar idea".
- **CA-3.2:** Cuando el dueño envíe una idea con texto, el sistema deberá crearla con `POST /api/ideas`, limpiar el área y mostrar la idea en "Para revisar" o en "Últimas ideas", según corresponda.
- **CA-3.3:** Mientras el texto esté vacío o solo tenga espacios, el botón deberá estar deshabilitado.
- **CA-3.4:** Si la creación falla, entonces el sistema deberá conservar el texto escrito y mostrar un mensaje de error.
- **CA-3.5:** El texto de la idea deberá respetar el límite de la spec 003 (5.000 caracteres); el contador o el error aparecen antes de enviar.

### RF-4 — Para revisar (HU-4)

- **CA-4.1:** El sistema deberá mostrar las ideas activas que llevan 14 días o más sin editarse, con su texto recortado a una línea y su fecha de última edición.
- **CA-4.2:** Si no hay ideas en esa situación, el sistema deberá ocultar la sección, sin mensaje vacío.
- **CA-4.3:** El sistema deberá calcular esta lista en una función pura de `src/logic/` que reciba `today` como parámetro.
- **CA-4.4:** Tocar una idea de "Para revisar" deberá llevar a la pestaña "Ideas" con esa idea abierta.

### RF-5 — Últimas ideas (HU-3)

- **CA-5.1:** El sistema deberá mostrar las cinco ideas activas más recientes por fecha de creación, con su fecha relativa ("hace 2 h", "ayer").
- **CA-5.2:** Si no hay ideas activas, el sistema deberá mostrar el texto "Todavía no anotaste ideas", sin botones adicionales.

### RF-6 — Resumen por correo (HU-1)

- **CA-6.1:** El sistema deberá mostrar al pie de la barra lateral el texto "Resumen por correo: 08:00", como información. No deberá permitir cambiar la hora.

### RF-7 — Estados de carga y error

- **CA-7.1:** Mientras "Hoy" carga, el sistema deberá mostrar un esquema vacío con la misma estructura, sin cifras de relleno.
- **CA-7.2:** Si la lista de hábitos no se puede cargar, entonces el sistema deberá mostrar un mensaje con la opción de reintentar y no deberá mostrar cifras parciales (igual que CA-1.4 de la spec 004).

## 7. Alcance técnico (orientativo)

- `src/logic/hoy.js`: funciones puras para pendientes de hoy, progreso del día, ideas sin editar y últimas ideas. Reciben `today` como parámetro.
- `src/hooks/useHoy.js`: carga hábitos e ideas, y expone las acciones de marcar y guardar.
- `src/pages/HoyPage.jsx` y `src/components/`: solo muestran.
- `src/App.jsx`: pasa a cuatro destinos y a la nueva navegación. Las pestañas de "Hábitos", "Ideas" y "Métricas" no cambian su contenido.
- Sin endpoints nuevos ni cambios en `Services/`.

## 8. Fuera de alcance

- Cambiar la hora del correo o el contenido del resumen.
- Alertas de rachas perdidas y "en riesgo" dentro de la app (esas siguen en el correo de la spec 005).
- Modo oscuro manual: se mantiene el seguimiento del sistema operativo.
- Edición de ideas o etiquetas desde "Hoy" (se hace en "Ideas").
- Cambios en los gráficos de "Métricas".

## 9. Decisiones tomadas

- **"Para revisar" solo muestra ideas sin editar.** Las alertas de hábitos en riesgo no se repiten en "Hoy": la regla vive en el servidor y duplicarla en el cliente sería una segunda fuente de verdad.
- **La pestaña inicial pasa a ser "Hoy".** Si las specs 001 o 002 fijan otra pestaña inicial, esta spec la reemplaza y se actualizan en el mismo commit.
- **Sin endpoints nuevos.** Todo sale de `GET /api/habits` y `GET /api/ideas`.
