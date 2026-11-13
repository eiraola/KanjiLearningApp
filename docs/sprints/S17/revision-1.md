# Revisión 1 - S17 Tarjetas a pantalla completa + recuadro de dibujo (2026-11-05)

Resultado: **funciona lo pedido, tras 3 correcciones en `SceneBuilder`.** Compila, tests en verde y APK generada. Falta la prueba en el móvil, con el dedo.
Esta revisión cubre compilación, escena, tests, prueba en Play y build. No es una revisión de código.

## Compilación
- Sin errores a la primera (`compilationFailed=false`). Unity creó los `.meta` de `DrawingPad.cs` y `DrawingPadTests.cs`.

## Correcciones hechas
Las tres en `Assets/_Project/Scripts/Editor/SceneBuilder.cs`, `BuildWordCardPopup`. La tarjeta de kanji no se toca.

### 1. El cuerpo largo no hacía scroll
El `Viewport` solo tiene `RectMask2D`, sin Graphic, y los textos tienen `raycastTarget=false`. El toque caía en `Panel`, así que el `ScrollRect` nunca recibía el arrastre.
```diff
             bodyLayout.flexibleHeight = 1;
+            // The labels ignore raycasts: without this the panel takes every drag and the body never scrolls.
+            scroll.viewport.gameObject.AddComponent<Image>().color = Color.clear;
```
Nota: `BuildPopupBody` se usa también en otros popups y no lo he cambiado. No he comprobado si esos popups tienen el mismo problema.

### 2. La fila verde/roja ocupaba media tarjeta tras Resolver
El `childForceExpandHeight` de la fila le da `flexibleHeight=1`, y repartía el alto libre con el cuerpo (fila de 972 px en lugar de 179 a 1080x2400).
```diff
-            answerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 160;
+            var answerLe = answerRow.gameObject.AddComponent<LayoutElement>();
+            answerLe.preferredHeight = 160;
+            answerLe.flexibleHeight = 0; // the row's force-expand would otherwise take half of the free height from the body
```

### 3. El fondo del recuadro se deformaba y casi no se veía
`input_field` no tiene borde vertical (`border` 32/0/32/0), así que sus esquinas se estiraban a lo alto. Con `SurfaceRaised`, además, el botón Borrar quedaba del mismo color que el fondo.
```diff
-            drawBg.sprite = _theme.InputSprite;
+            drawBg.sprite = _theme.PanelSprite; // InputSprite has no vertical border and deforms when this tall
             drawBg.type = Image.Type.Sliced;
             drawBg.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
-            drawBg.color = _theme.SurfaceRaised;
+            drawBg.color = _theme.Background;
```

## Escena
- `Kanji/Build S01 Scene` ejecutado y guardado tras cada corrección. La escena queda sin cambios pendientes (`isDirty=False`).

## Tests
Pasada final, con las 3 correcciones ya aplicadas.

| Modo | Resultado | Antes |
|---|---|---|
| EditMode | **276/276** | 276 |
| PlayMode | **32/32** (`--async_tests true`; `TestResults.xml` de las 13:56:16Z, esta pasada) | 28 + 4 de `DrawingPadTests` |

Ningún test cubre las 3 correcciones; las cubre la prueba de abajo.

## Prueba en Play
Editor a **1080x2400** (zona segura = pantalla completa), lista «Prueba» de 3 palabras.
**Forma de probarlo:** toques y arrastres inyectados como eventos de ratón del Input System, repartidos en varios frames. Pasan por el `InputSystemUIInputModule` real, no por `onClick.Invoke`. Es ratón simulado, no dedo.

### (a) Consulta
| Comprobación | Resultado |
|---|---|
| La tarjeta llena la pantalla | OK; panel x35-1044, y35-2364 (32 unidades × escala 1,118) |
| Sin recuadro de dibujo | OK |
| Cuerpo largo (texto escrito por script, contenido de 4608 frente a visor de 1619) | OK tras la corrección 1; llega al final |
| Cerrar | OK, con toque simulado |

### (b) Estudiar
| Comprobación | Resultado |
|---|---|
| Pregunta a pantalla completa, sin recuadro | OK |
| Resolver | OK; fila de respuesta de 179 px tras la corrección 2 |
| Rojo con botón | OK; pasa a la palabra siguiente |
| Deslizar a la derecha y a la izquierda sobre el cuerpo | OK; cuenta como respuesta y sale la palabra siguiente, o los resultados tras la última |

