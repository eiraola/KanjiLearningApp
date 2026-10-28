# S05b — Arquitectura (T1–T4)

Entradas: `plan.md`, `valoracion-arquitectura.md`, `valoracion-ux.md`, `propuesta-diseno.md`. ADR nuevo: [0005 Tema visual `UiTheme`](../../arquitectura/adr/0005-tema-visual-uitheme.md).

Supuestos confirmados por el usuario (cierran la ida y vuelta con el diseñador): verde = "lo sabía", rojo = "no lo sabía", **no se registra nada**; tocar fuera / Atrás cierran sin registrar; "Añadir a complicados" será un tercer botón en S06 (solo hueco en layout); duraciones 0,2 + 0,2 s entre pantallas y 0,15 s en popups.

Orden: **T1 → T2a → T2b → T3 → T4a → T4b**. T2 y T4 se parten para caber en ≤ 15 turnos cada una. Todas las rutas relativas a `Assets/_Project/`.

---

## T1 — `UiTheme` + subconjunto Dark UI
Cubre: plan T1; diseño T1 (tema oscuro único, kanji cian); ADR 0005.

### Ficheros
| Acción | Ruta |
|---|---|
| Crear | `Scripts/UI/Theming/UiTheme.cs` (asmdef `Kanji.UI`, namespace `Kanji.UI`) |
| Crear | `Scripts/Editor/DarkUiAssetSetup.cs` (asmdef `Kanji.Editor`) |
| Crear (por script) | `Settings/UiTheme_Dark.asset`, `Art/DarkUI/DarkUI.spriteatlasv2` |
| Copiar | `Art/DarkUI/{Buttons,Inputs,Panels,Icons}/*.png` + `Art/DarkUI/LICENSE.txt` |

### `UiTheme` (ScriptableObject)
Campos `[SerializeField] private` con **inicializadores = paleta de `valoracion-ux.md`** (así `CreateInstance<UiTheme>()` ya es el tema oscuro en tests) y propiedades de solo lectura.
```csharp
[CreateAssetMenu(menuName = "Kanji/UI Theme")]
public sealed class UiTheme : ScriptableObject
{
    // Colors
    public Color Background { get; }      // #14171C
    public Color Surface { get; }         // #1D222A  (barra, tarjetas, popups)
    public Color SurfaceRaised { get; }   // #272D38  (campos, secundario, toast)
    public Color Border { get; }          // #3A4150
    public Color TextPrimary { get; }     // #E8EAF0
    public Color TextSecondary { get; }   // #A3A9B8
    public Color TextOnAccent { get; }    // #FFFFFF  (texto sobre Primary/Success/Danger)
    public Color Primary { get; }         // #C4302B
    public Color PrimaryPressed { get; }  // #A32520
    public Color Secondary { get; }       // #272D38
    public Color SecondaryPressed { get; }// #3A4150
    public Color Success { get; }         // #1B7A3A
    public Color SuccessPressed { get; }  // #14632E
    public Color Danger { get; }          // #C4302B
    public Color DangerPressed { get; }   // #A32520
    public Color Disabled { get; }        // #272D38
    public Color DisabledText { get; }    // #6B7180
    public Color Link { get; }            // #22D3EE
    public Color Error { get; }           // #FF8A80
    public Color WarningSurface { get; }  // lo fija ux-ui (tono ámbar oscuro)
    public Color Scrim { get; }           // #000000 a 0.65
    public Color FadeColor { get; }       // #000000 (fundido entre pantallas)
    // Sprites
    public Sprite ButtonSprite, PanelSprite, InputSprite, DividerSprite, CircleSprite { get; }
    public Sprite IconBack, IconClose, IconTrash { get; }   // lista final: ux-ui
    public float SlicedPpuMultiplier { get; }               // ajuste de radio 9-slice (def. 1)
    // Fonts
    public TMP_FontAsset Regular, Bold { get; }
    // Sizes (px a 1080 de referencia)
    public float DisplaySize = 96, TitleSize = 56, ButtonSize = 52, ReadingSize = 48,
                 BodySize = 44, CaptionSize = 36, KanjiMinSize = 96, KanjiMaxSize = 220;
    // Motion (T4)
    public float ScreenFadeOutSeconds = 0.2f, ScreenFadeInSeconds = 0.2f, PopupFadeSeconds = 0.15f;

    public string LinkHex => "#" + ColorUtility.ToHtmlStringRGB(Link); // siempre #RRGGBB
    private void OnValidate(); // Debug.LogWarning por cada sprite/fuente null; duraciones < 0 -> 0
}
```
(Firmas abreviadas: cada miembro es campo privado serializado + propiedad `=>`.)

