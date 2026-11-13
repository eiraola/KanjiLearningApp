# Retro S14 - Listas de palabras

Estado: hecho. Pendiente: prueba del usuario en el móvil.
Compilación limpia a la primera, sin correcciones. Escena regenerada sin errores. EditMode 268/268 (+25 nuevos), PlayMode 28/28.
APK: `Builds/S14/KanjiLearning-S14-dev.apk`, 52.054.940 B (~49,6 MiB; S13: 84 MB con 32 MB de huecos). Build limpia: se apartó el APK intermedio de Gradle antes de construir (medida 1 de S13 confirmada).
Detalle de la revisión: `docs/sprints/S14/revision-1.md` (en el PC), capturas `play-lista.png` y `play-tarjeta.png`.

## Hecho
- T57 Core: `WordEntry`/`WordList`, `WordTsvParser` (columnas por nombre de cabecera, sin cabecera = orden por defecto, error si se perdieron los tabuladores), `WordImport` (plan + merge), `WordListLibrary`, `JsonWordListRepository` (`wordlists/<id>.json`, escritura atómica).
- T58 Menú lateral «Listas», pantalla de listas con «+» y pantalla de una lista con botón ⋮.
- T59 Tarjeta de palabra de solo consulta con SentenceReading.
- T60 Ajustes → Añadir palabras: caja multilínea + Pegar (lee el portapapeles) + Añadir. Repetidas (D1, decidido por el usuario): popup con la lista e Ignorar / Actualizar / Cancelar.
- T61 Prueba en Play en el Editor: crear, pegar, tarjeta, Ignorar y Actualizar, persistencia: correcto.

## A vigilar
- En una pasada por script con los pasos seguidos, Ignorar dejó el popup vacío y la pantalla sin filas (sin escribir en disco). Paso a paso funcionó; probablemente por no dejar pasar frames. Confirmar en el móvil con toques rápidos.
- Sin probar: Cancelar en repetidas, cerrar la tarjeta con Atrás/fuera, pegar el ejemplo de 8 palabras, el teclado de Android con la caja multilínea.
- Error de consola conocido "Subtarget property" (paquete com.unity.pipeline) durante el build: no lo impide (ya en backlog).

## Medidas
1. Apartar `launcher-debug.apk` intermedio antes de cada build (hacerlo dentro de `AndroidBuild`).
2. Las pantallas nuevas no tienen tests PlayMode: añadir uno del flujo pegar → repetidas si aparece algún fallo.
3. Pendiente de S12/S13: excluir `Assets/Art/KanjiBook.png` del build si no se usa.

## Fuera de alcance (backlog)
- Las otras 3 opciones del menú de ajustes (sin definir), renombrar/borrar listas, editar/borrar palabras, estudiar una lista.
