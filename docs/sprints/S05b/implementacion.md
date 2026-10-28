# S05b - Implementacion

## T1 - UiTheme + subconjunto Dark UI (HECHO)
Ficheros:
- `Assets/_Project/Scripts/UI/Theming/UiTheme.cs` (nuevo; campos privados serializados con la paleta oscura como valores por defecto, `LinkHex`, `OnValidate`).
- `Assets/_Project/Scripts/Editor/DarkUiAssetSetup.cs` (nuevo; menu `Kanji/Setup Dark UI Assets`, idempotente).
- `Assets/_Project/Art/DarkUI/{Buttons,Inputs,Panels,Icons}/*.png` (15 sprites) + `LICENSE.txt` + `DarkUI.spriteatlasv2`.
- `Assets/_Project/Settings/UiTheme_Dark.asset` (sprites y fuentes enlazados; colores sin tocar).

Decisiones locales:
- El paquete ya estaba importado en `Assets/Dark UI`; se copiaron los 15 PNG con nombres en ingles y se borro `Assets/Dark UI` entero (demo, escenas, resto).
- `maxTextureSize` 256/128 reduce la textura a la mitad y Unity reescala rect, borde y PPU (p. ej. btn_pill: 256x65, borde 32, PPU 50); el tamano en unidades y la proporcion del 9-slice no cambian.
- Texturas fuente en `Uncompressed` (el atlas las comprime a ASTC 6x6 en Android); evita el warning de Sprite Atlas.
- `WarningSurface = #4B3410` (spec ux-ui). Campos extra: IconCheck/Cross/Plus/Books/Book.
- `SlicedPpuMultiplier` por defecto 0,8 (spec ux-ui).

Como probarlo: `unity command eval "Kanji.Editor.DarkUiAssetSetup.Setup(); return 1;"` dos veces; la segunda no reimporta nada ni emite warnings. Compila sin errores ni warnings; consola limpia.
Pendiente de revisar a ojo: bordes 9-slice a 984 px de ancho (se comprueba en T2a al usar los sprites).

## T2a - SceneBuilder tematico (HECHO)
- `Assets/_Project/Scripts/Editor/SceneBuilder.cs`: carga `UiTheme_Dark.asset` (tras `FontSetup.Setup(); DarkUiAssetSetup.Setup();`), sin `Hex`, sin `UISprite.psd`, sin `new Color(`; solo `Color.clear` como literal. `NewButton(..., ButtonStyle)` Primary/Secondary/Success/Danger; Atras = area 144x144 transparente + icono `IconBack` 72 px. Tamanos desde `_theme.*Size`. `_linkColor` provisional con `_theme.Link` (lo quita T2b).
- Decision local: Delete -> Danger, Cerrar/Cancelar/Salir/KeepBoth -> Secondary, resto Primary. Toast `SurfaceRaised`, aviso `WarningSurface`, blocker `Scrim`.
- Escena y prefabs regenerados con `Kanji/Build S01 Scene` (escena guardada por el builder). 9-slice a multiplicador 0,8: bien en Inicio; no se ajusta.
- Captura (Inicio, fondo oscuro, sin tofu): `docs/sprints/S05b/t2a-home.png`. Pendiente: recorrido visual de Cargar/Biblioteca/popups/Lector (no capturado por limite de turnos; el codigo de pantallas sigue sin tocar hasta T2b).

## T2b - Pantallas leen UiTheme (HECHO)
- `ReaderScreen`, `LoadStoriesScreen`, `StoryListScreen`: `[SerializeField] UiTheme _theme` (LogError en `Init` si null); eliminados `_linkColor/_hintColor/_errorColor/_emptyColor/ToHex`. `ReaderScreen.CreateBuilder(UiTheme,int)` internal; `Scripts/UI/AssemblyInfo.cs` con `InternalsVisibleTo("Kanji.UI.Tests")`. `SceneBuilder` cablea `_theme` en los 3 prefabs. `Core` sin cambios.
- Test: `Tests/PlayMode/ReaderThemeTests.cs` (2 tests). Escena regenerada con `Kanji/Build S01 Scene`.
- Tests: EditMode 166/166. PlayMode: el runner (`unity command test run --mode PlayMode`) devuelve 0/0 aunque `test list` ve los 4 tests (reintentado una vez); la logica de `ReaderBuilder_UsesThemeLinkColor` se verifico via `eval` (LinkHex `#22D3EE`, markup contiene `<color=#22D3EE>`). Pendiente: ejecutar PlayMode desde la ventana Test Runner.
- Capturas (Game view, en play): `t2b-cargar.png`, `t2b-dialogo.png` (Nombre repetido), `t2b-biblioteca.png` (1 historia), `t2b-acciones.png` (popup de historia), `t2b-lector.png` (kanji cian). Sin fondos claros ni tofu. Nota: `capture screenshot` no ve Canvas Overlay; se cambio el canvas a ScreenSpaceCamera solo en runtime (no guardado). Para la historia con kanji se sustituyo temporalmente `sample-utf8.txt` (restaurado byte a byte).

