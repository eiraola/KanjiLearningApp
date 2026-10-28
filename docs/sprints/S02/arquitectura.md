# S02 — Arquitectura (T06-T09)

Rutas bajo `Assets/_Project/`. Requisitos: GDD §2.2 (cargar), §5.2 (codificación, 1 MB) y "Decisiones del usuario" de `plan.md`.
Orden recomendado: T06 ∥ T07 → **T09 antes de T08** (los dos tocan `AppBootstrap` y `SceneBuilder`).

## T06 — `IFilePicker` + adaptador NativeFilePicker (cubre GDD §2.2, ADR 0002)
**Crear**
- `Scripts/Core/Files/IFilePicker.cs` (`Kanji.Core`, ns `Kanji.Core.Files`): puerto + tipos de resultado.
- `Scripts/Infrastructure/Files/NativeFilePickerAdapter.cs` (`Kanji.Infrastructure`).
- `Scripts/Infrastructure/Kanji.Infrastructure.asmdef`: refs `Kanji.Core`, `NativeFilePicker.Runtime`; `autoReferenced: false`.
- Fixture para Editor: `Tests/Fixtures/Text/sample-utf8.txt` (texto con kanji, ~5 KB). Fuera de `Scripts/`, no entra en build.

**Modificar** `Scripts/App/Kanji.App.asmdef`: añadir `Kanji.Infrastructure`.

```csharp
public enum PickStatus { Picked, Cancelled, Error }
public enum PickError  { None, Busy, TooLarge, Unreadable }
public readonly struct PickResult {
    public PickStatus Status { get; } public PickError Error { get; }
    public string FileName { get; }   // sin extensión
    public long SizeBytes { get; }    // también en TooLarge
    public byte[] Bytes { get; }      // null salvo Picked
    public static PickResult Picked(string name, byte[] bytes); public static PickResult Cancelled();
    public static PickResult Failed(PickError e, string name = null, long size = 0);
}
public interface IFilePicker { void PickTextFile(Action<PickResult> onCompleted); } // siempre llama una vez, en el hilo principal
```
Adaptador: `public NativeFilePickerAdapter(long maxBytes, string editorSamplePath)`.
- Si ya hay una selección en curso (`_busy` o `NativeFilePicker.IsFilePickerBusy()`) → `Failed(Busy)` sin abrir nada.
- Llama a `NativeFilePicker.PickFile(cb, NativeFilePicker.ConvertExtensionToFileType("txt"))` (`text/plain`; no se rechaza por extensión, decisión del usuario). `path == null` → `Cancelled()`. El plugin ya devuelve el callback en el hilo principal.
- Con ruta: `FileInfo.Length > maxBytes` → `Failed(TooLarge, name, size)` **sin leer** (evita cargar en memoria archivos enormes). Si no, `File.ReadAllBytes`; `IOException`/`UnauthorizedAccessException` → `Failed(Unreadable)`. Ninguna excepción sale del adaptador.
- En Android borra la copia de caché del plugin en `finally` (solo `#if UNITY_ANDROID && !UNITY_EDITOR`).
- **Rama de Editor dentro del adaptador** (`#if UNITY_EDITOR`): si `editorSamplePath` existe, lo lee directamente sin diálogo modal (el Editor queda controlable por MCP); si está vacío, deja que el plugin abra su `OpenFilePanel`. Sin `EditorFilePickerStub` aparte (se actualiza ADR 0002).
- Patrón: puerto/adaptador (hexagonal). Plan B (plugin Java propio) solo cambiaría este fichero.

## T07 — `TextDecoder` (cubre GDD §5.2 filas Codificación y Archivo muy grande)
**Crear**
- `Scripts/Core/Text/DecodeResult.cs` (`Kanji.Core`, ns `Kanji.Core.Text`): enums + struct.
- `Scripts/Core/Text/TextDecoder.cs` (`Kanji.Core`).
- `Assets/_Project/link.xml` (no es script).
- Tests: `Tests/EditMode/Core/Text/TextDecoderTests.cs` (`Kanji.Core.Tests`).

