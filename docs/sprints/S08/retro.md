# Retro S08 — Nombre y estadísticas al cargar historias

Inicio: 2026-10-26T12:44:00Z · Cierre: 2026-10-26 · Estado: HECHO (pendiente de prueba en móvil)

## Hecho
- T35 (a, b): `LoadStats`, `StoryNameValidator` y tests EditMode.
- T36 (a, b, c): spec UX, popup de carga `NameInputPopup` integrado en Cargar, retirado el diálogo de conflicto de S04 (`ChoicePopup`, `ConflictPolicy`, `AskConflict`).
- T37 (a, b): spec UX y Editar nombre (`BtnEdit`, `StoryLibrary.Rename`, conserva id).
- T38: revisión-1 con veredicto OK (EditMode 209/209, PlayMode 19/19). Sin bloqueantes; 5 menores pasados al backlog.

## Pendiente
- Prueba en móvil del usuario (picker real, teclado, Duplicate, Editar, persistencia). Lista en `plan.md`.
- Menores de revision-1 en `docs/backlog.md` ("Origen S08").
- Docs de `ux-ui` por actualizar: `docs/ui/S04-biblioteca.md` (146-161) y `docs/ui/S08-nombre-historia.md` (50). Dueño: `ux-ui`.

## Tokens (`node tools/token-report.mjs --since 2026-10-26T12:44:00Z`)
Total: 14.168.940 tokens. Modelos: solo Sonnet y Haiku (cumple la regla de no usar Opus).

| Agente (id de transcripción) | Turnos | Total | Modelo |
|---|---|---|---|
| orquestador (sesión principal) | 28 | 3.605.692 | Sonnet |
| agent-af8993d36e002e3fe | 34 | 3.982.087 | Sonnet |
| agent-a29943588ce65a888 | 20 | 2.051.833 | Sonnet |
| agent-ab9f8e578352fa97f | 18 | 1.738.070 | Sonnet |
| agent-a109bc19ce18c4974 | 18 | 1.477.614 | Haiku |
| agent-a184874f8192abdc8 | 9 | 715.539 | Sonnet |
| agent-a15a264a214b66cd8 | 3 | 222.838 | Sonnet |
| agent-ab946aa8573b1e215 | 6 | 150.400 | Sonnet |
| agent-a6d1aba03663ab517 | 6 | 145.731 | Sonnet |
| agent-a3afed067a1aad030 | 3 | 39.936 | Haiku |
| agent-a20261439c898ad47 | 2 | 39.200 | Haiku |

Limitación: el informe da ids de transcripción, no el rol. No se puede asignar cada coste a arquitecto/programador/ux-ui/revisor sin revisar las transcripciones. Pedir al informe que muestre el rol.

## Gasto frente a lo esperado
- El presupuesto del plan era "medio-bajo". El total (14,2 M) está por encima de lo previsto.
- La transcripción de mayor coste tiene 34 turnos, por encima del tope de 15 turnos por invocación del plan (si fue una sola invocación).
- El orquestador gastó 3,6 M en 28 turnos. La mayor parte es caché de lectura, que crece con cada turno de la sesión principal.

## Medidas para S09
1. Cumplir el tope de 15 turnos por invocación: si el programador no cierra la subtarea, parar y reencargar con una subtarea más pequeña, no alargar la sesión.
2. Mantener la sesión principal corta: cerrar cada tarea con una sesión nueva o con solo las rutas necesarias. Leer secciones concretas (offset/limit) de `roadmap.md` y `backlog.md` en vez de ficheros enteros.
3. Revisiones de cambios pequeños (menores, docs) con Haiku. Reservar Sonnet para revisión de lógica y tests. Pedir a `tools/token-report.mjs` que muestre el rol de cada agente.
