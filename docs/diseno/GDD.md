# GDD — KanjiLearning

Fuente de verdad de la petición: `docs/diseno/definicion-usuario.md`. Este documento la concreta. Lo marcado **[SUPOSICIÓN]** está pendiente de confirmar por el usuario (ver "Preguntas abiertas").

## 1. Objetivo y alcance
Herramienta móvil para **reforzar kanjis dentro de textos propios**: el usuario carga historias en `.txt` donde ciertos kanjis están anotados con lectura y significado; lee la historia y consulta esos kanjis al tocarlos. Al cerrar la historia ve cuántos tuvo que revisar.

- Plataforma: **Android primero** (iOS fuera de alcance).
- **Solo lectura, offline**: sin cuentas, sin red, sin servidor. Todo se guarda en el dispositivo.
- Público: persona que estudia japonés y lee textos con kanjis que aún no domina.
- Fuera de alcance (por ahora): repaso espaciado, edición de historias en la app, exportar/compartir, iOS, estadísticas históricas.

## 2. Mapa de pantallas
```
Inicio ─┬─ Cargar historias   (botón "Cargar" -> selector de archivos .txt)
        ├─ Leer historias     (lista de historias) ── Lectura de historia ── Resumen/estadística
        ├─ Kanjis complicados (lista de kanjis guardados)
        └─ Salir              (cierra la app)
```

### 2.1 Inicio
4 botones, en este orden: **Cargar historias**, **Leer historias**, **Kanjis complicados**, **Salir**.
- Salir cierra la app inmediatamente (sin confirmación). En Android, el botón Atrás en Inicio también la cierra.
- Criterio: cada botón lleva a su pantalla en <1 s.

### 2.2 Cargar historias
- Un botón **Cargar**. Al pulsarlo se abre el selector de archivos del móvil filtrado a `.txt` (si el sistema no permite filtrar, se muestra todo y se valida al leer).
- Tras elegir un archivo, la app lo lee, lo analiza y muestra un resultado: "Historia 'X' cargada: N kanjis" o un error comprensible (ver 5.3).
- Se permite cargar varios archivos seguidos (uno por selección).
- **Cambio S08 (sustituye el resultado directo y el diálogo "Reemplazar / Conservar ambas / Cancelar" de S04, que se elimina):** tras elegir y analizar el archivo se abre un **popup de carga** con:
  - **Input de nombre**: placeholder = nombre del archivo sin extensión. Si queda vacío (o solo espacios **[SUPOSICIÓN]**, se recorta), se usa el nombre del archivo.
  - **Estadísticas** (solo lectura): nº de kanjis (bloques válidos, como 2.3), nº de caracteres y nº de avisos del parser (**solo si > 0**). **[SUPOSICIÓN]** "caracteres" = caracteres del texto visible (con cada bloque `{...}` sustituido por su Kanji), sin contar saltos de línea ni espacios.
  - **Aceptar**: guarda la historia con el nombre resultante y cierra el popup. **Cancelar** (y Atrás): cancela la carga, no se guarda nada y no se muestra mensaje. El popup no se cierra tocando fuera.
  - **Nombre duplicado**: si el nombre (ya recortado, comparación sin distinguir mayúsculas **[SUPOSICIÓN]**) coincide con el de una historia existente, **Aceptar queda desactivado** y aparece un mensaje **en rojo** bajo el input: "Ya existe una historia llamada 'X'. Elige otro nombre." La comprobación se actualiza al teclear (< 0,2 s) y también aplica al nombre por defecto (archivo vacío de nombre).
  - Los errores fatales (5.3) no abren el popup: se muestra el error y no se crea historia.
- Se pueden cargar varios archivos seguidos; no hay "Reemplazar" ni "Conservar ambas": para sustituir una historia hay que eliminarla antes o usar otro nombre.
- Atrás vuelve a Inicio.

