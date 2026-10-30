# Retro S13 - Deslizar la tarjeta para responder verde/rojo

Estado: hecho. Sin ronda de revisión formal (sprint de una sesión, sin subagentes). Compilación limpia a la primera, sin correcciones. EditMode 243/243, PlayMode 28/28 (23 + 5 nuevos de `CardSwipeTests`).
APK: `Builds/S13/KanjiLearning-S13-dev.apk`, 84.073.593 B (~80,2 MiB; S12: 68.794.451 B, +15,3 MB, ver medida 1).
Pendiente (usuario): probar en el móvil (ver «Prueba del usuario» en `plan.md`).

## Hecho
- T53: `CardSwipe` (en el `Panel` del popup) + `CardSwipeRelay` (se añade solo al `ScrollRect` del cuerpo). Unity manda los eventos de arrastre a todos los handlers del objeto, así que la tarjeta ve también los arrastres que empiezan sobre el cuerpo: si el movimiento es más horizontal que vertical lo toma y congela el scroll durante ese arrastre; si no, el scroll sigue igual. Umbral 30 % del ancho, giro máx. 8°, tinte máx. 35 % hacia `Success`/`Danger` del tema, salida y vuelta en 0,15 s (tiempo sin escalar).
- T54: `KanjiPopup` conecta `SwipedGreen`/`SwipedRed` a los mismos métodos que los botones (`AnswerKnew`/`AnswerDidntKnow`). Gesto activo solo con el popup abierto y `AnswerRow` visible (no antes de «Resolver», no en modo complicados). `Present` y `OnDisable` devuelven la tarjeta al centro y cancelan una animación pendiente (evita que un toque de botón durante la salida cuente dos veces).
- T55: `CardSwipeTests` (PlayMode): arrastre corto vuelve sin responder; largo a la derecha = verde; a la izquierda = rojo; en estudio sin efecto antes de Resolver y después responde sin cerrar; sobre el cuerpo, arrastre vertical no mueve la tarjeta y el horizontal sí responde.
- T56: escena regenerada (`Kanji/Build S01 Scene`), tests, APK y esta retro.

## Tokens (`node tools/token-report.mjs --since 2026-10-28T11:16:00Z`)
Total local: **1.497.526** (S12: 960.300). Sesión en el PC con Sonnet, 18 turnos (incluye abrir el Editor, que no estaba abierto). La sesión coordinadora (que escribió el código) no deja transcripción local y no entra en el informe.

| Agente | Turnos | Total |
|---|---|---|
| orquestador | 18 | 1.497.526 |

## Qué funcionó
- Mismo reparto que S12: código fuera, el PC solo compila, regenera, prueba y construye. Tests nuevos en verde a la primera.

## Qué cambiar (medidas)
1. El APK crece 15 MB sin assets nuevos; la regeneración tocó los SDF de las fuentes (atlas dinámicos). Revisar si los atlas guardan glifos de más y limpiarlos/volverlos a dinámicos vacíos antes del build.
2. Dejar el Editor abierto antes de lanzar la sesión del PC: abrirlo cuesta turnos.
3. Pendiente de S12 sin hacer: excluir `Assets/Art/KanjiBook.png` original del build si no se usa.
