# Spec 004 — Métricas de hábitos

## 1. Contexto y objetivo

Hoy la pestaña "Hábitos" muestra la racha y el estado del período en curso, pero no responde preguntas como: "¿qué tan constante fui el último mes?", "¿mejoro o empeoro semana a semana?" o "¿cuál fue mi mejor racha?". Los registros existen (spec 001), pero no se ven en conjunto.

**Objetivo:** que el dueño abra una pestaña "Métricas" y vea, sin hacer cuentas: el porcentaje de cumplimiento de cada hábito y el general, la tendencia de las últimas 12 semanas en un gráfico, la mejor racha de cada hábito y su historial día por día.

**Qué no es:** no es un sistema de objetivos ni de alertas. Solo muestra lo que ya pasó. Los avisos y los correos irán con la spec de notificaciones.

**Por qué ahora:** la spec 001 dejó fuera el porcentaje, los gráficos y el historial. Ya hay registros confiables sobre los que calcularlos, y no hace falta ningún cambio en el servidor.

## 2. Usuarios

- **Dueño (único usuario):** la persona con la cuenta de la spec 002. Solo él ve sus métricas.

## 3. Dependencias

- **Spec 001 (seguimiento de hábitos):** ya implementada. Las métricas usan sus reglas de semana, de semana cumplida (RF-6), de estado (RF-7) y de semana de creación (D-2), y los datos de `GET /api/habits`.
- **Spec 002 (cuenta del dueño, inicio y cierre de sesión):** ya implementada. La pestaña exige sesión válida.

## 4. Definiciones

- **Hoy / semana / período / semana cumplida / fecha de creación:** igual que en la spec 001 (sección 4 y RF-6). La semana va de lunes a domingo. "Hoy" es la fecha `today` que devuelve la API, en la zona horaria del dueño.
- **Día cerrado:** un día anterior a hoy. **Semana cerrada:** una semana cuyo domingo es anterior a hoy.
- **Período en curso:** hoy (hábitos diarios) o la semana actual (hábitos semanales). Todavía puede cambiar, por eso no entra en ningún porcentaje.
- **Período evaluable:** un período cerrado en el que el hábito ya existía. Un día es evaluable si es igual o posterior a la fecha de creación. Una semana es evaluable si es igual o posterior a la semana de creación; esa semana de creación cuenta con la meta completa (spec 001, D-2).
- **Ventana:** el rango de períodos sobre el que se calcula un porcentaje. Hay dos:
  - **Desde la creación:** todos los períodos evaluables del hábito hasta el último cerrado. En hábitos diarios termina ayer; en semanales, en la semana anterior a la actual.
  - **Últimas 4 semanas:** las 4 semanas cerradas inmediatamente anteriores a la actual (lunes a domingo, terminan el último domingo). En hábitos diarios son esos 28 días.
- **Porcentaje de cumplimiento:** en una ventana, períodos evaluables cumplidos sobre períodos evaluables, como entero (CA-3.4).
- **Sin datos:** estado de un hábito en una ventana donde no tiene ningún período evaluable. No es 0 %.
- **Mejor racha:** la mayor cantidad de períodos cumplidos consecutivos que el hábito tuvo en todo su historial (RF-6).
- **Tendencia:** el porcentaje general de cada una de las últimas 12 semanas cerradas (RF-7).

## 5. Historias de usuario

- **HU-1:** Como dueño, quiero ver el porcentaje de cumplimiento de cada hábito desde que lo creé y en las últimas 4 semanas, para saber cuáles sostengo y cuáles no.
- **HU-2:** Como dueño, quiero ver un porcentaje general, para tener una sola cifra de cómo voy.
- **HU-3:** Como dueño, quiero ver en un gráfico cómo evolucionó mi cumplimiento semana a semana, para saber si mejoro o empeoro.
- **HU-4:** Como dueño, quiero ver la mejor racha de cada hábito junto a la actual, para compararme con mi mejor momento.
- **HU-5:** Como dueño, quiero ver el historial día por día de cada hábito, para encontrar patrones (por ejemplo, que fallo los fines de semana).
- **HU-6:** Como dueño, quiero que el día o la semana en curso se muestren aparte, para que algo que todavía puedo cumplir no me baje el porcentaje.

