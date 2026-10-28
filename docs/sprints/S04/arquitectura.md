# S04 — Arquitectura

> Dueño: arquitecto. Base: `docs/arquitectura/ARQUITECTURA.md`, ADR 0004. UI concreta (layout, textos, colores): especificación única de ux-ui en `docs/ui/`. Aquí solo controladores, vistas y contratos.
> "≤ 3 scripts" cuenta scripts de producción; los tests y fakes van aparte. Cuando una tarea pasa de 3, se parte en **encargos** (a, b, c) para programadores distintos, sin crear tareas nuevas en el plan.

## Hallazgos del código actual que condicionan el sprint
1. `LoadStoriesScreen` (S02) **no analiza ni guarda**: solo decodifica y previsualiza. T18 tiene que conectar `StoryParser` + `StoryLibrary.Import` (es más que "un diálogo").
2. No existe `PopupService` ni ninguna vista de popup; `_popupLayer` de `AppBootstrap` está sin usar. Se crea en T16a y lo reutilizan T16b y T18.
3. `StoryLibrary` y `JsonStoryRepository` no están conectados en `AppBootstrap`.
4. No hay fecha de carga en el modelo ni en el DTO: el orden "más reciente primero" (S2) exige añadirla. `List()` ordena hoy por nombre (en repo y en librería).
5. `List()` no informa de cuántos ficheros se omitieron (GDD 2.3 pide "N historias no se pudieron leer").

---

## T19a — Parser: `Sentence.` con `:` en el valor
**Requisito:** GDD 5.2 filas "`Sentence.`" y "Valor que contiene `:`".

**Regla (decidida):** en cada campo se elige el separador así:
- Si hay un `.` **antes** del primer `:`/`：` y el texto anterior a ese `.` (trim) es un **nombre de campo conocido** (`Kanji`, `Reading`, `Meaning`, `Sentence`, `SentenceMeaning`, insensible a mayúsculas), el separador es el `.`.
- En cualquier otro caso, el separador es el primer `:`/`：` si existe; si no, el primer `.` (comportamiento actual).

Ejemplos: `Sentence. 会議は10:30です` -> Sentence = `会議は10:30です`. `Sentence: 10:30` -> igual que hoy. `Meaning: Mr. Smith` -> `:` va antes del `.`, no cambia. `Foo. a:b` -> `Foo.` no es conocido -> separa por `:` -> campo desconocido `Foo. a` (aviso, como hoy).

**Scripts (1):** `Core/Parsing/StoryParser.cs` — solo `EndField(colonPos, dotPos, ...)`; extraer `private static bool IsKnownField(string name)` y reutilizarla en `Assign` (misma lista, una sola fuente).

**Tests EditMode** (`StoryParserErrorTests` o `StoryParserTests`):
- `SentenceWithDot_KeepsColonInValue` (`{Kanji: 時; Sentence. 会議は10:30です}`).
- `SentenceWithColon_KeepsTimeInValue` (regresión `Sentence: 会議は10:30です`).
- `ColonBeforeDot_UsesColon` (`Meaning: Mr. Smith`).
- `UnknownNameBeforeDot_StillUsesColon` (aviso `UnknownField`).
- Los 115 previos en verde.

---

## T15 — Lista de biblioteca
**Requisitos:** GDD 2.3 (fila nombre + nº kanjis, vacía, orden más reciente, dañados omitidos con aviso), plan T15 (< 1 s con 200, persiste).

### Decisión de rendimiento de `List()` (backlog "HeaderDto/contadores")
No se puede ejecutar Unity desde el rol de arquitecto; estimación con los datos de S03:
- Fichero JSON de una historia de 50 000 caracteres / 200 kanjis ≈ 150–250 KB (segmentos + entradas). 200 historias en el peor caso ≈ 30–50 MB por cada apertura de la lista.
- Hoy `List()` hace por fichero: `ReadAllBytes` + `GetString` + `JsonUtility.FromJson<StoryDto>` + `FromDto` (crea todas las `KanjiEntry`, `Segment` y un `HashSet`). En gama media Android eso son decenas de ms por historia grande -> **varios segundos con 200**. Con historias típicas (≈ 5 000 caracteres) rondaría 0,3–0,6 s: demasiado justo.
- Además `Import` llama a `List()` en cada carga.

