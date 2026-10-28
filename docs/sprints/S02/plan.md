Inicio: 2026-10-22T09:08:22Z

# Sprint S02 — Cargar archivo de texto (selector, decodificación, vista previa)

## Objetivo
El usuario elige un .txt del dispositivo y ve nombre, tamaño, codificación y vista previa; sin guardar nada. De paso, se endurece el backlog de S01 y se valida en dispositivo.

## Reglas del sprint (retro S01)
- Una invocación de programador por tarea, con contexto nuevo.
- El orquestador deja el Editor sin diálogos modales (p. ej. añade el paquete yasirkula e importa antes de lanzar al programador).
- Una sola espera larga para builds/tests, sin sondeos.
- Límites de CLAUDE.md: máx. 2 consultas programador→arquitecto por tarea; máx. 2 idas y vueltas arquitecto↔diseñador; máx. 2 rondas de revisión.

## Tareas
Orden: T06 y T07 independientes, luego T08; T09 independiente; T10 al final.

| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T06 | Puerto `IFilePicker` + adaptador NativeFilePicker (yasirkula), ADR `docs/arquitectura/adr/0002`, con rama de Editor dentro del adaptador. 2 scripts | - En dispositivo abre el selector y devuelve bytes.<br>- En Editor devuelve un archivo de prueba.<br>- Cancelar se distingue de error en el resultado. | arquitecto (breve), programador | hecha |
| T07 | `TextDecoder` UTF-8 / UTF-8 con BOM / Shift-JIS; límite 1 MB comprobado en bytes antes de decodificar; `link.xml` y `CodePagesEncodingProvider` para Shift-JIS en IL2CPP. 2 scripts + tests EditMode | - Tests EditMode verdes para cada fila de codificación de GDD §5.2.<br>- Archivo >1 MB rechazado sin decodificar.<br>- Codificación no válida devuelve error, no excepción. | programador | hecha |
| T08 | Pantalla Cargar: botón, nombre sin extensión, KB, codificación ("UTF-8", "UTF-8 con BOM", "Shift-JIS"), vista previa desplazable de los primeros 3 000 caracteres con "… (N caracteres más)", errores (codificación, >1 MB). Usa T06 y T07. 2 scripts | - Cancelar no muestra mensaje.<br>- Errores de codificación y >1 MB muestran mensaje claro.<br>- No se guarda nada.<br>- Vista previa con kanji correcta y desplazable. | ux-ui, programador | hecha |
| T09 | Endurecimiento backlog S01: atlas de fuente (sampling ~48-64, padding 5-6, o atlas 2048), fallbacks globales TMP vacíos, quitar peso Medium, validación de `AppBootstrap` (prefab nulo, `ScreenId` duplicado: LogError + continue); corregir nota de `implementacion.md` T04 (el "1 error" es de `PipelineRuntimeBuildProcessor`, no Bee). 2 scripts (`FontSetup.cs`, `AppBootstrap.cs`) | - Compila sin errores.<br>- Atlas sin multiplicarse con kanji reales.<br>- Sin fallbacks globales Medium/Bold.<br>- Prefab nulo o ScreenId duplicado: log de error y la app arranca.<br>- Nota T04 corregida. | programador | hecha |
| T10 | Revisión en dispositivo: APK Development, Shift-JIS en IL2CPP (decisión documentada), Atrás real y gesto predictivo Android 13+ (si falla, `enableOnBackInvokedCallback`), memoria con Profiler, 5 cargas seguidas sin cierre | - Shift-JIS se decodifica bien en IL2CPP o decisión documentada.<br>- Atrás y gesto predictivo funcionan.<br>- Memoria medida y anotada.<br>- 5 cargas seguidas sin cierre.<br>- Resultado en `revision-<n>.md`. | revisor | pasa al usuario (comprobaciones en móvil, ver backlog) |

## Decisiones del usuario (2026-10-22, plan aprobado)
- Shift-JIS: sí (si falla en IL2CPP se quita).
- Límite de 1 MB: se mantiene.
- Vista previa de 3 000 caracteres: sí.
- Filtro text/plain sin rechazar por extensión: sí.

## Fuera de alcance (va al backlog)
- Build release con stripping, alinear menú de build, parametrizar dev/release (S08).
- Parser y persistencia (S03/S04).

## Riesgos
- Shift-JIS en IL2CPP (stripping de code pages): mitigado con `link.xml` y `CodePagesEncodingProvider`; verificar en T10.
- Compatibilidad del paquete NativeFilePicker con Unity 6000.6/Gradle. Plan B: plugin Java propio (ADR 0002).
- Drive y archivos no locales: el selector puede devolver rutas sin acceso directo o vacías.
