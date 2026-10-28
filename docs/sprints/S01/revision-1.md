# S01 — Revisión ronda 1 (T05)

Alcance: lo listado en `implementacion.md` (T01–T04) frente a `plan.md`, `arquitectura.md` y `docs/ui/{inicio,pantallas-vacias}.md`, más el APK `Builds/Android/KanjiLearning-dev.apk`.

## Verificación ejecutada
- Editor conectado (`unity status` = ready, 6000.6.2f1). Compilación: `compilationFailed: false`.
- Tests EditMode relanzados con `unity command test run --mode EditMode`: **5/5 verdes** (`NavigationStackTests`).
- Consola del Editor (búfer): 2 excepciones reales, ninguna activa ahora:
  - `07:42:52` `ArgumentException: The Object you want to instantiate is null` en `AppBootstrap.cs:24` → es el bug del builder ya corregido (las 4 referencias de `_screenPrefabs` en `Main.unity:168-172` están bien asignadas ahora).
  - `07:46:23` `ArgumentException: Subtarget property is not available for the platform (Android)` en `com.unity.pipeline` `PipelineRuntimeBuildProcessor.OnPreprocessBuild:64`, durante `AndroidBuild.cs:30`. Es el "1 error" del resumen del build (no un warning de Bee, como dice `implementacion.md`). No impide el build; el APK no lleva el runtime de Pipeline (solo `Unity.Pipeline.Attributes.dll`).
  - El resto de errores del búfer son `Package does not contain valid asset for: Assets/TextMesh Pro/Shaders/...` de la importación de TMP Essentials: ruido, sin efecto.
- ProjectSettings comprobados: `activeInputHandler: 1` (solo Input System), ARM64 solo, minSdk 26, Portrait, renderOutsideSafeArea, stripping Minimal, IL2CPP. TMP Settings: default = Regular SDF, fallbacks = Medium, Bold. Los 3 SDF con `ClearDynamicDataOnBuild = 1`.
- Sin dispositivo ni `adb` en el PATH: no se ha podido medir la memoria en el móvil ni probar la tecla Atrás real.

## APK (51,3 MB = 53 835 448 bytes)
Desglose comprimido (dentro del APK):
| Contenido | Comprimido | Sin comprimir |
|---|---|---|
| `libunity.so` | 16,6 MB | 42,0 MB |
| `libil2cpp.so` (dev, stripping Minimal) | 15,4 MB | 54,9 MB |
| `sharedassets0` (sobre todo los 3 TTF Noto Sans JP, ~5,7 MB c/u) | 10,8 MB | 17,9 MB |
| `global-metadata.dat` | 3,7 MB | 9,6 MB |
| `classes.dex` | 3,0 MB | 7,5 MB |
| resto (default resources, builtin extra, burst, libc++…) | ~2 MB | |

Recortes obvios (ninguno bloquea S01):
1. **Paquetes sin uso que acaban en el player** (ver `ScriptingAssemblies.json`): Visual Scripting (5 dll), Timeline, `com.unity.learn.iet-framework` (Unity.Tutorials), 2D Animation/IK/SpriteShape/Tilemap Extras, `Unity.2D.Welcome`, PathTracing/UnifiedRayTracing. Quitar del manifest también editor-only innecesarios (`2d.psdimporter`, `2d.aseprite`, `collab-proxy`) y módulos built-in sin uso (cloth, terrain, terrainphysics, vehicles, wind, video, xr, umbra, ai, unitywebrequest*, particlesystem si no se usa). Reduce `libil2cpp` + metadata.
2. **Build de release** (no Development) y stripping **Low/Medium** con `link.xml` (ya previsto para Shift-JIS en S02): `libil2cpp` baja mucho.
3. **Fuentes**: 3 pesos completos = ~10,8 MB. Quitar Medium (usar Regular o Bold para etiquetas) ahorra ~3,6 MB; no se puede subsetear porque las historias traen kanji arbitrarios.
Estimación con 1+2+3: release en torno a 25–30 MB.