**Decisión:** DTO **v2** con cabecera + `HeaderDto` para listar. Sin índice todavía (ADR 0004 lo deja detrás de `IStoryRepository` si hiciera falta).
- `StoryDto` v2 añade, **declarados antes** de `entries`/`segments`: `long importedAtUnixMs`, `int entryCount`. `CurrentVersion = 2`. (Diseñador, vuelta 2: "N kanjis" = `EntryCount`; no se guarda `distinctKanjiCount`.)
- `List()` deserializa cada fichero como `HeaderDto { int version; string id; string name; long importedAtUnixMs; int entryCount; }` (JsonUtility ignora los campos que no están en el tipo -> no crea listas ni objetos de modelo).
- Ficheros v1 (solo los de pruebas de S03): `List()` cae a `Load(id)` completo y calcula contadores; `importedAtUnixMs = 0` (quedan al final). `Migrate` v1->v2 rellena contadores desde las listas y fecha 0. No se reescriben en disco al listar.
- Validación de cabecera: `version` 1..2, `id` == nombre de fichero, contadores ≥ 0; si no -> cuenta como no legible.
- **Plan B (no se implementa ahora):** si el test de rendimiento del dispositivo (T19b) da > 1 s con 200, se añade `stories/index.json` detrás del repositorio.

### Contratos (Core)
```csharp
// Core/Model/StoryModel.cs
public sealed class Story {            // + ImportedAtUtc (DateTime, Kind Utc; MinValue si desconocida)
    public DateTime ImportedAtUtc { get; }
    public Story With(string id, string name, DateTime importedAtUtc);   // sustituye a With(id, name)
}
// Core/Library/IStoryRepository.cs
public sealed class StoryInfo {   // pasa a (id, name, entryCount, importedAtUtc); se quita DistinctKanjiCount (no está en la cabecera)
    public DateTime ImportedAtUtc { get; }
}
public sealed class StoryListing {
    public IReadOnlyList<StoryInfo> Items { get; }
    public int UnreadableCount { get; }          // ficheros .json omitidos (Corrupt, UnsupportedVersion, IoError)
}
public interface IStoryRepository { StoryListing List(); /* Load, Save, Delete sin cambios */ }
// Core/Library/StoryLibrary.cs
public StoryLibrary(IStoryRepository repository, Func<string> newId, Func<DateTime> utcNow);
public StoryListing List();      // orden: ImportedAtUtc desc, luego Name (OrdinalIgnoreCase), luego Id
public int Revision { get; }     // +1 tras cada Import con éxito y cada Delete con éxito (lo usa T17)
```
- `Import` sella la fecha con `utcNow()` en los tres caminos que guardan (nuevo, Replace, KeepBoth): reemplazar cuenta como "recién cargada".
- El orden vive **solo** en `StoryLibrary`; el repositorio deja de ordenar.
- `AppBootstrap` usa `() => Guid.NewGuid().ToString("N")` y `() => DateTime.UtcNow`.

### Vista (UI)
- `UI/StoryListScreen.cs : ScreenView` — `Init(ScreenNavigator nav, StoryLibrary library)`; T16b amplía con `PopupService`, popups y `ToastView`. `OnShow`: si `library.Revision != _renderedRevision` -> `Render()`. Muestra: estado vacío (`UiStrings.EmptyLibrary`) y aviso `UiStrings.UnreadableStories` (con N) si `UnreadableCount > 0`.
- `Render()` envuelve `library.List()` en `try/catch (Exception)`: ante excepción inesperada muestra el estado `UiStrings.LibraryLoadFailed` ("No se pudo cargar la biblioteca."), oculta las filas, `Debug.LogException` y **no** actualiza `_renderedRevision` (reintenta en el siguiente `OnShow`).
- `UI/StoryListRow.cs` — `Bind(StoryInfo info, Action<StoryInfo> onClick)`; contador con `UiStrings.KanjiCount(info.EntryCount)` (singular/plural: "1 kanji" / "N kanjis"; función estática pura en `UiStrings`).
- Filas: instanciar un prefab por historia dentro del `ScrollRect` (200 filas uGUI simples están bien; sin reciclado/virtualización: no sobrediseñar). Reutilizar instancias existentes al re-renderizar (pool por lista: activar/desactivar) para no generar basura.
- **N kanjis = `EntryCount`** (bloques válidos), igual que el informe de carga de GDD 5.3 y que el mensaje de T18. Confirmado por el diseñador (GDD 2.3).

