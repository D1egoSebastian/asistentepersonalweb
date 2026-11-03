# Spec 005 — Resumen diario por correo: ideas estancadas y hábitos en riesgo

## 1. Contexto y objetivo

Hoy la app solo guarda ideas y hábitos, pero no avisa de nada. Las ideas se olvidan y los hábitos se pierden sin que el dueño lo note hasta mirar la pantalla.

**Objetivo:** que cada mañana el dueño reciba en su Gmail un único correo con lo que necesita atención: ideas que llevan 14 días sin tocarse y hábitos en riesgo. Si no hay nada que atender, no llega ningún correo.

**Por qué ahora:** es el primer requisito de CLAUDE.md que sale de la app hacia el dueño ("mandarme correos a mi mismo sobre objetivos que quiero lograr, rutinas que me faltan"). Debe publicarse y funcionar de verdad, sin costo, para que el dueño lo use.

## 2. Usuarios

- **Dueño (único usuario):** recibe el correo en la cuenta de Gmail con la que se registró en la app. No hay otros destinatarios.

## 3. Historias de usuario

- **HU-1:** Como dueño, quiero que me recuerden las ideas que dejé sin editar, para decidir si las retomo o las archivo.
- **HU-2:** Como dueño, quiero que me avisen de mis hábitos en riesgo antes de que se pierdan, para corregir a tiempo.
- **HU-3:** Como dueño, quiero que la app esté publicada y funcione sin pagar, para usarla desde cualquier lugar.

## 4. Definiciones

- **Día del dueño:** la fecha de hoy en la zona horaria `OWNER_TIME_ZONE` (ya la calcula `OwnerClock`).
- **Resumen:** el único correo diario. Reúne las ideas y los hábitos que cumplen alguna regla de esta spec.
- **Idea estancada:** idea activa (no archivada) cuyo `UpdatedAt` tiene 14 días o más.
- **Hábito en riesgo:** hábito que cumple una de las alertas de RF-3.
- **Racha perdida:** racha diaria de 3 días o más que se cortó ayer.

## 5. Requisitos funcionales

Notación EARS, igual que en la spec 002.

### RF-1 — Un resumen por día (HU-1, HU-2)

- **CA-1.1:** Cuando sea 8:00 o más tarde en el día del dueño y todavía no se haya enviado el resumen de ese día, el sistema deberá calcular el resumen y enviarlo.
- **CA-1.2:** Si ninguna regla de RF-2 ni de RF-3 aplica, entonces el sistema no deberá enviar correo ese día.
- **CA-1.3:** Mientras exista un resumen enviado para el día del dueño, el sistema no deberá enviar otro correo para ese día (RNF-3).
- **CA-1.4:** Si el envío falla, entonces el sistema no deberá marcar el día como enviado, para que la siguiente ejecución del mismo día lo reintente.
- **CA-1.5:** El correo deberá ir a la dirección de email de la cuenta del dueño.
- **CA-1.6:** El correo deberá estar en texto plano, en español neutro, con el asunto "Tu resumen de hoy" seguido de las cifras (por ejemplo: "Tu resumen de hoy: 2 ideas y 1 hábito").

### RF-2 — Ideas estancadas (HU-1)

- **CA-2.1:** Cuando una idea activa tenga 14 días o más desde su último `UpdatedAt`, el sistema deberá incluirla en el resumen si no fue recordada antes.
- **CA-2.2:** Cuando una idea ya fue recordada y sigue sin editarse, el sistema deberá volver a incluirla 14 días después del último recordatorio.
- **CA-2.3:** Cuando el dueño edite una idea, el sistema deberá contar los 14 días desde esa edición (la idea deja de estar estancada).
- **CA-2.4:** Cuando el sistema incluya una idea, deberá mostrar su texto recortado a 80 caracteres y los días sin editarse.
- **CA-2.5:** El envío de un recordatorio no deberá cambiar `UpdatedAt` de la idea.
- **CA-2.6:** Las ideas archivadas no deberán entrar en el resumen.

### RF-3 — Hábitos en riesgo (HU-2)

Todas las reglas usan los registros del hábito y el día del dueño. "Hoy" aún no tiene registro cuando llega el correo de las 8:00.