## 6. Requisitos funcionales

Notación EARS: *ubicuo* ("El sistema deberá…"), *evento* ("Cuando…"), *estado* ("Mientras…"), *no deseado* ("Si…, entonces…").

### RF-1 — Pestaña y estados vacíos (HU-1 a HU-6)

- **CA-1.1:** El sistema deberá ofrecer una pestaña "Métricas" junto a "Hábitos" e "Ideas", disponible solo con sesión válida.
- **CA-1.2:** Mientras no existan hábitos, el sistema deberá mostrar un mensaje que invite a crear el primero en "Hábitos", sin porcentajes ni gráfico.
- **CA-1.3:** Cada vez que el dueño abra la pestaña, el sistema deberá usar la lista de hábitos más reciente, de modo que lo marcado o eliminado en "Hábitos" ya esté reflejado.
- **CA-1.4:** Si la lista de hábitos no se puede cargar, entonces el sistema deberá mostrar un mensaje de error con la opción de reintentar y no deberá mostrar cifras parciales.
- **CA-1.5:** Si la sesión venció, entonces el sistema deberá llevar al inicio de sesión con el mensaje de sesión expirada (spec 002, CA-3.3).
- **CA-1.6:** El sistema deberá ordenar los hábitos por fecha de creación, del más antiguo al más nuevo, igual que en la spec 001 (CA-2.2).

### RF-2 — Períodos y ventanas (HU-1, HU-6)

- **CA-2.1:** El sistema deberá contar en los porcentajes solo períodos evaluables. Hoy y la semana actual nunca cuentan, ni como cumplidos ni como fallados (D-4 resuelta).
- **CA-2.2:** El sistema deberá calcular la ventana "desde la creación" desde la fecha de creación del hábito (diario) o desde su semana de creación (semanal), hasta el último período cerrado.
- **CA-2.3:** El sistema deberá calcular la ventana "últimas 4 semanas" sobre las 4 semanas cerradas anteriores a la actual, aunque entre ellas y hoy haya días cerrados de la semana en curso. Esos días solo entran en "desde la creación" (D-2 resuelta).
- **CA-2.4:** El sistema deberá contar dentro de una ventana solo los períodos evaluables. Ejemplo: un hábito creado el jueves de la tercera semana de la ventana cuenta 11 días si es diario (jueves a domingo de esa semana, más los 7 de la cuarta) y 2 semanas si es semanal (la tercera, con la meta completa, y la cuarta).
- **CA-2.5:** Si un hábito no tiene ningún período evaluable en una ventana, entonces el sistema deberá mostrar "sin datos" en esa ventana, nunca 0 %.
- **CA-2.6:** Consecuencia aceptada: un hábito diario creado esta semana, con al menos un día cerrado, tiene porcentaje "desde la creación" y "sin datos" en "últimas 4 semanas". Un hábito creado hoy tiene "sin datos" en las dos ventanas (D-4 resuelta).

### RF-3 — Porcentaje por hábito (HU-1)

- **CA-3.1:** En un hábito diario, el sistema deberá calcular el porcentaje como días cerrados con registro sobre días cerrados evaluables de la ventana (D-1 resuelta).
- **CA-3.2:** En un hábito semanal, el sistema deberá calcular el porcentaje como semanas cumplidas sobre semanas cerradas evaluables de la ventana. La semana de creación cuenta con la meta completa, igual que en el estado rojo y en la racha semanal (D-1 resuelta).
- **CA-3.3:** El sistema deberá mostrar de cada hábito el porcentaje de las dos ventanas, y junto a cada uno la base del cálculo ("20 de 30 días" o "5 de 8 semanas"), para que el dueño entienda de dónde sale.
- **CA-3.4:** El sistema deberá mostrar los porcentajes como números enteros, redondeando hacia arriba desde la mitad (66,5 pasa a 67). El redondeo se aplica solo al mostrar.
- **CA-3.5:** Si una semana tiene más registros que la meta, entonces el sistema deberá contarla como una semana cumplida, sin que los registros de más aumenten el porcentaje.

### RF-4 — Porcentaje general (HU-2)

