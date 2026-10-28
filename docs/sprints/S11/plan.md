Inicio: 2026-10-26T15:40:00Z

# Sprint S11 — Estudiar kanjis complicados

Estado: hecho (pendiente de prueba en móvil)

## Objetivo
En la lista de complicados de una historia, botón "Estudiar kanjis": popups en orden aleatorio con kanji + frase, botón único "Resolver", luego toda la información y verde/rojo; al final, % de aciertos. Cada fila de la lista muestra un círculo de progreso (0 a 5 aciertos); lleno y verde = sugerencia de eliminar el kanji.

## Decisiones
- Progreso guardado junto a cada kanji en el fichero de su historia (`difficult/<id>.json`, campo `successes`, esquema v1 compatible: falta = 0). Eliminar el kanji o la historia borra su progreso.
- Se guarda en cada respuesta, no al final.
- Fallo (rojo): el círculo no cambia (P9, respondida por el usuario).
- Total del % = kanjis respondidos. Atrás de Android durante el estudio termina la sesión y enseña el % de lo respondido (si no hay ninguno, vuelve a la lista).
- Pantalla de resultado: se reutiliza `StatsScreen` (%, total, acertados, fallados y lista de fallados).

## Reglas de ahorro
- Sin subagentes: la sesión principal implementa (S06 y S07 fueron los sprints más baratos). Documentos de una página.
- Leer solo los ficheros citados; tests al final de cada tarea con UI.

## Tareas
| Id | Tarea | Criterios de aceptación | Estado |
|---|---|---|---|
| T44 | Progreso por kanji en Core + JSON | - `DifficultKanjiList.ProgressOf` y `RecordStudy(entry, knew)` (0..5; el fallo no cambia); se guarda al momento y revierte si falla.<br>- Repositorio lee/escribe `successes`; ficheros antiguos sin campo = 0.<br>- Tests EditMode. | hecho |
| T45 | `DifficultStudySession` (orden aleatorio, respuestas, `ReadingStats`) + tests | - Baraja todos los kanjis de la lista una vez; Current/Record/IsFinished.<br>- Stats: total = respondidos, fallados = rojos. | hecho |
| T46 | Popup en modo estudio | - Muestra kanji + frase y solo "Resolver"; al pulsar, toda la info + verde/rojo.<br>- Verde/rojo registran y abren el siguiente; al acabar, `StatsScreen`. | hecho |
| T47 | Botón "Estudiar kanjis" + círculo en cada fila | - Botón en la lista de la historia (oculto si está vacía).<br>- Círculo radial a la derecha: relleno = aciertos/5; verde al llegar a 5.<br>- Se refresca al volver del estudio. | hecho |
| T48 | Revisión, tests, APK dev, retro | - EditMode y PlayMode en verde; `Builds/S11/KanjiLearning-S11-dev.apk`. | hecho |

Prueba del usuario: estudiar una lista, comprobar orden aleatorio, Resolver, verde/rojo, %, círculos (subir a 5 y ver verde; fallar y ver que no cambia).
