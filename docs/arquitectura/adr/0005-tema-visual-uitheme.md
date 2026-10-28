# ADR 0005 — Tema visual centralizado (`UiTheme`) aplicado en construcción

Estado: aceptada (S05b).

## Contexto
La app pasa a tema oscuro con el paquete de terceros "Dark Theme UI" (Giniel Villacote). Hoy los colores están duplicados: 8 `Hex(...)` y literales sueltos en `Editor/SceneBuilder.cs`, y campos `Color` en `ReaderScreen`, `LoadStoriesScreen` y `StoryListScreen` con valores ligeramente distintos. Los sprites son el `UISprite.psd` built-in y las fuentes se cargan por ruta. Toda la UI se genera por script (`SceneBuilder`), y la escena/prefabs son reproducibles.

## Decisión
1. **Una sola fuente de verdad**: `UiTheme : ScriptableObject` en `Kanji.UI` (`Scripts/UI/Theming/UiTheme.cs`), asset único `Assets/_Project/Settings/UiTheme_Dark.asset`. Contiene tokens de color, sprites 9-slice/iconos, fuentes TMP, tamaños de texto y duraciones de animación. Los valores por defecto del código son la paleta oscura (un `CreateInstance` sirve en tests).
2. **Aplicado en construcción**: `SceneBuilder` carga el asset y hornea colores/sprites/fuentes/tamaños en escena y prefabs. No hay componentes `Themed*` en runtime.
3. **Lectura en runtime solo donde el color depende del estado** (enlace de kanji, mensajes de error/hint/vacío) y para duraciones: las vistas reciben el asset como `[SerializeField] private UiTheme _theme` (cableado por `SceneBuilder`) o por `Init(...)` desde `AppBootstrap`. Nada de acceso estático.
4. **Assets de terceros solo como subconjunto** copiado y renombrado (inglés) en `Assets/_Project/Art/DarkUI/{Buttons,Inputs,Panels,Icons}/` con su licencia; nada de prefabs, escenas, demo ni fuentes del paquete. Import settings y Sprite Atlas (`DarkUI.spriteatlasv2`) fijados por un script Editor reproducible (`DarkUiAssetSetup`), como `FontSetup`.
5. `Kanji.Core` no conoce el tema: sigue recibiendo el color del enlace como `#RRGGBB` (`UiTheme.LinkHex`).

## Alternativas descartadas
- **Componentes `ThemedImage`/`ThemedText` en runtime**: permiten cambiar de tema en caliente, pero no hay ese requisito; añaden un componente por nodo y coste en `Awake`.
- **Singleton/estático `Theme.Current`**: viola la regla de "sin estáticos con estado" y oculta la dependencia.
- **Usar los prefabs del paquete**: traen fuentes sin CJK y estilos/convenciones ajenos; romperían la reproducibilidad del `SceneBuilder`.
- **Importar el paquete entero**: ~400 PNG y 5,6 MB de ruido en repo e import; solo entra al APK lo referenciado, pero no aporta nada.

## Consecuencias
- Cambiar un color = editar el asset y regenerar escena (`Kanji/Build S01 Scene`); sin tocar código.
- Puerta abierta: un tema claro conmutable se añade con un segundo asset + componentes `Themed*` que lean el mismo `UiTheme`, sin cambiar pantallas ni `Core`.
- Si alguien edita a mano colores en la escena, se pierden al regenerar (igual que hoy).
- 9-slice exige `Mesh Type = Full Rect` y atlas sin rotación ni tight packing; lo garantiza `DarkUiAssetSetup`.
- La licencia del paquete viaja con el subconjunto en `Art/DarkUI/`.
