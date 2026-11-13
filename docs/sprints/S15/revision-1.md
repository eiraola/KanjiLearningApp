# Revisión 1 - S15 Estudiar una lista de palabras (2026-11-05)

Resultado: **sin bloqueantes de funcionamiento.** Compila, tests en verde y APK generada. Hay un aviso visual por decidir (el «punto rojo» es un aro) y falta la prueba en el móvil.
Esta revisión cubre compilación, escena, tests, prueba en Play y build. No es una revisión de código.

## Compilación
- Limpia a la primera: `compilationFailed=false`, sin errores.

## Correcciones hechas
- Ninguna. No se ha tocado código.

## Escena
- `Kanji/Build S01 Scene` ejecutado y escena guardada: 0 errores, 0 avisos, ningún error de `FindProperty` ni `NullReference`.
- Comprobado por script que están asignadas las referencias nuevas de `WordCardPopup` (complicadas, Resolver, verde/rojo, `CardSwipe`) y de `WordListScreen` (filtro, prefab de fila).

## Tests
| Modo | Resultado | Antes |
|---|---|---|
| EditMode | **274/274** | 268 |
| PlayMode | **28/28** | 28 |

Los tests nuevos de S15 son solo EditMode. La tarjeta en estudio y el filtro no tienen tests PlayMode propios; los cubre la prueba de abajo. `CardSwipeTests` (PlayMode) cubre el deslizamiento como componente.

## Consola
2 errores en toda la sesión, ninguno del código del proyecto:
1. `No command named 'input'`: comando mal escrito por mí en la CLI.
2. `ArgumentException: Subtarget property is not available for the platform (Android)`, durante el build. Es el mismo error de `com.unity.pipeline` que en S14. No impide el build.

Avisos: los que provocan a propósito los tests (`write FAILED ... Simulated replace failure`), los del build (25) y uno mío: `.meta` huérfano al mover una captura de `Assets/Temp` por fuera de Unity (carpeta ya eliminada).

## APK
- Ruta: `Builds/S15/KanjiLearning-S15-dev.apk`
- Tamaño: **52.071.628 B** (~49,7 MiB). Build `Succeeded` en unos 6 min. S14: 52.054.940 B.
- Build limpia: el `launcher-debug.apk` intermedio se apartó antes. Las entradas suman 51.990.280 B, así que el espacio muerto es de 81 KB.

## Prueba en Play
Editor a 1080x1920, sobre la lista «Prueba» de S14 (3 palabras). Cada paso se lanzó en una llamada aparte, con frames de por medio.
Forma de accionar: de la tarjeta al primer estudio, con `onClick.Invoke`; en las pruebas de Atrás y al marcar en estudio, los botones con **clics simulados del Input System** (`simulate_pointer`) sobre su posición en pantalla.

| Paso | Resultado |
|---|---|
| Abrir la lista con JSON de S14 (sin campos nuevos) | OK; círculos a 0, sin marcas, «Todas (3)» / «Complicadas (0)» |
| Abrir tarjeta → «Añadir a complicadas» | OK; pasa a «Ya en complicadas», marca en la fila, «Complicadas (1)» |
| Filtro «Complicadas (1)» → «Todas» | OK; solo esa palabra → las 3 |
| ⋮ → Estudiar | OK; palabra y frase, Resolver, sin Cerrar |
| Resolver | OK; lectura, significado, traducción, verde/rojo |
| Palabra sin frase (新語) | OK; la fila de frase se oculta |
| Verde, rojo, verde hasta el final | OK; resultados 3 / 2 / 1, **67 %**, fallada ～列目 |
| Aceptar | OK; vuelve a la lista con progreso 1/5, 0/5 y 1/5 |
| Toque fuera durante el estudio | OK; no cierra |
| Atrás con 1 respondida | OK; resultados con total 1, 100 % |
| Atrás con 0 respondidas | OK; vuelve a la lista sin resultados |
| Marcar complicada durante el estudio | OK; «Ya en complicadas» y marca en la fila al terminar |
| Otra pasada | OK; el orden cambia (barajado) |
| Salir de Play y volver a entrar | OK; progreso y marcas conservados |
| Quitar la marca con el filtro activo | OK; la fila desaparece y baja a «Complicadas (1)» |

Capturas:
- `play-lista-filtro.png`, `play-lista-progreso.png`: lista con filtro y progreso.
- `play-estudio-pregunta.png`, `play-estudio-resuelta.png`: tarjeta en estudio.
- `play-resultados.png`, `play-resultados-atras.png`: resultados.

### Pruebas limitadas o sin hacer
- **Deslizar con puntero simulado: no avanzó** (down, move, up con `simulate_pointer`, ya con la tarjeta resuelta). No he confirmado la causa. Sospecho de la simulación, porque llamando a `OnBeginDrag`/`OnDrag`/`OnEndDrag` del `CardSwipe` de la escena sí funciona: derecha cuenta verde, izquierda cuenta rojo, corto no cuenta. **Hay que probarlo con el dedo en el móvil.**
- **Deslizar sin resolver:** el intento con puntero simulado no cuenta como prueba por lo anterior. Solo está visto en código (`CanSwipe`).
- **Atrás con tecla real no probado:** `simulate_key` no llega a pulsar Escape (`escapeKey.isPressed` sigue en False). Se llamó a `PopupService.HandleBack()`, la función a la que llama `BackButtonRouter`; la lectura de la tecla no está cubierta en esta prueba.
- Sin probar: Atrás en la pantalla de resultados, fallo de guardado durante el estudio (toast), pegar 3 palabras nuevas (se reutilizó la lista existente), más de una pantalla de palabras con scroll, y todo lo que dependa del dispositivo Android.