- **CA-4.1:** El sistema deberá calcular el porcentaje general de cada ventana como el promedio simple de los porcentajes de los hábitos que tienen datos en esa ventana (D-3 resuelta).
- **CA-4.2:** El sistema deberá dar a cada hábito el mismo peso, sin importar su frecuencia, su antigüedad o la cantidad de períodos que tenga.
- **CA-4.3:** El sistema deberá excluir del promedio de una ventana a los hábitos "sin datos" en esa ventana.
- **CA-4.4:** Si todos los hábitos están "sin datos" en una ventana, entonces el sistema deberá mostrar "sin datos" como porcentaje general de esa ventana.
- **CA-4.5:** El sistema deberá promediar los porcentajes exactos de cada hábito, sin redondear antes. Por eso el general puede diferir en una unidad del promedio de las cifras que se muestran.

### RF-5 — Período en curso (HU-6)

- **CA-5.1:** El sistema deberá mostrar en cada hábito diario "Hoy: hecho" u "Hoy: pendiente", según tenga o no registro de hoy.
- **CA-5.2:** El sistema deberá mostrar en cada hábito semanal "Esta semana: X de Y" (por ejemplo, "2 de 3" o "4 de 3"), con los mismos valores que el avance de la spec 001 (CA-2.1).
- **CA-5.3:** El sistema no deberá permitir que el período en curso modifique ningún porcentaje por hábito, el general ni los puntos del gráfico.
- **CA-5.4:** Cuando el dueño marque o desmarque hoy en "Hábitos", el sistema deberá cambiar solo lo que se muestra del período en curso (CA-5.1 y CA-5.2), la racha y el historial. Los porcentajes cambian al cerrar el período.

### RF-6 — Racha actual y mejor racha (HU-4)

- **CA-6.1:** El sistema deberá mostrar la racha actual con el mismo valor que la pestaña "Hábitos" (spec 001, RF-5 y RF-6).
- **CA-6.2:** El sistema deberá mostrar como mejor racha la mayor cantidad de días consecutivos con registro (hábito diario) o de semanas cumplidas consecutivas (hábito semanal) de todo el historial del hábito.
- **CA-6.3:** El sistema deberá aplicar a la mejor racha las mismas reglas que a la actual: el período en curso suma si ya está cumplido, y si no lo está, ni suma ni corta una racha.
- **CA-6.4:** El sistema no deberá mostrar una mejor racha menor que la actual.
- **CA-6.5:** Mientras el hábito no tenga ningún registro, el sistema deberá mostrar mejor racha 0.
- **CA-6.6:** El sistema deberá mostrar las rachas con su unidad ("días" o "semanas").

### RF-7 — Gráfico de tendencia (HU-3)

- **CA-7.1:** El sistema deberá mostrar un gráfico de líneas con un punto por cada una de las últimas 12 semanas cerradas, con el porcentaje general de esa semana (de 0 a 100). La semana actual no se grafica (D-5 resuelta).
- **CA-7.2:** El sistema deberá calcular el porcentaje de un hábito en una semana así: en un hábito diario, días de esa semana con registro sobre días de esa semana en que el hábito existía; en un hábito semanal, 100 % si la semana está cumplida y 0 % si no. Un hábito creado después de esa semana no cuenta en ella.
- **CA-7.3:** El sistema deberá calcular el porcentaje general de una semana como el promedio simple de los hábitos que existían en ella (RF-4).
- **CA-7.4:** Si en una semana no existía ningún hábito, entonces el sistema deberá dejar esa semana "sin datos" en el gráfico, con la línea interrumpida y no en 0 %.
- **CA-7.5:** El sistema deberá ofrecer los mismos datos como texto (una tabla o una lista con la semana, identificada por la fecha de su lunes, y su porcentaje o "sin datos"), con el mismo redondeo que el gráfico y disponible sin necesidad de ver el gráfico.
- **CA-7.6:** El sistema deberá usar en el gráfico colores legibles tanto en modo claro como en modo oscuro, y cambiarlos cuando cambie el modo.
- **CA-7.7:** Mientras no exista ninguna semana con datos, el sistema deberá mostrar "sin datos" en lugar del gráfico.
- **CA-7.8:** La biblioteca de gráficos (Chart.js) deberá estar justificada en `docs/decisions.md` antes de escribir el código que la use (principio 1 de la constitución): qué problema resuelve y por qué no alcanza lo que ya hay.
- **CA-7.9:** Consecuencia aceptada: en una sola semana un hábito semanal vale 0 % o 100 %, así que la tendencia de una cuenta con pocos hábitos semanales es escalonada (D-10 resuelta).

