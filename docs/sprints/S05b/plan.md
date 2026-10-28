Inicio: 2026-10-23T08:20:00Z

# Sprint S05b — Dark theme, popup de vista única y transiciones

## Objetivo
Pasar la app a tema oscuro centralizado (`UiTheme`, kanji en cian), simplificar el popup de kanji (vista única, botones verde/rojo) y añadir fundidos y popups apilados. Prepara S06 y S07 sin tocar su lógica.

Entradas: `propuesta-diseno.md`, `valoracion-ux.md`, `valoracion-arquitectura.md` (en esta carpeta).

## Tareas
| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T1 | `UiTheme` + importación del subconjunto Dark UI | - `UiTheme.cs` (asmdef `Kanji.UI`, `Scripts/UI/Theming/`) y asset `Assets/_Project/Settings/UiTheme_Dark.asset` con la paleta de `valoracion-ux.md` (Link `#22D3EE`, Éxito, Peligro, etc.), sprites, fuentes y tamaños.<br>- `OnValidate` avisa de sprites/fuentes vacíos; `LinkHex` devuelve `#RRGGBB`.<br>- ADR `docs/arquitectura/adr/0005-tema-visual-uitheme.md` escrito.<br>- Subconjunto (~15 ficheros) en `Assets/_Project/Art/DarkUI/{Buttons,Inputs,Panels,Icons}/` con nombres en inglés + licencia; sin demo, prefabs ni escenas del paquete.<br>- `Editor/DarkUiAssetSetup.cs` fija import settings (Sprite, Full Rect, 9-slice borders, sin mipmaps) y crea el atlas `DarkUI.spriteatlasv2` (sin rotación ni tight packing).<br>- Sprites enlazados en el asset de tema; compila sin errores. | arquitecto, ux-ui, programador | hecha |
| T2 | Aplicar el tema a SceneBuilder y pantallas | - `SceneBuilder.cs` sin `Hex(...)` sueltos, sin `UISprite.psd` ni literales de color; todo sale de `UiTheme`; escena/prefabs regenerados.<br>- `ReaderScreen`, `LoadStoriesScreen`, `StoryListScreen` usan `[SerializeField] UiTheme _theme` (cableado por SceneBuilder); desaparecen `_linkColor/_hintColor/_errorColor/_emptyColor`.<br>- Ninguna pantalla ni popup con fondo claro; texto japonés sin "tofu".<br>- Enlaces de kanji en cian vía `_theme.LinkHex`; `Core` sin cambios.<br>- Test PlayMode: `ReaderScreen` genera `<color=#22D3EE>` en los enlaces.<br>- Contraste Link y TextSecondary >= 4,5:1 sobre Surface (confirmado por ux-ui). | arquitecto, ux-ui, programador | hecha |
| T3 | Popup de kanji de vista única (verde/rojo) | - Al pulsar un kanji se ven a la vez kanji, frase, lectura, significado y traducción; campos vacíos ocultos.<br>- No hay "Mostrar más" ni "Cerrar"; hay botón verde (Éxito) y rojo (Peligro), >= 48 dp, distinguibles también por texto/icono.<br>- Verde y rojo cierran el popup; no se registra nada; el scroll de la lectura se conserva.<br>- Tocar fuera y Atrás cierran sin registrar nada.<br>- Colores y sprites de `UiTheme`. | arquitecto, ux-ui, programador | hecha |
| T4 | Fundidos y popups apilados | - Cambio de pantalla: fundido a negro 0,2 s + 0,2 s; popups: fundido de opacidad 0,15 s al abrir y cerrar.<br>- Durante un fundido se ignoran toques y Atrás (doble toque rápido no abre dos pantallas ni popups).<br>- Con un popup sobre otro (historia -> Eliminar) el de debajo se oculta y no es interactuable; al cerrar el de arriba reaparece intacto.<br>- Salir de la app sin fundido.<br>- Duraciones configurables (no literales dispersos); test de la lógica de apilado/bloqueo si es testeable. | arquitecto, ux-ui, programador | hecha |
| T5 | Revisión, regresión y prueba en móvil | - Revisor ejecuta tests y confirma que los flujos de S01-S05 (cargar, listar, eliminar con diálogos, leer) siguen funcionando con tema, fundidos y popup nuevo.<br>- Máx. 2 rondas de revisión; bloqueantes persistentes van a `bloqueos.md`.<br>- APK generado y probado por el usuario en el móvil (legibilidad, fundidos, popup).<br>- Retro con informe de tokens. | revisor, programador, planificador | hecha |

Orden: T1 -> T2 -> T3 -> T4 -> T5 (T3 y T4 comparten SceneBuilder/popup: no en paralelo).

## Fuera de alcance (va al backlog)
- Registro de estadísticas, "Añadir a complicados" (S06), pantalla de estadística (S07).
- Nuevos tonos de tema más allá del importado; tema claro conmutable; componentes `Themed*` en runtime.
- Interlineado del Lector (decidir tras prueba en dispositivo).
- Pendientes de S05: historia real que no carga entera, medidas en móvil, `hostile-markup.txt`.

## Riesgos
- Importación: el paquete solo está en la caché de Asset Store (`%APPDATA%\Unity\Asset Store-5.x\Giniel Villacote\Textures MaterialsGUI Skins\Dark Theme UI.unitypackage`), no en el proyecto. El programador lo importa con el Editor, deja solo el subconjunto y borra el resto; si falla, parar y preguntar al usuario.
- T2 toca 4 scripts (+1 test), por encima de la regla práctica de 3: los cambios en las pantallas son sustituciones mecánicas de campos. Si el programador no cabe en una pasada, mover `LoadStoriesScreen`/`StoryListScreen` a T3 y avisar.
- 9-slice: bordes y `Image Type = Sliced` a 984 px de ancho sin deformar; círculos en Simple/Preserve Aspect; área táctil >= 144 px (no sprite desnudo).
- Lectura larga en tema oscuro cansa más: texto `#E8EAF0` (no blanco puro); validar en el móvil.
- Contraste del kanji cian contra la mezcla real con el scrim (verificar).
- Suposiciones del diseñador pendientes de confirmar por el usuario:
  1. Verde = "lo sabía", rojo = "no lo sabía" (significado en S07).
  2. Tocar fuera / Atrás cierran el popup sin registrar y cuentan como "no pulsado" (= acertado en S07).
  3. Kanji nunca pulsado o cerrado sin botón cuenta como acertado.
  4. "Añadir a complicados" (S06) será un botón secundario aparte que no cierra el popup.
  5. Duraciones: 0,2 s + 0,2 s entre pantallas, 0,15 s en popups.
- Límites: máx. 2 consultas programador -> arquitecto por tarea; máx. 2 idas y vueltas arquitecto <-> diseñador en el sprint.
