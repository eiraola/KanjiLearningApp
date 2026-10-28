# S05 - Lector: especificación de UI

Una sección por pantalla; cada programador lee solo la suya + "Común". Canvas 1080x1920 (match 0.5), solo vertical, uGUI + TMP Noto Sans JP SDF (fallback para raros). Colores, botones y popups de `guia-estilo.md` y `S04-biblioteca.md` (sección 0: plantilla de popup, orden de Atrás, toast). Textos en `UiStrings`. Decisiones técnicas de `docs/sprints/S05/arquitectura.md` (no se repiten aquí).

## 0. Común

### Textos exactos (`UiStrings`)
| Constante | Texto |
|---|---|
| `StoryOpenFailed` | `No se pudo abrir la historia.` |
| `ReaderLoading` | `Cargando…` |
| `ShowMore` | `Mostrar más` |
| `Close` (ya existe) | `Cerrar` |
| `FieldReading` | `Lectura` |
| `FieldMeaning` | `Significado` |
| `FieldSentenceMeaning` | `Traducción de la frase` |

No hay botón ni constante "Cerrar historia": la salida es el botón Atrás de la barra (decisión: un solo control para una sola acción; `_closeStoryButton` se omite).

### Tokens usados (todos de la guía; sin colores nuevos)
Fondo `#FAF7F0`, Superficie `#FFFFFF`, Texto `#1F2430`, Texto secundario `#5A6070`, **Kanji pulsable `#0B57D0`**, Scrim `#000000` 55 %.

---

## A. Lector (`ReaderScreen`, T21) - prefab `Reader` (variante de `ScreenTemplate`)

```
Reader                   stretch, SafeAreaFitter, Image Fondo
├─ TopBar                (plantilla) altura 144, Image Superficie
│  ├─ BtnBack            144x144, anclado izq., "<" (texto 52, botón secundario) -> nav.Pop()
│  └─ Title              TMP 44 Bold, Texto, 1 línea, Overflow Ellipsis, alineado izq., margen L 16 / R 48, richText=false,
│                        parseCtrlCharacters=false; muestra info.Name
├─ Scroll                ScrollRect (Vertical, Elastic, inertia on, decelerationRate 0.135, scrollSensitivity 40, sin scrollbar);
│                        anclas stretch, Top 144, resto 0
│  └─ Viewport           stretch, RectMask2D + Image transparente (raycast) + Canvas anidado + GraphicRaycaster
│     └─ Content         anclas/pivote (0.5,1), VerticalLayoutGroup (padding L/R 48, Top 32, Bottom 96, spacing 0,
│        │               childControlHeight/Width true, childForceExpandHeight false) + ContentSizeFitter V=Preferred
│        └─ ChunkTemplate (inactivo) TMP + LinkClickHandler (T22)
└─ LoadingText           TMP 44 Regular, Texto secundario, centrado H/V sobre el Scroll, "Cargando…"; inactivo por defecto
```

### Texto de lectura (plantilla de trozo)
- Fuente Noto Sans JP, peso Regular. **Tamaño 48** (la guía da 44 a "cuerpo"; la lectura de japonés largo en vertical usa 48 ≈ 18 dp en 411 dp de ancho: kanji legibles sin ampliar). Autosize desactivado.
- **Interlineado: `lineSpacing = 10`** (altura de línea efectiva ≈ 1,55 x tamaño ≈ 74 px). `paragraphSpacing = 0` (los saltos del original se respetan tal cual; las líneas en blanco ya vienen en el texto). `characterSpacing = 0`.
- Alineación izquierda, arriba. Sin justificado. `textWrappingMode = Normal`; asignar el asset de *Line Breaking Rules* por defecto de TMP (kinsoku: no empezar línea con `。、」`).
- Color de texto `#1F2430` (14:1 sobre Fondo). Ancho de línea = 1080 - 96 = 984 px ≈ 20 caracteres por línea.
- Márgenes: laterales 48, arriba 32 (bajo la barra), abajo 96 (el último párrafo no pega al borde).

### Enlace (kanji pulsable)
- **Color `#0B57D0`**: 6,3:1 sobre Fondo `#FAF7F0` (AA >= 4,5). Se pasa como `_linkColor` (`ReaderScreen`, `#0B57D0` en el prefab).
- **Sin subrayado** (decisión: el builder de T20 ya emite solo color; el azul frente a `#1F2430` es distinguible a 48 px y el subrayado ensuciaría kanjis seguidos). Sin cambio de peso.
- Sin feedback visual al pulsar (el popup abre en el mismo toque). Sin vibración ni sonido.

### Área táctil efectiva de un kanji
- Impacto exacto sobre la celda del glifo: **48 px de ancho x ~74 px de alto (línea)** = 18 x 28 dp, por debajo de 48 dp. Es una excepción asumida de la guía por ser texto corrido (no hay forma de ensanchar sin invadir el kanji vecino). Mitigación ya prevista en arquitectura: si T24 muestra fallos de puntería, `LinkClickHandler` pasa a `FindNearestLink` con distancia máxima **24 px**; no se cambia nada más.
- Umbral de arrastre ≈ 8 dp (arquitectura): arrastrar nunca abre popup.

