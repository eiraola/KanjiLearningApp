# S11 — Implementación

## Qué cambia
- **Core**: `DifficultKanjiList` guarda el progreso de cada kanji (`ProgressOf`, `RecordStudy`: 0..5 aciertos; un fallo no lo cambia). `IDifficultKanjiRepository` añade `LoadProgress` y `Save` con progreso (métodos por defecto). Nuevo `DifficultStudySession`: baraja la lista una vez, registra las respuestas y da `ReadingStats`.
- **Infraestructura**: `JsonDifficultKanjiRepository` lee y escribe `successes`. Los ficheros antiguos sin ese campo cuentan como 0.
- **UI**: `KanjiPopup` tiene modo estudio (kanji + frase, botón `BtnReveal` "Resolver", después toda la info + verde/rojo). `DifficultGridScreen` tiene el botón `BtnStudy` "Estudiar kanjis", oculto si la lista está vacía, y al acabar abre `StatsScreen`. `DifficultKanjiCell` tiene un círculo radial que se pone verde al llegar a 5. Hay textos nuevos en `UiStrings`.
- **Editor**: `SceneBuilder` genera los elementos nuevos. Escena y prefabs regenerados con *Kanji/Build S01 Scene*.

## Tests
- EditMode: **243/243** superados (incluye los nuevos `DifficultStudyTests` y `JsonDifficultKanjiRepositoryTests`).
- PlayMode: **23/23** superados (incluye `DifficultGridFlowTests`).

## Correcciones
Ninguna: compiló y los tests pasaron sin tocar nada.

## APK
`Builds/S11/KanjiLearning-S11-dev.apk`, 68,8 MB (65,6 MiB).
Durante el build, la consola muestra `ArgumentException: Subtarget property is not available for the platform (Android)`. Viene del hook de pre-build del paquete `com.unity.pipeline`. Salió también en builds anteriores y no impide generar el APK.