### `DarkUiAssetSetup` (Editor, idempotente)
```csharp
public static class DarkUiAssetSetup
{
    public const string ArtDir = "Assets/_Project/Art/DarkUI";
    public const string ThemePath = "Assets/_Project/Settings/UiTheme_Dark.asset";
    public const string AtlasPath = ArtDir + "/DarkUI.spriteatlasv2";
    [MenuItem("Kanji/Setup Dark UI Assets")] public static void Setup();
}
```
1. Tabla interna `(relativePath, Vector4 border, bool sliced, int maxSize)` con los ~15 sprites (nombres en inglés: `Buttons/btn_pill.png`, `Buttons/btn_pill_round.png`, `Buttons/btn_medium.png`, `Inputs/input_field.png`, `Panels/divider.png`, `Panels/circle_2px.png`, `Icons/icon_back.png`, `Icons/icon_close.png`, `Icons/icon_trash.png`…). Bordes medidos una vez en el Sprite Editor y escritos en la tabla (radio ≈ 64 px en los pill).
2. Por cada uno, `TextureImporter`: `textureType = Sprite`, `spriteImportMode = Single`, `TextureImporterSettings.spriteMeshType = FullRect`, `spriteBorder`, PPU 100, `mipmapEnabled = false`, `wrapMode = Clamp`, `filterMode = Bilinear`, `alphaIsTransparency = true`, `maxTextureSize` 256 (iconos 128); `SaveAndReimport()` solo si cambió algo.
3. Atlas: `EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2`; si no existe, `new SpriteAtlasAsset()` + `Add(new Object[]{ carpeta ArtDir })` + `SpriteAtlasAsset.Save(atlas, AtlasPath)`; luego `SpriteAtlasImporter`: `packingSettings` (padding 4, `enableRotation = false`, `enableTightPacking = false`), Android `TextureImporterFormat.ASTC_6x6`.
4. Tema: si no existe `ThemePath`, `CreateInstance<UiTheme>()` + `AssetDatabase.CreateAsset`. Después enlaza por `SerializedObject` sprites (desde la tabla) y fuentes (`FontSetup.RegularPath/BoldPath`) **sin pisar colores** (los colores solo se tocan en el Inspector o en el inicializador).

Importación del paquete: con el Editor, desde la caché de Asset Store (ruta en `plan.md`); dejar solo el subconjunto en `Art/DarkUI`, borrar el resto (`Assets/Dark UI` o similar). Si el import falla: parar y preguntar al usuario.

### Tests / verificación
- Compila; `Kanji/Setup Dark UI Assets` dos veces seguidas no reimporta nada la segunda.
- El test de `LinkHex` va en T2b (usa el tema por defecto).

---

## T2a — `SceneBuilder` temático
Cubre: plan T2 (SceneBuilder sin `Hex`, sin `UISprite.psd`, sin literales; ningún fondo claro; sin tofu); diseño T1.

### Ficheros
Modificar solo `Scripts/Editor/SceneBuilder.cs`; regenerar con `Kanji/Build S01 Scene` (por MCP/`unity command`, no a mano).