## T3 - Popup de kanji de vista unica (HECHO)
- `Scripts/UI/KanjiPopup.cs`: `KanjiPopupResult {Knew, DidntKnow, Dismissed}`, `Show(entry, Action<KanjiPopupResult> onClosed = null)`; verde/rojo/fuera/Atras/`Close()` resuelven una sola vez (guard `_open`), sin registrar nada. Campos vacios ocultos (incluida la frase); `FitBody` conserva el scroll. Fuera `_details`, `_btnShowMore`, `_btnClose`.
- `Scripts/UI/UiStrings.cs`: `Knew`, `DidntKnow`; borrado `ShowMore`; `FieldSentenceMeaning` = "Traduccion" (spec ux-ui); `Close` se mantiene (lo usa otro popup).
- `Scripts/Editor/SceneBuilder.cs`: `BuildKanjiPopup` con Kanji (color Link), Frase, 3 filas en `BodyContent`, `SecondaryActions` inactivo y `AnswerRow` (rojo izq. `IconCross`, verde der. `IconCheck`, 160 px); helper `BuildAnswerButton`; `BuildDetailRow` con tamano/color por fila. Escena regenerada con `Kanji/Build S01 Scene`.
- Tests: EditMode 166/166 (sin test nuevo, segun arquitectura). Sin errores de compilacion.
- Captura: `docs/sprints/S05b/t3-popup.png` (popup sobre el Lector, Game view en play con canvas ScreenSpaceCamera solo en runtime; entrada fabricada via `eval`). No probado a mano: tocar fuera/Atras ni scroll largo.

## T4a - Fundido entre pantallas + bloqueo (HECHO)
- Paso previo: ni `icon_cross` (daga) ni `icon_close` (circulo tachado) eran un aspa. Se genero `Art/DarkUI/Icons/icon_x.png` (icon_plus girado 45 grados), `DarkUiAssetSetup` lo importa y enlaza `IconCross` a el; setup y escena regenerados.
- Nuevos: `Core/Navigation/InputGate.cs`, `UI/IScreenTransition.cs`, `UI/TransitionOverlay.cs`. Modificados: `ScreenNavigator` (pila sincrona; solo `Apply` se difiere; primer Push sin fundido), `BackButtonRouter.Init(..., InputGate)` (ignora Atras si bloqueado), `AppBootstrap` (`_theme`, `_transition`, crea el gate; duraciones de `UiTheme`), `SceneBuilder` (CanvasGroup en Canvas, `TransitionOverlay` ultimo hijo del Canvas con alpha 0, cableado).
- Tests: `InputGateTests` (6, EditMode), `ScreenNavigatorTransitionTests` (4, PlayMode). EditMode 172/172. PlayMode: el runner vuelve a dar 0/0 (problema conocido); los tests compilan. Pendiente: ejecutarlos desde la ventana Test Runner y probar el fundido a mano en play.
- El gate queda en `AppBootstrap` (variable local `gate`) listo para T4b (popups).

## T4b - Fundido y apilado de popups (HECHO)
- `PopupView`: `FadeIn/FadeOut/SetCovered(bool, seconds)` (unscaled, scrim como `companion`, `seconds<=0` sincrono, `OnDisable` cierra el fundido pendiente); fuera `PlayOpenFade`/`FadeSeconds`.
- `PopupService(blocker, keepOnTop, InputGate gate, fadeSeconds)`: `Open/Close` bloquean el gate; el de debajo se oculta por CanvasGroup (sin desactivar) y reaparece al cerrar el de arriba; el cerrado se funde por encima del scrim y luego se desactiva (y apaga el scrim si es el ultimo). Toques en el scrim y Atras ignorados mientras el gate esta bloqueado.
- `AppBootstrap` pasa `gate` y `_theme.PopupFadeSeconds`; `SceneBuilder` anade `CanvasGroup` al blocker; escena regenerada y guardada.
- Tests: `PopupServiceTests` +4 (PlayMode, fade 0). EditMode 172/172. PlayMode: el runner da 0/0 (problema conocido; los tests compilan).
- Verificado en Play por `eval`: ChoicePopup -> ConfirmPopup (el de debajo alpha 0, sin raycast, activo) -> Cancelar -> el de debajo vuelve a alpha 1, interactable, confirm inactivo. No se uso la historia real, solo los popups de dialogo.

## T5 — APK de desarrollo S05b
Generado con `Kanji.Editor.AndroidBuild.BuildDevelopmentApk("Builds/S05b/KanjiLearning-S05b-dev.apk")` (mismo método que S05, vía `unity command script eval --detach`), escena guardada antes, sin cambios de código.
**APK:** `Builds/S05b/KanjiLearning-S05b-dev.apk` (68 793 667 bytes, ~65,6 MB; Development, IL2CPP, ARM64). Paquete `com.battlecroissant.kanjilearning`.
**Instalar:** `adb install -r Builds/S05b/KanjiLearning-S05b-dev.apk`
