# S08 - Nombre de la historia: popup de carga y Editar (T36a, T37a)

Canvas 1080x1920, tema oscuro. Tokens de `guia-estilo.md`; base de popup y scrim en `S05b-tema-oscuro-popup.md`; toast en `S04-biblioteca.md`. Lógica y firmas en `docs/sprints/S08/arquitectura.md`. Un solo componente `NameInputPopup : PopupView` para carga y edición; sustituye al diálogo de conflicto (Reemplazar / Conservar ambas, retirado).

## A. Estructura de `NameInputPopup` (prefab bajo `PopupLayer`)

```
NameInputPopup        stretch, Scrim #000 65 %, Image raycast (bloquea toques)
└─ Panel              Superficie + borde, ancho 888, centrado, alto por contenido (VLG padding 48, spacing 32, ContentSizeFitter)
   ├─ Title           TMP 56 Bold, Texto, 1-2 líneas
   ├─ NameField       TMP_InputField, alto 144, Superficie elevada, borde 3 px Borde (Error si hay error)
   │  ├─ Placeholder  TMP 44 Regular, Texto secundario
   │  └─ Text         TMP 44 Regular, Texto, 1 línea
   ├─ ErrorText       TMP 36 Regular, color Error (#FF8A80), wrap, máx. 3 líneas; inactivo si no hay error
   ├─ Stats           VLG spacing 8 (solo carga; inactivo en Editar)
   │  ├─ StatKanjis      TMP 44, "Kanjis: N"        (siempre)
   │  ├─ StatCharacters  TMP 44, "Caracteres: N"    (siempre)
   │  └─ StatWarnings    TMP 44, "Avisos: N"        (activo solo si N > 0; fondo WarningSurface, padding 16, icono aviso 48 + texto)
   └─ Buttons         HLG spacing 32, alto 160, ambos flexible 1
      ├─ BtnCancel    Secundario, TMP 52 Medium "Cancelar"
      └─ BtnAccept    Primario, TMP 52 Medium "Aceptar"
```

Referencias para el programador: `_title`, `_nameField`, `_placeholder`, `_errorText`, `_statsRoot`, `_statKanjis`, `_statCharacters`, `_statWarnings`, `_btnCancel`, `_btnAccept`. Todos los alturas táctiles >= 144 px (48 dp). El panel nunca supera el safe area: si el teclado deja poco espacio, el `Panel` se sube (ver D).

## B. Popup de carga (T36a)

Se abre tras decodificar y parsear sin error fatal. Se vuelve a abrir en cada carga.

| Elemento | Contenido |
|---|---|
| Title | `Nombre de la historia` |
| Placeholder | nombre de archivo sin extensión (`StoryName.FromFileName`), p. ej. `kanji_n5_lectura` (se ve en Texto secundario cuando el campo está vacío) |
| Campo | vacío al abrir; `characterLimit` 60; una línea; sin foco automático (no abre el teclado hasta tocar el campo) |
| Stats | `Kanjis: N`, `Caracteres: N` siempre; `Avisos: N` solo si N > 0 |
| Botones | `Cancelar` / `Aceptar` |

Reglas (validación en cada `onValueChanged` con `StoryLibrary.Check(input, fallback, null)`):

| Estado | Campo | ErrorText | Aceptar |
|---|---|---|---|
| Campo vacío, nombre por defecto libre | Placeholder visible | oculto | activo (usa el nombre por defecto) |
| Texto libre | borde Borde | oculto | activo |
| Duplicado (incluye el nombre por defecto si ya existe) | borde Error | `Ya existe una historia llamada 'X'. Elige otro nombre.` (X = `ConflictingName`, tal como está guardado) | desactivado (`interactable=false`, fondo `#272D38`, texto `#6B7180`) |
| `Empty` (sin texto ni nombre por defecto) | borde Borde | oculto | desactivado |