### Cambios
- Borrar las 8 constantes y `Hex(...)`. Nuevo `private static UiTheme _theme;` cargado en `Build()` tras `FontSetup.Setup(); DarkUiAssetSetup.Setup();` con `AssetDatabase.LoadAssetAtPath<UiTheme>(DarkUiAssetSetup.ThemePath)`; si es null, `Debug.LogError` y `return`.
- `_regular/_bold` salen de `_theme.Regular/Bold`.
- `NewButton(string name, Transform parent, string label, float height, ButtonStyle style)` con `enum ButtonStyle { Primary, Secondary, Success, Danger }` (privado en SceneBuilder). Sprite `_theme.ButtonSprite`, `Image.Type.Sliced`, `pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier`; `ColorBlock` normal/pressed del par de tokens del estilo; label `TextOnAccent` (Primary/Success/Danger) o `TextPrimary` (Secondary); tamaño `_theme.ButtonSize`.
- Mapeo de tokens: cámara y root de pantalla `Background`; top bar, panel de popup, `Info`, `PreviewPanel`, fila de historia `Surface` (pressed `SurfaceRaised`); toast `SurfaceRaised` + `TextPrimary`; aviso `WarningSurface` + `TextPrimary`; blocker `Scrim`; outline de foco `TextPrimary`; panel de popup y fila con `_theme.PanelSprite` (Sliced).
- Botón Atrás de la top bar: área 144×144 transparente (`Color.clear`, raycast) con hijo `Image` `_theme.IconBack` (Simple, Preserve Aspect, 72 px, color `TextPrimary`) en lugar del texto `"<"`.
- Tamaños: todos los `96/56/52/48/44/36/220` pasan a `_theme.*Size`.
- `ReaderScreen._linkColor` se rellena temporalmente con `_theme.Link` (lo elimina T2b).
- Permitido como literal técnico: solo `Color.clear` para capturadores de arrastre.

### Verificación
- Grep en `SceneBuilder.cs`: 0 resultados de `Hex(`, `UISprite`, `#[0-9A-F]{6}`, `new Color(`.
- Escena regenerada; recorrido manual de Inicio, Cargar, Biblioteca (+ popups), Lector: ningún fondo claro, japonés sin tofu. Tests existentes en verde.

---

## T2b — Pantallas leen `UiTheme` + test del enlace cian
Cubre: plan T2 (campos `Color` eliminados, enlaces vía `LinkHex`, test PlayMode, `Core` sin cambios); diseño T1 (kanji cian, contraste ya verificado por ux-ui en `valoracion-ux.md`: 8,8:1 Link y 6,8:1 TextSecondary sobre Surface).

### Ficheros
| Acción | Ruta |
|---|---|
| Modificar | `Scripts/UI/ReaderScreen.cs`, `Scripts/UI/LoadStoriesScreen.cs`, `Scripts/UI/StoryListScreen.cs`, `Scripts/Editor/SceneBuilder.cs` (3 líneas de cableado) |
| Crear | `Scripts/UI/AssemblyInfo.cs` (`[assembly: InternalsVisibleTo("Kanji.UI.Tests")]`) |
| Crear | `Tests/PlayMode/ReaderThemeTests.cs` |

### Cambios
- Las tres pantallas: `[SerializeField] private UiTheme _theme;` y fuera `_linkColor`, `_hintColor`, `_errorColor`, `_emptyColor`, `ToHex`. En `Init(...)`: si `_theme == null` → `Debug.LogError(..., this)`.
- `ReaderScreen`:
  ```csharp
  internal static StoryRichTextBuilder CreateBuilder(UiTheme theme, int maxChunkChars)
      => new StoryRichTextBuilder(theme.LinkHex, maxChunkChars);
  ```
  y `Open` usa `CreateBuilder(_theme, _maxChunkChars)`.
- `LoadStoriesScreen`: hint `_theme.TextSecondary`, error `_theme.Error`. `StoryListScreen`: vacío `_theme.TextSecondary`, error `_theme.Error`.
- `SceneBuilder`: `so.FindProperty("_theme").objectReferenceValue = _theme;` en los tres prefabs; borrar la línea de `_linkColor`.

### Tests (PlayMode, `Kanji.UI.Tests`)
- `DefaultTheme_LinkHex_IsCyan`: `CreateInstance<UiTheme>().LinkHex == "#22D3EE"`.
- `ReaderBuilder_UsesThemeLinkColor`: historia con un kanji (vía `StoryParser`) → `ReaderScreen.CreateBuilder(theme, 4000).Build(story)[0].Markup` contiene `<color=#22D3EE>`.
- `Kanji.Core.Tests` sin cambios.

---

## T3 — Popup de kanji de vista única (verde/rojo)
Cubre: plan T3; diseño T2 y supuestos 1, 2 y 4. Layout fino (orden de botones, icono/texto, márgenes): spec de ux-ui en `docs/ui/S05b-*.md`.

### Ficheros
| Acción | Ruta |
|---|---|
| Modificar | `Scripts/UI/KanjiPopup.cs`, `Scripts/UI/UiStrings.cs`, `Scripts/Editor/SceneBuilder.cs` (`BuildKanjiPopup`) |

