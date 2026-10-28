# S04 — Revisión 2 (combinada con S05, ronda 2)

Revisor. Alcance: solo las correcciones a los 3 hallazgos importantes de `revision-1.md`, descritas en "Correcciones revisión 1" de `implementacion.md`. Ficheros: `UI/PopupService.cs`, `App/AppBootstrap.cs`, `Infrastructure/Storage/IFileSystem.cs`, `Infrastructure/Storage/AtomicFileWriter.cs`, `Tests/EditMode/Infrastructure/AtomicFileWriterTests.cs`, `Tests/PlayMode/PopupServiceTests.cs`.

## Tests
- Editor conectado (`unity status` = ready). Sin errores de compilación (`console status`: `compilationFailed=false`, 0 errores).
- EditMode: **166/166** superadas (`unity command test run -- --mode EditMode`, 2,1 s).
- PlayMode: **no se ha podido ejecutar en esta sesión**. `test list --mode PlayMode` encuentra los 2 tests (`PopupServiceTests.TwoPopups_BlockerSitsBetweenLowerAndTop`, `RichTextTmpEscapeTests...`), pero todas las ejecuciones (`test run` sync y async, con y sin `--filter`, y también `TestRunnerApi.Execute` directo) terminan con "Run started: 0 test(s)" y `TestResults.xml` con `total="0"`. Es un problema del runner del Editor, no del código: el asmdef `Kanji.UI.Tests` está bien y compila. El programador indica 2/2.
- Compensación: he reproducido el escenario del test de PlayMode con `script eval_file` en el Editor, usando el mismo `TestPopup` y el `PopupService` real. Resultado:
  `open A: C(off),Blocker,A | open C: A,Blocker,C | close C: C(off),Blocker,A | close A: todo desactivado`.
  El orden es el correcto en todos los pasos (en la revisión 1 salía `Blocker, A, C`).

## Verificación de los hallazgos de la revisión 1
1. **`PopupService.Reorder`** (`PopupService.cs:60-77`): **resuelto**. Ahora no depende del índice del blocker: manda al final los popups inferiores, después el blocker, después el popup superior y por último `_keepOnTop`. Con [StoryPopup, ConfirmPopup], el StoryPopup queda debajo del scrim. Hay test nuevo con 2 popups que cubre abrir y cerrar.
2. **Contador de `.tmp`** (`AppBootstrap.cs:101`): **resuelto**. Cuenta en `persistentDataPath/stories`, la misma carpeta que usa `JsonStoryRepository.cs:28`. Hay que regenerar el APK dev antes de T24.
3. **Fallback de `Replace`** (`IFileSystem.cs:39`, `AtomicFileWriter.cs:31-46`): **resuelto**.
   - El `catch` tiene `when (PlatformNotSupportedException || NotImplementedException)`, así que el resto de `IOException` se propaga sin copiar.
   - `AtomicFileWriter` ya no borra el `.tmp` si el fallo ocurre durante o después de `Replace` (`replaceStarted`), y sí lo borra si falla al escribir el `.tmp`.
   - `data?.Length` en el `catch`: `Write(path, null)` devuelve `IoError` sin lanzar. Lo cubre `NullData_ReturnsIoError_NoThrow`. La línea 40 (`data.Length`) solo se alcanza si `WriteAllBytes` aceptó `data`, así que no es un riesgo.
   - Tests nuevos: `ReplaceFails_KeepsTemp_AsOnlyIntactCopy` y `TempWriteFails_RemovesTemp`. Los dos pasan.

## Hallazgos nuevos

| Severidad | Fichero:línea | Problema | Escenario que lo provoca | Corrección sugerida |
|---|---|---|---|---|
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/IFileSystem.cs:39` | Falta el test `Replace_IOException_DoesNotFallBackToCopy` que pedía `arquitectura.md:208`. El filtro `when` de `DiskFileSystem` no tiene ningún test. Los tests nuevos usan `FaultyFileSystem` y no pasan por `DiskFileSystem.Replace`. | Una regresión que quite el `when` no la detectaría ningún test. | Opcional: un test solo para Windows/Editor que abra `dest` con `FileShare.None`, llame a `Replace` y compruebe que lanza `IOException` y que `dest` no cambia. Otra opción es anotar en `implementacion.md` que la arquitectura lo acepta así. |
| menor | `Assets/_Project/Scripts/App/AppBootstrap.cs:101` | El literal `"stories"` está duplicado en `JsonStoryRepository.cs:28`. | Si se cambia la carpeta en el repositorio, el contador vuelve a dar 0 sin avisar (el mismo bug de la revisión 1). | Exponer la carpeta desde el repositorio o una constante compartida. |
| menor | `Assets/_Project/Scripts/Infrastructure/Storage/AtomicFileWriter.cs:45-46` | El `.tmp` que se conserva tras un fallo de `Replace` no se recupera nunca. La siguiente escritura correcta lo sobrescribe y en el arranque solo se cuenta. | `Replace` falla con `dest` dañado: la historia sale como no legible aunque la copia íntegra está en `.tmp`. | Fuera del alcance de S04. Proponerlo al backlog (recuperar `.tmp` en el arranque si `dest` no se puede leer). |

## Pendiente (no lo verifica el revisor)
- Ejecutar PlayMode desde la ventana Test Runner del Editor para confirmar el 2/2 (en esta sesión el runner por MCP devuelve 0 tests).
- Prueba en el móvil (T24) con el APK dev regenerado, según los pasos de `revision-1.md`.

VEREDICTO: APROBADO
