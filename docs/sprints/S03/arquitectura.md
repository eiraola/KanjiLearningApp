# S03 — Arquitectura: parser de bloques y persistencia JSON

Cada programador lee solo su sección (T11a, T11b, T12, T13, T14) más "Decisiones comunes". Requisitos: `docs/diseno/GDD.md` 5.1, 5.2, 6 y las decisiones del usuario en `plan.md` (P5 tolerante+avisos, P3 conflicto con 3 opciones, `.` como separador, nombre = archivo sin extensión insensible a mayúsculas, `KanjiKey` = solo Kanji, escapes `\; \: \}`, 1 MB ya resuelto en S02 por `TextDecoder`).
Estado de confirmación con el diseñador: las reglas de abajo aplican literalmente la tabla GDD 5.2 + decisiones del usuario; no hay desviaciones que requieran ida y vuelta (la única adaptación, ver T12, es que `List()` lee cabeceras en vez de un índice).

## Decisiones comunes

- Rutas: `Assets/_Project/Scripts/Core/` (asmdef `Kanji.Core`, `noEngineReferences: true`: **ni `using UnityEngine` ni `Debug.Log`**), `Assets/_Project/Scripts/Infrastructure/` (asmdef `Kanji.Infrastructure`, ya existe).
- Namespaces: `Kanji.Core.Model` (modelo), `Kanji.Core.Parsing` (parser), `Kanji.Core.Library` (puertos y `StoryLibrary`), `Kanji.Infrastructure.Storage` (repositorio, escritor, FS).
- Errores esperables = resultados, nunca excepciones hacia el llamador. Sin estáticos con estado. Sin paquetes nuevos.
- Inmutabilidad: modelo de solo lectura (`IReadOnlyList`), campos de texto nunca `null` (vacío = `""`).
- Un script puede agrupar tipos pequeños y muy acoplados (límite del plan: <= 3 scripts por tarea sin contar tests).
- `Story.Id` lo asigna `StoryLibrary` (T13), no el parser: el parser devuelve `Id = ""`.

---

## T11a — Modelo + `StoryParser` (casos válidos)

Cubre: GDD 5.1 y filas 5.2 de `Sentence.`, mayúsculas, trim/`：`/`；`/`　`, orden libre, `10:30`, salto de línea, escapes.

### Scripts (3)
1. `Core/Model/StoryModel.cs` — namespace `Kanji.Core.Model`: `SegmentKind`, `Segment`, `KanjiEntry`, `KanjiKey`, `Story`, `StoryName`.
2. `Core/Parsing/ParseReport.cs` — namespace `Kanji.Core.Parsing`: `ParseWarningKind`, `ParseWarning`, `ParseReport`, `ParseResult`.
3. `Core/Parsing/StoryParser.cs` — `StoryParser`.

### Firmas públicas
```csharp
namespace Kanji.Core.Model {
  public enum SegmentKind { Text = 0, Kanji = 1 }

  public readonly struct Segment {            // Text: texto plano; Kanji: el valor de Kanji (== Entries[EntryIndex].Kanji)
    public SegmentKind Kind { get; }
    public string Text { get; }
    public int EntryIndex { get; }            // -1 si Kind == Text
    public static Segment Plain(string text);
    public static Segment ForKanji(string kanji, int entryIndex);
  }

  public sealed class KanjiEntry {            // una por bloque valido (un kanji repetido => varias entries)
    public KanjiEntry(string kanji, string reading, string meaning, string sentence, string sentenceMeaning); // null -> ""
    public string Kanji { get; } public string Reading { get; } public string Meaning { get; }
    public string Sentence { get; } public string SentenceMeaning { get; }
    public KanjiKey Key { get; }              // new KanjiKey(Kanji)
  }

  public readonly struct KanjiKey : IEquatable<KanjiKey> {  // SOLO el campo Kanji, trim, comparacion Ordinal (sin normalizar Unicode)
    public KanjiKey(string kanji);            // ArgumentException si null/vacio tras trim
    public string Value { get; }              // Equals/GetHashCode/ToString sobre Value; operadores ==, !=
  }

  public sealed class Story {
    public Story(string id, string name, IReadOnlyList<Segment> segments, IReadOnlyList<KanjiEntry> entries);
    public string Id { get; } public string Name { get; }
    public IReadOnlyList<Segment> Segments { get; } public IReadOnlyList<KanjiEntry> Entries { get; }
    public int DistinctKanjiCount { get; }    // nº de KanjiKey distintas (calculado en ctor con HashSet)
    public Story With(string id, string name); // copia compartiendo segments/entries
  }

  public static class StoryName {             // puro, sin estado
    public const string DefaultName = "Historia";
    public static string FromFileName(string fileName); // ver reglas
    public static bool AreSame(string a, string b);     // trim + StringComparison.OrdinalIgnoreCase
  }
}
namespace Kanji.Core.Parsing {
  public enum ParseWarningKind { MissingKanji, DuplicateField, UnknownField, UnclosedBrace, StrayClosingBrace, NoKanjiFound }
  public sealed class ParseWarning { public ParseWarningKind Kind {get;} public int Line {get;} /*1-based*/ public string Detail {get;} /*campo o fragmento <= 40 chars*/ }
  public sealed class ParseReport {
    public IReadOnlyList<ParseWarning> Warnings { get; }
    public int BlockCount { get; }            // bloques validos (con repeticiones)
    public int DistinctKanjiCount { get; }    // KanjiKey distintas
  }
  public sealed class ParseResult { public Story Story { get; } public ParseReport Report { get; } }
  public sealed class StoryParser { public ParseResult Parse(string name, string text); } // text null => ArgumentNullException; sin estado, reutilizable
}
```
Invariantes de `Story` producido: segmentos `Text` adyacentes fusionados y nunca vacíos; los `Segment.Kanji` apuntan a `Entries` en orden de aparición.

