# S04 — Revisión 1 (combinada con S05, ronda 1)

Revisor. Alcance: código listado en `docs/sprints/S04/implementacion.md` (T19a, T15a-c, T16a-b, T17, T18, T19b) y dependencias directas, contra `plan.md` y `arquitectura.md` de S04.

## Tests
- EditMode: **164/164** superadas (`unity command test run -- --mode EditMode`, 7,8 s). Incluye los 136 de S04.
- PlayMode: **1/1** superada (`--mode playmode --async_tests`, `test status`).
- Editor conectado (`unity status` = ready), sin errores de compilación.

## Hallazgos

| Severidad | Fichero:línea | Problema | Escenario que lo provoca | Corrección sugerida |
|---|---|---|---|---|
| importante | `Assets/_Project/Scripts/UI/PopupService.cs:291-292` | `Reorder` coloca los popups inferiores **encima** del blocker, no debajo. `SetSiblingIndex(blockerIndex)` sobre un hijo con índice menor lo deja justo después del blocker. **Reproducido** en el Editor con `script eval` (hijos A, Blocker, C → tras la secuencia de `Reorder` queda `Blocker, A, C`). | Lista → tocar fila → Eliminar: la pila es [StoryPopup, ConfirmPopup]. StoryPopup queda sobre el scrim, sin oscurecer y pulsable. Su panel (~900 px) es más alto que el de confirmación (~600 px), así que Leer/Cerrar asoman y responden: Leer cierra StoryPopup y abre el lector con la confirmación de borrado aún abierta encima; además, en ese estado se puede eliminar la historia que se está leyendo. Incumple "Cancelar por defecto" y que el diálogo sea modal. | Reordenar sin depender del índice del blocker: `for (i = 0; i < n-1; i++) _stack[i].transform.SetAsLastSibling(); _blocker.transform.SetAsLastSibling(); _stack[n-1].transform.SetAsLastSibling();` y después `_keepOnTop`. Añadir un test (PlayMode o EditMode con GameObjects) que compruebe el orden con 2 popups. |
| importante | `Assets/_Project/Scripts/App/AppBootstrap.cs:313` | El resumen de arranque cuenta los `.tmp` en `Application.persistentDataPath`, pero `AtomicFileWriter` los escribe en `persistentDataPath/stories/` (`JsonStoryRepository.cs:28` + `AtomicFileWriter.cs:30`). `ListFiles` no es recursivo (`IFileSystem.cs:117`), así que **K .tmp siempre vale 0**. | Prueba T19b: force-stop durante un guardado deja `stories/<id>.json.tmp`; el log dice `0 .tmp` y el usuario no ve la prueba que pide el plan ("sin truncadas ni `.tmp` visibles"). La evidencia de T19b no vale. | Contar en `Path.Combine(Application.persistentDataPath, "stories")` (mejor: exponer la carpeta desde el repositorio para no duplicar la ruta). Hay que regenerar el APK dev antes de T24. |
| importante | `Assets/_Project/Scripts/Infrastructure/Storage/IFileSystem.cs:98-103` + `AtomicFileWriter.cs:43` | El *fallback* de `Replace` se activa con **cualquier `IOException`** y usa `File.Copy(tmp, dest, true)`, que no es atómico. Si falla a mitad, `AtomicFileWriter` borra el `.tmp`, que era la única copia íntegra. Es justo el fix del backlog (fallback solo con `PlatformNotSupported`/`NotImplemented`, sin borrar `.tmp`), todavía sin aplicar. | `File.Replace` lanza `IOException` por un error real (disco lleno, fichero bloqueado) → la copia sobrescribe `dest` a medias → la excepción llega a `Write` → `TryDelete(tmp)`. Resultado: historia truncada y sin `.tmp` para recuperarla. | Aplicar el fix previsto: `catch (Exception e) when (e is PlatformNotSupportedException \|\| e is NotImplementedException)`; dejar propagar el resto de `IOException`. Que `AtomicFileWriter` no borre el `.tmp` si `dest` puede estar dañado. Test `Replace_IOException_DoesNotFallBackToCopy` con `FaultyFileSystem` (como pide la arquitectura). Si T19b decide lo contrario, anotarlo en `bloqueos.md`. |
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/AtomicFileWriter.cs:38,44` | `data.Length` dentro del `catch`: si `data` es `null`, el `catch` lanza `NullReferenceException` y `Write` ya no devuelve `RepoResult`. Solo pasa en Editor/dev, porque `[Conditional]` también quita la evaluación de los argumentos. | `Write(path, null)` en Editor. Hoy no hay ningún llamador que lo haga. | `data?.Length ?? -1`, o validar `data` al entrar. |
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/JsonStoryRepository.cs:45-48` | El `catch (Exception)` de `List()` se traga la excepción sin log y sin contar el fichero como no legible. | Excepción inesperada al enumerar: la lista sale incompleta y no hay aviso. | `IoLog.Warn(...)` y/o `unreadable++`. |
| menor | `Assets/_Project/Scripts/UI/LoadStoriesScreen.cs:13` | El comentario dice "Saves nothing", pero desde T18 la pantalla analiza e importa. | Legibilidad. | Actualizar el `summary`. |
| menor | `Assets/_Project/Scripts/UI/StoryListScreen.cs:117,132` | `string.Format` sin `CultureInfo.InvariantCulture`, a diferencia del resto del código. Hoy no tiene efecto porque `{0}` es un string. | Ninguno visible hoy. | Unificar con `InvariantCulture`. |
| a verificar | `Assets/_Project/Scripts/UI/LoadStoriesScreen.cs:76-106` | Si el selector nativo devuelve el resultado con la pantalla ya oculta, `OnPicked` importa igualmente y puede abrir `ChoicePopup` sobre otra pantalla. | Elegir un archivo, pulsar Atrás antes de que vuelva el selector (Android). | Comprobar en el móvil. Si ocurre, ignorar el resultado cuando la pantalla no está activa. |

