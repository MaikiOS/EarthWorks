# EarthWorks

[Русская версия](README_RU.md)

![EarthWorks road planning](docs/images/earthworks-cover.png)

*Valheim-inspired concept art; not a gameplay screenshot.*

EarthWorks turns Valheim road building into a controlled, persistent project. Players define a route, roadbed shape, elevation, independent left/right width and surface, inspect the exact terrain-grid result, then execute the work through a persistent project board.

Current version: **0.6.3**. Built and statically verified against Valheim **1.0.12**, Steam build **25253764**, network version **40**, Unity **6000.0.75f1**, BepInExPack Valheim **5.4.2350**, and Jotunn **2.30.0** with the EarthWorks/TerrainTools terrain-operation registration fix.

> 0.6.3 is a verified development build. Valheim was not launched while preparing it; full single-player and multiplayer runtime acceptance remains pending.

## Features available now

### Route design

- Two or more control points.
- Straight segments, X-Spline, B-Spline, manual Bezier curves, and sharp Corner points.
- Per-segment straight/curved override.
- Insert or remove control points; preserve an unfinished draft while switching tools.

### Unified project editor

![Route editor concept](docs/images/route-editor-concept.png)

*Direction concept; the current build remains the source of truth for UI behavior.*

- `F7` editor with Plan and Isometric cameras, pan, orbit, zoom, and framing.
- Direct terrain-space point dragging, exact elevation anchors, independent side widths, Bezier handles, and X-Spline smoothing.
- Current, Result, and Difference previews.

### Terrain, surfaces, and execution

- Four longitudinal profiles and optional A/B fitting to original-terrain planes.
- Cut, fill, shoulder, grade, terrain-range, water, ward, and affected-vertex validation.
- Bare-earth and paved surfaces, globally or per segment.
- Persistent networked board with setup, marking, clearing, earthworks, surfacing, and completion stages.
- Terrain changes use Valheim `TerrainComp` and `Heightmap`; drafts never modify the world before confirmation.

### Localization

- Full English and Russian token sets: **252/252**.
- Automated key, placeholder, literal-use, dynamic-state, and dynamic-stage checks.
- English fallback for unresolved Valheim localization tokens.
- Editor, camera, validation, board, and terrain-operation messages all use the same localization layer.

## 0.6.3 compatibility changes

- Reverified `Hoverable.GetHoverOffset()`, vanilla `Sign.m_hoverOffset`, Harmony targets, and all direct/reflection contracts used by EarthWorks on Valheim 1.0.12.
- Built against Jotunn 2.30.0. The isolated test profile retains the local terrain-operation registration fix instead of replacing it with the official binary.
- Temporarily places the route tool in Jotunn's vanilla `Misc` category because Jotunn 2.30.0 documents custom build categories as not yet updated for Valheim 1.0.
- Fixed the broken `Drawing → state_draw/controls_draw` mapping and removed Russian-only UI paths.

## Current limits

- This is not yet the final survival-ready Road Project 0.1 release.
- Material, tool, stamina, and build-time economy is incomplete.
- Full reload/restart, second-client, and long multiplayer-session acceptance is pending.
- Intersections, road networks, platforms, excavations, and Terrain Edit remain roadmap work.
- EarthWorks must be installed on the server and every participating client.

## Installation

1. Install BepInExPack Valheim 5.4.2350 and Jotunn 2.30.0.
2. Download `EarthWorks-0.6.3.zip` from GitHub Releases.
3. Put `EarthWorks.dll` and `EarthWorks.Geometry.dll` together under `BepInEx/plugins`.

## Build and verification

```powershell
dotnet build .\src\EarthWorks\EarthWorks.csproj -c Release `
  -p:ProfileRoot="C:\path\to\ValheimProfile\" `
  -p:ValheimManagedDir="C:\path\to\Valheim\valheim_Data\Managed"
dotnet run --project .\tests\EarthWorks.GeometryTests\EarthWorks.GeometryTests.csproj -c Release
.\scripts\Audit-ValheimApi.ps1
.\scripts\Audit-Localization.ps1
```

## Documentation

- [Roadmap](ROADMAP.md) · [Русский](ROADMAP_RU.md)
- [Product contract](PROJECT_CONTRACT.md)
- [Runtime test protocol](TESTING.md)
- [Changelog](CHANGELOG.md) · [Русский](CHANGELOG_RU.md)
- [Contributing](CONTRIBUTING.md) · [Русский](CONTRIBUTING_RU.md)
- [0.6.3 release notes](docs/RELEASE_0.6.3.md) · [Русский](docs/RELEASE_0.6.3_RU.md)

## License

EarthWorks is proprietary source-available software, not open source. Official unmodified binaries may be used for personal non-commercial play. Source may be viewed and forked to prepare Pull Requests. Reuse in other projects, modified binary distribution, and commercial use require prior written permission from Ostrix. See [LICENSE.md](LICENSE.md).
