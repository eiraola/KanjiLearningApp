
## T25a
- Ficheros: `Core/Library/IDifficultKanjiRepository.cs`, `Core/Library/DifficultKanjiList.cs`, `Tests/EditMode/Core/DifficultKanjiListTests.cs`.
- `DifficultKanjiList` carga en el constructor (copia defensiva, ignora duplicados/null del repo), guarda en cada Add/Remove y revierte si `Save` falla.
- `Revision` solo sube con cambios guardados con exito; `AlreadyPresent` no guarda ni sube Revision.
- Decision/discrepancia: `KanjiKey` actual usa SOLO el campo Kanji (no Reading), contrario a lo que dice arquitectura.md; la dedup sigue `entry.Key` (como pide el plan). Si el diseñador quiere Kanji+Reading hay que cambiar `KanjiKey` (afecta a Story).
- Tests: 8 nuevos (copia, dedup, Contains/Remove, orden, SaveFailed en Add y Remove, Revision, recarga). EditMode completo: 172/172 verdes.

## T25b
- Ficheros: `Infrastructure/Storage/JsonDifficultKanjiRepository.cs`, `Tests/EditMode/Infrastructure/JsonDifficultKanjiRepositoryTests.cs`.
- Fichero unico `<root>/difficult_kanji.json` = `{version:1, items:[...]}` (JsonUtility, UTF-8 sin BOM), escrito con `AtomicFileWriter`.
- `Load()` nunca lanza: ausente, corrupto, sin version, version futura o item sin kanji -> lista vacia.
- Correccion T25a: `DifficultKanjiList` deduplica por Kanji + Reading con struct privado `DifficultKanjiKey`; `KanjiKey` y `Story` intactos.
- Cambio de API: `Contains`/`Remove` ahora reciben `KanjiEntry` (antes `KanjiKey`, que solo tiene kanji). T26b/T27b deben pasar la entrada.
- Tests: 1 nuevo en la lista (mismo kanji, otra lectura = 2) + 8 del repo. EditMode: 191/191 verdes (antes 180).

## T26b
- Ficheros: `UI/KanjiPopup.cs`, `UI/UiStrings.cs`, `App/AppBootstrap.cs`, `Editor/SceneBuilder.cs` (BuildKanjiPopup); escena regenerada con `Kanji/Build S01 Scene`.
- `KanjiPopup.Init(popups, difficult, toast)`; en `Show` el slot `SecondaryActions` se activa y el estado inicial sale de `Contains(entry)`. Botón `BtnAddDifficult` (secundario, alto 144, icono + / check verde).
- Pulsar: `Add` -> Added: botón "Ya en complicados" desactivado + toast `Añadido a complicados`; AlreadyPresent: solo estado; SaveFailed: toast `No se pudo guardar.` y el botón sigue disponible. El popup no se cierra.
- `AppBootstrap` crea `DifficultKanjiList` sobre `JsonDifficultKanjiRepository(fileSystem, persistentDataPath)`.
- Desviaciones: el popup actual es de una sola vista (sin "Mostrar más"), el botón aparece siempre; el toast dura 3 s (ToastView sin cambios, la spec decía 2 s). El toast ya queda sobre el scrim (PopupService lo mantiene último hermano).
- Sin test PlayMode nuevo (no hecho por límite de alcance); EditMode 191/191.

## T27b
- Ficheros: `UI/DifficultListScreen.cs`, `UI/DifficultListRow.cs` (nuevos), `UI/UiStrings.cs` (`EmptyDifficult`), `App/AppBootstrap.cs` (case `DifficultListScreen`), `Editor/SceneBuilder.cs` (`BuildDifficultListPrefab` + `DifficultRow`); escena regenerada.
- Lista desde `DifficultKanjiList.Items` en orden inverso (mas reciente primero); se re-renderiza en `OnShow` solo si cambia `Revision`, conservando scroll acotado.
- Fila: kanji (72, color Link) + lectura (o significado si no hay lectura). Tocar abre `KanjiPopup.Show(entry)` (modo Reader actual; boton "Ya en complicados" desactivado). Eliminar es T27c.
- Vacio: "Aún no has añadido kanjis" (texto del orquestador, distinto de la spec UI). Sin estado de error de carga: el repo nunca lanza.
- Sin test PlayMode nuevo; EditMode 191/191. Probar: Inicio -> Kanjis complicados -> añadir desde el lector -> volver.

## T27c
- Ficheros: `UI/KanjiPopup.cs` (`ShowDifficult(entry, onRemoved)`), `UI/DifficultListScreen.cs` (`Refresh()` + callback), `UI/UiStrings.cs` (`RemoveFromDifficult`, `RemovedFromDifficult`), `Editor/SceneBuilder.cs` (BuildKanjiPopup: `BtnRemoveDifficult` Peligro 160, `BtnCloseDifficult` Secundario 144, ref a `AnswerRow`); escena regenerada.
- Modo Difficult: oculta `BtnAddDifficult` y `AnswerRow`, muestra Eliminar + Cerrar. `Show` (Reader) resetea el modo, asi que el mismo prefab sirve a ambos.
- Eliminar: `DifficultKanjiList.Remove` sin confirmacion (persiste), cierra el popup, toast `Eliminado de complicados` y callback que re-renderiza la lista (estado vacio si queda vacia). Si falla el guardado: toast `No se pudo guardar.` y el popup sigue abierto.
- Desviacion: no hay enum `_mode` serializado; el modo lo fija la llamada (`Show` vs `ShowDifficult`).
- Test PlayMode `Tests/PlayMode/KanjiPopupDifficultTests.cs` (2 casos, repo en memoria, popup montado por reflexion): Reader anadir -> "Ya en complicados"; Difficult eliminar -> lista sin el item, popup cerrado, callback.
- Resultado: EditMode 191/191, PlayMode 14/14.

## Correcciones R1
- `JsonDifficultKanjiRepository.Load`: antes de devolver lista vacia (JSON corrupto, version futura, item sin kanji o excepcion) copia best-effort del original a `difficult_kanji.json.corrupt` (try/catch, sin lanzar).
- Tests EditMode nuevos: `Load_Unreadable_BacksUpOriginalToCorruptFile` (3 casos) y `Load_ValidFile_DoesNotCreateCorruptBackup`.
- `UiStrings.RemoveFailed` = "No se pudo eliminar."; `KanjiPopup.RemoveFromDifficult` lo usa en el fallo al eliminar.
- EditMode: 195/195 OK, sin errores de consola.