### 2.3 Leer historias (lista)
- Lista con una fila por historia (nombre y nº de kanjis). Lista vacía: mensaje "No hay historias. Ve a Cargar historias".
- Tocar una fila abre el **popup de historia**: botones **Leer**, **Editar** (S08), **Eliminar**, y cerrar (tocar fuera / Atrás).
- **Editar (S08)**: permite cambiar el nombre de la historia con un input precargado con el nombre actual, **Aceptar** y **Cancelar**. Misma validación que en la carga: vacío = Aceptar desactivado **[SUPOSICIÓN]** (aquí no hay nombre de archivo de reserva); duplicado con **otra** historia = Aceptar desactivado y mensaje rojo; el propio nombre sin cambios no cuenta como duplicado. Aceptar actualiza la lista y el popup de historia (< 1 s); no cambia el orden de la lista ni el contenido. Su lista de complicados (2.5) se conserva, pues va asociada al id y no al nombre.
- **Eliminar** abre confirmación: "¿Eliminar 'X'? No se puede deshacer." con **Cancelar** (por defecto) y **Eliminar**. Solo tras confirmar se borra y la lista se actualiza.
- Orden de la lista: la más recientemente cargada primero. Al volver a la lista tras eliminar, se muestra "Historia 'X' eliminada".
- Si la historia no se puede leer del disco (fichero dañado), la lista no falla: esa fila se omite y se avisa "N historias no se pudieron leer".
- ~~Conservar ambas (" (2)")~~: retirado en S08 (ver 2.2, regla de duplicados).
- **"N kanjis"** (fila, popup, Leer, mensaje de carga) = nº de bloques válidos de la historia (igual que el informe 5.3), no kanjis distintos. N=1 -> "1 kanji"; N=0 -> "0 kanjis".
- Diálogos: el de eliminar y el de nombre duplicado **no** se cierran tocando fuera; Atrás = Cancelar. El popup de historia sí se cierra tocando fuera/Atrás. Tras Cancelar en eliminar, el popup de historia sigue abierto.
- Textos de error/estado (aceptados en S04): "No se pudo cargar la biblioteca." (fallo inesperado al listar), "No se pudo eliminar la historia." (la fila permanece), "No se pudo guardar la historia." (al cargar), "No se pudo abrir la historia." (reservado para S05, cuando Leer relea el disco). Título del diálogo de duplicado: "Nombre repetido", mensaje "Ya existe una historia llamada 'X'.". Nota del placeholder de Leer: "La lectura llegará pronto.".
- (S08) El diálogo de duplicado de S04 desaparece; lo sustituye el mensaje rojo en línea de 2.2/Editar (el texto "Nombre repetido" ya no se usa).
- **Cambio S09:** **Eliminar** una historia **borra también su lista de complicados** (2.5). Se mantiene "Historia 'X' eliminada"; el texto de confirmación de eliminar debe avisar si la historia tiene complicados: "También se borrarán sus N kanjis complicados." (solo si N > 0).

### 2.4 Lectura de historia
- Texto completo de la historia, **desplazable** verticalmente. Respeta saltos de línea del original.
- Cada bloque `{...}` se sustituye por el valor de **Kanji**, en **cian** (desde S05b; antes azul) y pulsable. El resto, texto normal.
- Pulsar un kanji abre el **popup de kanji** (sección 3). La lectura sigue en la misma posición de scroll al cerrarlo.
- Salir de la lectura (botón Cerrar historia / Atrás) muestra la **estadística** (sección 4) y luego vuelve a la lista.
- La posición de scroll no se guarda entre sesiones (cada lectura empieza arriba) **[SUPOSICIÓN]**.
- **Alcance en S05 (aclaración):** el lector relee la historia del disco al abrirla; si falla, muestra "No se pudo abrir la historia." y vuelve a la lista. Cerrar historia / Atrás **vuelve directamente a la lista** (sin estadística; llega en S07). En el popup de kanji (vistas 1 y 2) **no aparece** el botón "Añadir a complicados" (llega en S06) y "Mostrar más" **aún no registra** el kanji como revelado (S07). Atrás con popup abierto cierra solo el popup; tocar fuera también lo cierra. En vista 2 las líneas de campos vacíos (Reading, Meaning, SentenceMeaning) se ocultan. Un kanji cuyo `Kanji` coincide en varias apariciones abre cada vez su propio bloque (con su propia frase).
- **Párrafos muy largos (aclaración S05):** si un párrafo sin saltos de línea supera el tamaño máximo de trozo, se acepta que el lector lo corte (preferentemente tras `。`, `！`, `？`, `、` o espacio) y que el resto aparezca en una línea nueva. Condición: no se pierde, duplica ni reordena ningún carácter y ningún kanji pulsable se parte.

