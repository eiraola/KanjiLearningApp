# Pantalla Cargar historias (`LoadStories`, S02-T08)

Prefab `LoadStories` (variante de `ScreenTemplate`, ver `pantallas-vacias.md`), script `LoadStoriesScreen`. Canvas 1080x1920, solo vertical. Colores y tamaños de `guia-estilo.md`. Solo `Image` sólida / sprite UI por defecto.

## Jerarquía
```
LoadStories              stretch, SafeAreaFitter, Image Fondo #FAF7F0
├─ TopBar                (plantilla) Title = "Cargar historias"; BtnBack 144x144 "<"
└─ Content               stretch, Top 144, márgenes L/R/B 48 (dentro del safe area)
   ├─ BtnPick            top-stretch, pivot (0.5,1), Y=-32 (bajo el borde de Content), alto 160, botón estándar (Primario), TMP 52 "Elegir archivo de texto"
   ├─ Info               top-stretch, pivot (0.5,1), Y=-224, alto 176, Image Superficie, VerticalLayoutGroup padding 24, spacing 8
   │  ├─ FileName        TMP 44 Regular, Texto, 1 línea, Overflow Ellipsis
   │  └─ FileMeta        TMP 36 Regular, Texto secundario, 1 línea: "{KB} · {codificación}"
   ├─ PreviewPanel       stretch, Top=432, Bottom=0, Image Superficie
   │  ├─ PreviewScroll   ScrollRect (Vertical, Elastic, Movement sensitivity 1), Viewport con RectMask2D, padding 24
   │  │  └─ Content/PreviewText   TMP 44 Regular, Texto, richText=false, wrapping on, Auto Size off, ContentSizeFitter Vertical=Preferred, alineación arriba-izquierda
   │  └─ MoreLabel       bottom-stretch, alto 80, TMP 36 Regular, Texto secundario, centrado, fuera del scroll (siempre visible bajo el texto)
   └─ Message            stretch (misma zona que PreviewPanel), TMP 44 Regular, centrado H/V, margen 24
```
`Info`, `PreviewPanel` y `Message` se activan según el estado (ver tabla). `PreviewScroll` ocupa PreviewPanel menos 80 inferiores si `MoreLabel` está activo.

## Estados
| Estado | BtnPick | Info | PreviewPanel | Message |
|---|---|---|---|---|
| Inicial | activo | oculto | oculto | `Elige un archivo .txt con tus historias.` (Texto secundario) |
| Cargando (selector abierto / decodificando) | desactivado (color Desactivado) | conserva lo previo | conserva lo previo | conserva lo previo |
| Con archivo | activo | visible | visible; scroll arriba (value 1) | oculto |
| Error | activo | oculto | oculto | texto del error en color Primario `#B3261E` (6.1:1 sobre Fondo) |
| Cancelar | activo | sin cambios | sin cambios | sin cambios (no muestra nada) |

Por cada resultado distinto de Cancelar/Busy se sustituye por completo el contenido anterior. Archivo vacío = estado "Con archivo" (no es error).

## Textos exactos
- Botón: `Elegir archivo de texto`
- Inicial: `Elige un archivo .txt con tus historias.`
- FileName: nombre sin extensión (p. ej. `historia1`); si no hay nombre, vacío.
- FileMeta: `{KB} KB · {codificación}`, con codificación `UTF-8`, `UTF-8 con BOM` o `Shift-JIS`. Ejemplo: `12 KB · Shift-JIS`.
- KB: 0 bytes = `0 KB`; en otro caso `ceil(bytes/1024)` (mínimo `1 KB`, máximo `1024 KB`), sin decimales ni separador de miles.
- MoreLabel (solo si RemainingChars > 0): `… ({0} caracteres más)`, `{0}` entero plano sin separador de miles (p. ej. `… (2417 caracteres más)`).
- Vista previa: primeros 3 000 caracteres (code points); vacía si el archivo está vacío (sin MoreLabel).
- Errores (literales de arquitectura):
  - `El archivo supera 1 MB. Divídelo en archivos más pequeños.`
  - `Codificación no soportada, guarda el archivo como UTF-8`
  - `No se pudo leer el archivo.`

## Interacción y feedback
- Tamaños táctiles: BtnPick 160 alto x ancho completo (984); BtnBack 144x144. Todo ≥ 144 px.
- BtnPick: pulsado = Primario pulsado; desactivado mientras se espera el selector (evita doble toque; `Busy` se ignora).
- PreviewScroll: arrastre vertical con inercia; sin scrollbar visible (barra fina opcional de 8 px, Secundario, solo en desplazamiento). Texto no seleccionable.
- Transiciones: sin animación en S02 (cambio instantáneo de estado). Sin sonido ni vibración.
- Atrás (BtnBack, tecla o gesto) vuelve a Inicio aunque haya contenido; `OnHide` limpia textos y no retiene bytes. Nada se guarda.
- Rotación: bloqueada a vertical. Tablet: el contenido conserva márgenes 48 y se estira a lo ancho.

## Notas para el programador
- Kanji: Noto Sans JP con fallback TMP dinámico (ver guía); `richText=false` para que `<` no se interprete.
- Referencias serializadas en `LoadStoriesScreen`: `_btnPick`, `_fileName`, `_fileMeta`, `_infoRoot`, `_previewRoot`, `_previewScroll`, `_previewText`, `_moreLabel`, `_message`.
- Textos en `UiStrings` (castellano solo en UI); `SceneBuilder` construye esta jerarquía en `LoadStories.prefab`.
