# Pantalla Inicio (Home)

Prefab `Home` bajo `ScreenLayer`. Referencia 1080×1920. Colores/tipos: ver `guia-estilo.md`.

## Jerarquía
```
Home                      RectTransform stretch (0,0)-(1,1), SafeAreaFitter, Image Fondo #FAF7F0
└─ Content                stretch, márgenes L/R 48, T/B 64
   ├─ TitleArea           anclas top-stretch, pivot (0.5,1), alto 480, Y=0
   │  ├─ AppTitle         TMP "Kanji Learning", 96 Bold, Texto, centrado, stretch
   │  └─ Subtitle         (opcional, omitir en S01)
   └─ ButtonsArea         anclas bottom-stretch, pivot (0.5,0), alto 928, Y=64
      (VerticalLayoutGroup: spacing 32, padding 0, Child Alignment UpperCenter,
       Control Child Size W+H = true, Child Force Expand W = true, H = false)
      ├─ BtnLoadStories   "Cargar historias"    alto 160 (LayoutElement preferredHeight 160), primario
      ├─ BtnReadStories   "Leer historias"      160, primario
      ├─ BtnDifficult     "Kanjis complicados"  160, primario
      ├─ Spacer           LayoutElement preferredHeight 48
      └─ BtnQuit          "Salir"               alto 144, SECUNDARIO (Secundario, texto Texto)
```
Cada botón: `Image` (UISprite sliced) + `Button` (ColorTint) + hijo `Label` TMP 52 Medium, stretch. Altura total de ButtonsArea = 160·3 + 32·4 + 48 + 144 + 32 ≈ 832 ≤ 928.

## Textos (UiStrings)
`Kanji Learning`, `Cargar historias`, `Leer historias`, `Kanjis complicados`, `Salir`.

## Comportamiento
- Orden fijo de arriba abajo como el GDD 2.1. Los 3 primeros hacen `Push` de LoadStories / StoryList / DifficultList; Salir = `Application.Quit()` sin confirmación. Atrás en Inicio también cierra (BackButtonRouter).
- Salir es visualmente secundario (color neutro, más bajo, separado por Spacer).
