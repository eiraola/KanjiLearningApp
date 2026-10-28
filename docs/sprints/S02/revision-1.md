# S02 — Revisión ronda 1 (T06-T09 + preparación de T10)

Alcance: lo listado en `implementacion.md` (T06, T07, T08, T09) contra `plan.md`, `arquitectura.md` y `docs/ui/cargar.md`. Rutas bajo `Assets/_Project/`.

## Hallazgos

| Severidad | Fichero:línea | Problema | Escenario que lo provoca | Corrección sugerida |
|---|---|---|---|---|
| **Bloqueante** | `Scripts/Infrastructure/Files/NativeFilePickerAdapter.cs:58` (`TryDelete`, línea 85) | Se borra **cualquier** ruta devuelta por el plugin, pero el plugin solo copia a caché cuando no puede leer el archivo; si puede, devuelve la **ruta original** del usuario (`NativeFilePickerPickResultOperation.getPathFromURI`: si `FileInputStream(path)` lee, `return path;`, y solo si no, `copyToTempFile`). | Android 8-9 (minSdk 26): el plugin pide READ+WRITE_EXTERNAL_STORAGE (su manifest las declara y `PickFile` llama a `RequestPermissionAsync(readPermissionOnly=false)`). El usuario elige `/storage/emulated/0/Download/historia.txt`, que se puede leer por ruta, y el `finally` **borra su archivo**. También pasa con cualquier archivo con acceso de escritura (p. ej. carpetas propias de la app). | Borrar solo si la ruta normalizada está dentro de `Application.temporaryCachePath` (allí escribe el plugin: `SelectedFilePath = Path.Combine(temporaryCachePath, "pickedFile")`). Por ejemplo, `Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.temporaryCachePath) + Path.DirectorySeparatorChar, StringComparison.Ordinal)`. |
| Importante | `Scripts/Infrastructure/Files/NativeFilePickerAdapter.cs:36-38` | Hay excepciones que salen del adaptador, cuando la arquitectura dice "ninguna excepción sale del adaptador". `_busy = true` se pone antes de `NativeFilePicker.PickFile(...)` y `ConvertExtensionToFileType("txt")`, que en Android son llamadas JNI (`AJC.CallStatic`) sin `try`. | Si la llamada JNI lanza `AndroidJavaException` (por ejemplo, la Activity está en un estado raro o falla el fragment de permisos), la excepción llega a `LoadStoriesScreen.OnPickClicked`, `_busy` se queda en `true` y `_btnPick` queda desactivado. A partir de ahí cada `PickTextFile` devuelve `Busy`, que la UI ignora: el botón no hace nada hasta reiniciar la app. | Envolver `ConvertExtensionToFileType` y `PickFile` en `try/catch (Exception)`. En el `catch`, `_busy = false` y `onCompleted(PickResult.Failed(PickError.Unreadable))`. |
| Importante | `Scripts/Editor/SceneBuilder.cs:128-134` (+ `Prefabs/Screens/Home.prefab`, objeto `FontTest_TEMP`, `m_IsActive: 1`) | El texto temporal de S01 sigue en la build. `SceneBuilder` todavía crea la etiqueta `FontTest_TEMP` ("。「」鬱麤饕餮髙") en Inicio. `FontSetup.cs:24` intenta borrar `Fonts/FontTest_TEMP.asset`, un asset que nunca ha existido, así que no quita nada. No cumple T09 ("Quitar `FontTest_TEMP` si sigue"). | Abrir el APK: la pantalla de Inicio muestra la línea de prueba de glifos. | Quitar las líneas 128-134 de `SceneBuilder`, ejecutar una vez `Kanji/Build S01 Scene` y quitar la línea 24 de `FontSetup`, que no sirve para nada. |
| Menor (decisión) | `Scripts/Core/Text/TextDecoder.cs:51/58` | La ida y vuelta **no** rechaza archivos Shift-JIS normales. Sí se rechazan, con el mensaje "Codificación no soportada…", los archivos CP932 que llevan **extensiones IBM** (`FA40-FC4B`) o **NEC-selected IBM** (`ED40-EEFC`), por ejemplo 髙 `FBFC`, 﨑 `FAB1` o ⅰ `FA40`, que son habituales en nombres propios. La causa es el `I18N.CJK.CP932` de Mono: no tiene esas filas, ignora `ExceptionFallback` y devuelve `?` comiéndose un byte. La ida y vuelta lo detecta y por eso falla de forma segura, sin corromper el texto. | Medido en el Editor (`script eval`) con `I18N.CJK.CP932`. Aceptados y con ida y vuelta exacta: 漢字, `\` `5C`→U+005C, `~` `7E`→U+007E, `\r\n`, katakana de ancho medio, `8160`→U+FF5E, `817C`→U+FF0D, NEC fila 13 (`8740` ①, `8754` Ⅰ, `8799` ⊿). Rechazados: `FA4A`, `FA40`, `FA5C`, `ED40`, `EEF9`, `F040` (zona de usuario) y los bytes sueltos `80` y `A0`. | No hace falta cambiar código. Proponer al diseñador que anote la limitación en GDD §5.2 y añadir un test que la documente (`FA B1` → `UnsupportedEncoding`). Si se quiere soportar, habría que usar una tabla propia de las filas IBM; queda fuera de S02. |
| Menor | `Scripts/UI/LoadStoriesScreen.cs:98` | `ForceRebuildLayoutImmediate(_previewText.rectTransform)` reconstruye el TMP, pero el `ContentSizeFitter` está en el `Content` padre (decisión local de T08). Por eso `verticalNormalizedPosition = 1f` se aplica con el tamaño del contenido anterior. | Puede verse un frame con la altura vieja tras cargar un segundo archivo. Ahora mismo no se aprecia porque el pivote superior deja el scroll arriba (a verificar en el móvil). | Reconstruir `_previewScroll.content`. |
| Menor | `Scripts/UI/LoadStoriesScreen.cs:60` | `OnPicked` no comprueba si la pantalla sigue visible. Si el resultado llega después de `OnHide`, se rellena y se retiene el texto en una pantalla oculta. | En Android es poco probable (Atrás con el selector abierto llega como Cancelar). | Ignorar el resultado si `!gameObject.activeSelf` o con un flag `_visible`. |
| Menor (a verificar) | `NativeFilePicker.PickFile` (plugin) | Si se deniega el permiso de almacenamiento, el plugin devuelve `null`, que se trata como Cancelar sin mensaje. Con "no volver a preguntar", el botón no hace nada. | Android 8-12 con el permiso denegado. | Valorar con ux-ui un mensaje o llamar a `NativeFilePicker.CheckPermission`. Va al backlog. |
| Menor | `Scripts/App/AppBootstrap.cs:15, 49` | `_screenPrefabs` o `_backButtonRouter` nulos lanzan NRE en `Awake`. T09 no lo pedía; solo cubre prefabs nulos o duplicados. | Escena mal configurada. | `LogError` + `return` al principio. Va al backlog. |

Comprobado sin incidencias:
- `TextDecoder`: límite antes de decodificar, BOM, vacío y `null`. No se escapan excepciones (captura `DecoderFallbackException`/`ArgumentException`, `EncoderFallbackException`).
- `TextPreview`: no parte pares sustitutos y cuenta en code points.
- Formato KB según `cargar.md`. Literales de `UiStrings` iguales a los de la arquitectura.
- `richText=false`.
- Cancelar/Busy conservan el estado y `OnHide` limpia.
- Validación de `AppBootstrap` de T09.
- Fuentes: 2048 / 48 / 5, multi-atlas, `ClearDynamicDataOnBuild: 1`. Sin assets Medium. Lista global de fallbacks vacía.
- El fixture está fuera de `Resources`.
- Dependencias `UI → Core` respetadas (UI solo conoce `IFilePicker`).
- `I18N.dll` e `I18N.CJK.dll` sobreviven al stripping: están en `Library/Bee/artifacts/Android/ManagedStripped/` y `il2cppOutput/cpp/I18N.CJK.cpp`.

## Tests
`unity command test run --mode EditMode` (Editor conectado): **19/19 superadas** (3,0 s). Consola: `compilationFailed=false`, 0 errores.

## Build
Menú `Kanji/Build Android APK`, una invocación: **Succeeded** en 6 min 35 s.
- APK: `F:/UnityProjects/KanjiLearning/Builds/Android/KanjiLearning-dev.apk`, **68 793 347 bytes (65,6 MB)**, 2026-10-22 18:40 (+09:00). El informe de Unity dice "811,1 MB" porque cuenta el tamaño sin comprimir.
- 1 error: `ArgumentException: Subtarget property is not available for the platform (Android)`. Viene de `PipelineRuntimeBuildProcessor` y es el mismo que se conoce de S01; no bloquea la build. Hay 5 warnings (shaders TMP e IL2CPP).
- La build se hizo **antes** de corregir los hallazgos: todavía lleva `FontTest_TEMP` y el borrado de caché sin restringir. **No conviene probar Descargas en Android 8-9 con este APK**: puede borrar el archivo elegido.

## Comprobaciones en el móvil (T10, las hace el usuario)
1. **Shift-JIS en IL2CPP**: `adb logcat -s Unity` al arrancar debe mostrar `[Kanji] Shift-JIS available: True`. Cargar un `.txt` en Shift-JIS (con kanji y `\`/`~`): FileMeta debe decir `· Shift-JIS` y la vista previa no debe salir corrupta. Si sale `False`, aplicar el plan B de `arquitectura.md` T07.
2. **Atrás**: con el botón `<`, con la tecla Atrás y con el **gesto predictivo** (Android 13+) desde Cargar se vuelve a Inicio. Desde Inicio se sale de la app. Con el selector abierto, Atrás lo cierra sin mensaje. Si el gesto no funciona, activar `enableOnBackInvokedCallback`.
3. **5 cargas seguidas** del mismo archivo y de archivos distintos, sin cierre ni bloqueo del botón. Probar también un archivo de más de 1 MB (mensaje de 1 MB) y uno no válido (mensaje de codificación).
4. **Descargas**: elegir un `.txt` de `Download/` y comprobar después en el gestor de archivos que **el original sigue existiendo** (riesgo del hallazgo bloqueante; en Android 10+ el riesgo es menor).
5. **Drive**: elegir un `.txt` de Google Drive (se copia a la caché). Debe cargarse o mostrar "No se pudo leer el archivo.", sin cuelgue.
6. **Memoria**: Profiler conectado (build Development), anotar el pico de memoria al cargar un archivo de unos 1 MB y tras volver a Inicio.
7. Comprobar a la vista que en Inicio **no** aparece la línea de prueba "。「」鬱麤饕餮髙". Con este APK aparecerá (hallazgo importante).

VEREDICTO: CAMBIOS NECESARIOS
