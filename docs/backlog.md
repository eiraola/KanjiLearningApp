# Backlog

> Lo mantienen `disenador` y `planificador`.

## Origen S02 (T10 y revision-2.md)
Comprobaciones en el móvil que hace el usuario con `Builds/Android/KanjiLearning-dev.apk` (T10):
| Pri | Elemento | Nota |
|---|---|---|
| alta | `adb logcat -s Unity` muestra `[Kanji] Shift-JIS available: True`; un .txt Shift-JIS (kanji, `\`, `~`) da "· Shift-JIS" sin corrupción | Si es `False`, plan B de T07 (quitar Shift-JIS) |
| alta | Atrás con `<`, tecla y gesto predictivo (13+): Cargar a Inicio, y desde Inicio se sale; con el selector abierto, Atrás lo cierra sin mensaje | Si falla, `enableOnBackInvokedCallback` |
| alta | 5 cargas seguidas sin bloqueo; >1 MB muestra el mensaje de 1 MB; archivo no válido, el de codificación | |
| alta | Tras cargar un .txt de `Download/`, el original sigue existiendo | Comprueba el fix de `TryDelete` |
| media | Drive: se carga o sale "No se pudo leer el archivo.", sin cuelgue | |
| media | Profiler: anotar pico de memoria al cargar ~1 MB y tras volver a Inicio | |
| media | En Inicio no aparece "。「」鬱麤饕餮髙" | |

Menores de `revision-2.md` (S03 o cuando se toque el código):
| Pri | Elemento | Nota |
|---|---|---|
| baja | `NativeFilePickerAdapter.cs:39-45`: si `PickFile` es síncrono y `onCompleted` lanza, se invoca dos veces | Flag `invoked` o limitar el `try` a las llamadas del plugin |
| baja | Decodificador: caso IBM (codificación) | Menor de R1 |
| baja | Rebuild del layout en la vista previa | Menor de R1 |
| baja | `OnPicked` oculto | Menor de R1 |
| baja | Mensaje específico para permiso denegado | Menor de R1 |

## Origen S03 (revision-1.md, todos menores)
| Pri | Elemento | Nota |
|---|---|---|
| media | **Planificado en S04 (T19a)** · `StoryParser.cs:160`: `{Kanji: 時; Sentence. 会議は10:30です}` pierde la frase (`:` gana a `.`, sale `UnknownField`). El usuario escribe `Sentence.` | Arquitecto: usar `.` si va antes del primer `:` y lo anterior es un nombre de campo conocido; si no, documentar limitación |
| media | **Planificado en S04 (T19b)** · `IFileSystem.cs:37-41` + `AtomicFileWriter.cs:28-30`: fallback `File.Copy` no atómico y se borra el `.tmp` si falla | Limitar a `PlatformNotSupportedException`/`NotImplementedException` o no borrar el `.tmp`. Verificar en dispositivo IL2CPP antes de S04 |
| baja | `IFileSystem.cs:23`: `WriteAllBytes` sin `fsync` | `FileStream` + `Flush(true)` antes del `Replace` |
| baja | `JsonStoryRepository.cs:17`: `$` admite `\n` final en el id | Usar `\z` |
| baja | `JsonStoryRepository.cs:36-41`: `List()` deserializa historias completas | `HeaderDto` o contadores en el DTO; medir en S04 |
| baja | `StoryParser.cs:170,200`: `Detail` de `UnknownField` sin recortar a 40 caracteres | Truncar en `ParseWarning` |
| baja | `StoryParser.cs:94`: `CountLines` ignora `\r` sueltos | Contar `\r` no seguido de `\n` |

## Origen S05 (fuera de alcance)
| Pri | Elemento | Nota |
|---|---|---|
| baja | Scroll del lector persistente entre sesiones | S05 no lo persiste |
| media | Deshacer tras eliminar historia | Heredado de S04 |
| — | Registro de revelados y "Añadir a complicados" | Ya en roadmap: S06/S07 |
| alta | Investigar la historia real del usuario que "no carga entera" en el lector | Con su .txt; puede ser el fichero (parser/troceo/tope de 1 MB) o el lector |
| alta | Medidas de rendimiento en móvil (T24 no las tomó) | `[Kanji][Perf]` totalMs, over33ms/maxMs, memoria con fixture 50 000 car. e historia real; solo existen las del Editor (1,0 s frío, 0,3 s caliente) |
| media | Probar `hostile-markup.txt` en el móvil | Marcas rich text literales |
| media | Confirmar PlayMode 2/2 desde la ventana Test Runner | El runner por MCP devolvió 0 tests |
| baja | Guardar la escena antes de cada `test run` | Evita el modal de "guardar escena" |
| baja | S04/revision-2: test `Replace_IOException_DoesNotFallBackToCopy` (`IFileSystem.cs:39`) | Solo Windows/Editor, `dest` con `FileShare.None` |
| baja | S04/revision-2: literal `"stories"` duplicado (`AppBootstrap.cs:101` y `JsonStoryRepository.cs:28`) | Constante compartida |
| baja | S04/revision-2: recuperar `.tmp` íntegro en el arranque si `dest` no se puede leer | `AtomicFileWriter.cs:45-46` |
| baja | S05/revision-1: `EventSystem.current` null en `Awake` (`AppBootstrap.cs:245`); `ReaderScreen` con args null tras `Pop` (`:64,93`); `openMs` solo mide `Show`; `reader-scroll` cuenta rebote (`:216`) | Menores |

## Origen S05b (revision-1.md y T5)
| Pri | Elemento | Nota |
|---|---|---|
| media | Decidir si los botones normales dejan de usar el mismo rojo que "No lo sabía" | El usuario no ha respondido; ux-ui recomienda otro color |
| media | Probar en el móvil: borrado real de una historia, tecla Atrás física, scroll del Lector con una historia larga | No probados en T5 |
| baja | `TransitionOverlay.cs:41-53`: si `Process` se corta, no se llama a `_gate.Release()` | `OnDisable`: limpiar `_pending`, `_running=false`, `Release()`, `SetAlpha(0)` |
| baja | `AppBootstrap.cs:60-61`: `_theme` y `_transition` sin comprobar null | `Debug.LogError(..., this); return;` como con `Home` |
| baja | `PopupView.cs:121-126` + `PopupService.cs:82`: `OnDisable` -> `Finish` -> `SetActive(false)` durante la desactivación | Comprobar `activeSelf` o no invocar `onDone` desde `OnDisable` (a verificar) |
| baja | `ReaderScreen.cs:122-124`: el toast de error de carga lo tapa el fundido ~0,2 s | Mostrarlo tras `Pop()` |
| baja | Warnings UAC0009 (`DEVELOPMENT_BUILD` obsoleto en `Conditional`): `ReaderScreen.cs:176,216,234,242`, `LoadStoriesScreen.cs:135`, `AppBootstrap.cs:99` | Pasar a `DEBUG`/`UNITY_EDITOR` |

Cerrado en S05b: confirmar PlayMode desde CLI (usar `--async_tests true` + `test status`; 12/12).

## Arreglos para el final del proyecto (origen S06)
Nota: todos los "Arreglos para el final del proyecto" se hacen tras S09, junto con S10 (pulido y release).
El usuario probó S06 en el móvil (2026-10-26): "casi perfecto". Estos arreglos se dejan para el final del proyecto.
| Pri | Elemento | Nota |
|---|---|---|
| baja | Toast de S06 dura 3 s; la spec `docs/ui/S06-complicados.md` pide 2 s | Ajustar duración en `ToastView` o pasarla por parámetro |
| baja | `KanjiPopup`/`DifficultListScreen`: listeners con lambdas sin desuscribir | Sin impacto real hoy (revision-1.md S06) |
| baja | `KanjiPopup.cs`: campos `[SerializeField]` declarados a mitad de la clase | Moverlos arriba (revision-1.md S06) |
| baja | Error del Editor al hacer la build Android: "ArgumentException: Subtarget property is not available for the platform (Android)" | La build sale bien; investigar origen (ventana Build Profiles / `AndroidBuild.ApplyPlayerSettings`) |

## Arreglos para el final del proyecto (origen S07)
| Pri | Elemento | Nota |
|---|---|---|
| baja | `StatsScreen`: la lista de fallados es solo texto (kanji + lectura); no abre el popup del kanji | Valorar abrir `KanjiPopup` en modo lectura al tocar una fila |
| baja | `ReadingSession` vive solo en memoria: si Android mata la app en segundo plano se pierden las respuestas | Aceptado por diseño (P2: no se guardan estadísticas) |
| baja | Roadmap S07 habla de "Mostrar más"/"revisados" (diseño antiguo); la regla vigente es verde = acierto, rojo = fallo, sin responder = acierto | Actualizar textos del roadmap/GDD (dueño: diseñador) |

## Pendiente de S01 que sigue abierto
| Pri | Elemento | Sprint sugerido | Nota |
|---|---|---|---|
| baja | Alinear menú `Kanji/Build Android APK` con `arquitectura.md` (`Kanji/Build/Android Dev APK`) | S08 | Decide arquitecto |
| baja | `AndroidBuild.cs:53-55`: parametrizar dev/release sin tocar `EditorUserBuildSettings.development` global | S08 | Con el build de release |
| baja | Error conocido `Subtarget property is not available` de `PipelineRuntimeBuildProcessor` | — | No bloquea |

## Reducción de tamaño del APK (65,6 MB dev; objetivo release 25-30 MB)
| Elemento | Sprint sugerido | Nota |
|---|---|---|
| Build de release + stripping Low/Medium con `link.xml` | S08 | Mayor ahorro en `libil2cpp`; conservar entradas de Shift-JIS |

## Cerrado en S02
Atlas de fuente, fallbacks globales TMP, peso Medium, validación de `AppBootstrap`, nota T04, paquetes sin uso (antes de S02).

## Origen S08 (revision-1.md, todos menores)
| Pri | Elemento | Nota |
|---|---|---|
| baja | Prueba en móvil pendiente: altura del popup con teclado, flujo completo (importar, editar, duplicado, persistencia) y picker real | Prueba manual con el APK (`Builds/Android/`); el usuario la hace |
| baja | `NameInputPopup.cs:120`: "Sin cambios" compara texto crudo con `InitialText`; "Nombre " (espacio final) activa Aceptar y hace un Rename sin cambio real (Revision++ y toast) | Comparar `r.Name == InitialText.Trim()` con `Ordinal` (los cambios solo de mayúsculas siguen permitidos) |
| baja | `StoryListScreen.cs:139-140`: fallo de guardado y `NameInvalid` muestran toast y error en línea a la vez; la cadena se llama `DifficultSaveFailed` (de S09), nombre engañoso | Usar `UiStrings.SaveFailed` y solo error en línea |
| baja | `LoadStoriesScreen.cs:62-65,118`: si `OnHide` pone `_pending=null` con el popup abierto, Aceptar lanza `ArgumentNullException` (hoy no alcanzable: popup modal) | Guardia `if (_pending == null) return null;` en `OnNameAccepted` |
| baja | `LoadStoriesScreen.cs:134-138`: rama `Duplicate` de `FinishImport` es código muerto (ya se gestiona en `OnNameAccepted`) | Eliminar |
| baja | Docs de `ux-ui` por actualizar: `docs/ui/S04-biblioteca.md` líneas 146-161 (describe el diálogo de conflicto ya retirado) y `docs/ui/S08-nombre-historia.md` línea 50 (cita `ChoicePopup`) | Dueño: `ux-ui`. Marcar como retirado y cambiar la referencia |
| baja | `AppBootstrap.cs:72-73`: dos instancias de `JsonDifficultKanjiRepository` (store y migración); inocuo mientras la migración corra antes de cualquier `For()` | Reutilizar una sola instancia |
| baja | `DifficultGridScreen.cs:57`: `StoryName` reconstruye `DifficultStorySelector.Build` (lee toda la biblioteca) en cada `OnShow` | Buscar solo por id con `library.List()` |
