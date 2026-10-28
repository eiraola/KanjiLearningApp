# S05 — Implementación

## T20 — `StoryRichTextBuilder`
**Ficheros:** `Core/Reading/StoryRichTextBuilder.cs` (incluye `RichTextChunk`); tests `Tests/EditMode/Core/Reading/StoryRichTextBuilderTests.cs`, helper `Tests/EditMode/Core/Fixtures/TmpVisibleText.cs`, `Tests/EditMode/Core/Reading/StoryFixtureExport.cs` (`[Explicit]`); `Tests/PlayMode/RichTextTmpEscapeTests.cs`; ficheros T24 `Tests/Fixtures/Text/perf-50k.txt` (87 KB, UTF-8 sin BOM) y `hostile-markup.txt`.
**Decisiones locales:**
- Escape de `<` según la arquitectura (`<` + `</noparse><noparse>`): validado contra TMP real, no hizo falta plan B.
- **Hallazgo:** TMP convierte `\uXXXX`/`\UXXXXXXXX` literal en el carácter aunque `parseCtrlCharacters = false` (`\n` y `\t` sí quedan literales). Se evita insertando un corte `</noparse><noparse>` entre `\` y `u`/`U` (invisible, no cambia el texto visible). Cambio solo en `AppendEscaped`.
- Cada trozo abre un único `<noparse>` por tramo de texto contiguo (no uno por carácter); el troceo de párrafos largos trabaja con unidades de 1 carácter/par suplente/enlace.
- Un `\n` final de la historia se conserva en el último trozo (`BreakAfter = false`).
- Sin `Reading/` previo en tests: se creó la carpeta; el asmdef de tests la cubre.
**Probar:** recompilar y `unity command test run -- --mode EditMode` -> 164/164 (136 previos + 28 nuevos). PlayMode: `unity command test run -- --mode playmode --async_tests` y `unity command test status` -> 1/1 (`RichTextTmpEscapeTests`). `Build_Fixture50k_UnderBudget` escribe el tiempo al log del test. Regenerar `perf-50k.txt`: ejecutar `ExportPerfFixture` (en Test Runner, o `unity command script eval` por reflexión, ya que `[Explicit]` no entra por filtro).
**Resultado:** EditMode 164/164, PlayMode 1/1.

## T21 — `ReaderScreen` real
**Ficheros:** `UI/ReaderScreen.cs` (reescrito), `UI/UiStrings.cs` (`StoryOpenFailed`, `ReaderLoading`; se quita `ReaderComingSoon`), `App/AppBootstrap.cs` (`reader.Init(navigator, library, _toast)`), `Editor/SceneBuilder.cs` (`BuildReaderPrefab` nuevo: barra, ScrollRect/Viewport con RectMask2D + Canvas anidado + GraphicRaycaster, Content con VLG + ContentSizeFitter, `ChunkTemplate` inactivo, `LoadingText`). Prefabs/escena regenerados con el menú `Kanji/Build S01 Scene` (MCP).
**Decisiones locales:**
- Sin `_closeStoryButton` (ux-ui: solo Atrás). `_loadingText` añadido según ux-ui; se activa tras 0,3 s sin primer trozo.
- Corrutina: `yield null` -> `Load` -> `Build` en try/catch; fallo -> `LogWarning` + toast + `Pop`. Un trozo por frame, el primero en el mismo frame de la carga. Trozos reutilizados como pool.
- Logs `[Kanji][Perf] reader-open` y `reader-scroll` con `[Conditional]`; `Update` solo bajo `UNITY_EDITOR || DEVELOPMENT_BUILD`.
**Probar:** menú `Kanji/Build S01 Scene`, Play, importar `Tests/Fixtures/Text/perf-50k.txt` y pulsar Leer; mirar `[Kanji][Perf] reader-open` en consola.
**Resultado:** compila sin errores; EditMode 164/164 (tras cerrar el diálogo modal "guardar escena" que bloqueó el primer intento); escena guardada y limpia, prefab `StoryReader` con el ReaderScreen y `ChunkTemplate`.
**Medida (Editor, Play, `perf-50k.txt` = 250 enlaces):** `reader-open chars=25963 chunks=7 links=250 loadMs=8 buildMs=2 firstVisibleMs=290 totalMs=1008 atlases=1` (primera apertura, con generación de glifos) y `firstVisibleMs=73 totalMs=318` en la segunda. Datos de prueba borrados. Falta la medida en móvil (T24) y el scroll (`reader-scroll`) no se midió en Editor.

## T22 — Pulsar kanji + `KanjiPopup` vista 1
**Ficheros:** `UI/LinkClickHandler.cs` y `UI/KanjiPopup.cs` (nuevos), `UI/ReaderScreen.cs` (`Init(..., KanjiPopup)`, `OnEntryTapped`, log `kanji-popup`, `Close()` defensivo en `OnHide`), `UI/UiStrings.cs` (`ShowMore`), `App/AppBootstrap.cs` (`_kanjiPopup`, `Init(popups)`, umbral de arrastre ≈ 8 dp), `Editor/SceneBuilder.cs` (`BuildKanjiPopup`, `LinkClickHandler` en `ChunkTemplate`). Escena y prefabs regenerados con el menú `Kanji/Build S01 Scene` y guardados.
**Decisiones locales:**
- Sin `Scrim` propio: se reutiliza el `PopupBlocker` compartido de `PopupService` (fuera/Atrás ya cierran vía `CloseOnOutsideTap`/`Cancel`), igual que `StoryPopup`.
- `BtnShowMore` visible; `ShowDetails()` es un hook vacío para T23. Sin grupo `Details` aún.
- `FitBody` fuerza el layout y limita el alto del cuerpo a `_maxBodyHeight = 1100`; reinicia el scroll del cuerpo en cada `Show`.
- `OnHide` del lector comprueba `_kanjiPopup != null` porque `AppBootstrap` llama `OnHide()` antes de `Init`.
**Probar:** menú `Kanji/Build S01 Scene`, Play, importar `perf-50k.txt`, Leer y tocar un kanji azul; arrastrar sobre un kanji no abre; tocar fuera/Atrás/Cerrar cierran solo el popup.
**Resultado:** compila sin errores; EditMode 164/164. Verificado por script en Play (historia `perf-50k` importada y borrada después): 12/12 enlaces del primer trozo abren el popup con el kanji de su entrada (incluye kanjis repetidos, entradas 2 y 4), `dragging`/arrastre > umbral y texto no azul no abren, y el scroll del lector queda igual tras abrir y cerrar (300 -> 300). Pendiente en móvil (T24): puntería real, tiempo de apertura y aspecto visual.

## T23 — Vista 2 "Mostrar más"
**Ficheros:** `UI/KanjiPopup.cs` (`_details`, filas, `_reading/_meaning/_sentenceMeaning`, `ShowDetails()`, `SetRow`; `Show` guarda la entrada y reinicia a vista 1), `UI/UiStrings.cs` (`FieldReading`, `FieldMeaning`, `FieldSentenceMeaning`), `Editor/SceneBuilder.cs` (`BuildDetailRow`, grupo `Details` dentro de `BodyContent`). Escena regenerada con `Kanji/Build S01 Scene` y guardada.
**Decisiones locales:** etiquetas en `TextSecondary`; filas vacías ocultas enteras; el scroll del cuerpo se reajusta con `FitBody` (arriba) y Cerrar no toca el lector.
**Probar:** Play, abrir historia, tocar un kanji, "Mostrar más"; Cerrar y reabrir vuelve a vista 1.
**Resultado:** compila sin errores; EditMode 164/164. Verificado por script en Play con 3 entradas (completa, campos vacíos, textos de 900 caracteres): vista 2 muestra filas correctas, "Mostrar más" desaparece, vacíos -> 0 filas, texto largo -> cuerpo limitado a 1100 y scroll arriba, reabrir -> vista 1. Sin datos de prueba persistentes; escena sin cambios pendientes. Pendiente en móvil (T24): aspecto visual.
