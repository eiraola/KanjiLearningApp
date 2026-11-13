Inicio: 2026-11-05T08:45:00Z

# Sprint S14 — Listas de palabras

Estado: hecho (pendiente de prueba en el móvil)

## Objetivo
Nueva sección «Listas» en el menú lateral, al nivel de las historias. Se crean listas con nombre, se abren para ver sus palabras y cada palabra abre su tarjeta con todos los datos. Desde los ajustes de una lista se añaden palabras pegando texto TSV (una línea = una palabra).

## Flujo
1. Menú lateral → **Listas** → pantalla con los nombres de las listas creadas (estado vacío si no hay ninguna). Arriba a la derecha, botón **+ Añadir lista** → popup de nombre (el mismo de las historias).
2. Tocar una lista → pantalla de la lista: sus palabras (palabra + lectura en cada fila). Arriba a la derecha, botón de ajustes (⋮) → menú.
3. Tocar una palabra → tarjeta con Word, Sentence, SentenceReading, Reading, Meaning y SentenceMeaning (campos vacíos ocultos).
4. Ajustes → **Añadir palabras** → popup con caja de texto multilínea, botón **Pegar** (rellena desde el portapapeles) y **Añadir**. Si hay repetidas, popup con su lista e Ignorar/Actualizar. Toast con el resultado («8 añadidas · 1 actualizada · 0 líneas no válidas»).

## Formato TSV
- Columnas separadas por tabulador. Primera línea = cabecera; las columnas se reconocen **por nombre** (sin distinguir mayúsculas, en cualquier orden): Word, Sentence, Reading, Meaning, SentenceMeaning, SentenceReading. Columnas que falten quedan vacías; columnas desconocidas se ignoran.
- Si la primera línea no es una cabecera (no contiene «Word»), se usa el orden Word, Sentence, Reading, Meaning, SentenceMeaning, SentenceReading.
- Líneas vacías se saltan; líneas sin Word cuentan como no válidas. Se aceptan `\r\n` y `\n`; se recortan espacios.

## Decisiones (D2-D4 aceptadas por el usuario)
- D1 (respondida por el usuario) Palabras repetidas (mismo Word ya en la lista o repetido dentro del texto pegado): antes de guardar sale un popup con la lista de repetidas y dos botones, **Ignorar** (se añaden solo las nuevas) y **Actualizar** (las repetidas sobrescriben sus datos). La decisión vale para todas las repetidas de ese pegado.
- D2 El menú de Ajustes muestra de momento solo «Añadir palabras»; las otras 3 opciones se añaden cuando se definan.
- D3 La tarjeta de palabra es solo de consulta: sin verde/rojo ni «Añadir a complicados», con botón Cerrar. Reutiliza el aspecto del popup de kanji y añade la fila SentenceReading.
- D4 Nombres de lista: obligatorios, máx. 60 caracteres, sin repetir (como las historias).
- Persistencia: un JSON por lista en `persistentDataPath/wordlists/` con escritura atómica (ADR 0004). Orden: listas por fecha de creación; palabras en orden de inserción.

## Tareas
| Id | Tarea | Criterios de aceptación | Estado |
|---|---|---|---|
| T57 | Core: modelo, parser TSV y repositorio | - `WordEntry`, `WordList`, `WordListTsvParser` (informe: añadidas/repetidas/no válidas), `WordListLibrary` (crear, listar, abrir, añadir palabras), `JsonWordListRepository`.<br>- Tests EditMode: cabecera en otro orden, sin SentenceReading, sin cabecera, líneas vacías/no válidas, repetidas, persistencia y nombre duplicado. | hecho |
| T58 | Pantallas Listas y Lista | - Opción «Listas» en el menú lateral.<br>- Pantalla de listas con «Añadir lista» y estado vacío; pantalla de lista con palabras, estado vacío y botón Ajustes. Atrás coherente. | hecho |
| T59 | Tarjeta de palabra | - Al tocar una palabra se ven todos sus campos (vacíos ocultos), con scroll si es larga; Cerrar, tocar fuera y Atrás la cierran. | hecho |
| T60 | Ajustes → Añadir palabras | - Menú de ajustes con «Añadir palabras».<br>- Si hay repetidas, popup con su lista e Ignorar/Actualizar.<br>- Popup con caja multilínea + Pegar + Añadir; la lista se actualiza y sale el toast con el recuento. Pegar el ejemplo del usuario añade 8 palabras. | hecho |
| T61 | Escena, tests, APK y retro | - Escena regenerada; EditMode y PlayMode en verde; `Builds/S14/KanjiLearning-S14-dev.apk` (build limpia, medida 1 de S13). | hecho |

## Fuera de alcance (al backlog)
- Las otras 3 opciones del menú de ajustes (sin definir).
- Renombrar/borrar listas, borrar o editar palabras, estudiar una lista, exportar.

## Reglas de ahorro
- Sin subagentes, como S12-S13: el código se escribe fuera y en el PC solo se compila, regenera la escena, se prueba y se construye.

Prueba del usuario: crear dos listas, pegar el ejemplo en una (8 palabras), ver la tarjeta de 二重丸 y de ～列目, volver a pegar (popup con las 8 repetidas; probar Ignorar y Actualizar), cerrar y reabrir la app y que todo siga.
