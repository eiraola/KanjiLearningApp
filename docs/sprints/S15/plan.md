Inicio: 2026-11-05T09:30:00Z

# Sprint S15 — Estudiar una lista de palabras

Estado: hecho (pendiente de prueba en el móvil)

## Objetivo
Nueva opción **Estudiar** en el menú ⋮ de una lista. Muestra las tarjetas de la lista en orden aleatorio, como el estudio de complicados de las historias (S11): primero palabra y frase, «Resolver» enseña el resto, y el usuario responde verde «Lo sabía» / rojo «No lo sabía» (botones o deslizando, S13). Desde la tarjeta se puede marcar la palabra como complicada. Al terminar (o con Atrás) sale la pantalla de resultados.

## Decisiones
- P11 (respondida) Complicadas: sublista propia de cada lista, como en las historias (marca por palabra, no lista global). Se ve en la fila de la palabra; botón «Añadir a complicadas» / «Ya en complicadas» en la tarjeta (en estudio y en consulta; volver a tocarlo la quita). Filtro «Todas (N)» / «Complicadas (M)» bajo la barra superior.
- P12 (respondida, se acepta lo recomendado) Se estudian todas las palabras de la lista; círculo de progreso 0..5 por palabra (verde suma 1, rojo no cambia, como P9). % y lista de falladas al final, en la pantalla de resultados existente.
- Segundo botón del menú: sin definir, no se inventa.
- Persistencia: progreso y marca de complicada dentro del JSON de la lista (campos nuevos; los ficheros de S14 se leen con 0 / no complicada).

## Tareas
| Id | Tarea | Criterios de aceptación | Estado |
|---|---|---|---|
| T62 | Core: progreso, complicadas y sesión de estudio | - `WordEntry` con `Progress` (0..5) y `Difficult`; JSON compatible con S14.<br>- `WordStudySession` (orden aleatorio, Record verde/rojo guarda progreso al momento, stats).<br>- `WordListLibrary.SetDifficult`.<br>- Tests EditMode. | hecho |
| T63 | Tarjeta en modo estudio | - `WordCardPopup`: modo estudio con Resolver, verde/rojo y deslizar (reutiliza `CardSwipe`); botón complicadas en los dos modos; tocar fuera no corta el estudio. | hecho |
| T64 | Menú y pantalla de lista | - «Estudiar» en el menú ⋮ (desactivado si la lista está vacía).<br>- Filas con círculo de progreso y marca de complicada; filtro «Solo complicadas».<br>- Resultados al terminar o con Atrás (si se respondió alguna). | hecho |
| T65 | Escena, tests, APK y retro | - EditMode y PlayMode en verde; `Builds/S15/KanjiLearning-S15-dev.apk` con build limpia. | hecho |

## Fuera de alcance
- Segundo botón nuevo y la tercera opción restante del menú (sin definir); estudiar solo complicadas.

Prueba del usuario: estudiar una lista, marcar complicadas, ver % y círculos, filtrar complicadas, reabrir la app.
