# Retro S12 - Nueva portada, icono y arranque sin logo

Estado: hecho. Sin ronda de revisión formal (sprint de una sola sesión, sin subagentes). Compilación limpia a la primera, sin correcciones. EditMode 243/243, PlayMode 23/23.
APK: `Builds/S12/KanjiLearning-S12-dev.apk`, 68.794.451 B (~65,6 MiB; S11: 68.793.451 B, prácticamente igual).
Pendiente (usuario): probar en el móvil icono en el lanzador, arranque sin logo, portada y menú lateral (abrir, tocar fuera, Atrás, cada opción).

## Hecho
- T49: recorte e icono en `Assets/_Project/Art/Cover/`. Player Settings de Android: icono adaptativo con las 6 resoluciones y las 2 capas (12/12 texturas). Unity 6 solo ofrece el tipo `Adaptive` para Android con este nivel de API.
- T50: `PlayerSettings.SplashScreen.show = false` comprobado tras el build.
- T51: portada nueva y `SideMenu` (en `Home.prefab`), escena regenerada con `Kanji/Build S01 Scene`.
- T52: compilación, tests, APK y esta retro.

## Tokens (`node tools/token-report.mjs --since 2026-10-26T16:06:00Z`)
Total local: **960.300** (S09: 18.160.465). Solo la sesión en el PC; la sesión coordinadora (que escribió el código) no deja transcripción local y no entra en el informe.

| Agente | Turnos | Total |
|---|---|---|
| orquestador | 11 | 960.300 |

## Qué funcionó
- Código escrito fuera y la sesión del PC solo compila, regenera, prueba y construye: un orden de magnitud menos de tokens que los sprints con subagentes.
- La secuencia de la memoria (recompile + status, invoke_menu + save_all, PlayMode async + status, build detach) funcionó sin intentos fallidos.

## Qué cambiar (medidas)
1. La excepción `Subtarget property is not available for the platform (Android)` de `com.unity.pipeline` sale en cada build; no rompe el APK pero ensucia la consola. Anotarla como conocida o filtrarla al revisar errores.
2. El tamaño del APK no cambia casi nada con la portada nueva: revisar si `Assets/Art/KanjiBook.png` original sigue entrando en el build y si puede excluirse.
3. Para que el informe de tokens cubra el sprint entero, anotar a mano el gasto de la sesión coordinadora al cerrar.