### Encargos
| Encargo | Scripts de producción | Tests EditMode |
|---|---|---|
| T15a Core | `StoryModel.cs`, `IStoryRepository.cs`, `StoryLibrary.cs` | Actualizar `InMemoryStoryRepository` (fake) y `StoryLibraryTests` (nuevo ctor). Nuevos: `List_OrdersByImportedAtDesc_ThenName`, `Import_StampsUtcNow`, `Replace_RefreshesImportedAt`, `KeepBoth_StampsUtcNow`, `List_PropagatesUnreadableCount`, `Revision_IncrementsOnImportAndDelete_NotOnFailureOrCancel` |
| T15b Infra | `JsonStoryRepository.cs` (+ `HeaderDto`, v2, migración) | `JsonStoryRepositoryTests`: `SaveLoad_RoundTripsImportedAtAndEntryCount`, `List_ReadsHeaderOnly_V2`, `List_V1File_FallsBackAndCounts`, `List_CorruptFile_SkippedAndCounted`, `List_FutureVersion_Counted`, `List_IgnoresTmpFiles`, `List_200Stories_UnderBudget` (200 historias de ~5 000 caracteres generadas con `StoryFixtureGenerator`; presupuesto 300 ms en PC, registrar el tiempo en el log del test) |
| T15c UI | `StoryListScreen.cs`, `StoryListRow.cs`, `AppBootstrap.cs` (crear `DiskFileSystem`, `JsonStoryRepository(fs, Application.persistentDataPath)`, `StoryLibrary`; `Init` de la pantalla) + `UiStrings` (constantes, no cuenta) | Ninguno nuevo en EditMode (vista delgada); el revisor verifica en Editor vía MCP |

---

## T16 — Popup de historia + Eliminar con confirmación
**Requisitos:** GDD 2.3 (popup Leer/Eliminar/cerrar, confirmación con Cancelar por defecto, lista actualizada, mensaje "Historia 'X' eliminada"), GDD 7 (≥ 48 dp).

### Patrón
Servicio de popups sencillo, **callbacks** (no `Task`: evita `async void` y contexto de sincronización en uGUI). Popups instanciados una vez en `PopupLayer` y activados/desactivados, igual que las pantallas.
```csharp
// UI/PopupService.cs (C# puro, lo crea AppBootstrap)
public sealed class PopupService {
    public PopupService(GameObject blocker);           // fondo a pantalla completa con Button -> OnBlockerTapped
    public bool HasOpen { get; }
    public void Open(PopupView popup);                  // apila, coloca blocker justo debajo (SetSiblingIndex)
    public void Close(PopupView popup);                 // desapila; oculta blocker si la pila queda vacía
    public bool HandleBack();                           // si hay popup: top.Cancel(); true
    void OnBlockerTapped();                             // si top.CloseOnOutsideTap: top.Cancel()
}
// UI/PopupView.cs
public abstract class PopupView : MonoBehaviour {
    public virtual bool CloseOnOutsideTap => true;
    public abstract void Cancel();                      // resultado "cancelar" + cerrar
    protected PopupService Popups { get; private set; }
    public void Init(PopupService popups);
}
// UI/StoryActionsPopup.cs
public enum StoryAction { Read, Delete, Dismiss }
public void Show(StoryInfo story, Action<StoryAction> onResult);   // Cancel() => Dismiss
public void Close();                                               // la pantalla lo cierra tras un borrado confirmado
// UI/ConfirmPopup.cs (genérico)   CloseOnOutsideTap = false
public void Show(string message, string confirmLabel, Action<bool> onResult); // Cancel() => false
// UI/ToastView.cs (MonoBehaviour en PopupLayer, encima de todo; NO entra en la pila de PopupService)
public void Show(string message);   // duración/animación según docs/ui sección 0; un toast nuevo sustituye al anterior
```
- Cada popup llama a `onResult` **una sola vez** (desactivar botones al primer clic). `Read` y `Dismiss` cierran el popup de historia antes de invocar `onResult`; **`Delete` no lo cierra**: el `ConfirmPopup` se apila encima, y si se cancela vuelve a verse el popup de historia.
- `ConfirmPopup` y `ChoicePopup`: `CloseOnOutsideTap = false` (tocar fuera no hace nada); **solo Atrás** = Cancelar. `StoryActionsPopup`: tocar fuera y Atrás = cerrar (`Dismiss`).
- "Cancelar por defecto": `EventSystem.current.SetSelectedGameObject(cancelButton)` al abrir + estilo según ux-ui.
- El `ToastView` es compartido (lista y Cargar), lo crea la escena y `AppBootstrap` lo pasa por `Init`; sustituye a cualquier línea de estado en pantalla. No bloquea toques ni consume Atrás.
- `BackButtonRouter.Init(navigator, popups, quit)`: primero `popups.HandleBack()`, después pantalla, después `Pop`/salir.

