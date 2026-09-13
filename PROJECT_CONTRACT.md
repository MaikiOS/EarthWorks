# EarthWorks Project Contract

## Product Promise

EarthWorks is a vanilla-friendly Valheim mod for planning and carrying out roads,
foundations, excavations, and other terrain projects. It automates repetitive
work without removing the work itself: survival players still gather materials,
prepare the site, spend time and stamina, react to danger, and obey progression
and server limits.

Public name: `EarthWorks`.

Technical plugin identifier: `com.ostrix.earthworks`.

This contract defines the current milestone. The authoritative full product
history, including confirmed future-tool behavior, superseded decisions, open
questions, and test-dependent values, is `DECISIONS.md`. Implementation deferral
does not make a confirmed product decision open again.

## Audience And Environment

- Solo and multiplayer Valheim survival worlds.
- Dedicated servers are a first-class target.
- Project state and permissions are server-authoritative.
- All participating clients need the mod for project editing and visualization.
- EarthWorks is a separate mod. `TerrainRamp` remains a small independent mod.
- Proven TerrainRamp geometry may be reused, but EarthWorks must not require the
  TerrainRamp package.

## Product Tool Set

- `Route` is the open-centerline tool. A two-point route replaces the standalone
  ramp; roads and later linear profiles are modes inside it.
- `Area` is the closed-contour tool for Platform/Foundation, Excavation, and
  later area profiles.
- `Terrain Edit` is the local brush/control-grid tool.
- There is no standalone player-facing Layout tool. Resurfacing and restoration
  are actions inside the relevant tool or project history.

## Vanilla-First Rules

- Normal play never turns a confirmed plan into free instant terrain edits.
- Materials, unlocks, stamina, time, threats, permissions, and protected areas
  remain meaningful.
- Terrain changes use Valheim terrain state and persistence rather than visual
  replacement meshes.
- The default terrain delta is Valheim's approximately `+/-8 m` range.
- If the server permits a larger range through another modification, EarthWorks
  uses the server-authorized range. A client cannot grant itself a larger range.
- Preview, resource calculation, validation, and execution use the same allowed
  terrain range and the same sampled terrain plan.
- Developer instant execution exists only in an explicit test environment.

## Development And Test Boundary

- EarthWorks test builds are installed only in the Thunderstore profile
  `TerrainRamp-1.0-Test` and only through `scripts\Deploy-TestBuild.ps1` with that
  profile named explicitly.
- `Default\BepInEx\plugins` must not contain `Ostrix-EarthWorks`,
  `EarthWorks.dll`, or `EarthWorks.Geometry.dll`.
- `Default\EarthWorks` is the source and artifact tree, not an installed mod. Its
  location does not authorize deployment into Default or moving the source tree.
- The controlled test identities are character `Test`, world `TerrainRamp_Lab`,
  and survival-validation world `TerrainRamp_SurvivalQA`.
- Test-only bootstrap/dev components and test deployments are separate from any
  public release package.

## First End-To-End Slice: Road Project 0.1

### Route Creation

- `LMB` adds route control points.
- The current test binding is a double `LMB` on the final point to finish route
  drawing and enter editing mode. EarthWorks does not intercept `Enter`, because
  Valheim uses it to open chat.
- While drawing, `RMB` removes the last point. When only A remains, the next
  `RMB` cancels the route. Outside the editor, vanilla RMB behavior returns.
- Switching to another tool hides the private draft but does not discard it;
  returning to the road tool restores it during the same world session.
- The route can contain any practical number of points within configured safety
  and performance limits.

### Route Editing

- Hover highlights a control point; `LMB` selects it.
- A selected point can be moved over the terrain.
- A vertical handle changes its elevation.
- Exact elevation can be entered numerically.
- `RMB` cancels the current point manipulation.
- With no point manipulation active, `RMB` returns to route drawing so points
  can be added or removed again.
- A manually edited elevation can be returned to `Auto`.

### Horizontal Geometry

- The point selector cycles `X-Spline`, `B-Spline`, `Bezier`, then `Corner`.
- `X-Spline` uses the Blanc-Schlick basis. `B-Spline` is the smooth no-handle
  point type. Switching onward to `Bezier` initializes its handles from the
  current automatic tangents instead of starting from an unrelated shape.
- A selected X-Spline point exposes smoothing from `0` to `100`: `0` is a
  corner, `50` is the through-point X-Spline preset, and `100` matches the
  B-Spline preset. The exact value is stored with the project.
- `Bezier` points expose independent curve handles, and `Corner` points create
  an intentional sharp turn.
- Any segment can be forced to remain strictly straight.

### Vertical Geometry

- `Automatic` creates a smooth feasible profile from the terrain and anchors.
- `Exact elevation anchors` fixes selected point heights and smoothly solves the
  profile between them.
- `Single elevation` sets a whole route or selected section to an exact height.
- The automatic solver prioritizes, in order: server terrain limits, maximum
  grade, smooth grade changes, minimum cut/fill, and proximity to existing land.

### Width And Cross-Section

- A route has a default width.
- Individual points can override width, with smooth interpolation between them.
- Left and right widths can be adjusted independently.
- The roadbed is flat by default.
- Four longitudinal roadbed profiles are available: straight slope, straight
  with soft joins, soft ends, and a full S-curve.
- Endpoint terrain-plane fitting matches both along and cross slope at A/B and
  can be toggled independently from the longitudinal profile.
- Cut and fill shoulders connect the roadbed to existing terrain.
- Manual lateral bank is optional; a convex crown is deferred.

