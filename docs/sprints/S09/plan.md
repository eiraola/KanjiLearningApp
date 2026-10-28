Inicio: 2026-10-26T13:44:00Z | Cierre: 2026-10-26

# Sprint S09 — Kanjis complicados por historia

Estado: hecho

## Objetivo
Los complicados pasan a ser una lista por id de historia, con borrado en cascada, migración única de la lista global de S06, selector de historias y cuadrícula solo-kanji. Fuente: GDD 2.3 (cambio S09) y 2.5.

## Reglas de ahorro (obligatorias)
- Modelos: Sonnet o Haiku en todos los agentes; nada de Opus.
- Una subtarea por llamada del programador, tope de 15 turnos.
- Documentos de una página como máximo.
- Arquitecto solo donde hay decisión nueva: T39a (modelo de lista por historia) y T40a (migración). Sin arquitecto en T41 ni T42.
- No cargar skills grandes ni docs viejos; leer solo GDD 2.3/2.5 y los ficheros citados.

## Tareas
Cada subtarea = una invocación del programador (<= 3 scripts).

| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T39 | Lista por historia y cascada | | arquitecto, programador | hecho |
| T39a | Arquitectura (1 pág.): modelo y persistencia por id | - Define dónde se guarda (un fichero por historia o uno con mapa), versión del esquema y puerto.<br>- Define cómo `StoryLibrary.Delete` borra la lista. | arquitecto | hecho |
| T39b | `DifficultKanjiStore` por id de historia + tests | - Añadir/eliminar/`Contains`/listar por id; sin duplicar (Kanji+Reading) dentro de la historia; el mismo kanji puede estar en dos historias de forma independiente.<br>- Ida y vuelta JSON; fichero ausente o corrupto = lista vacía.<br>- Tests en verde. | programador | hecho |
| T39c | Borrado en cascada | - Eliminar una historia borra su lista de complicados.<br>- Confirmación de eliminar añade "También se borrarán sus N kanjis complicados." solo si N > 0.<br>- Tests en verde. | programador | hecho |
| T40 | Migración única | | arquitecto, programador | hecho |
| T40a | Arquitectura (1 pág.): algoritmo de migración | - Orden estable de historias; marcador de "migrado"; seguridad ante fallo (no se pierde la global si falla a medias). | arquitecto | hecho |
| T40b | `DifficultMigration` + tests EditMode | - Cada kanji va a la primera historia cuyo texto lo contiene.<br>- No asignables se descartan y se cuentan.<br>- No se repite; la lista global se elimina al terminar.<br>- < 2 s con 500 complicados y 50 historias. | programador | hecho |
| T40c | Ejecutar al arrancar + toast | - Se lanza una vez en `AppBootstrap`.<br>- Toast "N kanjis complicados no se pudieron asignar y se descartaron." solo si N > 0. | programador | hecho |
| T41 | "Añadir a complicados" ligado a la historia | - Desde el lector, añade a la lista de la historia abierta; "Ya en complicados" se evalúa por historia.<br>- Toast y persistencia como S06. | programador | hecho |
| T42 | Selector, cuadrícula y popup | | ux-ui, programador | hecho |
| T42a | Spec UX (1 pág.): selector y cuadrícula | - Fila con nombre y "N complicados"; cuadrícula solo-kanji; estados vacíos; navegación Atrás. | ux-ui | hecho |
| T42b | Pantalla selector de historias con complicados | - Solo historias con >= 1 complicado; orden como 2.3.<br>- Vacío: "Aún no has añadido kanjis".<br>- Atrás vuelve a Inicio. | programador | hecho |
| T42c | Cuadrícula solo-kanji + popup Eliminar | - Tocar historia abre cuadrícula; Atrás vuelve al selector.<br>- Tocar kanji abre popup con todos los datos y "Eliminar de la lista" (sin confirmación, toast).<br>- Tras eliminar, el popup cierra y la cuadrícula se actualiza; si queda vacía, vuelve al selector. | programador | hecho |
| T43 | Revisión | - Revisor ejecuta tests EditMode y PlayMode.<br>- Sin bloqueantes tras <= 2 rondas.<br>- Persistencia y migración comprobadas tras reinicio. | revisor, programador | hecho |

Nota: son 5 tareas (T39-T43); T39/T40/T42 se dividen en subtareas de una invocación cada una.

## Prueba del usuario en móvil
1. Con datos de S06 instalados: abrir la app; migración con toast si hay descartados.
2. Añadir el mismo kanji en dos historias: aparece en ambas listas.
3. Kanjis complicados: selector solo con historias que tengan complicados, con su contador.
4. Tocar historia: cuadrícula solo-kanji; tocar kanji: popup completo; Eliminar: desaparece y no afecta a la otra historia.
5. Eliminar la historia desde Leer: aviso de N complicados; tras confirmar, desaparece del selector.
6. Reabrir la app: todo persiste y la migración no se repite.

## Fuera de alcance (va al backlog)
- Mover/copiar complicados entre historias; ordenar o filtrar la cuadrícula; exportar.
- Estadísticas de complicados.

## Riesgos
- Migración irreversible: debe ser a prueba de fallos (conservar la global hasta terminar) y probarse con datos reales de S06 antes de cerrar.
- Depende de S08: el renombrado no cambia el id (la lista va por id).
- Coste: reutilizar `KanjiPopup`, `Toast` y la lista de S06; no duplicar componentes.
- Orden de migración "más reciente primero" es [SUPOSICIÓN]; el arquitecto puede fijar otro estable.
- Máx. 2 consultas del programador al arquitecto por tarea; máx. 2 rondas de revisión; máx. 2 idas y vueltas arquitecto-diseñador.
