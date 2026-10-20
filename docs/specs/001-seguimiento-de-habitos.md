# Spec 001 — Seguimiento de hábitos

## 1. Contexto y objetivo

El Asistente Personal busca ayudar a su dueño a construir hábitos de forma sostenida (por ejemplo, un desafío diario de programación o ir al gimnasio 3 veces por semana). Hoy no existe ninguna forma de registrar si un hábito se cumplió ni de ver si se está siendo constante.

**Objetivo:** permitir crear hábitos diarios o semanales con meta, marcar cada día si se cumplieron y ver, de un vistazo, la racha actual y el estado de cada hábito (verde, pendiente o rojo).

**Por qué primero:** es el núcleo del producto. Las métricas, los gráficos, el dashboard y las notificaciones dependen de que existan registros de cumplimiento confiables.

## 2. Usuarios

- **Dueño (único usuario):** la persona que creó la app y la usa a diario desde la web. No hay otros usuarios ni registro público en esta versión.

## 3. Dependencias

- **Spec de autenticación (login):** debe estar implementada antes de publicar esta funcionalidad. Sin ella no se cumple RNF-1 ni el principio 5 de la constitución.

## 4. Definiciones

- **Hoy / ayer:** fechas de calendario según la hora de referencia del sistema (no la del dispositivo), expresadas en la zona horaria definida en RNF-3. Un día es una fecha de calendario, sin importar si dura 23, 24 o 25 horas.
- **Semana:** de lunes a domingo, aunque cruce un cambio de mes o de año.
- **Período:** el día (hábitos diarios) o la semana (hábitos semanales).
- **Fecha de creación:** la fecha de calendario de "hoy" en el momento de crear el hábito.
- **Carácter:** cada símbolo tal como lo ve el usuario (un emoji o una letra con acento cuenta como 1).
- **Texto vacío:** texto sin caracteres visibles (vacío, solo espacios o solo caracteres invisibles).

## 5. Historias de usuario

- **HU-1:** Como dueño, quiero crear un hábito diario para comprometerme a hacerlo todos los días.
- **HU-2:** Como dueño, quiero crear un hábito semanal con una meta (por ejemplo, 3 veces por semana) para los hábitos que no son diarios.
- **HU-3:** Como dueño, quiero marcar un hábito como cumplido hoy para llevar registro de mi constancia.
- **HU-4:** Como dueño, quiero marcar como cumplido el día de ayer de un hábito que ya existía ayer, si me olvidé, para que un olvido no me rompa la racha.
- **HU-5:** Como dueño, quiero desmarcar un registro hecho por error para que mis datos sean reales.
- **HU-6:** Como dueño, quiero ver la racha y el estado (verde, pendiente o rojo) de cada hábito para saber en qué voy bien y en qué estoy fallando.
- **HU-7:** Como dueño, quiero corregir el nombre o la descripción de un hábito sin perder su historial.
- **HU-8:** Como dueño, quiero eliminar un hábito que ya no me interesa, sabiendo que se pierde su historial.

## 6. Requisitos funcionales

Notación EARS: *ubicuo* ("El sistema deberá…"), *evento* ("Cuando…"), *estado* ("Mientras…"), *no deseado* ("Si…, entonces…").

"Inmediatamente" significa: en menos de 1 segundo desde que el usuario confirma la acción, sin recargar la página.

### RF-1 — Crear hábito (HU-1, HU-2)

Un hábito tiene un nombre (obligatorio), una descripción (opcional) y una frecuencia: **diaria** o **semanal con meta** (cantidad de días por semana).

- **CA-1.1:** Cuando el usuario cree un hábito con nombre válido y frecuencia diaria, el sistema deberá guardarlo con racha 0 y estado pendiente. Si el usuario envía una meta, el sistema la ignora y guarda el hábito sin meta.
- **CA-1.2:** Cuando el usuario cree un hábito con nombre válido, frecuencia semanal y una meta entre 1 y 7 (D-3 resuelta), el sistema deberá guardarlo con racha 0 y estado pendiente.
- **CA-1.3:** Si el nombre es un texto vacío, entonces el sistema deberá rechazar la creación e indicar que el nombre es obligatorio.
- **CA-1.4:** Si el nombre supera los 100 caracteres o la descripción supera los 500, entonces el sistema deberá rechazar la creación e indicar el límite superado.
- **CA-1.5:** Si la frecuencia es semanal y la meta falta o está fuera del rango de CA-1.2, entonces el sistema deberá rechazar la creación e indicar el rango válido.
- **CA-1.6:** El sistema deberá quitar los espacios al inicio y al final del nombre y de la descripción antes de guardarlos. Una descripción que queda como texto vacío se guarda como "sin descripción".
- **CA-1.7:** Si ya existen 50 hábitos, entonces el sistema deberá rechazar la creación e indicar que se alcanzó el máximo.
- **CA-1.8:** Si ya existe otro hábito del dueño con el mismo nombre (tras quitar los espacios de CA-1.6 y sin distinguir mayúsculas de minúsculas), entonces el sistema deberá rechazar la creación e indicar que ese nombre ya está en uso (D-4 y D-5 resueltas).
- **CA-1.9:** Si la frecuencia no es diaria ni semanal, entonces el sistema deberá rechazar la creación e indicar que debe ser diaria o semanal.

