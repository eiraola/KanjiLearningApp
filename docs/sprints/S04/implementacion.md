
## T19a — Parser: `Sentence.` con `:` en el valor
- `StoryParser.cs`: `ChooseSeparator` (el `.` anterior al primer `:` solo gana si el nombre previo es campo conocido) e `IsKnownField` (lista única, reutilizada en `Assign`).
- Tests nuevos en `StoryParserErrorTests.cs`: `SentenceWithDot_KeepsColonInValue`, `SentenceWithColon_KeepsTimeInValue`, `ColonBeforeDot_UsesColon`, `UnknownNameBeforeDot_StillUsesColon`.

## T15a — Core de la lista
**Ficheros:** `Core/Model/StoryModel.cs` (`Story.ImportedAtUtc`, `With(id, name, importedAtUtc)`), `Core/Library/IStoryRepository.cs` (`StoryInfo(id, name, entryCount, importedAtUtc)`, nuevo `StoryListing`, `List()` devuelve `StoryListing`), `Core/Library/StoryLibrary.cs` (ctor con `utcNow`, `Revision`, orden fecha desc/nombre/id, sellado en los 3 caminos).
**Tests:** `StoryLibraryTests` (+6 nuevos, ctor nuevo), fake `InMemoryStoryRepository` (`UnreadableCount`), `StoryParserTests` (`With`), `JsonStoryRepositoryTests` (`.Items`, quitado `DistinctKanjiCount`).
**Decisiones locales:** `Story.DistinctKanjiCount` se mantiene (lo usa el informe del parser); solo desaparece de `StoryInfo`. Ajuste mínimo para compilar en `JsonStoryRepository.List()` (fuera de T15a): devuelve `StoryListing` con `unreadable` contado y `ImportedAtUtc` de la historia cargada; sigue ordenando por nombre y leyendo ficheros completos. T15b debe sustituirlo (HeaderDto, v2, sin ordenar).
**Probar:** recompilar y `unity command test run -- --mode EditMode` -> 125/125.

## T15b — Repositorio v2 + HeaderDto
**Ficheros:** `Infrastructure/Storage/JsonStoryRepository.cs` (`CurrentVersion = 2`; `StoryDto` con `importedAtUnixMs`/`entryCount` antes de las listas; `HeaderDto`; `List()` ya no ordena, lee solo cabecera con validación version 1..2 / id == fichero / entryCount >= 0; v1 cae a `Decode` completo y fecha 0; `Migrate` v1->v2; `Save` sella fecha y contador; `Load` devuelve `ImportedAtUtc`). Tests: `JsonStoryRepositoryTests` (+7 de T15b, `List_StableOrder` ya no asume orden) y `Kanji.Infrastructure.Tests.asmdef` (+ref a `Kanji.Core.Tests` para `StoryFixtureGenerator`).
**Decisiones locales:** el fichero se sigue leyendo entero (`ReadAllBytes`) pero sin crear objetos de modelo. El test escribe el tiempo en `%TEMP%\kanji_list200_ms.txt` (el log de tests no se captura por CLI). Logs `[Kanji][IO]` de T19b: no tocan este script, se dejan para T19b.
**Medido:** `List()` con 200 historias de ~5 000 caracteres = 159 ms en PC (presupuesto 300 ms). Móvil sin medir (T19b).
**Probar:** `unity command test run -- --mode EditMode` -> 132/132.