```csharp
public enum TextEncodingKind { Utf8, Utf8Bom, ShiftJis }
public enum DecodeError { None, TooLarge, UnsupportedEncoding }
public readonly struct DecodeResult {
    public bool IsSuccess { get; } public DecodeError Error { get; }
    public TextEncodingKind Encoding { get; } public string Text { get; }
    public static DecodeResult Ok(string text, TextEncodingKind enc); public static DecodeResult Fail(DecodeError e);
}
public sealed class TextDecoder {
    public const long MaxBytes = 1024 * 1024;               // 1 MB (decisión del usuario)
    public TextDecoder(Encoding shiftJis);                   // null = Shift-JIS no disponible
    public static Encoding TryCreateShiftJis();              // null si la plataforma no lo tiene
    public bool SupportsShiftJis { get; }
    public DecodeResult Decode(byte[] bytes);
}
```
Algoritmo de `Decode` (sin excepciones hacia fuera):
1. `bytes == null` → `Fail(UnsupportedEncoding)`. `bytes.Length > MaxBytes` → `Fail(TooLarge)` **antes de decodificar**.
2. BOM `EF BB BF` → decodificar el resto como UTF-8 estricto → `Utf8Bom`. Sin BOM → UTF-8 estricto → `Utf8`. Estricto = `new UTF8Encoding(false, throwOnInvalidBytes: true)` en `try/catch (DecoderFallbackException)`.
3. Si UTF-8 falla y `SupportsShiftJis` → Shift-JIS con `DecoderFallback.ExceptionFallback` en `try/catch (ArgumentException)`; además, si el texto contiene `'�'` se considera inválido (por si la implementación de Mono/I18N ignora el fallback).
4. Si nada vale → `Fail(UnsupportedEncoding)`. Archivo vacío → `Ok("", Utf8)`. No normaliza `\r\n` (lo hará el parser en S03).

**Shift-JIS en IL2CPP (decisión).** Unity 6000.6 usa el BCL de Mono con perfil .NET Standard 2.1: `CodePagesEncodingProvider` **no** está en `netstandard.dll` en tiempo de compilación, y en Mono la página 932 la sirve `I18N.CJK.dll`. Por eso `TryCreateShiftJis()`:
- Intenta registrar el proveedor **por reflexión**, sin referencia de compilación: `Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages")?.GetProperty("Instance")` → `Encoding.RegisterProvider(...)` (una vez, si el tipo existe).
- Luego `Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)` en `try/catch` (`ArgumentException`, `NotSupportedException`) → `null` si falla. Sin estado estático mutable fuera de ese registro idempotente del BCL.
- `link.xml`: `<linker><assembly fullname="I18N" preserve="all" ignoreIfMissing="1"/><assembly fullname="I18N.CJK" preserve="all" ignoreIfMissing="1"/><assembly fullname="System.Text.Encoding.CodePages" preserve="all" ignoreIfMissing="1"/></linker>`.
- T10 lo verifica en dispositivo (`SupportsShiftJis` se registra con `Debug.Log` en `AppBootstrap`). Si es `false` en IL2CPP: plan B = copiar `I18N.dll` + `I18N.CJK.dll` del Editor (`Data/MonoBleedingEdge/lib/mono/unityaot-*`) a `Plugins/I18N/`; si tampoco, se quita Shift-JIS (decisión del usuario) y el decoder devuelve `UnsupportedEncoding`, sin tocar UI. En ese caso el diseñador actualiza GDD §5.2 (fila Codificación).

Tests (casos nominales, bytes literales en el test, sin depender de archivos): UTF-8 con kanji; UTF-8 con BOM (el BOM no aparece en `Text`); Shift-JIS `漢字` = `8A BF 8E 9A`; bytes inválidos en ambos (`FF FE 81 20`) → `UnsupportedEncoding`; `MaxBytes + 1` → `TooLarge`; exactamente `MaxBytes` válido → Ok; vacío; `new TextDecoder(null)` con SJIS → `UnsupportedEncoding`. Si `TryCreateShiftJis()` es null en Editor, `Assert.Ignore` en los tests SJIS.

## T08 — Pantalla Cargar (cubre GDD §2.2; layout y textos: `docs/ui/` de ux-ui)
**Crear**
- `Scripts/UI/LoadStoriesScreen.cs` (`Kanji.UI`): `ScreenView`, `public void Init(ScreenNavigator navigator, IFilePicker picker, TextDecoder decoder)`.
- `Scripts/Core/Text/TextPreview.cs` (`Kanji.Core`): lógica pura de recorte. Test: `Tests/EditMode/Core/Text/TextPreviewTests.cs`.

