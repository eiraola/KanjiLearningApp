# Guía de estilo de UI

> La mantiene el agente `ux-ui`. Referencia: Canvas 1080×1920 (CanvasScaler match 0.5). Sprites 9-slice del subconjunto Dark UI (`Assets/_Project/Art/DarkUI/`) teñidos por `Image.color`. Los tokens viven en un único ScriptableObject `UiTheme` (permite reintroducir un tema claro). Detalle de sprites y popup: `S05b-tema-oscuro-popup.md`.

## Paleta (tema oscuro)
| Token | Hex | Uso / contraste |
|---|---|---|
| Fondo | `#14171C` | fondo de pantallas |
| Superficie | `#1D222A` | barra superior, tarjetas, popups |
| Superficie elevada | `#272D38` | campos, botón secundario, chip, toast |
| Borde | `#3A4150` | InputField, divider, borde de popup |
| Texto | `#E8EAF0` | principal, no blanco puro (13,3:1 sobre Superficie) |
| Texto secundario | `#A3A9B8` | placeholders, subtítulos (6,8:1 sobre Superficie) |
| Texto sobre acento | `#FFFFFF` | sobre Primario/Éxito/Peligro |
| Primario | `#C4302B` / pulsado `#A32520` | botón estándar (texto blanco 5,5:1) |
| Secundario | `#272D38` / pulsado `#3A4150` | botón Salir/Atrás (texto Texto) |
| Éxito | `#1B7A3A` / pulsado `#14632E` | botón verde "Lo sabía" (5,4:1) |
| Peligro | `#C4302B` / pulsado `#A32520` | botón rojo "No lo sabía" |
| Kanji pulsable (cian) | `#22D3EE` / pulsado `#A5F3FC` | kanjis `{...}`; 8,8:1 sobre Superficie |
| Error | `#FF8A80` | mensajes de error (7,0:1 sobre Superficie) |
| Aviso (WarningSurface) | `#4B3410` | fondo de avisos (Texto 9,7:1) |
| Desactivado | `#272D38` / texto `#6B7180` | exento de AA |
| Scrim popup | `#000000` a 65 % | fondo bloqueante |

El rojo `#C4302B` solo como relleno de botón; como texto/acento usar Error. Verde y rojo siempre con icono + texto (daltonismo).

## Tipografía (CJK + fallback TMP)
Noto Sans JP (TMP SDF dinámico, fallback para raros). Todo en TMP. LiberationSans no (sin CJK).
| Rol | Tamaño | Peso |
|---|---|---|
| Título de app | 96 | Bold |
| Título de pantalla | 56 | Bold |
| Botón | 52 | Medium |
| Cuerpo / lectura | 44 | Regular |
| Secundario | 36 | Regular |
| Kanji grande del popup | 220 | Regular |

Regla de lectura: texto no blanco puro; probar en dispositivo (sesión de 10 min) antes de fijar interlineado (+5 % candidato en el Lector).

## Espaciados y tamaños táctiles
- Escala base 8 px: 8 / 16 / 24 / 32 / 48 / 64. Margen lateral de pantalla 48; separación entre botones 32.
- Táctil mínimo 144 px de alto (48 dp).
- **Botón estándar**: alto 160, ancho de contenedor, sprite `btn_pill` Sliced (borde 64, `SlicedPpuMultiplier` 0,8 -> radio 80), texto 52 centrado. Estados (ColorTint): normal = Primario; pulsado = Primario pulsado; desactivado = Desactivado. **Botón secundario**: igual con Secundario y texto Texto.

## Iconografía
Iconos blancos teñidos, 48 dp visuales (64-72 px) dentro de un área pulsable >= 144 px (contenedor con Image raycast), nunca el sprite desnudo. Círculos pequeños: Simple/Preserve Aspect, no se estiran.

## Safe area y orientación
Solo vertical (Portrait). `SafeAreaFitter` en el root de cada pantalla (hijo de `ScreenLayer`, anclas estiradas); el fondo puede ocupar todo el Canvas, el contenido va dentro del safe area.
