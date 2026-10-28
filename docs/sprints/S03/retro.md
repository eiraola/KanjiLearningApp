# S03 — Retro

Informe: `node tools/token-report.mjs --since 2026-10-22T10:30:00Z`. Total **10,98 M** (cache read ~10,4 M; output ~22 k; cache write ~0,56 M). S02: 18,55 M -> **-41 %**.

## Hecho
- T11a modelo + `StoryParser` (casos válidos), T11b reglas de error (una fila GDD 5.2 = un test).
- T12 `JsonStoryRepository` + `AtomicFileWriter` + DTO con `version`.
- T13 `StoryLibrary` (Import/Conflict/Replace/KeepBoth/Cancel).
- T14 fixture 50 000 caracteres / 200 kanjis; parseo ≈1,6 ms (< 1 s).
- Revisión: 1 ronda, APROBADO, 115/115 EditMode, 0 bloqueantes, 7 menores (al backlog).

## Tokens por agente
Atribución aproximada (subagentes como `agent-<id>`).
| Agente | Rol probable | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador | sesión principal | 41 | 5,53 M | Opus (17,7 k output) |
| agent-a7d113… | programador | 18 | 1,56 M | Sonnet |
| agent-a205c6… | programador | 11 | 0,99 M | Sonnet |
| agent-acf814… | programador | 9 | 0,83 M | Sonnet |
| agent-ad657e… | programador | 9 | 0,74 M | Sonnet |
| agent-ac20f6… | arquitecto | 13 | 0,52 M | Opus |
| agent-a7c53d… | revisor | 6 | 0,51 M | Sonnet |
| otros 6 | diseñador, planificador, correcciones | 1-5 | 0,31 M | Sonnet |

## Efecto de las medidas de S02
- Programador nuevo por tarea: funcionó. Ninguna invocación pasó de 18 turnos (S02: 23) y el mayor es 1,56 M (S02: 2,14 M).
- Sin modales ni sondeos: sin reintentos visibles; el revisor lanzó los tests una vez (6 turnos).
- Arquitecto: 13 turnos y 0,52 M (S02: 30 turnos y 1,5 M), dentro del tope de 15.
- Orquestador: 41 turnos y 5,53 M (S02: 61 y 8,63 M). Sigue siendo el mayor gasto individual (50 % del total), pero baja un 36 %.

## Qué gastó de más
- Orquestador sigue en ~50 %: contexto largo en Opus, casi todo cache read.
- El primer programador (T11a, 1,56 M) cargó el modelo y el parser a la vez; es la tarea más densa.

## Medidas para S04
1. Orquestador en Sonnet para el relevo entre agentes (solo Opus en decisiones nuevas); si no es posible, partir la sesión en dos (construir / revisar) para no arrastrar contexto.
2. S04 es el primer sprint con UI: encargar a ux-ui una sola especificación por sprint (no por tarea) y pasar al programador solo la sección de su pantalla.
3. Mantener tareas <= 3 scripts y un programador por tarea; T11a-like (modelo + lógica) separar en dos tareas si superan 15 turnos.