- **CA-3.1 — Ayer no cumplí (diario):** Cuando un hábito diario existía ayer y no tiene registro de ayer, el sistema deberá incluir la alerta.
- **CA-3.2 — Meta semanal en riesgo (semanal):** Mientras la semana en curso tenga menos cumplimientos que la meta, y los días que quedan (incluido hoy) sean igual o menos que los cumplimientos que faltan, el sistema deberá incluir la alerta. No se incluye si el hábito se creó en la semana en curso: la alerta empieza la semana siguiente (decisión del dueño, 2026-11-02).
- **CA-3.3 — Semana pasada fallada (semanal):** Cuando sea lunes y el hábito semanal existía durante la semana anterior con menos cumplimientos que su meta, el sistema deberá incluir la alerta.
- **CA-3.4 — Racha perdida (diario):** Cuando un hábito diario tenga una racha de 3 días o más que se cortó ayer (no hay registro de ayer, pero sí de los 3 días anteriores a ayer), el sistema deberá incluir la alerta.
- **CA-3.5:** Cada hábito puede aparecer con más de una alerta, una línea por alerta.
- **CA-3.6:** Un hábito eliminado no deberá aparecer en el resumen.

### RF-4 — Disparo seguro del envío

- **CA-4.1:** El sistema deberá exponer `POST /api/reminders/daily`, que calcula y envía el resumen del día si corresponde (RF-1).
- **CA-4.2:** Si la petición no trae la cabecera `X-Reminder-Secret` con el valor de `Reminders__Secret`, entonces el sistema deberá responder 401 sin enviar nada y sin revelar si hay datos. La comparación deberá ser en tiempo constante.
- **CA-4.3:** Este endpoint es la única excepción a "todo endpoint salvo `/api/auth/*` exige sesión" (ver S-4). No usa la sesión del dueño.
- **CA-4.4:** La respuesta deberá contener solo el resultado (`sent`, `skipped` o `failed`) y el número de ítems; nunca el texto de las ideas ni los nombres de los hábitos.

### RF-5 — Publicación gratuita (HU-3)

- **CA-5.1:** El frontend deberá publicarse en GitHub Pages en `https://<usuario>.github.io/<repo>/`.
- **CA-5.2:** El backend deberá publicarse como servicio web gratuito (Render, plan free) y la base como PostgreSQL gratuito (Neon).
- **CA-5.3:** El correo deberá enviarse con Resend (API HTTPS, plan free), porque los servicios gratuitos suelen bloquear el SMTP de Gmail.
- **CA-5.4:** Un flujo de GitHub Actions deberá llamar a `POST /api/reminders/daily` cada hora. Deberá reintentar dos veces si el servicio tarda en despertar.
- **CA-5.5:** La API deberá aceptar peticiones solo del origen `https://<usuario>.github.io` (CORS), con las cabeceras `Authorization` y `X-Reminder-Secret`, y exponer `X-Refreshed-Token`.
- **CA-5.6:** La URL de la API deberá configurarse al construir el frontend (`VITE_API_URL`), sin escribirla en el repositorio.

## 6. Requisitos no funcionales

- **RNF-1 (costo):** $0 mensuales. Ningún servicio exige tarjeta de crédito; si uno la exige, se descarta antes de implementar.
- **RNF-2 (secretos):** `Resend__ApiKey`, `Reminders__Secret`, `Jwt__SigningKey` y la cadena de conexión viven solo en variables de entorno del servicio y de GitHub Actions (principio 5). Nunca en el repositorio ni en los logs.
- **RNF-3 (sin duplicados):** el resumen de un día se registra en una tabla `DailyDigestLog` con `ForDate` único. Dos ejecuciones del mismo día no envían dos correos.
- **RNF-4 (puntualidad):** el resumen llega entre las 8:00 y ~9:30 del día del dueño cuando el servicio está despierto. Si Render duerme, puede sumarse el arranque (~1 minuto). Es aceptable para un resumen diario.
- **RNF-5 (vigilancia):** si el flujo de GitHub Actions recibe otra respuesta que no sea 2xx tras los reintentos, termina en error. GitHub avisa de ese fallo por correo al dueño.
- **RNF-6 (privacidad):** el correo contiene el texto de las ideas del dueño y se envía a su propia cuenta. Resend procesa ese contenido como proveedor; se documenta en `docs/decisions.md`.
- **RNF-7 (dependencias):** sin librería nueva. El envío usa `HttpClient` contra la API REST de Resend (principio 1).
- **RNF-8 (idioma):** textos del correo y de la respuesta del endpoint en español neutro; código en inglés (principio 6).
- **RNF-9 (reglas en el servidor):** las reglas viven en `Services/` (`ReminderService`, `ReminderRules`). Los endpoints solo delegan (principio 3).

## 7. Casos límite

