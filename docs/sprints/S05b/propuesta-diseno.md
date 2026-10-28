# S05b - Propuesta de diseño (mini sprint entre S05 y S06)

GDD actualizado: §2.4 (cian), §3 (3.1 vista única, 3.4 apilados, 3.5 transiciones), §4 (nota S05b), §8 P1.

## Porqué
Adoptar el dark theme y simplificar el popup (sin "Mostrar más") prepara S06 (complicados) y S07 (estadística por verde/rojo) sin tocar aún su lógica.

## Tareas propuestas (4, la 4ª es revisión)
**T1 Dark theme y kanji en cian.** Como usuario quiero la app en tema oscuro y los kanjis legibles sobre él.
- Todas las pantallas y popups usan el dark theme importado; no queda ningún fondo claro.
- Kanji pulsable de la lectura en cian; contraste >= 4,5:1 con el fondo (verificado por ux-ui).
- El texto japonés sigue sin "tofu".

**T2 Popup de kanji de vista única con botones verde/rojo.**
- Al pulsar un kanji se ven de golpe kanji, frase, lectura, significado y traducción (campos vacíos ocultos).
- No existen "Mostrar más" ni "Cerrar"; hay un botón verde y uno rojo, >= 48 dp.
- Verde y rojo cierran el popup, el scroll de la lectura se conserva y no se guarda nada.
- Tocar fuera y Atrás cierran sin registrar nada [SUPOSICIÓN].

**T3 Transiciones y popups apilados.**
- Cambio de pantalla: fundido a negro 0,2 s + 0,2 s [SUPOSICIÓN: duraciones]. Popup: fundido de opacidad 0,15 s al abrir y al cerrar.
- Durante un fundido, toques y Atrás se ignoran (probar: doble toque rápido no abre dos pantallas ni popups).
- Con un popup sobre otro (p. ej. historia -> Eliminar), el de debajo no se ve ni recibe toques; al cerrar el de arriba reaparece intacto.
- Salir de la app sin fundido.

**T4 Revisión y regresión.** Los flujos de S01-S05 (cargar, listar, eliminar con diálogos, leer) siguen funcionando con tema, fundidos y popup nuevo.

## Suposiciones que necesitan confirmación del usuario
1. Verde = "lo sabía", rojo = "no lo sabía" (significado en S07).
2. Tocar fuera / Atrás cierran el popup sin registrar nada y cuentan como "no pulsado" (= acertado en S07).
3. Kanji nunca pulsado o cerrado sin botón cuenta como acertado (igual que antes).
4. "Añadir a complicados" (S06) será un botón secundario aparte, que no cierra el popup.
5. Duraciones: 0,2 s + 0,2 s entre pantallas, 0,15 s en popups.

## Fuera de alcance
Registro de estadísticas, "Añadir a complicados", pantalla de estadística, nuevos tonos de tema más allá del importado.
