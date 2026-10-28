# S06 — Arquitectura: Kanjis complicados

Reutiliza: `RepoResult`, `IFileSystem`/`AtomicFileWriter`/`DiskFileSystem`, `PopupService`, `ToastView.Show(string)`, `KanjiPopup` (slot `_secondaryActions` reservado), `ScreenId.DifficultList` y botón de `HomeScreen` (ya existen; `DifficultList.prefab` es hoy un `EmptyScreen`). Sin asmdefs nuevos.

## Core (`Kanji.Core.Library`, sin UnityEngine)
```csharp
public interface IDifficultKanjiRepository {          // puerto
    IReadOnlyList<KanjiEntry> Load();                 // nunca lanza; ausente/corrupto/versión futura => vacío
    RepoResult Save(IReadOnlyList<KanjiEntry> items);
}
public enum DifficultAddResult { Added, AlreadyPresent, SaveFailed }
public sealed class DifficultKanjiList {              // patrón igual que StoryLibrary
    public DifficultKanjiList(IDifficultKanjiRepository repo);   // carga en el constructor
    public IReadOnlyList<KanjiEntry> Items { get; }   // orden de inserción
    public int Revision { get; }                      // ++ en cada cambio (la pantalla re-renderiza si cambia)
    public bool Contains(KanjiKey key);
    public DifficultAddResult Add(KanjiEntry entry);  // copia nueva de los 5 campos; dedup por Key; si Save falla revierte => SaveFailed
    public bool Remove(KanjiKey key);                 // false si no estaba o Save falló (revierte)
}
```
Dominio puro; la persistencia se hace dentro de `Add/Remove` vía el puerto (mutar y guardar en una sola operación).

## Infrastructure (`Kanji.Infrastructure.Storage`)
`JsonDifficultKanjiRepository(IFileSystem fs, string rootDirectory)` -> `rootDirectory/difficult.json`, `JsonUtility` + `AtomicFileWriter` (ADR 0004). En Load corrupto/versión > actual: copia best-effort a `difficult.json.corrupt` y devuelve vacío (evita perder datos al sobrescribir). Migración: `if (version < Current) Migrate(dto)` como en `JsonStoryRepository`.
```json
{ "version": 1,
  "items": [ { "kanji": "", "reading": "", "meaning": "", "sentence": "", "sentenceMeaning": "" } ] }
```
Ampliar (p. ej. `addedAtUnixMs` en S07) = subir `version` y migrar; el orden del array es el orden de la lista. DTOs `private sealed class` dentro del repo.

## UI (`Kanji.UI`)
- `KanjiPopupMode { Reader, Difficult }`; `KanjiPopupResult` gana `Removed`.
- `KanjiPopup.Init(PopupService, DifficultKanjiList, ToastView)` (amplía el actual) y `Show(KanjiEntry, KanjiPopupMode mode = Reader, Action<KanjiPopupResult> onClosed = null)`. Un único botón secundario (en `_secondaryActions`), con texto/estado según modo:
  - Reader: Knew/DidntKnow visibles; botón "Añadir a complicados" (`Add` -> toast "Añadido a complicados"; `AlreadyPresent` o tras añadir -> "Ya en complicados" desactivado; `SaveFailed` -> toast de error). El popup NO se cierra al añadir. Estado inicial = `Contains(entry.Key)`.
  - Difficult: Knew/DidntKnow ocultos; botón "Eliminar de la lista" -> `Remove` -> toast "Eliminado de complicados" -> cierra con `Removed`. Sin confirmación (P4).
- `DifficultListScreen : ScreenView` (Id = `DifficultList`): `Init(ScreenNavigator, DifficultKanjiList, KanjiPopup)`; filas por pool/instanciado como `StoryListScreen`; re-render en `OnShow` solo si `Revision` cambió y tras `Removed`; estado vacío; `HandleBack` = Pop (vuelve a Home). Fila pulsada -> `KanjiPopup.Show(entry, Difficult, r => if Removed refrescar)`.
- Textos nuevos en `UiStrings`.

## Cableado en `AppBootstrap`
```csharp
var difficultRepo = new JsonDifficultKanjiRepository(fileSystem, Application.persistentDataPath);
var difficult = new DifficultKanjiList(difficultRepo);          // tras crear 'library'
_kanjiPopup.Init(popups, difficult, _toast);                    // mover Init del popup tras crear difficult
case DifficultListScreen dl: dl.Init(navigator, difficult, _kanjiPopup); break;
```
`ReaderScreen` no cambia su firma: el botón vive en `KanjiPopup`. Sustituir `EmptyScreen` por `DifficultListScreen` en `DifficultList.prefab` vía MCP.

## Subtareas y ficheros (<= 3 scripts)
| Subtarea | Ficheros |
|---|---|
| T25a | `Core/Library/IDifficultKanjiRepository.cs`, `Core/Library/DifficultKanjiList.cs`, `Tests/EditMode/Core/DifficultKanjiListTests.cs` (repo falso en memoria: copia, dedup, Remove, Contains, orden, SaveFailed revierte, Revision) |
| T25b | `Infrastructure/Storage/JsonDifficultKanjiRepository.cs`, `Tests/EditMode/Infrastructure/JsonDifficultKanjiRepositoryTests.cs` (ida y vuelta con todos los campos y orden, ausente, corrupto, versión futura, vacío, kanji/Unicode) |
| T26a | solo `docs/ui/` (ux-ui) |
| T26b | `UI/KanjiPopup.cs` (modo Reader, Init ampliado, botón/estado), `UI/UiStrings.cs`, `App/AppBootstrap.cs` (repo + list + Init del popup). Prefab `KanjiPopup` vía MCP. `ToastView` se reutiliza tal cual |
| T27a | solo `docs/ui/` (ux-ui) |
| T27b | `UI/DifficultListScreen.cs`, `App/AppBootstrap.cs` (case `DifficultListScreen`), (+ prefab `DifficultList` vía MCP). Test PlayMode: Home -> DifficultList -> atrás; estado vacío |
| T27c | `UI/KanjiPopup.cs` (modo Difficult, `Removed`), `UI/DifficultListScreen.cs` (callback y refresco). Test PlayMode: abrir popup Difficult, Eliminar -> lista sin el item, toast, `Contains` false |

Tests EditMode: T25a/T25b. PlayMode (`Kanji.UI.Tests`, solo esenciales): T26b (añadir en Reader -> botón desactivado "Ya en complicados"), T27b, T27c. Persistencia tras reinicio: nuevo `DifficultKanjiList` sobre el mismo repo/carpeta temporal (EditMode) + comprobación manual en T28.

## Cobertura de requisitos
T25a/T25b -> modelo, dedup, persistencia versionada (riesgo del plan). T26b -> botón y toast reutilizable. T27b/T27c -> pantalla, popup Difficult, eliminar sin confirmación (P4).

## Preguntas al diseñador
1. `KanjiKey` dedup incluye Reading (según ARQUITECTURA.md): el mismo kanji con lecturas distintas son entradas distintas en complicados; ¿correcto?
2. ¿El orden de la lista es de inserción (más antiguo primero) o más reciente primero?

## Respuestas (orquestador, según GDD §2.5)
1. Sí: la clave de dedup es Kanji + Reading (GDD §2.5).
2. El GDD no lo fija: se guarda en orden de inserción y la pantalla muestra **el más reciente primero**.