```csharp
public readonly struct TextPreview {
    public string Head { get; } public int RemainingChars { get; } // en code points
    public static TextPreview Create(string text, int maxChars);   // no parte pares sustitutos
}
```
**Modificar (no lógicos):** `UiStrings.cs` (textos: botón, etiquetas de codificación "UTF-8"/"UTF-8 con BOM"/"Shift-JIS", "… ({0} caracteres más)", errores); `Editor/SceneBuilder.cs` para que `LoadStories.prefab` use `LoadStoriesScreen` en lugar de `EmptyScreen`.

Flujo: botón → deshabilitar botón → `picker.PickTextFile(r => …)` → rehabilitar.
- `Cancelled` → no mostrar nada y **conservar** la vista previa/mensaje anterior. `Busy` → ignorar.
- Cualquier otro resultado (éxito o error) **sustituye** por completo la vista previa o el mensaje anterior.
- `Failed(TooLarge)` o `DecodeError.TooLarge` → `UiStrings.ErrorTooLarge = "El archivo supera 1 MB. Divídelo en archivos más pequeños."`
- `UnsupportedEncoding` → `UiStrings.ErrorEncoding = "Codificación no soportada, guarda el archivo como UTF-8"` (literal de GDD §5.2).
- `Unreadable` → `UiStrings.ErrorUnreadable = "No se pudo leer el archivo."`
- `Picked` → `decoder.Decode(bytes)` → nombre, KB (`SizeBytes`, formato de ux-ui), etiqueta de codificación, `TextPreview.Create(text, 3000)`.
- Archivo vacío (`Ok("")`): **no es error**; se muestran nombre, "0 KB" y "UTF-8", con la vista previa vacía y sin línea "… (N caracteres más)".
- Atrás (botón o tecla/gesto) → `HandleBack()` devuelve `false` → `ScreenNavigator.Pop()` → vuelve a Inicio. Si el selector está abierto, Android lo cierra solo (llega como Cancelar).
- La vista previa va en un `TMP_Text` con `richText = false` (el `<` del usuario no se interpreta) dentro de un `ScrollRect`; 3 000 caracteres caben en un solo TMP. No guarda nada; `OnHide` limpia textos y no retiene bytes.
- Mapeo de `TextEncodingKind`/errores a texto **solo en UI** (Core no lleva castellano).

Cableado en `AppBootstrap.Awake` (tras crear `navigator`):
```csharp
var decoder = new TextDecoder(TextDecoder.TryCreateShiftJis());
Debug.Log($"[Kanji] Shift-JIS available: {decoder.SupportsShiftJis}");
IFilePicker picker = new NativeFilePickerAdapter(TextDecoder.MaxBytes, _editorSampleFile);
// switch: case LoadStoriesScreen load: load.Init(navigator, picker, decoder); break;
```
`[SerializeField] private string _editorSampleFile = "Assets/_Project/Tests/Fixtures/Text/sample-utf8.txt";` (solo se usa en Editor).

## T09 — Endurecimiento S01
**Modificar** `Scripts/Editor/FontSetup.cs` y `Scripts/App/AppBootstrap.cs` (+ referencia a Medium en `SceneBuilder.cs`, una línea).
- Fuentes: `CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true)`. ~1 200 glifos por atlas (4 MB Alpha8). Idempotente: si el asset existe con otros parámetros, recrearlo (o `ClearFontAssetData` + reconfigurar).
- Quitar `NotoSansJP-Medium` (asset SDF y referencias); etiquetas de botón pasan a Regular. Lista de fallbacks **global** de TMP Settings vacía; Bold conserva Regular como fallback **local**. Quitar `FontTest_TEMP` si sigue.
- `AppBootstrap`: antes de `Instantiate`, prefab nulo → `Debug.LogError` + `continue`; `prefab.Id` ya en el diccionario → `LogError` + `continue` (no se instancia); si falta `ScreenId.Home` al final → `LogError` y no hacer `Push`. Nunca lanzar.
- Nota T04 de `S01/implementacion.md`: la corrige el programador (es su documento).

## Dependencias tras S02
`App → {UI, Infrastructure, Core}`; `UI → Core`; `Infrastructure → {Core, NativeFilePicker.Runtime}`. UI no conoce el adaptador; solo `IFilePicker`.