### 2.5 Kanjis complicados (lista)
- **Cambio S09 (sustituye la lista global):** los complicados son **una lista por historia**, asociada al **id** de la historia. "Añadir a complicados" (3.1) añade el kanji a la lista de la historia que se está leyendo.
- Un mismo kanji (mismos Kanji + Reading) no se duplica **dentro de la misma historia**. Si se marca en dos historias, queda en **ambas listas por separado** (eliminarlo de una no afecta a la otra).
- **Pantalla 1, selector de historias:** "Kanjis complicados" abre primero una lista con **solo las historias con >= 1 complicado**; cada fila muestra **nombre y nº de complicados** (N=1 -> "1 complicado"). Orden: igual que 2.3 **[SUPOSICIÓN]**. Si ninguna historia tiene complicados: "Aún no has añadido kanjis". Atrás vuelve a Inicio.
- **Pantalla 2, cuadrícula:** al tocar una historia se ve una **cuadrícula de kanjis grandes con solo el kanji** (sin lectura ni significado). Atrás vuelve al selector. Si al eliminar el último kanji la lista queda vacía, se vuelve al selector (que ya no muestra esa historia) **[SUPOSICIÓN]**.
- **Popup:** al tocar un kanji de la cuadrícula se abre el popup actual con todos los datos y **Eliminar de la lista** (3.3). Tras eliminar, el popup se cierra y la cuadrícula se actualiza.
- **Migración (una sola vez, al arrancar):** la lista global existente se reparte: cada kanji se asigna a la **primera historia cuyo texto lo contenga** (orden de la lista 2.3 **[SUPOSICIÓN]**: la más recientemente cargada primero; el arquitecto puede fijar otro orden estable); los que no encajen en ninguna se **descartan** y se muestra un **toast**: "N kanjis complicados no se pudieron asignar y se descartaron." (solo si N > 0). La lista global se elimina tras migrar y la migración no se repite; si falla a medias no se pierde la lista global original **[SUPOSICIÓN]**. Criterio: sin pérdida salvo los no asignables; ejecución < 2 s con 500 complicados y 50 historias **[SUPOSICIÓN]**.

## 3. Popups de kanji
**Cambio S05b:** ya no hay vista 1 / vista 2 ni "Mostrar más". Las secciones 3.1 y 3.2 anteriores quedan sustituidas por 3.1 (vista única).

### 3.1 Popup de kanji (vista única, desde S05b)
- Al pulsar un kanji se muestra **todo de golpe**: kanji en grande, frase (Sentence), lectura (Reading), significado (Meaning) y traducción de la frase (SentenceMeaning). Los campos vacíos se **ocultan** (sin hueco ni relleno).
- Abajo, **dos botones**: **verde** y **rojo**. En S05b ambos **solo cierran** el popup y no registran nada. Significado futuro **[SUPOSICIÓN]**: verde = "lo sabía", rojo = "no lo sabía" (alimentan estadísticas en S07).
- Textos/iconos de los botones: los decide ux-ui; deben tener etiqueta accesible distinta y tamaño >= 48 dp.
- **Tocar fuera y Atrás** cierran el popup **sin registrar nada** (equivale a "no pulsado", ver 4) **[SUPOSICIÓN]**.
- **"Añadir a complicados" (S06)** **[SUPOSICIÓN]**: botón secundario (de texto o icono, visualmente distinto de verde/rojo) situado sobre ellos; **no cierra** el popup; tras añadir muestra "Añadido" y pasa a "Ya en complicados" (desactivado). Es independiente de verde/rojo.

### 3.2 (retirada)
Sin vista 2. "Mostrar más" ya no existe y no marca nada como revelado.

### 3.3 Popup en la lista de complicados
Igual que 3.1 y con **todos los datos** (la cuadrícula de 2.5 solo muestra el kanji; el popup muestra el resto). En S06 el botón secundario "Añadir a complicados" se **sustituye** por **Eliminar de la lista** (sin confirmación extra **[SUPOSICIÓN]**, por ser reversible volviendo a añadirlo desde su historia). Desde S09 elimina **solo de la lista de la historia abierta**.
- Datos para el popup: cada lista de complicados guarda una **copia completa** del kanji (los 5 campos) de modo que el popup no depende de releer la historia.