Datos de prueba: «Prueba» queda en `persistentDataPath/wordlists/` del PC con progreso y una marca. No está en el APK.

## Avisos visuales (no se han cambiado)
1. **El «punto rojo» no es un punto.** `SceneBuilder` usa `_theme.CircleSprite`, que es `circle_2px.png`, un aro de 2 px. A 32 px se ve como un círculo hueco muy fino y cuesta distinguirlo (`play-lista-filtro.png`). Función correcta, pero no cumple lo que describe el encargo. Es probablemente la misma causa de los puntos ⋮ poco visibles que anotó S14.
2. **El progreso también se ve poco.** Con el mismo sprite, 1/5 es un arco fino sobre un aro fino (`play-lista-progreso.png`). No he comprobado cómo se ve en las celdas de complicados de S11, que usan el mismo sprite.
3. **Texto de resultados.** La pantalla, compartida con las historias, dice «Kanjis fallados» aunque aquí se estudian palabras.

Propuesta: usar un sprite de círculo relleno para la marca y el ⋮, y decidir aparte el grosor del progreso. Lo decide el usuario; el encargo era no rediseñar.

## Limpieza
- `Assets/Temp/`, creada por las capturas, a la papelera con `AssetDatabase.MoveAssetToTrash`. Las capturas están en `docs/sprints/S15/`.
- APK intermedio de S14 movido a la carpeta temporal de la sesión, no borrado. Gradle generó uno nuevo.
- Game View del Editor: sigue en 1080x1920 («Portrait»); antes estaba a 729x602. No afecta al proyecto.
- Sin commit.

## Para el planificador
- La tabla de tareas de `plan.md` sigue con todo en «pendiente».
- Sigue disponible Unity CLI 1.0.0-beta.16 (hay instalada la beta.14); no he actualizado porque requiere la aprobación del usuario.

## Ronda 2 - Marca y ⋮ como discos rellenos (2026-11-05)

Resultado: **aviso visual 1 corregido, sin bloqueantes.** Compila, tests en verde y APK generada. Falta la prueba en el móvil.

### Cambio
Solo en `SceneBuilder.cs`:
- Nuevo `FilledCircle()`, con el sprite integrado `UI/Skin/Knob.psd`.
- Los 3 puntos del botón ⋮ y `DifficultMark` lo usan en lugar de `_theme.CircleSprite`.
- `DifficultMark` pasa de 32x32 a 36x36.

No se han tocado los círculos de progreso ni el texto de resultados (avisos 2 y 3), por decisión del coordinador.

### Comprobaciones
| Comprobación | Resultado |
|---|---|
| Compilación | `compilationFailed=false` |
| `Kanji/Build S01 Scene` y guardado | OK; escena sin cambios pendientes tras guardar |
| Prefabs, por script | `DifficultMark`: `Knob`, 36x36. `Dot` de `WordList`: `Knob`. `ProgressTrack`: sigue con `circle_2px` |
| EditMode | **274/274** |
| PlayMode | **28/28** (`--async_tests true`; `TestResults.xml` de las 10:08:27Z, esta pasada) |

### Prueba en Play
Editor a 1080x1920, lista «Prueba». Se llegó a la lista con `onClick.Invoke`, sin clics simulados.
- La marca roja y los 3 puntos del ⋮ se ven como discos rellenos: `play-marca.png`.
- El disco de la marca se ve algo menor que sus 36 px, unos 32 a ojo, porque el sprite tiene margen transparente.
- En la misma captura se aprecia que el progreso sigue siendo un arco fino (aviso 2, sin cambios).
- Solo se ha mirado esa pantalla. No se ha repetido el resto de la prueba en Play de la ronda 1.
- `capture screenshot` saca la pantalla sin la UI (solo el fondo). La captura se hizo con `ScreenCapture.CaptureScreenshot`.

### APK
- Ruta: `Builds/S15/KanjiLearning-S15-dev.apk`
- Tamaño: **52.071.862 B** (~49,7 MiB), 234 B más que en la ronda 1.
- `BuildReport`: `Succeeded`, 10:13:40Z a 10:14:30Z (50 s, frente a unos 6 min de la ronda 1). Su único error es el de abajo.
- Build limpia: el `launcher-debug.apk` intermedio se apartó antes. Las 440 entradas suman 51.990.351 B, así que el espacio muerto es de 81.511 B (~81 KB, igual que en la ronda 1). Ningún `.apk` anidado.

### Consola
2 errores nuevos, ninguno del código del proyecto:
1. `ArgumentException: Subtarget property is not available for the platform (Android)`, durante el build. Es el mismo de `com.unity.pipeline` de la ronda 1 y de S14.
2. `No command named 'job status'`: comando mal escrito por mí en la CLI.

### Limpieza
- Movidos a la carpeta temporal de la sesión, no borrados: el `launcher-debug.apk` intermedio anterior y la APK de la ronda 1.
- Sin commit.

### Sigue sin probar
Lo que la ronda 1 dejó pendiente, sobre todo deslizar con el dedo y el resto de lo que depende del dispositivo Android. Esta ronda no lo cubre.
