# Constitución — Asistente Personal

1. **Stack cerrado**: React + Vite, ASP.NET Core, PostgreSQL. Cualquier librería nueva se justifica antes en `docs/decisions.md`: qué problema resuelve y por qué no alcanza lo que ya hay.
2. **Spec antes que código**: toda funcionalidad empieza en `docs/specs/NNN-<feature>.md` (NNN = número correlativo) con criterios de aceptación. Si el código cambia el comportamiento, la spec se actualiza en el mismo commit.
3. **Lógica separada de la interfaz**: las reglas de negocio (rachas, % de cumplimiento) viven en `Services/` y en funciones puras de `src/logic/`. Los Controllers y componentes React solo reciben, delegan y muestran.
4. **Tests sin dependencias extra**: backend con la plantilla `dotnet new xunit` sin agregados; frontend con `node --test`. Cada regla de negocio tiene al menos un test, y ambos comandos pasan antes de cada commit.
5. **Datos del usuario**: app de un solo usuario, sin registro público. Todo endpoint salvo el login exige autenticación. Las contraseñas se guardan solo como hash y los secretos van en variables de entorno, nunca en el repo.
6. **Idioma**: código, identificadores y commits en inglés; textos de UI, specs y docs en español neutro.
7. **Legible para un junior**: funciones de 30 líneas como máximo y sin abstracciones "por si acaso". En los componentes React, el marcado JSX no cuenta para ese límite, pero la lógica sí (estado, efectos y manejadores). Si algo necesita un comentario para entenderse, primero se simplifica.
