# Revision S09 T43 - ronda 1

Verificacion: recompile up_to_date, 0 errores de consola. EditMode 232/232 OK. PlayMode: no se pudo lanzar por CLI (el runner del Editor arranca con "0 test(s)", con y sin filtro, 4 intentos); fallo identificado por analisis estatico (ver B1). Sin referencias rotas a DifficultListRow/DifficultRow en Assets (grep limpio); prefab DifficultGrid enlazado en Main.unity.

| Sev. | Fichero:linea | Problema / escenario | Correccion |
|---|---|---|---|
| BLOQUEANTE | Tests/PlayMode/DifficultGridFlowTests.cs:94 (y SetUp 114) | Es el test que falla: `DifficultGridScreen` se crea con AddComponent y su `_id` (campo privado de `ScreenView`) queda en 0 = `ScreenId.Home`. `_navigator.Current.Id` devuelve Home, no DifficultGrid, y el Assert falla. `SetOn` no sirve para el campo heredado: `GetType().GetField` no ve privados de la clase base. | Cambiar la linea 94 por `Assert.AreSame(_grid, _navigator.Current)`. O asignar `_id` con reflexion sobre `typeof(ScreenView)`. |
| MENOR | Core/Library/DifficultMigration.cs:88-89 | Si `library.Load(id)` falla (historia ilegible o temporal), sus kanjis se cuentan como descartados. Despues se escribe el marcador y se borra la global: perdida irreversible. | Si `List().UnreadableCount > 0` o algun Load falla, devolver Failed sin marcar y reintentar en el siguiente arranque. |
| MENOR | App/AppBootstrap.cs:72-73 | Dos instancias de `JsonDifficultKanjiRepository` (una para el store y otra para la migracion). Hoy es inocuo porque la migracion corre antes de cualquier `For()`. | Reutilizar una sola instancia. |
| MENOR | UI/DifficultGridScreen.cs:57 | `StoryName` reconstruye `DifficultStorySelector.Build` (lee la biblioteca entera) en cada `OnShow`. | Buscar solo por id con `library.List()`. Impacto bajo. |

Criterios T39-T42 revisados sin hallazgos: cascada (Deleted idempotente + aviso solo si Count>0), migracion (Failed sin tocar, marcador tras todo OK, reintento idempotente por dedup), "Ya en complicados" por historia (`Show(entry, storyId)` usa `store.For`), selector (filtra N>=1 en orden de biblioteca, re-render por Revision), cuadrícula/popup eliminar (callback tras cerrar; vacía -> Pop), renombrado S08 conserva id (Rename usa `story.Id`), celda con `Screen.dpi/160` (4 col, 6 si >=600dp, minimo 64dp).

VEREDICTO: CAMBIOS NECESARIOS (solo por B1, un fallo de test y no de produccion)
