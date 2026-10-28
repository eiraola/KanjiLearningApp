
## T11a — Modelo + StoryParser (casos válidos)
**Ficheros** (bajo `Assets/_Project/`): `Scripts/Core/Model/StoryModel.cs`, `Scripts/Core/Parsing/ParseReport.cs`, `Scripts/Core/Parsing/StoryParser.cs`, `Tests/EditMode/Core/StoryParserTests.cs`.
**Decisiones locales**
- `StoryParser` es sin estado: cada `Parse` crea una `Run` privada con los buffers (reutilizable y seguro entre llamadas).
- Algoritmo lineal según arquitectura (`FindClose` + `ParseBlock` con `StringBuilder` reutilizable). Solo se emiten `UnclosedBrace`, `StrayClosingBrace` y `NoKanjiFound`; `MissingKanji`, `DuplicateField` y `UnknownField` quedan para T11b (hoy: bloque sin Kanji -> texto crudo sin aviso; duplicado: gana el primero sin aviso; desconocido: ignorado sin aviso).
- `\{` se trata como `\` literal seguido de `{` sin escapar, por tanto abre/cierra anidado (coherente con la precedencia fijada).
- `Story` calcula `DistinctKanjiCount` en el constructor; `Segment` es `readonly struct` con constructor privado.
**Tests**: `unity command test run --mode EditMode --filter StoryParserTests` -> 24/24 superadas. `unity recompile` sin errores.

## T11b — Reglas de error del parser
**Ficheros**: `Assets/_Project/Scripts/Core/Parsing/StoryParser.cs` (modificado), `Assets/_Project/Tests/EditMode/Core/StoryParserErrorTests.cs` (nuevo).
**Decisiones locales**
- `MissingKanji`, `DuplicateField` y `UnknownField` se emiten con la línea del `{` del bloque (`blockLine`); `Detail` = nombre del campo tal como se escribió (o el fragmento si no hay separador). Fragmentos vacíos/blancos se ignoran sin aviso.
- Duplicado: `Set(ref campo, ...)` mantiene el primero aunque sea vacío (un `Kanji` vacío seguido de uno con valor sigue siendo `MissingKanji`).
**Tests**: `unity command test run --mode EditMode --filter StoryParser` -> 44/44 superadas (24 de T11a + 20 nuevos). `unity recompile` sin errores.

## T12 — JsonStoryRepository + AtomicFileWriter + FS inyectable
**Ficheros** (bajo `Assets/_Project/`): `Scripts/Core/Library/IStoryRepository.cs`, `Scripts/Infrastructure/Storage/{IFileSystem,AtomicFileWriter,JsonStoryRepository}.cs`, `Tests/EditMode/Infrastructure/{Kanji.Infrastructure.Tests.asmdef,FaultyFileSystem,AtomicFileWriterTests,JsonStoryRepositoryTests}.cs`.
**Decisiones locales**
- DTOs `[Serializable]` privados anidados en `JsonStoryRepository`; `List()` carga cada fichero con `Load` y omite los que fallan (sin indice, segun ADR 0004).
- Entrada sin `kanji` (vacia) o segmento nulo -> `Corrupt`; `Delete`/`Save`/`Load` con id invalido -> `InvalidId`; `Save(null)` -> `InvalidId`.
- El asmdef de tests usa `noEngineReferences: false` (el repositorio usa `JsonUtility`).
**Tests**: `unity command test run --mode EditMode` -> 92/92 superadas (T12 aporta 7 de `AtomicFileWriterTests` + los de `JsonStoryRepositoryTests`). `unity recompile` sin errores.

## T13
**Ficheros**: `Scripts/Core/Library/StoryLibrary.cs` (StoryLibrary, ConflictPolicy, ImportStatus, ImportResult); tests `Tests/EditMode/Core/StoryLibraryTests.cs` y fake `Tests/EditMode/Core/Fakes/InMemoryStoryRepository.cs` (flag `FailOnSave`, `SaveCount`).
**Decisiones locales**: `ImportResult` con fabricas estaticas; `Import(null)` lanza `ArgumentNullException` (error de programacion); `List()` ordena una copia (Name OrdinalIgnoreCase, Id Ordinal); Replace guarda con el nombre nuevo.
**Probar**: `unity recompile` y `unity command test run --mode EditMode`.

## T14
**Ficheros** (bajo `Assets/_Project/Tests/EditMode/Core/`): `Fixtures/StoryFixtureGenerator.cs` (xorshift32 propio, 200 kanjis U+4E00 + 37*i, 250 bloques con repeticiones, relleno hiragana/ASCII con `.`, `10:30`, `\n`, `\r\n`, `　`), `StoryParserPerformanceTests.cs` (5 tests).
**Decisiones locales**: el relleno usa objetivo absoluto por hueco para que la longitud final quede en [50000, 50500] (50 000 con la semilla por defecto); el test de texto reconstruido compara los segmentos Text con el texto sin bloques.
**Medido**: parseo de 50 000 caracteres / 200 kanjis ≈ 1,6 ms tras calentamiento (límite 1000 ms). `unity command test run --mode EditMode` -> 115/115 superadas.
