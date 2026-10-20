# Spec 002 — Cuenta del dueño, inicio y cierre de sesión

## 1. Contexto y objetivo

La spec 001 (seguimiento de hábitos) necesita que solo el dueño pueda ver y cambiar sus datos. Hoy la API exige un token que nadie puede obtener, así que ninguna pantalla muestra datos reales.

**Objetivo:** que el dueño cree su cuenta la primera vez que abre la app, inicie sesión con su email y su contraseña, y cierre la sesión desde un botón. Sin cuenta, nadie usa la app. Con cuenta, nadie más puede crear otra.

**Por qué ahora:** es la dependencia que bloquea la verificación de la spec 001 (T47) y la publicación de cualquier dato real.

## 2. Usuarios

- **Dueño (único usuario):** la persona que usa la app. Crea su cuenta una sola vez. No hay otros usuarios, ni registro público, ni recuperación de contraseña en esta versión.

## 3. Historias de usuario

- **HU-1:** Como dueño, quiero crear mi cuenta la primera vez que abro la app, para empezar a usarla sin configurar nada en el servidor.
- **HU-2:** Como dueño, quiero iniciar sesión con mi email y mi contraseña, para ver mis hábitos.
- **HU-3:** Como dueño, quiero seguir con la sesión abierta durante 7 días, para no volver a escribir la contraseña cada vez.
- **HU-4:** Como dueño, quiero un botón "Salir", para cerrar la sesión en una computadora compartida.

## 4. Definiciones

- **Cuenta del dueño:** el único registro de usuario de la app. Tiene un email y una contraseña.
- **Sesión:** el permiso que el navegador guarda después de iniciar sesión, válido por 7 días desde el último uso.
- **Pantalla de primer uso:** la pantalla que pide crear la cuenta, visible solo mientras no exista ninguna cuenta.

## 5. Requisitos funcionales

Notación EARS: *ubicuo* ("El sistema deberá…"), *evento* ("Cuando…"), *estado* ("Mientras…"), *no deseado* ("Si…, entonces…").

### RF-1 — Crear la cuenta del dueño (HU-1)

- **CA-1.1:** Mientras no exista ninguna cuenta, el sistema deberá mostrar la pantalla de primer uso con los campos email, contraseña y repetir contraseña.
- **CA-1.2:** Cuando el usuario envíe la pantalla de primer uso con un email válido, una contraseña de al menos 12 caracteres y la misma contraseña repetida, el sistema deberá crear la cuenta, guardar la contraseña solo como hash e iniciar la sesión.
- **CA-1.3:** Si la contraseña tiene menos de 12 caracteres, entonces el sistema deberá rechazar la creación e indicar el mínimo de 12 caracteres.
- **CA-1.4:** Si la contraseña y su repetición no coinciden, entonces el sistema deberá rechazar la creación e indicar que no coinciden.
- **CA-1.5:** Si el email no tiene formato válido, entonces el sistema deberá rechazar la creación e indicar que el email no es válido.
- **CA-1.6:** Mientras exista una cuenta, el sistema no deberá mostrar la pantalla de primer uso, y deberá rechazar cualquier intento de crear otra cuenta.
- **CA-1.7:** Si dos intentos de creación llegan a la vez, entonces el sistema deberá crear una sola cuenta y rechazar el otro intento.
- **CA-1.8:** Si el email tiene más de 254 caracteres o la contraseña más de 256, entonces el sistema deberá rechazar la creación e indicar el límite superado.
- **CA-1.9:** Si la creación se rechaza porque la cuenta ya existe, entonces la interfaz deberá mostrar la pantalla de inicio de sesión con el mensaje "Ya existe una cuenta.".

### RF-2 — Iniciar sesión (HU-2, HU-3)

- **CA-2.1:** Cuando el usuario envíe el email y la contraseña correctos, el sistema deberá emitir una sesión válida durante 7 días desde el último uso.
- **CA-2.5:** Cuando el usuario use la app con una sesión válida, el sistema deberá renovar la sesión para 7 días más desde ese uso (D-1 resuelta).
- **CA-2.2:** Si el email o la contraseña son incorrectos, entonces el sistema deberá rechazar el inicio de sesión con el mensaje "Email o contraseña incorrectos.", sin indicar cuál de los dos está mal.
- **CA-2.3:** El sistema no deberá bloquear el inicio de sesión después de intentos fallidos (ver riesgo aceptado en RNF-3).
- **CA-2.4:** Si no existe ninguna cuenta, entonces el sistema deberá responder al inicio de sesión igual que a una contraseña incorrecta, y mostrar la pantalla de primer uso.

