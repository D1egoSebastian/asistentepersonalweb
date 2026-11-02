# Spec 003 — Anotador de ideas

## 1. Contexto y objetivo

Las ideas aparecen en cualquier momento: en la calle, leyendo, programando. Hoy se pierden en notas sueltas del teléfono o en la memoria, y cuando se quieren retomar no se encuentran, o no se sabe cuáles hablaban del mismo tema.

**Objetivo:** que el dueño pueda anotar una idea en segundos, agruparla con las etiquetas que él elija, encontrarla después por texto o por etiqueta, y decidir qué hacer con las ideas que ya no le sirven: archivarlas (para recuperarlas si vuelven a servir) o eliminarlas.

**Qué no es:** no es una lista de tareas. Las ideas no se "completan" ni tienen estados de avance. Son un lugar para pensar y guardar, no para cumplir.

**Por qué ahora:** es una de las funcionalidades centrales del Asistente Personal desde el inicio, y no depende de las métricas ni de las notificaciones.

## 2. Usuarios

- **Dueño (único usuario):** la persona con la cuenta de la spec 002. Solo él ve y modifica sus ideas.

## 3. Dependencias

- **Spec 002 (cuenta del dueño, inicio y cierre de sesión):** ya implementada. Todas las acciones de esta spec exigen sesión válida.

## 4. Definiciones

- **Idea:** un texto escrito por el dueño, con cero o más etiquetas, una fecha de creación y una fecha de última modificación.
- **Resumen:** la primera línea con texto de la idea. Es lo que se ve en la lista.
- **Etiqueta:** una palabra o frase corta que el dueño elige para agrupar ideas (por ejemplo, "negocio" o "lluvia de ideas"). "Negocio" y "negocio" son la misma etiqueta.
- **Idea activa:** una idea que aparece en la lista principal.
- **Idea archivada:** una idea que el dueño apartó. No aparece en la lista principal, pero se conserva completa y se puede restaurar.
- **Texto vacío:** texto sin caracteres visibles (vacío, solo espacios, solo saltos de línea o solo caracteres invisibles).
- **Carácter:** cada símbolo tal como lo ve el dueño, igual que en la spec 001: un emoji o una letra con tilde cuentan como 1. Los límites y el corte del resumen nunca parten un carácter.
- **Fecha mostrada:** la fecha de calendario en la zona horaria del dueño, la misma que define "hoy" en la spec 001 (D-1). Una idea anotada a las 23:30 se muestra con la fecha de ese día, sin importar la zona del navegador.

## 5. Historias de usuario

- **HU-1:** Como dueño, quiero anotar una idea escribiendo solo el texto, para no perderla mientras pienso en otra cosa.
- **HU-2:** Como dueño, quiero elegir las etiquetas de cada idea, con sugerencias de las que ya usé, para agrupar ideas del mismo tema sin crear etiquetas repetidas.
- **HU-3:** Como dueño, quiero ver mis ideas activas con su resumen y sus etiquetas, para repasarlas de un vistazo.
- **HU-4:** Como dueño, quiero buscar ideas por una palabra, para encontrar una que anoté hace tiempo.
- **HU-5:** Como dueño, quiero filtrar por una etiqueta, para ver juntas todas las ideas de un tema.
- **HU-6:** Como dueño, quiero corregir el texto o las etiquetas de una idea, porque las ideas cambian mientras las pienso.
- **HU-7:** Como dueño, quiero archivar una idea que hoy no me sirve y poder restaurarla, porque a veces una idea descartada vuelve a ser útil.
- **HU-8:** Como dueño, quiero eliminar para siempre una idea que no quiero conservar.

## 6. Requisitos funcionales

Notación EARS: *ubicuo* ("El sistema deberá…"), *evento* ("Cuando…"), *estado* ("Mientras…"), *no deseado* ("Si…, entonces…").

### RF-1 — Anotar una idea (HU-1)

- **CA-1.1:** Cuando el dueño guarde una idea con texto que no sea vacío, el sistema deberá guardarla como activa, con las etiquetas elegidas (o ninguna), y con la fecha de creación y de última modificación iguales al momento de guardar.
- **CA-1.2:** Si el texto es vacío, entonces el sistema deberá rechazar la idea e indicar que el texto es obligatorio.
- **CA-1.3:** Si el texto supera los 5.000 caracteres, entonces el sistema deberá rechazar la idea e indicar el límite (D-3 resuelta).
- **CA-1.4:** El sistema deberá quitar los espacios y saltos de línea al inicio y al final del texto, y conservar los saltos de línea del medio.
- **CA-1.5:** Si el guardado falla, entonces el sistema deberá conservar en el formulario el texto y las etiquetas escritos, para no perder la idea.

### RF-2 — Etiquetas (HU-2)

