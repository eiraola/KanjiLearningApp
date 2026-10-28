Inicio: 2026-10-26T12:44:00Z

# Sprint S08 — Nombre y estadísticas al cargar historias

Estado: HECHO (pendiente de prueba en móvil)

## Objetivo
Tras elegir un .txt se abre un popup de carga con input de nombre y estadísticas. Se puede renombrar una historia desde su popup (Editar). Nombre duplicado: Aceptar desactivado y mensaje rojo; se elimina el diálogo de conflicto de S04. Fuente: GDD 2.2 y 2.3.

## Reglas de ahorro (obligatorias)
- Modelos: Sonnet o Haiku en todos los agentes; nada de Opus.
- Una subtarea por llamada del programador, tope de 15 turnos.
- Documentos de una página como máximo (arquitectura, spec UX, revisión).
- Arquitecto solo donde hay decisión nueva (T35: validación y estadísticas; T37 si cambia el repositorio). Sin arquitecto en T36 ni T38.
- No cargar skills grandes ni docs viejos; leer solo GDD 2.2/2.3 y los ficheros citados.

## Tareas
Cada subtarea = una invocación del programador (<= 3 scripts).

| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T35 | Estadísticas de carga y validación de nombre | | arquitecto, programador | hecha |
| T35a | Arquitectura (1 pág.): definición de estadísticas y de `NameValidator` | - Define caracteres (texto visible sin `{...}`→Kanji, sin saltos ni espacios).<br>- Define comparación de nombres (recortado, sin distinguir mayúsculas). | arquitecto | hecha |
| T35b | `LoadStats` + `StoryNameValidator` (Core) + tests EditMode | - Devuelve nº kanjis, caracteres, avisos.<br>- Validador: vacío→nombre de archivo; duplicado detectado; en renombrar, el propio nombre no es duplicado.<br>- Tests en verde. | programador | hecha |
| T36 | Popup de carga | | ux-ui, programador | hecha |
| T36a | Spec UX (1 pág.): popup de carga y mensaje rojo | - Textos, placeholder, estados de Aceptar, color del error, no cierra al tocar fuera. | ux-ui | hecha |
| T36b | Popup de carga + integración en pantalla Cargar | - Tras analizar se abre con placeholder = nombre de archivo sin extensión.<br>- Muestra kanjis, caracteres y avisos (solo si > 0).<br>- Aceptar guarda con el nombre resultante; Cancelar/Atrás no guarda ni muestra mensaje.<br>- Duplicado: Aceptar desactivado + "Ya existe una historia llamada 'X'. Elige otro nombre." en rojo, < 0,2 s al teclear.<br>- Errores fatales de 5.3 no abren el popup. | programador | hecha |
| T36c | Retirar el diálogo de conflicto de S04 | - No queda código ni texto "Nombre repetido" / Reemplazar / Conservar ambas.<br>- Tests de S04 actualizados y en verde. | programador | hecha |
| T37 | Editar nombre | | ux-ui, programador | hecha |
| T37a | Spec UX (1 pág.): botón Editar y popup de edición | - Botón en el popup de historia; input precargado; Aceptar/Cancelar; mismo mensaje rojo. | ux-ui | hecha |
| T37b | Renombrar en `StoryLibrary` + popup Editar | - Aceptar actualiza lista y popup de historia en < 1 s, mismo id, mismo orden y contenido.<br>- Vacío o duplicado con OTRA historia: Aceptar desactivado; el nombre sin cambios no es duplicado.<br>- Persiste tras reabrir. | programador | hecha |
| T38 | Revisión | - Revisor ejecuta tests EditMode y PlayMode.<br>- Sin bloqueantes tras <= 2 rondas. | revisor, programador | hecha |

## Prueba del usuario en móvil
1. Cargar un .txt: aparece el popup con nombre por defecto y estadísticas.
2. Escribir un nombre propio y Aceptar: la historia aparece con ese nombre.
3. Cargar otro archivo con un nombre ya existente: mensaje rojo y Aceptar desactivado; cambiarlo lo reactiva.
4. Cancelar: no se guarda nada.
5. Editar una historia: cambiar nombre, duplicado bloqueado, Cancelar no cambia nada.
6. Reabrir la app: nombres conservados.

## Fuera de alcance (va al backlog)
- Editar contenido de la historia; reordenar; deshacer borrado.
- Complicados por historia (S09).

## Riesgos
- Decisión del usuario: "palabras" queda fuera de las estadísticas (P6 cerrada).
- Renombrar no debe cambiar el id (los complicados de S09 se asocian al id): comprobar en T37b.
- Retirar el diálogo de S04 puede romper tests o popups compartidos (PopupService): revisar usos antes de borrar.
- Máx. 2 consultas del programador al arquitecto por tarea; máx. 2 rondas de revisión; máx. 2 idas y vueltas arquitecto-diseñador.
