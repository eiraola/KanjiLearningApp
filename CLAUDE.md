# KanjiLearning

App 2D para móvil para aprender kanji. Unity **6000.6.2f1**, URP 2D, Input System, uGUI/TextMeshPro, Test Framework.
Idioma de trabajo con el usuario: **español**. Código, identificadores y commits: inglés.

## Unity en vivo (MCP)
- El servidor MCP `unity` (`.mcp.json`) expone el Editor abierto como herramientas. Requiere el paquete `com.unity.pipeline` (ya instalado) y **el Editor abierto con este proyecto**.
- Comprobar conexión: `unity status` (estado `ready`). Si no conecta, mirar Safe Mode con `unity pipeline list`.
- Nunca editar a mano `.unity`, `.prefab` o `.asset` si hay Editor conectado; usar las herramientas MCP o `unity command`.

## Equipo de agentes (`.claude/agents/`)
| Agente | Rol | Escribe en |
|---|---|---|
| `planificador` | Organiza sprints, evita bucles, vigila tokens | `docs/sprints/S<NN>/plan.md`, `retro.md`, `bloqueos.md`, `docs/backlog.md` |
| `disenador` | Decide cómo funciona la app | `docs/diseno/GDD.md` |
| `ux-ui` | Decide la interfaz | `docs/ui/` |
| `arquitecto` | Arquitectura modular y escalable | `docs/arquitectura/`, `docs/sprints/S<NN>/arquitectura.md` |
| `programador` | Implementa (2D móvil, patrones) | `Assets/`, `implementacion.md`, `dudas.md` |
| `revisor` | Busca bugs, ejecuta tests | `docs/sprints/S<NN>/revision-<n>.md` |

Los subagentes no pueden llamarse entre sí: **la sesión principal hace de orquestador** y pasa el trabajo de uno a otro a través de los ficheros de `docs/sprints/S<NN>/`. Al encargar algo a un agente, pásale solo el número de sprint, la tarea y las rutas que debe leer; no le copies documentos enteros.

## Flujo de un sprint
1. `/sprint-planificar <idea>` → diseñador propone alcance → planificador escribe `plan.md`. **Pausa para que el usuario apruebe el plan.**
2. `/sprint-construir <NN>` → por tarea: arquitecto escribe `arquitectura.md` (confirmando requisitos con el diseñador) → ux-ui especifica la UI si la tarea la tiene → programador implementa; sus dudas van al arquitecto.
3. `/sprint-revisar <NN>` → revisor revisa y prueba → programador corrige → planificador cierra con la retro y el informe de tokens.

## Límites anti-bucle (los vigila el planificador)
- Máximo **5 tareas** por sprint.
- Máximo **2 consultas** del programador al arquitecto por tarea. A la tercera, se para y se pregunta al usuario.
- Máximo **2 idas y vueltas** arquitecto ↔ diseñador por sprint.
- Máximo **2 rondas** de revisión. Si tras la segunda sigue habiendo bloqueantes, se para y se anota en `bloqueos.md`.
- Ningún agente reescribe un documento de otro agente; propone el cambio y el dueño lo aplica.

## Gasto de tokens
`node tools/token-report.mjs --since <ISO>` resume tokens por agente a partir de las transcripciones locales de Claude Code. El planificador lo ejecuta al cerrar cada sprint.
