# S05 — Arquitectura

> Dueño: arquitecto. Base: `docs/arquitectura/ARQUITECTURA.md`, ADR 0003 (uGUI + TMP), `docs/sprints/S04/arquitectura.md` (patrones `ScreenView`, `PopupView`, `PopupService`, `ScreenNavigator`, `SceneBuilder`). UI concreta (layout, tamaños, color azul, textos): especificación única de ux-ui en `docs/ui/` (pendiente antes de T21). Aquí solo contratos, responsabilidades y medidas.
> Convención de S04: "≤ 3 scripts" cuenta scripts de producción. No cuentan: tests/fakes/helpers de test, constantes en `UiStrings`, el cableado de 1–3 líneas en `AppBootstrap` cuando la tarea ya tiene 3 scripts, ni `Editor/SceneBuilder.cs` (herramienta de Editor; la escena y los prefabs se regeneran con su menú vía MCP/`unity command`, nunca a mano).

## Hallazgos del código actual que condicionan el sprint
1. `ARQUITECTURA.md` situaba `StoryRichTextBuilder` en `Kanji.UI`. Se mueve a **`Kanji.Core`** (genera *strings*, no necesita TMP; así es testeable en EditMode como pide el plan). Actualizado en `ARQUITECTURA.md`.
2. `StoryFixtureGenerator.Generate()` produce **250 bloques** (200 kanjis distintos + 50 repeticiones). **Resuelto por el diseñador (GDD 2.4):** criterio "un enlace por bloque (250) y 200 kanjis distintos": nº de enlaces == `story.Entries.Count` == `fixture.BlockCount` (250) y kanjis distintos enlazados == 200.
3. El parser crea **una `KanjiEntry` por bloque** y `Segment.ForKanji(kanji, entryIndex)` apunta a ella: el id del enlace = `EntryIndex` ya garantiza "cada aparición abre su bloque" sin lógica extra.
4. `ScreenNavigator.Push` llama a `OnShow` de forma síncrona; hacer `Pop()` dentro de `OnShow` sería reentrante. Por eso la carga del lector va en corrutina y el fallo vuelve a la lista **un frame después**.
5. TMP interpreta por defecto secuencias `\n`, `\t`, `\u....` escritas literalmente en el texto (`parseCtrlCharacters`). Un `.txt` con `\n` literal se vería como salto de línea: **todos los TMP que muestran contenido del usuario llevan `parseCtrlCharacters = false`**.

---

## T20 — `StoryRichTextBuilder` (Core, EditMode)
**Requisitos:** GDD 2.4 (saltos de línea, bloque -> Kanji azul pulsable, resto normal), GDD 5.1, GDD 7 (50 000 car./200 kanjis), plan T20.

### Contrato (`Assets/_Project/Scripts/Core/Reading/StoryRichTextBuilder.cs`, namespace `Kanji.Core.Reading`)
```csharp
public readonly struct RichTextChunk {
    public string Markup { get; }        // rich text TMP listo para asignar a .text
    public int VisibleLength { get; }    // caracteres UTF-16 visibles (sin etiquetas)
    public int LinkCount { get; }
    public bool BreakAfter { get; }      // true si el corte consumió un '\n' (el salto lo pone el cambio de TMP)
}
public sealed class StoryRichTextBuilder {
    public const int DefaultMaxChunkChars = 8000;
    public const int MinMaxChunkChars = 256;
    public StoryRichTextBuilder(string linkColorHex, int maxChunkChars = DefaultMaxChunkChars); // "#RRGGBB"; si no -> ArgumentException; max < 256 -> ArgumentOutOfRangeException
    public IReadOnlyList<RichTextChunk> Build(Story story);   // story null -> ArgumentNullException; historia vacía -> lista vacía
    public static string LinkIdFor(int entryIndex);           // "k" + índice (InvariantCulture)
    public static bool TryParseLinkId(string linkId, out int entryIndex); // inversa; false si no es "k<n>" con n >= 0
}
```
- Sin estado entre llamadas, sin `UnityEngine`. Un solo `StringBuilder` por chunk; capacidad inicial ≈ `max * 1.2`.

