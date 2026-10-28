# S05b — Retro

Informe: `node tools/token-report.mjs --since 2026-10-23T08:15:00Z`. Total **30,44 M** (S05: 43,09 M; S04 ≈ 37,8 M). Orquestador 6,30 M (49 turnos, Opus), subagentes ~24,1 M. El sprint estuvo parado del 2026-10-23 al 2026-10-26 por el límite semanal de uso.

## Hecho
- T1-T4 hechas (T2 y T4 partidas en a/b): fundidos entre pantallas y popups, `InputGate`, `TransitionOverlay`, `PopupService`, popup de kanji de vista única, Atrás.
- Revisión ronda 1 APROBADA, sin bloqueantes, 5 menores (`revision-1.md`).
- Tests: 172/172 EditMode y 12/12 PlayMode.
- T5 (usuario, 2026-10-26, APK `Builds/S05b/KanjiLearning-S05b-dev.apk`): funciona en el móvil.
- Lección: PlayMode por CLI necesita `--async_tests true` y después `unity command test status`; el modo síncrono devuelve 0/0.

## Pendiente (al backlog)
- Decidir si los botones normales dejan de usar el mismo rojo que "No lo sabía" (el usuario no ha respondido; ux-ui recomienda otro color).
- Sin probar: borrado real de una historia, tecla Atrás física, scroll del Lector con una historia larga.
- 5 menores de `revision-1.md`.

## Tokens por agente
| Agente | Rol probable | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador | sesión principal | 49 | 6,30 M | Opus |
| agent-a62607… | programador | 47 | 5,03 M | Sonnet |
| agent-a9e157… | programador | 33 | 4,06 M | Sonnet |
| agent-aec7db… | programador/revisor | 35 | 3,91 M | Sonnet |
| agent-a69d8f… | arquitecto | 34 | 2,29 M | Opus |
| agent-a824a9…, a58379… | programador/ux-ui | 20 | 2,00 / 1,94 M | Sonnet |
| agent-a31a29… | revisor | 17 | 1,85 M | Sonnet |
| agent-a91801…, a96865… | varios (a96865 en Opus) | 13-16 | 1,29 / 0,88 M | Sonnet / Opus |
| otros 3 | planificador, correcciones | 1-9 | ~0,87 M | Sonnet |

## Qué gastó de más
- Tres invocaciones de programador/revisor con 33-47 turnos y 4-5 M cada una (el límite práctico de 15 turnos sigue sin cumplirse).
- a31a29: 210 k de cache write con solo 17 turnos (contexto grande leído de golpe).
- Opus en arquitecto y a96865 (33,7 k de output en 13 turnos): mucho texto de documento.
- Revisión: el PlayMode síncrono devolvía 0/0 y costó investigar hasta dar con `--async_tests true`.

## Medidas para S06
1. Programador: encargos de una sola subtarea (a/b desde el plan) y parada a los 15 turnos con nota en `dudas.md`; no releer ficheros grandes ya leídos.
2. Revisión y tests: usar siempre `test run --mode playmode --async_tests true` + `test status` y guardar la escena antes; sin reintentos si da 0 tests.
3. Arquitecto/ux-ui: documentos de máximo ~1 página y encargos con rutas concretas (sin pasar documentos enteros), para recortar output y cache write.
