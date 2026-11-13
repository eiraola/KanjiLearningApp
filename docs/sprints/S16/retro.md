# Retro S16 - Aprendizaje inverso

Estado: hecho. Pendiente: prueba del usuario en el móvil.
EditMode 276/276 (+2), PlayMode 28/28. Compilación limpia en las dos rondas.
APK: `Builds/S16/KanjiLearning-S16-dev.apk`, 52.073.762 B, build limpia.
Detalle: `docs/sprints/S16/revision-1.md` (rondas 1 y 2) y capturas `play-*.png` (en el PC).

## Hecho
- T66 Core: `ReverseProgress` por palabra (JSON `reverseProgress`, listas antiguas = 0); `RecordStudy(..., reverse)`; `WordStudySession(reverse)`. Pegar con Actualizar y marcar complicada lo conservan.
- T67 «Aprendizaje inverso» en el menú ⋮. Delante solo la lectura en grande; al resolver, palabra + Lectura + Significado (sin frase ni traducción). Verde/rojo, deslizar, complicadas y resultados como en Estudiar. Segundo círculo violeta en cada fila; el cian (Estudiar) no cambia.
- Ronda 2: lecturas de más de ~9 kana se cortaban delante. Ahora el texto grande encoge (mín. 56) y pasa a 2 líneas: 22 kana se leen enteras. Estudiar normal no cambia.

## Sin probar (móvil)
- Deslizar con el dedo en la tarjeta inversa; una lista real con lecturas largas; palabra sin lectura; Atrás físico.

## Pendiente de decidir (backlog)
- Fila con dos círculos: palabras muy largas se cortan con «…» y con palabra y lectura muy largas los círculos se encogen.
- Arcos finos de progreso y el texto «Kanjis fallados» en resultados (desde S15).
- Opción restante del menú ⋮: sin definir.
