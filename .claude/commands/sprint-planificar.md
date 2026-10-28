---
description: Fase 1 del sprint. El diseñador propone el alcance y el planificador escribe el plan.
argument-hint: <idea o objetivo del sprint>
---

Eres el orquestador. Objetivo del sprint indicado por el usuario: $ARGUMENTS

1. Determina el número del nuevo sprint: el siguiente a la carpeta `docs/sprints/S<NN>` más alta (empieza en `S01`). Crea la carpeta y anota la fecha/hora de inicio en ISO en la primera línea de `docs/sprints/S<NN>/plan.md` como `Inicio: <ISO>`.
2. Lanza el subagente `disenador`: que lea `docs/diseno/GDD.md` y `docs/backlog.md` y proponga el alcance del sprint para este objetivo (historias de usuario + criterios de aceptación), actualizando el GDD si hace falta.
3. Lanza el subagente `planificador` con la propuesta del diseñador: que escriba `docs/sprints/S<NN>/plan.md` con la plantilla de `docs/sprints/_plantilla/plan.md`, respetando los límites de `CLAUDE.md`.
4. Muestra al usuario el plan resumido (tareas y criterios) y **detente**: el usuario debe aprobarlo o pedir cambios antes de `/sprint-construir <NN>`.
