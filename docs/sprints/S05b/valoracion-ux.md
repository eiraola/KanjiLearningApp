# S05b - Valoración UX: paquete "Dark Theme UI" (Giniel Villacote)

Estado: propuesta, sin importar. Pantallas existentes: Inicio, Cargar, Biblioteca, Lector (kanjis azules + popup), pantallas vacías.

## Veredicto
Conviene **adoptarlo parcialmente**: sirve como kit de formas (pills y círculos) y de iconos blancos, no como tema completo. Los sprites "Free" son blancos/casi blancos de ~512x128, pensados para teñirse con `Image.color`, lo que encaja con nuestro sistema de tokens. El resto (prefabs, paneles Facebook, cintas) no aporta.

## Usar (importar solo esto, ~15 ficheros)
- `Free/Button_Large_A` y `ButtonLargeRound` (botón estándar/secundario, 9-slice) y `Button_Medium_A` (chips/botones pequeños).
- `Free/InputField` (campo de búsqueda/pegado; borde gris claro, teñir a Borde).
- `Free/Close BTN` (cerrar popup), `Free/Left Arrow` / `Left BTN` (atrás), `Free/Divider` (separador, 122 B).
- `Free/CIRCLE2PXMED` / `CIRCLE4PXMED` (botón circular, fondo de icono, indicador).
- `New Icons/`: `White Backward`, `White Close`, `White Trash Can` (borrar texto), `White Books` / `White Book 2` (Biblioteca/Lector). Revisar a mano 3-4 más (ajustes, info) solo si una pantalla los pide.
- Fuente LiberationSans: **no** (no tiene CJK).

## Descartar
Coin, Gem, Heart, Star, Shop, Inventory, Missions, Ranks, World, Facebook, Bell, Messages, Music, Sound (juego/tienda/social, no aplica); Ribbons/; Prefabs Panel y Prefabs Buttons; escena Demo; letras A-F; los ~320 iconos restantes de New Icons; sets 32/64/128 salvo necesidad; BTN_Line_4PX_* (contorno, ya cubierto por tinte).

## Paleta dark (tokens)
| Token | Hex | Contraste / nota |
|---|---|---|
| Fondo | `#14171C` | base |
| Superficie | `#1D222A` | barra, tarjetas, popup |
| Superficie elevada | `#272D38` | campos, botón secundario, chip |
| Borde | `#3A4150` | InputField, divider |
| Texto | `#E8EAF0` | 14.9:1 sobre Fondo, 13.3:1 sobre Superficie |
| Texto secundario | `#A3A9B8` | 7.6:1 Fondo, 6.8:1 Superficie |
| Primario | `#C4302B` | texto blanco 5.5:1 (AA) |
| Primario pulsado | `#A32520` | texto blanco 7.4:1 |
| Secundario (fondo) | `#272D38` + texto `#E8EAF0` | 11.5:1 aprox. |
| Kanji enlace (cyan) | `#22D3EE` | 9.9:1 Fondo, 8.8:1 Superficie, 7.7:1 Elevada (AA y AAA); decisión del usuario |
| Kanji enlace pulsado | `#A5F3FC` | 14.4:1 sobre Fondo; feedback táctil |
| Éxito (botón verde) | `#1B7A3A` | texto blanco 5.4:1; pulsado `#14632E` (7.4:1) |
| Peligro (botón rojo) | `#C4302B` | = Primario, texto blanco 5.5:1; pulsado `#A32520` (7.4:1) |
| Error | `#FF8A80` | 7.0:1 sobre Superficie |
| Desactivado | fondo `#272D38`, texto `#6B7180` | exento de AA; distinguible por baja saturación |
| Scrim | `#000000` al 65 % | más opaco que en claro |
El rojo se desatura en uso de texto/acento (`#FF8A80`); `#C4302B` solo como relleno de botón. Verificar el kanji cyan contra la mezcla real con el scrim.

## Tipografía
Noto Sans JP se mantiene (el paquete no aporta CJK). Tamaños sin cambios (96/56/52/44/36/220). Motivo para tocar: en lectura larga sobre fondo oscuro, el texto blanco "brilla"; mitigarlo con Texto `#E8EAF0` (no blanco puro) y, si se prueba, interlineado +5 % en el Lector. Decidir tras prueba en dispositivo.

## Pantallas afectadas
- Todas: recolorear fondo/superficies; botones pasan de `UISprite` a sprites pill.
- Inicio/Cargar: botón principal, InputField, iconos (Libros, Papelera).
- Biblioteca: tarjetas (Superficie + Divider), botón borrar (Trash Can), vacíos.
- Lector: popup con dos botones (verde Éxito y rojo Peligro, ambos cierran; distinguir también por icono/texto, no solo color, por daltonismo); barra superior (Atrás), fondo oscuro, kanji cyan, popup (Superficie elevada + Close BTN, kanji de 220).
- Fuera de alcance: lógica y layouts (anclajes se mantienen).

## Riesgos
1. **Lectura larga**: texto claro sobre oscuro fatiga más; ofrecer a futuro tema claro (los tokens permiten alternar) y validar con una sesión de 10 min.
2. **9-slice**: sprites ~512x128 con radio ~64 px; a alto 160 en 1080x1920 el radio escala bien, pero hay que fijar Borders en el importador (Sprite Editor, Full Rect, Pixels Per Unit coherente) y comprobar `Image Type = Sliced` sin deformar a anchos de 984 px. Los círculos pequeños no se estiran: usar Simple/Preserve Aspect.
3. **Tamaños táctiles**: iconos de 48 dp visuales dentro de un área pulsable >= 144 px (Image raycast en contenedor), nunca el sprite desnudo.
4. **Sprites casi blancos**: el tinte multiplica, así que Primario/Fondo oscuros funcionan, pero el borde claro de InputField puede verse blanco; teñir a Borde.
5. Licencia/peso: ~5,6 MB; importar solo el subconjunto y borrar demo evita ruido en el build.

## Propuestas de cambio a guia-estilo.md
1. Sustituir la paleta por la dark de arriba (mantener nombres de token) y añadir Superficie elevada, Borde, Error, Kanji pulsado, Éxito y Peligro (botones del popup).
2. Cambiar "sin imágenes externas" por "sprites 9-slice de `Assets/UI/DarkKit/` (subconjunto) teñidos por `Image.color`".
3. Botón estándar: sprite `Button_Large_A`/`ButtonLargeRound`, Sliced, radio visual ~64 px, mismos estados por ColorTint.
4. Añadir sección Iconografía: iconos blancos, 48 dp visuales, área táctil >= 144 px.
5. Regla de lectura: texto no blanco puro, prueba de legibilidad en dispositivo antes de fijar interlineado.
6. Indicar que los tokens deben vivir en un único ScriptableObject/tema para poder reintroducir el tema claro.