### 3.4 Popups apilados
- Si se abre un popup/diálogo sobre otro, el **anterior se oculta** (no visible) y **no es interactuable** mientras haya otro encima. Solo el de arriba recibe toques y Atrás.
- Al cerrar el de arriba, el anterior **reaparece** con su estado intacto (p. ej. el popup de historia tras Cancelar en Eliminar).
- Criterio: con dos popups abiertos, un toque en la zona del de debajo no dispara ninguna acción suya.

### 3.5 Transiciones
- **Cambio de pantalla**: fundido a negro. Salida 0,2 s hasta negro total, cambio de pantalla, entrada 0,2 s desde negro (total 0,4 s) **[SUPOSICIÓN: duraciones]**.
- **Abrir/cerrar popup**: fundido de opacidad (0 a 1 al abrir, 1 a 0 al cerrar) de 0,15 s; el resto de la pantalla no se oscurece con fundido a negro.
- **Durante cualquier fundido la entrada táctil y Atrás se ignoran** (sin acciones encoladas). Un toque durante el fundido no abre ni cierra nada.
- Los fundidos no retrasan la acción más de su duración; Salir de la app no lleva fundido.
- Tema visual: **dark theme** a partir de S05b; el kanji pulsable en la lectura pasa de azul a **cian** (tono exacto y contraste >= 4,5:1 sobre el fondo oscuro: ux-ui).
- Datos para el popup: la lista de complicados guarda una **copia completa** del kanji (los 5 campos), de modo que funciona aunque la historia original se haya borrado.

## 4. Estadística al cerrar la historia
Petición del usuario: "cuántos kanjis tuvimos que revisar con lectura y todo para saber el porcentaje de acierto".

**Diseño final (S07, probado en móvil el 2026-10-26).** "Mostrar más" ya no existe; las respuestas salen de los botones de la tarjeta del kanji (3.1).

### Definiciones
- **Kanji de la historia**: kanjis distintos (por `KanjiKey`). Si el mismo kanji aparece 5 veces, cuenta 1.
- **Verde = acierto, rojo = fallo.** Si se responde varias veces el mismo kanji, cuenta la **última** respuesta.
- **Sin responder = acierto**: un kanji nunca pulsado, o abierto y cerrado sin botón (tocar fuera / Atrás), cuenta como acertado.
- **Acierto %** = acertados / total × 100, redondeado al entero.
- La sesión empieza al abrir la historia y termina al cerrarla; al reabrir empieza de cero.

### Presentación (pantalla Resultados)
- % de acierto, total, acertados (verde) y fallados (rojo).
- Lista de kanjis fallados (kanji + lectura), o "¡Ningún fallo!".
- **Aceptar** (o Atrás) vuelve a la lista de historias.
- Historia sin kanjis, o cerrada mientras carga: vuelve directamente a la lista.
- Las estadísticas **no se guardan**.

## 5. Formato del archivo `.txt`
### 5.1 Especificación
Texto plano. Cualquier trozo envuelto en `{ ... }` es un bloque de kanji:
```
{Kanji: 勉強; Reading: べんきょう; Meaning: estudio; Sentence: 毎日日本語を勉強します。; SentenceMeaning: Estudio japonés cada día.;}
```
- 5 campos: `Kanji`, `Reading`, `Meaning`, `Sentence`, `SentenceMeaning`.
- Separador campo/valor: `:`. Separador entre campos: `;`. El `;` final es opcional.
- En la lectura, el bloque se reemplaza por el valor de `Kanji` (en azul). El texto fuera de llaves se muestra tal cual.

