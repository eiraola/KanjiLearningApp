# ADR 0002 — Selector de archivos Android: NativeFilePicker (yasirkula) detrás de `IFilePicker`

Estado: aceptado (a validar en dispositivo en el primer sprint) · Fecha: 2026-10-22

## Contexto
GDD §2.2 / Fase 1: abrir el selector del sistema, elegir un `.txt` (Descargas, Drive…) y leerlo. En Android moderno la vía correcta es Storage Access Framework (`ACTION_OPEN_DOCUMENT`), que devuelve una URI `content://` sin pedir permisos de almacenamiento. Unity no lo expone.

## Decisión
Usar **NativeFilePicker** de yasirkula (MIT) — `https://github.com/yasirkula/UnityNativeFilePicker.git` o OpenUPM `com.yasirkula.nativefilepicker` — encapsulado en `Kanji.Infrastructure.NativeFilePickerAdapter : IFilePicker`.

```csharp
// Kanji.Core
public interface IFilePicker {
    void PickTextFile(Action<PickedFile> onPicked, Action onCancelled); // o Task<PickedFile?>
}
public readonly struct PickedFile { public readonly string FileName; public readonly byte[] Bytes; }
```
- Filtro MIME `text/plain` (en Android usa `NativeFilePicker.ConvertExtensionToFileType("txt")`). Si un proveedor no marca bien el tipo, el usuario ve todo y validamos al leer (tamaño ≤ 1 MB, decodificación).
- El plugin copia el documento elegido a caché y devuelve una ruta; el adaptador lee bytes, extrae el nombre sin extensión y borra la copia.
- **Permisos:** ninguno de almacenamiento ni de red en el manifest. No llamar a APIs de permiso del plugin (no-op con SAF en Android 10+).
- **Editor/tests:** ~~`EditorFilePickerStub` aparte~~ (revisado en S02): la rama de Editor va **dentro del adaptador** (`#if UNITY_EDITOR`): lee un archivo de muestra configurado sin diálogo modal, o, si no hay, deja que el plugin abra su `OpenFilePanel`. En tests, un `FakeFilePicker`. La UI no sabe cuál hay.
- **Contrato definitivo (S02):** `IFilePicker.PickTextFile(Action<PickResult>)`, `PickResult` con `PickStatus {Picked, Cancelled, Error}` y `PickError {None, Busy, TooLarge, Unreadable}`; ver `docs/sprints/S02/arquitectura.md` T06.

## Alternativas
- **Plugin propio Java/Kotlin (`ACTION_OPEN_DOCUMENT`):** ~100–150 líneas, sin dependencia externa, pero requiere recibir `onActivityResult` fuera de la `UnityPlayerActivity` (fragment headless o Activity propia transparente), leer el `ContentResolver`, resolver `DISPLAY_NAME` y puentear con `AndroidJavaProxy`; más superficie de errores y solo probable en dispositivo. Queda como **plan B** si el paquete no compila con Unity 6000.6/Gradle actual.
- **Leer de una carpeta fija** (`/Download`): requiere permisos amplios, rota en Android 11+. Descartado.

## Consecuencias
- Hay que añadir el paquete al `manifest.json` (tarea del programador; requiere git o el scoped registry de OpenUPM).
- Validar en móvil real en el primer sprint: Descargas, Drive (archivo no local, el plugin lo descarga a caché), nombres con japonés, cancelar.
- Cambiar a plan B solo afecta a `Infrastructure`.
