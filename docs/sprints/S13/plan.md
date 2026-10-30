Inicio: 2026-10-28T11:16:00Z

# Sprint S13 — Deslizar la tarjeta para responder verde/rojo

Estado: hecho

## Objetivo
Cuando el popup del kanji enseña los botones verde y rojo (al leer una historia y al estudiar complicados, S11), se puede arrastrar la tarjeta en horizontal con el dedo. Si se suelta pasado un umbral, cuenta igual que pulsar el botón de ese lado; si no, la tarjeta vuelve a su sitio. La dirección encaja con la posición de los botones.

## Decisiones
- Botones actuales (`SceneBuilder`, fila `AnswerRow`): rojo «No lo sabía» a la izquierda, verde «Lo sabía» a la derecha.
- P10 (respondida por el usuario): se siguen los botones: derecha = verde, izquierda = rojo; el diseño no cambia.
- El gesto solo está activo mientras la fila verde/rojo es visible (no antes de «Resolver» en estudio).
- Soltar el gesto llama exactamente al mismo código que el botón: mismas estadísticas, progreso (P9) y paso al siguiente kanji.
- Solo horizontal: un arrastre mayormente vertical sigue haciendo scroll del cuerpo del popup.
- Umbral ≈ 30 % del ancho de la tarjeta; mientras se arrastra, la tarjeta gira un poco y se tiñe del color del lado hacia el que va. Al superar el umbral sale por ese lado; si no, vuelve con una animación corta.

## Reglas de ahorro
- Sin subagentes; documentos de una página. Leer solo `KanjiPopup.cs`, `SceneBuilder.cs` (popup) y el componente nuevo.

## Tareas
| Id | Tarea | Criterios de aceptación | Estado |
|---|---|---|---|
| T53 | Componente `CardSwipe` (UI) | - Drag horizontal sobre el panel del popup (IBeginDrag/IDrag/IEndDrag); arrastres verticales pasan al ScrollRect.<br>- Umbral, giro, tinte, salida y vuelta animadas.<br>- Eventos `SwipedGreen` (derecha) / `SwipedRed` (izquierda). | hecho |
| T54 | Conectar con `KanjiPopup` en lectura y estudio | - Activo solo con la fila verde/rojo visible.<br>- Cada evento invoca el mismo handler que su botón. | hecho |
| T55 | Tests | - PlayMode: arrastre corto vuelve sin responder; arrastre largo a cada lado = verde/rojo correcto; sin efecto antes de Resolver. | hecho |
| T56 | Revisión, APK dev, retro | - EditMode y PlayMode en verde; `Builds/S13/KanjiLearning-S13-dev.apk`. | hecho |

Prueba del usuario: en lectura y en estudio, arrastrar poco (vuelve), arrastrar a cada lado (cuenta como el botón de ese lado), scroll vertical sigue funcionando.
