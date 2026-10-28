# S05b - Tema oscuro: sprites, WarningSurface y popup de kanji

Complementa `docs/sprints/S05b/arquitectura.md` (T1, T3) y `valoracion-ux.md`. Canvas 1080x1920, px de referencia. Tokens = campos de `UiTheme`.

## 1. WarningSurface y contrastes

`WarningSurface = #4B3410` (ámbar oscuro). Texto `TextPrimary` sobre él: 9,7:1; `TextSecondary`: 5,0:1. Contra Fondo 1,5:1 (se distingue por tono cálido + borde `Border` 2 px opcional).

Contraste calculado (WCAG, mínimo 4,5:1):
| Par | Ratio | Resultado |
|---|---|---|
| Link `#22D3EE` / Surface `#1D222A` | 8,8:1 | OK (AAA) |
| Link / SurfaceRaised `#272D38` | 7,7:1 | OK |
| TextSecondary `#A3A9B8` / Surface | 6,8:1 | OK |
| TextSecondary / SurfaceRaised | 5,9:1 | OK |
| Blanco / Success `#1B7A3A` (pulsado `#14632E`) | 5,4:1 (7,4:1) | OK |
| Blanco / Danger `#C4302B` (pulsado `#A32520`) | 5,5:1 (7,4:1) | OK |

Contra el scrim (negro 65 %): el lector queda atenuado detrás del popup y **no es interactivo**, así que no aplica el 4,5:1. Mezcla real: kanji cian atenuado `#0C4A53` sobre fondo `#07080A` = 2,0:1; TextSecondary atenuado 1,8:1. Es deliberado (jerarquía: el popup manda). Todo texto que debe leerse en un popup está sobre `Surface`, que se pinta encima del scrim (no se mezcla), así que los valores de la tabla son los reales. Superficie del popup contra el fondo atenuado: solo 1,25:1, por eso el panel lleva borde `Border` (ver 3) para que se lea su contorno.

Verde y rojo contra Surface tienen solo ~2,9:1: no dependen del color, siempre llevan texto + icono (daltonismo).

## 2. Sprites a importar (15) -> `Assets/_Project/Art/DarkUI/`

Origen: `Assets/Dark UI/...` del paquete. Todos blancos, se tiñen con `Image.color`. PPU 100, Full Rect, Bilinear, sin mipmaps.

| Destino | Origen | Tamaño | Borde 9-slice (L,B,R,T) | Uso (campo `UiTheme`) |
|---|---|---|---|---|
| `Buttons/btn_pill.png` | `Free/Button_Large_A` | 512x129 | 64,64,64,64 | `ButtonSprite`, `PanelSprite` |
| `Buttons/btn_pill_round.png` | `Free/ButtonLargeRound` | 512x129 | 64,64,64,64 | variante de botón (reserva) |
| `Buttons/btn_medium.png` | `Free/Button_Medium_A` | 386x129 | 64,64,64,64 | chips / botón pequeño (S06) |
| `Inputs/input_field.png` | `Free/InputField` | 512x128 | 64,0,64,0 | `InputSprite` (teñir a `Border`/`SurfaceRaised`) |
| `Panels/divider.png` | `Free/Divider` | 256x9 | 2,2,2,2 | `DividerSprite` |
| `Panels/circle_2px.png` | `Free/CIRCLE2PXMED` | 152x153 | sin borde, Simple | `CircleSprite` |
| `Panels/circle_4px.png` | `Free/CIRCLE4PXMED` | 154x155 | sin borde, Simple | indicador/foco (reserva) |
| `Icons/icon_back.png` | `New Icons/White Backward` | 512 (max 128) | - | `IconBack` |
| `Icons/icon_close.png` | `New Icons/White Close` | 512 (max 128) | - | `IconClose` |
| `Icons/icon_trash.png` | `New Icons/White Trash Can` | 512 (max 128) | - | `IconTrash` |
| `Icons/icon_check.png` | `New Icons/White Check` | 512 (max 128) | - | botón "Lo sabía" |
| `Icons/icon_cross.png` | `New Icons/White Cross` | 512 (max 128) | - | botón "No lo sabía" |
| `Icons/icon_plus.png` | `New Icons/White Plus` | 512 (max 128) | - | S06 "Añadir a complicados" |
| `Icons/icon_books.png` | `New Icons/White Books` | 512 (max 128) | - | Biblioteca/Inicio |
| `Icons/icon_book.png` | `New Icons/White Book 2` | 512 (max 128) | - | Lector/Cargar |

Notas para `DarkUiAssetSetup`:
- Añadir a `UiTheme`: `IconCheck`, `IconCross`, `IconPlus`, `IconBooks`, `IconBook` (los tres primeros se usan ya en T3; el resto puede quedar sin enlazar).
- Los iconos de 512 px a `maxTextureSize` 128: se muestran a 64-72 px, suficiente.
- `SlicedPpuMultiplier`: **0,8** para botones (los 64 px de borde se dibujan como 80 px = media altura de un botón de 160 -> píldora completa). Paneles y filas usan `SlicedPpuMultiplier * 2,5` (radio 32 px) fijado en el `Image`.
- Los bordes del paquete original vienen a 0 salvo InputField y Divider: se fijan en la tabla del importador. Comprobar a 984 px de ancho que no se deforman.

## 3. Popup de kanji (vista única)

