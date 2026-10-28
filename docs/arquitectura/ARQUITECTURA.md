# KanjiLearning — Arquitectura

> La mantiene el agente `arquitecto`. Decisiones difíciles de revertir en `adr/`:
> [0001 Almacenamiento](adr/0001-almacenamiento-json.md) · [0002 Selector de archivos Android](adr/0002-selector-archivos-android.md) · [0003 Tecnología de UI](adr/0003-ui-ugui-textmeshpro.md) · [0004 JsonUtility y escritura atómica](adr/0004-serializador-jsonutility-y-escritura-atomica.md) · [0005 Tema visual UiTheme](adr/0005-tema-visual-uitheme.md)

Principio rector: lógica en C# puro testeable en EditMode; Unity solo en los bordes (UI, disco, plugin nativo). Una escena, sin singletons.

## Módulos y asmdefs
| asmdef | Carpeta | Contenido | Refs Unity |
|---|---|---|---|
| `Kanji.Core` | `Assets/_Project/Scripts/Core` | Modelo (`Story`, `KanjiEntry`, `Segment`, `KanjiKey`), `StoryParser` + `ParseReport`, `TextDecoder` (UTF-8/Shift-JIS), `ReadingSession` + `ReadingStats`, `StoryRichTextBuilder` (S05: genera el rich text TMP como strings, sin TMP), servicios de caso de uso (`StoryLibrary`, `DifficultKanjiList`), puertos (`IStoryRepository`, `IDifficultKanjiRepository`, `IFilePicker`, `IClock`) | **No** (`noEngineReferences: true`) |
| `Kanji.Infrastructure` | `.../Scripts/Infrastructure` | `JsonStoryRepository`, `JsonDifficultKanjiRepository`, `AtomicFileWriter`, DTOs + migraciones, `NativeFilePickerAdapter` (Android + rama de Editor interna); refs `Kanji.Core`, `NativeFilePicker.Runtime` | Sí |
| `Kanji.UI` | `.../Scripts/UI` | `ScreenNavigator`, `PopupService`, `BackButtonRouter`, pantallas, popups (`KanjiPopup` con vistas 1/2 como estado), `LinkClickHandler`, `SafeAreaFitter` | Sí (uGUI, TMP, Input System) |
| `Kanji.App` | `.../Scripts/App` | `AppBootstrap` (composition root, MonoBehaviour en la escena) | Sí |
| `Kanji.Core.Tests` | `Assets/_Project/Tests/EditMode/Core` | Parser (todos los casos de GDD 5.2), decoder, sesión/estadística, servicios con repos falsos | EditMode |
| `Kanji.Infrastructure.Tests` | `.../Tests/EditMode/Infrastructure` | Repos JSON contra carpeta temporal, migraciones, escritura atómica | EditMode |
| `Kanji.UI.Tests` | `.../Tests/PlayMode` | Navegación y popups (solo lo esencial) | PlayMode |

## Dependencias permitidas
```
Kanji.App ──► Kanji.UI ──► Kanji.Core
    └───────► Kanji.Infrastructure ──► Kanji.Core
                     └──► NativeFilePicker (paquete de terceros, solo aquí)
```
- `Core` no referencia nada. `UI` e `Infrastructure` no se conocen entre sí; solo hablan vía puertos de `Core`.
- Solo `App` instancia implementaciones concretas y las pasa por constructor/`Init(...)`.
- Prohibido: `UI → Infrastructure`, `Core → UnityEngine`, estáticos con estado.

## Estructura de carpetas
```
Assets/_Project/
  Scenes/Main.unity          ← única escena
  Scripts/{Core,Infrastructure,UI,App}/   (cada una con su .asmdef)
  Prefabs/Screens/  Prefabs/Popups/  Prefabs/Widgets/
  Fonts/  (NotoSansJP-*.ttf/otf + SDF dinámicos)
  Settings/ (UI ScriptableObjects de estilo si hacen falta)
  Resources/ (TMP Settings: TMP lo carga desde Resources)
  Tests/{EditMode,PlayMode}/
  Plugins/Android/           ← solo si hiciera falta (ver ADR 0002)
  link.xml                   ← preserva I18N/I18N.CJK/CodePages para Shift-JIS en IL2CPP (S02 T07)
```
Fuera de `_Project`: plantilla URP y paquetes; no tocar.

## Flujo de datos
**Carga:** `LoadStoriesScreen` → `IFilePicker.PickTextFile()` → (`bytes`, `fileName`) → `TextDecoder.Decode` → `StoryParser.Parse` → (`Story`, `ParseReport`) → `StoryLibrary.Import(story, conflictPolicy)` → `IStoryRepository.Save` → pantalla muestra informe. Conflicto de nombre: `Import` devuelve `NameConflict`, la UI abre popup (Reemplazar/Conservar ambas/Cancelar) y reintenta con la política elegida.

**Lista:** `StoryListScreen` → `StoryLibrary.List()` → `StoryListing` (items + nº de ficheros ilegibles). El repo lee solo la cabecera (`HeaderDto`, DTO v2 con `importedAtUnixMs`, `entryCount`; sin índice, ver ADR 0004). Orden (solo en `StoryLibrary`): más reciente primero. La pantalla se oculta, no se destruye, y solo re-renderiza si cambia `StoryLibrary.Revision` (conserva scroll). Fila → `StoryActionsPopup` → Eliminar → `ConfirmPopup` → `StoryLibrary.Delete(id)`.

