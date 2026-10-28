# S09 - Arquitectura (T39a, T40a)

Cubre GDD 2.3 (cambio S09) y 2.5. Reutiliza `DifficultKanjiList`, `KanjiEntry`, `AtomicFileWriter`, `IFileSystem`, `RepoResult`.

## 1. T39a - Lista por historia y cascada

**Persistencia:** un fichero por historia, `<root>/difficult/<storyId>.json` = `{version:1, items:[{kanji,reading,meaning,sentence,sentenceMeaning}]}` (mismo DTO y misma escritura atomica que hoy). Razon: borrado en cascada = borrar un fichero; un fichero corrupto solo afecta a su historia; escrituras pequenas. Carga: ausente/corrupto/version futura = lista vacia (corrupto se copia a `.corrupt`, como ahora). `storyId` se valida (solo `[0-9a-f]`/GUID; si no, `RepoError.InvalidId`) para evitar rutas arbitrarias.

**Puerto (Core):** `IDifficultKanjiRepository` pasa a ser por historia (Load/Save/Delete/ListStoryIds). `JsonDifficultKanjiRepository` (Infrastructure) lo implementa y cambia de fichero unico a carpeta.

**Modelo (Core):** `DifficultKanjiList` se mantiene tal cual (dedup Kanji+Reading, Revision, rollback si falla guardar) y recibe `storyId` en el constructor: es "la lista de UNA historia". Nuevo `DifficultKanjiStore` (fachada, cache `Dictionary<storyId, DifficultKanjiList>`) es lo que consumen la UI y el popup; no hay singleton, se crea en `AppBootstrap`.

**Cascada:** `StoryLibrary` no conoce los complicados (Core.Library ya contiene ambos, pero se evita acoplar). Anade `event Action<string> Deleted` que se dispara tras `_repository.Delete` correcto (antes de `Revision++`). En `AppBootstrap`: `library.Deleted += id => store.DeleteStory(id)`. Si borrar la lista falla, la historia ya esta borrada: queda un fichero huerfano inocuo (id GUID unico, nunca se muestra porque el selector cruza con `library.List()`); se registra con `IoLog.Warn`.

**Aviso N:** `StoryListScreen` llama `store.Count(id)` al abrir la confirmacion; si N > 0 anade "\nTambien se borraran sus N kanjis complicados." (con tildes en el texto real).

## 2. T40a - Migracion unica

`DifficultMigration` (Core.Library, sin UnityEngine) recibe `StoryLibrary`, `IDifficultKanjiRepository`, `IDifficultMigrationSource`. `Run()` sincrono, antes de crear pantallas.

1. `IsMigrated()` -> si true: borrar global sobrante si existe (cleanup) y devolver `Skipped`.
2. `LoadLegacy()`: si no existe -> `MarkMigrated(0)`, `Skipped`. Si esta **corrupta o falla** -> `Failed` sin tocar nada (se reintenta al siguiente arranque; la global se conserva).
3. Orden estable de historias: `library.List().Items` (ya ordenado: importada mas reciente primero, desempate por nombre y luego id; el rename no lo altera). Cada historia se carga una sola vez (`library.Load`) y se indexa en un `HashSet<string>` con el `Kanji` de sus bloques `{...}`; el kanji se asigna a la primera historia cuyo set lo contiene (O(1) por kanji; evita 500x50 busquedas en texto). Historias ilegibles se omiten.
4. Agrupar en memoria `storyId -> List<KanjiEntry>` respetando el orden de la global. No asignables: `discarded++`.
5. Por historia: `Load` existente + anadir las que falten (dedup Kanji+Reading) + `Save`. Es idempotente: repetir tras un fallo a medias no duplica. Cualquier `Save` fallido -> `Failed`, sin marcador y sin borrar la global.
6. Solo si todo fue bien: `MarkMigrated(discarded)` (fichero `difficult_migrated.json` atomico) y despues `DeleteLegacy()`. Si se cae entre ambos, el paso 1 limpia al siguiente arranque.
7. Coste: 1 lectura global + <=50 lecturas de historia + <=50 escrituras pequenas; < 2 s con 500 y 50 (test con repos falsos en memoria).

`AppBootstrap` (T40c): tras construir `library` y `store`, `var r = migration.Run();` y si `r.Discarded > 0` toast "N kanjis complicados no se pudieron asignar y se descartaron." (el toast se muestra una vez al terminar el arranque).

## Contratos para el programador

```csharp
// Core.Library
public interface IDifficultKanjiRepository {
  IReadOnlyList<KanjiEntry> Load(string storyId);            // never throws; bad data => empty
  RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items);
  RepoResult Delete(string storyId);                         // missing file = success
  IReadOnlyList<string> ListStoryIds();
}
public sealed class DifficultKanjiList {                     // existing + storyId
  public DifficultKanjiList(IDifficultKanjiRepository repo, string storyId);
  public string StoryId { get; }
}
public sealed class DifficultKanjiStore {
  public DifficultKanjiStore(IDifficultKanjiRepository repo);
  public DifficultKanjiList For(string storyId);             // cached
  public int Count(string storyId);
  public IReadOnlyList<string> StoryIdsWithItems();          // consumers intersect with library.List()
  public RepoResult DeleteStory(string storyId);             // drops cache + file
  public int Revision { get; }                               // sum of changes/deletes
}
// StoryLibrary
public event Action<string> Deleted;                         // raised after successful Delete(id)

// Migration
public interface IDifficultMigrationSource {                 // implemented by JsonDifficultKanjiRepository (difficult_kanji.json + marker)
  bool IsMigrated();
  RepoResult<IReadOnlyList<KanjiEntry>> LoadLegacy();        // Fail if corrupt; Ok(empty) if missing
  bool LegacyExists();
  RepoResult MarkMigrated(int discarded);
  RepoResult DeleteLegacy();
}
public enum MigrationStatus { Skipped, Done, Failed }
public readonly struct MigrationResult { public MigrationStatus Status; public int Assigned; public int Discarded; }
public sealed class DifficultMigration {
  public DifficultMigration(StoryLibrary library, IDifficultKanjiRepository repo, IDifficultMigrationSource source);
  public MigrationResult Run();
}
```

Supuesto a verificar por el programador: que `Story` exponga los `Kanji` de sus bloques; si no, indexar a partir del texto una sola vez por historia (no `Contains` por kanji).
