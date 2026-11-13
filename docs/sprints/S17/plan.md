# S17 – Tarjetas a pantalla completa + recuadro de dibujo

Petición: las tarjetas ocupan toda la pantalla (ahora se ajustan al contenido). En las tarjetas inversas, abajo, un recuadro para dibujar con el dedo; se borra entre tarjeta y tarjeta y tiene botón "Borrar".

Alcance: solo la tarjeta de palabras de las listas (WordCardPopup). La tarjeta de kanji de las historias no cambia.

## Cambios
- WordCardPopup: `_fillScreen` (el cuerpo ocupa el alto que dejan los botones), `_drawArea`/`_pad`/`_btnClearDrawing`. El recuadro solo aparece en aprendizaje inverso, antes y después de Resolver; se limpia en cada tarjeta nueva y con "Borrar".
- DrawingPad (nuevo): RawImage + Texture2D RGBA32, trazos redondos; captura sus propios arrastres para que dibujar no deslice la tarjeta ni haga scroll.
- SceneBuilder: panel estirado a la zona segura con margen 32, sin ContentSizeFitter; cuerpo flexible; DrawArea (640 px) bajo el cuerpo con el botón Borrar arriba a la derecha.
- UiStrings.ClearDrawing = "Borrar".
- Tests PlayMode DrawingPadTests (punto, trazo continuo, Clear, sin umbral de arrastre).

## Verificación
Compilar, regenerar escena, EditMode + PlayMode, prueba en Play (consulta / estudiar / inverso; dibujar, borrar, cambio de tarjeta, swipe fuera del recuadro), APK Builds/S17.