### (c) Aprendizaje inverso
| Comprobación | Resultado |
|---|---|
| Recuadro bajo el cuerpo, Borrar arriba a la derecha | OK; 716 px de pantalla (640 unidades) |
| Trazo donde se arrastra | OK; extremos del trazo en los puntos pedidos, sin desplazamiento apreciable en la captura |
| Dibujar no mueve la tarjeta | OK; desplazamiento máximo del panel **0** durante un arrastre horizontal de 800 px, ya con Resolver hecho |
| Dibujar no hace scroll | OK; `contentY` sigue en 0 tras un arrastre vertical en el recuadro, con cuerpo largo |
| Borrar | OK; `pad=0` |
| Tras Resolver el dibujo sigue | OK; 2792 texels leídos tras Resolver, y el trazo se ve en la captura |
| Tarjeta nueva, recuadro vacío | OK por las dos vías: deslizando y con el botón verde (de 2654 texels a `pad=0`) |
| Deslizar sobre el cuerpo, fuera del recuadro | OK; responde y pasa a la siguiente |
| Scroll del cuerpo en inverso | OK; `contentY` 2274, sin añadir tinta |
| Todos los botones visibles | OK a 1080x2400 y a 1080x1920 (cuerpo 720, recuadro 640) |
| Lectura delante legible | OK; にじゅうまる en una línea |

### Capturas (`docs/sprints/S17/`)
- Con el aspecto final: `play-c-trazo.png`, `play-c-1080x1920.png`, `play-c-inversa-resuelta-larga.png`.
- Tomadas antes de la corrección 3, con el fondo anterior del recuadro: `play-c-inversa-pregunta.png`, `play-c-inversa-trazo-tras-resolver.png`. Esta última muestra que el dibujo sigue tras Resolver.
- Otras: `play-a-consulta.png`, `play-a-scroll.png`, `play-b-estudiar-pregunta.png`, `play-b-estudiar-resuelta.png`, `play-b-resultados.png`.

Una captura de la tarjeta salió entera en negro, como en S16. Al repetirla salió bien y el estado leído por script era correcto; sigo sin confirmar la causa.

## APK
- Ruta: `Builds/S17/KanjiLearning-S17-dev.apk`
- Tamaño: **52.082.810 B** (~49,7 MiB), 9.048 B más que S16.
- `Succeeded`, 13:56:46Z a 14:01:41Z (~5 min). Su único error es el de `Subtarget` de `com.unity.pipeline`, el mismo de sprints anteriores; 23 avisos.
- Build limpia: el `launcher-debug.apk` intermedio se apartó antes. Las 440 entradas suman 52.001.297 B, así que el espacio muerto es de 81.513 B (~81 KB, igual que antes). Ningún `.apk` anidado.

## Consola
Errores nuevos en esta sesión, ninguno del código del proyecto:
1. `Subtarget property is not available for the platform (Android)`, durante el build.
2. `Path '../docs/sprints/S17/play-a-consulta.png' must not contain '..'`: comando mal escrito por mí.

Sin revisar los avisos uno a uno.

## Problemas vistos, sin cambiar
1. **Icono cortado.** El círculo de los iconos de «Añadir a complicadas» y «No lo sabía» sale cortado por abajo. Ya se ve en las capturas de S16, no viene de este sprint.
2. **Botones de respuesta de distinto ancho** (472 y 394 px). También como en S16.
3. **Hueco grande en tarjetas cortas.** Es consecuencia de llenar la pantalla: los botones quedan abajo y el centro vacío. Encaja con lo pedido, pero conviene mirarlo en el móvil.

## Sin probar
- **Con el dedo y en Android**: toque real, latencia del trazo, dibujar con dos dedos. El trazo copia la textura entera en cada evento, sin medir el coste en el móvil.
- **Zona segura con muesca**: en el Editor ocupa toda la pantalla, así que el margen solo se ha visto sin recorte.
- Salir del recuadro a media pincelada: por código se ajusta al borde, sin probar.
- Atrás con tecla real, y el resto de popups que usan `BuildPopupBody`.
- El guardado del progreso: no he mirado el JSON esta vez.

Datos de prueba: «Prueba» en `persistentDataPath/wordlists/` del PC, con las respuestas de esta prueba acumuladas (2 verdes en inverso; 1 verde y 2 rojas en Estudiar). No está en la APK.

## Limpieza
- Movidos a la carpeta temporal de la sesión, no borrados: el `launcher-debug.apk` intermedio anterior y dos capturas de trabajo.
- Una captura de trabajo mía, ya obsoleta, se borró.
- Una captura guardada con `capture game_view` creó `Assets/docs` por error; la moví y eliminé esa carpeta y su `.meta`, ambas creadas por mí.
- `Run In Background`, activado para la prueba, ha vuelto al valor del proyecto. Editor fuera de Play. La Game View queda en 1080x2400 (añadí ese tamaño a la lista). Sin commit.

## Para el planificador
- Sigue disponible Unity CLI 1.0.0-beta.16 (hay instalada la beta.14); no he actualizado, por indicación del encargo.
