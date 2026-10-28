Inicio: 2026-10-26T08:19:34Z

# Sprint S06 — Kanjis complicados

## Objetivo
Añadir, listar y eliminar kanjis complicados con persistencia entre sesiones. Alcance aprobado por el usuario (roadmap S06). Decisión P4: eliminar SIN confirmación, con toast breve.

## Tareas
Cada subtarea = una invocación del programador (<= 3 scripts).

| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T25 | Modelo y persistencia de `DifficultKanjiList` | | arquitecto, programador, revisor | hecha |
| T25a | Modelo `DifficultKanjiList` + tests EditMode | - Guarda copia completa del kanji (no referencia).<br>- Añadir un kanji ya presente no duplica (misma KanjiKey) y lo indica.<br>- Eliminar, `Contains` y listado funcionan.<br>- Tests EditMode en verde. | arquitecto, programador | hecha |
| T25b | Repositorio JSON persistente + tests | - Guardar/cargar JSON en `persistentDataPath`.<br>- Ida y vuelta conserva todos los campos y el orden.<br>- Fichero ausente o corrupto devuelve lista vacía sin excepción.<br>- Tests en verde. | programador | hecha |
| T26 | Botón "Añadir a complicados" + Toast | | ux-ui, programador | hecha |
| T26a | Spec UX del botón ("Añadir a complicados" / "Ya en complicados") y Toast | - Spec en `docs/ui/` con estados, textos, posición, objetivo táctil 48 dp y duración del toast. | ux-ui | hecha |
| T26b | Implementación botón + Toast | - Aparece en la vista de detalle del kanji en la lectura.<br>- Pulsar añade, persiste y muestra toast.<br>- Si ya está, muestra "Ya en complicados" desactivado, sin duplicar.<br>- Toast reutilizable. | programador | hecha |
| T27 | Pantalla Kanjis complicados + popup Difficult | | ux-ui, programador | hecha |
| T27a | Spec UX de la pantalla y del popup modo Difficult con "Eliminar de la lista" | - Spec en `docs/ui/`: lista, estado vacío, navegación y atrás, popup, botón eliminar, toast. | ux-ui | hecha |
| T27b | Pantalla lista de kanjis complicados | - Accesible desde el menú principal.<br>- Muestra los kanjis guardados; estado vacío si no hay.<br>- Pulsar un kanji abre el popup.<br>- Atrás vuelve al menú. | programador | hecha |
| T27c | Popup modo Difficult + eliminar | - Popup muestra los datos del kanji guardado.<br>- "Eliminar de la lista" quita el kanji sin confirmación, persiste, muestra toast breve y refresca la lista. | programador | hecha |
| T28 | Revisión | - Revisor ejecuta tests EditMode y PlayMode.<br>- Sin bloqueantes tras <= 2 rondas.<br>- Persistencia comprobada tras reinicio. | revisor, programador | hecha |

## Prueba del usuario en móvil
1. Abrir una historia, pulsar un kanji y "Añadir a complicados"; ver toast.
2. Pulsar el mismo kanji otra vez: sale "Ya en complicados".
3. Cerrar del todo la app y reabrirla.
4. Abrir Kanjis complicados: el kanji sigue en la lista.
5. Abrirlo y pulsar "Eliminar de la lista": desaparece al instante con toast.
6. Reabrir la app: sigue eliminado.

## Fuera de alcance (va al backlog)
- Confirmación al eliminar (descartada por P4).
- Estadísticas (S07), ordenar o filtrar la lista, exportar o importar.

## Riesgos
- Esquema JSON difícil de ampliar: el arquitecto debe incluir un campo de versión.
- Toast y popup deben reutilizar los componentes de S05 (vistas 1 y 2) sin duplicar código.
- Máx. 2 consultas del programador al arquitecto por tarea; máx. 2 rondas de revisión.