**Popups:** `PopupService` (C# puro, pila + fondo bloqueante) y `PopupView` (base con `Cancel()` y `CloseOnOutsideTap`); resultados por callback. `BackButtonRouter` consulta primero `PopupService.HandleBack()`.

**Lectura:** `ReaderScreen` → `StoryLibrary.Load(id)` → `StoryRichTextBuilder` (Core) genera N trozos (por párrafos, tamaño máximo parametrizable) con `<link="k{idx}"><color=#..>漢</color></link>` y texto plano dentro de `<noparse>` (cada `<` literal seguido de cierre/reapertura de `noparse`; detalle en `sprints/S05/arquitectura.md`) → un `TextMeshProUGUI` por trozo (`parseCtrlCharacters = false`) en un `ScrollRect` con `RectMask2D` y sub-canvas, rellenado un trozo por frame. Toque → `TMP_TextUtilities.FindIntersectingLink` → índice → `KanjiPopup(entry, mode: Reader)`. "Mostrar más" → `ReadingSession.MarkRevealed(KanjiKey)`. Cerrar → `ReadingSession.ComputeStats()` → `StatsScreen` → lista.

**Complicados:** `KanjiPopup` → `DifficultKanjiList.Add(entry)` (copia de los 5 campos, dedup por `KanjiKey` = Kanji+Reading normalizados) → `IDifficultKanjiRepository.Save`. `DifficultListScreen` → `KanjiPopup(mode: Difficult)` → `Remove(key)`.

## Navegación entre pantallas y popups
- Una escena `Main` con un `Canvas` (Screen Space Overlay, `CanvasScaler` 1080×1920, match 0.5) y dos capas: `ScreenLayer` y `PopupLayer` (con fondo bloqueante que cierra al tocar fuera si el popup lo permite).
- `ScreenNavigator`: pila de pantallas (`Push/Pop/Replace`). Pantallas = prefabs instanciados una vez y activados/desactivados. Interfaz:
  ```csharp
  public interface IScreen { void OnShow(object args); void OnHide(); bool HandleBack(); } // false = Pop por defecto
  ```
- `PopupService`: pila de popups; `Show<TPopup>(args)` devuelve `Task<TResult>` (o callback) para que la pantalla espere la elección (confirmar, conflicto de nombre).
- `BackButtonRouter` (Input System, `Keyboard.current.escapeKey` = Atrás en Android): primero cierra el popup superior; si no hay, `screen.HandleBack()`; en Inicio → `Application.Quit()`.
- Pantallas: `Home`, `LoadStories`, `StoryList`, `Reader`, `Stats`, `DifficultList`. Popups: `StoryActions`, `Confirm` (genérico), `Choice` (genérico, 3 botones), `Kanji` (vistas 1/2, modo Reader/Difficult), `Toast`.

## Convenciones
- Código, identificadores, commits: inglés. Namespaces = asmdef (`Kanji.Core`, …). `PascalCase` tipos/métodos, `_camelCase` campos privados, `[SerializeField] private` en vistas.
- Vistas MonoBehaviour delgadas: reciben servicios en `Init(...)` desde `AppBootstrap`; la lógica va a `Core`.
- Nada de `FindObjectOfType`, singletons ni `static` mutable. Eventos C# (`event Action<T>`) dentro de UI; no UnityEvents para lógica.
- Errores esperables (archivo inválido, conflicto) como resultados (`Result`/`ParseReport`), no excepciones.
- IO síncrono permitido (archivos pequeños); si una historia supera ~200 ms al cargar, mover a `Task.Run` y volver al hilo principal.
- Toda clase de `Core` con tests EditMode; los casos de GDD 5.2 son tests nominales.
- Textos de UI en español dentro de los prefabs/un `UiStrings` estático de constantes (sin paquete Localization).
- Táctil ≥ 48 dp (≈ 144 px a 1080 de referencia); `SafeAreaFitter` en el root de cada pantalla.

## Notas para el planificador
- **Paquetes a añadir:** NativeFilePicker (yasirkula, MIT) vía git URL o OpenUPM (ADR 0002); TMP ya viene en `com.unity.ugui` 2.x, solo hay que importar *TMP Essential Resources*. Opcional y a confirmar con el usuario: quitar paquetes sin uso (visualscripting, iet-framework, timeline, 2d animation/aseprite/psdimporter/spriteshape/tilemap) para acelerar builds.
- **El usuario necesita:** módulo *Android Build Support* (+ OpenJDK, SDK & NDK) para 6000.6.2f1 en Unity Hub; móvil Android con opciones de desarrollador y depuración USB activas, cable y `adb` autorizado; git instalado si se usa git URL; descargar Noto Sans JP (OFL) desde Google Fonts y dejarla en `Assets/_Project/Fonts/` (descarga = acción del usuario).
- **Fases 0+1 en un sprint** como *spike* de riesgos en dispositivo: build Android, fuente CJK, NativeFilePicker y decodificación Shift-JIS. Riesgo: `Encoding.GetEncoding(932)` puede no existir en IL2CPP Android (faltan datos I18N); si falla, decodificador SJIS propio o quitar SJIS (decisión del usuario).
- **Parser y persistencia no dependen del móvil**: son 100 % EditMode y pueden adelantarse o ir en paralelo a la Fase 1. Conviene crear en Fase 2 un fixture de 50 000 caracteres/200 kanjis para la Fase 3.
- **Fase 3 más cara de lo que parece:** un solo `TextMeshProUGUI` no admite 50 000 caracteres (límite ~65 k vértices ≈ 16 k glifos), hay que trocear por párrafos y escapar `<` del usuario (`<noparse>`). Estimar 1 sprint completo para lector + popup.
- Las preguntas abiertas del GDD no bloquean arquitectura: la definición de acierto vive solo en `ReadingSession` (Fase 5) y el escape `\;` solo en `StoryParser`; pueden confirmarse más tarde sin rehacer nada.