### RF-3 — Proteger los datos (HU-2)

- **CA-3.1:** El sistema deberá exigir una sesión válida en todos los endpoints de hábitos.
- **CA-3.2:** Si la petición no trae sesión o la sesión venció, entonces el sistema deberá responder 401 y no deberá cambiar ningún dato.
- **CA-3.3:** Cuando la interfaz reciba un 401, deberá mostrar "Tu sesión expiró. Vuelve a iniciar sesión." y volver a la pantalla de inicio de sesión.

### RF-4 — Cerrar sesión (HU-4)

- **CA-4.1:** Cuando el usuario pulse "Salir", el sistema deberá borrar la sesión de ese navegador y mostrar la pantalla de inicio de sesión.
- **CA-4.2:** Cerrar sesión no deberá borrar ni cambiar ningún hábito ni registro.

## 6. Requisitos no funcionales

- **RNF-1 (contraseña):** la contraseña se guarda solo como hash. Nunca se guarda en texto plano, nunca se escribe en los logs y nunca se devuelve en una respuesta.
- **RNF-2 (secretos):** la clave que firma las sesiones se toma de una variable de entorno. Nunca se escribe en el repositorio (principio 5 de la constitución).
- **RNF-3 (riesgo aceptado):** sin límite de intentos fallidos. Esto deja la contraseña expuesta a pruebas repetidas. Se acepta mientras la app sea de uso personal y no esté publicada; antes de publicarla, esta decisión se revisa (ver CA-2.3).
- **RNF-4 (idioma):** todos los mensajes visibles están en español neutro.
- **RNF-5 (email):** el email se compara sin distinguir mayúsculas de minúsculas.

## 7. Casos límite

- **Primer uso desde dos pestañas:** las dos pantallas están abiertas y se envían casi a la vez. Solo una cuenta se crea (CA-1.7).
- **Cuenta creada en otra pestaña:** la pantalla de primer uso desaparece al recargar, y un envío atrasado se rechaza y lleva al inicio de sesión (CA-1.6, CA-1.9).
- **"Salir" con una petición en curso:** la respuesta que llega después de cerrar sesión no vuelve a abrirla (CA-4.1).
- **Sesión que vence mientras se escribe un hábito:** la acción se rechaza sin cambios y la interfaz pide volver a iniciar sesión (CA-3.2, CA-3.3).
- **Salir y volver a entrar:** los hábitos siguen igual (CA-4.2).
- **Email con mayúsculas al iniciar sesión:** se acepta igual que en minúsculas (RNF-5).

## 8. Fuera de alcance (esta versión)

- Recuperar o cambiar la contraseña.
- Cambiar el email.
- Más de una cuenta, registro público o roles.
- Bloqueo por intentos fallidos (ver RNF-3).
- Inicio de sesión con Google o con otra cuenta externa.
- Cerrar la sesión en otros dispositivos desde la app.

## 9. Criterios de finalización

- Todos los criterios de RF-1 a RF-4 se cumplen.
- Cada regla de RF-1 a RF-4 tiene al menos un test automatizado que pasa, como exige el principio 4 de la constitución.
- El dueño puede crear su cuenta, iniciar sesión, usar la app, cerrar sesión y volver a entrar.
- Con sesión vencida, cualquier acción de la fase 8 muestra el mensaje de sesión expirada y no cambia datos.
- Las dudas D-1 y D-2 están resueltas o diferidas explícitamente antes de empezar el plan.
- La spec 001 queda desbloqueada: T47 puede ejecutarse.

## 10. Dudas abiertas

- **D-1** Resuelta: la sesión se renueva con cada uso y vence 7 días después del último uso. Mientras se use la app, nunca vence.
- **D-2** Resuelta: "Salir" solo borra el acceso del navegador. Quien haya copiado el acceso antes de cerrar sesión puede seguir usándolo hasta que venza. Invalidar en el servidor queda fuera de esta versión (exige guardar las sesiones activas).
