# ADR 0003 — UI con uGUI + TextMeshPro; fuente Noto Sans JP con atlas dinámico

Estado: aceptado · Fecha: 2026-10-22

## Contexto
La pantalla clave es un texto japonés largo (hasta 50 000 caracteres, 200 kanjis) desplazable, con palabras pulsables en azul, scroll fluido y apertura < 2 s en gama media (GDD §2.4, §7). Sin "tofu" en ninguna pantalla.

## Decisión
- **uGUI 2.6 + TextMeshPro** (incluido en `com.unity.ugui`), `ScrollRect` + `VerticalLayoutGroup` + `ContentSizeFitter`.
- Kanjis pulsables con `<link="k{idx}"><color=#1E66F5>…</color></link>`; detección en `IPointerClickHandler` con `TMP_TextUtilities.FindIntersectingLink(text, eventData.position, null)` (cámara `null` en Overlay). Distinguir toque de arrastre (no abrir popup si hubo drag del `ScrollRect`).
- **Troceo obligatorio:** un `TextMeshProUGUI` por bloque de párrafos de ≤ ~8 000 caracteres (límite de malla ~65 k vértices; además el relayout de un texto gigante es lento). Si el rendimiento no basta: virtualización (activar solo trozos visibles), sin cambiar el modelo.
- Texto del usuario dentro de `<noparse>…</noparse>` para que `<`/`>` del `.txt` no se interpreten como etiquetas.
- **Fuente:** Noto Sans JP (OFL) Regular (+ Bold si UX lo pide). `TMP_FontAsset` **dinámico** (atlas 2048², SDFAA, padding 9, *Multi Atlas Textures* activado) como fuente por defecto en *TMP Settings*; un asset estático pequeño con ASCII + kana + `。「」、` para el primer frame es opcional. Fallback: cadena con el dinámico y, si hiciera falta, Noto Sans (latino). Activar *Clear Dynamic Data on Build* para no versionar atlas sucios.
- Coste estimado: +5–9 MB de APK por el `.ttf/.otf`; aceptable.

## Alternativas
- **UI Toolkit:** en Unity 6 soporta `<link>` y eventos de link y comparte font assets, pero para móvil con scroll inercial, safe area y el ecosistema de ejemplos/skills del equipo, uGUI es más maduro y predecible; además el `CLAUDE.md` ya fija uGUI/TMP. Descartado por ahora.
- **Un botón por kanji intercalado en layouts:** imposible respetar el flujo de texto japonés. Descartado.
- **Atlas estático con todos los kanji:** atlas enormes (decenas de MB) y aún así incompleto. Descartado.

## Consecuencias
- Importar *TMP Essential Resources* al empezar.
- El lector necesita una prueba de rendimiento en dispositivo con el fixture de 50 000 caracteres (Fase 3).
- Primer uso de kanjis nuevos rasteriza glifos en el atlas dinámico (pequeño hitch); mitigable pre-poblando con los kanjis de la historia al abrirla (`TryAddCharacters`).
