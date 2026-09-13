# EarthWorks 0.6.4 Unified Editor And Laboratory Test

[Русская версия](TESTING_RU.md)

## Scope

This build tests one continuous Route workflow:

`route -> interactive editing -> surface -> review -> automatic board -> construction stages`

Geometry, height, and width are now edited together around the selected flag.
The draft remains private until project creation. Instant construction stages
remain restricted to character `Test` in `TerrainRamp_Lab`.

## Environment

- Thunderstore profile: `TerrainRamp-1.0-Test` only.
- Character: `Test`.
- World: `TerrainRamp_Lab`.
- Never install or test EarthWorks in `Default`.
- Expected log lines: `EarthWorks 0.6.4 loaded` and
  `Terrain Ramp Test Bootstrap 0.3.0 loaded for profile-only testing`.

## Laboratory Panel

1. Press `F8` in `TerrainRamp_Lab`. The laboratory panel must release the
   cursor and block character actions while it is open.
2. Move `Прогресс персонажа` below `50%`: EarthWorks must offer bare earth but
   reject paving. At `50%` or higher, paving becomes available. This simulates
   EarthWorks unlocks without permanently teaching recipes to character `Test`.
3. Set maximum stamina between `25` and `300`, close the panel, spend stamina,
   then reopen it and press `Заполнить выносливость`. Leaving the laboratory
   restores the value captured before the test override.
4. Choose `Чистая площадка` or `Смешанный тест`, press
   `Сбросить лабораторию сейчас`, then confirm. The live reset must remove old
   EarthWorks projects, restore and rebuild terrain, and keep the game running.
   The mixed preset adds the same small set of vanilla trees and pickables to
   the central work zone after every reset.

## Route And Camera

1. Equip the hoe, open EarthWorks, and select `Маршрут`.
2. Add at least five points with `LMB`, including one sharp turn. `RMB` removes
   the last point. Tool switching must hide and restore the same draft.
3. Press `F7`. The cursor must be free. `MMB` pans, `Shift+MMB` rotates
   Isometric, wheel zooms, and `WASD` or arrows pan continuously.
4. Confirm the terrain grid stays world-anchored and the cursor cross marks the
   exact snapped position. `Home` frames the route and `Numpad .` frames the
   selected flag.
5. Double-click the final point or press `G`. The stage list must select
   `2. Редактирование`.

## Unified Editing

1. Select a flag with `LMB`. A local panel and three direct manipulators appear:
   centre for horizontal position, green for height, orange for left/right width.
2. Drag each manipulator. The planned ribbon must update interactively and the
   Inspector must show the current height mode and numeric values. The green
   height handle is intentionally available in Isometric; Plan keeps the centre
   point unambiguous for horizontal movement.
3. `Ctrl+LMB` on the visible curve inserts a flag. `Delete` or the local
   `Удалить` button removes the selected flag while at least two remain.
4. In the local panel select X-Spline, B-Spline, Bezier, and Corner. Bezier
   exposes draggable handles. The X-Spline slider and strict straight/curved
   segment button must update without using the right edge of the screen.
5. Toggle `Высота: Auto/точно`, drag the green handle, and enter a numeric Y.
   Anchored flags remain visibly distinct. The four global height modes stay in
   Inspector.
6. At a sharp turn, both ribbon edges must retain their full width without the
   previous inward pinch or twisted yellow line.
7. The default roadbed must be cross-flat. Its left and right edges may change
   longitudinally but must not inherit a continuous side slope. `Alt` explicitly
   enables short A/B terrain-plane fitting; the middle remains cross-flat.
8. `P` cycles the four longitudinal profiles. Current/Result/Difference must
   show the calculated roadbed, not the original terrain bumps.
9. `RMB` cancels an active drag. With no drag it first clears selection, then a
   further `RMB` returns to route drawing. Press `G` to open Surface.

## Surface And Review

1. With no selected segment choose `Чистая земля` or `Мощение` for the route.
2. Select a curved segment with `LMB`; it turns magenta. Choose a local override.
   `RMB` removes that override; otherwise it returns to Editing.
3. Press `G`. In `Результат`, review must show a green terrain-grid wireframe
   at the exact final vertex heights. `Разница` must instead show blue fill,
   orange cut, white unchanged cells, and red invalid cells. The inspector must
   show affected vertices, volumes, maximum grade, profile, and A/B fitting.
4. Press `G` again. A vanilla-derived board appears automatically near A.

## Construction Result

Interact with the board using the displayed `E` binding once per stage:

1. Site setup.
2. Marking.
3. Clearing.
4. Earthworks.
5. Surfacing.
6. Completion.

After Earthworks, check a straight section from both directions: the roadbed
must have no centre ridge or unintended transverse lean. After Surfacing, the
chosen dirt/paved mask must cover the whole roadbed without four-cell gaps.
After Completion, the board, collision, route line, ropes, and stakes must be
gone. Reload the world and confirm terrain height and paint persist.

## Failure Capture

- Screenshot with the editor stage or board text visible.
- Exact input immediately before the failure.
- Whether the route crossed a Heightmap boundary.
- `TerrainRamp-1.0-Test\BepInEx\LogOutput.log` from that launch.

## Reset The Laboratory

Press `F8` while `Test` is in `TerrainRamp_Lab`, choose the vegetation preset,
press `Сбросить лабораторию сейчас`, and confirm. The bootstrap performs the
reset immediately without restarting Valheim. `RebuildOnNextLoad = true`
remains an emergency fallback for a laboratory that cannot be entered normally.