### Flujo en `StoryListScreen`
Fila -> `StoryActionsPopup.Show(info, a => ...)`:
- `Dismiss`: nada.
- `Read`: `nav.Push(ScreenId.Reader, info)` (T17).
- `Delete` (popup de historia sigue abierto): `ConfirmPopup.Show(Format(UiStrings.ConfirmDelete, name), UiStrings.Delete, ok => ...)`:
  - `false`: no se hace nada; queda visible el popup de historia.
  - `true` -> `library.Delete(id)`; éxito (o `NotFound`) -> `actionsPopup.Close()` + `Render()` + `toast.Show(Format(UiStrings.StoryDeleted, name))`; fallo -> `actionsPopup.Close()` + `toast.Show(UiStrings.DeleteFailed)` (la fila se queda).

### Encargos
| Encargo | Scripts | Tests |
|---|---|---|
| T16a Infra de popups | `PopupService.cs`, `PopupView.cs`, `ToastView.cs` (+ cableado: 3 líneas en `BackButtonRouter`, 2 en `AppBootstrap`; blocker y toast en la escena vía MCP/`SceneBuilder`) | La pila puede reutilizar `NavigationStack<T>` de Core (ya testeada). PlayMode no obligatorio. |
| T16b Popups + flujo | `StoryActionsPopup.cs`, `ConfirmPopup.cs`, `StoryListScreen.cs` | EditMode (Core/Infra): `Delete_ThenList_DoesNotContain` y `Delete_PersistsAcrossNewRepositoryInstance` en `JsonStoryRepositoryTests`; `Delete_NotFound_ReturnsNotFound` si falta. |

---

## T17 — Leer (placeholder) y conservación del scroll
**Requisitos:** plan S1 (nombre y nº kanjis), plan T17 (Atrás vuelve a la misma posición de scroll), GDD 2.4 (Atrás).

**Decisión: ocultar, no destruir.** Ya es el comportamiento de `ScreenNavigator`/`ScreenView` (`SetActive(false)`), y un `ScrollRect` desactivado conserva `content.anchoredPosition`. Lo que rompería el scroll es **re-renderizar** al volver; por eso `StoryListScreen.OnShow` solo re-renderiza si `library.Revision` cambió (T15). Volver de Leer no cambia `Revision` -> no se toca la lista. Si se re-renderiza (tras cargar/eliminar), se guarda `verticalNormalizedPosition` antes y se restaura tras `LayoutRebuilder.ForceRebuildLayoutImmediate` (clamp automático de ScrollRect).
- No hace falta persistir nada entre sesiones (GDD 2.4).

**Contrato:** `UI/ReaderScreen.cs : ScreenView` — `Init(ScreenNavigator nav)`; `OnShow(object args)` con `args` = `StoryInfo` (si no lo es, muestra texto vacío y log de error). Botón Atrás -> `nav.Pop()`. En S05 cambiará a `Load(info.Id)`; el contrato de args se mantiene.

