# S06 - Revisión ronda 2 (T28)

Solo se revisan las correcciones de R1. Tests del orquestador: EditMode 195/195, PlayMode 14/14.

| Hallazgo R1 | Estado | Evidencia |
|---|---|---|
| IMPORTANTE: Load corrupto/versión futura sin copia | Resuelto | JsonDifficultKanjiRepository.cs:28-66. Todas las salidas "vacías" (dto nulo, versión fuera de rango, items nulo, item sin kanji, excepción) pasan por `Unreadable(raw)`, que copia a `.corrupt` con try/catch. Si falla la lectura, `raw` es null y no se escribe. Fichero inexistente no genera copia. Tests nuevos (3 casos y fichero válido sin backup) en JsonDifficultKanjiRepositoryTests.cs:103-112. |
| MENOR: texto "No se pudo eliminar." | Resuelto | `UiStrings.RemoveFailed` (UiStrings.cs:16), usado en KanjiPopup.cs:91. |

Nota menor, no bloqueante: una segunda corrupción sobrescribe el `.corrupt` anterior (best-effort aceptado).

VEREDICTO: APROBADO
