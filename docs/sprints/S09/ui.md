# S09 - UI: Kanjis complicados por historia (T42a)

Alcance: T42b (selector) y T42c (cuadrícula + popup). GDD 2.5, 3.3. Textos nuevos van a `UiStrings`.

## Reutilización (sin componentes nuevos salvo los indicados)
- `ScreenView` + `ScreenNavigator` (Push/Pop), `ToastView`, `KanjiPopup.ShowDifficult(entry, onRemoved)` (ya hace: sin confirmación, cierra, toast `UiStrings.RemovedFromDifficult`, callback). Sin cambios en el popup salvo que `DifficultKanjiList.Remove` opere sobre la lista de la historia abierta (lógica, no UI).
- `DifficultListScreen` se **reconvierte en `DifficultStoryListScreen` (selector)** conservando `_backButton`, `_listScroll`, `_content`, `_emptyMessage`, patrón `Render()`/`Refresh()`/revision. Fila: reutilizar el prefab de fila de la lista de historias (2.3: nombre + subtítulo) en vez de `DifficultListRow`; el único cambio es el subtítulo.
- Nuevo (único): `DifficultGridScreen : ScreenView` (cuadrícula) y su prefab de celda `DifficultKanjiCell` (Button + TMP_Text). `DifficultListRow` queda sin uso: borrar.

## Pantalla 1: selector (`DifficultStoryListScreen`)
- Cabecera: `_backButton` arriba-izquierda (>=48dp) -> `_navigator.Pop()` (vuelve a Inicio); título "Kanjis complicados".
- `_listScroll` (Vertical Layout Group en `_content`, spacing 8dp, padding 16dp). Fila: alto >=64dp, toda la fila táctil; texto 1: nombre de la historia (truncar con "..." a 1 línea); texto 2 (secundario): "N complicados" ("1 complicado" si N=1). Chevron ">" a la derecha.
- Solo historias con N>=1; orden igual que 2.3 (más recientemente cargada primero).
- Vacío: `_listScroll` oculto, `_emptyMessage` centrado: "Aún no has añadido kanjis" (nueva constante, reemplaza `EmptyDifficult` si difiere).
- Tocar fila -> `Push(DifficultGridScreen, storyId)`. Al volver (OnShow) re-render si cambió la revisión (ya soportado), conservando scroll.

## Pantalla 2: cuadrícula (`DifficultGridScreen`)
- Cabecera: `_backButton` -> `Pop()` (vuelve al selector); título = nombre de la historia (1 línea, truncado).
- `ScrollRect` vertical con `_content` + `GridLayoutGroup`: `constraint = FixedColumnCount`, **4 columnas en móvil / 6 si el ancho del viewport >= 600dp** (calcular en `OnShow` y al cambiar `Screen.width`); cell size cuadrada = (ancho viewport - 2*16dp padding - (cols-1)*8dp spacing)/cols, **mínimo 64dp** (>=48dp táctil garantizado: con 360dp de ancho y 4 columnas salen ~74dp); spacing 8dp; Start Corner Upper Left, alineación Upper Left.
- Celda: fondo redondeado (mismo estilo que botones secundarios), TMP_Text con solo `entry.Kanji`, auto-size 28-48 con fuente CJK + fallback de la guía de estilo, centrado. Sin lectura ni significado. Orden: más reciente añadido primero (como el actual).
- Tocar celda -> `_popup.ShowDifficult(entry, OnRemoved)`; feedback: tinte de pulsación estándar del Button.
- `OnRemoved`: si la lista de la historia queda vacía -> `Pop()` al selector (que ya no la muestra); si no, re-render de la cuadrícula (scroll conservado). El toast lo muestra el popup.
- Vacío (solo transitorio, nunca persiste): se trata como "queda vacía" -> Pop. Si se entra con una historia sin complicados (p. ej. dato obsoleto), mostrar `_emptyMessage` "Aún no has añadido kanjis" y Atrás funciona igual.

## Popup (T42c)
- Sin cambios visuales: modo difícil actual (todos los datos, "Eliminar de la lista" + cerrar, sin confirmación). Tras eliminar: popup cierra, toast "Eliminado de complicados" (texto actual), cuadrícula actualizada o Pop si vacía.
- Atrás de sistema: cierra popup (PopupService) y, sin popup, ejecuta el Atrás de la pantalla.

## Safe area
Ambas pantallas bajo el contenedor safe-area existente de `ScreenView`; la cabecera respeta el notch, el scroll llega hasta el borde inferior con padding inferior 16dp.
