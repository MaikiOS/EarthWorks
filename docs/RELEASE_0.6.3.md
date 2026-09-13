# EarthWorks 0.6.3 — Valheim 1.0.12 and localization

[Русская версия](https://github.com/MaikiOS/EarthWorks/blob/main/docs/RELEASE_0.6.3_RU.md)

EarthWorks 0.6.3 is a compatibility and localization patch for Valheim 1.0.12 (Steam build 25253764, network version 40, Unity 6000.0.75f1) and Jotunn 2.30.0.

## Fixed and verified

- `RoadProjectBoard` still implements the Valheim 1.0 `Hoverable.GetHoverOffset()` contract and returns the cloned vanilla `Sign.m_hoverOffset` value.
- Direct calls, Harmony targets, and terrain reflection contracts pass the current assembly audit.
- The route piece uses vanilla `Misc` while Jotunn 2.30.0 custom build categories remain unavailable.
- The `Drawing` localization key mismatch was fixed.
- All editor, camera, planner, board, and terrain-operation text now routes through matched English/Russian dictionaries.
- The localization audit passes 252 keys and matching format placeholders.
- All 24 geometry checks pass; Release build completes with zero warnings and errors.

The test profile keeps the custom Jotunn 2.30.0 terrain-operation registration fix. No TerrainTools GPL implementation was copied; roadmap ideas will be independently implemented.

## Runtime status

Valheim was not launched. Project execution, persistence, reconnect, second-client, and survival acceptance remain the next controlled test.

## SHA-256

```text
EarthWorks.dll          366231DBD3651F60D805017CA70BC8222CACA939548D90A785C9B7124C0D165A
EarthWorks.Geometry.dll 02C8B477388C3168E62B48A3F975CB9ADC409CA8C231F226505C52969EC3A1FB
EarthWorks-0.6.3.zip    5282AB51C129B9B5DD1AB3B9A4C37354E97CCF9102D6CC416352E43324A4706B
```
