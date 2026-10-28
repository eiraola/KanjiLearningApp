Inicio: 2026-10-26T16:06:00Z

# Sprint S12 — Nueva portada, icono y arranque sin logo

Estado: hecho

## Objetivo
La imagen `Assets/Art/KanjiBook.png` pasa a ser la cara de la app: portada en grande con «Leer historias» debajo, icono de la app y arranque sin el logo de Unity. El resto de opciones pasa a un menú lateral.

## Decisiones
- El original no se toca. Versiones derivadas en `Assets/_Project/Art/Cover/`: `KanjiBook_Cutout.png` (fondo blanco exterior transparente, recortado), `KanjiBook_Icon.png` (1024, cuadrado), `KanjiBook_IconFg/Bg.png` (icono adaptativo de Android, fondo = `Background` del tema).
- Recorte: relleno por inundación desde los bordes (tolerancia 40) con borde suavizado; las páginas del libro quedan opacas porque las cierra el contorno.
- Logo de Unity: Unity 6 permite quitar el splash en todos los planes (también Personal); `AndroidBuild` pone `SplashScreen.show = false`.
- Menú lateral a la izquierda (78 % del ancho, desliza 0,2 s = `ScreenFadeOutSeconds`), botón hamburguesa arriba a la izquierda. Contiene Cargar historias, Kanjis complicados y Salir. Tocar fuera o Atrás lo cierra.
- Sin título de texto en la portada: la imagen ya lo es.

## Reglas de ahorro
- Sin subagentes; la sesión principal escribe el código y la sesión en el PC compila, regenera la escena y construye.

## Tareas
| Id | Tarea | Criterios de aceptación | Estado |
|---|---|---|---|
| T49 | Recorte e icono | - Sin halo blanco sobre fondo oscuro; páginas opacas.<br>- Icono normal y adaptativo aplicados por `AndroidBuild.ApplyPlayerSettings`. | hecho |
| T50 | Arranque sin logo de Unity | - `SplashScreen.show = false`; al abrir la app no aparece "Made with Unity". | hecho |
| T51 | Portada nueva + `SideMenu` | - Imagen grande centrada, «Leer historias» debajo (176 px).<br>- Hamburguesa abre el menú; fondo oscuro tocable lo cierra; Atrás lo cierra antes de salir.<br>- Al ir a otra pantalla el menú queda cerrado al volver. | hecho |
| T52 | Compilar, tests, APK dev, retro | - EditMode y PlayMode en verde; `Builds/S12/KanjiLearning-S12-dev.apk`. | hecho |

Prueba del usuario: icono en el lanzador, arranque sin logo, portada, menú lateral (abrir, tocar fuera, Atrás, cada opción).
