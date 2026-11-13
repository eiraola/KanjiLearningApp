# Revisión 1 - S16 Aprendizaje inverso (2026-11-05)

Resultado: **funciona lo pedido, con un problema por decidir.** Compila, tests en verde y APK generada. El problema: **una lectura de más de unas 9 kana no cabe delante de la tarjeta y se corta por los dos lados**. Falta la prueba en el móvil.
Esta revisión cubre compilación, escena, tests, prueba en Play y build. No es una revisión de código.

## Compilación
- Limpia a la primera: `compilationFailed=false`, sin errores.

## Correcciones hechas
- Ninguna. No se ha tocado código.

## Escena
- `Kanji/Build S01 Scene` ejecutado y guardado: la escena queda sin cambios pendientes (`dirty=False`).
- Ningún error de `FindProperty` ni `NullReference` entre los errores de la consola.
- Comprobado por script: `WordRow.prefab` tiene asignados `_progressFill` y `_reverseFill`. En Play, el menú muestra `BtnStudyReverse`.

## Tests
| Modo | Resultado | Antes |
|---|---|---|
| EditMode | **276/276** | 274 |
| PlayMode | **28/28** (`--async_tests true` + `test status`) | 28 |

La tarjeta inversa y el segundo círculo no tienen tests PlayMode propios; los cubre la prueba de abajo.

## Consola
2 errores nuevos en esta sesión, ninguno del código del proyecto:
1. `No command named 'editor play'`: comando mal escrito por mí en la CLI.
2. `ArgumentException: Subtarget property is not available for the platform (Android)`, durante el build. Es el mismo de `com.unity.pipeline` de S14 y S15. No impide el build.

Avisos: he mirado los 60 últimos, no todos. Son los que provocan a propósito los tests (`write FAILED`), de paquetes (`Failed to download any source lists`, manifiestos), de compilación en scripts anteriores a S16 (`ReaderScreen`, `LoadStoriesScreen`, `AppBootstrap`, `Atomic...`, sin abrir uno a uno) y del build.

## APK
- Ruta: `Builds/S16/KanjiLearning-S16-dev.apk`
- Tamaño: **52.073.990 B** (~49,7 MiB), 2.128 B más que S15.
- `BuildReport`: `Succeeded`, 13:02:03Z a 13:06:17Z (~4 min). Su único error es el de `Subtarget` de arriba; 25 avisos.
- Build limpia: el `launcher-debug.apk` intermedio se apartó antes. Las 440 entradas suman 51.992.477 B, así que el espacio muerto es de 81.513 B (~81 KB, igual que en S15). Ningún `.apk` anidado.
- `BuildReport.totalSize` dice 847.436.460 B: no es el tamaño de la APK. No he investigado qué cuenta.

## Prueba en Play
Editor a 1080x1920, lista «Prueba» (3 palabras, JSON sin `reverseProgress`). Cada paso se lanzó en una llamada aparte, con frames de por medio. Los botones se accionaron con `onClick.Invoke`, no con toques simulados.

| Paso | Resultado |
|---|---|
| Abrir la lista con JSON anterior a S16 | OK; círculo violeta a 0, progreso de Estudiar conservado |
| ⋮ | OK; Añadir palabras, Estudiar, Aprendizaje inverso, Cerrar |
| ⋮ → Aprendizaje inverso, delante | OK; solo la lectura en grande, sin frase, con «Añadir a complicadas» y Resolver |
| Resolver | OK; palabra en grande + filas Lectura y Significado, sin frase ni traducción, con rojo y verde |
| Verde, rojo, verde hasta el final | OK; resultados 3 / 2 / 1, **67 %**, fallada ～列目 |
| Aceptar | OK; violeta 0,2 / 0,2 / 0 (verdes +1, rojo sin cambio); cian sin cambiar (0,4 / 0,4 / 0,2) |
| JSON | OK; `reverseProgress` 1 / 0 / 1, `progress` igual que antes |
| Estudiar normal, 3 verdes | OK; frase delante (salvo 新語, que no tiene) y traducción al resolver; cian 0,6 / 0,6 / 0,4; violeta sin cambiar |
| Palabra ya complicada en inverso | OK; delante «Ya en complicadas» (la marca se conserva) |
| Atrás a media sesión con 1 respondida (`Cancel()`) | OK; resultados con total 1, 0 % |

