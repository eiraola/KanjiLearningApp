Inicio: 2026-10-22T10:30:00Z

# Sprint S03 — Parser de bloques y persistencia JSON

## Objetivo
Parser de bloques `{Kanji: ; Reading: ; Meaning: ; Sentence: ; SentenceMeaning: ;}` + persistencia JSON de historias + `StoryLibrary`. Nada visible; 100 % cubierto con tests EditMode.
Referencias: `docs/diseno/GDD.md` secciones 5.1, 5.2 y 6; sección S03 de `docs/roadmap.md`.
Ya hecho en S02 (no se repite): decodificación UTF-8/Shift-JIS (`TextDecoder`) y límite de 1 MB.

## Requisitos del usuario antes de construir
Confirmar (recomendación entre paréntesis); sin respuesta se aplica la recomendación.
- P5 (bloqueante): errores de formato tolerantes + avisos (tolerante+avisos).
- P3: nombre duplicado al importar -> preguntar con 3 opciones Reemplazar / Conservar ambas / Cancelar (lógica en S03, UI en S04).
- Separador: `.` vale como separador en cualquier campo si no hay `:`.
- Nombre de historia = nombre de archivo sin extensión, comparación insensible a mayúsculas.
- `KanjiKey` = solo el campo Kanji.
- Escapes `\;` `\:` `\}` soportados (sí).
- Límite de 1 MB se mantiene.

## Tareas
| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T11a | Modelo (`Story`, `KanjiEntry`, `Segment`, `KanjiKey`) + `StoryParser`/`ParseReport`, casos válidos | - Ejemplo GDD 5.1 -> 1 `KanjiEntry` con 5 campos y segmentos texto/kanji/texto<br>- `;` final opcional; `Sentence.` = `Sentence:`; nombres insensibles a mayúsculas<br>- `：` `；` `　` y trim; orden libre de campos<br>- `10:30` en un valor se conserva; salto de línea en valor -> espacio<br>- Un test EditMode por caso | arquitecto, programador, revisor | hecha |
| T11b | Reglas de error del mismo parser (una fila de GDD 5.2 = un test) | - Kanji ausente/vacío -> texto con llaves + aviso; opcional vacío aceptado<br>- Duplicado -> gana el primero + aviso; campo desconocido -> ignorado + aviso<br>- `{` sin cierre no consume el resto + aviso; `}` suelta = texto + aviso<br>- Anidadas -> vale la interna<br>- Kanji repetido = N segmentos, 1 `KanjiKey`<br>- Sin bloques -> historia sin kanjis + aviso | programador, revisor | hecha |
| T12 | `JsonStoryRepository` + `AtomicFileWriter` + DTO con `version` | - Ida y vuelta idéntica en carpeta temporal; borrar funciona<br>- Campo extra o ausente tolerado<br>- Fallo de escritura simulado deja intacto el fichero anterior<br>- JSON corrupto -> error controlado (sin excepción sin capturar) | arquitecto, programador, revisor | hecha |
| T13 | `StoryLibrary` con repositorios falsos (solo lógica) | - Import; `Conflict` ante nombre duplicado (insensible a mayúsculas)<br>- Resolución Replace / KeepBoth ("nombre (2)") / Cancel<br>- List estable (mismo orden entre llamadas)<br>- Delete de inexistente no falla<br>- UI del conflicto fuera (S04) | programador, revisor | hecha |
| T14 | Fixture reproducible 50 000 caracteres / 200 kanjis + test de rendimiento | - Generador determinista (misma semilla -> mismo texto)<br>- Parseo del fixture < 1 s en EditMode<br>- 200 `KanjiKey` esperados | programador, revisor | hecha |

Orden: T11a -> T11b -> T12 -> T13 -> T14 (T12 puede ir en paralelo a T11b).
Sin ux-ui: no hay UI en este sprint.

## Medidas de retro aplicadas (S02 y S03)
- Un programador nuevo por tarea (contexto limpio); encargo = sprint, tarea y rangos de `arquitectura.md`/`implementacion.md` de su tarea, no ficheros enteros.
- Tareas <= 3 scripts (sin contar tests).
- Sin ventanas modales de Unity: el orquestador deja el Editor limpio antes; el programador no usa menús que abran diálogos.
- Una sola espera larga para tests; sin sondeos.
- Arquitecto solo en T11a y T12 (decisiones nuevas), tope 15 turnos, máx. 2 consultas del programador por tarea. T11b, T13, T14 siguen su `arquitectura.md`.
- Orquestador: no releer documentos ya resumidos; pasar solo ruta + tarea; Sonnet para el relevo si el usuario lo permite.

## Fuera de alcance (va al backlog)
- UI del diálogo de conflicto de nombre (S04).
- Pantallas de biblioteca/lectura; cualquier cosa visible.
- Menores de `docs/sprints/S02/revision-2.md` y T10 en móvil (ya en backlog).

## Riesgos
- P5/P3 sin confirmar cambian los tests de T11b/T13: confirmar antes de construir.
- Reglas de escape y separador `.` pueden chocar con valores reales (`10:30`, frases con `.`): el arquitecto fija precedencia en T11a.
- Rendimiento del parser (<1 s en 50 000 caracteres): evitar concatenación O(n²).
- Fallo de escritura atómica difícil de simular: el arquitecto define una abstracción de sistema de ficheros inyectable en T12.