### RF-8 — Historial día por día (HU-5)

- **CA-8.1:** El sistema deberá mostrar de cada hábito una grilla de días ordenados en semanas de lunes a domingo.
- **CA-8.2:** El sistema deberá mostrar inicialmente las últimas 12 semanas, contando la semana actual, sin retroceder más allá de la semana de creación del hábito (D-9 resuelta).
- **CA-8.3:** Mientras el hábito tenga más de 12 semanas desde su creación, el sistema deberá ofrecer "Ver todo el historial", que muestra todas las semanas desde la de creación, y una opción para volver a las últimas 12.
- **CA-8.4:** El sistema deberá mostrar cada día con uno de estos estados: *cumplido* (tiene registro); *no cumplido* (día cerrado sin registro, solo en hábitos diarios); *sin registro* (día sin registro de un hábito semanal, que por sí solo no es un fallo); *pendiente* (hoy sin registro); *fuera de rango* (anterior a la fecha de creación o posterior a hoy).
- **CA-8.5:** En hábitos semanales, el sistema deberá mostrar junto a cada semana los registros sobre la meta ("2 de 3") y si la semana está cumplida, no cumplida o en curso.
- **CA-8.6:** El sistema no deberá permitir marcar ni desmarcar ningún día desde la grilla, tampoco hoy ni ayer. Marcar y desmarcar sigue en "Hábitos", solo para hoy o ayer (spec 001, RF-3 y RF-4).
- **CA-8.7:** El sistema deberá mostrar en la grilla exactamente los días con registro que devuelve la API, sin inventar ni omitir ninguno.

### RF-9 — Consistencia (HU-1, HU-4)

- **CA-9.1:** El sistema no deberá mostrar un porcentaje que contradiga el estado del mismo hábito. Si el período anterior se muestra en rojo (spec 001, RF-7), ese período cuenta como fallado en el porcentaje.
- **CA-9.2:** El sistema no deberá mostrar un porcentaje que contradiga la racha del mismo hábito. Si la racha actual es de n períodos cerrados, esos n períodos cuentan como cumplidos en el porcentaje "desde la creación".
- **CA-9.3:** El sistema deberá usar para los porcentajes, las rachas y la grilla las mismas reglas de existencia del hábito, de semana cumplida y de semana de creación que usan el estado y la racha de la spec 001.
- **CA-9.4:** Cuando el dueño elimine un hábito en "Hábitos", el sistema deberá quitarlo de las métricas al abrir la pestaña y recalcular sin él el porcentaje general y el gráfico (spec 001, HU-8: su historial se pierde).
- **CA-9.5:** Mientras el dueño no haya cambiado de pestaña, el sistema no deberá cambiar "hoy" a mitad de la visualización. Al volver a abrir la pestaña, se usa el `today` de la lista nueva (CA-1.3).

## 7. Requisitos no funcionales

- **RNF-1 (privacidad):** solo el dueño autenticado puede ver sus métricas.
- **RNF-2 (verificabilidad):** todas las reglas de RF-2 a RF-9 que no sean puramente visuales se pueden verificar con tests automáticos sin depender de la interfaz y controlando la fecha de "hoy".
- **RNF-3 (rendimiento):** con 50 hábitos y 5 años de registros, la pestaña "Métricas" se muestra en menos de 1 segundo, medido como en la spec 001 (RNF-4): navegador de escritorio con banda ancha, desde que se abre la pestaña hasta que es visible, sin contar la primera carga de la aplicación.
- **RNF-4 (accesibilidad):** el gráfico tiene una alternativa en texto (CA-7.5), y cada celda de la grilla expone como texto su fecha y su estado, sin depender solo del color.
- **RNF-5 (idioma):** todos los textos visibles, incluidos los mensajes de error, están en español neutro.
- **RNF-6 (origen de los datos):** todo se calcula en el navegador con lo que ya devuelve `GET /api/habits` (`today`, fecha de creación y fechas de registro de cada hábito). Esta spec no agrega endpoints ni cambia la base de datos.
- **RNF-7 (dependencias):** la única biblioteca nueva es la del gráfico (CA-7.8). Para todo lo demás no se asume ninguna herramienta adicional.