`StoryName.FromFileName`: quitar ruta (todo hasta el último `/` o `\`), quitar **solo la última extensión** (`a.b.txt` -> `a.b`; sin punto -> igual; `.txt` -> vacío), `Trim()`; si queda vacío -> `DefaultName`.

### Precedencia de escapes y separadores (fija; T11b la reutiliza)
Alfabeto: delimitadores de bloque solo `{` `}` ASCII. Separador de campos `;` o `；`. Separador nombre/valor `:` o `：`; si el campo no tiene ninguno, vale el primer `.`. Espacio en blanco = `char.IsWhiteSpace` (incluye `　` U+3000, tab, saltos).
Orden de resolución dentro de un bloque, de mayor a menor:
1. **Escape**: `\` seguido de `;`, `:` o `}` -> se consume el par y se emite el carácter literal; no actúa como separador ni cierra el bloque. `\` seguido de cualquier otra cosa (incl. `\\`, `\{`, fin) es un `\` literal; no hay escape de la propia barra (`\\;` = `\` literal + `\;` escapado = `\;`). Fuera de bloques **no** se procesan escapes (el `\` queda literal). Un `\` escapa también la variante japonesa? No: solo los tres ASCII.
2. `{` sin escapar dentro de un bloque abierto = el bloque abierto estaba mal cerrado (ver anidadas).
3. `}` sin escapar cierra el bloque.
4. Separador de campos (`;` `；`) sin escapar.
5. En cada campo, **el primer** `:`/`：` sin escapar separa nombre de valor; los siguientes son parte del valor (`Meaning: 10:30` -> `10:30`). Solo si no hay ningún `:`/`：` sin escapar, el primer `.` sin escapar hace de separador (`Sentence. texto`); si hay `:`, los `.` son texto (frases con `.` no se rompen). Campo sin ningún separador (p. ej. `foo` o un valor con `;` sin escapar partido en dos) -> campo desconocido (aviso T11b), salvo que esté vacío/solo blancos (se ignora sin aviso: `;` final, `;;`).
6. Nombre: trim + comparación `OrdinalIgnoreCase` contra `Kanji, Reading, Meaning, Sentence, SentenceMeaning`.
7. Valor: cada salto de línea (`\r\n`, `\n` o `\r` suelto) -> **un** espacio; luego trim (los espacios internos se conservan). Un `;` sin escapar en una frase real parte el campo: el usuario debe escribir `\;`.

### Algoritmo del parser (una pasada lineal, sin O(n²))
Estado: `StringBuilder plain` (texto pendiente), `List<Segment> segs`, `List<KanjiEntry> entries`, `HashSet<KanjiKey>`, `List<ParseWarning>`, `int line` (se incrementa al ver `\n`; `Detail` y `Line` se calculan sin volver a recorrer el texto). Nunca `string +=` ni `Substring` en bucles: usar `StringBuilder.Append(text, start, count)`.
```
i = 0
while i < n:
  c = text[i]
  if c == '{':
     j = FindClose(i)            // barrido hacia delante desde i+1: salta pares '\'+(; : }) ; se detiene en el primer '}' sin escapar
                                 // (=> cierra, devuelve su indice) o en el primer '{' sin escapar o fin de texto (=> devuelve -1)
     if j < 0: plain.Append('{'); warn UnclosedBrace; i++ ; continue   // NO consume el resto: la siguiente iteracion sigue en i+1
     blockOk = ParseBlock(i+1, j, out entry)   // recorre [i+1, j) una vez con buffer de campo reutilizable
     if blockOk: Flush(plain -> Plain segment); entries.Add(entry); segs.Add(ForKanji); 
     else:       plain.Append(text, i, j-i+1)  // bloque descartado = texto crudo con llaves (T11b: aviso MissingKanji)
     (contar '\n' del bloque para line); i = j+1
  elif c == '}': plain.Append('}'); warn StrayClosingBrace; i++
  else: plain.Append(c); i++     (o copiar rangos hasta la siguiente llave)
end: Flush; if entries.Count == 0 warn NoKanjiFound
```
Coste: cada carácter se visita como mucho 2 veces (barrido de `FindClose` + `ParseBlock`, o barrido fallido + modo texto, porque el barrido siempre se detiene en la siguiente llave) => O(n). `ParseBlock`: bucle sobre chars con un `StringBuilder buf` reutilizable y `colonPos`/`dotPos` (posiciones en `buf` del primer `:` y primer `.` sin escapar); al llegar a `;` o al final: `sep = colonPos >= 0 ? colonPos : dotPos`; nombre = `buf[0..sep)`, valor = `buf[sep+1..]`; asignar al campo (primero gana, repetido = `DuplicateField`), `buf.Clear()`. Los avisos de T11b (`MissingKanji`, `DuplicateField`, `UnknownField`, `UnclosedBrace`, `StrayClosingBrace`, `NoKanjiFound`) están declarados ya en T11a, pero T11a solo necesita emitir los que aparecen en casos válidos (ninguno, salvo `NoKanjiFound` si procede); T11b completa los avisos de error en el mismo código.

### Tests EditMode (`Assets/_Project/Tests/EditMode/Core/StoryParserTests.cs`, asmdef `Kanji.Core.Tests` ya existente)
`Parse_<caso>`, un test por caso:
1. `Example_GddSection51` -> 1 `KanjiEntry` con los 5 campos; segmentos Text/Kanji/Text (con texto antes y después); 0 avisos; `BlockCount=1`.
2. `TrailingSemicolon_IsOptional` (con y sin `;` final -> mismo resultado).
3. `SentenceWithDot_EqualsColon` (`Sentence. x`).
4. `FieldNames_AreCaseInsensitive` (`kanji`, `KANJI`, `sEnTeNcE`).
5. `FullWidthSeparators_Work` (`：` `；`).
6. `FullWidthSpaceAndTabs_AreTrimmed` (`　`, `\t` alrededor de llaves, `:` y `;`).
7. `FieldOrder_IsFree`.
8. `ColonInValue_Preserved` (`Meaning: 10:30` -> `10:30`).
9. `DotsInValue_NotSeparator_WhenColonPresent` (`Meaning: Mr. Tanaka` -> `Mr. Tanaka`).
10. `NewlineInsideValue_BecomesSpace` (`\n`, `\r\n` -> un solo espacio) y `NewlineBetweenFields_Ok`.
11. `InnerSpaces_Preserved`.
12. `Escapes_Semicolon_Colon_Brace` (`\;`, `\:`, `\}` quedan literales en el valor; `\}` no cierra).
13. `Backslash_BeforeOtherChar_IsLiteral` y `Escape_OutsideBlock_NotProcessed`.
14. `OptionalFieldsAbsent_Accepted` (solo `Kanji`) -> demás `""`.
15. `TextOutsideBlocks_Unchanged` (incl. varias líneas, `<`, `\r\n` sin tocar fuera de bloques).
16. `MultipleBlocks_OrderAndEntryIndex`.
17. `KanjiKey_TrimmedAndOrdinal` y `StoryName_FromFileName` (`a.txt`, `a.b.txt`, `ruta\x.TXT`, `.txt`, vacío) y `StoryName_AreSame_IgnoresCase`.

---

## T11b — Reglas de error del parser (misma implementación, sin decisiones nuevas)

Sigue la sección T11a. Un test por fila de GDD 5.2 en `StoryParserErrorTests.cs`. Cubre P5 (tolerante + avisos).

Comportamiento exacto (todos devuelven siempre `Story` + `ParseReport`, nunca lanzan):
| Caso | Resultado |
|---|---|
| `Kanji` ausente o vacío/blanco | Bloque descartado: su texto crudo (con llaves, sin procesar escapes) pasa al texto; aviso `MissingKanji` (con la línea del `{`); `BlockCount` no sube |
| Opcional vacío (`Reading: ;`) | Aceptado, `""`, sin aviso |
| Campo repetido | Gana el primero; `DuplicateField` con `Detail` = nombre; si el primero era un `Kanji` vacío y el repetido tiene valor, **sigue ganando el primero** (vacío -> `MissingKanji`) |
| Campo desconocido o campo sin separador (no vacío) | Ignorado; `UnknownField` con `Detail` = nombre o fragmento |
| `{` sin cierre antes del siguiente `{` o fin | Se emite `{` como texto; `UnclosedBrace`; el texto siguiente se procesa normalmente |
| `}` suelta (fuera de bloque) | Texto `}`; `StrayClosingBrace` |
| Anidadas `{ a {Kanji: x} b }` | `{ a ` texto + aviso `UnclosedBrace`, bloque interno válido, ` b ` texto, `}` suelta + aviso `StrayClosingBrace` |
| Kanji repetido | N segmentos `Kanji`, N `KanjiEntry`, `DistinctKanjiCount` = 1 |
| Sin bloques / vacío | Historia con un único segmento `Text` (o ninguno si `""`), `NoKanjiFound` |
| Bloque `{}` o `{ }` | `MissingKanji` |

Tests (uno por línea de la tabla + extras): `MissingKanji_*` (ausente, vacío, solo `　`; el texto con llaves aparece intacto en un segmento Text), `EmptyOptional_Accepted`, `DuplicateField_FirstWins_Warns`, `UnknownField_Ignored_Warns`, `FieldWithoutSeparator_Warns`, `UnclosedBrace_DoesNotConsumeRest` (el bloque válido posterior se parsea), `UnclosedBrace_AtEndOfText`, `StrayClosingBrace_IsText_Warns`, `Nested_InnerWins`, `RepeatedKanji_NSegments_OneKey`, `NoBlocks_StoryWithoutKanjis_Warns`, `EmptyText`, `WarningLine_IsOneBased` (bloque en la línea 3), `ManyUnclosedBraces_StaysLinear` (10 000 `{` seguidas: termina < 1 s, sin cuelgue), `NullText_Throws`.

---

## T12 — `JsonStoryRepository` + `AtomicFileWriter` + DTO versionado + FS inyectable

Cubre GDD 6 (sobrevivir a cierres/actualizaciones; crecer sin perder datos) y criterios de T12. ADR: `docs/arquitectura/adr/0004-serializador-jsonutility-y-escritura-atomica.md`.

### Scripts (3 en Infrastructure + puerto en Core)
- `Core/Library/IStoryRepository.cs` (namespace `Kanji.Core.Library`): `IStoryRepository`, `StoryInfo`, `RepoError`, `RepoResult`, `RepoResult<T>`. (Puerto; no cuenta para el límite.)
- `Infrastructure/Storage/IFileSystem.cs` (namespace `Kanji.Infrastructure.Storage`): `IFileSystem` + `DiskFileSystem`.
- `Infrastructure/Storage/AtomicFileWriter.cs`.
- `Infrastructure/Storage/JsonStoryRepository.cs` (DTOs como clases `internal` anidadas o en el mismo fichero).

### Firmas
```csharp
namespace Kanji.Core.Library {
  public enum RepoError { None, NotFound, Corrupt, UnsupportedVersion, InvalidId, IoError }
  public readonly struct RepoResult { public bool IsSuccess {get;} public RepoError Error {get;} public string Message {get;}
      public static RepoResult Ok(); public static RepoResult Fail(RepoError e, string message); }
  public readonly struct RepoResult<T> { public bool IsSuccess; public RepoError Error; public string Message; public T Value;
      public static RepoResult<T> Ok(T v); public static RepoResult<T> Fail(RepoError e, string message); }
  public sealed class StoryInfo { public string Id; public string Name; public int EntryCount; public int DistinctKanjiCount; }  // propiedades get-only
  public interface IStoryRepository {
    IReadOnlyList<StoryInfo> List();            // sin excepciones; ficheros corruptos se omiten; orden: Name (OrdinalIgnoreCase) y luego Id (Ordinal) => estable
    RepoResult<Story> Load(string id);          // NotFound / Corrupt / UnsupportedVersion / InvalidId
    RepoResult Save(Story story);               // crea o sobrescribe por story.Id; IoError si falla; el fichero previo queda intacto
    RepoResult Delete(string id);               // inexistente => Ok
  }
}
namespace Kanji.Infrastructure.Storage {
  public interface IFileSystem {                // todas pueden lanzar IOException/UnauthorizedAccessException
    bool FileExists(string path);
    byte[] ReadAllBytes(string path);
    void WriteAllBytes(string path, byte[] data);
    void Replace(string tempPath, string destPath); // mueve temp sobre dest (dest puede no existir); lo mas atomico posible
    void Delete(string path);                       // no falla si no existe
    void CreateDirectory(string path);
    IEnumerable<string> ListFiles(string directory, string extension); // extension ".json"; carpeta inexistente => vacio
  }
  public sealed class DiskFileSystem : IFileSystem { }  // System.IO. Replace: si dest existe File.Replace(temp,dest,null); si no File.Move. Si File.Replace lanza PlatformNotSupported/IOException de plataforma, fallback File.Copy(temp,dest,true)+Delete(temp). Sin UnityEngine.
  public sealed class AtomicFileWriter {
    public AtomicFileWriter(IFileSystem fs);
    public RepoResult Write(string path, byte[] data); // captura IOException/UnauthorizedAccessException -> IoError
  }
  public sealed class JsonStoryRepository : IStoryRepository {
    public JsonStoryRepository(IFileSystem fs, string rootDirectory); // ficheros: <root>/stories/<id>.json ; directorio creado en Save
  }
}
```
`AtomicFileWriter.Write`: `CreateDirectory(dir)` -> `WriteAllBytes(path + ".tmp", data)` -> `Replace(tmp, path)`. Si cualquier paso lanza: intentar `Delete(tmp)` (también capturando) y devolver `IoError`; el destino no se toca nunca antes del `Replace`. Un `.tmp` huérfano de un cierre abrupto se sobrescribe en el siguiente `Write` y `List`/`Load` solo miran `.json`.
`Id` válido: `^[A-Za-z0-9_-]{1,64}$` (anti path-traversal); si no -> `InvalidId` (en `Save`, `Load`, `Delete`). Ojo: `Delete` de id inválido devuelve `InvalidId`; de id válido inexistente, `Ok`.

### Serializador y DTO versionado
- **`UnityEngine.JsonUtility`** (módulo built-in; cero paquetes, cero diálogos). Texto UTF-8 sin BOM (`new UTF8Encoding(false)`). `JsonUtility` ignora campos extra y deja por defecto los ausentes (cumple "campo extra o ausente tolerado"). Limitaciones asumidas: sin `Dictionary`, sin polimorfismo, sin `null` vs vacío (por eso el DTO es plano y usa `""`).
- Formato v1 (`CurrentVersion = 1`), clases `[Serializable]` con campos públicos:
```json
{ "version": 1, "id": "a1b2", "name": "Mi historia",
  "entries":  [ { "kanji":"勉強", "reading":"べんきょう", "meaning":"estudio", "sentence":"...", "sentenceMeaning":"..." } ],
  "segments": [ { "kind":0, "text":"Hoy ", "entry":-1 }, { "kind":1, "text":"", "entry":0 } ] }
```
  Segmentos `kind=1` guardan `text:""` (se reconstruye de `entries[entry].kanji`).
- Carga: bytes -> string -> `JsonUtility.FromJson<StoryDto>` dentro de `try/catch (Exception)` -> `Corrupt` si lanza o devuelve `null`. Validaciones (todas -> `Corrupt` salvo versión): `version == 0` (campo ausente, p. ej. `{}`) -> `Corrupt`; `version > CurrentVersion` -> `UnsupportedVersion`; `version < Current` -> pasar por `Migrate(dto)` (método privado por versión, hoy sin migraciones, estructura `switch` lista); `id` vacío o distinto del nombre de fichero -> `Corrupt`; `kind` fuera de {0,1}; `entry` fuera de rango en segmento Kanji -> `Corrupt`; listas `null` -> vacías; textos `null` -> `""`.
- Ida y vuelta: `Save(story)` luego `Load(id)` debe dar una historia estructuralmente igual (mismos id, name, entries, segments). Añadir campos en el futuro = campos nuevos en DTO con valor por defecto sensato; subir `version` solo si cambia el significado de un campo existente.

### Tests EditMode (nuevo asmdef `Assets/_Project/Tests/EditMode/Infrastructure/Kanji.Infrastructure.Tests.asmdef`)
Refs: `Kanji.Core`, `Kanji.Infrastructure`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `includePlatforms: ["Editor"]`; `defineConstraints: ["UNITY_INCLUDE_TESTS"]`; mismo patrón que `Kanji.Core.Tests.asmdef`. Ficheros: `JsonStoryRepositoryTests.cs`, `AtomicFileWriterTests.cs`, helper `FaultyFileSystem.cs` (decorador de `DiskFileSystem` con flags `FailOnWrite`, `FailOnReplace`, `FailOnDelete` que lanzan `IOException`; **cada test usa carpeta temporal propia** `Path.Combine(Path.GetTempPath(), Guid)` borrada en `[TearDown]`).
Casos:
- `AtomicFileWriter`: `Write_CreatesFile`, `Write_Overwrites`, `Write_CreatesMissingDirectory`, `WriteFails_OnTempWrite_LeavesOldFileIntact`, `ReplaceFails_LeavesOldFileIntact_AndReturnsIoError`, `WriteFails_RemovesTemp`, `Write_NeverThrows`.
- Repositorio: `SaveLoad_RoundTrip_Identical` (historia con 3 segmentos, `10:30`, japonés, emoji/`<`/comillas/saltos de línea), `SaveTwice_Overwrites`, `Delete_RemovesFile`, `Delete_Nonexistent_Ok`, `Load_Nonexistent_NotFound`, `Load_ExtraField_Tolerated`, `Load_MissingOptionalField_Tolerated`, `Load_CorruptJson_ReturnsCorrupt` (basura, truncado, vacío, `null`, `{}`), `Load_FutureVersion_UnsupportedVersion`, `Load_BadEntryIndex_Corrupt`, `Load_IdMismatchWithFileName_Corrupt`, `Save_InvalidId_InvalidId` (`..\x`, `a/b`, vacío), `Save_WriteFailure_KeepsPreviousStory`, `List_StableOrder_SkipsCorruptAndTmp`, `List_EmptyOrMissingFolder_Empty`, `Save_LargeStory_50kChars_Ok`.

---

## T13 — `StoryLibrary` (solo lógica, repositorios falsos)

Sigue las secciones T11a y T12. Cubre P3 (conflicto con 3 opciones; la UI es de S04).

### Scripts (1-2)
- `Core/Library/StoryLibrary.cs` (namespace `Kanji.Core.Library`): `StoryLibrary`, `ConflictPolicy`, `ImportStatus`, `ImportResult`.

### Firmas
```csharp
public enum ConflictPolicy { Ask, Replace, KeepBoth, Cancel }
public enum ImportStatus { Imported, Conflict, Cancelled, Failed }
public sealed class ImportResult {
  public ImportStatus Status { get; }
  public Story Story { get; }              // el guardado (Imported); null en el resto
  public string ConflictingName { get; }   // nombre existente en Conflict
  public RepoError Error { get; }          // en Failed
  public string Message { get; }
}
public sealed class StoryLibrary {
  public StoryLibrary(IStoryRepository repository, Func<string> newId);   // newId: p. ej. () => Guid.NewGuid().ToString("N") (lo pasa AppBootstrap); en tests, determinista
  public ImportResult Import(Story parsed, ConflictPolicy policy = ConflictPolicy.Ask);
  public IReadOnlyList<StoryInfo> List();  // repo.List() reordenado de forma estable (Name OrdinalIgnoreCase, Id Ordinal)
  public RepoResult<Story> Load(string id);
  public RepoResult Delete(string id);     // inexistente => Ok
}
```
Reglas de `Import` (`parsed.Name` se normaliza con trim; vacío -> `StoryName.DefaultName`):
- Sin nombre coincidente (`StoryName.AreSame` contra `List()`): `Id = newId()`, `Save`, `Imported`.
- Coincide y policy `Ask`: `Conflict` sin guardar nada (`ConflictingName` = nombre existente).
- `Replace`: reutiliza el `Id` de la coincidente (la primera en orden de `List()`), guarda con el nombre nuevo -> `Imported`.
- `KeepBoth`: nombre `"<nombre> (N)"` con el menor N >= 2 que no colisione (insensible a mayúsculas) con ningún nombre existente; `Id` nuevo; `Imported`. (El `N` se calcula sobre la lista actual en el momento de guardar.)
- `Cancel`: `Cancelled`, nada guardado (sin conflicto, `Cancel` no impide importar? **Regla**: `Cancel` solo se evalúa si hay conflicto; sin conflicto importa normalmente).
- `Replace`/`KeepBoth` sin conflicto: importan normalmente (la política se ignora).
- Fallo del repositorio -> `Failed` con `Error`/`Message` del repo; nunca excepción.

### Tests (`Assets/_Project/Tests/EditMode/Core/StoryLibraryTests.cs` + fake `InMemoryStoryRepository` en `Tests/EditMode/Core/Fakes/`, con flag `FailOnSave`)
`Import_NewName_Saves_WithGeneratedId`, `Import_DuplicateName_ReturnsConflict_SavesNothing`, `Import_Conflict_IsCaseInsensitive` (`Foo` vs `foo`), `Import_Conflict_IgnoresSurroundingSpaces`, `Replace_KeepsExistingId_UpdatesContent`, `KeepBoth_AppendsSuffix2`, `KeepBoth_SkipsTakenSuffixes` (existen `a`, `a (2)` -> `a (3)`), `KeepBoth_SuffixCollisionCaseInsensitive`, `Cancel_WithConflict_SavesNothing`, `Cancel_WithoutConflict_StillImports`, `Replace_WithoutConflict_ImportsNormally`, `Import_EmptyName_UsesDefault`, `Import_RepoFailure_ReturnsFailed`, `List_StableOrderBetweenCalls`, `List_OrderIsCaseInsensitiveByName`, `Delete_Existing_Removes`, `Delete_Nonexistent_DoesNotFail`, `Load_Passthrough`.

---

## T14 — Fixture reproducible 50 000 caracteres / 200 kanjis + rendimiento

Sin decisiones nuevas. Solo tests; ningún script de producción.

### Scripts (2, en `Assets/_Project/Tests/EditMode/Core/`)
- `Fixtures/StoryFixtureGenerator.cs` (namespace `Kanji.Core.Tests`): 
```csharp
public sealed class GeneratedFixture { public string Text; public IReadOnlyList<string> ExpectedKanji; /*200 distintos, orden de primera aparicion*/ public int BlockCount; }
public static class StoryFixtureGenerator { public static GeneratedFixture Generate(int seed = 12345, int minChars = 50000, int kanjiCount = 200); }
```
  Determinismo: **no usar `System.Random`** (puede variar entre runtimes); usar un xorshift32/LCG propio sembrado con `seed`. Kanji: 200 caracteres distintos tomados de U+4E00.. (paso fijo) excluyendo ninguno de los especiales (no están en ese rango). Relleno: frases en hiragana + ASCII con `.`, `10:30`, saltos `\n` y `\r\n`, y espacios `　`. Cada kanji aparece como bloque completo (5 campos, con `Meaning: 10:30`) al menos una vez, y algunos se repiten; los bloques se intercalan con relleno hasta alcanzar `minChars` (longitud final entre `minChars` y `minChars + 500`). Sin llaves sueltas.
- `StoryParserPerformanceTests.cs`: 
  - `Generator_SameSeed_SameText`, `Generator_DifferentSeed_DifferentText`.
  - `Fixture_Has200ExpectedKeys_AndAtLeast50000Chars`.
  - `Parse_Fixture_Under1Second` (hacer un parseo previo de calentamiento de una cadena pequeña; medir con `Stopwatch`; `Assert.Less(ms, 1000)`) y comprobar `Report.Warnings` vacío, `DistinctKanjiCount == 200`, y que el conjunto de `KanjiKey` de `Story.Entries` == `ExpectedKanji`.
  - `Parse_Fixture_ConcatenatedText_Preserved` (el texto plano reconstruido de los segmentos Text, más los kanjis, contiene toda la prosa: comprobación de longitud total de segmentos Text).
  - Opcional: `SaveLoad_Fixture_RoundTrip` NO va aquí (es de Infrastructure.Tests; ya cubierto por `Save_LargeStory_50kChars_Ok`).