Capturas:
- `play-inversa-pregunta.png`, `play-inversa-resuelta.png`: tarjeta antes y después de Resolver.
- `play-lista-dos-circulos-antes.png`, `play-lista-dos-circulos.png`: lista antes y después (violeta a la derecha).
- `play-menu.png`, `play-inversa-resultados.png`, `play-estudiar-normal.png`, `play-tras-estudiar-normal.png`, `play-inversa-atras.png`.
- `play-inversa-lectura-6kana.png`, `play-inversa-lectura-larga.png`, `play-lista-palabras-largas.png`: casos largos.

Una primera captura de la tarjeta resuelta salió entera en negro. Al repetirla salió bien y no volvió a pasar; creo que es de la captura, sin confirmar.

## Problemas encontrados (no se han cambiado)
1. **Lectura larga cortada delante de la tarjeta. Por decidir.** El texto grande no pasa de una línea y su tamaño mínimo es 96, así que en 888 px caben unas 9 kana. Con 6 kana se ve bien (tamaño 148). Con 22 kana se ve solo el centro, sin el principio ni el final (`play-inversa-lectura-larga.png`). Afecta a la pregunta de este modo, y las lecturas suelen ser más largas que las palabras. **Forma de probarlo:** escribí ese texto por script en la tarjeta abierta, no con una palabra real de la lista; esa escritura no recalcula el alto de la tarjeta, pero no cambia el ancho que causa el corte. Propuesta: permitir dos líneas o bajar el mínimo. Lo decide el usuario; el encargo era no rediseñar.
2. **Fila con dos círculos: la palabra se ve bien en casos normales.** Con el mismo método (texto escrito por script en las filas):
   - 4 y 6 kanji caben enteros.
   - 12 kanji se cortan con «…» tras unos 10.
   - La lectura de la fila se corta con «…» a partir de unas 7 kana (en la lista real, にじゅうまる, de 6, cabe entera).
   - Con palabra y lectura muy largas (12 kanji y 22 kana), **los círculos se encogen y dejan de ser redondos**: unos 71 x 80 px en la captura, medido a ojo. Es poco visible y solo en ese caso extremo.
3. **Los arcos siguen siendo finos.** El violeta a 1/5 cuesta de ver, igual que el cian (aviso 2 de S15).
4. **Texto de resultados.** Sigue diciendo «Kanjis fallados» (aviso 3 de S15).

## Sin probar
- **Palabra sin lectura** (delante debe verse la palabra): la lista no tiene ninguna; visto solo en código.
- **Deslizar en la tarjeta inversa**: no probado. El deslizamiento con puntero simulado no funcionó en S15. **Hay que probarlo con el dedo.**
- **Atrás con tecla real**: se llamó a `Cancel()`; la lectura de la tecla no está cubierta.
- Marcar o desmarcar complicada durante el inverso, tras salir de Play y volver a entrar (el JSON sí está guardado), lista con scroll, fallo de guardado y todo lo que dependa del dispositivo Android.

Datos de prueba: «Prueba» queda en `persistentDataPath/wordlists/` del PC con `progress` 3 / 2 / 3 y `reverseProgress` 1 / 0 / 1. No está en el APK.

## Limpieza
- `launcher-debug.apk` intermedio de S15 movido a la carpeta temporal de la sesión, no borrado. Gradle generó uno nuevo.
- Editor fuera de Play. Sin commit.

## Para el planificador
- En `plan.md`, T68 sigue «pendiente» y el sprint «en construcción».
- Sigue disponible Unity CLI 1.0.0-beta.16 (hay instalada la beta.14); no he actualizado porque requiere la aprobación del usuario.