**Scripts (2):** `ReaderScreen.cs` (nuevo), `StoryListScreen.cs` (guardar/restaurar scroll). `AppBootstrap` +1 `case`. Prefab `Reader` vía MCP/`SceneBuilder` y añadido a `_screenPrefabs`.
**Tests:** sin EditMode nuevo (lógica cubierta por `Revision_*` de T15a). Verificación del revisor en Editor: hacer scroll, Leer, Atrás -> misma posición.

---

## T18 — Cargar: análisis, guardado y diálogo de nombre duplicado
**Requisitos:** GDD 2.2 (mensaje "Historia 'X' cargada: N kanjis", Reemplazar / Conservar ambas / Cancelar), GDD 2.3 (" (2)", " (3)"), GDD 5.3 (informe), plan T18 (Cancelar sin cambios ni `.tmp`, Atrás = Cancelar, sin diálogo si el nombre no existe).

### Flujo (en `LoadStoriesScreen`)
```
OnPicked -> Decode -> parser.Parse(StoryName.FromFileName(fileName), text) -> _pending = result
  -> r = library.Import(story, ConflictPolicy.Ask)
     Imported  -> ShowResult(r.Story, report)
     Conflict  -> ChoicePopup.Show(UiStrings.NameConflictTitle /* "Nombre repetido" */,
                     Format(UiStrings.NameConflict, r.ConflictingName),
                     UiStrings.Replace, UiStrings.KeepBoth, UiStrings.Cancel, choice => ...)
                  First  -> library.Import(story, Replace)  -> ShowResult
                  Second -> library.Import(story, KeepBoth) -> ShowResult (nombre final "X (n)")
                  Cancel -> sin mensaje; nada en disco
     Failed    -> ShowError(UiStrings.SaveFailed)
  _pending = null al terminar; botón Cargar deshabilitado mientras el diálogo está abierto
```
- **Sin `.tmp` al cancelar, por construcción:** `Import(Ask)` y `Import(Cancel)` no llaman a `Save`; el único escritor es `AtomicFileWriter`, que solo se invoca tras elegir Reemplazar/Conservar. No se escribe nada "provisional" antes del diálogo. Se cubre con test.
- Reemplazar conserva el `Id` existente -> un solo fichero (escritura atómica sobre el mismo `<id>.json`).
- Atrás con el diálogo abierto: `BackButtonRouter` -> `PopupService.HandleBack()` -> `ChoicePopup.Cancel()` -> rama Cancelar. `OnHide` de la pantalla descarta `_pending`.
- `ShowResult` usa `UiStrings.StoryLoaded` ("Historia '{0}' cargada: {1}", con `{1}` = `UiStrings.KanjiCount(story.Entries.Count)`) y, si hay avisos, la línea de avisos (formato y soporte —toast o texto en pantalla— según `docs/ui/`). Ese nombre es el **final** (incluye " (2)").
- La previsualización de S02 se mantiene o se sustituye según `docs/ui/` (ux-ui); la arquitectura no depende de ella.

### Contrato
```csharp
// UI/ChoicePopup.cs (genérico, 3 botones)
public enum ChoiceResult { First, Second, Cancel }
// CloseOnOutsideTap = false; solo Atrás = Cancel
public void Show(string title, string message, string first, string second, string cancel, Action<ChoiceResult> onResult);
// LoadStoriesScreen.Init(ScreenNavigator nav, IFilePicker picker, TextDecoder decoder,
//                        StoryParser parser, StoryLibrary library, PopupService popups, ChoicePopup choice, ToastView toast)
```
**Scripts (3):** `ChoicePopup.cs` (nuevo), `LoadStoriesScreen.cs`, `UiStrings.cs`. `AppBootstrap`: ampliar `Init` (1 línea).

