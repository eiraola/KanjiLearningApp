Inicio: 2026-10-22T05:55:00Z

# Sprint S01 — Esqueleto Android + japonés legible

## Objetivo
Build Android instalable con Inicio (4 botones), navegación a pantallas vacías y fuente CJK correcta.

## Tareas
| Id | Tarea | Criterios de aceptación | Agentes | Estado |
|---|---|---|---|---|
| T01 | Estructura `_Project`, asmdefs, escena `Main`, Canvas 1080x1920, `AppBootstrap` | Compila sin errores; `Kanji.Core` sin refs a Unity | arquitecto, programador | hecha |
| T02 | `ScreenNavigator` + Home (4 botones) + pantallas vacías + Atrás/Salir | Navega a las 3 pantallas y vuelve; Atrás en Inicio y Salir cierran la app | ux-ui, programador | hecha |
| T03 | Fuente Noto Sans JP en TMP (dinámica + fallback), texto de prueba `。「」` y kanjis raros | Sin cuadrados en el Editor | programador | hecha |
| T04 | Config Android (IL2CPP, ARM64, vertical, safe area) y APK de desarrollo | APK generado | arquitecto, programador | hecha |
| T05 | Revisión de la build y del tamaño/memoria | Informe sin bloqueantes | revisor | hecha |

## Fuera de alcance
- Selector de archivos, parser, lector (S02+).

## Riesgos
- Paquete Pipeline aún no resuelto en el Editor: sin él no hay MCP en vivo.
