# Retro S15 - Estudiar una lista de palabras

Estado: hecho. Pendiente: prueba del usuario en el móvil.
EditMode 274/274 (+6), PlayMode 28/28. Compilación limpia a la primera en las dos rondas.
APK: `Builds/S15/KanjiLearning-S15-dev.apk`, 52.071.862 B (~49,7 MiB), build limpia (81 KB de espacio muerto).
Detalle: `docs/sprints/S15/revision-1.md` (rondas 1 y 2) y capturas `play-*.png` (en el PC).

## Hecho
- T62 Core: `WordEntry` con `Progress` (0..5) y `Difficult`; `WordListLibrary.SetDifficult` / `RecordStudy`; `WordStudySession` (orden aleatorio, guarda cada respuesta). JSON compatible con las listas de S14. «Actualizar» al pegar conserva progreso y marca.
- T63 Tarjeta de palabra con modo estudio (Resolver, verde/rojo, deslizar) y botón «Añadir a complicadas» / «Ya en complicadas» en los dos modos.
- T64 Menú ⋮ con «Estudiar»; filas con disco rojo de complicada y círculo de progreso; filtro Todas / Complicadas.
- Ronda 2: la marca y los puntos del ⋮ usaban un aro de 2 px casi invisible; ahora son discos rellenos.

## Prueba en el Editor (correcta)
Marcar/desmarcar, filtro, estudio completo con resultados (67 %), Atrás a mitad (con 0 respuestas vuelve a la lista), tocar fuera no corta el estudio, persistencia, lista de S14 abierta con el formato nuevo.

## Sin probar (móvil)
- Deslizar con el dedo (con el puntero simulado no avanzó; con eventos directos a `CardSwipe` sí).
- Botón Atrás físico (se probó la función que llama).

## Pendiente de decidir (backlog)
- El círculo de progreso (igual que en las historias, S11) es un arco fino poco visible con 1/5.
- La pantalla de resultados dice «Kanjis fallados» también al estudiar palabras.
- Segundo botón nuevo y la opción restante del menú ⋮: sin definir.

## Medidas
1. Revisar en el editor los sprites del tema antes de usarlos (CircleSprite es un aro, no un disco).
2. Seguir apartando `launcher-debug.apk` antes de cada build (moverlo a `AndroidBuild`).