## 8. Casos límite

Cada caso indica la regla que lo resuelve.

- **Sin hábitos:** mensaje que invita a crear el primero (CA-1.2).
- **Hábito creado hoy:** "sin datos" en las dos ventanas, solo se ve el período en curso, y no cuenta en el general ni en la tendencia de semanas anteriores (CA-2.5, CA-2.6, CA-7.2).
- **Hábito diario creado el lunes de esta semana, hoy miércoles:** tiene porcentaje "desde la creación" (lunes y martes) y "sin datos" en "últimas 4 semanas" (CA-2.3, CA-2.6).
- **Hábito creado un domingo:** si es diario, esa semana cuenta un solo día; si es semanal, la semana de creación se evalúa con la meta completa aunque solo tuviera un día (CA-2.4, CA-3.2). Con meta mayor que 1 esa semana queda fallada.
- **Hoy es lunes:** la semana en curso no tiene días cerrados, así que el avance semanal es "0 de Y" y no afecta ningún porcentaje. Ayer, domingo, es un día cerrado y entra en "desde la creación" de los diarios. "Últimas 4 semanas" son las 4 semanas que terminaron ayer (CA-2.1, CA-2.3, CA-5.2).
- **Semana con más registros que la meta:** cuenta como una semana cumplida, sin pasar de 100 % (CA-3.5).
- **Hábito semanal con la semana anterior en rojo:** esa semana cuenta como fallada en el porcentaje (CA-9.1).
- **Marcar el domingo desde "Hábitos" el lunes:** la semana anterior puede pasar de fallada a cumplida y el porcentaje se recalcula al abrir la pestaña (CA-1.3, CA-9.1).
- **Hábito eliminado:** desaparece de las métricas y se recalculan el general y el gráfico (CA-9.4).
- **Todos los hábitos "sin datos" en una ventana:** el general de esa ventana dice "sin datos" (CA-4.4).
- **Todas las semanas del gráfico sin datos:** se muestra "sin datos" en lugar del gráfico (CA-7.7).
- **Hábito semanal en la tendencia:** una semana vale 0 % o 100 %; la línea puede verse escalonada y es lo esperado (CA-7.9).
- **Hábito con más de 12 semanas:** la grilla muestra las últimas 12 y ofrece ver todo (CA-8.2, CA-8.3).
- **Hábito con menos de 12 semanas:** la grilla empieza en la semana de creación y no ofrece "Ver todo el historial" (CA-8.2, CA-8.3).
- **Días sin registro en un hábito semanal:** se ven como "sin registro", no como fallo (CA-8.4).
- **Intento de editar un día pasado desde la grilla:** no es posible; la grilla es de solo lectura (CA-8.6).
- **Zona horaria del dueño:** "hoy" es el `today` de la API, no la fecha del dispositivo (sección 4).
- **Cambio de medianoche con la pestaña abierta:** los valores corresponden al `today` de la última carga; al reabrir la pestaña se actualizan (CA-9.5).
- **Mejor racha igual a la actual:** se muestran los dos valores iguales; nunca es menor (CA-6.4).
- **Cuenta sin conexión o error al cargar:** mensaje con reintento, sin cifras parciales (CA-1.4).
- **Sesión vencida:** vuelve al inicio de sesión (CA-1.5).
- **Colores en modo oscuro:** el gráfico sigue siendo legible (CA-7.6).

## 9. Fuera de alcance (esta versión)

