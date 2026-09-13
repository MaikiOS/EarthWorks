# EarthWorks 0.6.2 — Valheim 1.0 compatibility

[Русская версия](RELEASE_0.6.2_RU.md)

EarthWorks 0.6.2 was the compatibility patch for Valheim 1.0.7, Steam build 25185596, network version 39, and Unity 6000.0.75f1.

Valheim 1.0 added `float GetHoverOffset()` to `Hoverable`. Without it, `RoadProjectBoard` could not build its vtable and repeatedly raised `TypeLoadException`. Version 0.6.2 implemented the method while preserving the cloned vanilla `Sign.m_hoverOffset`. It also updated `Heightmap.Poke(int, bool)` and `TerrainComp.Save(bool)` calls.

Release verification completed with zero build warnings/errors, 24/24 geometry checks, API-contract audit, package inspection, and independent read-only review. Valheim itself was not launched.

EarthWorks 0.6.4 supersedes this build for current Valheim versions.
