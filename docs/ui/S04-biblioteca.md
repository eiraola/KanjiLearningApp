# S04 - Biblioteca: especificación de UI

Una sección por pantalla/diálogo; cada programador lee solo la suya + "Común". Canvas 1080x1920 (match 0.5), solo vertical, uGUI + TMP (Noto Sans JP con fallback), solo `Image` sólida / `UISprite`. Colores, tamaños y botones de `guia-estilo.md`; plantilla de pantalla de `pantallas-vacias.md`. Textos en `UiStrings`.

## 0. Común (T15-T18)

### Capas y orden de Atrás (Android)
- Las pantallas viven en `ScreenLayer`; popups y diálogos en `PopupLayer` (por encima, hijo del Canvas, no dentro del SafeAreaFitter de pantalla; el scrim ocupa todo el Canvas y el contenido del popup respeta safe area).
- La tecla/gesto Atrás se resuelve de arriba abajo, consumiendo un solo nivel por pulsación: **1) diálogo de confirmación/duplicado -> 2) popup de historia -> 3) pantalla** (`HandleBack()`). Nunca cierra dos niveles a la vez.
- Mientras hay popup/diálogo, el scrim (`#000000` 55 %, `Image` con raycastTarget) bloquea toques a la pantalla de debajo.

### Plantilla de popup/diálogo
```
<Nombre>Popup            stretch (PopupLayer), CanvasGroup
├─ Scrim                 stretch, Image #000000 a 55 %, Button (transición None); tocar = "cerrar" solo si la sección lo dice
└─ Panel                 anclas centro (0.5,0.5), ancho 984 (stretch con márgenes L/R 48 en tablet), alto = contenido
                         Image Superficie, radio visual (UISprite Sliced), VerticalLayoutGroup padding 48, spacing 32,
                         ContentSizeFitter Vertical=Preferred; el panel nunca sobrepasa el safe area (máx. alto = safe area - 96)
```
- Aparición: fundido 0->1 en 0,15 s (CanvasGroup alpha, tiempo no escalado). Cierre: instantáneo. Sin sonido ni vibración.
- Botones de popup: botón estándar (alto 160, ancho completo del panel, texto 52). Separación vertical 32.

### Toast de mensaje (informativo, no bloqueante)
`Toast`: hijo de `PopupLayer`, anclas bottom-stretch, márgenes L/R 48, Y = 48 sobre el borde inferior del safe area, alto mínimo 144 (ContentSizeFitter), Image `#1F2430`, texto TMP 44 Regular blanco (`#FFFFFF`, 14:1), centrado, máx. 2 líneas con ellipsis, padding 24. No recibe toques (raycastTarget off). Visible 3 s (fundido de entrada 0,15 s, salida 0,3 s); un toast nuevo sustituye al anterior. Para accesibilidad el mensaje queda también como texto, nunca solo color.

### Contador de kanjis
Texto `{N} kanjis`; N=1 -> `1 kanji`; N=0 -> `0 kanjis`. Entero plano, sin separador de miles. Helper único `UiStrings.KanjiCount(int)`.

---

## 1. Lista de biblioteca (T15) - prefab `StoryList` (ya existe, variante de `ScreenTemplate`; script `StoryListScreen`)

Title = `Leer historias` (ya en plantilla). Se sustituye `PlaceholderText` por:

```
StoryList                stretch, SafeAreaFitter, Image Fondo
├─ TopBar                (plantilla) BtnBack 144x144 -> Inicio
└─ Content               stretch, Top 144, márgenes L/R/B 48
   ├─ Warning            top-stretch, pivot (0.5,1), Y=-24, alto 112, Image #FBE9E7 (tinte del Primario), TMP 36 Regular, texto Texto #1F2430 (contraste 12:1), alineado izq., padding 24, 1 línea ellipsis. Activo solo si hay JSON dañado
   ├─ ListScroll         ScrollRect (Vertical, Elastic, inertia on, sin scrollbar visible; barra fina opcional 8 px Secundario); Top = 0 (sin aviso) o 136 (con aviso), Bottom 0
   │  └─ Viewport/Content   RectMask2D; VerticalLayoutGroup spacing 16, ContentSizeFitter Vertical=Preferred, ChildControlWidth/Height, padding bottom 48
   │     └─ StoryRow (prefab, uno por historia)
   └─ EmptyMessage       stretch, TMP 44 Regular, Texto secundario, centrado H/V, margen 24
```

### StoryRow (prefab)
- Root: `Button` (transición ColorTint: normal Superficie, pulsado `#E6E1D6`), Image Superficie, alto **168** (>= 144), ancho completo (984), radio visual 24.
- Hijos con HorizontalLayoutGroup padding L/R 32, T/B 24:
  - `Name`: TMP 44 Regular, Texto, 1 línea, Overflow Ellipsis, flexible width 1. Muestra el nombre completo (kanji/kana permitidos).
  - `Count`: TMP 36 Regular, Texto secundario, alineación derecha, 1 línea, ancho preferido; texto `{N} kanjis`.
