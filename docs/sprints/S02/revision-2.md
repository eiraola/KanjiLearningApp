# S02 — Revisión ronda 2 (última)

Alcance: solo las "Correcciones revisión 1" de `implementacion.md`, comparadas con los hallazgos de `revision-1.md`. Rutas bajo `Assets/_Project/`.

## Estado de los hallazgos

| Hallazgo R1 | Severidad | Estado | Comprobación |
|---|---|---|---|
| `TryDelete` borraba el archivo original | Bloqueante | **Corregido** | `Scripts/Infrastructure/Files/NativeFilePickerAdapter.cs:96-98`. Solo se borra si `Path.GetFullPath(path)` empieza por `GetFullPath(temporaryCachePath) + separador`, con comparación ordinal y dentro de `try/catch`. Los originales de `Download/` y de otras carpetas ya no se tocan. |
| Excepciones JNI que salían del adaptador | Importante | **Corregido** | `NativeFilePickerAdapter.cs:36-46`. Hay `try/catch (Exception)` alrededor de `ConvertExtensionToFileType` y `PickFile`. En el `catch` se pone `_busy = false` y se llama a `Failed(Unreadable)`. |
| `FontTest_TEMP` en Inicio | Importante | **Corregido** | Hay 0 apariciones de `FontTest` en `Assets/`, incluidos `SceneBuilder.cs`, `FontSetup.cs` y `Home.prefab`. La consola muestra que se ejecutaron `Kanji/Setup Fonts` y `Kanji/Build S01 Scene` (09:43 UTC). |
| Menores (decodificador IBM, rebuild del layout, `OnPicked` oculto, permiso denegado, nulos en `AppBootstrap`) | Menor | Sin tocar, como se acordó | Pasan al backlog o a la retro. |

Nuevo hallazgo, **menor**: en `NativeFilePickerAdapter.cs:39-45`, si `PickFile` llama al callback de forma **síncrona** y `onCompleted` lanza una excepción, el `catch` vuelve a llamar a `onCompleted(Failed)`, así que se invoca dos veces. Solo ocurre en el Editor sin muestra (`OpenFilePanel` es síncrono) y si falla la UI. En Android la llamada es asíncrona. Corrección sugerida: poner un flag `invoked` o limitar el `try` a las dos llamadas del plugin y sacar el callback fuera. No impide aprobar.

## Tests
`unity command test run --mode EditMode` (Editor conectado): **19/19 superadas** (2,93 s). Consola: `compilationFailed=false`.

## Build
Una sola invocación de `editor invoke_menu "Kanji/Build Android APK"` (timeout 900 s): **Succeeded** en 57,7 s (build incremental).
- APK: `F:/UnityProjects/KanjiLearning/Builds/Android/KanjiLearning-dev.apk`, **68 793 347 bytes (65,6 MB)**, 2026-10-22 18:45 (+09:00). Unity informa de 811,0 MB sin comprimir; antes eran 811,1 MB.
- Hay 1 error ya conocido, `ArgumentException: Subtarget property is not available for the platform (Android)` (`PipelineRuntimeBuildProcessor`, de S01), que no bloquea. Hay 5 warnings (shaders TMP e IL2CPP).
- Este APK **ya incluye** las correcciones.

## Comprobaciones T10 en el móvil (las hace el usuario)
1. `adb logcat -s Unity` muestra `[Kanji] Shift-JIS available: True`. Un `.txt` en Shift-JIS (kanji, `\`, `~`) da `· Shift-JIS` y se ve sin corrupción. Si sale `False`, aplicar el plan B de T07.
2. Atrás: con `<`, con la tecla y con el gesto predictivo (13+) se pasa de Cargar a Inicio, y desde Inicio se sale. Con el selector abierto, Atrás lo cierra sin mensaje.
3. 5 cargas seguidas sin bloqueo. Un archivo de más de 1 MB muestra el mensaje de 1 MB y uno no válido muestra el de codificación.
4. Descargas: después de cargar un `.txt` de `Download/`, **el original sigue existiendo**.
5. Drive: el archivo se carga o sale "No se pudo leer el archivo.", sin cuelgue.
6. Profiler: anotar el pico de memoria al cargar unos 1 MB y tras volver a Inicio.
7. En Inicio **no** aparece "。「」鬱麤饕餮髙".

VEREDICTO: APROBADO