- Endpoints `/api/metrics/*` y `/api/dashboard`, y cualquier cálculo en el servidor. Lo que requiera el servidor (avisos, correos) irá con la spec de notificaciones y Gmail.
- Métricas de ideas.
- Dashboard o resumen en la pantalla de inicio fuera de la pestaña "Métricas".
- Editar registros de días pasados, desde la grilla o desde otro lugar.
- Rangos de fechas elegidos por el dueño, o ventanas distintas de las dos definidas.
- Comparar hábitos entre sí, ordenarlos por porcentaje o filtrarlos.
- Otros tipos de gráfico (barras, circular, mapa de calor) o un gráfico por hábito.
- Metas o alertas del tipo "avisarme si bajo de X %".
- Exportar métricas.
- Ponderar el porcentaje general por frecuencia, antigüedad o importancia.
- Guardar el historial de porcentajes: todo se recalcula a partir de los registros actuales.

## 10. Criterios de finalización

- Todos los criterios de aceptación de RF-1 a RF-9 se cumplen.
- Cada regla de RF-2 a RF-9 tiene al menos un test con `node --test` que pasa, y los tests del backend siguen pasando (principio 4 de la constitución). Ambos comandos pasan antes de cada commit.
- Cada caso límite de la sección 8 tiene una verificación: automática si es una regla, en el navegador si es solo de interfaz.
- Existe en `docs/decisions.md` la entrada que justifica Chart.js, anterior al código que lo usa (CA-7.8).
- El dueño puede, sin ayuda: abrir "Métricas", leer el porcentaje de cada hábito y el general en las dos ventanas, entender el gráfico y su versión en texto, ver la mejor racha de cada hábito y recorrer su historial.
- Con 50 hábitos y 5 años de registros, la pestaña se muestra en menos de 1 segundo (RNF-3).
- Las dudas D-1 a D-11 están resueltas.

## 11. Dudas abiertas

- **D-1** Resuelta: en hábitos diarios, el porcentaje es días cerrados con registro sobre días cerrados desde la fecha de creación. En semanales, semanas cumplidas sobre semanas cerradas desde la semana de creación, que cuenta con la meta completa (spec 001, D-2), igual que el estado rojo y la racha semanal.
- **D-2** Resuelta: cada hábito tiene dos ventanas, "desde la creación" (hasta el último período cerrado) y "últimas 4 semanas" (las 4 semanas cerradas anteriores a la actual; en diarios, esos 28 días). Dentro de una ventana solo cuentan los períodos en que el hábito existía.
- **D-3** Resuelta: el porcentaje general es el promedio simple de los porcentajes por hábito, calculado por ventana. Cada hábito pesa igual.
- **D-4** Resuelta: solo cuentan los períodos cerrados. Hoy y la semana en curso se muestran aparte y nunca bajan el porcentaje. Un hábito sin ningún período cerrado en una ventana muestra "sin datos" y queda fuera del promedio general de esa ventana. Consecuencia: un hábito diario creado esta semana tiene porcentaje "desde la creación" pero "sin datos" en "últimas 4 semanas".
- **D-5** Resuelta: los gráficos usan Chart.js (el dueño lo prefirió a CSS plano). El alcance es un gráfico de líneas con el porcentaje general de cada una de las últimas 12 semanas cerradas. Como un canvas no lo leen los lectores de pantalla, los mismos datos se ofrecen como texto, y los colores siguen el modo claro y oscuro. La biblioteca se justifica en `docs/decisions.md` antes del código.
- **D-6** Resuelta: por hábito se muestra el porcentaje en las dos ventanas, la racha actual, la mejor racha (mismas reglas que la actual, nunca menor) y una grilla día por día en semanas de lunes a domingo. La grilla es el historial y es de solo lectura.
- **D-7** Resuelta: todo se calcula en el frontend con los datos que ya devuelve `GET /api/habits`. No hay endpoints nuevos; `/api/metrics/*` y `/api/dashboard` quedan fuera de alcance.
- **D-8** Resuelta: "Métricas" es una pestaña nueva junto a "Hábitos" e "Ideas". Los estados vacíos son "sin hábitos" y "sin datos".
- **D-9** Resuelta: la grilla muestra al inicio las últimas 12 semanas, con opción de ver todo el historial, y no permite editar días pasados.
- **D-10** Resuelta: aceptado que en una sola semana un hábito semanal vale 0 % o 100 %, por lo que la tendencia semanal es gruesa.
- **D-11** Resuelta: las métricas de ideas quedan fuera de alcance de esta spec.
