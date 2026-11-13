# Revisión 1 - S14 Listas de palabras (2026-11-05)

Resultado: **sin bloqueantes.** Compila, tests en verde y APK generada. Falta la prueba en el móvil.
Esta revisión cubre compilación, escena, tests, prueba en Play y build. No es una revisión de código.

## Compilación
- Limpia a la primera: 0 `error CS`, `compilationFailed=false`.

## Correcciones hechas
- Ninguna. No se ha tocado código.

## Escena
- `Kanji/Build S01 Scene` ejecutado y escena guardada: 0 errores, 0 avisos, ningún error de `FindProperty`.
- Prefabs nuevos: `Screens/WordList`, `Screens/WordLists` y `Widgets/SimpleRow`.
- La regeneración vuelve a modificar `NotoSansJP-Regular SDF.asset` y `NotoSansJP-Bold SDF.asset`.

## Tests
| Modo | Resultado | Antes |
|---|---|---|
| EditMode | **268/268** | 243 |
| PlayMode | **28/28** | 28 |

Los tests de S14 son solo EditMode (+25). Las pantallas y popups nuevos no tienen tests PlayMode; solo los cubre la prueba manual de abajo.

## Consola
3 errores en toda la sesión, ninguno del código del proyecto:
1. `No command named 'list'`: comando mal escrito por mí en la CLI.
2. `Path '../docs/...' must not contain '..'`: ruta de captura rechazada, también mía.
3. `ArgumentException: Subtarget property is not available for the platform (Android)`, durante el build. Viene de `PipelineRuntimeBuildProcessor.OnPreprocessBuild` (paquete `com.unity.pipeline`). No impide el build, pero es el «1 errors» de su resumen. No he comprobado si salía en builds anteriores.

Los avisos de la consola son de los tests, que los provocan a propósito (p. ej. `AtomicFileWriterTests`), y del build (TMP, IL2CPP, `DEVELOPMENT_BUILD` obsoleto en `AppBootstrap.cs:136`, descarga de manifests de paquetes).

## APK
- Ruta: `Builds/S14/KanjiLearning-S14-dev.apk`
- Tamaño: **52.054.940 B** (~49,6 MiB). Build `Succeeded` en 6 min 52 s.
- S13 pesaba 84.073.593 B, pero sus entradas solo suman 51.927.767 B: llevaba **32,1 MB de espacio muerto**. En S14 son 81 KB.
- La causa probable del engorde es el APK intermedio de Gradle reutilizado, no los atlas de fuentes como suponía la retro S13: el contenido real apenas cambia entre S13 y S14 (+46 KB). Conviene mantener esta limpieza antes de cada build.

## Prueba en Play
Editor a 1080x1920. Los botones se accionaron por script (`onClick.Invoke`), no con toques reales. El portapapeles se simuló con `ReadClipboard` usando un TSV de prueba de 2 palabras, así que **no se ha usado el ejemplo de 8 palabras del usuario**.

| Paso | Resultado |
|---|---|
| Menú lateral → Listas → + crear «Prueba» | OK; la fila muestra «0 palabras» |
| Abrir la lista vacía | OK; mensaje de lista vacía |
| ⋮ → Añadir palabras → Pegar | OK; los tabuladores se conservan |
| Añadir | OK; 2 filas (二重丸, ～列目), toast «2 añadidas» |
| Abrir tarjeta | OK; palabra, frase, lectura, significado y traducción |
| Pegar otra vez | OK; popup «Palabras repetidas (2)» con la lista, Ignorar, Actualizar y Cancelar |
| Actualizar | OK; toast «2 actualizadas», la tarjeta muestra el significado nuevo |
| Salir de Play y volver a entrar | OK; la lista conserva sus datos («2 palabras») |
| Pegar 1 repetida + 1 nueva → Ignorar | OK; toast «1 añadida · 1 repetida ignorada», la repetida no cambia |

Capturas: `play-lista.png` (hecha antes de la prueba de Ignorar) y `play-tarjeta.png`.

Sin probar:
- Cancelar en el popup de repetidas.
- Cerrar la tarjeta tocando fuera o con Atrás.
- Teclado y toques reales, y todo lo que dependa del dispositivo Android.

Incidencia de la prueba: una primera pasada de Ignorar, con los pasos lanzados seguidos, dio popup con texto vacío y pantalla sin filas, sin escribir nada en disco. Repetida paso a paso funcionó bien. Lo atribuyo a no dejar pasar frames entre pasos, pero no he confirmado la causa exacta. Si en el móvil aparece algo parecido con toques muy rápidos, hay que revisarlo.

Datos de prueba: la lista «Prueba» sigue en `persistentDataPath/wordlists/` del PC. No está en el APK ni afecta al móvil.

## Limpieza
- `Assets/_Recovery/` a la papelera mediante `AssetDatabase.MoveAssetToTrash`. Está en git como borrado sin commit (`0.unity`, su `.meta` y `_Recovery.meta`).
- APK intermedio de S13 movido a la carpeta temporal de la sesión en vez de borrado, porque el permiso de borrado directo fue denegado. Gradle generó uno nuevo.
- `Assets/Temp/`, creada por las capturas, también a la papelera.

## Para el planificador
- `plan.md` está desfasado: D1 dice que las repetidas se saltan (ahora hay popup con Ignorar/Actualizar) y habla de botón ⚙ (ahora es ⋮).
- Las «medida 1» y «medida 3» citadas en el encargo no coinciden con `S13/retro.md`: su medida 1 habla de los atlas y la 3 de excluir `KanjiBook.png`. Lo de excluir `KanjiBook.png` sigue sin hacer.
- Detalle visual en `play-lista.png`: los puntos del botón ⋮ se ven pequeños y poco contrastados. No se ha cambiado.
