# S01 — Retro

Informe: `node tools/token-report.mjs --since 2026-10-22T05:55:00Z`. Total **18,94 M** (cache read ~18,2 M; output solo ~20,9 k; cache write ~0,71 M).

## Tokens por agente
Atribución por orden de lanzamiento y tamaño (aproximada: los subagentes salieron como `agent-<id>` genéricos).
| Agente (transcripción) | Rol probable | Turnos | Total | Modelo |
|---|---|---|---|---|
| agent-a3f90c… | programador (T01-T04) | 69 | 8,67 M | Sonnet |
| orquestador | sesión principal | 40 | 7,95 M | Opus (17,9 k output) |
| agent-ad43cb… | arquitecto / diseñador | 25 | 0,96 M | Opus |
| agent-a26df2… | ux-ui o revisor | 9 | 0,77 M | Sonnet |
| agent-a9ee0f… | diseñador / planificador | 4 | 0,29 M | Opus |
| agent-a8e420… | ux-ui / revisor | 4 | 0,28 M | Sonnet |
| agent-ab25ce… | planificador (cierre) | 2 | 0,02 M | Sonnet |

## Qué gastó de más
- Programador (46 % del total): 69 turnos, cada uno releyendo un contexto de ~120 k en cache. Causas: bloqueo ~15 min por el diálogo modal de import de TMP (turnos de espera/reintento) y el build IL2CPP de ~11 min (sondeos).
- Orquestador (42 %): contexto largo acumulado durante todo el sprint, en Opus.
- Arquitecto/diseñador en Opus: poco volumen, aceptable.

## Medidas para S02
1. Antes de lanzar al programador, el orquestador debe dejar el Editor sin diálogos modales (importar TMP Essentials/paquetes a mano) y pedir al programador que no sondee: una sola espera con `timeout` largo para builds y tests, sin bucles de comprobación.
2. Tareas de programación de ≤2 scripts y una invocación por tarea (contexto nuevo cada vez) en lugar de un agente largo de 69 turnos; así el cache read por turno no crece.
3. Reducir el contexto del orquestador: no releer docs/ficheros ya resumidos en `implementacion.md`, y usar Sonnet para el orquestador en tareas de simple relevo si el usuario lo permite.
