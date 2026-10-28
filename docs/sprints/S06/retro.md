# S06 — Retro

Inicio: 2026-10-26T08:19:34Z. Informe: `node tools/token-report.mjs --since 2026-10-26T08:19:34Z`. Total **10,61 M** (S05b: 30,44 M; **−65,1 %**, queda en el 34,9 % de S05b). Orquestador 4,85 M (46 turnos, Opus); subagentes ~5,76 M.

## Hecho
- Kanjis complicados completo: T25 (modelo `DifficultKanjiList`, repositorio JSON, tests), T26 (botón "Añadir a complicados" + toast reutilizable), T27 (pantalla de complicados + popup modo Difficult con "Eliminar de la lista"), T28 (revisión).
- Specs de ux-ui en `docs/ui/` para T26a y T27a.
- Tests: EditMode 195/195, PlayMode 14/14.
- Revisión ronda 1: CAMBIOS NECESARIOS (0 bloqueantes, 1 importante, 3 menores). Ronda 2: APROBADO.
- Sin consultas al arquitecto ni `dudas.md`.
- APK: `Builds/S06/KanjiLearning-S06-dev.apk` (68 793 667 bytes, ~65,6 MB; Development, IL2CPP, ARM64; build de 8 min 38 s).
- **Prueba en móvil (T28) superada el 2026-10-26** con ese APK: añadir desde la lectura, reabrir la app, ver y eliminar. Valoración del usuario: "casi perfecto"; los arreglos menores se dejan para el final del proyecto.

## Pendiente (al backlog, sección "Arreglos para el final del proyecto (origen S06)")
- Menores de `revision-1.md` no corregidos: listeners con lambdas; orden de `SerializeField` en `KanjiPopup`.
- Toast de 3 s frente a los 2 s de la spec.
- Error del Editor durante la build Android: "Subtarget property is not available for the platform (Android)" (la build sale bien).

## Lecciones
- Unity no recompila solo con el Editor en segundo plano. Usar `unity command script recompile` y luego `recompile_status`. T25a dio 172/172 falsos por no hacerlo.
- PlayMode: `test status` consultado al poco de lanzar da 0/0. Repetirlo al terminar da 14/14.

## Tokens por agente

| Agente | Rol | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador | sesión principal | 46 | 4.852.363 | Opus |
| a2c55b29 | programador T27c | 14 | 1.093.184 | Sonnet |
| a9ef1b6b | programador T25b | 12 | 912.488 | Sonnet |
| a56c20d2 | programador T26b | 11 | 878.323 | Sonnet |
| a7ae5e54 | programador T27b | 11 | 853.995 | Sonnet |
| aa5a7c39 | programador T25a | 10 | 695.765 | Sonnet |
| a8f5677c | programador correcciones R1 | 9 | 603.311 | Sonnet |
| abfc1dbd | ux-ui | 5 | 341.280 | Sonnet |
| a9bb7781 | revisor R1 | 6 | 144.022 | Sonnet |
| a1531a2a | arquitecto | 5 | 119.347 | Sonnet |
| a4d0e405 | revisor R2 | 5 | 67.976 | Sonnet |
| a54f33e3 | planificador | 3 | 39.986 | Sonnet |
| a8477dba | planificador (cierre) | 1 | 9.365 | Haiku |
| **Total** | | | **10.611.405** | |

Nota: a54f33e3 (plan) corrió en Sonnet porque se lanzó antes de que se recargara el cambio de modelo; el cierre (a8477dba) ya corrió en Haiku, así que el cambio está aplicado.

## Frente a S05b
- S05b: 30,44 M, 49 turnos de orquestador (6,30 M), arquitecto en Opus, programadores de 33-47 turnos.
- S06: 10,61 M. Ningún subagente pasó de 14 turnos. Arquitecto y revisor ya en Sonnet.
- Lo que sigue pesando: el orquestador (4,85 M, 46 turnos, Opus) es el 46 % del total.

## Qué gastó de más
- Orquestador: 46 turnos y 15 k de output. Gran parte es coordinación y lectura de ficheros.
- Relanzamientos por recompilación y PlayMode mal consultados (T25a y `test status`).
- ux-ui: 75 k de cache write en solo 5 turnos (contexto grande de partida).

## Medidas para S07
1. Encargos con el procedimiento de Unity incluido (recompile + `recompile_status`; `test status` repetido al final). Evita relanzar tests.
2. Probar el orquestador con menos turnos: agrupar lecturas y encargos en un mismo turno cuando no dependan entre sí. Valorar Sonnet para sprints de tareas pequeñas.
3. Encargos a ux-ui con rutas exactas de la plantilla y de la spec previa, sin releer `docs/ui/` completo.
