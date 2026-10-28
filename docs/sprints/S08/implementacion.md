
## T35b - LoadStats y StoryNameValidator (Core)
- Nuevos: `Assets/_Project/Scripts/Core/Library/LoadStats.cs`, `.../StoryNameValidator.cs` (incluye `NameCheck`, `NameResult`).
- Tests: `Assets/_Project/Tests/EditMode/Core/LoadStatsAndNameValidatorTests.cs` (9, en verde).
- Decisiones locales: `LoadStats` expone también constructor público; `Characters` salta el low surrogate (par = 1) y excluye `char.IsWhiteSpace`. `Validate` aplica la comprobación de duplicado también al nombre de respaldo y tolera `existing == null`. `ConflictingName` queda vacío si no hay duplicado.
- No se toca `StoryLibrary`.
- Probar: `unity command test run --mode editmode --filter LoadStatsAndNameValidatorTests`.

## T36b-1 - StoryLibrary sin conflicto, Check y Rename

**Ficheros tocados**
- `Core/Library/StoryLibrary.cs`: `Import(Story, string name)`, `Check(input, fallback, ownId)`, `Rename(id, newName)`; `ImportStatus { Imported, Duplicate, Failed }`; retirados `ConflictPolicy`, `Conflict/Cancelled`, `NextFreeName`.
- `Core/Library/IStoryRepository.cs`: `RepoError` gana `NameInvalid` (nombre vacio) y `NameDuplicate` (en `Rename`; `Message` = nombre en conflicto).
- `UI/UiStrings.cs`: quitadas `NameConflictTitle`, `NameConflict`, `Replace`, `KeepBoth`; anadidas `NameTitle`, `EditNameTitle`, `EditName`, `NameDuplicate`, `StatKanjis`, `StatCharacters`, `StatWarnings`, `NameUpdated` (`Accept`/`Cancel` ya existian).
- `UI/LoadStoriesScreen.cs` (minimo para compilar): importa directo con `_pending.Name`; `Duplicate` muestra error `NameDuplicate`. `AskConflict`/`OnChoice` eliminados; `Init` conserva el parametro `ChoicePopup` (sin uso) para no tocar `AppBootstrap`. La UI real va en T36b-2.
- `Editor/SceneBuilder.cs`: los botones del `ChoicePopup` usan `string.Empty` en vez de las cadenas retiradas (el popup rotula en `Show`).
- Tests: `StoryLibraryTests` reescrito (Import con nombre, Duplicate, Check, Rename x7, Revision); `JsonStoryRepositoryTests` adaptado (2 tests).

**Decisiones**: `Rename` desconocido -> error de `Load` (`NotFound`); `Rename` conserva Id, `ImportedAtUtc` y contenido; `Revision++` solo si guarda. `Import` revalida siempre (red de seguridad).

**Probar**: recompile OK sin errores; `unity command test run --mode editmode` -> 209/209. Pendiente: `Main.unity` aun contiene el objeto `BtnKeepBoth` (nombre, sin cadena visible); T36c hace el grep final.

## T36b-2 - NameInputPopup e integracion en la carga

**Ficheros tocados (4 scripts)**
- Nuevo `UI/NameInputPopup.cs` (`: PopupView`, `NameInputRequest`): validacion en vivo en `onValueChanged`, Aceptar desactivado (texto `DisabledText`) si no es `Ok` o si el texto es identico a `InitialText` no vacio (modo Editar), mensaje rojo + borde `Error`, estadisticas opcionales (`Stats == null` las oculta; `Avisos` solo si > 0), `CloseOnOutsideTap = false`, Cancelar/Atras sin callback, Done del teclado = Aceptar si activo. `Update` solo mientras esta abierto: sube el `Panel` segun `TouchScreenKeyboard.area` (sin allocations).
- `UI/LoadStoriesScreen.cs`: `Init` recibe `NameInputPopup` (en vez de `ChoicePopup`); tras parsear abre el popup (placeholder = `StoryName.FromFileName`, stats `LoadStats.From`, validacion `library.Check(input, fallback, null)`); Aceptar -> `Import(story, name)` + toast existente. Cancelar no guarda.
- `App/AppBootstrap.cs`: campo `_nameInputPopup` sustituye a `_choicePopup`; `Init(popups)` y cableado.
- `Editor/SceneBuilder.cs`: `BuildChoicePopup` sustituido por `BuildNameInputPopup` (+ `BuildNameField`, `NewStatLabel`). Escena `Main.unity` regenerada con "Kanji/Build S01 Scene" y guardada; ya no contiene `BtnKeepBoth` ni `DuplicateNamePopup`.