### Surface

- A route has one default surface and can override selected sections.
- Only vanilla surface types unlocked for the player are available in survival.
- Bare cleared earth is valid.
- Earthwork and surfacing costs are calculated separately.
- Surface boundaries follow the terrain grid; Valheim may visually blend cells.
- A later resurfacing project can replace the player-selected section only.

### Intersections

- Preview detects intersections with recorded EarthWorks roads.
- Roads at compatible elevations merge into a common editable T, X, or Y area.
- The player chooses the intersection surface and can adjust the merge extent.
- Roads at incompatible elevations cannot form a terrain intersection; bridges,
  tunnels, and grade-separated junctions are separate future project types.
- A completed road keeps a lightweight geometry record so future projects can
  recognize it. The completed construction project itself remains closed.

### Confirmation And Persistence

- Editing is a private draft and does not modify the world.
- Confirmation triggers a fresh server validation of geometry, limits,
  permissions, protected areas, and conflicts.
- A valid confirmation creates a persistent multiplayer project.
- The project board is placed automatically near point A, outside the roadbed,
  shoulders, water, structures, and protected areas.
- If no safe board position exists nearby, project creation fails visibly rather
  than placing the board far away.
- Interacting with the board returns a player to the project.
- The first board stage accepts only the exact missing site-setup materials; it
  has no free inventory slots and cannot be used as storage.
- Multiple players may contribute safely to the same requirements.

### Project Editor

- `F7` enters or exits a dedicated editor while Route is selected. The player
  stays in place, normal character/build actions are blocked, and a visible
  cursor edits route points directly.
- Plan/Isometric navigation uses `MMB` pan, `Shift+MMB` orbit, `WASD` or arrow
  keys for continuous movement, wheel zoom, `Numpad 7/5` view switching,
  `Home` frame-all, and `Numpad .` frame-selected. A world-anchored terrain grid
  and exact cursor marker make point placement readable in Isometric view.
- The editor shows stage progress on the left, a contextual Inspector on the
  right, camera/preview/layer controls at the top, and explicit Back/Next
  actions at the bottom.
- Geometry, elevation, and width share one direct-manipulation stage. A selected
  flag exposes a centre drag, a vertical elevation handle, and independent left/
  right width handles. `Ctrl+LMB` inserts a flag on the visible curve and
  `Delete` removes the selected flag.
- The roadbed remains cross-flat by default. Endpoint terrain-plane fitting is
  an explicit optional mode; automatic curve banking is deferred until it has
  its own visible control and in-game tests.
- Damage or the accepted hostile-distance rule closes the editor and restores
  normal player input.

## Confirmed Construction Direction

These behaviors are confirmed but follow the first road-project slice:

- Site marking uses sparse vanilla-derived stakes and small flags. Exact models,
  spacing, and material balance remain test-driven.
- Site setup materials are consumed and are not refunded.
- A later modular construction warehouse accepts only exact project materials
  and cannot become a general-purpose storage exploit.
- Clearing, earthworks, surfacing, and completion are separate stages.
- Work pauses when stamina is empty and resumes after recovery.
- Work stops when a hostile threat is close enough to require a response.
- Multiple players can help; the target is at least ten simultaneous workers,
  with assigned work positions that prevent character overlap.
- Finished projects are immutable. Altering a road or surface creates a new
  project over the selected area.
- Vanilla, server-configured, and architect/admin rule modes are supported, but
  none may exceed the terrain capability actually authorized by the server.
- Wards, traders, world spawn, other critical locations, and configured protected
  areas prevent destructive project creation.
- Abandoned projects age every ten world days. At day fifty they are cancelled,
  restrictions are removed, and half of remaining materials survive in damaged
  vanilla-style chests. After another fifty days the remaining items are reduced
  once more; the chests then behave as ordinary world objects.

## Intentionally Deferred

- NPC construction workers.
- Bridges, tunnels, retaining structures, and template-built road details.
- Convex/crowned road cross-sections.
- Full free-form terrain sculpting, polygon foundations, booleans, bevels, and
  excavations. Their product behavior is confirmed in `DECISIONS.md`; only their
  implementation is deferred until the road foundation is proven.

## Open Risks And Test Decisions

- Compatibility adapters for specific terrain-limit mods.
- Exact vanilla prefabs for boards, stakes, flags, warehouse modules, and effects.
- Material quantities and marker spacing.
- Tuning of the confirmed hostile-detection baseline and notification behavior.
- Safe distribution of simultaneous worker positions along small projects.
- Exact renewable-plant relocation whitelist, feasibility, and fallback.
- Performance limits for long routes, previews, project records, and markers.
- Compatibility testing between built-in Plan/Isometric and additional camera
  mods. EarthWorks restores the player camera when its route tool is deselected.

## First Milestone Evidence

Road Project 0.1 is demonstrated only when:

1. A route with a straight segment, smooth curve, and sharp corner can be drawn
   and edited before confirmation.
2. Automatic and manual heights produce the same geometry in preview and test
   execution.
3. Width and surface overrides are visible and deterministic.
4. Invalid terrain ranges, grades, protected areas, and unloaded footprints are
   rejected before project creation.
5. Confirmation creates an automatically placed project board and a persistent
   project visible to a second client.
6. The project survives a world save, server restart, and client reconnect.
7. Test-only execution applies the exact previewed terrain through Valheim's
   persistent multiplayer terrain system.
8. Normal survival mode cannot invoke instant execution.
