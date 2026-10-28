# Plantilla común de pantalla (S01: pantallas vacías)

Usada por `LoadStories`, `StoryList` y `DifficultList`. Prefab base `ScreenTemplate`, variantes por pantalla. Referencia 1080×1920.

## Jerarquía
```
<NombrePantalla>          stretch, SafeAreaFitter, Image Fondo #FAF7F0
├─ TopBar                 anclas top-stretch, pivot (0.5,1), alto 144, Image Superficie #FFFFFF
│  ├─ BtnBack             anclas left-middle, pivot (0,0.5), X=24, tamaño 144×144, secundario, TMP "<" (o "Atrás" 36) centrado
│  └─ Title               TMP 56 Bold, Texto, centrado, anclas stretch con márgenes L/R 192 (simétrico al botón)
└─ Content                anclas stretch, Top = 144 (bajo la barra), márgenes L/R/B 48
   └─ PlaceholderText     TMP 44 Regular, Texto secundario #5A6070, centrado H/V, stretch
```
(La barra se estira hacia arriba en el notch gracias a SafeAreaFitter; el alto 144 es el contenido útil.)

## Variantes
| Pantalla | Title | PlaceholderText |
|---|---|---|
| LoadStories | Cargar historias | `Pantalla en construcción` |
| StoryList | Leer historias | `Pantalla en construcción` |
| DifficultList | Kanjis complicados | `Pantalla en construcción` |

## Comportamiento
- `BtnBack` y la tecla Atrás hacen `Pop` y vuelven a Inicio (`HandleBack()` = false).
- Solo `Image` de color sólido / sprite UI por defecto. Los textos reales de estado vacío (GDD 2.3/2.5) llegarán en sprints posteriores.
