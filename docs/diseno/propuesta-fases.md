# Propuesta de fases — KanjiLearning

Cada fase termina en algo que el usuario puede **probar en el móvil Android**. Ordenadas por valor y por riesgo: los dos riesgos técnicos clave (selector nativo de archivos y texto japonés en TextMeshPro) se atacan en las fases 0 y 1, antes de construir funcionalidad encima. Los sprints (máx. 5 tareas) se detallarán con el planificador tras la confirmación del usuario. Referencia: `docs/diseno/GDD.md`.

## Fase 0 — Esqueleto en el móvil + japonés legible (riesgo: fuente CJK)
Resultado probable: APK instalable con la pantalla de Inicio (4 botones, navegación entre pantallas vacías, Salir funciona) y un texto de prueba en japonés que se ve sin cuadrados.
- Build Android funcionando (instalación en el móvil del usuario, orientación vertical, resolución adaptable).
- Fuente CJK en TextMeshPro: fuente con kanji/kana, atlas dinámico con fallback, comprobar `。「」` y kanjis poco comunes; vigilar tamaño del APK y memoria.
- Prueba del usuario: abre la app, navega, ve japonés correcto.
- Riesgo mitigado: toolchain Android y fuente.

## Fase 1 — Cargar un .txt desde el móvil (riesgo: selector nativo Android / SAF)
Resultado: en "Cargar historias", el botón abre el selector de archivos del sistema, el usuario elige un `.txt` y la app muestra su contenido (texto crudo) y su tamaño/codificación detectada.
- Integración nativa Android (Storage Access Framework, `ACTION_OPEN_DOCUMENT`, tipo `text/plain`) desde Unity mediante plugin Java/Kotlin o paquete existente; lectura vía URI de contenido (sin permisos de almacenamiento amplios).
- Detección UTF-8 / Shift-JIS.
- Prueba del usuario: elegir un archivo desde Descargas / Drive y ver el texto.
- Es el mayor riesgo técnico: si falla, se replantea (p. ej. ruta alternativa) antes de seguir. Debe validarse en dispositivo real, no solo en Editor (en Editor se usa un selector simulado).

## Fase 2 — Interpretar y guardar historias
Resultado: el archivo se analiza (bloques `{...}`), se guarda en el dispositivo y aparece en la lista de "Leer historias" con nombre y nº de kanjis; informe de carga con avisos.
- Parser tolerante según GDD 5.2 (con tests automáticos con muchos casos del GDD).
- Persistencia local (formato a decidir por el arquitecto) y gestión de nombre duplicado.
- Lista de historias con popup Leer / Eliminar (con confirmación de borrado). "Leer" aún sin lectura real.
- Prueba del usuario: cargar varios archivos, ver la lista, borrar con confirmación, cerrar y reabrir la app y que sigan ahí.

## Fase 3 — Leer la historia con kanjis pulsables (valor central)
Resultado: la historia se lee con scroll, kanjis en azul y popup vista 1 / vista 2 (Cerrar, Mostrar más).
- Texto largo scrolleable con segmentos pulsables mezclados con texto normal (riesgo de rendimiento con TMP en historias largas: probar con 50 000 caracteres).
- Popups de kanji vista 1 y 2; "Añadir a complicados" ya presente pero puede guardar aún sin pantalla propia.
- Prueba del usuario: leer una historia real de principio a fin.

## Fase 4 — Kanjis complicados
Resultado: botón "Añadir a complicados" funcional, pantalla "Kanjis complicados" con lista y popup con "Eliminar de la lista".
- Persistencia de la lista con copia completa de los datos; sin duplicados.
- Prueba del usuario: añadir desde la lectura, reabrir la app, ver y eliminar.

## Fase 5 — Estadística al cerrar la historia
Resultado: al cerrar la historia aparece el panel con revisados / acertados / % de acierto (definición del GDD 4, sujeta a confirmación).
- Registro de "revelados" por sesión, panel y lista de kanjis revisados.
- Prueba del usuario: leer pulsando algunos kanjis y comprobar que el porcentaje cuadra.

## Fase 6 — Pulido y robustez para uso diario
- Botón Atrás de Android coherente en todas las pantallas, estados vacíos, mensajes de error claros, objetivos táctiles de 48 dp.
- Rendimiento y memoria en gama media, icono y nombre de la app, build firmada (release).
- Opcional según respuestas del usuario: historial de estadísticas, tamaño de letra ajustable, "Añadir a complicados" desde el panel de estadística.

## Resumen de orden y por qué
| Fase | Valor para el usuario | Riesgo | Por qué en este orden |
|---|---|---|---|
| 0 | Bajo (ver la app) | Medio (fuente, build) | Desbloquea todo; el japonés es imprescindible |
| 1 | Medio | **Alto** (SAF) | Sin carga de archivos la app no sirve; mejor fallar pronto |
| 2 | Medio | Medio (parser, persistencia) | Base de datos de todo lo demás |
| 3 | **Alto** | Medio (rendimiento texto largo) | Es la razón de ser de la app |
| 4 | Medio | Bajo | Reutiliza popups de la fase 3 |
| 5 | Medio | Bajo (depende de decisión del usuario) | Requiere confirmar la definición de acierto |
| 6 | Medio | Bajo | Cierre |

Cada fase puede dividirse en 1–2 sprints de ≤5 tareas. Las fases 0 y 1 podrían fusionarse en un solo sprint si la compilación Android ya está configurada.