- **Ninguna idea ni hábito que reportar:** no hay correo, y el día no se marca como enviado; la ejecución de la hora siguiente vuelve a comprobar, sin costo (CA-1.2).
- **Ejecuciones solapadas a las 8:00:** la fila de `DailyDigestLog` ya insertada evita el segundo envío (RNF-3).
- **Resend responde error:** el día no queda marcado y la siguiente hora reintenta (CA-1.4). Si nunca se recupera, el flujo falla y GitHub avisa (RNF-5).
- **Render dormido:** la primera llamada tarda ~1 minuto; los reintentos del flujo lo cubren (CA-5.4).
- **Cambio de horario de verano:** el día del dueño se calcula con `OwnerClock`, no con la hora UTC del servidor.
- **Idea editada el mismo día del resumen:** cuenta como edición; ya no entra (CA-2.3).
- **Idea restaurada tras archivarse:** el reloj cuenta desde la restauración (S-3), no desde su última edición antes de archivarla.
- **Hábito semanal creado esta semana:** no aplica CA-3.3 hasta la semana siguiente (no existía la semana anterior completa).
- **Hábito diario creado ayer:** no aplica CA-3.1 (no existía ayer).
- **Correo a spam:** el remitente de prueba de Resend (`onboarding@resend.dev`) puede caer en spam; se verifica al primer envío real (D-2).

## 8. Fuera de alcance (esta versión)

- Enlaces desde el correo a la app.
- Elegir la hora de envío desde la interfaz (la hora es fija: 8:00).
- Varios destinatarios, dominio propio o remitente personalizado.
- Notificaciones push, SMS o WhatsApp.
- Usar Gmail SMTP o la API de Gmail (se documentará en `docs/decisions.md` al implementar).
- Recordatorios por hora o por semana fuera de las reglas de RF-2 y RF-3.

## 9. Criterios de finalización

- Todos los CA de RF-1 a RF-5 se cumplen.
- Cada regla de RF-2 y RF-3 tiene al menos un test de xunit que pasa (principio 4). Las reglas de racha se comparan con `streak.js` mediante casos compartidos (D-3).
- Un envío real llega al Gmail del dueño desde la URL publicada, y `POST /api/reminders/daily` sin cabecera responde 401.
- El flujo de GitHub Actions corre al menos una vez con éxito en el repositorio publicado.
- `CLAUDE.md` describe la publicación real (Pages, Render, Neon, Resend) y la excepción del endpoint de recordatorios.
- `docs/constitution.md` y `docs/specs/002-login.md` reflejan la excepción de S-4 en el mismo commit.

## 10. Supuestos y decisiones pendientes de aprobación

El dueño aprueba esta spec completa. Los puntos siguientes son decisiones de diseño que no se mencionaron en la conversación; si alguno no encaja, se cambia antes de escribir tareas.

- **S-1 Racha perdida:** solo aplica a hábitos diarios, con mínimo de 3 días. Los semanales se cubren con CA-3.3.
- **S-2 "En riesgo" semanal:** cuenta como en riesgo cuando los días que quedan (incluido hoy) no alcanzan más que para los cumplimientos que faltan (CA-3.2).
- **S-3 Restaurar una idea:** reinicia el reloj de 14 días. Esto cambia el comportamiento de la spec 003, que hoy no toca `UpdatedAt` al restaurar; el cambio va en el mismo commit (principio 2).
- **S-4 Enmienda de la constitución:** el principio 5 y la sección de endpoints de `CLAUDE.md` dicen que todo endpoint salvo el login exige sesión. Se añade la excepción de `POST /api/reminders/daily`, protegido por `X-Reminder-Secret` (CA-4.3). La spec 002 se actualiza en el mismo commit.
- **S-5 Frontend en GitHub Pages:** reemplaza a Vercel como plataforma del frontend. `CLAUDE.md` (sección "Deployment") se actualiza.
- **S-6 Vigilancia:** el único aviso de fallo es el correo que GitHub envía al dueño cuando un flujo falla (RNF-5). No hay otro monitoreo.
- **S-7 Repositorio público:** GitHub desactiva los flujos programados de un repositorio público tras 60 días sin actividad. Los commits normales lo evitan; si el correo deja de llegar, se revisa primero esto.
- **S-8 Constantes fijas:** 14 días, 3 días de racha, 80 caracteres y las 8:00 son constantes en `ReminderRules`, no variables de entorno.

## 11. Dudas abiertas (se resuelven durante la implementación)

- **D-1** Verificar que Neon soporta la extensión `unaccent` antes de desplegar. No se ha comprobado.
- **D-2** Confirmar que el primer correo de `onboarding@resend.dev` llega a la bandeja principal de Gmail y no a spam.
- **D-3** Evitar que `ReminderRules` (C#) y `streak.js` (JS) diverjan: un archivo JSON de casos de racha lo leen tanto xunit como `node --test`.
