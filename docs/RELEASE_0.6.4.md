# EarthWorks 0.6.4 — authority, persistence, localization, and maintainability

[Русская версия](RELEASE_0.6.4_RU.md)

EarthWorks 0.6.4 is a patch release for Valheim 1.0.12 (Steam build 25253764, network version 40, Unity 6000.0.75f1), BepInExPack 5.4.2350, and Jotunn 2.30.0.

## User-visible behavior

The existing Route workflow remains intact: multi-point straight and curved geometry, exact and automatic elevation, independent side widths, four longitudinal profiles, Current/Result/Difference terrain previews, per-segment surfaces, and a persistent six-stage project board.

Project-board stage requests are now authorized against the actual RPC sender. The server/owner validates distance, the laboratory-only shortcut, project-piece ownership, and every enabled ward covering the board before executing a stage.

English and Russian remain built into `EarthWorks.dll`. Translators may optionally override strings with `Translations/EarthWorks/English/translations.json` and `Translations/EarthWorks/Russian/translations.json`; an invalid external file is ignored without losing the embedded fallback.

## Compatibility and code health

- Reverified `Hoverable.GetHoverOffset()`, vanilla `Sign.m_hoverOffset`, Harmony targets, and every direct/reflection Valheim API contract used by EarthWorks.
- Preserved stable persisted enum IDs and v1-v4 project compatibility.
- Added invalid/truncated/oversized payload rejection coverage.
- Removed the unreachable legacy staged-editor path and its unused localization.
- Separated editor view, direct manipulation, camera grid/patches, project creation, persistence, and planner configuration into named modules.
- Added `EarthWorks.sln`, deterministic build defaults, clone-friendly local path overrides, portable CI, and bilingual architecture/contribution guides.

## Verification completed without launching Valheim

- Release solution build: 0 warnings, 0 errors.
- Geometry regression executable: 24/24 PASS.
- Persistence/localization executable: 5/5 PASS.
- Localization audit: 227/227 EN/RU keys plus placeholder, literal, dynamic state/stage, and mixed-language checks.
- Valheim API audit: all direct, interface, Harmony, and reflection contracts PASS.

Full single-player, reload, reconnect, and multiplayer runtime acceptance remains pending. The game was not launched while preparing this release.

## License

EarthWorks remains proprietary source-available software. Official unmodified binaries may be used for personal non-commercial play. Forks are allowed to prepare Pull Requests. Code reuse, modified binary distribution, and commercial use require prior written permission from Ostrix; see `LICENSE.md`.
