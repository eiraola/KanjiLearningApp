
## T06 — IFilePicker + NativeFilePickerAdapter
**Ficheros** (bajo `Assets/_Project/`): `Scripts/Core/Files/IFilePicker.cs`, `Scripts/Infrastructure/Files/NativeFilePickerAdapter.cs`, `Scripts/Infrastructure/Kanji.Infrastructure.asmdef`, `Tests/Fixtures/Text/sample-utf8.txt` (4,6 KB, kanji), `Scripts/App/Kanji.App.asmdef` (+`Kanji.Infrastructure`).
**Decisiones**
- `PickResult.Picked` rellena `SizeBytes` con `bytes.Length`.
- Rama de Editor: si `editorSamplePath` existe se lee sin diálogo; si no, se usa el plugin. En Android se borra la copia de caché en `finally`.
- Se capturan también `ArgumentException`/`NotSupportedException` (rutas raras) como `Unreadable`.
- Sin tests nuevos (adaptador depende del plugin/IO); no se cablea en `AppBootstrap` (T08).
**Verificar**: `unity recompile` sin errores. En T08, construir `new NativeFilePickerAdapter(1_048_576, <ruta absoluta del fixture>)` en el Editor y comprobar que `PickTextFile` devuelve `Picked` sin diálogo.

## T07 — TextDecoder
**Ficheros** (bajo `Assets/_Project/`): `Scripts/Core/Text/DecodeResult.cs`, `Scripts/Core/Text/TextDecoder.cs`, `link.xml`, `Tests/EditMode/Core/Text/TextDecoderTests.cs`.
**Decisiones**
- Algoritmo según arquitectura. Cambio local: la validación Shift-JIS comprueba que el texto se recodifica a los mismos bytes (ida y vuelta) además de no contener U+FFFD, porque el Mono I18N del Editor aceptó `FF FE 81 20` pese a `ExceptionFallback`.
- `CodePagesEncodingProvider` se registra por reflexión una sola vez (flag estático idempotente).
**Tests**: `unity command test run --mode EditMode --filter TextDecoderTests` → 9/9 superadas (1.ª ejecución 8/9 por lo anterior; tras corregir 9/9). Compilación sin errores ni warnings.
**Verificar en dispositivo**: `SupportsShiftJis` en IL2CPP (T10).

## T09 — Endurecimiento S01
- `Scripts/Editor/FontSetup.cs`: fuentes dinámicas con sampling 48, padding 5, atlas 2048 multi-atlas (SDFAA); idempotente (recrea el asset si difiere en tamaño/sampling/multi-atlas). Elimina `NotoSansJP-Medium` (.ttf y SDF) y `FontTest_TEMP` si existen. Lista de fallbacks global de TMP Settings vacía; Bold mantiene Regular como fallback local.
- `Scripts/Editor/SceneBuilder.cs`: etiquetas de botón pasan a Regular; sin referencias a Medium.
- `Scripts/App/AppBootstrap.cs`: prefab nulo o `ScreenId` duplicado -> `LogError` + `continue`; sin `Home` registrado -> `LogError` y sin `Push`. No lanza.
- `docs/sprints/S01/implementacion.md` T04: nota del "1 error" corregida (viene de `PipelineRuntimeBuildProcessor`, no de Bee).
- Ejecutado una vez `Kanji/Setup Fonts` y una vez `Kanji/Build S01 Scene` (escena regenerada). Tests EditMode: 14/14 en verde.
- Probar: menú `Kanji/Setup Fonts`; comprobar que `NotoSansJP-Regular SDF.asset` tiene Atlas 2048 / Sampling 48 / Padding 5.

## T08 — Pantalla Cargar + TextPreview + cableado
**Ficheros** (bajo `Assets/_Project/`): `Scripts/Core/Text/TextPreview.cs`, `Tests/EditMode/Core/Text/TextPreviewTests.cs`, `Scripts/UI/LoadStoriesScreen.cs`, `Scripts/UI/UiStrings.cs` (textos Cargar), `Scripts/Editor/SceneBuilder.cs` (`BuildLoadStoriesPrefab`, helpers `BuildTopBar`/`TopStretch`), `Scripts/App/AppBootstrap.cs` (decoder, picker, `_editorSampleFile`, `case LoadStoriesScreen`).
**Decisiones locales**
- Formato KB en `LoadStoriesScreen.FormatKb` (UI); colores de pista/error serializados con valores de la guía.
- El `ContentSizeFitter` va en el `Content` del scroll con un `VerticalLayoutGroup` (en vez de en el propio TMP) para que la altura siga al texto de forma fiable.
- `OnShow` vuelve al estado Inicial y rehabilita el botón; `OnHide` limpia textos (no retiene bytes).
- Con `MoreLabel` visible, `PreviewScroll` deja 80 px inferiores (`SetPreviewInset`).
**Verificación**: `unity recompile` sin errores; EditMode 19/19. Play Mode (Editor, sin diálogo): Cargar muestra `sample-utf8 | 5 KB · UTF-8`, 1560 caracteres, sin "más" (< 3000). La captura del Game View salió en blanco (Canvas Overlay no se captura en este modo), así que la comprobación visual queda para T10/dispositivo.
**Probar**: `Kanji/Build S01 Scene`, Play, Inicio > Cargar historias > Elegir archivo de texto.

## Correcciones revisión 1
- `Assets/_Project/Scripts/Infrastructure/Files/NativeFilePickerAdapter.cs:94-97` (`TryDelete`): solo borra si la ruta completa está dentro de `Application.temporaryCachePath`; nunca el archivo original del usuario (bloqueante).
- `NativeFilePickerAdapter.cs:36-46`: `try/catch` alrededor de `ConvertExtensionToFileType` y `PickFile`; en error `_busy = false` y `PickResult.Failed(PickError.Unreadable)` (importante).
- `Assets/_Project/Scripts/Editor/SceneBuilder.cs:128`: eliminado el bloque `FontTest_TEMP`; `Home.prefab` regenerado con `Kanji/Build S01 Scene` (0 apariciones de `FontTest`).
- `Assets/_Project/Scripts/Editor/FontSetup.cs:24`: eliminada la línea muerta `DeleteObsolete(... FontTest_TEMP.asset)`.
- Verificación: recompilación sin errores; EditMode 19/19. Menores no tocados.