**Tests EditMode:**
- `StoryLibraryTests`: `Import_NoConflict_DoesNotAsk`, `Import_Conflict_Ask_DoesNotSave`, `Import_Cancel_DoesNotSave`, `Replace_KeepsSingleStoryWithNewContent`, `KeepBoth_Twice_CreatesX2ThenX3` (añadir solo los que falten de S03).
- `JsonStoryRepositoryTests` (o integración `StoryLibrary` + `JsonStoryRepository` en carpeta temporal): `ImportConflict_Cancel_LeavesNoTmpAndSameFiles`, `ImportConflict_Replace_LeavesOneFileNoTmp`.

---

## T19b — Guardado atómico en Android IL2CPP: logs a preparar
**Requisito:** plan T19b. Objetivo: que el usuario vea en `adb logcat -s Unity` qué camino tomó cada guardado y qué quedó en disco tras un force-stop.

Logs (prefijo único `[Kanji][IO]`, solo `DEVELOPMENT_BUILD || UNITY_EDITOR` con `[Conditional]` o `#if`; en release no se emiten):
1. `DiskFileSystem.Replace`: rama tomada (`move` destino nuevo / `replace` / `fallback-copy`) y, en fallback, **tipo exacto de excepción** + mensaje. Es el dato que decide el fix.
2. `AtomicFileWriter.Write`: `path`, bytes, ms, resultado; en fallo, tipo de excepción y si el borrado del `.tmp` funcionó.
3. Arranque (`AppBootstrap`, tras crear la librería): `stories: N ok, M unreadable, K .tmp` (K con `fs.ListFiles(dir, ".tmp")`) + `persistentDataPath`. Tras el force-stop esta línea es la prueba del criterio "sin truncadas ni `.tmp` visibles" (los `.tmp` existen pero no se listan).
4. `LoadStoriesScreen` (resultado de Import): `id`, nombre final, política, estado.

**Scripts (≤ 3):** `IFileSystem.cs` (`DiskFileSystem`), `AtomicFileWriter.cs`, `AppBootstrap.cs`. Si el fix del backlog hace falta: solo `DiskFileSystem.Replace` (fallback únicamente con `PlatformNotSupportedException`/`NotImplementedException`, sin borrar el `.tmp` en ese camino). Límite: 1 fix; si no basta, `bloqueos.md`.
**Tests:** si se aplica el fix, en `AtomicFileWriterTests` con `FaultyFileSystem`: `Replace_IOException_DoesNotFallBackToCopy`. APK dev: `AndroidBuild.cs` existente.

---

## Cobertura de requisitos (para confirmar con el diseñador)
| Requisito | Pieza |
|---|---|
| GDD 5.2 `Sentence.` + `10:30` | T19a `StoryParser.EndField` + `IsKnownField` |
| 2.3 fila nombre + N kanjis | `StoryListRow` (`EntryCount` + `UiStrings.KanjiCount`, confirmado) |
| Error inesperado al listar | `StoryListScreen.Render` try/catch -> `UiStrings.LibraryLoadFailed` |
| 2.3 vacía | `StoryListScreen` + `UiStrings.EmptyLibrary` |
| 2.3 más reciente primero | `Story/StoryInfo.ImportedAtUtc`, orden en `StoryLibrary.List` |
| 2.3 dañadas omitidas + aviso N | `StoryListing.UnreadableCount` |
| Plan: 200 en < 1 s | DTO v2 + `HeaderDto`; test de presupuesto; plan B índice |
| 2.3 popup Leer/Eliminar/cerrar | `StoryActionsPopup` + `PopupService` (fuera/Atrás) |
| 2.3 confirmación, Cancelar por defecto | `ConfirmPopup` (selección inicial; Atrás = Cancelar; fuera no cierra; Cancelar vuelve al popup de historia) |
| 2.3 "Historia 'X' eliminada" | `ToastView` (UI sección 0) |
| Plan S1 Leer placeholder + scroll | `ReaderScreen`; ocultar + `Revision` |
| 2.2 duplicados / 2.3 " (2)" | `ChoicePopup` + `StoryLibrary.Import(policy)` |
| 2.2 "Historia 'X' cargada: N kanjis" | `LoadStoriesScreen.ShowResult` |
| 6 sobrevive cierre | `AtomicFileWriter`; verificación T19b |