### RF-2 — Ver hábitos (HU-6)

- **CA-2.1:** El sistema deberá mostrar cada hábito con su nombre, su frecuencia (y su meta si es semanal), su racha actual, su estado y, si es semanal, su avance en la semana actual (por ejemplo, "2 de 3" o "4 de 3").
- **CA-2.2:** El sistema deberá ordenar los hábitos por fecha de creación, del más antiguo al más nuevo.
- **CA-2.3:** Mientras no existan hábitos, el sistema deberá mostrar un mensaje que invite a crear el primero.

### RF-3 — Registrar cumplimiento (HU-3, HU-4)

Cada acción de marcar indica explícitamente la fecha que el usuario vio (hoy o ayer). El sistema valida esa fecha al recibir la acción.

- **CA-3.1:** Cuando el usuario marque un hábito como cumplido para hoy o para ayer, y ese día no tenga registro, el sistema deberá guardar el registro y recalcular inmediatamente la racha y el estado.
- **CA-3.2:** Si la fecha es anterior a ayer al momento de recibir la acción, entonces el sistema deberá rechazar el registro e indicar que solo se puede registrar hoy o ayer.
- **CA-3.3:** Si la fecha es futura, entonces el sistema deberá rechazar el registro.
- **CA-3.4:** Si ese día ya tiene un registro para ese hábito, entonces el sistema no deberá crear un segundo registro y deberá mostrar el día como cumplido, sin mensaje de error.
- **CA-3.5:** Si la fecha es anterior a la fecha de creación del hábito, entonces el sistema deberá rechazar el registro e indicar que el hábito todavía no existía ese día.
- **CA-3.6:** Mientras un hábito semanal ya haya alcanzado su meta, el sistema deberá seguir permitiendo registrar otros días de la misma semana.

### RF-4 — Desmarcar cumplimiento (HU-5)

- **CA-4.1:** Cuando el usuario desmarque el registro de hoy o de ayer, el sistema deberá eliminar ese registro y recalcular inmediatamente la racha y el estado.
- **CA-4.2:** Si el registro es anterior a ayer al momento de recibir la acción, entonces el sistema deberá rechazar la acción e indicar que ese registro ya no se puede modificar.
- **CA-4.3:** Si el día a desmarcar no tiene registro, entonces el sistema no deberá modificar nada y deberá mostrar el día como no cumplido, sin mensaje de error.

### RF-5 — Racha diaria (HU-6)

- **CA-5.1:** Mientras hoy tenga registro, el sistema deberá mostrar como racha la cantidad de días consecutivos con registro que terminan hoy.
- **CA-5.2:** Mientras hoy no tenga registro, el sistema deberá mostrar como racha la cantidad de días consecutivos con registro que terminan ayer (el día en curso no rompe la racha).
- **CA-5.3:** Si ni hoy ni ayer tienen registro, entonces el sistema deberá mostrar racha 0.

### RF-6 — Racha semanal (HU-6)

Una semana está **cumplida** si tiene una cantidad de registros mayor o igual a la meta.

- **CA-6.1:** Mientras la semana actual haya alcanzado la meta, el sistema deberá mostrar como racha la cantidad de semanas cumplidas consecutivas que terminan en la semana actual.
- **CA-6.2:** Mientras la semana actual no haya alcanzado la meta, el sistema deberá mostrar como racha la cantidad de semanas cumplidas consecutivas que terminan en la semana anterior (la semana en curso no rompe la racha hasta el domingo).
- **CA-6.3:** Si la semana anterior no se cumplió y la actual todavía no alcanzó la meta, entonces el sistema deberá mostrar racha 0.

### RF-7 — Estado del período actual (HU-6)

Si se cumple más de una regla, la prioridad es verde > rojo > pendiente.

