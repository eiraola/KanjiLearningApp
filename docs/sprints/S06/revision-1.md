# S06 - Revisión ronda 1 (T28)

Tests: EditMode 191/191 OK. Compilación OK. PlayMode: `test status` devolvió 0/0 (no se reintenta; a verificar manualmente, la implementación declara 14/14).

Cumple GDD 2.5/3.3: dedup Kanji+Reading, copia de 5 campos, más reciente primero, eliminar sin confirmación con toast, texto vacío correcto. Cableado de AppBootstrap correcto; el estado del botón se recalcula en cada `Present`; `Resolve` protege de doble toque en Eliminar.

| Sev. | Ubicación | Problema | Corrección |
|---|---|---|---|
| IMPORTANTE | JsonDifficultKanjiRepository.cs:132-152 | `Load` con fichero corrupto, versión futura o un solo item sin kanji devuelve lista vacía sin copia de seguridad. El siguiente `Add` sobrescribe el fichero y se pierde toda la lista (arquitectura.md pedía copia `.corrupt` best-effort). Escenario: downgrade de app o un item dañado, luego el usuario añade un kanji. | Antes de devolver vacío, copiar el original a `difficult_kanji.json.corrupt` (try/catch). Mejor aún, descartar solo el item inválido en vez de toda la lista. |
| MENOR | KanjiPopup.cs:395 | Fallo al eliminar muestra "No se pudo guardar."; la spec UI pide "No se pudo eliminar." | Añadir `UiStrings.RemoveFailed`. |
| MENOR | KanjiPopup.cs:356-360 y DifficultListScreen.cs:227 | Listeners con lambdas nunca se desuscriben. Los objetos viven toda la app, sin impacto real. | Ninguna necesaria. |
| MENOR | KanjiPopup.cs:350-352 | Campos `[SerializeField]` declarados a mitad de clase; no hay `_mode` ni `KanjiPopupResult.Removed` como en arquitectura (desviación documentada). | Solo legibilidad. |

No se encontraron bloqueantes. Persistencia atómica vía `AtomicFileWriter`, rollback en `SaveFailed`, refresco tras eliminar (Revision + `Refresh`) y estado vacío son correctos.

VEREDICTO: CAMBIOS NECESARIOS (1 importante: pérdida de datos por sobrescritura tras Load corrupto/versión futura)
