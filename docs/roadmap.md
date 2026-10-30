# Roadmap — KanjiLearning

Fuentes: `docs/diseno/GDD.md`, `docs/diseno/propuesta-fases.md`, `docs/arquitectura/ARQUITECTURA.md`. Máx. 5 tareas/sprint, tarea <= 3 scripts. Agentes: ARQ arquitecto, UX ux-ui, PRG programador, REV revisor, DIS diseñador. Detalle de cada sprint: `docs/sprints/S<NN>/plan.md` (se crea tras aprobación). Los sprints de las fases 2-6 pueden reajustarse con la retro.

## Fase A — Spike en dispositivo (Fases 0+1 del diseñador)
Hito: APK en el móvil, Inicio navegable, japonés sin "tofu", selector de archivos real mostrando un .txt.

### S01 — Esqueleto Android + japonés legible
Objetivo: build Android instalable con Inicio y fuente CJK correcta.
- T01 Estructura `_Project`, asmdefs, escena `Main`, Canvas 1080x1920, `AppBootstrap`. Acept.: compila sin errores; Core sin refs Unity. (ARQ, PRG)
- T02 `ScreenNavigator` + Home (4 botones) + pantallas vacías + Atrás/Salir. Acept.: navega a las 3 pantallas y vuelve; Salir cierra. (UX, PRG)
- T03 Fuente Noto Sans JP en TMP (dinámica + fallback), texto de prueba `。「」` y kanjis raros. Acept.: sin cuadrados en PC. (PRG)
- T04 Config Android (IL2CPP, ARM64, vertical, safe area) y APK de desarrollo. Acept.: APK generado. (ARQ, PRG)
- T05 Revisión de la build y del tamaño/memoria. Acept.: informe sin bloqueantes. (REV)
Prueba del usuario: instala el APK, navega entre pantallas, ve japonés correcto, Salir funciona.
**REQUISITOS DEL USUARIO (antes de S01):** [R1] Android Build Support + OpenJDK + SDK/NDK para 6000.6.2f1 en Unity Hub. [R2] Móvil Android con opciones de desarrollador y depuración USB, cable y `adb` autorizado. [R3] Descargar Noto Sans JP (OFL, Google Fonts) y dejarla en `Assets/_Project/Fonts/`. [R4] Decidir si se quitan paquetes sin uso (opcional).
Preguntas del GDD: ninguna.

### S02 — Selector de archivos nativo y decodificación
Objetivo: elegir un .txt en el móvil y ver su texto, tamaño y codificación.
- T06 Puerto `IFilePicker` + `EditorFilePickerStub` + `NativeFilePickerAdapter` (paquete yasirkula). Acept.: en dispositivo abre el selector del sistema. (ARQ, PRG)
- T07 `TextDecoder` UTF-8/BOM/Shift-JIS con tests EditMode. Acept.: tests verdes. (PRG, REV)
- T08 Verificar SJIS en IL2CPP Android; si falla, decodificador propio o quitar SJIS. Acept.: decisión documentada. (ARQ, PRG)
- T09 Pantalla Cargar: botón Cargar, muestra texto crudo, tamaño, codificación y error si >1 MB. Acept.: funciona en móvil con archivo de Descargas/Drive. (UX, PRG)
- T10 Revisión en dispositivo. (REV)
Prueba del usuario: cargar un .txt UTF-8 y otro Shift-JIS y ver su contenido.
**REQUISITOS DEL USUARIO:** [R5] git instalado (si el paquete va por git URL). [R6] Preparar 2-3 .txt de prueba (UTF-8 y, si puede, Shift-JIS) en el móvil. [R7] Si SJIS no funciona en IL2CPP: decidir si se descarta SJIS.
Preguntas del GDD: ninguna.

## Fase B — Interpretar y guardar historias
Hito: cargar varios archivos, verlos en la lista, borrarlos, y que persistan al reabrir.

### S03 — Parser y persistencia (100 % EditMode, adelantable antes de S02)
Objetivo: modelo, parser tolerante y repositorio JSON probados por tests.
- T11 Modelo (`Story`, `KanjiEntry`, `Segment`, `KanjiKey`) + `StoryParser`/`ParseReport` con todos los casos de GDD 5.2. Acept.: tests de cada fila de 5.2 verdes. (ARQ, PRG, REV)
- T12 `JsonStoryRepository` + `AtomicFileWriter` + DTO versionado. Acept.: guardar/leer/borrar en carpeta temporal. (PRG)
- T13 `StoryLibrary` (Import, conflicto de nombre, List, Delete) con repos falsos. Acept.: tests de conflicto. (PRG)
- T14 Fixture de 50 000 caracteres/200 kanjis para S05. Acept.: fichero generado + test de parseo <1 s. (PRG)
Prueba del usuario: ninguna visible (solo informe de tests).
**REQUISITOS DEL USUARIO:** respuestas a P3 y P5 (ver tabla).

