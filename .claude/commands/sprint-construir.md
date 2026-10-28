---
description: Fase 2 del sprint. Arquitectura, especificación de UI e implementación de cada tarea.
argument-hint: <número de sprint, p. ej. 01>
---

Eres el orquestador del sprint S$ARGUMENTS. Lee `docs/sprints/S$ARGUMENTS/plan.md` y `CLAUDE.md` (límites anti-bucle).

0. Comprueba con `unity status` si hay un Editor conectado. Si no, avisa al usuario de que el programador no podrá tocar escenas/prefabs en vivo y pregunta si quiere abrir Unity antes de seguir.

1. **Arquitectura (una vez para todo el sprint).** Lanza `arquitecto`: que escriba `docs/sprints/S$ARGUMENTS/arquitectura.md` para todas las tareas. Después lanza `disenador` para que confirme que la arquitectura cubre los requisitos; si señala huecos, vuelve al arquitecto. Máximo 2 vueltas; si no hay acuerdo, para y pregunta al usuario.

2. **Por cada tarea, en el orden del plan:**
   a. Si la tarea tiene interfaz y no existe su spec, lanza `ux-ui` para escribirla en `docs/ui/`.
   b. Lanza `programador` con: número de sprint, id de la tarea y las rutas a leer.
   c. Si el programador termina con `ESTADO: BLOQUEADO`, lanza `arquitecto` con la pregunta de `dudas.md`, y luego continúa al programador (SendMessage al mismo agente si es posible) con la respuesta. Máximo 2 consultas por tarea; a la tercera, para y pregunta al usuario.
   d. Marca la tarea como hecha en `plan.md`.

3. Al terminar todas las tareas, resume al usuario qué se ha implementado y sugiere `/sprint-revisar $ARGUMENTS`.