- El mensaje rojo aparece/desaparece en el mismo frame que cambia el texto, sin animación; el panel se reajusta por el VLG. Se añade icono de aviso 32 junto al texto para no depender solo del color.
- Aceptar: valida de nuevo en `Import(story, name)`; cierra el popup (fundido 0,15 s) y muestra el toast existente `Historia 'X' cargada: N kanjis` (2 s; con avisos se mantiene el comportamiento actual). Si la red de seguridad devuelve Duplicate: el popup sigue abierto y muestra el mensaje rojo.
- Cancelar: cierra sin importar, sin toast. Atrás del sistema / tecla Escape = Cancelar (con teclado abierto, el primer Atrás solo lo cierra).
- **No se cierra al tocar fuera**: el scrim absorbe el toque y no hace nada (mismo mecanismo que `ChoicePopup`).
- Sin vibración ni sonido.

## C. Botón Editar en el popup de historia (T37a)

- Se añade `BtnEdit` al popup de historia de la lista, entre la información y `Cerrar`/acciones existentes (misma columna VLG, spacing 24, ancho 888). Estilo **Secundario**, alto 144, `icon_edit` 64 (Texto) + TMP 52 Medium `Editar nombre`.
- Al pulsarlo se abre `NameInputPopup` **encima** del popup de historia (el de historia queda debajo, oculto por el scrim; no se cierra).

Diferencias respecto a B:

| Elemento | Editar |
|---|---|
| Title | `Editar nombre` |
| Campo | precargado con el nombre actual; texto seleccionado al enfocar (caret al final); `characterLimit` 60 |
| Placeholder | `Nombre de la historia` (se ve si el usuario borra todo) |
| Stats | ocultas (`_statsRoot` inactivo) |
| Validación | `Check(input, null, ownId)`: el propio nombre (incluso cambiando solo mayúsculas) no es duplicado; vacío = `Empty` |
| Aceptar | activo si es válido; **desactivado si el texto es idéntico al actual** (sin cambios) o vacío o duplicado |

- Mensaje rojo y Cancelar / Atrás / no cerrar al tocar fuera: idénticos a B.
- Aceptar: `library.Rename(id, name)`, cierra el popup de edición, el popup de historia muestra el nuevo nombre en el mismo frame y la fila de la lista se refresca (orden y contenido intactos); toast `Nombre actualizado` (2 s). Fallo: el popup sigue abierto y toast `No se pudo guardar.` (3 s).
- Cancelar: vuelve al popup de historia sin cambios.

## D. Teclado móvil y accesibilidad

- `TMP_InputField`: `contentType` Standard, `lineType` SingleLine, `inputType` Standard, `keyboardType` Default, `returnKeyType` Done (`TouchScreenKeyboard`), `shouldHideMobileInput=false` en Android para ver el campo nativo si hace falta, autocorrección desactivada. Done del teclado = pulsar Aceptar si está activo; si no, solo cierra el teclado.
- Teclado abierto: el `Panel` se desplaza verticalmente (`anchoredPosition.y`) lo justo para que `NameField`, `ErrorText` y `Buttons` queden sobre el teclado (`TouchScreenKeyboard.area`); al cerrarlo vuelve a la posición central. En tablet el panel no se mueve si ya cabe.
- Fuente del campo Noto Sans JP con fallback CJK (el nombre puede llevar kanji/kana).
- Contraste: ErrorText Error sobre Superficie 7,0:1; borde de campo en error como refuerzo, no único indicador.
- Safe area: el scrim cubre todo el Canvas; el `Panel` respeta el safe area con margen lateral >= 48.

## E. Textos nuevos en `UiStrings`

`NameTitle` = `Nombre de la historia`; `EditNameTitle` = `Editar nombre`; `EditName` = `Editar nombre` (botón); `NameDuplicate` = `Ya existe una historia llamada '{0}'. Elige otro nombre.`; `StatKanjis` = `Kanjis: {0}`; `StatCharacters` = `Caracteres: {0}`; `StatWarnings` = `Avisos: {0}`; `Accept` = `Aceptar`; `Cancel` = `Cancelar` (se conserva, se reutiliza); `NameUpdated` = `Nombre actualizado`.