### 5.2 Ambigüedades y comportamiento por defecto
| Caso | Comportamiento por defecto |
|---|---|
| El usuario escribió `Sentence.` (con punto) en su ejemplo | Se acepta `Sentence:` y también `Sentence.` como nombre del campo (típico error de tecleo). |
| Mayúsculas/minúsculas en nombres de campo | Insensible (`kanji`, `KANJI`). |
| Espacios y tabuladores alrededor de `:`, `;`, llaves | Se ignoran (trim). Espacios internos del valor se conservan. Se aceptan espacios de ancho completo (`　`) y `：` `；` japoneses como separadores. |
| Orden de campos distinto | Válido; se identifican por nombre. |
| Campos obligatorios | Solo `Kanji` es obligatorio. Si falta `Kanji` o está vacío: el bloque se descarta y el texto se deja tal cual como texto normal (con llaves) y se cuenta como aviso. |
| Campos opcionales vacíos o ausentes | Aceptados; el popup oculta la línea vacía (o muestra "—"). |
| Valor que contiene `;` o `:` | No soportado en esta versión. Si hace falta, el usuario puede escapar con `\;` y `\:` **[SUPOSICIÓN]**. La URL/hora `10:30` en un valor funciona porque solo el primer `:` del campo separa nombre de valor. |
| Llave sin cerrar `{` o sin abrir `}` | La llave suelta se trata como texto normal; aviso en el informe de carga. Un `{` sin cierre no "se come" el resto del archivo: si no hay `}` antes del siguiente `{`, se descarta ese `{`. |
| Llaves anidadas `{ {` | Se toma el bloque más interno bien formado; el exterior es texto. |
| Salto de línea dentro del bloque | Permitido; se normaliza a un espacio dentro de cada valor. |
| Campo repetido dentro del bloque | Vale el primero; aviso. |
| Campo desconocido | Ignorado; aviso. |
| Mismo kanji repetido en la historia | Cada ocurrencia es pulsable; para la estadística cuenta como uno (4). |
| Codificación | Se intenta **UTF-8** (con o sin BOM). Si hay bytes inválidos se intenta **Shift-JIS**. Si ninguna decodifica bien, error "Codificación no soportada, guarda el archivo como UTF-8". Saltos de línea `\r\n`/`\n` aceptados. |
| Archivo vacío o sin bloques | Se carga igualmente como historia sin kanjis, con aviso "No se encontraron kanjis". |
| Archivo muy grande | Límite sugerido 1 MB **[SUPOSICIÓN]**; por encima, error claro. |

### 5.3 Informe de carga
Al terminar la carga se muestra: nombre, nº de kanjis válidos, nº de avisos (bloques descartados, campos duplicados…). Los errores fatales (no se pudo leer el archivo, codificación inválida) no crean historia.

## 6. Datos guardados en el dispositivo
- Historias cargadas (texto + bloques ya analizados) y lista de kanjis complicados.
- El formato interno (JSON u otro) lo decide el arquitecto; requisito de diseño: sobrevivir al cierre de la app y a actualizaciones, y soportar crecer (nuevos campos) sin perder datos.
- No se guarda el archivo original ni se accede de nuevo a él tras la carga.

## 7. Requisitos no funcionales (comprobables)
- Abrir una historia de 50 000 caracteres y 200 kanjis en <2 s en un móvil de gama media.
- Scroll fluido (sin saltos visibles) en esa misma historia.
- El japonés se muestra correctamente (kanji, kana, signos `。「」`) sin cuadrados "tofu" en ninguna pantalla.
- Todo funciona sin conexión; la app no pide permisos de red.
- Objetivos táctiles de al menos 48 dp.
- Orientación: vertical **[SUPOSICIÓN]**.

## 8. Preguntas abiertas para el usuario
1. **Definición de "fallado" y "acierto" (sección 4).** **RESPONDIDA (S07):** verde = acierto, rojo = fallo, sin responder = acierto.
2. **¿Se guardan las estadísticas?** **RESPONDIDA (S07):** no se guardan; solo se ven al cerrar. Historial por historia, posible mejora futura.
3. **Historias con el mismo nombre al cargar.** **RESPONDIDA (S08):** no se permite; Aceptar desactivado con mensaje rojo y el usuario elige otro nombre (2.2).
4. **Eliminar de la lista de complicados: ¿con confirmación?** **Recomendado:** sin confirmación (es reversible y más rápido); con confirmación si prefieres ser consistente con borrar historias.
5. **Tolerancia del formato.** ¿Aceptamos las variantes (`Sentence.`, `：`/`；` japoneses, mayúsculas, campos vacíos) y descartamos con aviso los bloques mal formados, o prefieres rechazar el archivo entero si hay algún error? **Recomendado:** ser tolerante y avisar (5.2).
