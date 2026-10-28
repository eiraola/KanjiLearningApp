# ADR 0004 — Serializador JsonUtility, DTO versionado y escritura atómica

Estado: aceptada (S03). Complementa el ADR 0001 (almacenamiento JSON).

## Contexto
Hay que persistir historias (hasta ~50 000 caracteres) sin perder datos si la app se cierra a mitad de escritura, y poder añadir campos más adelante. El proyecto evita paquetes nuevos (en sprints anteriores añadir paquetes abrió diálogos modales en el Editor).

## Decisión
- Serializar con `UnityEngine.JsonUtility` (integrado, sin paquetes) sobre DTOs planos `[Serializable]` con `int version` en la raíz. Solo vive en `Kanji.Infrastructure`; `Kanji.Core` no lo conoce.
- Un fichero por historia: `<root>/stories/<id>.json`. Sin índice en S03: `List()` lee las cabeceras (decenas de historias, cada una <= 1 MB). Si más adelante hace falta, un índice se añade detrás de `IStoryRepository` sin cambiar a los consumidores.
- Escritura atómica: escribir `<ruta>.tmp` y reemplazar el destino. Acceso a disco tras `IFileSystem` inyectable para simular fallos en tests.
- Carga tolerante: campos extra o ausentes se aceptan; JSON ilegible, versión 0 o referencias fuera de rango = `Corrupt`; versión futura = `UnsupportedVersion`. Siempre como resultado, no excepción.

## Alternativas
- Newtonsoft (`com.unity.nuget.newtonsoft-json`): más potente, pero es un paquete nuevo; sin necesidad real porque el DTO es plano.
- `System.Text.Json`: no disponible de forma fiable en Unity.
- SQLite/binario: sobredimensionado y no inspeccionable.

## Consecuencias
- Limitaciones de `JsonUtility` (sin diccionarios, polimorfismo ni `null`) se esquivan con DTO plano y cadenas vacías. Si el modelo futuro las necesita (p. ej. estadísticas con diccionarios), se cambia el serializador dentro de Infrastructure sin tocar Core; el formato en disco (`version`) permite migrar.
- Cambiar el nombre de un campo del DTO es incompatible: añadir campos nuevos, y subir `version` con una migración si cambia el significado de uno existente.