- **CA-7.1:** Mientras el período actual esté cumplido, el sistema deberá mostrar el estado verde.
- **CA-7.2:** Mientras el período actual no esté cumplido y el período anterior haya terminado sin cumplirse, el sistema deberá mostrar el estado rojo. En hábitos semanales, el rojo se mantiene durante toda la semana actual hasta que se alcance la meta, aunque el avance vaya al día.
- **CA-7.3:** Mientras el período actual no esté cumplido y no aplique el rojo, el sistema deberá mostrar el estado pendiente.
- **CA-7.4:** Si la fecha de creación del hábito es posterior al período anterior, entonces el sistema no deberá mostrar el estado rojo. Un hábito diario creado ayer sí existía durante el período anterior. En hábitos semanales, la semana de creación se evalúa con la meta completa, igual que cualquier otra (D-2 resuelta).

### RF-8 — Editar hábito (HU-7)

- **CA-8.1:** Cuando el usuario edite el nombre o la descripción de un hábito, el sistema deberá guardar el cambio conservando todo su historial, su racha y su estado.
- **CA-8.2:** El sistema no deberá permitir modificar la frecuencia ni la meta de un hábito existente.
- **CA-8.3:** Si los nuevos valores no cumplen las validaciones de RF-1, entonces el sistema deberá rechazar la edición sin modificar el hábito.
- **CA-8.4:** Cuando el usuario deje la descripción como texto vacío, el sistema deberá guardar el hábito como "sin descripción".
- **CA-8.5:** Cuando el usuario guarde una edición sin cambios, el sistema deberá aceptarla sin modificar nada.

### RF-9 — Eliminar hábito (HU-8)

- **CA-9.1:** Cuando el usuario solicite eliminar un hábito, el sistema deberá pedir una confirmación que avise que se borrará todo su historial y que no se puede deshacer.
- **CA-9.2:** Cuando el usuario confirme la eliminación, el sistema deberá borrar el hábito y todos sus registros de forma definitiva.
- **CA-9.3:** Si el usuario cancela la confirmación, entonces el sistema no deberá modificar nada.

### RF-10 — Consistencia ante acciones inválidas o simultáneas

- **CA-10.1:** Si el usuario intenta marcar, desmarcar, editar o eliminar un hábito que ya no existe, entonces el sistema deberá rechazar la acción, indicar que el hábito ya no existe y actualizar la lista.
- **CA-10.2:** El sistema no deberá permitir, ni siquiera ante acciones simultáneas desde varios dispositivos, dos registros del mismo hábito en el mismo día ni registros de un hábito eliminado.
- **CA-10.3:** Si la sesión del dueño expiró al momento de recibir una acción, entonces el sistema no deberá aplicar ningún cambio y deberá indicar que hay que volver a iniciar sesión.

## 7. Requisitos no funcionales

- **RNF-1 (privacidad):** Solo el dueño autenticado puede ver o modificar hábitos y registros (ver la sección 3).
- **RNF-2 (verificabilidad):** Todas las reglas de RF-1 a RF-10 que no sean puramente visuales deben poder verificarse con tests automatizados, sin depender de la interfaz y controlando la fecha y hora de referencia.
- **RNF-3 (fecha de referencia):** "Hoy", "ayer", la semana y la fecha de creación se calculan según la zona horaria fija del dueño (diferida, ver D-1).
- **RNF-4 (rendimiento):** Con 50 hábitos y 5 años de registros, la lista de hábitos con rachas y estados debe mostrarse en menos de 1 segundo. Se mide en un navegador de escritorio con conexión de banda ancha, desde que se pide la lista hasta que es visible, sin contar la primera carga de la aplicación.
- **RNF-5 (idioma):** Todos los textos visibles, incluidos los mensajes de error, están en español neutro.
- **RNF-6 (persistencia):** Un registro confirmado no se pierde al recargar la página, al volver a iniciar sesión ni al reiniciar el sistema.
- **RNF-7 (simplicidad):** Esta spec no asume ninguna herramienta adicional para manejar fechas. Si el plan la necesita, debe justificarse según el principio 1 de la constitución.

## 8. Casos límite

Cada caso indica la regla que lo resuelve.

