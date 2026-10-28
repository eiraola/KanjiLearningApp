# Retro S09 - Kanjis complicados por historia

Estado: hecho. Revision: 1 ronda, 1 bloqueante (test mal escrito, `_id` heredado de `ScreenView`) corregido. EditMode 234/234, PlayMode 21/21. Menores (2 instancias de repo, `StoryName`) al backlog; el de migracion con `Load` fallido se corrigio.
Pendiente (usuario): probar S08+S09 en movil con un solo APK.

## Tokens (`node tools/token-report.mjs --since 2026-10-26T13:44:00Z`)
Total: **18.160.465** (S08: 14.168.940, +28 %). Solo Sonnet.

| Agente | Turnos | Total |
|---|---|---|
| orquestador | 31 | 3.360.598 |
| a3c09737d0eda0f39 | 26 | 2.950.351 |
| a2398f0c75084f281 | 26 | 2.406.835 |
| a30025836773b89fb | 24 | 2.027.956 |
| aca1d9e147d430728 | 18 | 1.504.190 |
| ac21121cc704b055d | 16 | 1.449.885 |
| a198f4524214630e5 | 14 | 1.155.865 |
| a94fd4137d5cfb95a | 12 | 1.067.026 |
| abb617aa020f11ec5 | 13 | 1.046.310 |
| a118f52de3f657bbf | 25 | 871.618 |
| a00f6c0dd3a745378 | 3 | 220.192 |
| ae28a958dd5eed16e | 4 | 90.211 |
| a25f9f2eab6c16bc6 | 1 | 9.428 |

El informe no etiqueta rol ni subtarea; no se puede asignar cada id a T39b...T42c sin cruzar transcripciones. Turnos del informe incluyen llamadas de herramienta, no son comparables 1:1 con el tope de 15 del encargo. Segun implementacion.md, solo T42c llego al tope (quedo un fallo de PlayMode sin identificar, resuelto en revision).

## Que funciono
- Subtareas de una invocacion (<=3 scripts): T39b, T39c, T40b, T40c, T41, T42b cerraron sin incidencias.
- Arquitecto solo en T39a/T40a: migracion a prueba de fallos (marcador tras todo OK, global intacta).
- Una sola ronda de revision; el unico bloqueante fue de test, no de produccion.

## Que cambiar (medidas)
1. T42c (cuadricula + popup + prefabs + borrado de legado) debia dividirse en dos: T42c1 cuadricula, T42c2 popup Eliminar y limpieza. Evita llegar al tope y dejar tests sin diagnosticar.
2. El programador debe ejecutar PlayMode al final de cada subtarea con UI y anotar el test que falla; ahorra una vuelta de revisor (el revisor gasto 4 intentos de CLI en PlayMode).
3. Orquestador (3,4 M) es el mayor consumidor: encargos con solo ruta + criterio, sin pegar resultados largos de tests; y mejorar `token-report.mjs` para mostrar rol/subtarea por agente.