### Estados
| Estado | Qué se ve |
|---|---|
| Cargando | Barra con título visible, `Scroll` vacío. `LoadingText` se activa si pasan > 0,3 s sin primer trozo; se oculta al asignar el primero. Sin spinner. |
| Con texto | Trozos rellenados uno por frame; el usuario puede hacer scroll mientras se rellena (el contenido crece por debajo). Arranca arriba (`Content.anchoredPosition = 0`). |
| Sin kanjis | Texto normal sin enlaces, sin error ni aviso. |
| Historia vacía | `Scroll` vacío, sin mensaje, sin error. |
| Error (no se pudo leer / `args` no válido) | **No hay pantalla de error**: toast `No se pudo abrir la historia.` (sección 0 de S04: abajo, 3 s) y vuelta a la lista un frame después. Sin diálogo. |

### Navegación
- BtnBack / tecla Atrás (sin popup) -> `Pop` a la lista, que conserva su scroll (S04). Sin estadística ni toast al salir (S07).
- Orden de Atrás: popup de kanji -> pantalla.
- Transición sin animación.

### Referencias serializadas
`_backButton`, `_title`, `_scroll`, `_content`, `_chunkTemplate`, `_loadingText`, `_linkColor`, `_maxChunkChars` (4000). Todos los TMP de contenido del usuario: `richText=false` salvo los trozos (`richText=true`), `parseCtrlCharacters=false` en todos.

---

## B. KanjiPopup vista 1 (T22) - prefab `KanjiPopup`, script `KanjiPopup`

Plantilla de popup de S04 (Scrim + Panel 984 de ancho, padding 48, spacing 32, Superficie). **Tocar fuera cierra; Atrás cierra** (solo el popup; el lector no se mueve). Aparición: fundido 0,15 s; cierre instantáneo.

```
KanjiPopup
├─ Scrim                 Button -> Close
└─ Panel                 VerticalLayoutGroup (padding 48, spacing 32), ContentSizeFitter V=Preferred
   ├─ Body               ScrollRect vertical (sin scrollbar, Elastic) + LayoutElement preferredHeight = min(contenido, _maxBodyHeight)
   │  └─ Viewport/BodyContent   RectMask2D; VerticalLayoutGroup spacing 32, childControlWidth/Height, ContentSizeFitter V=Preferred
   │     ├─ Kanji        TMP 220 Regular, Texto, centrado, 1 línea (autosize min 96 / max 220 para palabras de varios kanjis; sin wrapping)
   │     ├─ Sentence     TMP 44 Regular, Texto, centrado, wrapping, interlineado 10
   │     └─ Details      (vista 2; inactivo, sección C)
   ├─ BtnShowMore        botón estándar (Primario, alto 160, ancho 888) "Mostrar más"
   └─ BtnClose           botón secundario (alto 160, ancho 888) "Cerrar"
```
- Los botones quedan **fuera** del scroll: siempre visibles. Orden fijo: Mostrar más (principal) arriba, Cerrar abajo (igual que `StoryPopup`).
- `_maxBodyHeight = 1100` px (≈ 57 % del alto; con botones 160+160+32+32 y padding 96 el panel mide como máximo ~1580 y cabe en el safe area). El panel nunca supera safe area - 96.
- `richText=false`, `parseCtrlCharacters=false` en `Kanji`, `Sentence` y campos de vista 2.
- **Vista 1 no muestra lectura ni traducción.** `Sentence` vacía/blanca: la línea se oculta (`SetActive(false)`).
- Cada apertura: vista 1, scroll del cuerpo arriba.
- Estados: único (sin carga ni error). Sin sonido ni vibración.
- Referencias: `_kanji`, `_sentence`, `_btnShowMore`, `_btnClose`, `_scrim`, `_body`, `_bodyLayout`, `_bodyContent`.

---

## C. Vista 2 "Mostrar más" (T23) - mismo prefab `KanjiPopup`

Pulsar `Mostrar más`: se activa `Details` y **desaparece** `BtnShowMore` (queda solo `Cerrar`, sin "Añadir a complicados"). Sin animación; el panel se reajusta (`FitBody`) y el scroll del cuerpo no se mueve de arriba.

```
Details                  VerticalLayoutGroup spacing 32, childControlWidth/Height
├─ RowReading            VerticalLayoutGroup spacing 8
│  ├─ Label              TMP 36 Regular, Texto secundario, izq.: "Lectura"
│  └─ Value              TMP 44 Regular, Texto, izq., wrapping
├─ RowMeaning            igual: "Significado"
└─ RowSentenceMeaning    igual: "Traducción de la frase"
```
- Etiqueta encima del valor (no en columnas: el ancho es de 888 px). Alineación izquierda en `Details` (el kanji y la frase de arriba siguen centrados).
- Cada fila se oculta entera (etiqueta + valor) si su valor es nulo/blanco. Si las tres están vacías, `Details` aparece sin filas y solo queda `Cerrar`.
- Valores largos: wrapping sin límite de líneas; el cuerpo se desplaza dentro del popup cuando supera `_maxBodyHeight` (gesto de arrastre en el cuerpo; los botones no se mueven).
- Cerrar / fuera / Atrás: cierra el popup; el scroll del lector queda igual. Reabrir (de cualquier kanji) empieza en vista 1.
- Referencias añadidas: `_details`, `_rowReading`, `_rowMeaning`, `_rowSentenceMeaning`, `_reading`, `_meaning`, `_sentenceMeaning`.
