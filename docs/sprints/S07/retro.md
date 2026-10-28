# S07 — Retro

Inicio aprox.: 2026-10-26T09:48Z (cierre de S06). Informe: `node tools/token-report.mjs --since 2026-10-26T09:48:00Z`. Total local **5,81 M** (S06: 10,61 M; **−45 %**). Solo cuenta la sesión local del PC (48 turnos, Opus). La sesión coordinadora del proyecto no deja transcripción local y no está incluida. No hubo subagentes.

## Hecho
- T29: `ReadingSession` y `ReadingStats` con 5 tests EditMode (cuentan kanjis distintos por `KanjiKey`; la última respuesta es la que vale).
- T30: `ReaderScreen` crea una sesión por historia abierta. En `KanjiPopup`: verde = acierto, rojo = fallo, cerrar sin responder = nada (cuenta como acierto). Al cerrar (botón o Back de Android), si la historia tiene kanjis, se sustituye el lector por la nueva `StatsScreen`: %, total, acertados (verde), fallados (rojo), lista de fallados y botón Aceptar. Aceptar o Atrás vuelven a la lista. Sin kanjis, o si aún está cargando, se vuelve directamente a la lista. No se guarda nada. Textos en `UiStrings`; prefab `Stats` generado por `SceneBuilder`.
- T31: nuevo `ReaderStatsFlowTests` (PlayMode, 5 tests): verde/rojo/sin marcar → 3 / 1 fallado / 67 % y Aceptar vuelve a la lista; cerrar con el popup abierto; reabrir la historia empieza de cero; historia sin kanjis; cerrar mientras carga.
- Revisión (cierre durante la carga, Back de Android, popup abierto al cerrar, sesión al reabrir): sin bugs. Las tres situaciones están cubiertas por tests. Los arreglos menores van al backlog, sección "Arreglos para el final del proyecto (origen S07)".
- Tests finales: **EditMode 200/200, PlayMode 19/19**.
- APK: `Builds/S07/KanjiLearning-S07-dev.apk` (68 793 667 bytes, ~65,6 MB; Development; build de ~6 min). Pesa exactamente lo mismo que el de S06, pero el fichero es distinto y su `global-metadata.dat` incluye `StatsScreen` (el de S06 no). Durante la build sale otra vez el error conocido "Subtarget property is not available" (ya está en el backlog) y la build termina bien.

- **Prueba en móvil superada el 2026-10-26** con ese APK: responder en verde y en rojo, cerrar y comprobar el % y la lista de fallados. Valoración del usuario: "Funciona perfecto".

## Pendiente
- El roadmap todavía describe el diseño antiguo ("Mostrar más", "revisados"); queda anotado en el backlog para el diseñador.

## Lecciones
- `unity command ...` dentro de bucles `$(...)` a veces no devuelve salida. Es más fiable lanzar el comando suelto y leer el resultado.
- Construir las vistas a mano en PlayMode (como en `KanjiPopupDifficultTests`) permite probar todo el flujo sin la escena ni datos en disco.

## Tokens por agente

| Agente | Rol | Turnos | Total | Modelo |
|---|---|---|---|---|
| orquestador (sesión local) | compilar, T30, T31, retro, APK | 48 | 5.812.867 | Opus |
| **Total local** | | | **5.812.867** | |

## Frente a S06
- Sin subagentes: una sola sesión hizo T30 y T31 con el procedimiento de Unity ya conocido (recompile → `recompile_status` → tests). No hubo que relanzar nada por código viejo.
- Todo el gasto queda en Opus; para un sprint tan pequeño, Sonnet habría bastado.
