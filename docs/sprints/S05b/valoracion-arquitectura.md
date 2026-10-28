# S05b — Valoración de arquitectura: tema oscuro centralizado (`UiTheme`)

## Situación actual (grep)
- `Editor/SceneBuilder.cs`: 8 tokens `Hex("#...")` (L21-28) + literales sueltos (overlay `0,0,0,.55` L95, aviso `#FBE9E7` L545, pressed secundario `#CFCABF` L869, link `#0B57D0` L525), sprite `UI/Skin/UISprite.psd` built-in (L138, 609, 859), fuentes cargadas por ruta de `FontSetup` (L37-38), tamaños `44`, `96..220`.
- `UI/ReaderScreen.cs` `_linkColor` (L30) → `ToHex` → `StoryRichTextBuilder(string linkColorHex)` (Core, ya recibe hex: **Core no cambia**).
- `UI/LoadStoriesScreen.cs` `_hintColor/_errorColor`, `UI/StoryListScreen.cs` `_emptyColor/_errorColor`: duplican tokens con valores ligeramente distintos.
- El paquete "Dark UI" trae ~400 PNG (la mayoría iconos "New Icons"), prefabs de demo/panel y escenas; **no trae fuentes** → seguimos con Noto Sans JP.

## Propuesta: `UiTheme` (ScriptableObject)
- Asmdef **`Kanji.UI`**, `Scripts/UI/Theming/UiTheme.cs`. Asset único `Assets/_Project/Settings/UiTheme_Dark.asset` (carpeta ya prevista en ARQUITECTURA).
- Campos (solo lo que hoy se usa, sin sobrediseñar):
  - Color: `Background, Surface, SurfaceRaised, TextPrimary, TextSecondary, Primary, PrimaryPressed, Secondary, SecondaryPressed, Disabled, Overlay, Link, Error, WarningSurface`.
  - Sprite (9-slice): `ButtonSprite, InputSprite, PanelSprite, DividerSprite`; iconos: `IconClose, IconBack` (lista final la cierra ux-ui).
  - `TMP_FontAsset Regular, Bold`.
  - Tamaños: `TitleSize, BodySize, CaptionSize, KanjiMinSize, KanjiMaxSize` (ya repetidos en SceneBuilder).
  - Helper `string LinkHex` (`#RRGGBB`) para el rich text. `OnValidate` avisa si falta algún sprite/fuente.
- Valores concretos de color/contraste: los decide ux-ui (`docs/ui/`); exigir contraste ≥ 4.5:1 de `Link` y `TextSecondary` sobre `Surface`.

## Acceso / inyección
- **Se aplica en construcción, no en runtime**: `SceneBuilder` carga el asset (`AssetDatabase.LoadAssetAtPath<UiTheme>`) y lo usa en lugar de las constantes `Hex(...)`, del `UISprite.psd` y de las rutas de fuentes. Los colores/sprites quedan horneados en prefabs y escena, coherente con el flujo actual (escena reproducible por script).
- **Sin `ThemedImage`/`ThemedText`** por ahora: no hay requisito de cambiar de tema en caliente; añadirían un componente por nodo y coste de `Awake`. Puerta abierta: si un día hay tema claro/oscuro conmutable, se añaden esos componentes leyendo el mismo `UiTheme` sin tocar las pantallas (ADR lo registra).
- Colores usados en runtime (link, hint, error, empty): las tres pantallas sustituyen sus campos `Color` por `[SerializeField] private UiTheme _theme;` cableado por `SceneBuilder` (referencia a asset, no singleton). `AppBootstrap` no cambia.
- **Kanji en el lector**: `ReaderScreen` hace `new StoryRichTextBuilder(_theme.LinkHex, _maxChunkChars)`. El color del texto base sale del `TMP_Text.color` (= `TextPrimary`, puesto por SceneBuilder); el `<color>` del builder solo pinta los enlaces. Revisar que el `<link>` no lleve otro color fijo y que el resaltado de kanji difícil (si existe) use otro token.

## Importación del paquete
- Importar **solo el subconjunto** (desmarcar en el diálogo `Demo/`, escenas, `Prefabs *`, `Ribbons/`, `New Icons/` salvo los iconos elegidos) y mover a `Assets/_Project/Art/DarkUI/{Buttons,Inputs,Panels,Icons}/` con nombres propios en inglés (`btn_rounded.png`…). Conservar licencia/`Documentation.txt` en esa carpeta. Nada de `Assets/Dark UI` entero: los prefabs del paquete arrastran fuentes/estilos ajenos y no siguen nuestras convenciones.
- Import settings (aplicados por script Editor reproducible, como `FontSetup`): Texture Type `Sprite (2D and UI)`, Sprite Mode `Single`, Mesh Type `Full Rect` (obligatorio para 9-slice), `spriteBorder` fijado por script, PPU 100 (ajuste fino con `Image.pixelsPerUnitMultiplier`), sin mipmaps, Wrap `Clamp`, Filter `Bilinear`, Alpha is Transparency, Max Size 256 (iconos 128).
- **Sprite Atlas: sí**, uno (`DarkUI.spriteatlasv2`), padding 4, sin rotación ni tight packing (rompen 9-slice), compresión Android **ASTC 6x6** (4x4 si los bordes se ven sucios). Agrupa todos los sprites de UI en 1 textura → menos draw calls en uGUI.

## Impacto
- Tests: `Kanji.Core.Tests` sin cambios (builder ya toma hex). PlayMode: revisar asserts sobre colores si los hay; añadir 1 test que compruebe que `ReaderScreen` genera el `<color=...>` con `theme.LinkHex`. Validación del asset por `OnValidate`.
- APK: solo entra lo referenciado; ~15-25 sprites en un atlas 512² ASTC 6x6 ≈ 120 KB. Sin fuentes nuevas. Despreciable. Importar el paquete entero no infla el APK pero sí el repo y el tiempo de import.

## ADR nuevo
`docs/arquitectura/adr/0005-tema-visual-uitheme.md`: un `UiTheme` SO en `Kanji.UI` como única fuente de colores/sprites/fuentes/tamaños, aplicado en construcción por `SceneBuilder`; assets de terceros solo como subconjunto en `_Project/Art/`; alternativas descartadas (componentes `Themed*` en runtime, singleton estático `Theme.Current`, usar prefabs del paquete). Al cerrar, actualizar ARQUITECTURA (carpeta `Art/`, `Theming/`).

## Troceo propuesto (≤4 tareas, ≤3 scripts)
1. **T1 `UiTheme`**: `UiTheme.cs` + asset `UiTheme_Dark` con valores de ux-ui + ADR 0005. (1 script)
2. **T2 Importación**: subconjunto en `Art/DarkUI`, `Editor/DarkUiAssetSetup.cs` (import settings + bordes 9-slice + atlas), referenciar sprites en el asset. (1 script)
3. **T3 SceneBuilder temático**: `SceneBuilder.cs` usa `UiTheme` (colores, sprites, fuentes, tamaños), elimina `Hex` y `UISprite.psd`; regenerar escena/prefabs. (1-2 scripts)
4. **T4 Pantallas runtime**: `ReaderScreen.cs`, `LoadStoriesScreen.cs`, `StoryListScreen.cs` leen de `_theme`; + test PlayMode del link. (3 scripts + 1 test)

Dependencias: T1 → T2 → T3; T4 tras T1 (en paralelo con T2/T3, pero el cableado `_theme` lo hace T3).