## Criterios de aceptación pendientes (no verificables por el revisor)
- T17 y T18: el programador indica "no verificado visualmente en Play". Siguen pendientes y se prueban en T24.
- T19b: falta la sesión del usuario (20 importaciones + force-stop). **Antes hay que corregir el contador de `.tmp`** (hallazgo 2); si no, la prueba no demuestra el criterio.

## Comprobaciones en el móvil (las hace el usuario en T24)
1. Instalar el APK dev **regenerado** después de los fixes y ver `adb logcat -s Unity` filtrando `[Kanji]`.
2. Línea de arranque `startup stories: N ok, M unreadable, K .tmp`: anotar N/M/K.
3. 20 importaciones seguidas (algunas con Reemplazar y Conservar ambas). Cada una debe dar `replace branch=move|replace`, `write ok ... ms=` e `import status=Imported`. Anotar si aparece `branch=fallback-copy` o `write FAILED` (línea completa).
4. Force-stop durante el guardado varias veces (`adb shell am force-stop com.battlecroissant.kanjilearning`) y reabrir: `unreadable` = 0, ninguna historia truncada en la lista, ningún `.tmp` en la lista, y K coherente (con el fix, K > 0 es posible y no es fallo).
5. T17: lista larga → scroll → Leer → Atrás: vuelve a la misma posición.
6. T18: cargar el mismo archivo dos veces → diálogo. Cancelar y Atrás no cambian nada. Reemplazar deja una sola historia. Conservar ambas crea "X (2)" y luego "X (3)". Toast "Historia 'X' cargada: N kanjis".
7. Eliminar: tocar fila → Eliminar → confirmación con Cancelar enfocado. Con el fix del hallazgo 1, el popup de historia debe quedar oscurecido y no responder. Cancelar vuelve al popup de historia. Eliminar quita la fila en < 1 s y no reaparece tras reiniciar.
8. Pulsar Atrás con el selector de archivos abierto o recién cerrado (hallazgo "a verificar").

VEREDICTO: CAMBIOS NECESARIOS