- **CA-2.1:** Mientras el dueño escribe una etiqueta, el sistema deberá sugerir las etiquetas existentes que empiezan con lo escrito, sin distinguir mayúsculas.
- **CA-2.2:** Cuando el dueño agregue una etiqueta que ya existe con otras mayúsculas, el sistema deberá usar la escritura que ya existe.
- **CA-2.11:** Si por una acción simultánea quedan dos escrituras de la misma etiqueta (por ejemplo, "App" y "app"), entonces el sistema deberá tratarlas como una sola en las sugerencias, en el filtro y en el conteo, y mostrar la escritura que usan más ideas.
- **CA-2.3:** El sistema deberá quitar los espacios al inicio y al final de cada etiqueta, y un "#" al inicio, si lo tiene.
- **CA-2.4:** Si una etiqueta queda vacía, entonces el sistema deberá rechazarla.
- **CA-2.5:** Si una etiqueta supera los 30 caracteres, entonces el sistema deberá rechazarla e indicar el límite (D-3 resuelta).
- **CA-2.6:** Si la idea ya tiene esa etiqueta (sin distinguir mayúsculas), entonces el sistema no deberá agregarla dos veces.
- **CA-2.7:** Si el dueño intenta poner más de 10 etiquetas en una idea, entonces el sistema deberá rechazar la etiqueta extra e indicar el límite (D-3 resuelta).
- **CA-2.8:** Cuando el dueño quite una etiqueta de una idea, el sistema no deberá cambiar las demás ideas que la usan.
- **CA-2.9:** Mientras ninguna idea (activa ni archivada) use una etiqueta, el sistema no deberá sugerirla.
- **CA-2.10:** Cuando el dueño presione Enter o escriba una coma en el campo de etiquetas, o elija una sugerencia, el sistema deberá agregar la etiqueta escrita. Por eso una etiqueta no puede contener comas.

### RF-3 — Ver las ideas (HU-3)

- **CA-3.1:** El sistema deberá mostrar las ideas ordenadas por fecha de creación, de la más nueva a la más antigua, tanto en la sección de activas como en la de archivadas. Editar o archivar una idea no cambia su lugar (D-1 resuelta).
- **CA-3.2:** El sistema deberá mostrar de cada idea su resumen (cortado a 120 caracteres con "…" si es más largo), sus etiquetas y su fecha de creación (fecha mostrada, sección 4).
- **CA-3.3:** Cuando el dueño abra una idea, activa o archivada, el sistema deberá mostrar el texto completo con sus saltos de línea. Una idea archivada se abre solo para leer (CA-5.4).
- **CA-3.4:** Mientras no existan ideas activas, el sistema deberá mostrar un mensaje que invite a anotar la primera.

### RF-4 — Buscar y filtrar (HU-4, HU-5)

- **CA-4.1:** Cuando el dueño escriba en la búsqueda, el sistema deberá mostrar solo las ideas cuyo texto contenga lo escrito, sin distinguir mayúsculas ni acentos: "cancion" encuentra "canción", y "CANCIÓN" encuentra "cancion" (D-2 resuelta). Con varias palabras, el sistema deberá mostrar las ideas que contengan todas, en cualquier orden: "sueño app" encuentra "Una app para medir el sueño" (D-4 resuelta). Cada palabra puede estar en el texto o en alguna etiqueta de la idea, con las mismas reglas de mayúsculas y acentos: "sueño salud" encuentra una idea con el texto "medir el sueño" y la etiqueta "salud" (D-5 resuelta).
- **CA-4.2:** Cuando el dueño elija una etiqueta del filtro, el sistema deberá mostrar solo las ideas que tienen esa etiqueta.
- **CA-4.3:** Mientras haya una búsqueda y una etiqueta elegidas a la vez, el sistema deberá mostrar solo las ideas que cumplen las dos condiciones.
- **CA-4.4:** Si ninguna idea coincide, entonces el sistema deberá mostrar "No hay ideas que coincidan." y un botón "Quitar filtros" que limpia la búsqueda y la etiqueta, y mantiene la sección (activas o archivadas) que se está viendo.
- **CA-4.5:** El sistema deberá tratar los caracteres especiales de la búsqueda (como `%` o `_`) como texto común.
- **CA-4.6:** El sistema deberá ofrecer en el filtro solo las etiquetas que tienen al menos una idea en la sección que se está viendo (activas o archivadas).
- **CA-4.7:** Si la búsqueda está vacía o tiene solo espacios, entonces el sistema deberá tratarla como si no hubiera búsqueda.

### RF-5 — Editar una idea (HU-6)

