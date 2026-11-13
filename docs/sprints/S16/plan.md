Inicio: 2026-11-05T12:47:00Z

# Sprint S16 — Aprendizaje inverso

Estado: hecho (pendiente de prueba en el móvil)

## Objetivo
Nueva opción **Aprendizaje inverso** en el menú ⋮ de una lista. Funciona igual que Estudiar (S15): orden aleatorio, Resolver, verde/rojo con botones o deslizando, «Añadir a complicadas», resultados con %. La diferencia es lo que se enseña:
- Delante: solo la lectura de la palabra (en grande).
- Al resolver: el kanji (palabra), la lectura y el significado. No se muestran la frase ni sus campos.

Cada palabra tiene un segundo círculo de progreso 0..5 para este modo (verde suma 1, rojo no cambia), independiente del de Estudiar.

## Decisiones
- La lectura sigue visible tras resolver (es la pregunta); el usuario pidió ver «el significado y el kanji».
- Palabra sin lectura: delante se muestra la palabra.
- Colores de los círculos en la fila: Estudiar = cian (tema), Inverso = violeta.
- JSON: campo `reverseProgress` (listas anteriores = 0).

## Tareas
| Id | Tarea | Criterios de aceptación | Estado |
|---|---|---|---|
| T66 | Core: `ReverseProgress`, `RecordStudy(..., reverse)`, sesión inversa | Tests EditMode: solo cambia el progreso del modo; pegar con Actualizar y marcar complicada lo conservan; JSON ida y vuelta. | hecho |
| T67 | Tarjeta inversa, opción en el menú, segundo círculo | Delante solo lectura; al resolver kanji + lectura + significado; menú con «Aprendizaje inverso»; dos círculos por fila. | hecho |
| T68 | Escena, tests, APK y retro | EditMode y PlayMode en verde; `Builds/S16/KanjiLearning-S16-dev.apk`. | hecho |

Prueba del usuario: aprendizaje inverso de una lista, comprobar qué se ve antes y después de Resolver, y que se llena el círculo violeta sin tocar el cian.