### S04 — Cargar y listar historias
Objetivo: flujo completo Cargar -> informe -> lista -> eliminar.
- T15 Integrar parser en pantalla Cargar con informe (nombre, nº kanjis, avisos). (UX, PRG)
- T16 Popup de conflicto de nombre (Choice 3 botones). (UX, PRG)
- T17 Pantalla lista de historias con estado vacío. (UX, PRG)
- T18 Popup StoryActions (Leer/Eliminar) + Confirm de borrado. (UX, PRG)
- T19 Revisión + prueba de persistencia al reabrir. (REV)
Prueba del usuario: cargar varios archivos, ver lista, borrar con confirmación, reabrir y que sigan.
**REQUISITOS DEL USUARIO:** P3 y P5 respondidas antes de S03/S04.

## Fase C — Lectura (valor central)
Hito: leer una historia real de principio a fin.

### S05 — Lector con kanjis pulsables (sprint completo, el más caro)
Objetivo: texto largo scrolleable, kanjis azules pulsables, rendimiento aceptable.
- T20 `StoryRichTextBuilder`: troceo por párrafos (<= ~8 000 car.), `<noparse>`, enlaces. Acept.: tests; 50 000 car. sin truncar. (ARQ, PRG)
- T21 `ReaderScreen` con ScrollRect y un TMP por trozo; scroll estable. Acept.: abre en <2 s en móvil. (UX, PRG)
- T22 `LinkClickHandler` + KanjiPopup vista 1 (kanji + frase, Cerrar). Acept.: pulsar abre popup sin perder scroll. (UX, PRG)
- T23 Vista 2 (Mostrar más: lectura, significado, traducción). (UX, PRG)
- T24 Revisión combinada S04+S05 (ficheros de S04 por separado) + prueba en móvil: T19b, rendimiento en gama media, pruebas pendientes de S04. (REV, usuario)
Prueba del usuario: leer la historia fixture/real, pulsar kanjis, ver vista 1 y 2 (vista 2 sin "Añadir a complicados", llega en S06).
**REQUISITOS DEL USUARIO:** [R-S05a] historia real larga .txt (≥ 30 000 car.); [R-S05b] móvil por USB con `adb` y tiempo para 20 importaciones + force-stop; [R-S05c] modelo del móvil.

### S06 — Kanjis complicados — HECHO (2026-10-26, prueba en móvil superada)
Objetivo: añadir/eliminar/listar kanjis complicados con persistencia.
- T25 `DifficultKanjiList` + repositorio JSON (copia completa, sin duplicados). (PRG, REV)
- T26 Botón "Añadir a complicados" / "Ya en complicados" + Toast. (UX, PRG)
- T27 Pantalla Kanjis complicados + popup modo Difficult con "Eliminar de la lista". (UX, PRG)
- T28 Revisión. (REV)
Prueba del usuario: añadir desde la lectura, reabrir, ver y eliminar.
**REQUISITOS DEL USUARIO:** P4 respondida: sin confirmación.

## Fase D — Estadística y cierre

### S07 — Estadística al cerrar la historia — HECHO (2026-10-26, prueba en móvil superada)
- T29 `ReadingSession` + `ReadingStats` (respuestas por KanjiKey) con tests. (PRG, REV)
- T30 `StatsScreen` (%, total, acertados, fallados, lista de fallados) y cierre de lectura. (UX, PRG)
- T31 Revisión. (REV)
Prueba del usuario: responder verde/rojo en algunos kanjis, cerrar y comprobar el %. **OK en móvil (2026-10-26).**
**REQUISITOS DEL USUARIO:** P1 y P2 respondidas antes de S07.

## Fase E — Gestión de historias y complicados por historia (GDD 2.2, 2.3, 2.5)
Hito: cargar con nombre y estadísticas, renombrar, y complicados separados por historia.

### S08 — Nombre y estadísticas al cargar historias — HECHO (2026-10-26, pendiente de prueba en móvil)
Objetivo: popup de carga con input de nombre y estadísticas; Editar nombre; duplicados bloqueados en rojo (sustituye el diálogo de S04).
- T35 Servicio de estadísticas de carga y validación de nombre (duplicado sin mayúsculas, vacío). (ARQ, PRG, REV)
- T36 Popup de carga (nombre, estadísticas, Aceptar/Cancelar) y retirada del diálogo de S04. (UX, PRG)
- T37 Editar nombre desde el popup de historia (renombrar conservando id). (UX, PRG)
- T38 Revisión. (REV)
Prueba del usuario: cargar con nombre propio, duplicado (rojo, Aceptar desactivado), vacío, cancelar, editar.
P6 cerrada: sin "palabras" en las estadísticas.

### S09 — Kanjis complicados por historia (pendiente)
Objetivo: lista de complicados por id de historia, selector, cuadrícula solo-kanji.
- T39 Modelo de lista por historia + repositorio + borrado en cascada (ARQ, PRG, REV)
- T40 Migración única de la lista global (primera historia que lo contenga; descartados con toast). (ARQ, PRG)
- T41 "Añadir a complicados" ligado a la historia en lectura. (PRG)
- T42 Selector de historias + cuadrícula solo-kanji + popup con Eliminar. (UX, PRG)
- T43 Revisión. (REV)
Prueba del usuario: añadir en dos historias, ver selector y cuadrícula, eliminar, borrar historia, migración con datos de S06.

