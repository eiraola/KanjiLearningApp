# S01 — Implementación T01 + T02 + T03

## Estado real (verificado en el Editor 6000.6.2f1)
- TMP Essential Resources importado (`Assets/TextMesh Pro`). Compilación sin errores.
- `Kanji/Build S01 Scene` genera `Scenes/Main.unity`, `Prefabs/Screens/{Home,LoadStories,StoryList,DifficultList}.prefab`; Main es la única escena en Build Settings.
- Tests EditMode: `NavigationStackTests` 5/5 verdes (ejecutados con TestRunnerApi).
- Play: Home con 4 botones; `BtnLoadStories.onClick` activa LoadStories y `BtnBack.onClick` vuelve a Home. Salida de Play y escena guardada.
- Consola: sin errores. Un warning conocido: `Kanji.UI.Tests.asmdef` (PlayMode) "has no scripts" (asmdef vacío a propósito hasta que haya tests PlayMode).

## Ficheros (bajo `Assets/_Project/`)
- asmdefs: Core, UI, App, Editor, Core.Tests, UI.Tests (vacío).
- Core: `Navigation/NavigationStack.cs` + test.
- UI: `ScreenId`, `IScreen`, `ScreenView`, `ScreenNavigator`, `BackButtonRouter`, `SafeAreaFitter`, `HomeScreen`, `EmptyScreen`, `UiStrings`.
- App: `AppBootstrap.cs`.
- Editor: `SceneBuilder.cs` (menú `Kanji/Build S01 Scene`), `FontSetup.cs` (menú `Kanji/Setup Fonts`, idempotente; el builder lo llama).
- Fonts: `NotoSansJP-{Regular,Medium,Bold} SDF.asset` (TMP dinámicos, atlas 1024, SDFAA, multi-atlas).

## T03 — fuentes
- Regular = fuente por defecto de TMP Settings (`Assets/TextMesh Pro/Resources/TMP Settings.asset`); Medium y Bold en su Fallback list. Medium y Bold tienen Regular como fallback.
- Pesos por asset (no `FontStyles.Bold`): títulos = Bold, etiquetas de botón = Medium, resto Regular (SceneBuilder asigna `tmp.font`).
- Texto temporal `FontTest_TEMP` en Home (`。「」鬱麤饕餮髙`): verificado en Play, sin cuadrados. **Quitar al cerrar el sprint** (bloque marcado `TEMP (T03)` en `SceneBuilder.BuildHomePrefab`).
- Hallazgo: 齉 (U+9F49) y 龘 (U+9F98) NO están en Noto Sans JP y salen como cuadrado; es una limitación de la fuente (no de la configuración). Si alguna vez hacen falta, habría que añadir un fallback con cobertura CJK ampliada.

## Decisiones locales
- `Kanji.Editor.asmdef` referencia Core/UI/App/UGUI/TMP/InputSystem (el builder añade esos componentes).
- Bug corregido en el builder: `NewScene` descarga los prefabs recién guardados y las referencias de `_screenPrefabs` quedaban nulas; ahora se guardan las rutas y se recargan tras `NewScene`.
- Botón secundario pulsado `#CFCABF` (la guía no lo define).
- Para capturas en Play el overlay no sale con `screenshot`; truco: pasar el Canvas a ScreenSpaceCamera por eval (solo runtime).

## Cómo probar
Menú `Kanji > Build S01 Scene`; Test Runner EditMode (5 verdes); Play en `Main`.

## No hecho
Nada más pendiente en T01-T04.

## T04 — Android APK
- `Scripts/Editor/AndroidBuild.cs`: menú `Kanji/Build Android APK` (`BuildDevelopmentApk`); `ApplyPlayerSettings` fija BattleCroissant/KanjiLearning, `com.battlecroissant.kanjilearning`, IL2CPP, solo ARM64, minSdk 26, targetSdk Auto, Portrait, renderOutsideSafeArea, stripping Minimal, APK (no AAB), Development Build sin script debugging. `/Builds/` añadido a `.gitignore`.
- Build ejecutado en el Editor (cambia la plataforma activa a Android): **Succeeded**, ~11 min, `Builds/Android/KanjiLearning-dev.apk` = 51,3 MB (el informe de Unity da 829 MB de "totalSize" por símbolos/intermedios, no es el tamaño del APK).
- Warnings: 6, todos benignos (shaders HDRP de TMP sin subshaders para Android, `-x c++` de clang, métodos TMP grandes en IL2CPP). El "1 error" del resumen proviene de `PipelineRuntimeBuildProcessor` (com.unity.pipeline), no de Bee, y no afecta: el resultado es Succeeded.
- Instalar: `adb install -r Builds/Android/KanjiLearning-dev.apk`. Active Input Handling no tiene API pública: no se ha verificado/forzado desde código (el proyecto ya usa Input System).
- El Editor queda con Android como plataforma activa.
