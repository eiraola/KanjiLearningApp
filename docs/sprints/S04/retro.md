# S04 — Retro

Informe: `node tools/token-report.mjs --since 2026-10-22T12:04:23Z`. La herramienta no admite fin: el informe incluye S05 (80,87 M). **S04 ≈ 80,87 - 43,09 (informe de S05) = ~37,8 M** (orquestador ~13,7 M, 87 turnos; resto ~24 M en subagentes). S03: 10,98 M -> S04 x3,4 (primer sprint con UI, 6 encargos de programador + T19b + revisión en 2 rondas). Cifra aproximada: un subagente que cruza el inicio de S05 cuenta solo en S05.

## Hecho
- T19a parser `Sentence.` (119 tests), T15 lista (T15a-c), T16 popups + Eliminar (T16a-b), T17 Leer placeholder, T18 duplicados en Cargar, T19b APK dev + logs.
- Revisión combinada con S05: ronda 1 con 1 importante + 2 más (PopupService.Reorder, contador `.tmp`, fallback de `Replace`); ronda 2 APROBADO, 166/166 EditMode.
- PlayMode 2/2: no confirmado (el runner por MCP devolvió 0 tests).
- Prueba en móvil (T24, 2026-10-23): T19b correcto (20 importaciones + force-stop), diálogo de duplicado y Atrás a la lista en su posición correctos.

## Tokens por agente
Atribución aproximada (subagentes `agent-<id>`; ver informe completo en el comando).
| Agente | Rol probable | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador | sesión principal | 191 (acum. S04+S05) | 30,2 M (acum.) | Opus |
| agent-a405bef… | programador | 47 | 5,84 M | Sonnet |
| agent-a520a1… | programador | 36 | 3,91 M | Sonnet |
| agent-aa6d0c… | programador | 24 | 2,21 M | Sonnet |
| agent-af6dff…, ab09a6…, af8bce… | programadores | 19-23 | 1,8-2,0 M c/u | Sonnet |
| agent-a973fd… | arquitecto | 25 | 2,11 M | Opus |
| otros ~14 | arquitecto, ux-ui, revisores, correcciones | 2-26 | ~5 M | Opus/Sonnet |

## Efecto de las medidas de S03
- Sesión partida construir/revisar: el orquestador sigue en ~37 % pero con 87 turnos propios (S03: 41); el contexto largo se mantiene.
- Un programador por tarea: varios superan 15 turnos (47, 36, 24); la regla de partir a 15 turnos no se aplicó.
- ux-ui con una sola especificación: bien, sin repeticiones visibles.

## Qué gastó de más
- Orquestador en Opus: ~13,7 M solo en S04, casi todo cache read.
- Encargos de UI (T15c, T16b) con 36-47 turnos de programador.
- Hallazgo importante de la ronda 1 (`Reorder`) tuvo coste de segunda ronda completa.

## Medidas para S06
1. Partir en dos encargos cualquier tarea de UI cuyo programador pase de 15 turnos y exigir en el encargo una lista de ficheros máxima (3).
2. El orquestador (relevos) en Sonnet; Opus solo para arquitecto y decisiones nuevas.
3. Test de orden/modalidad de popups desde el principio (checklist en el encargo de UI) para evitar rondas de revisión por defectos de capas.
