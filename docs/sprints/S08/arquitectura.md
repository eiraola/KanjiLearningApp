# S08 - Arquitectura (T35-T37)

Cubre GDD 2.2 y 2.3 (S08). Sin ADR: no hay decisiones difíciles de revertir. Dependencias sin cambios: UI -> Core; Core sin UnityEngine.

## Piezas (Kanji.Core.Library salvo indicación)

**`LoadStats` (nuevo, Core, puro)** - GDD 2.2 estadísticas.
`readonly struct LoadStats { int Kanjis; int Characters; int Warnings; static LoadStats From(Story s, ParseReport r) }`
- `Kanjis` = `story.Entries.Count` (bloques válidos, igual que 2.3).
- `Characters` = recorrer `story.Segments`, sumar los code points de `Segment.Text` (sirve para Text y Kanji, que ya trae el Kanji sustituido) que no sean `char.IsWhiteSpace` (espacios, saltos, espacio ideográfico U+3000). Un par subrogado cuenta 1.
- `Warnings` = `report?.Warnings.Count ?? 0`. **No hay palabras.** La UI muestra cada línea solo si Avisos > 0 (kanjis y caracteres siempre).

**`StoryNameValidator` (nuevo, Core, estático)** - GDD 2.2 duplicado y 2.3 Editar.
```
enum NameCheck { Ok, Empty, Duplicate }
readonly struct NameResult { NameCheck Check; string Name /*recortado, ya resuelto*/; string ConflictingName }
static NameResult Validate(string input, IReadOnlyList<StoryInfo> existing,
                           string fallbackName /*nombre de archivo; null en Editar*/,
                           string ownId /*null en carga; id de la historia en Editar*/)
```
Reglas: `name = (input ?? "").Trim()`; si vacío -> `fallbackName` (si es null/vacío tras recortar -> `Empty`). Duplicado = existe un `StoryInfo` con `StoryName.AreSame(info.Name, name)` (ya recorta y es OrdinalIgnoreCase; se reutiliza, no se duplica lógica) **y** `info.Id != ownId`. Así el propio nombre al renombrar (incluso cambiando solo mayúsculas) no es duplicado. `ConflictingName` = nombre de la historia existente (para el texto "Ya existe una historia llamada 'X'..."). Aceptar activo solo si `Check == Ok`. Aplica también al nombre por defecto.

**`StoryLibrary` (modificado)**
- `Import(Story)`: se retira `ConflictPolicy` y el estado `Conflict`. Firma nueva `ImportResult Import(Story parsed, string name)`: valida con `StoryNameValidator` (red de seguridad ante carreras); `Duplicate` -> `ImportResult.Failed`-like nuevo `ImportStatus.Duplicate` (con `ConflictingName`), sin guardar; si no, `Save(parsed.With(_newId(), name, _utcNow()))`. Se eliminan `ConflictPolicy`, `ImportResult.Conflict/Cancelled`, `ImportStatus.Conflict/Cancelled`, `NextFreeName`. Se conserva `Duplicate` como único rechazo.
- `RepoResult Rename(string id, string newName)` (nuevo): `Load(id)`; si falla, devuelve ese error; valida con `Validate(newName, List().Items, null, id)`; si no es `Ok` -> `RepoResult.Fail(RepoError.<invalid/duplicate; reutilizar el existente más cercano o añadir `NameInvalid`>, msg)`; guarda `story.With(story.Id, name, story.ImportedAtUtc)` con `_repository.Save`. **Mismo id, mismo `ImportedAtUtc` (orden de lista intacto), mismo contenido** (`With` reutiliza Segments/Entries). Éxito -> `Revision++` (las vistas ya re-renderizan por Revision). Complicados de S09 asociados al id: se conservan.
- `List()` ya expone `Items` para el validador; `StoryLibrary` añade `NameResult Check(string input, string fallback, string ownId)` como fachada para que la UI no toque el repositorio.

