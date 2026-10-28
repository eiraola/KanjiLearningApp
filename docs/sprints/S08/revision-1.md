# S08 - Revisión ronda 1

**Pruebas**: recompile al día; EditMode 209/209; PlayMode 19/19. Consola sin errores propios. No se probó en dispositivo (picker, teclado).

## Verificado correcto
- Duplicados: `Validate` recorta, usa `StoryName.AreSame` (OrdinalIgnoreCase), excluye `ownId` (renombrar solo mayúsculas permitido) y aplica el chequeo al nombre de respaldo.
- `Rename` conserva Id, `ImportedAtUtc` y contenido; `Revision++` solo si guarda; error de Load se propaga.
- Cancelar/Atrás no invocan el callback; `CloseOnOutsideTap=false`. `Func<string,string>`: null cierra, texto deja abierto y muestra el error; se limpia al teclear (`Refresh`).
- Restos del diálogo de conflicto: `ChoicePopup`, `ConflictPolicy`, `AskConflict` y cadenas retiradas de código. Pendiente solo en docs de `ux-ui` (S04-biblioteca.md 146-161; S08-nombre-historia.md:50).
- Listeners se registran en Awake y el popup vive toda la sesión (sin fuga). `Update` solo con popup abierto, sin allocations.

## Hallazgos
| Sev. | Fichero:línea | Problema | Corrección |
|---|---|---|---|
| Menor | NameInputPopup.cs:120 | "Sin cambios" compara texto crudo con `InitialText`: "Nombre " (espacio final) activa Aceptar y hace un Rename sin cambio real (Revision++ y toast). | Comparar `r.Name == InitialText.Trim()` con `Ordinal` (los cambios solo de mayúsculas siguen permitidos). |
| Menor | StoryListScreen.cs:139-140 | Fallo de guardado/`NameInvalid` muestra toast y error en línea a la vez; la cadena se llama `DifficultSaveFailed` (de S09), nombre engañoso. | Usar `UiStrings.SaveFailed` y solo error en línea. |
| Menor | LoadStoriesScreen.cs:62-65,118 | Si `OnHide` pone `_pending=null` con el popup abierto (p. ej. cambio de pantalla programático), Aceptar lanzaría `ArgumentNullException`. No alcanzable hoy (popup modal). | Guardia `if (_pending == null) return null;` en `OnNameAccepted`. |
| Menor | LoadStoriesScreen.cs:134-138 | Rama `Duplicate` en `FinishImport` es código muerto (ya se gestiona en `OnNameAccepted`). | Eliminar. |
| Menor | implementacion.md | Altura del popup con teclado y flujo completo (importar, editar, duplicado, persistencia) sin probar en dispositivo/Play. | Prueba manual en APK. |

Sin bloqueantes ni importantes.

VEREDICTO: OK