- **Hábito creado hoy:** racha 0, estado pendiente y nunca rojo (CA-7.4).
- **Hábito creado hoy, hecho ayer:** no se puede registrar ayer porque el hábito no existía (CA-3.5).
- **Hábito diario creado ayer y no marcado:** hoy se muestra rojo (CA-7.4).
- **Hábito semanal creado a mitad de semana:** la semana de creación necesita la meta completa; si no se cumple, la semana siguiente se ve roja (CA-7.2, D-2 resuelta).
- **Lunes con registro de ayer:** marcar o desmarcar el domingo cambia el resultado de la semana anterior; la racha y el estado se recalculan, y un rojo puede pasar a pendiente (CA-3.1, CA-4.1).
- **Desmarcar ayer rompe la racha:** si ayer era el único eslabón entre dos tramos, la racha baja (CA-4.1).
- **Acción enviada a las 23:59:59 que llega después de medianoche:** la fecha enviada como "hoy" pasa a ser "ayer" y sigue siendo válida (RF-3).
- **Medianoche:** el antiguo "ayer" pasa a ser anteayer y sus registros ya no se pueden marcar ni desmarcar (CA-3.2, CA-4.2).
- **Reloj del dispositivo desfasado:** no afecta, porque se usa la hora de referencia del sistema (sección 4).
- **Cambio de horario de verano:** no afecta, porque un día es una fecha de calendario (sección 4).
- **Semana que cruza cambio de mes o de año:** sigue siendo una única semana de lunes a domingo (sección 4).
- **Doble acción rápida al marcar:** resulta en un único registro y sin mensaje de error (CA-3.4).
- **Desmarcar un día sin registro:** no cambia nada y no muestra error (CA-4.3).
- **Más registros que la meta semanal:** se permiten y el avance lo muestra (por ejemplo, "4 de 3") (CA-3.6, CA-2.1).
- **Hábito eliminado desde otro dispositivo:** las acciones sobre él se rechazan (CA-10.1).
- **Acciones simultáneas desde dos dispositivos:** nunca hay duplicados ni registros huérfanos (CA-10.2).
- **Sesión expirada en medio de una acción:** no se aplica ningún cambio (CA-10.3).
- **Volver tras semanas sin usar la app:** racha 0; rojo si el período anterior no se cumplió (RF-5 a RF-7).
- **Edición sin cambios o con la descripción vacía:** CA-8.5 y CA-8.4.
- **Límite de 50 hábitos:** se rechaza el hábito 51 (CA-1.7).
- **Meta semanal de 7:** se permite. Se evalúa como cualquier otra meta semanal, con racha en semanas (D-3 resuelta).
- **Nombre duplicado:** se rechaza al crear o al editar, según CA-1.8 y CA-8.3 (D-4 resuelta).

## 9. Fuera de alcance (esta versión)

- Porcentaje de cumplimiento, gráficos y dashboard (irán en la spec de métricas).
- Vista de calendario o historial completo de registros.
- Notas en los registros.
- Más de un registro por día por hábito.
- Cambiar la frecuencia o la meta de un hábito existente.
- Archivar hábitos (solo se pueden eliminar).
- Deshacer una eliminación.
- Frecuencias distintas de diaria y semanal (mensual, días específicos de la semana, etc.).
- Notificaciones y correos por Gmail.
- Gestión de ideas.
- Login y gestión de la sesión (es una dependencia, ver la sección 3; irá en su propia spec).

## 10. Criterios de finalización

- Todos los criterios de aceptación de RF-1 a RF-10 se cumplen.
- Cada criterio de aceptación que define una regla de negocio (RF-1 a RF-10) tiene al menos un test automatizado que pasa, como exige el principio 4 de la constitución.
- Cada caso límite de la sección 8 que referencia una regla tiene al menos un test automatizado que pasa. Los casos marcados [NECESITA ACLARACIÓN] se testean una vez resueltos.
- El dueño puede, sin ayuda, crear un hábito diario y uno semanal, marcar hoy y ayer, desmarcar, ver la racha y el estado, editar y eliminar.
- Todas las dudas marcadas como [NECESITA ACLARACIÓN] están resueltas o diferidas explícitamente antes de empezar el plan.
- La dependencia de la sección 3 está implementada antes de publicar.

## 11. Dudas abiertas

- **D-1** Diferida: supuesto del plan §0. "Hoy" usa una zona horaria fija del dueño, configurada en `OWNER_TIME_ZONE`. Esto implica que, si se usa la app viajando, "hoy" no cambia con la ubicación del dispositivo.
- **D-2** Resuelta: la semana de creación de un hábito semanal cuenta con la meta completa, igual que cualquier otra semana. Si no se cumple, la semana siguiente se ve roja.
- **D-3** Resuelta: la meta semanal va de 1 a 7. Una meta de 7 se permite y se evalúa como cualquier otra meta semanal.
- **D-4** Resuelta: no se permiten dos hábitos del mismo dueño con el mismo nombre. Ver CA-1.8.
- **D-5** Resuelta: "mismo nombre" no distingue mayúsculas de minúsculas. "Ejercicio" y "ejercicio" son el mismo nombre (ver CA-1.8).
