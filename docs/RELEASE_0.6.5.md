# EarthWorks 0.6.5 — native paint grid and corridor refresh

[Русская версия](RELEASE_0.6.5_RU.md)

EarthWorks 0.6.5 is a patch release for Valheim 1.0.12 (Steam build 25253764, network version 40, Unity 6000.0.75f1), BepInExPack 5.4.2350, and Jotunn 2.30.0.

## Changes

- Paint uses the exact native terrain-grid coordinates already stored by the planner; the obsolete half-cell offset is removed.
- Dirt and paving preserve the current paint-mask alpha used by special terrain such as lava.
- The shared paint index is tested at both sides of a zone seam, every corner of the 65×65 mask, and outside coordinates.
- Grass refresh follows sampled road positions and local widths instead of clearing the entire route bounding circle.
- English/Russian roadmap and contribution guidance now list concrete work for players, server owners, translators, UI contributors, C# contributors, and other mod authors.

Exact paint-core/bilinear-feather preview remains open. In-game two-zone/four-zone, Ashlands alpha, save/reload, second-client, and ATMC coexistence proof is also pending.

## Verification completed without launching Valheim

- Release solution build: 0 warnings, 0 errors.
- Geometry and paint-grid executable: 26/26 PASS.
- Persistence/localization executable: 5/5 PASS.
- Localization audit: 227/227 EN/RU keys plus placeholder, literal, dynamic state/stage, and mixed-language checks.
- Valheim API audit: all direct, interface, Harmony, reflection, native paint-coordinate, alpha-preservation, and corridor-refresh contracts PASS.
- ZIP structure inspected: only the two DLLs, icon, manifest, README, changelog, and license are included.

## Verified artifacts and deployment

```text
EarthWorks.dll           7797C10D98E124E45883A86FD9322E0476CEEA15E502CEC3FCB69EBFAE3BAFD5
EarthWorks.Geometry.dll  74BF59B52BC25486AA7659A6988C18C4A9044BC728947B159A46155A295C3D6A
EarthWorks-0.6.5.zip      EE754DC0CB1B98991D0781448626D059FD38FDAB5B1D70DDAB2D1906B5DCDDAC
```

With Valheim closed, both 0.6.5 DLLs were installed through the guarded deployment script into `TerrainRamp-1.0-Test\BepInEx\plugins\Ostrix-EarthWorks`. Installed versions are 0.6.5.0 and their hashes match the package. All 86 non-EarthWorks files in the profile were hash-checked before and after deployment; none changed.

## License

EarthWorks remains proprietary source-available software. Official unmodified binaries may be used for personal non-commercial play. Forks are allowed to prepare Pull Requests. Code reuse, modified binary distribution, and commercial use require prior written permission from Ostrix; see `LICENSE.md`.