- **CA-5.1:** Cuando el dueño guarde cambios en el texto o las etiquetas de una idea activa, el sistema deberá guardarlos, actualizar la fecha de última modificación y conservar la fecha de creación.
- **CA-5.2:** Si los cambios no cumplen las reglas de RF-1 o RF-2, entonces el sistema deberá rechazar la edición sin modificar la idea.
- **CA-5.3:** Cuando el dueño guarde sin haber cambiado nada, el sistema no deberá modificar la fecha de última modificación.
- **CA-5.4:** Si el dueño intenta editar una idea archivada, entonces el sistema deberá rechazar la edición e indicar que primero hay que restaurarla.
- **CA-5.5:** Si la idea cambió desde que el dueño la abrió para editar (por ejemplo, desde otra pestaña), entonces el sistema deberá rechazar la edición sin modificar la idea, avisar "Esta idea cambió mientras la editabas. Esta es la versión nueva." y mostrar la versión nueva junto al formulario, que conserva lo que el dueño escribió (CA-1.5). Desde ahí el dueño elige: "Usar la versión nueva" descarta lo suyo, y "Guardar" aplica lo suyo sobre la versión nueva, que ya vio.

### RF-6 — Archivar y restaurar (HU-7)

- **CA-6.1:** Cuando el dueño archive una idea, el sistema deberá sacarla de la lista principal, de su búsqueda y de su filtro, sin pedir confirmación (la acción se puede deshacer).
- **CA-6.2:** El sistema deberá mostrar las ideas archivadas en una sección aparte, con la misma búsqueda y el mismo filtro por etiqueta.
- **CA-6.3:** Cuando el dueño restaure una idea archivada, el sistema deberá devolverla a la lista principal con su texto, sus etiquetas y su fecha de creación originales, y deberá reiniciar su fecha de última edición (`UpdatedAt`) al momento de restaurar, para que el recordatorio de ideas sin editar (spec 005, S-3) cuente desde ahí. El orden de la lista no cambia, porque se ordena por fecha de creación.
- **CA-6.4:** Mientras una idea esté archivada, el sistema deberá conservar sus etiquetas, y esas etiquetas deberán seguir sugiriéndose (CA-2.9).

### RF-7 — Eliminar una idea (HU-8)

- **CA-7.1:** Cuando el dueño pida eliminar una idea (activa o archivada), el sistema deberá pedir una confirmación que avise que la eliminación es definitiva y que, si puede volver a servir, conviene archivarla.
- **CA-7.2:** Cuando el dueño confirme, el sistema deberá borrar la idea de forma definitiva, junto con su relación con las etiquetas. Las demás ideas conservan sus etiquetas (CA-2.8).
- **CA-7.3:** Si el dueño cancela la confirmación, entonces el sistema no deberá modificar nada.

### RF-8 — Consistencia

- **CA-8.1:** Si el dueño actúa sobre una idea que ya no existe (por ejemplo, eliminada desde otra pestaña), entonces el sistema deberá rechazar la acción, indicar "Esta idea ya no existe." y actualizar la lista.
- **CA-8.2:** Mientras una idea se está guardando, la interfaz deberá bloquear "Guardar", para que un doble clic envíe la idea una sola vez. Dos ideas con el mismo texto son válidas cuando el dueño las anota a propósito, así que el sistema no rechaza duplicados.
- **CA-8.3:** Si la sesión venció, entonces el sistema no deberá aplicar ningún cambio y deberá llevar al inicio de sesión con el mensaje de sesión expirada (spec 002, CA-3.3).

## 7. Requisitos no funcionales

- **RNF-1 (privacidad):** solo el dueño autenticado puede ver, buscar o modificar sus ideas.
- **RNF-2 (rapidez de captura):** al abrir la pantalla de ideas, el cuadro de texto tiene el foco. Anotar una idea de una línea sin etiquetas lleva dos acciones: escribir, y guardar con el botón o con Ctrl+Enter.
- **RNF-3 (rendimiento):** con 5.000 ideas y 200 etiquetas, la lista, la búsqueda y el filtro se muestran en menos de 1 segundo, medido como en la spec 001 (RNF-4). No hay tope de ideas; por encima de 5.000, el tiempo no está garantizado.
- **RNF-4 (verificabilidad):** las reglas de texto, de etiquetas, de búsqueda y de archivado (RF-1, RF-2, RF-4, RF-6) se pueden verificar con tests automáticos sin depender de la interfaz.
- **RNF-5 (idioma):** todos los textos visibles, incluidos los mensajes de error, están en español neutro.

## 8. Casos límite