## Hallazgos
| Severidad | Fichero:línea | Problema | Escenario | Corrección sugerida |
|---|---|---|---|---|
| menor | `Assets/_Project/Scripts/Editor/FontSetup.cs:46` | Font assets con sampling 90 + padding 9 en atlas 1024: caben ~80-90 glifos por atlas. Con multi-atlas, una página del lector (S03) con cientos de kanji distintos generará varios atlas de 1 MB por peso, más memoria y un submesh/draw call extra por atlas. | Lector con texto real (S03); hoy no se nota. | Bajar sampling a ~48-64 y padding a 5-6 (o atlas 2048) antes del lector; medir en dispositivo. Pasar al backlog S02/S03. |
| menor | `Assets/TextMesh Pro/Resources/TMP Settings.asset:36-38` | Medium y Bold son fallbacks globales aunque se asignan directamente por componente. Un glifo ausente (p. ej. 齉, 龘) se busca en Regular → Medium → Bold (3 consultas FreeType/frame en que se reconstruye el texto) y obliga a tener las 3 fuentes cargadas. | Texto con glifos no cubiertos por Noto Sans JP. | Dejar la lista global vacía (o solo una futura fuente CJK ampliada); Medium/Bold ya tienen Regular como fallback propio. |
| menor | `Assets/_Project/Scripts/App/AppBootstrap.cs:22-27` | Sin validación: un prefab nulo lanza en `Awake` y la app se queda en negro (ocurrió en este sprint, ver consola 07:42:52); un `ScreenId` repetido sobrescribe en silencio. | Escena regenerada mal o prefab con Id duplicado. | `if (prefab == null) { Debug.LogError(...); continue; }` y comprobar `ContainsKey` antes de añadir. |
| menor | `docs/sprints/S01/implementacion.md` (T04) | Diagnóstico incorrecto del "1 error" del build: es una excepción del build processor de `com.unity.pipeline` (paquete experimental), no un warning de Bee. | Cada build Android desde el Editor. | Corregir la nota; si molesta en CI, el build por CLI con el Editor cerrado no carga ese processor de la misma forma (a verificar). |
| menor | `Assets/_Project/Scripts/Editor/AndroidBuild.cs:15` | Menú `Kanji/Build Android APK` no coincide con `arquitectura.md` (`Kanji/Build/Android Dev APK`). | — | Alinear uno de los dos (lo decide el arquitecto). |
| menor | `Assets/_Project/Scripts/Editor/AndroidBuild.cs:53-55` | `ApplyPlayerSettings` deja `EditorUserBuildSettings.development = true` de forma global: el siguiente build hecho desde la ventana Build Profiles también sale en Development. | Hacer un build de release a mano tras usar el menú. | Aceptable en S01; cuando haya build de release, parametrizar (dev/release) en vez de tocar el ajuste global. |
| a verificar | `Assets/_Project/Scripts/UI/BackButtonRouter.cs:22-26` | La tecla Atrás de Android solo se ha probado como Escape en el Editor. Con `targetSdk = Auto` (API 35/36), el gesto Atrás predictivo de Android 13+ puede no entregar `KEYCODE_BACK` según cómo lo gestione el Activity de Unity 6. | Pulsar Atrás en Inicio en un dispositivo Android 14-16 debe cerrar la app (criterio T02). | Probarlo en un dispositivo real al instalar el APK; si falla, revisar `enableOnBackInvokedCallback` en el manifest. |
| a verificar | — | Memoria en dispositivo no medida (sin dispositivo/adb). Estimación: 3 TTF cargadas (~17 MB) + 3 atlas Alpha8 1024 (~3 MB) + base Unity URP. | — | Medir con el Profiler conectado (el APK es Development) cuando haya dispositivo. |

No se han encontrado bugs de ciclo de vida, suscripciones sin desuscribir ni allocations por frame: `BackButtonRouter.Update` no asigna memoria, los listeners se añaden una sola vez en `Init` y los objetos viven lo mismo que la escena. Las dependencias entre asmdefs cumplen `arquitectura.md` (Core con `noEngineReferences`, App es el único que toca `Application.Quit`). Los criterios de T01–T04 se cumplen en el Editor; el texto `FontTest_TEMP` es temporal y está aceptado para este sprint.

VEREDICTO: APROBADO
