# EarthWorks

EarthWorks is a development build for planning persistent multiplayer roads on Valheim's real terrain grid. Version 0.6.2 updates the project board for Valheim 1.0.7, Steam build 25185596, network version 39, and Unity 6000.0.75f1.

## Current features

- Multi-point routes with straight segments, X-Spline, B-Spline, Bezier, and Corner control points.
- Unified editing of horizontal position, exact or automatic elevation, and independent left/right width.
- Four longitudinal road profiles and optional endpoint terrain-plane fitting.
- Bare-earth and paved surfaces with per-segment overrides.
- Current, Result, and Difference previews using the same sampled terrain plan as execution.
- Automatic persistent project board with setup, marking, clearing, earthworks, surfacing, and completion stages.
- Plan and Isometric project-editor cameras.
- Russian and English localization.

## Compatibility

- Valheim 1.0.7, Steam build 25185596, network version 39.
- BepInExPack Valheim 5.4.2350.
- Jotunn 2.29.2.
- The mod must be installed on the server and every participating client.

## Development status

EarthWorks 0.6.2 is a test build, not a completed Road Project 0.1 release. Runtime acceptance still requires the controlled `TerrainRamp_Lab` test followed by persistence, reconnect, second-client, and normal survival checks.

Developer instant construction is restricted to character `Test` in world `TerrainRamp_Lab`.

## Manual installation

Install BepInExPack Valheim and Jotunn, then copy `EarthWorks.dll` and `EarthWorks.Geometry.dll` into one folder under `BepInEx/plugins`.

---

# EarthWorks по-русски

EarthWorks позволяет проектировать многоточечные дороги, редактировать кривые, высоту и ширину, заранее видеть точный результат на terrain-сетке и выполнять проект по этапам через постоянную табличку.

Версия 0.6.2 совместима с Valheim 1.0.7 и исправляет загрузку проектной таблички после добавления `Hoverable.GetHoverOffset()` в Valheim 1.0.

Это тестовая сборка. Полная игровая проверка выполняется отдельно в лабораторном мире и не входит в статическую проверку ZIP.
