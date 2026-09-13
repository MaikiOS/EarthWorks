# Changelog

[Русская версия](CHANGELOG_RU.md)

## 0.6.3

- Rebuilt and audited against Valheim 1.0.12, Steam build 25253764, network version 40, Unity 6000.0.75f1, BepInExPack 5.4.2350, and Jotunn 2.30.0.
- Preserved the local Jotunn 2.30.0 terrain-operation registration fix in the isolated test profile.
- Moved the route tool to the vanilla `Misc` category until Jotunn restores Valheim 1.0 custom-category support.
- Fixed `Drawing` state and control localization resolving nonexistent `state_drawing`/`controls_drawing` tokens.
- Added English fallback for unresolved Valheim localization tokens.
- Localized the full project editor, camera safety messages, planner errors, board creation errors, and terrain-operation results.
- Added an automated localization audit: 252-key EN/RU parity, placeholder parity, literal token validation, dynamic state/stage coverage, and mixed-language detection.
- Added English-primary documentation with Russian counterparts and expanded the roadmap with independently implementable TerrainTools-inspired ideas.

## 0.6.2

- Added Valheim 1.0 `Hoverable.GetHoverOffset()` to `RoadProjectBoard`.
- Preserved the vanilla board hover offset, updated `Heightmap.Poke(int, bool)` and `TerrainComp.Save(bool)` calls, and audited Valheim 1.0.7 API contracts.

## 0.6.1

- Unified route geometry, elevation, and width editing.
- Added X-Spline, B-Spline, Bezier, Corner, independent side widths, four longitudinal profiles, endpoint fitting, exact preview, and staged laboratory execution.
