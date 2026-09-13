# Changelog

## 0.6.2

- Added the Valheim 1.0 `Hoverable.GetHoverOffset()` contract to `RoadProjectBoard`.
- Preserved the cloned vanilla sign's configured hover offset when replacing its `Sign` component with the EarthWorks project-board component.
- Updated immediate Heightmap refresh calls for Valheim 1.0's `Poke(int delayed, bool paintOnly)` signature.
- Updated reflective terrain persistence calls for Valheim 1.0's `TerrainComp.Save(bool)` signature.
- Rebuilt against Valheim 1.0.7 build 25185596, BepInExPack 5.4.2350, and Jotunn 2.29.2.
- Updated plugin and geometry assembly versions to 0.6.2.
- No terrain, project-state, editor, or construction behavior was intentionally changed.

## 0.6.1

- Combined route geometry, elevation, and width into one direct-manipulation editor stage.
- Added X-Spline, B-Spline, Bezier, and Corner controls, independent side widths, four longitudinal profiles, endpoint fitting, exact review, and staged laboratory construction.
