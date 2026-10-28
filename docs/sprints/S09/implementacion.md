
## T39b
- Nuevo `DifficultKanjiStore` (Core/Library) + `IDifficultKanjiRepository` por storyId (Load/Save/Delete/ListStoryIds); `DifficultKanjiList(repo, storyId)`.
- `JsonDifficultKanjiRepository`: un fichero `difficult/<storyId>.json` (id validado hex/guion); corrupto -> `.corrupt` + lista vacia.
- Provisional: `AppBootstrap` usa `store.For("0")` para DifficultListScreen/KanjiPopup hasta la UI por historia (sin migrar el `difficult_kanji.json` antiguo: T40).
- Tests: `DifficultKanjiStoreTests` (nuevo) y adaptados repo/list/popup. EditMode 219/219 en verde.

## T39c - Borrado en cascada
- `StoryLibrary.Deleted` (Action<string>) se lanza tras `Delete` correcto (incluye id inexistente: el borrado es idempotente).
- `AppBootstrap`: `library.Deleted += id => difficultStore.DeleteStory(id)`; `StoryListScreen.Init` recibe `DifficultKanjiStore`.
- Confirmación de eliminar añade `UiStrings.AlsoDeleteDifficult` solo si `store.Count(id) > 0`.
- Tests: 2 en `StoryLibraryTests` (evento al borrar; idempotente). EditMode completo en verde; sin test PlayMode nuevo.
- Probar: importar historia, marcar complicados, eliminar desde Biblioteca y ver el aviso.

## T40b - DifficultMigration
- `Core/Library/DifficultMigration.cs`: `DifficultMigration.Run()` + `IDifficultMigrationSource`, `MigrationStatus`, `MigrationResult` (1 solo script nuevo). Usa `Story.Entries[].Key` (HashSet por historia, indexada una vez) en el orden de `library.List()`.
- `JsonDifficultKanjiRepository` implementa la fuente: `root/difficult_kanji.json` (global antigua) y marcador `root/difficult_migrated.json`; global corrupta => `LoadLegacy` falla => `Failed` sin tocar nada.
- Por historia: merge con lo existente (dedup Kanji+Reading) y un solo `Save`; marcador y borrado de la global solo tras todo OK (reintento idempotente).
- Tests: `Tests/EditMode/Core/DifficultMigrationTests.cs` (6: primera historia, descartes, no repetir/borrar global, sin global, corrupta, fallo a medias + reintento, 500x50 < 2 s).
- No conectado a `AppBootstrap` (T40c).

## T40c - Migracion al arrancar + toast
- `AppBootstrap.RunDifficultMigration`: tras crear library/store y antes de enlazar pantallas; `new DifficultMigration(library, repo, repo).Run()` en try/catch (Failed => warning y reintento en el siguiente arranque).
- Toast con `UiStrings.DifficultMigrationDiscarded` solo si `Done` y `Discarded > 0`.
- No repetir: lo garantiza el marcador (`DifficultMigrationTests` ya cubre Skipped; nuevos tests de repo: marcador persistente entre instancias, legacy carga+borra, legacy corrupta conserva fichero).
- Ficheros: AppBootstrap.cs, UiStrings.cs, JsonDifficultKanjiRepositoryTests.cs. EditMode 230/230.

## T41 - "Añadir a complicados" ligado a la historia
- `KanjiPopup`: nuevo `Show(entry, storyId, onClosed)`; usa `DifficultKanjiStore.For(storyId)` (store inyectado como 4.º parámetro opcional de `Init`). El `Show(entry, onClosed)` anterior y `ShowDifficult` siguen usando la lista fija "0" (provisional hasta T42).
- `ReaderScreen` pasa `_story.Id`; `AppBootstrap` inyecta `difficultStore` al popup.
- Estado "Ya en complicados", toast y persistencia salen de la lista de esa historia (igual que S06).
- Test PlayMode `SameKanjiInTwoStories_AlreadyStateIsPerStory` (MemoryRepo ahora por historia): 3/3 KanjiPopupDifficultTests OK. Compila sin errores.
- DifficultListScreen intacta; T42 debe pasar su storyId y retirar el "0".

## T42b - Selector de historias con complicados
- `DifficultListScreen` (nombre conservado hasta T42c) pasa a selector: `Init(navigator, library, store, onStorySelected)`; filas con el prefab `StoryRow` (`StoryListRow.Bind(info, subtitle, onClick)`), subtitulo `UiStrings.DifficultCount` ("1 complicado"/"N complicados"). Prefab `DifficultList` con `_rowPrefab` reasignado y `SceneBuilder` actualizado.
- Nuevo `Core/Library/DifficultStorySelector.Build(library, store)`: filtra N>=1 manteniendo el orden de `library.List()` (GDD 2.3). Re-render por `store.Revision` + `library.Revision`; vacio = `EmptyDifficult`; Atras = Pop.
- Tocar fila invoca `onStorySelected(storyId)`; AppBootstrap aun no lo pasa (TODO T42c: Push(grid, storyId)). `DifficultListRow` sigue sin uso (borrar en T42c). El "0" provisional queda solo en `KanjiPopup.ShowDifficult` (T42c).
- Tests: `DifficultStorySelectorTests` (2). EditMode 232/232 en verde.

## T42c - Cuadrícula solo-kanji + popup Eliminar
- Nuevos: `DifficultGridScreen` (ScreenId.DifficultGrid; 4 col, 6 si ancho >=600dp; celda cuadrada >=64dp; Atrás->Pop; vacía->Pop), `DifficultKanjiCell` (+ prefabs `DifficultGrid`, `DifficultKanjiCell` generados por SceneBuilder), test `DifficultGridFlowTests`.
- `KanjiPopup.ShowDifficult(entry, storyId, onRemoved)` usa `store.For(storyId)`; AppBootstrap ya no usa la lista "0" y cablea selector -> `Push(DifficultGrid, storyId)`.
- Borrados: `DifficultListRow` (+meta), `BuildDifficultRowPrefab`, `DifficultRow.prefab` (borrados desde shell; Editor los reimporta).
- Probar: Kanji/Build S01 Scene, PlayMode (`--async_tests true`). Resultado: 20/21 superadas; 1 fallo sin identificar (tope de turnos), revisar si es `DifficultGridFlowTests` o previo.


## Correcciones R1
- `DifficultGridFlowTests`: aserción cambiada a `Assert.AreSame(_grid, _navigator.Current)` (el `_id` heredado de `ScreenView` no se asignaba).
- `DifficultMigration.Assign`: un `Load` fallido distinto de NotFound devuelve Failed (sin marcador, global intacta, reintento al arrancar); NotFound se ignora. 2 tests nuevos en `DifficultMigrationTests`.
- Menores (dos instancias del repo, `StoryName`) anotados en `docs/backlog.md`.