## T15c — UI de la lista
**Ficheros:** `UI/StoryListScreen.cs` (ScreenView; `Init(nav, library)`; `OnShow` re-renderiza solo si cambia `Revision`; pool de filas activar/desactivar; aviso de dañados; estado vacío; `try/catch` en `Render` -> `LibraryLoadFailed` sin actualizar la revisión; conserva scroll en px acotado), `UI/StoryListRow.cs` (`Bind(info, onClick)`), `UI/UiStrings.cs` (`EmptyLibrary`, `LibraryLoadFailed`, `KanjiCount`, `UnreadableStories`), `App/AppBootstrap.cs` (`DiskFileSystem` + `JsonStoryRepository(persistentDataPath)` + `StoryLibrary`; `case StoryListScreen`), `Editor/SceneBuilder.cs` (`BuildStoryListPrefab`, `BuildStoryRowPrefab`). Regenerados con `Kanji/Build S01 Scene`: `Prefabs/Screens/StoryList.prefab`, nuevo `Prefabs/Widgets/StoryRow.prefab` y `Scenes/Main.unity`.
**Decisiones locales:** el toque de fila es un hook vacío (`OnRowClicked`) hasta T16b. Toast no implementado (T16a). `StoryList` ya no usa `EmptyScreen`.
**Probar:** Play, Inicio -> Leer historias. Verificado por script en Editor con 30 historias + 1 JSON roto (30 filas, orden por fecha desc, "1 kanji"/"N kanjis", aviso "1 historia no se pudo leer") y con 0 (mensaje vacío). Datos de prueba borrados. EditMode: 132/132. No hay captura visual: `screenshot` de Game view no captura el canvas Overlay.

