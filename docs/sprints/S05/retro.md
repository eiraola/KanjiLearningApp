# S05 — Retro

Informe: `node tools/token-report.mjs --since 2026-10-22T16:42:41Z`. Total **43,09 M** (S04 ≈ 37,8 M; S03: 10,98 M). Orquestador 16,49 M (104 turnos, Opus), subagentes ~26,6 M. Incluye revisión S04/S05, fixes y cierre.

## Hecho
- T20 `StoryRichTextBuilder` (164/164 EditMode), T21 `ReaderScreen` (Editor: 50 000 car. 1,0 s en frío, 0,3 s en caliente, 7 trozos), T22 pulsar kanji + popup vista 1, T23 vista 2 "Mostrar más".
- Revisión S05 ronda 1 sin bloqueantes (4 menores); ronda 2 de S04 aprobada (166/166 EditMode).
- T24 (usuario, 2026-10-23, APK `Builds/S05/KanjiLearning-S05-dev.apk`): toques y popup vistas 1 y 2 correctos; T19b y pendientes de S04 correctos.
- No consta: medidas de rendimiento en móvil (`[Kanji][Perf]` totalMs, over33ms/maxMs, memoria), prueba de `hostile-markup.txt` en móvil, PlayMode 2/2 desde Test Runner. La historia real del usuario "no carga entera" en el lector (posible fallo del .txt; sin investigar). Todo al backlog.

## Tokens por agente
| Agente | Rol probable | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador | sesión principal | 104 | 16,49 M | Opus |
| agent-ae5018… | programador (T21) | 74 | 9,24 M | Sonnet |
| agent-a0d1af… | programador | 43 | 3,97 M | Sonnet |
| agent-a2457a… | programador | 32 | 3,66 M | Sonnet |
| agent-a4a8db… | programador | 31 | 3,37 M | Sonnet |
| agent-a973fd… | arquitecto | 25 | 2,11 M | Opus |
| agent-ae97ed… | programador/revisor | 23 | 2,05 M | Sonnet |
| agent-af3e11…, ad550b… | arquitecto/ux-ui | 13-26 | 0,80 / 0,74 M | Opus |
| otros 5 | planificador, correcciones | 2-5 | ~0,7 M | Sonnet |

## Qué gastó de más
- T21 (lector): un programador con 74 turnos y 9,2 M (limite 15 turnos incumplido; mayor invocación de todo el proyecto).
- Orquestador: 38 % del total, otra vez por contexto largo en Opus.
- Editor bloqueado por diálogo modal "guardar escena" del test runner tras regenerar la escena, con reintentos.
- Revisión por MCP: PlayMode devolvió 0 tests; varios intentos sin resultado.

## Medidas para S06
1. Guardar la escena antes de cada `test run` (y tras regenerarla) para evitar el modal; no reintentar más de una vez un runner que devuelve 0 tests: anotar y seguir.
2. Partir tareas como T21 (pantalla + rendimiento + errores) en 2 encargos y parar al programador a los 15 turnos.
3. Pedir la prueba en móvil con una lista de medidas obligatoria (logs `[Kanji][Perf]`, `adb logcat`) antes de cerrar la tarea; sin esas medidas, la tarea no pasa a "hecha".