- **Texto con solo saltos de línea o espacios:** se rechaza por vacío (CA-1.2).
- **Primera línea vacía y texto en la segunda:** el resumen usa la primera línea con texto (definición de resumen).
- **Idea larga en una sola línea:** el resumen se corta a 120 caracteres con "…" (CA-3.2).
- **Etiqueta escrita como "#App":** se guarda como "App", o como la versión existente si ya había una "app" (CA-2.2, CA-2.3).
- **Etiqueta con espacios en el medio** ("lluvia de ideas"): se acepta tal cual (CA-2.3 solo recorta los extremos).
- **Etiqueta usada solo en ideas archivadas:** se sigue sugiriendo (CA-6.4), pero no aparece en el filtro de la lista principal (CA-4.6).
- **Quitar la última idea que usaba una etiqueta:** la etiqueta deja de sugerirse (CA-2.9).
- **Archivar en una pestaña y editar en otra:** la edición se rechaza por estar archivada (CA-5.4), y la lista se actualiza.
- **Editar en una pestaña y archivar en otra:** el archivado se aplica a la versión más reciente, porque no cambia el texto ni las etiquetas. La edición que quedó abierta se rechaza al guardar, con el aviso de idea archivada (CA-5.4).
- **Eliminar en una pestaña y editar, archivar o restaurar en otra:** se rechaza con "Esta idea ya no existe." (CA-8.1).
- **Búsqueda con `%` o `_`:** se buscan como caracteres comunes (CA-4.5).
- **Búsqueda sin tilde:** "cancion" encuentra "canción", y al revés (CA-4.1).
- **Guardar falla por la red:** el texto escrito no se pierde (CA-1.5).
- **Doble clic en "Guardar":** se envía una sola vez (CA-8.2).
- **"App" y "app" guardadas a la vez desde dos pestañas:** se tratan como una sola etiqueta (CA-2.11).
- **Etiqueta escrita con una coma ("libros, cine"):** se agregan dos etiquetas, "libros" y "cine" (CA-2.10).
- **Emoji en el corte del resumen:** el corte nunca lo parte (definición de carácter).
- **Búsqueda de solo espacios:** se ignora (CA-4.7).
- **Búsqueda "sueño app":** encuentra "Una app para medir el sueño", aunque las palabras estén en otro orden (CA-4.1).
- **Búsqueda "salud" en una idea que no dice "salud" pero tiene esa etiqueta:** la encuentra (CA-4.1, D-5).
- **Abrir una idea archivada:** se lee el texto completo, sin poder editarlo (CA-3.3, CA-5.4).
- **Editar la misma idea en dos pestañas:** la primera que guarda gana; la segunda se rechaza con el aviso y ve la versión nueva (CA-5.5).
- **Abrir para editar, no cambiar nada en otra pestaña y guardar:** se guarda con normalidad; solo un cambio guardado cuenta como "la idea cambió" (CA-5.5).

## 9. Fuera de alcance (esta versión)

- Estados de avance (nueva, en desarrollo, hecha) o cualquier forma de "completar" una idea.
- Convertir una idea en hábito.
- Renombrar o combinar etiquetas en todas las ideas a la vez.
- Filtrar por varias etiquetas al mismo tiempo.
- Colores o íconos para las etiquetas.
- Formato de texto (negrita, listas), imágenes, adjuntos o vista previa de enlaces.
- Fijar ideas arriba u ordenar a mano.
- Recordatorios o correos sobre ideas (irán con la spec de notificaciones).
- Exportar o compartir ideas.
- Papelera o deshacer después de eliminar (para eso está archivar).
- Búsqueda por rango de fechas.

## 10. Criterios de finalización

- Todos los criterios de aceptación de RF-1 a RF-8 se cumplen.
- Cada regla de RF-1, RF-2, RF-4 y RF-6 tiene al menos un test automático que pasa (principio 4 de la constitución).
- Cada caso límite de la sección 8 tiene una verificación: automática si es una regla, en el navegador si es solo de interfaz.
- El dueño puede, sin ayuda: anotar una idea en dos acciones, ponerle etiquetas usando las sugerencias, buscarla por texto, filtrarla por etiqueta, editarla, archivarla, restaurarla y eliminarla.
- Las dudas D-1 a D-6 están resueltas o diferidas explícitamente antes de empezar a implementar.

## 11. Dudas abiertas

- **D-1** Resuelta: la lista se ordena por fecha de creación, de la más nueva a la más antigua. Editar una idea no la mueve.
- **D-2** Resuelta: la búsqueda ignora mayúsculas y acentos. Las etiquetas siguen comparándose solo sin distinguir mayúsculas (CA-2.2).
- **D-3** Resuelta: 5.000 caracteres por idea, 30 por etiqueta y 10 etiquetas por idea.
- **D-4** Resuelta: con varias palabras, la búsqueda encuentra las ideas que tienen todas, en cualquier orden.
- **D-5** Resuelta: la búsqueda también mira las etiquetas. Cada palabra puede estar en el texto o en una etiqueta.
- **D-6** Resuelta: si la idea cambió mientras se editaba, la segunda edición se rechaza con un aviso y se muestra la versión nueva (CA-5.5).