### `KanjiPopup`
```csharp
public enum KanjiPopupResult { Knew, DidntKnow, Dismissed }

public sealed class KanjiPopup : PopupView
{
    [SerializeField] TMP_Text _kanji, _sentence, _reading, _meaning, _sentenceMeaning;
    [SerializeField] GameObject _rowReading, _rowMeaning, _rowSentenceMeaning;
    [SerializeField] Button _btnKnew, _btnDidntKnow;
    [SerializeField] RectTransform _secondaryActions; // hueco S06, vacío e inactivo
    [SerializeField] ScrollRect _body; [SerializeField] LayoutElement _bodyLayout;
    [SerializeField] RectTransform _bodyContent; [SerializeField] float _maxBodyHeight = 1100f;

    public void Show(KanjiEntry entry, Action<KanjiPopupResult> onClosed = null);
    public void Close();                 // = Resolve(Dismissed); no-op si no está abierto
    public override void Cancel() => Resolve(KanjiPopupResult.Dismissed); // fuera / Atrás
    private void Resolve(KanjiPopupResult r); // guard _resolved; Popups.Close(this); onClosed?.Invoke(r)
}
```
- `Show` rellena todo de una vez: kanji, frase y las tres filas con `SetRow` (vacíos ocultos); `FitBody()` como hoy. Desaparecen `_details`, `_btnShowMore`, `_btnClose`, `ShowDetails`.
- Verde → `Knew`, rojo → `DidntKnow`; ambos cierran. **Nadie registra nada**: `ReaderScreen` sigue llamando `Show(entry)` sin callback. El enum solo deja la puerta abierta a S07 (`ReadingSession`) sin reescribir el popup.
- El scroll del lector no se toca (el popup no conoce al lector).

### SceneBuilder (`BuildKanjiPopup`)
`Panel` (VLG): `Body` (scroll: `Kanji`, `Sentence`, `RowReading`, `RowMeaning`, `RowSentenceMeaning` directamente en `BodyContent`, sin `Details`) → `SecondaryActions` (RectTransform + VLG, **inactivo**, S06 mete ahí "Añadir a complicados") → `AnswerRow` (HLG, spacing 32, dos hijos `flexibleWidth = 1`, alto 160 ≥ 144 px): `BtnDidntKnow` (`ButtonStyle.Danger`, `UiStrings.DidntKnow`) y `BtnKnew` (`ButtonStyle.Success`, `UiStrings.Knew`), con icono si ux-ui lo pide.

`UiStrings`: añadir `Knew = "Lo sabía"`, `DidntKnow = "No lo sabía"`; borrar `ShowMore` (y `Close` solo si ya no tiene usos).

### Tests / verificación
Sin test automático nuevo (montar el popup a mano en un test cuesta más que lo que aporta). Verificación manual (y T5): campos vacíos ocultos; verde, rojo, fuera y Atrás cierran; scroll del lector conservado; botones ≥ 144 px de alto. `PopupServiceTests` sigue en verde.

---

## T4a — Fundido entre pantallas + bloqueo de entrada
Cubre: plan T4 (fundido 0,2 + 0,2 s, toques y Atrás ignorados, salir sin fundido, duraciones configurables, test); diseño T3 y supuesto 5.

