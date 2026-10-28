# S06 - Kanjis complicados (T26a, T27a)

Canvas 1080x1920, tema oscuro. Tokens de `guia-estilo.md`; popup en `S05b-tema-oscuro-popup.md`; vista 2 en `S05-lector.md` (C); toast en `S04-biblioteca.md` (sec. 0); plantilla de pantalla en `pantallas-vacias.md`. Textos en `UiStrings`. No hay componentes nuevos salvo `DifficultRow` (variante de `StoryRow`).

## A. Botón en la vista 2 del popup del kanji (T26a)

- Va en el hueco `SecondaryActions` de `KanjiPopup` (VLG spacing 24, entre `Body` y la fila inferior). Se activa **solo en la vista 2** (al pulsar Mostrar más). En la vista 1 sigue inactivo. Reabrir empieza en vista 1.
- Hijo `BtnAddDifficult`: `btn_pill` Sliced, estilo **Secundario** (Superficie elevada, texto Texto), alto 144 (>= 48 dp), ancho completo del panel (888). Interior HLG centrado, spacing 16: `Icon` 64x64 + `Label` TMP 52 Medium.
- Estados:

| Estado | Icono | Texto | Interactivo |
|---|---|---|---|
| Disponible | `icon_plus` (Texto) | `Añadir a complicados` | si |
| Pulsado | - | - | ColorBlock pressed `#3A4150` |
| Ya añadido (al abrir la vista 2, o tras añadir) | `icon_check` (Éxito claro `#7BD88F`, no solo color: el texto cambia) | `Ya en complicados` | no (color Desactivado `#272D38`, texto `#6B7180`, exento de AA; `Button.interactable=false`) |

- Al abrir la vista 2 se consulta `Contains(KanjiKey)` para fijar el estado inicial.
- Pulsar: añade y persiste, el botón pasa en el mismo frame a "Ya en complicados", **el popup no se cierra**, y toast `Añadido a complicados`. Sin vibración ni sonido.
- Si ya estaba (carrera/duplicado): sin duplicar, estado "Ya en complicados", sin toast. Si falla el guardado: el botón queda "Añadir a complicados" y toast `No se pudo guardar.`
- Toast (`ToastView`, ya definido): abajo, 3 s (fundido in 0,15 s / out 0,3 s), no recibe toques; sustituye al anterior. Se muestra sobre el popup: `Toast` debe renderizar **por encima** del `PopupLayer` (último hermano) para verse sobre el scrim. Si se prefiere, 2 s para mensajes de confirmación breves: aquí se fija **2 s** para `Añadido...` y `Eliminado...`, 3 s para errores.

## B. Pantalla "Kanjis complicados" (T27a) - prefab `DifficultList` (variante de `ScreenTemplate`, script `DifficultListScreen`)

Acceso: `BtnDifficult` de Inicio (`Push`). Title `Kanjis complicados`. Estructura análoga a `StoryList`:

```
DifficultList         stretch, SafeAreaFitter, Fondo
├─ TopBar             (plantilla) BtnBack 144x144 -> Pop a Inicio; Title
└─ Content            stretch, Top 144, margenes L/R/B 48
   ├─ ListScroll      ScrollRect vertical, sin barra; Viewport/Content: VLG spacing 16, padding bottom 48
   │  └─ DifficultRow (prefab)
   └─ EmptyScreen     stretch, TMP 44 Regular, Texto secundario, centrado, margen 24 (mismo objeto que EmptyMessage)
```

- `DifficultRow`: igual a `StoryRow` (Button, Superficie, pulsado Superficie elevada, alto 168, ancho 984, padding 32/24). Hijos HLG: `Kanji` TMP 72 Regular color Link (#22D3EE), ancho preferido 120, centrado, 1 linea; `Reading` TMP 44 Regular Texto, flexible 1, 1 linea ellipsis (si no hay lectura, usa el significado); `Meaning` omitido en la fila. Toque en la fila -> popup modo Difficult (C).
- Orden: el último añadido arriba. Se recarga en `OnShow` y tras eliminar, conservando el offset en px acotado al nuevo máximo.
- Estados:

| Estado | ListScroll | EmptyScreen |
|---|---|---|
| Con kanjis | visible | oculto |
| Vacío | oculto | `Aún no tienes kanjis complicados.` + salto de línea + `Añádelos desde el lector con Mostrar más.` |
| Error de carga | oculto | `No se pudo cargar la lista.` (color Error) |

- Atrás (BtnBack, tecla, gesto) sin popup -> Inicio. Con popup abierto: cierra solo el popup.

## C. Popup en modo Difficult (T27a)

Mismo prefab `KanjiPopup` con `Mode = Difficult` (sin recibir `Contains`, siempre muestra la copia guardada).

- Muestra de entrada **todo el contenido** (kanji, frase, lectura, significado, traducción) como la vista 2; sin `BtnShowMore`, sin `AnswerRow` ni `BtnAddDifficult`.
- Botones fuera del scroll, orden: `BtnRemoveDifficult` arriba, `Cerrar` abajo (igual separación 32 que el popup actual).
- `BtnRemoveDifficult`: `btn_pill`, estilo **Peligro** (`#C4302B`, pulsado `#A32520`), alto 160, ancho 888, `icon_cross` 64 (`TextOnAccent`) + TMP 52 Medium `Eliminar de la lista`. `Cerrar`: Secundario, 144.
- Pulsar Eliminar (sin confirmación, decisión P4): quita y persiste, **cierra el popup** (cierre instantáneo del frame; fundido 0,15 s del panel), la fila desaparece de la lista en el acto (sin animación), se mantiene el scroll y se muestra toast `Eliminado de complicados` (2 s). Si se vació la lista, aparece `EmptyScreen`. Si falla: popup abierto, toast `No se pudo eliminar.` (3 s).
- Tras pulsar, el botón queda desactivado hasta terminar (evita doble toque). Cerrar / fuera / Atrás: cierra sin cambios.
- Referencias para el programador: `_btnAddDifficult`, `_btnRemoveDifficult`, `_secondaryActions`, `_mode` (Reader / Difficult).