### S10 — Pulido y release (antes S08)
- T32 Atrás coherente, estados vacíos, mensajes de error, 48 dp. (UX, PRG)
- T33 Icono, nombre, orientación, build release firmada. (PRG) — icono y splash adelantados a S12.
- T34 Rendimiento/memoria y revisión final. (REV)
Prueba del usuario: uso diario de la app firmada. Incluye los "Arreglos para el final del proyecto" de `docs/backlog.md`.
**REQUISITOS DEL USUARIO:** [R8] crear/guardar su keystore de firma (acción suya, no se introduce ninguna clave en chat). Opcionales (historial, tamaño de letra) al backlog salvo petición.

### S11 — Estudiar kanjis complicados — HECHO (2026-10-26, pendiente de prueba en móvil)
Objetivo: botón "Estudiar kanjis" en la lista de complicados de una historia (popups aleatorios con Resolver y verde/rojo, % final) y círculo de progreso 0..5 por kanji.
- T44 Progreso por kanji (Core + JSON). (PRG)
- T45 Sesión de estudio aleatoria + stats. (PRG)
- T46 Popup en modo estudio. (PRG)
- T47 Botón Estudiar + círculo por fila. (PRG)
- T48 Revisión, APK y retro. (REV)
Prueba del usuario: estudiar, ver % y círculos (llenar a 5; fallar no lo cambia).

### S12 — Nueva portada, icono y arranque sin logo (hecho)
Objetivo: la imagen KanjiBook (fondo blanco exterior transparente) como portada grande con «Leer historias» debajo, como icono de la app y sin logo de Unity al arrancar; resto de opciones en un menú lateral.
- T49 Recorte e icono (normal + adaptativo). (PRG)
- T50 Arranque sin logo de Unity. (PRG)
- T51 Portada nueva + menú lateral. (PRG)
- T52 Compilar, tests, APK y retro. (REV)
Prueba del usuario: icono, arranque, portada y menú lateral.

### S13 — Deslizar la tarjeta para responder verde/rojo (hecho)
Objetivo: en el popup (lectura y estudio de complicados), arrastrar la tarjeta a un lado pasado un umbral cuenta como el botón verde o rojo de ese lado; si no, vuelve.
- T53 Componente de deslizar (umbral, giro, tinte, animación). (PRG)
- T54 Conectar en lectura y estudio, mismo efecto que los botones. (PRG)
- T55 Tests PlayMode del gesto. (PRG)
- T56 Revisión, APK y retro. (REV)
Prueba del usuario: arrastrar poco vuelve; a cada lado cuenta como su botón; el scroll vertical sigue funcionando.

## Preguntas abiertas del GDD: cuándo responder
| Pregunta | Responder antes de | Recomendado |
|---|---|---|
| P5 Tolerancia del formato | S03 | tolerante + avisos |
| P3 Nombre duplicado | S04 | preguntar (3 opciones) |
| P4 Eliminar de complicados con/sin confirmación | S06 | **RESPONDIDA: sin confirmación** (toast breve) |
| P1 Definición de fallado/acierto | S07 | **RESPONDIDA:** verde = acierto, rojo = fallo; sin responder = acierto |
| P2 Guardar estadísticas | S07 | **RESPONDIDA: no se guardan** (solo se ven al cerrar) |
| P3 (revisada) Nombre duplicado | S08 | **RESPONDIDA:** Aceptar desactivado + mensaje rojo; sin diálogo |
| P6 "Palabras" en estadísticas de carga | S08 | **RESPONDIDA: se descartan**; solo kanjis, caracteres y avisos (>0) |
| P7 Migración de complicados globales | S09 | **RESPONDIDA:** primera historia que contenga el kanji; no asignables descartados con toast |
| P8 Borrar historia con complicados | S09 | **RESPONDIDA:** se borra también su lista |
| P9 Fallo en estudio y círculo | S11 | **RESPONDIDA:** el fallo no cambia el círculo |
| P10 Dirección del deslizamiento | S13 | **RESPONDIDA:** seguir los botones: derecha = verde, izquierda = rojo |

## Presupuesto de tokens (relativo)
S01 medio · S02 alto (nativo + dispositivo) · S03 medio · S04 medio · S05 alto · S06 medio · S07 bajo · S08 medio-bajo · S09 medio (migración y modelo) · S10 medio · S11 bajo (sin subagentes) · S12 bajo (sin subagentes) · S13 bajo (sin subagentes).
Reglas de ahorro:
- Encargos con solo nº de sprint, tarea y rutas; sin pegar documentos.
- Leer secciones concretas (offset/limit) en vez de ficheros enteros; no releer lo ya leído.
- Tareas pequeñas (<= 3 scripts); arquitecto solo en tareas con decisión nueva, no en las repetitivas.
- Revisor solo sobre lo cambiado; máx. 2 rondas; programador máx. 2 consultas al arquitecto por tarea.
- Planificador cierra cada sprint con `node tools/token-report.mjs` y 1-3 medidas en `retro.md`.
