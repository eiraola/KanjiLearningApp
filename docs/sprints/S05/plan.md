Inicio: 2026-10-22T16:42:41Z

# Sprint S05 — Lector con kanjis pulsables + revisión combinada S04/S05

## Objetivo
Leer una historia guardada: texto largo desplazable, kanjis azules pulsables y popup (vista 1 y 2), con rendimiento aceptable en móvil. Cerrar la revisión y la prueba en móvil pendientes de S04.

## Reglas de ejecución (medidas de la retro S03, vigentes desde S04)
- ux-ui escribe **una sola especificación** para todo el sprint (`docs/ui/`); cada programador recibe solo la sección de su pantalla.
- Tareas de ≤ 3 scripts; **un programador nuevo por tarea**.
- Sesión partida: una para `/sprint-construir`, otra para `/sprint-revisar`.
- Si una tarea supera 15 turnos del programador, se parte.

## Supuestos (aprobados por el usuario el 2026-10-22, junto con el plan)
- S1: sin estadística ni botón "Añadir a complicados" en S05; Cerrar vuelve a la lista y "Mostrar más" aún no registra revelados (S06/S07).
- S2: el scroll no persiste entre sesiones.
- S3: en la vista 2 los campos vacíos se ocultan.
- S4: la revisión de S04 va dentro de la de S05 con sus propios ficheros (`S04/revision-1.md`, `S04/retro.md`); S05 no se cierra hasta la prueba del usuario.

## Orden de ejecución
T20 -> T21 -> T22 -> T23 -> T24 (necesita al usuario con el móvil y los requisitos R-S05a/b/c).
ux-ui: una especificación única antes de T21.

## Tareas
| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T20 | `StoryRichTextBuilder` (Core, EditMode) | - Respeta saltos de línea y texto fuera de llaves.<br>- Cada bloque válido sale como el valor de Kanji en azul con enlace al índice de su bloque.<br>- `<`, `>` y marcas rich text del .txt se muestran literales.<br>- Troceo por párrafos en trozos ≤ ~8 000 car. (parametrizable) sin cortar enlace; un párrafo solo se corta si supera el máximo.<br>- Fixture S03 (50 000 car.): conserva 100 % del texto, un enlace por bloque (250) y 200 kanjis distintos.<br>- Historia sin kanjis: sin enlaces.<br>- Tests nuevos verdes y 136 previos intactos. | arquitecto, programador | hecha (164/164 EditMode, 1/1 PlayMode) |
| T21 | `ReaderScreen` real | - Leer abre el lector desplazable, arranca arriba.<br>- Relee del disco; si falla: "No se pudo abrir la historia." y vuelve a la lista.<br>- 50 000 car. abre en < 2 s en móvil gama media; scroll sin saltos.<br>- Sin tofu.<br>- Atrás/Cerrar historia vuelven a la lista en su misma posición de scroll.<br>- Historia sin kanjis abre sin error. | ux-ui, arquitecto, programador | hecho (Editor: apertura 1.0 s fría, 0.3 s caliente, 7 trozos) |
| T22 | Pulsar kanji + popup vista 1 | - Tocar un enlace abre popup con kanji grande y Sentence.<br>- Cerrar/fuera/Atrás cierran solo el popup; el scroll queda igual.<br>- Abre en < 1 s; botones ≥ 48 dp.<br>- Texto no azul: no hace nada; arrastrar sobre un kanji no abre popup.<br>- Cada aparición abre su bloque aunque el kanji se repita.<br>- Vista 1 sin lectura ni traducción. | ux-ui, programador | hecha |
| T23 | Vista 2 "Mostrar más" | - Añade Reading, Meaning y SentenceMeaning en el mismo popup.<br>- Campos vacíos se ocultan.<br>- Solo botón Cerrar (sin "Añadir a complicados").<br>- Cerrar/Atrás no mueve el scroll.<br>- Reabrir empieza en vista 1.<br>- Texto largo cabe o se desplaza dentro del popup. | ux-ui, programador | hecha |
| T24 | Revisión combinada S04+S05 y prueba en móvil | - Un solo `/sprint-revisar 05` que escribe además `S04/revision-1.md` y `S04/retro.md` por separado; máx. 2 rondas para el conjunto.<br>- Una sesión del usuario con APK dev: T19b (20 importaciones + force-stop; sin truncados ni `.tmp` listados), rendimiento con fixture e historia real larga (apertura < 2 s, scroll, toques, pico de memoria) y pruebas en Play pendientes de S04 (T17, T18).<br>- Si T19b exige el fix del backlog: 1 fix; si no, `bloqueos.md`.<br>- Salida: informe sin bloqueantes y 3 medidas de rendimiento en la retro. | revisor, programador (fixes), usuario (ejecuta) | hecha (2026-10-23, APK dev): T19b, toques/popup vistas 1 y 2, diálogo de duplicado y Atrás en su posición correctos. Medidas en móvil no tomadas (rendimiento, memoria); historia real no carga entera (sin investigar); ver backlog |

## Requisitos del usuario (antes de T24)
- [R-S05a] Historia real larga .txt (ideal ≥ 30 000 car., con bloques `{...}` reales) en el móvil.
- [R-S05b] Móvil por USB con `adb` y tiempo para 20 importaciones + force-stop.
- [R-S05c] Modelo del móvil.

## Fuera de alcance (va al backlog)
- Estadística y registro de revelados (S07); "Añadir a complicados" (S06).
- Scroll persistente entre sesiones.
- Menores de S03 aún abiertos (`fsync`, `\z`, `Detail` a 40 car., `\r` sueltos).

## Riesgos
- Rendimiento de TMP con trozos largos: T20 parametriza el tamaño.
- Toque vs arrastre en ScrollRect (T22).
- T24 es cuello de botella: si el usuario no está disponible, lo pendiente va a `bloqueos.md`.
- Revisión conjunta S04+S05 amplia: acotar al código cambiado; máx. 2 rondas.
- Tope de 2 consultas programador -> arquitecto por tarea y 2 idas y vueltas arquitecto <-> diseñador.