**`NameInputPopup` (nuevo, UI, `: PopupView`)** - un solo popup para carga y edición (T36b y T37b).
`Show(NameInputRequest req, Action<string> onAccept)`; `NameInputRequest { Title, Placeholder, InitialText, LoadStats? Stats (null en Editar = oculta estadísticas), Func<string, NameResult> Validate }`. En cada `onValueChanged` del `TMP_InputField` llama a `Validate`, activa/desactiva Aceptar y muestra/oculta el mensaje rojo (`UiTheme.Error`) con `UiStrings.NameDuplicate`. Cancelar y Atrás cierran sin callback de aceptación. No se cierra al tocar fuera (mismo mecanismo que `ChoicePopup`). Patrón: popup con request inmutable + callback, sin lógica de dominio (la lógica vive en Core, testeable en EditMode).

**`LoadStoriesScreen` (modificado)**: tras decodificar y parsear, si el parser no devuelve error fatal (5.3) -> `LoadStats.From`, abre `NameInputPopup` (placeholder = `StoryName.FromFileName`, validación con fallback = ese nombre). Aceptar -> `library.Import(story, result.Name)` -> toast existente "Historia 'X' cargada: N kanjis" (+ avisos). Cancelar -> nada. Se eliminan `AskConflict`, `OnChoice`, el parámetro `ChoicePopup` de `Init`.

**Popup de historia (lista, T37b)**: añade botón Editar que abre `NameInputPopup` con `InitialText = nombre actual`, `Stats = null`, validador con `ownId`; Aceptar -> `library.Rename(id, name)` y refresco de lista y popup de historia.

## Retirada del diálogo de conflicto sin romper PopupService
- `PopupService` y `PopupView` **no se tocan**. `ChoicePopup` se queda en la escena como componente genérico (solo deja de usarse en Cargar); evita editar escena/prefab ni la serialización de `AppBootstrap`. Se borran en `UiStrings`: `NameConflictTitle`, `NameConflict`, `Replace`, `KeepBoth` (y `Cancel` solo si queda sin uso); se añaden `NameDuplicate = "Ya existe una historia llamada '{0}'. Elige otro nombre."` y textos del popup (los fija la spec UX).
- Antes de borrar: `Grep` de `ConflictPolicy|NameConflict|KeepBoth|UiStrings.Replace` (hoy: StoryLibrary, LoadStoriesScreen, UiStrings, tests `StoryLibraryTests` x14 y `JsonStoryRepositoryTests` x3, `AppBootstrap`/`SceneBuilder` por `_choicePopup`). `AppBootstrap` y `SceneBuilder` dejan de pasar `_choicePopup` a `LoadStoriesScreen.Init` (el campo y su `Init(popups)` pueden quedar).
- Tests: reescribir los de conflicto (`Import_DuplicateName...`, `Replace`, `KeepBoth`, `Cancel`) como tests de `Import(story, name)` con `Duplicate`; los 3 de `JsonStoryRepositoryTests` pasan a la nueva API. Cero apariciones de "Nombre repetido"/Reemplazar/Conservar ambas.

## Subtareas (<= 3 scripts de producción cada una; tests aparte no cuentan)
| Subtarea | Scripts | Notas |
|---|---|---|
| T35b | `LoadStats.cs`, `StoryNameValidator.cs` (+ tests EditMode) | Solo nuevo; no toca `StoryLibrary`. |
| T36b-1 | `StoryLibrary.cs` (Import con nombre, `Check`, estado `Duplicate`), `UiStrings.cs` | Adaptar tests de StoryLibrary/JsonStoryRepository para compilar; ver T36c. |
| T36b-2 | `NameInputPopup.cs`, `LoadStoriesScreen.cs`, `AppBootstrap.cs` | Popup nuevo + integración; edición de escena vía MCP (`SceneBuilder.cs` como 4.º fichero solo si hace falta: dividir en T36b-3). |
| T36c | `LoadStoriesScreen.cs`, `UiStrings.cs`, tests | Limpieza final: grep sin restos y tests en verde. |
| T37b-1 | `StoryLibrary.cs` (`Rename`), tests | Mismo id/orden/contenido, persiste. |
| T37b-2 | popup de historia (lista) + reutiliza `NameInputPopup` | Botón Editar; cierre y refresco. |

Nota: T36b-1 y T36c quedan separadas para que el árbol compile entre invocaciones; si el programador prefiere, T36c se fusiona en T36b-1 al retirar `ConflictPolicy` (la retirada rompe tests, no se puede dejar para después).