**Decisiones locales**
- El callback de aceptar es `Func<string, string>` (no `Action<string>`): devuelve `null` para cerrar o un texto de error que el popup muestra en linea y se queda abierto (necesario para la red de seguridad Duplicate de la carga y reutilizable por Rename en T37b-2).
- No existen `icon_edit` ni icono de aviso en `Art/DarkUI/Icons`: solo texto (el aviso lleva fondo `WarningSurface` en un `WarningBox`; el popup activa/desactiva su padre). `Edit` y icono de error: pendientes si se anaden los sprites.
- `ChoicePopup.cs` queda sin uso en escena (la clase sigue en el codigo; T36c puede borrarla).
- El parser no tiene "error fatal", asi que el popup se abre siempre tras parsear.
- La altura del campo en pantalla de teclado no se pudo probar en dispositivo.

**Verificacion**: recompile OK, 0 errores/warnings en consola; EditMode 209/209. Play Mode via eval: `Show` con stats/placeholder/botones correctos y errores 0; captura del Game view en negro (vista sin foco), sin comprobacion visual. Pendiente probar en dispositivo: picker real, teclado y Duplicate.

## T36c - Retirar el diálogo de conflicto de S04

**Ficheros tocados**
- `Assets/_Project/Scripts/UI/ChoicePopup.cs` y `.meta`: borrados (ya sin uso; su GUID `14a02cf46a26e854faa6f24e6f7e9a5a` no aparece en ningún `.unity`, `.prefab` ni `.asset` de `Assets/`).

**Búsqueda final**
- `Assets/_Project` y `docs/ui/`: `Nombre repetido`, `Reemplazar`, `Conservar ambas`, `KeepBoth`, `ConflictPolicy`, `ChoicePopup`, `AskConflict`. En código no queda ninguna; las cadenas de UI ya se habían retirado en T36b-1 y la escena se regeneró en T36b-2.
- `docs/ui/S04-biblioteca.md` (líneas 146-161) sigue describiendo el diálogo de conflicto. Es un documento del agente `ux-ui`, así que no se ha tocado; queda pendiente de que su dueño lo marque como retirado.
- `docs/ui/S08-nombre-historia.md` línea 50 menciona `ChoicePopup` como referencia de mecanismo ("mismo mecanismo que `ChoicePopup`"). Tampoco se ha tocado por el mismo motivo; hay que cambiarlo a otra referencia o quitarlo.

**Verificación**
- `unity command recompile` + `mcp__unity__recompile_status`: `completed`, sin errores de compilación.
- `unity command test run --mode editmode`: 209/209 superadas.
- Consola: 0 errores del proyecto. Los 5 errores `script recompile_status` vienen de llamadas CLI mal formadas durante esta tarea, no del código. Los `[Kanji][IO] write FAILED` son trazas de los tests de escritura con fallo esperado. Warnings `UAC0009` (`DEVELOPMENT_BUILD` deprecado) en `AppBootstrap.cs`, `LoadStoriesScreen.cs` y `ReaderScreen.cs`: no los introduce esta tarea; revisar en una tarea aparte.

**Decisiones locales**
- No se han tocado documentos de sprints antiguos ni los de `docs/ui/`.

## T37b - Editar nombre (UI)

**Ficheros tocados (4 scripts)**
- `UI/StoryActionsPopup.cs`: `StoryAction.Edit`, campo `_btnEdit`; Edit (como Delete) deja el popup abierto bajo el popup de edicion; nuevo `Refresh(StoryInfo)` para actualizar nombre/contador en el mismo frame.
- `UI/StoryListScreen.cs`: `Init` recibe `NameInputPopup`; `AskRename` abre el popup en modo edicion (sin stats, `InitialText` = nombre actual, `Validate = library.Check(text, null, info.Id)`); `ApplyRename` llama a `StoryLibrary.Rename`, refresca lista (`Render`) y popup de historia y muestra toast `Nombre actualizado`. Si falla: devuelve error en linea (`NameDuplicate` con el nombre en conflicto, o `No se pudo guardar.`) y el popup sigue abierto.
- `App/AppBootstrap.cs`: pasa `_nameInputPopup` a `StoryListScreen.Init`.
- `Editor/SceneBuilder.cs`: `BtnEdit` (Secundario, alto 144, texto `Editar nombre`) en `BuildStoryPopup`, antes de `Leer`. Escena regenerada con "Kanji/Build S01 Scene" y guardada.

**Decisiones locales**
- `NameInputPopup` no necesito cambios: ya valida via `request.Validate`, asi que el `ownId` se pasa desde la peticion; ya desactiva Aceptar si el texto es identico a `InitialText`.
- Sin test nuevo: `StoryLibraryTests.Check_Ok_Empty_Duplicate_AndFallback` (ownId + otras mayusculas) y `Rename_OnlyCase_IsAllowed` ya cubren el caso.
- Sin `icon_edit` (no existe el sprite): solo texto, como en T36b-2.

**Verificacion**: recompile OK sin errores; EditMode 209/209; escena guardada con `BtnEdit` cableado. Play Mode: `BtnEdit` presente en `StoryPopup`; no se pudo recorrer el flujo completo (la biblioteca estaba vacia). Pendiente probar a mano: importar una historia, Editar nombre, duplicado, mayusculas, toast y persistencia.
