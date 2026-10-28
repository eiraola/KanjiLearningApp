# S05b - Revisión ronda 1 (T5)

Alcance: ficheros de `implementacion.md` (T1-T4b) contra `plan.md` y `arquitectura.md`, con foco en `InputGate`, `TransitionOverlay`, `ScreenNavigator`, `BackButtonRouter`, `PopupService`, `PopupView`, `KanjiPopup` y la regresión S01-S05.

## Tests
- **EditMode**: `unity command test run --mode editor` -> **172/172** en verde (incluye los 6 de `InputGateTests`).
- **PlayMode**: `test run --mode playmode` (síncrono) vuelve a dar 0/0. **Causa encontrada**: el modo síncrono vuelve antes de que el runner entre en Play Mode. Con `unity command test run --mode playmode --async_tests true` y después `unity command test status` se ejecutan: **12/12** en verde (`PopupServiceTests` x5, `ReaderThemeTests` x2, `ScreenNavigatorTransitionTests` x4, `RichTextTmpEscapeTests` x1). El asmdef y `testMode` están bien; no hace falta cambiarlos. Hay que usar este modo a partir de ahora.
- Consola: compila sin errores (`compilationFailed: false`). El único error del buffer es de la herramienta Pipeline (llamada sin `condition`), no del proyecto.

## Regresión en Play (lanzada por `script eval`, invocando los `onClick` de los botones y leyendo el estado)
| Flujo | Resultado |
|---|---|
| Inicio -> Biblioteca (fundido) | Raycasts del Canvas a `false` durante el fundido y a `true` al terminar; la pantalla cambia al acabar. OK |
| Fila -> popup de historia -> Eliminar -> confirmación | El popup de debajo queda con alpha 0, sin raycasts y activo; el scrim queda entre los dos. OK |
| Cancelar confirmación | El popup de historia reaparece (alpha 1, interactuable); la confirmación se desactiva. OK |
| Tocar fuera (scrim) en el popup de historia | Se cierra el popup y luego se apaga el scrim. OK |
| Leer -> Lector (fundido) -> pulsar kanji | El popup muestra kanji, frase, lectura, significado y traducción; "No lo sabía"/"Lo sabía". OK |
| Verde x2 + rojo en el mismo frame | Se cierra una sola vez y sin errores. OK |
| Tocar fuera / rojo | Se cierra; al terminar el fundido no queda nada activo. OK |
| Atrás (botón de la barra) en el Lector | Fundido de vuelta a la Biblioteca. OK |
| Cargar -> Elegir (`sample-utf8`) -> diálogo "Nombre repetido" -> Cancelar | OK; no se modifican datos |
| Cerrar el último popup y abrir otro en el mismo frame (prueba forzada) | El scrim acaba con alpha 1 y el popup nuevo queda arriba. OK |

No probado (a propósito o porque no se puede desde aquí): **borrar de verdad** (para no tocar los datos del usuario; leyendo el código, `DeleteStory` cierra el popup de abajo después de la confirmación y es correcto), la tecla Atrás física (el router lee `Keyboard.current`; el camino `HandleBack` está cubierto por el código y es el mismo que `Cancel`), que el scroll del Lector se conserve con una historia larga (la historia de prueba no hace scroll) y el APK en el móvil.

## Hallazgos
| Severidad | Fichero:línea | Problema | Escenario | Corrección sugerida |
|---|---|---|---|---|
| menor | `Assets/_Project/Scripts/UI/TransitionOverlay.cs:41-53` | Si la corrutina `Process` se corta (GameObject desactivado o destruido a mitad del fundido), `_gate.Release()` no se llama y la entrada queda bloqueada para siempre. | Hoy no hay ningún flujo que desactive el overlay; solo pasaría por cambios futuros de escena o jerarquía. | En `OnDisable`: si `_running`, `_pending.Clear()`, `_running = false`, `_gate.Release()` y `SetAlpha(0)`. |
| menor | `Assets/_Project/Scripts/App/AppBootstrap.cs:60-61` | `_theme` y `_transition` se usan sin comprobar si son null: un fallo de cableado lanza NRE en `Awake` y deja la app en blanco, sin un mensaje claro. | Escena regenerada sin `UiTheme_Dark.asset`, o con la referencia rota. | `Debug.LogError(..., this); return;` igual que se hace con `Home`. |
| menor (a verificar) | `Assets/_Project/Scripts/UI/PopupView.cs:121-126` + `PopupService.cs:82` | `OnDisable` llama a `Finish` -> `onDone` -> `SetActive(false)` sobre un objeto que ya se está desactivando. Unity puede avisar de que se cambia el estado activo durante la desactivación. | Salir de Play o descargar la escena justo durante el fundido de cierre de un popup. | En `OnClosed`, comprobar `popup.gameObject.activeSelf`, o no invocar `onDone` desde `OnDisable` y aplicar solo el estado final. |
| menor | `Assets/_Project/Scripts/UI/ReaderScreen.cs:122-124` | Si falla la carga, el toast se enseña mientras entra el Lector y el fundido encadenado de vuelta lo tapa unos 0,2 s. Es solo cosmético. | Abrir una historia corrupta o que ya no existe. | Mostrar el toast después del `Pop()`, o dejarlo como está y revisarlo en el móvil. |
| menor | `ReaderScreen.cs:176,216,234,242`, `LoadStoriesScreen.cs:135`, `AppBootstrap.cs:99` | Warnings UAC0009 (`DEVELOPMENT_BUILD` está obsoleto en `Conditional`). Vienen de sprints anteriores, pero contradicen "consola limpia". | Cada compilación. | Pasar a `Conditional("DEBUG")`/`UNITY_EDITOR`, o anotarlo en el backlog. |

No hay bloqueantes ni importantes. Se cumplen los criterios de T1-T4:
- fundido de 0,2 + 0,2 s entre pantallas y de 0,15 s en popups, con las duraciones en `UiTheme`;
- toques y Atrás bloqueados mediante el `CanvasGroup` raíz y `InputGate`;
- popup de debajo oculto pero intacto;
- primer Push y salida sin fundido;
- popup de kanji de vista única que se cierra una sola vez;
- `Core` sin dependencias de UI.

Rendimiento: el overlay y el scrim con alpha 0 no se dibujan (`m_CullTransparentMesh: 1` en la escena), y `Update` no reserva memoria.

Pendiente para el usuario (T5): APK en el móvil (legibilidad, fundidos, popup y scroll conservado con una historia larga).

VEREDICTO: APROBADO