- Altura fija: filas homogéneas para poder usar pooling si 200 filas no llegan a 1 s (decisión del arquitecto; la UI no depende de ello). Si hace falta virtualizar, mantener alto 168 + spacing 16.
- Toque en fila -> abre popup de historia (sección 2) con id/nombre/contador de esa fila.

### Estados
| Estado | Warning | ListScroll | EmptyMessage |
|---|---|---|---|
| Cargando (< 1 s) | oculto | oculto | oculto (no mostrar spinner si carga < 0,3 s; si más, TMP 44 secundario `Cargando…` en EmptyMessage) |
| Con historias | según daño | visible, scroll en la posición conservada o arriba la 1ª vez | oculto |
| Vacía | oculto (o visible si todas dañadas) | oculto | `No hay historias. Ve a Cargar historias` |
| Con JSON dañado | visible: `{N} historias no se pudieron leer` (N=1: `1 historia no se pudo leer`) | resto de filas | según haya o no filas |
| Error inesperado de carga | oculto | oculto | `No se pudo cargar la biblioteca.` (color Primario) |

- Orden: más reciente primero (la fila nueva queda arriba).
- Al abrir la pantalla (OnShow) se recarga la lista; el aviso de dañados se recalcula en cada recarga (no es persistente).
- La fila omitida por JSON dañado no aparece ni es tocable.

### Scroll y Atrás
- Al volver desde Lectura (T17) la lista conserva su `verticalNormalizedPosition`/offset en px (ver sección 4). Tras eliminar, la lista se refresca y se mantiene la posición en px acotada al nuevo máximo.
- BtnBack/Atrás (sin popup abierto) -> Inicio.

### Referencias serializadas (`StoryListScreen`)
`_listScroll`, `_content`, `_rowPrefab`, `_warning`, `_warningText`, `_emptyMessage`.

---

## 2. Popup de historia (T16) - prefab `StoryPopup`, script `StoryPopup`

Plantilla de popup. Scrim: **tocar fuera cierra** (sin cambios) y Atrás cierra.

```
StoryPopup
├─ Scrim                 Button: onClick = Close
└─ Panel
   ├─ Name               TMP 56 Bold, Texto, centrado, máx. 2 líneas, Overflow Ellipsis
   ├─ Count              TMP 36 Regular, Texto secundario, centrado: "{N} kanjis"
   ├─ BtnRead            botón estándar (Primario) "Leer"
   ├─ BtnDelete          botón secundario "Eliminar" (texto Texto; no rojo, para evitar toque accidental destacado)
   └─ BtnClose           botón secundario "Cerrar"
```
- Orden vertical fijo: Leer (acción principal, más cerca del pulgar zona media), Eliminar, Cerrar. Todos 160 de alto x 984-96=888 de ancho.
- Textos exactos: `Leer`, `Eliminar`, `Cerrar`.
- Estados: único (sin carga). Si la historia ya no existe al pulsar Leer (borrada fuera de la app): cierra el popup y toast `No se pudo abrir la historia.` y recarga la lista.
- Acciones: Leer -> cierra popup y `Push` pantalla Lectura (sección 4). Eliminar -> abre diálogo de confirmación (sección 3) **encima** del popup (el popup queda visible bajo un segundo scrim, o se oculta; recomendado dejarlo visible). Cerrar / scrim / Atrás -> cierra sin cambios.
- Referencias: `_name`, `_count`, `_btnRead`, `_btnDelete`, `_btnClose`, `_scrim`.

---

## 3. Confirmación de eliminar (T16) - prefab `ConfirmDeletePopup`, script `ConfirmDialog` (reutilizable)

Plantilla de popup. **Tocar fuera NO cierra** (evita cancelar/confirmar por error); Atrás = Cancelar.

```
ConfirmDeletePopup
├─ Scrim                 sin acción (solo bloquea)
└─ Panel
   ├─ Message            TMP 44 Regular, Texto, centrado, wrapping: "¿Eliminar '{X}'? No se puede deshacer."
   ├─ BtnCancel          botón secundario "Cancelar" (arriba)
   └─ BtnConfirm         botón estándar Primario "Eliminar" (abajo)
```
- Textos exactos: `¿Eliminar '{X}'? No se puede deshacer.` (comillas simples rectas), `Cancelar`, `Eliminar`. `{X}` = nombre tal cual; si es muy largo, ellipsis a 3 líneas en `Message`.
- **Foco por defecto en Cancelar**: al abrir, `EventSystem.SetSelectedGameObject(BtnCancel)`; `BtnCancel` muestra el estado Selected con un contorno de 6 px `#1F2430` (Outline o Image hija) para que el foco sea visible; mismo comportamiento con teclado/mando. Cancelar queda en la posición superior (la más cercana al pulgar); Eliminar abajo, separado 32 y nunca preseleccionado.
- Estados: normal; tras pulsar Eliminar los dos botones pasan a desactivados (color Desactivado) hasta que la operación termina (evita doble borrado). Si falla el borrado en disco: se cierra el diálogo y toast `No se pudo eliminar la historia.` (la fila permanece).
- Resultado de Eliminar: cierra confirmación y popup, la fila desaparece de la lista en < 1 s (sin animación de salida; se puede aplicar fundido 0,15 s opcional) y se muestra el toast `Historia '{X}' eliminada` (3 s) sobre la lista.
- Cancelar / Atrás: cierra solo el diálogo; el popup de historia sigue abierto; nada se borra.
- Referencias: `_message`, `_btnCancel`, `_btnConfirm`.

