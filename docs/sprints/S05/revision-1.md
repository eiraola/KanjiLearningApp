# S05 — Revisión 1 (combinada con S04)

Revisor. Alcance: código listado en `docs/sprints/S05/implementacion.md` (T20 `StoryRichTextBuilder`, T21 `ReaderScreen`, T22 `LinkClickHandler`/`KanjiPopup`, T23 vista 2) y dependencias directas, contra `plan.md` y `arquitectura.md` de S05. Los hallazgos de S04 están en `docs/sprints/S04/revision-1.md`.

## Tests
- EditMode: **164/164** superadas (136 previos + 28 de T20).
- PlayMode: **1/1** superada (`RichTextTmpEscapeTests`: escape contra TMP real, con `<b>`, `</noparse>`, `A`, `\U0001F600`, `<size>`, `<br>` y `<sprite>`).
- Editor conectado, sin errores de compilación.

## Revisión del código (resumen)
- `StoryRichTextBuilder`: he repasado a mano el troceo (párrafo normal / párrafo de más del máximo con retroceso a `lastSoft`), la normalización de `\r\n`/`\r` (también entre segmentos), `BreakAfter`, los pares suplentes, que no se partan enlaces y el escape de `<` y `\u`/`\U` entre trozos y enlaces. No encuentro pérdidas, duplicados ni errores de uno. Cumple el contrato.
- `ReaderScreen`: la corrutina se para en `OnHide`. El `Pop` del caso de error va un frame después (sin reentrada). Los trozos se reutilizan como pool y se limpian al ocultar. `_story` se suelta al ocultar. Cumple T21.
- `LinkClickHandler` y `KanjiPopup`: doble guarda frente al arrastre. La vista 1 se reinicia en cada `Show` y los campos vacíos se ocultan. `richText = false` en todo el contenido del usuario. Cumple T22/T23.
- `KanjiPopup` siempre está solo en la pila, así que el bug de orden de `PopupService` (S04, importante) no le afecta hoy. Le afectará en cuanto S06 apile otro popup encima.

## Hallazgos

| Severidad | Fichero:línea | Problema | Escenario que lo provoca | Corrección sugerida |
|---|---|---|---|---|
| menor (a verificar) | `Assets/_Project/Scripts/App/AppBootstrap.cs:245-246` | `EventSystem.current` se asigna en `EventSystem.OnEnable`. Desde `AppBootstrap.Awake` puede ser `null` según el orden de carga de la escena, y entonces el umbral de arrastre de ≈8 dp no se aplica, sin avisar. | Si al cargar la escena el `EventSystem` se habilita después de `AppBootstrap`, se queda el umbral por defecto de 10 px y en pantallas de ~3x los toques sobre kanjis se convierten en arrastres. | Mover la asignación a `Start` o buscar el `EventSystem` con `FindAnyObjectByType`, y registrar el valor final en un log `[Kanji]` para comprobarlo en el móvil. |
| menor | `Assets/_Project/Scripts/UI/ReaderScreen.cs:162-169` | `kanji-popup openMs` solo mide el `Show` síncrono. No incluye el frame de render ni el fundido de 0,15 s, así que no mide exactamente el "abre en < 1 s" del criterio. | Medidas de T24: `openMs` saldrá de pocos ms aunque el popup tarde más en verse. | Aceptable como indicador. Si se quiere precisión, medir hasta el final del fundido o hasta el siguiente frame. |
| menor | `Assets/_Project/Scripts/UI/ReaderScreen.cs:64,93` | Si el lector vuelve a mostrarse por un `Pop` (args `null`), entra en el camino de error y hace otro `Pop`. Hoy no se puede dar porque nada se apila encima del lector. | Futuro: una pantalla apilada sobre el lector. Al volver: toast "No se pudo abrir la historia." y salto a la lista. | Al recibir args `null` con historia ya cargada, conservarla. Anotarlo para S06/S07. |
| menor | `Assets/_Project/Scripts/UI/ReaderScreen.cs:216` | `reader-scroll` cuenta frames mientras `velocity != 0`, también durante el rebote elástico y la inercia residual. Puede subir `frames` sin que el usuario esté desplazando. | Medida T24 de scroll. | Usar un umbral (`velocity.sqrMagnitude > 1`). Solo afecta a la medida. |

Sin bloqueantes ni importantes en el código de S05.

## T24 — pendiente (necesita al usuario)
T24 no se ha ejecutado. S05 no se cierra hasta la prueba en el móvil (supuesto S4). Antes de la prueba hay que aplicar los importantes de `S04/revision-1.md` (orden de popups, contador de `.tmp` y *fallback* de `Replace`) y regenerar el APK dev con *Development Build*.

### Qué debe comprobarse en el móvil
1. **Preparación:** APK dev regenerado. `adb push` de `Assets/_Project/Tests/Fixtures/Text/perf-50k.txt`, `hostile-markup.txt` y la historia real larga [R-S05a] a `/sdcard/Download/`. Anotar el modelo del móvil [R-S05c].
2. **Apertura** (`[Kanji][Perf] reader-open`): abrir dos veces `perf-50k` y dos veces la historia real. Criterio `totalMs` < 2000. Anotar `loadMs` (si > 200 → `Task.Run`), `buildMs`, `firstVisibleMs`, `atlases` (crecimiento en la primera apertura) y `monoMB/reservedMB`.
3. **Scroll** (`[Kanji][Perf] reader-scroll`): de arriba abajo en la historia real. `over33ms` ≈ 0 y anotar `maxMs`.
4. **Pico de memoria:** `adb shell dumpsys meminfo com.battlecroissant.kanjilearning` antes y después de abrir la historia larga.
5. **Toques:** tocar kanjis azules (también repetidos: cada aparición abre su bloque), tocar texto no azul (no pasa nada), arrastrar empezando sobre un kanji (no abre) y tocar durante la inercia del scroll. Anotar fallos de puntería. Comprobar en el log el umbral de arrastre aplicado (hallazgo 1).
6. **Popup:** `kanji-popup openMs` < 1000. Vista 1 sin lectura ni traducción. "Mostrar más" añade las filas y oculta los campos vacíos. Texto largo desplazable dentro del popup. Cerrar, tocar fuera y Atrás cierran solo el popup y el scroll del lector no se mueve. Reabrir empieza en vista 1.
7. **Marcas hostiles:** `hostile-markup.txt` se ve literal (sin negrita, colores, tamaños ni saltos falsos; `\n`, `\t` y `A` se ven tal cual), sin tofu, y solo 猫/犬 en azul.
8. **Navegación:** el lector arranca arriba. Atrás vuelve a la lista en su misma posición. Una historia sin kanjis abre sin error. Una historia borrada o dañada muestra "No se pudo abrir la historia." y vuelve a la lista.
9. **Pendientes de S04 (T17, T18, T19b):** ver la lista de `docs/sprints/S04/revision-1.md`.
10. **Retro:** pasar a `retro.md` las 3 medidas (`totalMs` de apertura de fixture e historia real, `over33ms/maxMs` de scroll y pico de memoria).

VEREDICTO: APROBADO
