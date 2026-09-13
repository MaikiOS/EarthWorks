# Terrain compatibility and tool test plan

[Русская версия](TERRAIN_COMPATIBILITY_AND_TEST_PLAN_RU.md)

**Status:** static audit completed September 13, 2026. Runtime proof is still required.

This document compares EarthWorks 0.6.5 with the proven Valheim 1.0 terrain behavior in AdvancedTerrainModifiersCompatible 1.4.8. It records behavior requirements only; GPL implementation code is not copied into EarthWorks.

## Confirmed in EarthWorks now

- Height edits use native `Heightmap.WorldToVertex` coordinates and write every matching copy of a border vertex in adjacent Heightmaps.
- `TerrainComp` arrays are validated as `(width + 1)²` before mutation.
- The owner is claimed and verified before writes; operations are saved with `TerrainComp.Save(false)` and refreshed with `Heightmap.Poke(0, false)`.
- Height and paint arrays, operation count, last-operation point, and radius are snapshotted and restored if a batch fails.
- Preview and execution consume the same stored road-height plan.

## 0.6.5 status and remaining paint gates

1. **Paint-grid mapping — implemented:** writes use the planner's stored native `GridX/GridZ`; the obsolete `WorldToVertexMask` half-cell conversion is gone.
2. **Special mask channels — implemented:** dirt and paving copy the current `Heightmap.GetPaintMask(x, z).a` into the new color before saving.
3. **Chunk seams and corners — automated coverage added:** the shared `(width + 1)` index helper covers every 65×65 corner and rejects outside coordinates. Real two-zone and four-zone runtime proof is still required.
4. **Visible texture footprint:** the current road ribbon does not show the actual paint texel core and bilinear filtering feather. The editor needs separate core/feather visualization based on the same bounds used for writes.
5. **Clutter scope — implemented, runtime tuning pending:** grass refresh now follows stored preview samples with each local side width plus one metre, with a centerline fallback for old records.
6. **ATMC coexistence:** ATMC globally corrects Heightmap render UVs. EarthWorks must work both with and without that patch and must not apply a competing global correction twice.

ATMC's serialized `TerrainOp.Settings` payload is not required by EarthWorks because EarthWorks persists its own explicit vertex plan. Terrain restoration rules for legacy `TerrainModifier` objects become relevant when EarthWorks gains a Restore tool.

## Runtime test order

1. Establish screenshots and coordinates for one untouched control area.
2. Test height-only road edits at zone center, one zone edge, and a four-zone corner.
3. Test Dirt and Paved separately at the same positions; compare core, feather, seams, and untouched neighboring texels.
4. Repeat over Ashlands lava-capable terrain and verify that alpha/special data is unchanged.
5. Compare Current, Result, and Difference preview against the applied height and texture result.
6. Save, reload, reconnect, and inspect the same cells from a second client.
7. Repeat once with ATMC 1.4.8 enabled and once without it.
8. Force one failed batch and verify exact rollback of height, paint, operation metadata, and visuals.

Each result must record world coordinates, Heightmap indices, selected surface, screenshots before/preview/after/reload, and the relevant `Player.log` section.

## Interface usability pass

Before adding more modes, test every existing Route action with a player who has not read the source: finding Route in the hoe menu, placing and finishing points, opening `F7`, selecting/dragging handles, setting exact height and side widths, choosing a profile/surface, understanding validation, creating the board, and completing all six stages.

For each action record: expected intent, visible control, actual result, wrong action attempted, and wording that caused hesitation. Redesign should prioritize a short guided first route, persistent control hints, clear selected-object state, and preview legends; it should not add another hidden hotkey layer.

## Native manual terrain tools

EarthWorks will independently implement small manual operations after the road paint gate:

- Raise, Lower, Smooth, Level, Slope, Dirt, Paved, Cultivate, Restore, Undo, and Redo.
- Point, small square, large square, circle, and line footprints snapped through Valheim's native grid conversion.
- Candidate square sizes are a **2×2 m logical core** and a **4×4 m work area**. These are footprint sizes, not ambiguous radius values; final affected texels and visible filtered support must be measured in-game before the contract is locked.
- The preview must show height vertices, paint core, bilinear feather, exact dimensions, target height/delta, and every affected Heightmap copy.
- Every operation uses server authority, wards, protected-zone checks, configurable terrain range, transaction rollback, and explicit survival cost.

The first implementation milestone is one square Level tool and one square Paint tool sharing a tested grid-footprint helper. More tools are added only after those two match preview, save/reload, seams, and multiplayer behavior.