### Jerarquía y anclajes (`BuildKanjiPopup`)
```
PopupKanji (PopupView, CanvasGroup)  anclas centro, ancho 984 (margen 48), alto = contenido, máx 1100 del cuerpo + 160 botones + 3x32 hueco
 Panel            Image PanelSprite Sliced (mult x2,5), color Surface; borde: Outline/Image hijo Border 2 px opcional
                  VerticalLayoutGroup: padding 48/48/48/48, spacing 32, childControlWidth/Height = true, childForceExpandHeight = false
  Body            ScrollRect (vertical, sin barra visible) + LayoutElement (preferredHeight = min(contenido, 1100))
   BodyContent    VLG spacing 32, ContentSizeFitter vertical = PreferredSize, childAlignment UpperCenter
    Kanji         TMP 220 (KanjiMaxSize) Regular, color Link (#22D3EE), centrado, Auto Size off
    Sentence      TMP 44 BodySize, TextPrimary, centrado, kanji resaltado con <color=LinkHex>
    RowReading    VLG spacing 8: Label (TMP 36 Caption TextSecondary, "Lectura") + Value (TMP 48 ReadingSize TextPrimary)
    RowMeaning    igual: Label "Significado" + Value 44 BodySize TextPrimary
    RowSentenceMeaning  igual: Label "Traducción" + Value 44 BodySize TextSecondary (cursiva no: Noto no la trae)
  SecondaryActions  RectTransform + VLG spacing 24, INACTIVO (S06: btn_medium/btn_pill Secondary de 144 alto, icono icon_plus + "Añadir a complicados")
  AnswerRow       HorizontalLayoutGroup spacing 32, childForceExpand width, LayoutElement preferredHeight 160
   BtnDidntKnow   Button + Image ButtonSprite (Sliced, mult 0,8) estilo Danger, flexibleWidth 1, alto 160
   BtnKnew        igual, estilo Success
```
Cada botón: `HorizontalLayoutGroup` centrado, spacing 16: `Icon` (Image, Preserve Aspect, 64x64, color `TextOnAccent`; `icon_cross` en rojo, `icon_check` en verde) + `Label` (TMP 52 ButtonSize Bold, `TextOnAccent`, "No lo sabía"/"Lo sabía"). Ancho de cada botón = (984-96-32)/2 = 428 px: cabe "No lo sabía" a 52 px (~330 px con icono). Si el texto no cabe (fuentes grandes del sistema): permitir Auto Size 40-52 solo en el Label.

### Orden y reglas
1. Kanji, 2. Frase, 3. Lectura, 4. Significado, 5. Traducción de la frase, 6. (hueco S06), 7. Fila de respuesta.
Filas con valor vacío: `SetActive(false)` (fila entera, label incluido). Sin kanji de frase vacío: igual con `Sentence`.
Rojo a la izquierda, verde a la derecha (verde bajo el pulgar derecho, es la acción "positiva"/más frecuente). Los botones quedan al fondo, fuera del scroll, siempre visibles.

### Estados
| Estado | Comportamiento |
|---|---|
| Normal | como arriba |
| Pulsado | `ColorBlock` pressed (`SuccessPressed`/`DangerPressed`), sin animación de escala |
| Contenido largo | solo `Body` hace scroll (máx 1100 px); `AnswerRow` fijo |
| Campos vacíos | filas ocultas, el panel se encoge |
| Cargando/Error | no aplica (los datos ya están en `KanjiEntry`) |
| Abrir/cerrar | fundido `PopupFadeSeconds` (0,15 s) panel + scrim, entrada bloqueada ese tiempo |

### Cierre y feedback
- Verde -> cierra con `Knew`; rojo -> `DidntKnow`; tocar scrim o Atrás -> `Dismissed`. Ninguno registra nada en S05b.
- Sin botón X en el popup (fuera/Atrás cubren el cierre; evita un 4º objetivo táctil). `icon_close` queda para otros usos.
- Táctil: `Button` en todo el rectángulo de 428x160. Sin sonido ni vibración en S05b (se evalúa en S07 con el registro de respuestas).
- Accesibilidad: verde/rojo siempre con icono + texto; foco/teclado: orden Rojo, Verde; outline de foco `TextPrimary` 4 px.

### Textos (`UiStrings`)
`Knew = "Lo sabía"`, `DidntKnow = "No lo sabía"`. Labels de fila: reutilizar los existentes de `UiStrings` (Lectura / Significado / Traducción); si falta alguno, añadirlo con esos valores.

## Resumen (5 líneas)
1. `WarningSurface = #4B3410` (texto principal 9,7:1, secundario 5,0:1).
2. Contraste confirmado: Link 8,8:1 y TextSecondary 6,8:1 sobre Surface (7,7 y 5,9 sobre SurfaceRaised); bajo el scrim el lector queda a ~2:1 por diseño (no interactivo), el popup va sobre Surface sin mezcla.
3. 15 sprites en inglés (btn_pill/round/medium, input_field, divider, circle_2px/4px, 8 iconos: back, close, trash, check, cross, plus, books, book); bordes 64 en botones, `SlicedPpuMultiplier` 0,8 (paneles x2,5).
4. Popup: Kanji(220, cian) > Frase > Lectura > Significado > Traducción > hueco `SecondaryActions` inactivo > `AnswerRow` (rojo izq. `icon_cross`+"No lo sabía", verde der. `icon_check`+"Lo sabía", 160 px alto, 428 px ancho).
5. Fuera y Atrás cierran (`Dismissed`), sin botón X, nada se registra; scroll solo en el cuerpo, botones fijos.
