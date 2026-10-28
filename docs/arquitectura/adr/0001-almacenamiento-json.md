# ADR 0001 — Almacenamiento local: JSON ya parseado, un archivo por historia + índice

Estado: aceptado · Fecha: 2026-10-22

## Contexto
GDD §6: guardar historias (texto + bloques analizados) y la lista de complicados; sobrevivir a cierres y actualizaciones; crecer con campos nuevos sin perder datos; no conservar el archivo original. Volumen: decenas de historias de hasta 1 MB.

## Decisión
- Raíz: `Application.persistentDataPath/kanji/` (no se borra al actualizar la app en Android).
  ```
  kanji/index.json                 { schemaVersion, stories:[{id,name,kanjiCount,importedAtUtc}] }
  kanji/stories/<id>.json          { schemaVersion, parserVersion, id, name, importedAtUtc,
                                     kanjis:[{kanji,reading,meaning,sentence,sentenceMeaning}],
                                     segments:[{text, kanjiIndex}] }   // kanjiIndex=-1 → texto plano
  kanji/difficult.json             { schemaVersion, items:[{kanji,reading,meaning,sentence,sentenceMeaning,addedAtUtc}] }
  ```
- `id` = GUID (el nombre visible puede repetirse o tener caracteres no válidos en rutas).
- La lista se pinta solo con `index.json`; la historia completa se carga al abrirla.
- Complicados: archivo único con **copia completa** de los 5 campos (independiente de las historias, GDD §3.3); dedup por `KanjiKey` (Kanji+Reading, trim + NFC).
- Serialización: `JsonUtility` sobre DTOs `[Serializable]` en `Kanji.Infrastructure`; el dominio de `Core` no conoce los DTOs (mapeo explícito). Sin paquete extra.
- Escritura atómica: escribir `*.tmp` y reemplazar (`File.Replace`/`Move`) para no corromper si la app muere a mitad. Orden en import: primero historia, luego índice. Al arrancar, si el índice no cuadra con `stories/`, se reconstruye desde los archivos.
- Versionado: `schemaVersion` entero por archivo. Al leer: si `< actual`, cadena de migraciones `Migrate_vN_to_vN+1` (sobre DTO o JSON crudo) y se reescribe; si `> actual` (downgrade), no se toca y se muestra error. Campos nuevos = opcionales con valor por defecto (JsonUtility ignora desconocidos y pone default a ausentes).
- `parserVersion` permite, en el futuro, saber qué historias se analizaron con reglas antiguas (no se puede re-parsear porque no se guarda el original).

## Alternativas
- **Guardar el `.txt` original y parsear al abrir:** permite re-parsear con parser mejorado, pero contradice GDD §6 y duplica el coste de apertura (requisito <2 s). Descartado; si el usuario lo quisiera, basta añadir un campo `sourceText` en una versión de esquema futura.
- **Un único JSON con todo:** reescribe todo en cada cambio y crece sin límite. Descartado.
- **SQLite / PlayerPrefs:** SQLite exige plugin nativo; PlayerPrefs no sirve para MB de texto. Descartado.
- **Newtonsoft (`com.unity.nuget.newtonsoft-json`):** más flexible para migraciones; se adoptará solo si una migración lo requiere (cambio local a `Infrastructure`).

## Consecuencias
- Repositorios testeables en EditMode apuntando a una carpeta temporal.
- Borrar historia = borrar su archivo + actualizar índice; no toca `difficult.json`.
- Un archivo de 1 MB de texto ≈ 1,5–2 MB de JSON; aceptable.
