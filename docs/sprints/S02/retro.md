# S02 — Retro

Informe: `node tools/token-report.mjs --since 2026-10-22T09:08:22Z`. Total **18,55 M** (cache read ~17,4 M; output ~39 k; cache write ~0,83 M).

## Hecho
- T06 `IFilePicker` + `NativeFilePickerAdapter` (ADR 0002).
- T07 `TextDecoder` UTF-8 / BOM / Shift-JIS, límite 1 MB, `link.xml`.
- T08 pantalla Cargar con vista previa y errores.
- T09 endurecimiento S01 (atlas, fallbacks, `AppBootstrap`, nota T04).
- Revisión: 2 rondas (límite respetado). Ronda 1 con 1 bloqueante (`TryDelete` borraba el original), corregido. Ronda 2: APROBADO, 19/19 tests EditMode, APK dev 65,6 MB.

## Pendiente (pasado a `docs/backlog.md`)
- T10 en móvil, a cargo del usuario (7 comprobaciones de `revision-2.md`).
- Menores de `revision-2.md`: doble callback en el adaptador, decodificador IBM, rebuild del layout, `OnPicked` oculto, permiso denegado.
- Build release con stripping, alinear menú de build, parametrizar dev/release.

## Tokens por agente
Atribución aproximada (los subagentes salen como `agent-<id>`).
| Agente | Rol probable | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador | sesión principal | 61 | 8,63 M | Opus (26,5 k output) |
| agent-a46d22… | programador | 20 | 2,14 M | Sonnet |
| agent-a25c1a… | programador | 23 | 2,00 M | Sonnet |
| agent-a33f89… | programador | 19 | 1,71 M | Sonnet |
| agent-a1da4d… | arquitecto / diseñador | 30 | 1,50 M | Opus |
| agent-a6c0a3… | programador / revisor | 10 | 0,79 M | Sonnet |
| agent-aba643… | revisor / ux-ui | 7 | 0,52 M | Sonnet |
| otros 8 (Opus y Sonnet) | diseñador, planificador, correcciones | 1-18 | 1,26 M | mixto |

## Qué gastó de más
- Orquestador: 46 % del total (más que en S01: 61 turnos con contexto largo en Opus). Es el mayor gasto individual.
- Programador repartido en 3-4 invocaciones (~6,6 M): la regla de una invocación por tarea funcionó (ninguna pasó de 23 turnos frente a 69 en S01), pero cada una relee ~85 k de contexto.
- Un agente Opus de 30 turnos (1,5 M), probablemente arquitecto con consultas; vigilar el límite de 2 consultas.
- Cache write bajo (0,83 M) y output mínimo: el gasto es casi todo relectura de cache.

## Medidas para S03
1. Orquestador: no releer documentos ya resumidos y pasar a los agentes solo ruta + tarea; si el usuario lo permite, usar Sonnet para el relevo entre agentes.
2. Contexto de arranque más pequeño para el programador: indicar en el encargo solo las secciones de `arquitectura.md` e `implementacion.md` de su tarea (lectura por rango), no los ficheros enteros.
3. Arquitecto en Sonnet para tareas con ADR ya decidido (p. ej. parser/persistencia con patrones de S02), y Opus solo si hay decisión nueva; tope de 15 turnos.
