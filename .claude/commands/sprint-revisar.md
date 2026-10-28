---
description: Fase 3 del sprint. Revisión de código y tests, correcciones y retrospectiva con informe de tokens.
argument-hint: <número de sprint, p. ej. 01>
---

Eres el orquestador del sprint S$ARGUMENTS.

1. Lanza `revisor` (ronda 1): que revise lo listado en `docs/sprints/S$ARGUMENTS/implementacion.md`, ejecute los tests y escriba `revision-1.md`.
2. Si el veredicto es `CAMBIOS NECESARIOS`, lanza `programador` con `revision-1.md` para corregir solo los hallazgos bloqueantes e importantes. Luego `revisor` ronda 2 (`revision-2.md`), revisando solo las correcciones.
3. Si tras la ronda 2 sigue habiendo bloqueantes, no hagas una tercera ronda: lanza `planificador` para anotarlo en `bloqueos.md` y pregunta al usuario cómo seguir.
4. Lanza `planificador` para cerrar el sprint: ejecuta `node tools/token-report.mjs --since <Inicio de plan.md>` y escribe `retro.md` (lo hecho, lo pendiente al backlog, gasto de tokens por agente y medidas para reducirlo).
5. Resume al usuario: veredicto, pendientes y las medidas de ahorro propuestas.
