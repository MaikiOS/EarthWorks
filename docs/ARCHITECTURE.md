# EarthWorks Architecture

[Русская версия](ARCHITECTURE_RU.md)

EarthWorks separates route mathematics, editor interaction, planning, persistence, and world execution so a change can be reviewed at the boundary it affects.

## Runtime flow

`EarthWorksPlugin` registers configuration and prefabs. `RoadDraftSession` owns the private draft state. `RoadTerrainPlanner` converts that draft into one immutable `RoadBuildPlan`. Both preview and execution consume that same plan. `RoadProjectRecord` serializes the persistent subset into the board ZDO. The board validates the RPC initiator before `RoadTerrainApplier` writes through Valheim's `Heightmap` and `TerrainComp` systems.

## Module map

| Area | Files | Change here when |
| --- | --- | --- |
| Bootstrap and config | `EarthWorksPlugin.cs` | Registering the plugin, config, tool, or board prefab |
| Editor UI | `EarthWorksEditorView.cs` | Changing panels, labels, controls, or layout |
| Draft lifecycle | `RoadDraftSession.cs` | Changing stages, route completion, review, or project creation |
| Editor interaction | `RoadDraftSession.Editor.cs` | Changing selection, dragging, handles, numeric entry, or shortcuts |
| Camera and grid | `RoadEditorCamera.cs`, `RoadEditorGrid.cs`, `RoadEditorPatches.cs` | Changing Plan/Isometric navigation or input ownership |
| Pure geometry | `src/EarthWorks.Geometry` | Changing curves, profiles, offsets, or solvers |
| Terrain planning | `RoadTerrainPlanner.cs`, `RoadBuildSettings.cs` | Changing validation, sampling, cut/fill, or footprint generation |
| Persistence | `RoadProjectRecord.cs` | Changing saved project data or format compatibility |
| Board authority and visuals | `RoadProjectAuthority.cs`, `RoadProjectBoard.cs`, `RoadProjectFactory.cs` | Changing placement, RPC permission, stages, or site markers |
| World writes | `RoadTerrainApplier.cs` | Changing ownership, terrain transactions, rollback, or paint |
| Localization | `Translations/EarthWorks` and `EarthWorksLocalization.cs` | Adding or translating user-facing text |

## Contracts that must not be broken

- Persisted enum numeric values are permanent. Add new values at the end and add a persistence test.
- Project format changes require a new format version and readers for every supported older version.
- Clients never authorize terrain range, ward access, or construction stage changes.
- Preview and execution must use the same calculated plan.
- Normal gameplay cannot reach developer-lab instant execution.
- `Player.Update` is patched before vanilla input handling only so EarthWorks can consume input it currently owns; keep that scope narrow.

## Verification boundaries

Portable CI runs geometry and localization checks. A full mod build, persistence tests, and API audit require legally installed local game assemblies. Runtime claims require the explicit in-game protocol in `TESTING.md`; a successful build is not runtime proof.
