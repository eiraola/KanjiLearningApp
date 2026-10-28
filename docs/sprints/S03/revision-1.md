# S03 — Revisión ronda 1

Alcance: ficheros listados en `implementacion.md` (T11a, T11b, T12, T13, T14) contra `plan.md`, `arquitectura.md` y GDD 5.1/5.2/6.

## Tests
- `unity status`: ready (6000.6.2f1).
- `unity command test run --mode EditMode` -> **115/115 superadas** (3,83 s). Sin errores de compilación.
- Cobertura: existen todos los tests pedidos por `arquitectura.md` para T11a, T11b, T12 y T13 (un test por fila de GDD 5.2), además de los 5 de rendimiento de T14 (≈1,6 ms para 50 000 caracteres según `implementacion.md`).

## Comprobaciones sin hallazgo
- `Kanji.Core` mantiene `noEngineReferences: true`; no hay `UnityEngine` en `Scripts/Core`. `JsonUtility` solo está en Infrastructure.
- Parser lineal: `FindClose` se detiene en la siguiente llave y el modo texto copia por rangos con `Append(text, start, count)`; 10 000 `{` seguidas siguen siendo O(n). No hay `string +=` ni `Substring` en bucles.
- Precedencia de escapes coherente entre `FindClose` y `ParseBlock` (`\}` no cierra, `\\;` = `\` + `;` literal, sin escapes fuera de bloques), tal como fija la arquitectura.
- Las filas de GDD 5.2 dan el resultado esperado: anidadas, `{` sin cierre que no consume el resto, `}` suelta, Kanji ausente o vacío (el texto crudo se conserva con llaves), duplicado (gana el primero), campo desconocido, sin bloques, kanji repetido.
- Escritura atómica: el destino no se toca antes de `Replace`; si falla la escritura del temporal o el `Replace` simulado, el fichero anterior queda intacto; `List`/`Load` ignoran `.tmp`.
- Ids: la regex bloquea `..`, `/`, `\` y la cadena vacía (no hay path traversal). Las versiones futuras devuelven `UnsupportedVersion` y el JSON corrupto devuelve `Corrupt` sin excepción.
- `StoryLibrary`: Ask/Replace/KeepBoth/Cancel según la arquitectura; `List` hace una copia ordenada de forma estable.

## Hallazgos

| Severidad | Fichero:línea | Problema | Escenario | Corrección sugerida |
|---|---|---|---|---|
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/IFileSystem.cs:37-41` + `AtomicFileWriter.cs:28-30` | El fallback de `Replace` hace `File.Copy(temp, dest, true)`, que no es atómico: trunca `dest` antes de copiar. Si la copia falla a medias, `AtomicFileWriter` borra a continuación el `.tmp` (línea 30), que era la única copia buena: se pierden la versión antigua y la nueva. El `catch` captura cualquier `IOException`, no solo las que indican "no soportado en esta plataforma". | `File.Replace` lanza `IOException` (fichero bloqueado, disco lleno) y `File.Copy` falla a mitad. Es poco probable (en Android/iOS `File.Replace` acaba en un `rename`). **A verificar en dispositivo con IL2CPP.** | Limitar el fallback a `PlatformNotSupportedException`/`NotImplementedException`. O bien, si ya se ha llegado al paso de `Replace`, no borrar el `.tmp` y recuperarlo en `Load`. |
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/IFileSystem.cs:23` | `File.WriteAllBytes` no hace `fsync`. Tras un `rename`, un corte de energía o un cuelgue del SO (no basta con cerrar la app) puede dejar `id.json` vacío en ext4/F2FS de Android. | Corte de batería justo después de guardar. | Escribir con `FileStream` + `Flush(true)` antes del `Replace`. |
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/JsonStoryRepository.cs:17` | En .NET, `$` también coincide antes de un `\n` final, así que `"abc\n"` pasa la validación de id. No permite path traversal: en Windows da `IoError` en lugar de `InvalidId`, y en Android crea un fichero con un salto de línea en el nombre. | `newId` (o un llamador) pasa un id con `\n` final. | Usar `\z` en lugar de `$` (`^[A-Za-z0-9_-]{1,64}\z`). |
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/JsonStoryRepository.cs:36-41` | `List()` deserializa cada historia completa (segmentos y entradas) solo para sacar nombre y contadores. `arquitectura.md` (línea 4 de la sección) habla de "leer cabeceras". Con muchas historias de hasta 1 MB, el listado de la biblioteca en S04 bloqueará el hilo principal. | Biblioteca con 20 historias grandes. | Usar un `HeaderDto` (version, id, name, entries) sin construir `Segment`, o guardar los contadores en el DTO. Medirlo en S04. |
| menor | `Assets/_Project/Scripts/Core/Parsing/StoryParser.cs:170,200` | El `Detail` de `UnknownField` no se recorta a 40 caracteres, como exige el contrato de `ParseWarning` en `arquitectura.md`. Un fragmento sin separador puede ocupar una frase entera. | Una frase con un `;` sin escapar y sin `:` ni `.`. | Truncar `Detail` a 40 caracteres (en `ParseWarning` o al emitirlo). |
| menor | `Assets/_Project/Scripts/Core/Parsing/StoryParser.cs:160` | Con `Sentence.` (la errata del GDD) y un `:` en el valor, el `:` gana como separador: `Sentence. Son las 10:30` da el nombre `Sentence. Son las 10` -> `UnknownField`, y la frase se pierde (con aviso). Cumple la precedencia fijada, pero es justo la combinación de dos casos de 5.2. | `{Kanji: 時; Sentence. 会議は10:30です}`. | Que el arquitecto valore usar el `.` si aparece antes del primer `:` y el texto que lo precede es un nombre de campo conocido. Si no, documentarlo como limitación. |
| menor | `Assets/_Project/Scripts/Core/Parsing/StoryParser.cs:94` | `CountLines` solo cuenta `\n`, así que los saltos de línea `\r` sueltos (Mac clásico) no suben `Line` en los avisos. El GDD solo exige `\r\n`/`\n`. | Archivo con finales de línea `\r`. | Contar también los `\r` que no van seguidos de `\n`. |

No hay hallazgos bloqueantes ni importantes. Los menores pueden ir al backlog. El primero conviene verificarlo en dispositivo antes de S04, porque afecta a la persistencia.

VEREDICTO: APROBADO