### Patrón
- **Estrategia** `IScreenTransition` inyectada en `ScreenNavigator` (sigue siendo C# puro y testeable). La pila lógica cambia **en el acto**; solo el cambio visual (`OnHide`/`OnShow`) se difiere. Así `Pop()`, `CanGoBack` y `Current` siguen siendo síncronos y correctos.
- **`InputGate`** (C# puro, `Kanji.Core.Navigation`, junto a `NavigationStack`): única verdad de "hay una transición en curso". La aplica a la UI un único MonoBehaviour.

### Ficheros
| Acción | Ruta |
|---|---|
| Crear | `Scripts/Core/Navigation/InputGate.cs` |
| Crear | `Scripts/UI/IScreenTransition.cs`, `Scripts/UI/TransitionOverlay.cs` |
| Modificar | `Scripts/UI/ScreenNavigator.cs`, `Scripts/UI/BackButtonRouter.cs`, `Scripts/App/AppBootstrap.cs`, `Scripts/Editor/SceneBuilder.cs` |
| Crear | `Tests/EditMode/Core/InputGateTests.cs`, `Tests/PlayMode/ScreenNavigatorTransitionTests.cs` |

### APIs
```csharp
// Kanji.Core.Navigation
public sealed class InputGate
{
    public InputGate(Func<double> now);
    public bool IsBlocked { get; }          // _holds > 0 || now() < _until
    public event Action Blocked;            // se emite al pasar de libre a bloqueado
    public void BlockFor(double seconds);   // _until = max(_until, now + seconds) (popups, T4b)
    public void Hold();  public void Release(); // contador (transición de pantalla)
}

// Kanji.UI
public interface IScreenTransition { void Run(Action swap); }

public sealed class ScreenNavigator
{
    public ScreenNavigator(IReadOnlyDictionary<ScreenId, IScreen> screens, IScreenTransition transition = null);
    // Push/Pop/Replace: from = Current; muta _stack; to = Current;
    // si from == null o transition == null -> Apply() inmediato; si no, transition.Run(Apply).
    // Apply: from?.OnHide(); to.OnShow(args); Changed?.Invoke(toId).
}

public sealed class TransitionOverlay : MonoBehaviour, IScreenTransition
{
    [SerializeField] private Image _fade;            // full-screen, FadeColor, raycastTarget=false, alpha 0
    [SerializeField] private CanvasGroup _inputGroup; // CanvasGroup del Canvas raíz
    public void Init(InputGate gate, float fadeOutSeconds, float fadeInSeconds);
    public void Run(Action swap); // encola; si no hay otra en curso arranca la corrutina
}
```
- `TransitionOverlay.Run`: `gate.Hold()` → alpha 0→1 en `fadeOut` (tiempo no escalado) → `swap()` (en negro: aquí corre el `OnShow` pesado del lector) → 1→0 en `fadeIn` → si la cola tiene otra, la encadena; si no, `gate.Release()`. Un `swap` que llegue durante otra transición se encola (caso: `ReaderScreen` hace `Pop()` por error de carga durante el fundido de entrada).
- Bloqueo de toques: `TransitionOverlay` se suscribe a `gate.Blocked` y pone **en el acto** `_inputGroup.blocksRaycasts = false`; en `Update` lo devuelve a `true` cuando `!gate.IsBlocked`. Se usa el `CanvasGroup` del Canvas raíz (no una imagen encima) porque el viewport del lector tiene sub-canvas con su propio `GraphicRaycaster`.
- `BackButtonRouter.Init(ScreenNavigator, PopupService, Action quit, InputGate gate)`: tras detectar la tecla, `if (_gate.IsBlocked) return;` (antes de popups/pila, para no salir de la app por un `Pop` en curso).
- Salir sin fundido: `HomeScreen` y el `quit` del router siguen llamando `Application.Quit` directamente. Arranque sin fundido: el primer `Push(Home)` tiene `from == null`.
- `AppBootstrap`: nuevos `[SerializeField] UiTheme _theme; [SerializeField] TransitionOverlay _transition;`. Crea `var gate = new InputGate(() => Time.unscaledTimeAsDouble);`, `_transition.Init(gate, _theme.ScreenFadeOutSeconds, _theme.ScreenFadeInSeconds)`, `new ScreenNavigator(screens, _transition)`, router con `gate`. Guarda `gate` para T4b.
- `SceneBuilder.BuildScene`: `CanvasGroup` en `Canvas`; hijo `TransitionOverlay` como **último hijo del Canvas** (encima de `PopupLayer`), estirado, `Image` color `_theme.FadeColor` con alpha 0; cablear `_fade`, `_inputGroup` y en `AppBootstrap` `_theme`, `_transition`.

### Tests
- EditMode `InputGateTests` (reloj falso): libre al inicio; `BlockFor(0.15)` bloquea hasta 0,15 y libera después; un `BlockFor` más corto no acorta; `Hold` bloquea indefinidamente hasta `Release`; `Blocked` se emite una vez al pasar de libre a bloqueado.
- PlayMode `ScreenNavigatorTransitionTests` (pantallas falsas `IScreen` + transición falsa que guarda los `swap`): tras `Push(B)` `Current == B` pero `B.OnShow` aún no llamado; al ejecutar el swap: `A.OnHide` y `B.OnShow` en ese orden; dos peticiones seguidas se aplican en orden; sin transición (o `from == null`) es inmediato.

---

## T4b — Popups: fundido al cerrar y apilado con ocultación
Cubre: plan T4 (fundido 0,15 s al abrir y cerrar, el de debajo oculto y no interactuable, reaparece intacto, doble toque no abre dos popups); diseño T3.

### Ficheros
| Acción | Ruta |
|---|---|
| Modificar | `Scripts/UI/PopupService.cs`, `Scripts/UI/PopupView.cs`, `Scripts/App/AppBootstrap.cs`, `Scripts/Editor/SceneBuilder.cs` (CanvasGroup en el blocker) |
| Modificar | `Tests/PlayMode/PopupServiceTests.cs` |

### APIs
```csharp
public sealed class PopupService
{
    public PopupService(GameObject blocker, Transform keepOnTop = null, InputGate gate = null, float fadeSeconds = 0f);
    public void Open(PopupView popup);   // gate?.BlockFor(fade); popups de debajo -> SetCovered(true);
                                         // activa, Reorder, FadeIn (con el scrim si la pila estaba vacía)
    public void Close(PopupView popup);  // saca de la pila al instante; gate?.BlockFor(fade);
                                         // nuevo top -> SetCovered(false) (reaparece con fundido);
                                         // el cerrado hace FadeOut (con el scrim si la pila queda vacía) y al
                                         // terminar SetActive(false) y, si la pila sigue vacía, apaga el blocker.
}

public abstract class PopupView : MonoBehaviour   // [RequireComponent(typeof(CanvasGroup))]
{
    internal void FadeIn(float seconds, CanvasGroup companion);
    internal void FadeOut(float seconds, CanvasGroup companion, Action onDone);
    internal void SetCovered(bool covered); // true: para fundido, alpha 0, interactable/blocksRaycasts false
                                            // false: interactable/blocksRaycasts true + FadeIn
    // seconds <= 0 -> inmediato y síncrono (tests). OnDisable: fija alpha final y llama al onDone pendiente.
}
```
- Se elimina la constante `FadeSeconds` y `PlayOpenFade` de `PopupView`; la duración viene de `PopupService` (inyectada desde `_theme.PopupFadeSeconds` por `AppBootstrap`).
- Durante su `FadeOut` el popup que se cierra tiene `blocksRaycasts = false` y queda por encima del blocker (`SetAsLastSibling` tras `Reorder`, por debajo de `keepOnTop`). Reabrir un popup en pleno `FadeOut` cancela el fundido y su `onDone`.
- El scrim (blocker) tiene `CanvasGroup` (lo añade `SceneBuilder`) y se funde junto al primer popup que abre / último que cierra; si no tiene `CanvasGroup` (tests), `companion = null`.
- "Intacto": el de debajo no se desactiva (no `SetActive(false)`), solo se oculta por `CanvasGroup`; conserva textos, scroll y callbacks.
- Doble toque: `Open`/`Close` bloquean la entrada 0,15 s vía `InputGate` (raycasts del Canvas fuera), y el scrim ya tapa lo de debajo.
- `AppBootstrap`: `new PopupService(_popupBlocker, _toast.transform.parent, gate, _theme.PopupFadeSeconds)`.

### Tests (PlayMode, `fadeSeconds = 0`)
- Existente `TwoPopups_BlockerSitsBetweenLowerAndTop` sigue en verde.
- `Stacked_LowerIsHiddenAndNotInteractable`: abrir A y C → A `alpha == 0`, `blocksRaycasts == false`, sigue activo.
- `CloseTop_LowerReappearsIntact`: cerrar C → A `alpha == 1`, `blocksRaycasts == true`; C inactivo; blocker activo.
- `CloseLast_HidesBlocker`.
- `OpenAndClose_BlockInput`: con `InputGate` de reloj falso y `fadeSeconds = 0.15`, tras `Open` `gate.IsBlocked`.

---

## Notas para ARQUITECTURA.md (lo aplico yo al cerrar el sprint)
`Scripts/UI/Theming/`, `Art/DarkUI/`, `Settings/UiTheme_Dark.asset`; `KanjiPopup` de vista única; `InputGate`, `IScreenTransition`/`TransitionOverlay`; enlace al ADR 0005.