---

## 4. Lectura placeholder (T17) - prefab `StoryReader` (variante de `ScreenTemplate`; script `StoryReaderScreen`; la lectura real se rehará en S05)

```
StoryReader              stretch, SafeAreaFitter, Image Fondo
├─ TopBar                (plantilla) Title = "Leer"; BtnBack 144x144 "<"
└─ Content               stretch, Top 144, márgenes L/R/B 48
   └─ Card               anclas centro (0.5,0.5), ancho 984, alto por contenido, Image Superficie, VerticalLayoutGroup padding 48 spacing 24, centrado
      ├─ Name            TMP 56 Bold, Texto, centrado, máx. 3 líneas, Ellipsis
      ├─ Count           TMP 44 Regular, Texto, centrado: "{N} kanjis"
      └─ Note            TMP 36 Regular, Texto secundario, centrado: "La lectura llegará pronto."
```
- Title de la TopBar: `Leer` (fijo; el nombre va en la tarjeta para no truncarse).
- Datos recibidos de la fila tocada (nombre y contador); no se relee el disco.
- Atrás (BtnBack, tecla o gesto) = `Pop` a la lista. La lista **no se destruye ni se resetea**: conserva exactamente el offset de scroll (ver sección 1). Sin estadística ni toast en este sprint.
- Estados: único. Transición sin animación.
- Referencias: `_name`, `_count`.

---

## 5. Diálogo de nombre duplicado (T18) - prefab `DuplicateNamePopup`, script `DuplicateNameDialog`; opera sobre la pantalla Cargar

Plantilla de popup. Aparece solo si, al confirmar la carga, ya existe una historia con ese nombre. Tocar fuera **no** cierra; Atrás = Cancelar.

```
DuplicateNamePopup
├─ Scrim                 sin acción
└─ Panel
   ├─ Title              TMP 56 Bold, Texto, centrado: "Nombre repetido"
   ├─ Message            TMP 44 Regular, Texto, centrado: "Ya existe una historia llamada '{X}'."
   ├─ BtnReplace         botón estándar Primario "Reemplazar"
   ├─ BtnKeepBoth        botón secundario "Conservar ambas"
   └─ BtnCancel          botón secundario "Cancelar" (foco por defecto, contorno 6 px como en sección 3)
```
- Textos exactos: `Nombre repetido`, `Ya existe una historia llamada '{X}'.`, `Reemplazar`, `Conservar ambas`, `Cancelar`.
- Foco por defecto en **Cancelar** (acción no destructiva). Orden: Reemplazar, Conservar ambas, Cancelar (Cancelar al final, igual que la jerarquía; el foco no depende del orden).
- Pantalla Cargar: la vista previa y `Info` siguen visibles tras el scrim. Mientras el diálogo está abierto, `BtnPick` queda bloqueado por el scrim.
- Resultados:
  - Reemplazar: guarda sobre la existente; una sola historia.
  - Conservar ambas: guarda como `X (2)` (o `(3)`, ...); se muestra el nombre final en el mensaje.
  - Cancelar / Atrás: cierra, no guarda, no deja `.tmp`; la pantalla vuelve al estado "Con archivo" previo sin mensaje.
- Tras guardar (Reemplazar o Conservar ambas, y también sin duplicado): se cierra el diálogo y se muestra el mensaje `Historia '{X}' cargada: {N} kanjis` (con `{X}` = nombre final guardado y N el contador; N=1 -> `1 kanji`). Se muestra como **toast** (sección 0) sobre Cargar, 3 s; además `Message` de la pantalla (zona de vista previa) permanece según su estado "Con archivo" (no se reemplaza). Estado visual de la pantalla: sin cambios (continúa mostrando la vista previa; se permite cargar otro archivo).
- Botones desactivados durante el guardado (igual que sección 3). Error de guardado: cierra el diálogo y `Message` en estado Error con `No se pudo guardar la historia.` (color Primario).
- Referencias: `_message`, `_btnReplace`, `_btnKeepBoth`, `_btnCancel`.

---

## Notas generales para los programadores
- `SceneBuilder` construye estos prefabs; los textos viven en `UiStrings` (castellano solo en UI).
- `richText=false` en todos los TMP que muestran nombres de historia (pueden contener `<`).
- Tablet: contenido con márgenes 48 y paneles de popup a ancho máximo 984 centrados.
- Rotación bloqueada a vertical.