## T16a — Sistema de popups + ToastView
**Ficheros:** `UI/PopupView.cs` (base abstracta: `CloseOnOutsideTap`, `Cancel()`, `Init(PopupService)`, fundido 0,15 s en tiempo no escalado), `UI/PopupService.cs` (pila propia en `List`, blocker/scrim único bajo el popup superior, `Open/Close/HandleBack`, toque en blocker -> `Cancel()` solo si `CloseOnOutsideTap`), `UI/ToastView.cs` (`Show(msg)`; 0,15 s in / 3 s / 0,3 s out; uno nuevo sustituye al anterior; no bloquea toques). `UI/BackButtonRouter.cs` (`Init(navigator, popups, quit)`; popups primero), `App/AppBootstrap.cs` (campos `_popupBlocker`, `_toast`; crea `PopupService`), `Editor/SceneBuilder.cs` (`PopupBlocker` y `ToastArea/Toast` en `PopupLayer`; `Main.unity` regenerada).
**Decisiones locales:** no se reutiliza `NavigationStack<T>` (no deja sacar el elemento raíz); `PopupService` usa su propia lista. El blocker hace de scrim (#000 55 %), por eso los prefabs de popup (T16b) solo llevan `Panel`, no `Scrim`. El toast va dentro de `ToastArea` con `SafeAreaFitter` y `PopupService` mantiene esa área siempre encima. Los popups de T16b deben ser hijos de `PopupLayer`, tener `CanvasGroup` y llamar a `Init(popups)` desde `AppBootstrap`. Sin tests nuevos (la UI no tiene asmdef de tests).
**Probar:** EditMode 132/132. Verificada por script la referencia `_toast`/`_popupBlocker` en la escena regenerada; sin popups reales hasta T16b.

## T16b — Popup de historia + confirmación de eliminar
**Ficheros:** `UI/StoryActionsPopup.cs` (`Show(info, onResult)`, `Close()`; Leer/Cerrar/Atrás/fuera cierran y devuelven `Read`/`Dismiss`; Eliminar NO cierra), `UI/ConfirmPopup.cs` (genérico, `CloseOnOutsideTap=false`, foco en Cancelar + Outline 6 px, resultado una sola vez), `UI/StoryListScreen.cs` (`Init(nav, library, actionsPopup, confirmPopup, toast)`; flujo fila -> popup -> confirmar -> `library.Delete` -> `Render()` + toast), `UI/UiStrings.cs` (textos), `App/AppBootstrap.cs` (campos `_storyPopup`, `_confirmPopup`, `Init(popups)`), `Editor/SceneBuilder.cs` (`BuildStoryPopup`/`BuildConfirmPopup` como hijos de `PopupLayer`; sin Scrim propio, el blocker lo es). `Main.unity` regenerada. Tests: `Delete_ThenList_DoesNotContain`, `Delete_PersistsAcrossNewRepositoryInstance` en `JsonStoryRepositoryTests`.
**Decisiones locales:** los popups son objetos de escena (no prefabs aparte). `Read` queda como hook en `StoryListScreen.OnRowClicked` hasta T17. `NotFound` al borrar se trata como éxito. Si falla el borrado: cierra popup, toast `No se pudo eliminar la historia.`.
**Probar:** Play, Leer historias, tocar fila. Verificado por script con 2 historias temporales (borradas): foco en Cancelar; Cancelar deja abierto el popup de historia; Eliminar cierra ambos, la fila desaparece y toast "Historia 'TEMP_B' eliminada".

## T17 — Leer (placeholder) y conservación del scroll
**Ficheros:** `UI/ReaderScreen.cs` (nuevo; `Init(nav)`, `OnShow(args)` con `StoryInfo` -> nombre + `KanjiCount`; otro tipo -> texto vacío + `LogError`; Atrás = `Pop`), `UI/StoryListScreen.cs` (`StoryAction.Read` -> `Push(ScreenId.Reader, info)`), `UI/UiStrings.cs` (`ReaderComingSoon`), `App/AppBootstrap.cs` (+`case ReaderScreen`), `Editor/SceneBuilder.cs` (`BuildReaderPrefab` -> `Prefabs/Screens/StoryReader.prefab`, añadido a `_screenPrefabs`; `Main.unity` regenerada con el menú).
**Decisiones locales:** la lista se oculta, no se destruye; volver de Leer no cambia `Revision`, así que no se re-renderiza y el scroll queda intacto. Si se re-renderiza (cargar/eliminar) ya se conservaba el offset acotado (T15c). Sin tests nuevos (según arquitectura). Sin persistencia entre sesiones.
**Probar:** Play, Leer historias, hacer scroll, tocar fila -> Leer: tarjeta con nombre, "N kanjis" y "La lectura llegará pronto."; Atrás vuelve en la misma posición. No verificado visualmente en Play (solo compilación, escena regenerada y tests). EditMode: 134/134.

## T18 — Cargar: análisis, guardado y diálogo de nombre duplicado
**Ficheros:** `UI/ChoicePopup.cs` (nuevo; `Show(title,msg,first,second,cancel,onResult)`, `ChoiceResult`, `CloseOnOutsideTap=false`, foco+Outline en Cancelar, resultado una sola vez), `UI/LoadStoriesScreen.cs` (`Init(nav, picker, decoder, parser, library, choice, toast)`; Decode -> Parse -> `Import(Ask)`; Conflict -> ChoicePopup -> `Import(Replace/KeepBoth)`; Cancelar/Atrás no escribe; `OnHide` descarta lo pendiente; Failed -> `UiStrings.SaveFailed`), `UI/UiStrings.cs`, `App/AppBootstrap.cs` (`_choicePopup`, `new StoryParser()`), `Editor/SceneBuilder.cs` (`BuildChoicePopup` "DuplicateNamePopup"; `Main.unity` regenerada). Tests: `ImportConflict_Cancel_LeavesNoTmpAndSameFiles`, `ImportConflict_Replace_LeavesOneFileNoTmp` (carpeta temporal, `JsonStoryRepositoryTests`); los de `StoryLibraryTests` ya existían.
**Decisiones locales:** el mensaje "Historia 'X' cargada: N kanjis" es toast (nombre final); si hay avisos del parser se añade una segunda línea "N avisos al analizar el archivo". La previsualización de S02 se mantiene. Botón Cargar bloqueado mientras el diálogo está abierto. Botón Atrás de la pantalla no se bloquea (el blocker lo cubre).
**Probar:** Play, Cargar historias, elegir el mismo archivo dos veces -> diálogo; Cancelar no cambia nada. No verificado visualmente en Play. EditMode: 136/136.

## T19b — Guardado atómico en Android IL2CPP: preparación
**Ficheros:** `Infrastructure/Storage/AtomicFileWriter.cs` (nueva `IoLog` con `[Conditional]` DEVELOPMENT_BUILD/UNITY_EDITOR; log de cada escritura: ruta, bytes, ms, resultado, y en fallo tipo de excepción y si se borró el `.tmp`), `Infrastructure/Storage/IFileSystem.cs` (rama de `Replace`: move / replace / fallback-copy + tipo exacto de excepción), `App/AppBootstrap.cs` (resumen al arrancar), `UI/LoadStoriesScreen.cs` (resultado de cada importación), `Editor/AndroidBuild.cs` (sobrecarga con ruta de salida). El fix de `File.Replace` NO se ha aplicado. EditMode: 136/136.
**APK:** `Builds/S04/KanjiLearning-S04-dev.apk` (Development, IL2CPP, ARM64). Paquete: `com.battlecroissant.kanjilearning`.

**Procedimiento para el usuario** (móvil con depuración USB):
1. Instalar: `adb install -r Builds/S04/KanjiLearning-S04-dev.apk`
2. Ver logs en otra terminal: `adb logcat -c` y luego `adb logcat -s Unity | findstr /L /C:"[Kanji][IO]"`
3. Abrir la app. Anotar la línea de arranque `startup stories: N ok, M unreadable, K .tmp`.
4. Importar 20 historias seguidas desde Cargar historias (si el nombre se repite, elegir Reemplazar en algunas). Por cada una deben salir `replace branch=...`, `write ok ... ms=...` e `import status=Imported`.
5. Repetir haciendo force-stop durante el guardado: importar y, justo después de pulsar, ejecutar `adb shell am force-stop com.battlecroissant.kanjilearning`. Reabrir y repetir varias veces (con y sin reemplazo).
6. Qué mirar en el resumen al arrancar tras cada force-stop: `unreadable` debe ser 0 (sin historias truncadas); `.tmp` pueden existir (no se listan en la app), no es fallo; `ok` debe ser el esperado.
7. Qué mirar en los logs: si aparece `replace branch=fallback-copy` o `write FAILED`, copiar la línea completa (lleva el tipo exacto de excepción); eso decide si hace falta el fix de `File.Replace`. Si todo sale `branch=replace`/`move` y `unreadable=0`, T19b queda verificada.

## Correcciones revisión 1
Hallazgos importantes de `revision-1.md` (combinada S04/S05):
1. `PopupService.Reorder`: ahora manda los popups inferiores, luego el blocker y luego el popup superior al final (`SetAsLastSibling`), sin depender del índice del blocker. Test PlayMode nuevo: `Tests/PlayMode/PopupServiceTests.cs`.
2. `AppBootstrap.LogStorageSummary`: cuenta los `.tmp` en `persistentDataPath/stories`. Hay que regenerar el APK dev antes de T24.
3. `DiskFileSystem.Replace`: el fallback `File.Copy` solo ante `PlatformNotSupportedException`/`NotImplementedException`; el resto de `IOException` se propaga. `AtomicFileWriter.Write`: si el fallo ocurre al hacer `Replace`, NO borra el `.tmp` (puede ser la única copia íntegra); si falla antes (escritura del `.tmp`), sí lo borra. De paso, `data?.Length` en el log del `catch` (hallazgo menor, `Write(path, null)`).
   - Tests (`AtomicFileWriterTests`): `WriteFails_RemovesTemp` sustituido por `ReplaceFails_KeepsTemp_AsOnlyIntactCopy`, `TempWriteFails_RemovesTemp` y `NullData_ReturnsIoError_NoThrow`. No hay test del fallback de `DiskFileSystem` (no se puede forzar un `IOException` de `File.Replace` de forma portable).

Resultado: EditMode 166/166, PlayMode 2/2, sin errores de compilación.
