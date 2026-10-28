Inicio: 2026-10-22T12:04:23Z

# Sprint S04 — Biblioteca: lista, popup, eliminar, Leer (placeholder) y duplicados

## Objetivo
Primera UI de biblioteca sobre `StoryLibrary`/`JsonStoryRepository` (S03): listar, abrir popup, eliminar con confirmación y resolver nombres duplicados al cargar. Cerrar menores de S03 (parser `Sentence.` y guardado atómico en móvil).

## Reglas de ejecución (medidas de la retro S03)
- ux-ui escribe **una sola especificación** para todo el sprint (`docs/ui/`); cada programador recibe solo la sección de su pantalla.
- Tareas de ≤ 3 scripts; **un programador nuevo por tarea**.
- Sesión partida: una para `/sprint-construir`, otra para `/sprint-revisar`.
- Si una tarea supera 15 turnos del programador, se parte.

## Supuestos (aprobados por el usuario el 2026-10-22, junto con el plan)
- S1: Leer es un placeholder (nombre y nº de kanjis); la lectura real va en S05.
- S2: Orden de la lista: más reciente primero.
- S3: Tras eliminar solo hay mensaje "Historia 'X' eliminada", sin Deshacer.

## Orden de ejecución
T19(a) -> T15 -> T16 -> T17 -> T18 -> T19(b) (al final, necesita al usuario con el móvil).

## Encargos (partición del arquitecto, sin añadir tareas; detalle en `arquitectura.md`)
- T15 = T15a (Core) + T15b (repositorio) + T15c (UI).
- T16 = T16a (sistema de popups + ToastView) + T16b (popups de la lista).
- Cada encargo ≤ 3 scripts, un programador nuevo por encargo.

## Tareas
| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T19a | Parser: `Sentence.` con `:` en la frase (backlog S03) | - `{Kanji: 時; Sentence. 会議は10:30です}` conserva la frase.<br>- `Sentence:` con 10:30 sigue bien.<br>- Test EditMode nuevo.<br>- 115 tests previos en verde. | arquitecto (criterio `.` antes del primer `:` y campo conocido; si no, documentar), programador, revisor | hecha (119/119 EditMode) |
| T15 | Lista de biblioteca (carga + render) | - Fila con nombre y "N kanjis".<br>- Más reciente primero.<br>- Vacía: "No hay historias. Ve a Cargar historias".<br>- 200 historias abren en < 1 s.<br>- Persiste tras reiniciar.<br>- JSON dañado se omite con aviso.<br>- Arquitecto mide `List()` y decide `HeaderDto`/contadores (backlog). | arquitecto, ux-ui (especificación única del sprint), programador, revisor | hecha (T15a-c; 132/132) |
| T16 | Popup de historia + Eliminar con confirmación | - Cierra con tocar fuera/Atrás sin cambios.<br>- Eliminar abre "¿Eliminar 'X'? No se puede deshacer." con Cancelar (foco por defecto) y Eliminar.<br>- Cancelar no borra.<br>- Eliminar borra del disco, la fila desaparece < 1 s y no reaparece tras reiniciar.<br>- Botones ≥ 48 dp.<br>- Mensaje "Historia 'X' eliminada". | arquitecto, programador, revisor | hecha (T16a-b; 134/134) |
| T17 | Leer (placeholder) | - Abre pantalla con nombre y nº de kanjis de la fila tocada.<br>- Atrás vuelve a la lista en la misma posición de scroll. | arquitecto, programador, revisor | hecha (134/134; falta probar en Play) |
| T18 | Diálogo de nombre duplicado en Cargar | - Reemplazar deja una sola historia con contenido nuevo.<br>- Conservar ambas crea "X (2)"/"X (3)".<br>- Cancelar no cambia nada ni deja `.tmp`.<br>- Se informa "Historia 'X' cargada: N kanjis".<br>- Atrás = Cancelar.<br>- No aparece si el nombre no existe. | arquitecto, programador, revisor | hecha (136/136; falta probar en Play) |
| T19b | Verificar guardado atómico en Android IL2CPP | - Programador prepara logs y APK dev.<br>- Usuario importa 20 historias seguidas y repite con force-stop durante el guardado.<br>- `List()` sin historias truncadas ni `.tmp` visibles.<br>- Si `File.Replace` falla: se anota y se aplica el fix del backlog (fallback solo con `PlatformNotSupported`/`NotImplemented`; no borrar `.tmp`). | programador, usuario (ejecuta) | preparada (APK + logs; 136/136); pendiente de la prueba del usuario |

## Fuera de alcance (va al backlog)
- Lectura real de historias (S05).
- Deshacer tras eliminar.
- Menores de S03 no incluidos: `fsync`, `\z` en id, `Detail` a 40 caracteres, `\r` sueltos.

## Riesgos
- T15: `List()` deserializa historias completas; con 200 historias puede superar 1 s. Mitigar con `HeaderDto`/contadores (cambia el DTO: versión).
- T15-T17 comparten la UI: la especificación única de ux-ui es camino crítico; sin ella no empiezan T15-T18.
- T17: conservar posición de scroll al volver exige decidir si la lista se destruye o se oculta (arquitecto).
- T18: hoy Cargar no guarda; T18 debe conectar parser + `StoryLibrary.Import` (alcance mayor que solo el diálogo; si supera 15 turnos, partir en encargos). Toca el flujo de Cargar (S02) y `StoryLibrary`; riesgo de regresión y de `.tmp` al cancelar.
- T19b depende del usuario y del dispositivo; si `File.Replace` falla, añade trabajo no estimado (límite: 1 fix, si no, `bloqueos.md`).
- Tope de 2 consultas programador -> arquitecto por tarea y 2 rondas de revisión.