## Ronda 2 - Lectura larga en dos líneas (2026-11-05)

Resultado: **problema 1 corregido, sin bloqueantes.** Compila, tests en verde y APK generada. Falta la prueba en el móvil.
El cambio lo indicó el coordinador.

### Cambio
Solo en `SceneBuilder.cs`, `BuildWordCardPopup`, label `Word` (`BuildKanjiPopup` no se toca):
- `fontSizeMin`: de `_theme.KanjiMinSize` (96) a 56.
- `textWrappingMode`: de `NoWrap` a `Normal`.
- `overflowMode`: `Ellipsis`.

### Comprobaciones
| Comprobación | Resultado |
|---|---|
| Compilación | Sin errores nuevos en consola |
| `Kanji/Build S01 Scene` y guardado | OK; en la escena guardada, `Word` tiene min=56, `Normal`, `Ellipsis`; `dirty=False` |
| EditMode | **276/276** |
| PlayMode | **28/28** (`--async_tests true`; `TestResults.xml` de las 13:09:51Z, esta pasada) |

### Prueba en Play
Editor a 1080x1920, lista «Prueba». Las lecturas largas se escribieron por script en la tarjeta inversa abierta, igual que en la ronda 1, no con palabras reales de la lista. Las palabras de Estudiar son las reales.

| Texto | Tamaño | Líneas | Cortado |
|---|---|---|---|
| 6 kana (にじゅうまる, también vista en una tarjeta real en la ronda 1) | 148 | 1 | No |
| 11 kana | 96,65 | 2 (8 + 3) | No |
| 12 kana | 96,65 | 2 | No |
| 14 kana | 96,65 | 2 (9 + 5) | No |
| 22 kana | 80,7 | 2 (11 + 11) | No |
| 新語, 二重丸, ～列目 en Estudiar | 193,35 | 1 | No; frase visible en las dos que la tienen |
| 新語 y 一生懸命 escritos por script en la tarjeta inversa | 193,35 | 1 | No |

- Las palabras cortas conservan el tamaño de la ronda 1.
- Las respuestas de esta pasada fueron rojas: el progreso guardado no cambia (`progress` 3 / 2 / 3, `reverseProgress` 1 / 0 / 1).
- El salto de línea no respeta la palabra: el de 22 kana parte きょう/いく (`play-inversa-lectura-larga-2.png`). Se lee entero.
- Sin probar: lecturas de más de 22 kana, donde el texto acabaría en «…», y el aspecto de un texto largo en la cara resuelta, donde el hueco grande muestra la palabra.

Capturas: `play-inversa-lectura-larga-2.png`, `play-inversa-lectura-6kana-2.png`, `play-estudiar-normal-2.png`.

### APK
- Ruta: `Builds/S16/KanjiLearning-S16-dev.apk`
- Tamaño: **52.073.762 B** (~49,7 MiB), 228 B menos que en la ronda 1.
- `BuildReport`: `Succeeded`, 13:14:28Z a 13:15:07Z (39 s). Su único error es el de `Subtarget`, ahora con 14 avisos.
- Build limpia: el `launcher-debug.apk` intermedio se apartó antes. Las 440 entradas suman 51.992.251 B, así que el espacio muerto es de 81.511 B (~81 KB). Ningún `.apk` anidado.

### Consola
Único error nuevo: `Subtarget` de `com.unity.pipeline` durante el build, ninguno del código del proyecto.

### Limpieza
- Movidos a la carpeta temporal de la sesión, no borrados: el `launcher-debug.apk` intermedio y la APK de la ronda 1.
- Editor fuera de Play. Sin commit.

### Sigue sin probar
Lo que la ronda 1 dejó pendiente, sobre todo deslizar con el dedo y todo lo que dependa del dispositivo Android. Los problemas 2, 3 y 4 de la ronda 1 siguen igual.