### Formato de salida
- **Enlace** (segmento `Kanji`): `<link="k{EntryIndex}"><color=#RRGGBB><noparse>{Kanji}</noparse></color></link>`. El valor del kanji pasa por el mismo escape que el texto. Subrayado u otros adornos: solo si ux-ui los pide (se añadirían aquí, en un sitio).
- **Texto literal** (segmento `Text`): se envuelve en `<noparse>…</noparse>`. **Regla de escape:** cada `<` del texto literal se emite como `<` + `</noparse><noparse>`. Así el `<` queda justo delante de un cierre de `noparse`; TMP no puede formar etiqueta con él (la búsqueda de etiqueta se corta al encontrar otro `<`) y se pinta literal. Esto cubre `<b>`, `<link=…>`, `</noparse>` (en cualquier mayúscula/minúscula) y `<` al final del texto, con una regla única y sin comparar cadenas. `>` y `\` no necesitan escape (con `parseCtrlCharacters = false` en el TMP).
- **Plan B si el test real de TMP (abajo) falla:** dentro de `<noparse>`, insertar U+200B (zero width space) tras cada `<` (`<​/noparse>` ya no es etiqueta). Solo cambia el método privado de escape.
- **Saltos de línea:** `\r\n` y `\r` sueltos se normalizan a `\n` al construir (el texto "visible" de referencia de los tests es esa normalización).

### Troceo
- Unidad = párrafo (texto hasta un `\n`, inclusive). Se acumulan párrafos mientras `VisibleLength` ≤ `maxChunkChars`. Al cortar en un `\n`, ese `\n` se **consume** (`BreakAfter = true`); los `\n` adicionales (líneas en blanco) se conservan en el trozo.
- Nunca se corta dentro de un enlace (un enlace es indivisible) ni entre un par suplente (kanji de extensión B).
- **Párrafo > `maxChunkChars`** (texto sin saltos): se corta en el último límite suave antes del máximo (`。`, `！`, `？`, `、`, espacio, fin de enlace); si no hay, corte duro en el máximo. `BreakAfter = false`. Limitación conocida: el siguiente trozo empieza en línea nueva (un TMP por trozo). Es el único caso en que se parte un párrafo; sin este corte TMP rompe (> ~16 k glifos). **Resuelto por el diseñador (GDD 2.4):** se acepta, sin perder, duplicar ni reordenar caracteres ni partir enlaces o pares suplentes.

### Tests (EditMode, `Kanji.Core.Tests`)
- `Assets/_Project/Tests/EditMode/Core/Reading/StoryRichTextBuilderTests.cs`.
- Helper de test `Assets/_Project/Tests/EditMode/Core/Fixtures/TmpVisibleText.cs`: `string Strip(string markup)` que simula el parser de TMP para las etiquetas que emitimos (fuera de `noparse` quita `<link…>`, `</link>`, `<color…>`, `</color>`, `<noparse>`; dentro de `noparse` todo es literal salvo la secuencia exacta `</noparse>`, insensible a mayúsculas). Y `IReadOnlyList<string> LinkIds(string markup)`.
- Casos: `PlainText_PreservesLineBreaks`, `CrLf_NormalizedToLf`, `KanjiSegment_EmitsLinkWithColorAndEntryIndex` (markup exacto), `RepeatedKanji_EachOccurrenceHasOwnIndex`, `HostileMarkup_IsLiteral` (`<b>x</b>`, `</noparse>`, `</NoParse>`, `<noparse>`, `<link="k0">`, `<<`, `<` final, `\n` literal con barra) -> `Strip == fuente`, `NoKanji_NoLinks`, `EmptyStory_NoChunks`, `Chunks_RespectMax_AndNeverSplitLinks`, `LongParagraph_CutAtSoftBreak`, `LongParagraph_NoSoftBreak_HardCutNoSurrogateSplit`, `RoundTrip_ConcatWithBreakAfter_EqualsNormalizedText`, `TryParseLinkId_RoundTrip_AndRejectsGarbage`, `Ctor_InvalidColorOrMax_Throws`.
- **Fixture S03:** `var fx = StoryFixtureGenerator.Generate(); var story = new StoryParser().Parse("fixture", fx.Text).Story;` (o el nombre de propiedad real de `ParseResult`) -> `Build_Fixture50k_KeepsAllTextAndLinks`: texto visible reconstruido == segmentos concatenados normalizados; enlaces == `fx.BlockCount`; índices 0..N-1 en orden; kanjis distintos == 200; todos los trozos ≤ máximo. `Build_Fixture50k_UnderBudget`: < 50 ms en PC, tiempo al log del test.
- **Test real de TMP** (verifica la regla de escape contra TMP, no contra nuestra simulación): `Assets/_Project/Tests/PlayMode/RichTextTmpEscapeTests.cs` (`Kanji.UI.Tests`, ya referencia `Kanji.Core` y `Unity.TextMeshPro`): crea Canvas + `TextMeshProUGUI` (`parseCtrlCharacters = false`), asigna el `Markup` de las cadenas hostiles, `ForceMeshUpdate()` y comprueba `GetParsedText()` == fuente y `textInfo.linkCount` esperado. Si falla, se aplica el plan B.
- **Preparación T24:** test `[Explicit]` `ExportPerfFixture` que escribe `fx.Text` en `Assets/_Project/Tests/Fixtures/Text/perf-50k.txt` (UTF-8 sin BOM), y fichero a mano `hostile-markup.txt` en la misma carpeta (con bloques `{Kanji: …}` y las cadenas hostiles). El usuario los copia al móvil con `adb push`.
- 136 tests previos intactos.

**Scripts de producción (1):** `Core/Reading/StoryRichTextBuilder.cs` (incluye `RichTextChunk`). Un encargo.

---

## T21 — `ReaderScreen` real
**Requisitos:** GDD 2.4 (desplazable, saltos, azul, arranca arriba, Atrás), alcance S05 (relee del disco; error -> mensaje y lista; Cerrar/Atrás -> lista), GDD 7 (< 2 s, scroll fluido, sin tofu), plan T21, plan T17 (lista en su misma posición).

### Contrato
```csharp
// UI/ReaderScreen.cs : ScreenView
[SerializeField] Button _backButton, _closeStoryButton;  // según ux-ui (Atrás de la barra / "Cerrar historia")
[SerializeField] TMP_Text _title;                         // richText = false
[SerializeField] ScrollRect _scroll; [SerializeField] RectTransform _content;
[SerializeField] TextMeshProUGUI _chunkTemplate;          // hijo inactivo de _content
[SerializeField] Color _linkColor;                        // valor de ux-ui
[SerializeField] int _maxChunkChars = 4000;               // ajustable tras T24 sin tocar código
public void Init(ScreenNavigator nav, StoryLibrary library, ToastView toast);   // T22 añade KanjiPopup
public override void OnShow(object args);   // args = StoryInfo (contrato de S04 sin cambios)
public override void OnHide();
```
### Flujo
1. `OnShow`: `base.OnShow`, limpia trozos (texto vacío + desactivar, se quedan en el pool), `_content.anchoredPosition = 0`, `_scroll.StopMovement()`, título = `info.Name`, arranca la corrutina `Open(info)`. Si `args` no es `StoryInfo` -> mismo camino de error.
2. Corrutina: `yield return null` (sale de `Push`; evita reentrada) -> `library.Load(info.Id)` **síncrono** (JSON ≈ 200 KB: decenas de ms; la convención de `Task.Run` solo si T24 mide > 200 ms en `loadMs`) -> `new StoryRichTextBuilder(hex(_linkColor), _maxChunkChars).Build(story)`. Todo dentro de `try/catch (Exception)`.
3. Error (`!result.IsSuccess`, excepción o `Value == null`): `Debug.LogWarning` con `Error`/excepción, `toast.Show(UiStrings.StoryOpenFailed)` ("No se pudo abrir la historia."), `nav.Pop()`. La lista conserva su scroll por el mecanismo de S04 (no cambia `Revision`).
4. Relleno **un trozo por frame** (el primero en el mismo frame que la carga, para que se vea texto cuanto antes): instancia del pool o `Instantiate(_chunkTemplate, _content)`, `text = chunk.Markup`, `SetActive(true)`. Se guarda la `Story` actual en un campo (T22 la usa para mapear enlaces).
5. Historia sin trozos (vacía): pantalla con el área de texto vacía, sin error.
6. `OnHide`: para la corrutina, vacía textos (libera mallas), suelta la referencia a la `Story`, `base.OnHide`. Atrás/Cerrar historia -> `nav.Pop()` (Atrás llega por `BackButtonRouter`; `HandleBack` por defecto = Pop).

### Maquetación (decisión: VerticalLayoutGroup + ContentSizeFitter, no cálculo propio)
- `Content`: ancla y pivote arriba (0.5, 1), `VerticalLayoutGroup` (`childControlHeight = true`, `childForceExpandHeight = false`, `spacing = 0`) + `ContentSizeFitter` vertical `PreferredSize`. Con ~13 trozos la reconstrucción del layout es barata; el coste real es generar el texto TMP, que ocurre igual con cálculo propio. Los trozos nuevos se añaden **debajo**: con pivote arriba el contenido visible no salta.
- `Viewport`: `RectMask2D` (no `Mask`: recorta sin stencil y **descarta** los trozos fuera de vista) + `Image` transparente (recoge arrastres en huecos) + **`Canvas` anidado + `GraphicRaycaster`** para que el scroll solo re-agrupe este sub-canvas y no toda la UI.
- Plantilla de trozo `TextMeshProUGUI`: fuente Noto Sans JP SDF (sin tofu), `richText = true`, `parseCtrlCharacters = false`, `textWrappingMode = Normal`, `overflowMode = Overflow`, `enableAutoSizing = false`, `extraPadding = false`, `raycastTarget = true` (T22 lo necesita), `isTextObjectScaleStatic = true`.
- `_maxChunkChars = 4000` (no 8 000): trozos más pequeños reparten el coste entre frames y permiten que `RectMask2D` descarte más malla al hacer scroll. T24 decide el valor final.

### Medidas (`[Kanji][Perf]`, solo `DEVELOPMENT_BUILD || UNITY_EDITOR` con `[Conditional]`, método privado en `ReaderScreen`; `Stopwatch`)
- Apertura: `reader-open id=… chars=… chunks=… links=… loadMs=… buildMs=… firstVisibleMs=… totalMs=… frames=… monoMB=… reservedMB=… atlases=…`. `totalMs` = desde `OnShow` hasta el frame **siguiente** a asignar el último trozo (incluye el layout). `firstVisibleMs` = igual para el primer trozo. Memoria con `Profiler.GetMonoUsedSizeLong()` / `GetTotalReservedMemoryLong()`; `atlases` = nº de texturas del atlas dinámico de la fuente (detecta crecimiento del atlas por glifos nuevos).
- Scroll: en `Update`, mientras `_scroll.velocity` ≠ 0, acumula frames, frames > 33 ms y máximo; al `OnHide` log `reader-scroll frames=… over33ms=… maxMs=… avgMs=…`.

**Scripts (3):** `UI/ReaderScreen.cs`, `UI/UiStrings.cs` (`StoryOpenFailed`, `CloseStory` y los que fije ux-ui), `App/AppBootstrap.cs` (`reader.Init(navigator, library, _toast)`). `SceneBuilder.BuildReaderPrefab` reescrito (barra superior, ScrollRect/Viewport/Content/plantilla como arriba). Tests: sin EditMode nuevo (la lógica está en T20); verificación del revisor en Editor con `perf-50k.txt` importado.

---

## T22 — Pulsar kanji + popup vista 1
**Requisitos:** GDD 2.4 (pulsar abre popup, scroll igual al cerrar), GDD 3.1, alcance S05 (Atrás/fuera cierran solo el popup; cada aparición su bloque), GDD 7 (≥ 48 dp), plan T22.

### Toque vs arrastre
```csharp
// UI/LinkClickHandler.cs — en la plantilla de trozo, junto al TMP
[RequireComponent(typeof(TMP_Text))]
public sealed class LinkClickHandler : MonoBehaviour, IPointerClickHandler {
    public event Action<int> EntryTapped;     // índice de KanjiEntry
    public void OnPointerClick(PointerEventData e);
}
```
- `OnPointerClick` no llega si el EventSystem inició arrastre (el `ScrollRect` padre recibe el drag porque el TMP no implementa `IDragHandler`). Doble guarda: salir si `e.dragging` o si `(e.position - e.pressPosition).sqrMagnitude > threshold²` con `threshold = EventSystem.current.pixelDragThreshold`.
- Umbral físico: `AppBootstrap` fija una vez `EventSystem.current.pixelDragThreshold = Max(10, Round(8 * dpi / 160))` (≈ 8 dp; `dpi` 0 -> 10). El valor por defecto (10 px) es demasiado pequeño en pantallas de ~3x y convierte toques en arrastres.
- Acierto: `TMP_TextUtilities.FindIntersectingLink(text, e.position, e.pressEventCamera)` (overlay -> `null`); `< 0` = texto no azul, no hace nada. Id -> `StoryRichTextBuilder.TryParseLinkId(text.textInfo.linkInfo[i].GetLinkID(), out idx)`.
- Sin tolerancia alrededor del kanji (impacto exacto). Si T24 muestra fallos de puntería, el cambio se limita a este script (`FindNearestLink` + distancia máxima).

### Mapeo y apertura (en `ReaderScreen`)
- Al crear cada instancia de trozo: `GetComponent<LinkClickHandler>().EntryTapped += OnEntryTapped` (una vez por instancia del pool).
- `OnEntryTapped(idx)`: si `_story != null && 0 ≤ idx < _story.Entries.Count` -> `_kanjiPopup.Show(_story.Entries[idx])`; si no, `LogWarning` y nada. Log `[Kanji][Perf] kanji-popup idx=… openMs=…` (desde el clic hasta volver de `Show`).
- `OnHide` del lector: `_kanjiPopup.Close()` defensivo (idempotente en `PopupService`).

### Popup (un solo `PopupView` con estado, no dos popups)
```csharp
// UI/KanjiPopup.cs : PopupView      CloseOnOutsideTap = true (fuera y Atrás cierran)
[SerializeField] TMP_Text _kanji, _sentence;                 // richText = false, parseCtrlCharacters = false
[SerializeField] Button _btnClose, _btnShowMore;             // altura ≥ 144 px de referencia
public void Show(KanjiEntry entry);   // SIEMPRE entra en vista 1; reinicia el scroll del cuerpo
public void Close();                  // Popups.Close(this)
public override void Cancel() => Close();
private void ShowDetails();           // T22: vacío; T23 lo implementa
```
- Un solo popup: el cambio 1 -> 2 es mostrar un grupo y ocultar un botón sin reabrir ni animar, y "reabrir en vista 1" se cumple porque `Show` siempre reinicia el estado. Dos popups duplicarían prefab, cierre y pila.
- Vista 1: kanji grande + `Sentence` (si `Sentence` está vacía, la línea se oculta; resuelto por el diseñador, GDD 3.1). Sin lectura ni traducción.
- Todo el contenido del usuario en el popup va con `richText = false`: no hace falta escape.
- Extensión prevista (no en S05): S06 añade `Show(entry, KanjiPopupMode mode)` y el botón de complicados; S07 un `event Action<KanjiEntry> Revealed` disparado desde `ShowDetails`. Nada de eso se crea ahora.

**Scripts (3):** `UI/LinkClickHandler.cs`, `UI/KanjiPopup.cs`, `UI/ReaderScreen.cs`. Cableado: `AppBootstrap` (`[SerializeField] KanjiPopup _kanjiPopup`, `_kanjiPopup.Init(popups)`, `reader.Init(…, _kanjiPopup)`, umbral de arrastre), `UiStrings` (`ShowMore`, `Close` ya existe). `SceneBuilder`: `BuildKanjiPopup(popupLayer)` con `NewPopupRoot` y `LinkClickHandler` en la plantilla de trozo. Tests: sin EditMode nuevo (`TryParseLinkId` ya cubierto en T20); revisor en Editor: tocar, arrastrar sobre un kanji, kanji repetido, tocar fuera, Atrás.

---

## T23 — Vista 2 "Mostrar más"
**Requisitos:** GDD 3.2 (sin "Añadir a complicados" por S1), alcance S05 (campos vacíos ocultos, Cerrar/Atrás no mueven scroll, reabrir en vista 1), plan T23 (texto largo cabe o se desplaza).

- `KanjiPopup` añade `[SerializeField] GameObject _details` con `_reading`, `_meaning`, `_sentenceMeaning` (TMP, `richText = false`) y sus filas/etiquetas.
- `ShowDetails()`: `_details.SetActive(true)`, `_btnShowMore` oculto (queda solo Cerrar), cada fila `SetActive(!string.IsNullOrWhiteSpace(valor))`, `FitBody()`.
- `Show(entry)`: `_details.SetActive(false)`, `_btnShowMore` visible, `FitBody()`, `_body.verticalNormalizedPosition = 1`.
- **Texto largo:** el cuerpo (kanji, frase, detalles) va dentro de un `ScrollRect` vertical; los botones quedan fuera, siempre visibles. `FitBody()`: `LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent)` y `_bodyLayout.preferredHeight = Min(bodyContent.rect.height, _maxBodyHeight)` (`_maxBodyHeight` serializado, valor de ux-ui; p. ej. ≈ 60 % de 1920). uGUI no tiene "altura máxima"; esto es lo más simple.
- Cerrar/Atrás/fuera no tocan el lector: el popup vive en `PopupLayer` y el bloqueador impide arrastres sobre el texto.

**Scripts (1):** `UI/KanjiPopup.cs` (+ `UiStrings` etiquetas de campo; `SceneBuilder.BuildKanjiPopup` con grupo de detalles y ScrollRect del cuerpo). Revisor en Editor: entrada con todos los campos, con campos vacíos y con `Meaning`/`SentenceMeaning` muy largos.

---

## T24 — Qué deja preparado el programador para la prueba en móvil
- **APK dev** con `Editor/AndroidBuild.cs` existente y *Development Build* activo (sin él, los logs `[Conditional]` no existen).
- **Ficheros:** `perf-50k.txt` y `hostile-markup.txt` (T20) + la historia real del usuario [R-S05a]. `adb push … /sdcard/Download/` y se importan con el selector.
- **Logs** (`adb logcat -s Unity`, filtrar `[Kanji]`):
  - `[Kanji][Perf] reader-open …` (T21): `totalMs` < 2000 es el criterio; abrir **dos veces** la misma historia (la primera incluye el relleno del atlas dinámico de la fuente; ver `atlases`). Si `loadMs` > 200 -> mover `Load` a `Task.Run` (1 fix). Si `totalMs` > 2000 con `buildMs`/`loadMs` bajos -> bajar `_maxChunkChars` (dato serializado, sin código).
  - `[Kanji][Perf] reader-scroll …` (T21): `over33ms` ≈ 0 en un scroll de arriba abajo = "sin saltos".
  - `[Kanji][Perf] kanji-popup … openMs` (T22) < 1000.
  - Pico de memoria: `adb shell dumpsys meminfo <paquete>` antes y después de abrir la historia larga (+ `monoMB/reservedMB` del log).
  - **T19b**: logs `[Kanji][IO]` ya preparados en S04 (rama de `DiskFileSystem.Replace`, `AtomicFileWriter.Write`, resumen de arranque `stories: N ok, M unreadable, K .tmp`). No hay trabajo nuevo; el revisor solo comprueba que siguen en el APK.
- Las 3 medidas para la retro: `totalMs` de apertura (fixture e historia real), `over33ms/maxMs` de scroll y pico de memoria.

---

## Encargos
| Encargo | Scripts de producción | Tests / extra |
|---|---|---|
| T20 | `Core/Reading/StoryRichTextBuilder.cs` | `StoryRichTextBuilderTests`, `TmpVisibleText` (helper), `PlayMode/RichTextTmpEscapeTests`, export `perf-50k.txt`, `hostile-markup.txt` |
| T21 | `UI/ReaderScreen.cs`, `UI/UiStrings.cs`, `App/AppBootstrap.cs` | `SceneBuilder.BuildReaderPrefab` |
| T22 | `UI/LinkClickHandler.cs`, `UI/KanjiPopup.cs`, `UI/ReaderScreen.cs` | cableado `AppBootstrap`/`UiStrings`; `SceneBuilder.BuildKanjiPopup` |
| T23 | `UI/KanjiPopup.cs` | `UiStrings`; `SceneBuilder.BuildKanjiPopup` (detalles + scroll) |

## Riesgos
- **Escape `noparse`**: depende de un detalle del parser de TMP; lo valida el test PlayMode de T20 y hay plan B (U+200B) en un solo método.
- **Atlas dinámico de Noto Sans JP**: la primera apertura de una historia con muchos kanjis nuevos genera glifos SDF en el hilo principal; puede ser el coste dominante en móvil. Se mide con `atlases` y apertura doble en T24; si es el cuello de botella, la solución (atlas precalculado de kanji comunes) va al backlog, no a S05.
- **Puntería sobre un solo kanji** (glifo de ~44 px < 48 dp): impacto exacto en S05; tolerancia solo si T24 lo pide (cambio local en `LinkClickHandler`).
- **Toque durante la inercia del scroll**: uGUI frena el scroll y también entrega el clic; se acepta salvo que T24 lo marque.
- **Párrafo sin saltos > máximo**: se parte con salto visual (aceptado por el diseñador, GDD 2.4).

## Cobertura de requisitos (confirmada por el diseñador vía orquestador)
| Requisito | Pieza |
|---|---|
| 2.4 saltos de línea y texto fuera de llaves | `StoryRichTextBuilder` (normalización, `BreakAfter`) |
| 2.4/5.1 bloque -> Kanji azul pulsable | enlace `<link="k{idx}"><color>`; color de ux-ui vía `ReaderScreen._linkColor` |
| Marcas del .txt literales | escape `noparse` + `parseCtrlCharacters = false`; tests hostiles EditMode + PlayMode |
| 7: 50 000 car./200 kanjis | troceo ≤ máx., fixture S03, `[Kanji][Perf] reader-open` |
| 7 scroll fluido | `RectMask2D` + sub-canvas + trozos de 4 000; `reader-scroll` |
| 7 sin tofu | Noto Sans JP SDF en plantilla y popup |
| 2.4 relee del disco / error -> lista | `ReaderScreen.Open` + `UiStrings.StoryOpenFailed` + `Pop` diferido |
| 2.4 arranca arriba; Atrás/Cerrar -> lista con su scroll | `OnShow` reinicia `Content`; mecanismo de S04 (`Revision`) |
| 2.4 historia sin kanjis | `Build` sin enlaces / lista vacía sin error |
| 3.1 vista 1, cada aparición su bloque | `KanjiPopup.Show` + id = `EntryIndex` |
| Arrastrar no abre / texto normal no hace nada | `LinkClickHandler` (umbral dp + `FindIntersectingLink`) |
| Fuera/Atrás cierran solo el popup | `PopupService` (S04), `CloseOnOutsideTap = true` |
| 3.2 vista 2, vacíos ocultos, solo Cerrar | `KanjiPopup.ShowDetails` |
| Reabrir en vista 1 | `KanjiPopup.Show` reinicia estado |
| Texto largo en popup | ScrollRect del cuerpo + `FitBody` |
| Preguntas al diseñador (resueltas) | (1) corte de párrafos largos en 。/！/？/、/espacio o en el máximo: aceptado (GDD 2.4); (2) `Sentence` vacía oculta también en vista 1 (GDD 3.1); (3) fixture: un enlace por bloque (250) y 200 kanjis distintos (GDD 2.4) |
