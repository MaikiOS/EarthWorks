using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using OstrixMods.EarthWorks.Geometry;

namespace OstrixMods.EarthWorks
{
    internal static class EarthWorksLocalization
    {
        private const string Prefix = "earthworks_";
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "piece_name", "Route" },
            { "piece_desc", "Plan and build a persistent terrain road project." },
            { "hud_title", "EARTHWORKS · ROUTE" },
            { "state_idle", "READY" },
            { "state_draw", "DRAW" },
            { "state_geometry", "EDITING" },
            { "state_surface", "SURFACE" },
            { "state_review", "REVIEW" },
            { "click_start", "Place the first route point." },
            { "draw_route", "Add points; double-LMB on the final point or G finishes drawing." },
            { "need_two", "A route needs at least two different points." },
            { "point_added", "Control point {0} added." },
            { "double_click_finish", "Double-LMB on the final point finishes drawing." },
            { "point_removed", "Last control point removed." },
            { "draft_cancelled", "Road draft cancelled." },
            { "route_finished", "Interactive editing: position, height, width and curve. G continues to surface." },
            { "geometry_help", "Select a flag; drag its center, green height handle or orange width handles. Ctrl+LMB adds a flag." },
            { "geometry_place", "Drag the active handle; RMB cancels only this change." },
            { "select_point", "Aim at a control point." },
            { "point_selected", "Point selected. Click its new position; RMB cancels." },
            { "handle_selected", "Manual handle selected. Click its new position; RMB cancels." },
            { "geometry_changed", "Route geometry changed." },
            { "move_cancelled", "Current manipulation cancelled." },
            { "drawing_resumed", "Route drawing resumed." },
            { "duplicate_point", "Consecutive route points must differ." },
            { "too_many_points", "Route reached the safety limit of {0} points." },
            { "aim_loaded", "Aim at loaded terrain." },
            { "finish_manipulation", "Place or cancel the active point/handle first." },
            { "segment_straight", "Selected segment is strictly straight." },
            { "segment_curved", "Selected segment uses the route curve." },
            { "point_mode_changed", "Point {0}: {1}." },
            { "stage_geometry", "Returned to interactive editing." },
            { "stage_surface", "Road surface. Select a curved segment and choose its surface in Inspector." },
            { "height_selection_cleared", "Point selection cleared. The calculated profile is unchanged." },
            { "height_select_first", "Select a control point first, or choose Single elevation." },
            { "height_mode_changed", "Height profile mode changed." },
            { "height_changed", "Height changed." },
            { "height_invalid", "That height is not a valid number." },
            { "height_exact_set", "Exact height set." },
            { "height_anchor_on", "Exact elevation anchor set. The road is smoothed between fixed anchors." },
            { "height_anchor_off", "Exact elevation anchor removed. This point is automatic again." },
            { "height_input_unavailable", "Valheim text input is unavailable." },
            { "height_input_title", "Exact road height" },
            { "width_changed", "Road width changed." },
            { "surface_help", "Select a segment, then choose bare earth or paving in Inspector. G validates the project." },
            { "surface_segment_selected", "Road segment selected for a surface override." },
            { "select_segment", "Aim closer to a route segment." },
            { "surface_changed", "Road surface changed." },
            { "surface_reset_default", "Segment surface returned to the route default." },
            { "review_ready", "Validation passed. G creates the persistent project and automatic board." },
            { "review_invalid", "Validation failed. Read the reason, then RMB returns to editing." },
            { "review_use_g", "Press G to create the validated project." },
            { "project_created", "Road project created. The board was placed automatically near A." },
            { "whole_route", "whole route" },
            { "metrics", "Points {0} | Segments {1} | Length {2:F1} m" },
            { "surface_detail", "Target: {0} | Here: {1} | Route default: {2}" },
            { "review_detail", "Vertices {0} | Cut {1:F0} m³ | Fill {2:F0} m³ | Grade {3:F1}%" },
            { "controls_idle", "[LMB] start" },
            { "controls_draw", "[LMB] point  [double LMB/G] geometry  [RMB] remove" },
            { "controls_geometry", "[LMB] select/drag  [Ctrl+LMB] add  [Delete] remove  [F/H] exact height  [G] surface  [RMB] cancel/back" },
            { "controls_surface", "[LMB] segment  [MMB] surface  [P] roadbed  [Alt] fit A/B  [G] review" },
            { "controls_review", "[P] roadbed  [Alt] fit A/B  [G] create project  [RMB] edit" },
            { "mode_auto", "Optimal profile" },
            { "mode_anchored", "Exact elevation anchors" },
            { "mode_single", "Single elevation" },
            { "mode_uniform", "Uniform A-B grade" },
            { "control_xspline", "X-Spline" },
            { "control_bspline", "B-Spline" },
            { "control_corner", "Corner" },
            { "control_bezier", "Bezier" },
            { "roadbed_detail", "Roadbed: {0} | Endpoint terrain planes: {1}" },
            { "endpoint_fit_on", "ON" },
            { "endpoint_fit_off", "OFF" },
            { "endpoint_fit_changed", "Endpoint terrain-plane fitting changed." },
            { "road_profile_changed", "Roadbed profile changed." },
            { "profile_linear", "Straight slope" },
            { "profile_linear_joined", "Straight + soft joins" },
            { "profile_soft_ends", "Soft ends" },
            { "profile_smooth", "Full S-curve" },
            { "camera_detail", "Camera: {0}" },
            { "camera_player", "Player" },
            { "camera_plan", "Plan" },
            { "camera_isometric", "Isometric" },
            { "camera_controls_player", "[F7] Project Editor" },
            { "camera_controls_active", "[MMB] pan  [Shift+MMB] orbit  [WASD/arrows] move  [wheel] zoom  [Numpad 7/5] view  [F7] exit" },
            { "surface_bare", "Bare earth" },
            { "surface_paved", "Paved" },
            { "board_name", "EarthWorks road project" },
            { "board_corrupt", "EarthWorks project data is unavailable." },
            { "board_hover", "Stage {0}/6: {1}\nRoad length: {2:F1} m" },
            { "board_stage_lists", "Done: {0}\nRemaining: {1}" },
            { "board_none", "none" },
            { "board_result", "After the action: {0}" },
            { "board_action", "[<color=yellow><b>{0}</b></color>] {1}" },
            { "lab_only", "Instant stage execution is available only to Test in TerrainRamp_Lab." },
            { "board_access_lost", "Construction stopped: access to the site is no longer allowed." },
            { "board_threat", "Construction stopped: a hostile creature is nearby." },
            { "stage_setup", "Site setup" },
            { "stage_marking", "Marking" },
            { "stage_clearing", "Clearing" },
            { "stage_earthworks", "Earthworks" },
            { "stage_surfacing", "Surfacing" },
            { "stage_completion", "Completion" },
            { "stage_completed", "Completed" },
            { "stage_action_setup", "confirm the site setup" },
            { "stage_action_marking", "confirm the route marking" },
            { "stage_action_clearing", "clear the roadbed" },
            { "stage_action_earthworks", "apply the calculated terrain heights" },
            { "stage_action_surfacing", "apply the selected surface" },
            { "stage_action_completion", "complete the project" },
            { "stage_result_setup", "route boundaries and stakes become visible" },
            { "stage_result_marking", "the clearing stage becomes active" },
            { "stage_result_clearing", "grass is removed; large objects still require manual clearing in this build" },
            { "stage_result_earthworks", "terrain receives the saved road profile" },
            { "stage_result_surfacing", "the selected dirt or paved surface is painted across the roadbed" },
            { "stage_result_completion", "the board and construction markings disappear; route metadata remains" },
            { "stage_setup_done", "Site setup accepted. The project is ready for marking." },
            { "stage_marking_done", "Route marking confirmed. The site is ready for clearing." },
            { "stage_clearing_done", "Clutter clearing completed. Earthworks are next." },
            { "stage_already_completed", "This road project is already complete." },
            { "editor_preview_current", "Current" },
            { "editor_preview_result", "Result" },
            { "editor_preview_difference", "Difference" },
            { "editor_layer_roadbed", "Roadbed" },
            { "editor_layer_grid", "2 m grid" },
            { "editor_layer_characters", "Characters" },
            { "editor_layer_pieces", "Buildings" },
            { "editor_layer_world", "World objects" },
            { "editor_clean_view", "Clean view" },
            { "editor_show_all", "Show all" },
            { "editor_stages", "PROJECT STAGES" },
            { "editor_controls", "LMB: select/drag\nCtrl+LMB: add point\nDelete: remove point\nMMB: pan camera\nShift+MMB: orbit\nF7: exit" },
            { "editor_draw_help", "Place route points. Double-LMB the final point or press G to finish." },
            { "editor_start_help", "LMB on terrain starts a new route." },
            { "editor_profile", "Profile: {0}" },
            { "editor_cycle_profile", "Change roadbed profile (P)" },
            { "editor_fit_on", "A/B fit terrain" },
            { "editor_fit_off", "A/B do not fit terrain" },
            { "editor_smoothing", "X-Spline smoothing: {0}%" },
            { "editor_next_straight", "Next segment: STRAIGHT" },
            { "editor_next_curved", "Next segment: CURVED" },
            { "editor_flag_help", "Select and drag a flag. Ctrl+LMB adds one; Delete removes the selected flag." },
            { "editor_local_help", "Local settings are shown beside the selected flag." },
            { "editor_height_profile", "Height profile" },
            { "editor_point_summary", "Height {0:F2} m · width {1:F2} / {2:F2} m" },
            { "editor_point", "POINT {0}" },
            { "editor_segment", "SEGMENT {0}" },
            { "editor_nothing_selected", "Nothing selected" },
            { "editor_add_point", "+ point" },
            { "editor_remove", "Remove" },
            { "editor_height_exact", "Height: exact" },
            { "editor_height_auto", "Height: Auto" },
            { "editor_exact_y", "Exact Y..." },
            { "editor_drag_handles", "Drag center: position · green: Y (Isometric)\norange: left/right width" },
            { "editor_height", "Height: {0:F2} m" },
            { "editor_exact_height", "Exact height..." },
            { "editor_anchored", "Anchored" },
            { "editor_anchor", "Anchor" },
            { "editor_width_point", "Selected point width" },
            { "editor_width_route", "Whole route width" },
            { "editor_left", "Left: {0:F2} m" },
            { "editor_right", "Right: {0:F2} m" },
            { "editor_reset_width", "Restore route width" },
            { "editor_surface_segment", "Selected segment surface" },
            { "editor_surface_route", "Whole route surface" },
            { "editor_plan_pending", "Calculation is not ready." },
            { "editor_plan_ready", "PROJECT READY TO CREATE" },
            { "editor_plan_errors", "PROJECT HAS ERRORS" },
            { "editor_plan_metrics", "Vertices: {0}\nCut: {1:F1} m³\nFill: {2:F1} m³\nMaximum grade: {3:F1}%" },
            { "editor_back", "Back" },
            { "editor_next_draw", "Finish route (G)" },
            { "editor_next_surface", "Continue to surface (G)" },
            { "editor_next_review", "Continue to review (G)" },
            { "editor_next_create", "Create project (G)" },
            { "editor_next_continue", "Continue (G)" },
            { "editor_stage_route", "1. Route" },
            { "editor_stage_edit", "2. Editing" },
            { "editor_stage_surface", "3. Surface" },
            { "editor_stage_review", "4. Review" },
            { "editor_stage_new", "New project" },
            { "camera_pointer", "Pointer: X {0:F1} · Y {1:F1} · Z {2:F1}" },
            { "camera_pointer_unloaded", "Pointer: outside loaded terrain" },
            { "camera_editor_title", "PROJECT EDITOR · {0}" },
            { "camera_open", "F7 · open project editor" },
            { "camera_active_help", "MMB: pan · Shift+MMB: orbit · WASD/arrows: move · wheel: zoom · Numpad 7/5: view" },
            { "camera_inactive_help", "F7: Plan/Isometric editor" },
            { "camera_closed_threat", "Editor closed: a hostile creature is nearby." },
            { "camera_closed_damage", "Editor closed: the character took damage." },
            { "draft_selected_summary", "Point {0}{1} | Y {2:F2} m | width {3:F2} / {4:F2} m" },
            { "draft_y_anchored", " · Y anchored" },
            { "draft_y_auto", " · Y Auto" },
            { "draft_select_flag", "Height: {0} | select a flag or Ctrl+LMB to add one." },
            { "board_plan_missing", "The road project has not been calculated." },
            { "board_safe_place_missing", "No safe automatic board position was found near point A." },
            { "board_prefab_missing", "The project board prefab is not registered." },
            { "board_creation_failed", "Valheim did not create a persistent project board." },
            { "terrain_apply_rolled_back", "Terrain was not changed; the original state was restored." },
            { "terrain_apply_rollback_failed", "Terrain application and rollback failed. Stop the test and save the log." },
            { "terrain_applied", "Terrain was changed using the saved project plan." },
            { "surface_bare_no_paint", "Bare earth selected; no separate surface paint is required." },
            { "surface_apply_rolled_back", "Surface was not changed; the original paint was restored." },
            { "surface_apply_rollback_failed", "Surface application and rollback failed. Stop the test and save the log." },
            { "clearing_applied", "Grass was cleared. Large objects still require manual clearing in this build." },
            { "surface_applied", "The road surface was applied and saved." },
            { "terrain_plan_corrupt", "The saved project plan is empty or damaged." },
            { "terrain_access_lost", "Access to part of the construction site is no longer allowed." },
            { "terrain_comp_create_failed", "Valheim did not create a persistent terrain compiler for part of the project." },
            { "terrain_structure_incompatible", "This Valheim terrain structure is incompatible with EarthWorks." },
            { "terrain_unloaded", "Part of the project is unloaded or the world terrain grid changed." },
            { "terrain_prepare_failed", "The project terrain operation could not be prepared." },
            { "plan_need_two", "A road needs at least two valid points." },
            { "plan_geometry_invalid", "Route geometry is invalid." },
            { "plan_loaded_required", "The entire route and its shoulders must be loaded." },
            { "plan_water_unsupported", "This route version cannot build roads through water." },
            { "plan_endpoint_planes_failed", "Stable terrain planes could not be built at A/B. Disable Alt fitting or move the endpoint away from the cliff." },
            { "plan_endpoint_short", "The endpoint segment is too short for A/B terrain fitting." },
            { "plan_grade_exceeded", "The selected roadbed profile exceeds the allowed longitudinal grade." },
            { "plan_turn_tight", "The turn is too tight for the selected width. Reduce width or increase curve radius." },
            { "plan_hits_water", "The route or its shoulder intersects water." },
            { "plan_height_limit", "The road exceeds the server-authorized terrain height limit." },
            { "plan_private_area", "Part of the route is protected by a ward." },
            { "plan_no_build_zone", "Part of the route enters a world no-build zone." },
            { "plan_vertex_limit", "The project exceeds the safe terrain-vertex limit: {0}." },
            { "plan_no_vertices", "The route does not affect any loaded terrain vertices." },
            { "profile_height_limit", "The profile exceeds the server-authorized terrain height limit." },
            { "profile_grade_impossible", "The allowed grade cannot be maintained between fixed elevations." },
            { "profile_failed", "The vertical route profile could not be calculated." }
        };

        private static readonly Dictionary<string, string> Russian = new Dictionary<string, string>
        {
            { "piece_name", "Маршрут" },
            { "piece_desc", "Спроектировать и построить сохраняемую дорогу на terrain Valheim." },
            { "hud_title", "EARTHWORKS · МАРШРУТ" },
            { "state_idle", "ГОТОВ" },
            { "state_draw", "ТОЧКИ МАРШРУТА" },
            { "state_geometry", "РЕДАКТИРОВАНИЕ" },
            { "state_surface", "ПОКРЫТИЕ" },
            { "state_review", "ПРОВЕРКА" },
            { "click_start", "Поставь первую точку маршрута." },
            { "draw_route", "Добавляй точки; двойной ЛКМ по последней или G завершает маршрут." },
            { "need_two", "Для маршрута нужны минимум две разные точки." },
            { "point_added", "Добавлена контрольная точка {0}." },
            { "double_click_finish", "Двойной ЛКМ по последней точке завершает маршрут." },
            { "point_removed", "Последняя контрольная точка удалена." },
            { "draft_cancelled", "Черновик дороги отменён." },
            { "route_finished", "Интерактивная правка положения, высоты, ширины и кривой. G переходит к покрытию." },
            { "geometry_help", "Выбери флагшток; тяни центр, зелёную высоту или оранжевые ручки ширины. Ctrl+ЛКМ добавляет флагшток." },
            { "geometry_place", "Тяни активную ручку; ПКМ отменяет только это изменение." },
            { "select_point", "Наведи на контрольную точку." },
            { "point_selected", "Точка выбрана. Укажи новое место; ПКМ отменяет." },
            { "handle_selected", "Ручка выбрана. Укажи новое место; ПКМ отменяет." },
            { "geometry_changed", "Геометрия маршрута изменена." },
            { "move_cancelled", "Текущее изменение отменено." },
            { "drawing_resumed", "Добавление точек маршрута продолжено." },
            { "duplicate_point", "Соседние точки маршрута не могут совпадать." },
            { "too_many_points", "Достигнут безопасный лимит: {0} точек." },
            { "aim_loaded", "Наведи инструмент на загруженную землю." },
            { "finish_manipulation", "Сначала поставь или отмени выбранную точку/ручку." },
            { "segment_straight", "Выбранный участок теперь строго прямой." },
            { "segment_curved", "Выбранный участок снова использует кривую маршрута." },
            { "point_mode_changed", "Точка {0}: {1}." },
            { "stage_geometry", "Возврат к интерактивному редактированию." },
            { "stage_surface", "Покрытие дороги. Выбери участок кривой и его покрытие в Inspector." },
            { "height_selection_cleared", "Выбор точки снят. Рассчитанный профиль не изменён." },
            { "height_select_first", "Сначала выбери точку или режим «Одна высота»." },
            { "height_mode_changed", "Режим профиля высоты изменён." },
            { "height_changed", "Высота изменена." },
            { "height_invalid", "Введено некорректное число высоты." },
            { "height_exact_set", "Точная высота установлена." },
            { "height_anchor_on", "Поставлен точный высотный флаг. Между закреплёнными точками дорога сглаживается автоматически." },
            { "height_anchor_off", "Точный высотный флаг снят. Высота точки снова рассчитывается автоматически." },
            { "height_input_unavailable", "Окно числового ввода Valheim недоступно." },
            { "height_input_title", "Точная высота дороги" },
            { "width_changed", "Ширина дороги изменена." },
            { "surface_help", "Выбери участок, затем чистую землю или мощение в Inspector. G запускает проверку." },
            { "surface_segment_selected", "Выбран участок для отдельного покрытия." },
            { "select_segment", "Наведи ближе к участку маршрута." },
            { "surface_changed", "Покрытие дороги изменено." },
            { "surface_reset_default", "Покрытие участка возвращено к общему." },
            { "review_ready", "Проверка пройдена. G создаёт проект и автоматическую табличку." },
            { "review_invalid", "Проверка не пройдена. Прочитай причину; ПКМ возвращает к правке." },
            { "review_use_g", "Нажми G, чтобы создать проверенный проект." },
            { "project_created", "Проект дороги создан. Табличка поставлена автоматически возле A." },
            { "whole_route", "весь маршрут" },
            { "metrics", "Точек {0} | Участков {1} | Длина {2:F1} м" },
            { "surface_detail", "Цель: {0} | Здесь: {1} | Весь маршрут: {2}" },
            { "review_detail", "Вершин {0} | Срез {1:F0} м³ | Насыпь {2:F0} м³ | Уклон {3:F1}%" },
            { "controls_idle", "[ЛКМ] начать" },
            { "controls_draw", "[ЛКМ] точка  [двойной ЛКМ/G] геометрия  [ПКМ] убрать" },
            { "controls_geometry", "[ЛКМ] выбрать/тянуть  [Ctrl+ЛКМ] добавить  [Delete] удалить  [F/H] точная Y  [G] покрытие  [ПКМ] отмена/назад" },
            { "controls_surface", "[ЛКМ] участок  [СКМ] покрытие  [P] полотно  [Alt] A/B  [G] проверка" },
            { "controls_review", "[P] полотно  [Alt] прилегание A/B  [G] создать  [ПКМ] править" },
            { "mode_auto", "Оптимальный профиль" },
            { "mode_anchored", "Точные высотные точки" },
            { "mode_single", "Одна высота" },
            { "mode_uniform", "Равномерный уклон A-B" },
            { "control_xspline", "X-Spline" },
            { "control_bspline", "B-Spline" },
            { "control_corner", "Угол" },
            { "control_bezier", "Безье" },
            { "roadbed_detail", "Полотно: {0} | Плоскости рельефа A/B: {1}" },
            { "endpoint_fit_on", "ВКЛ" },
            { "endpoint_fit_off", "ВЫКЛ" },
            { "endpoint_fit_changed", "Прилегание полотна к плоскостям рельефа A/B изменено." },
            { "road_profile_changed", "Профиль полотна изменён." },
            { "profile_linear", "Прямой склон" },
            { "profile_linear_joined", "Прямой + мягкие стыки" },
            { "profile_soft_ends", "Мягкие концы" },
            { "profile_smooth", "Полная S-кривая" },
            { "camera_detail", "Камера: {0}" },
            { "camera_player", "От игрока" },
            { "camera_plan", "Plan сверху" },
            { "camera_isometric", "Isometric" },
            { "camera_controls_player", "[F7] редактор проекта" },
            { "camera_controls_active", "[СКМ] двигать  [Shift+СКМ] вращать  [WASD/стрелки] двигать  [колесо] масштаб  [Numpad 7/5] вид  [F7] выход" },
            { "surface_bare", "Чистая земля" },
            { "surface_paved", "Мощёная" },
            { "board_name", "EarthWorks: проект дороги" },
            { "board_corrupt", "Данные проекта EarthWorks недоступны." },
            { "board_hover", "Этап {0}/6: {1}\nДлина дороги: {2:F1} м" },
            { "board_stage_lists", "Готово: {0}\nОсталось: {1}" },
            { "board_none", "ничего" },
            { "board_result", "После действия: {0}" },
            { "board_action", "[<color=yellow><b>{0}</b></color>] {1}" },
            { "lab_only", "Мгновенные тестовые этапы доступны только Test в TerrainRamp_Lab." },
            { "board_access_lost", "Работа остановлена: доступ к площадке больше не разрешён." },
            { "board_threat", "Работа остановлена: рядом враждебное существо." },
            { "stage_setup", "Подготовка площадки" },
            { "stage_marking", "Разметка" },
            { "stage_clearing", "Расчистка" },
            { "stage_earthworks", "Земляные работы" },
            { "stage_surfacing", "Покрытие" },
            { "stage_completion", "Завершение" },
            { "stage_completed", "Завершён" },
            { "stage_action_setup", "подтвердить подготовку площадки" },
            { "stage_action_marking", "подтвердить разметку маршрута" },
            { "stage_action_clearing", "расчистить полотно дороги" },
            { "stage_action_earthworks", "применить рассчитанные высоты рельефа" },
            { "stage_action_surfacing", "нанести выбранное покрытие" },
            { "stage_action_completion", "завершить проект" },
            { "stage_result_setup", "появятся границы маршрута и колышки" },
            { "stage_result_marking", "станет активен этап расчистки" },
            { "stage_result_clearing", "трава исчезнет; крупные объекты в этой сборке пока расчищаются вручную" },
            { "stage_result_earthworks", "рельеф примет сохранённый профиль дороги" },
            { "stage_result_surfacing", "чистая земля или мощение покроет всё полотно" },
            { "stage_result_completion", "табличка и строительная разметка исчезнут; данные маршрута сохранятся" },
            { "stage_setup_done", "Подготовка площадки принята. Следующий этап — разметка." },
            { "stage_marking_done", "Разметка маршрута подтверждена. Следующий этап — расчистка." },
            { "stage_clearing_done", "Растительный clutter очищен. Следующий этап — земляные работы." },
            { "stage_already_completed", "Этот проект дороги уже завершён." },
            { "editor_preview_current", "Сейчас" },
            { "editor_preview_result", "Результат" },
            { "editor_preview_difference", "Разница" },
            { "editor_layer_roadbed", "Полотно" },
            { "editor_layer_grid", "Сетка 2 м" },
            { "editor_layer_characters", "Персонажи" },
            { "editor_layer_pieces", "Постройки" },
            { "editor_layer_world", "Объекты мира" },
            { "editor_clean_view", "Чистый вид" },
            { "editor_show_all", "Показать всё" },
            { "editor_stages", "ЭТАПЫ ПРОЕКТА" },
            { "editor_controls", "ЛКМ: выбрать/тянуть\nCtrl+ЛКМ: добавить точку\nDelete: удалить точку\nСКМ: двигать камеру\nShift+СКМ: вращать\nF7: выйти" },
            { "editor_draw_help", "Поставь точки маршрута. Двойной ЛКМ по последней точке или G завершает маршрут." },
            { "editor_start_help", "ЛКМ по земле начинает новый маршрут." },
            { "editor_profile", "Профиль: {0}" },
            { "editor_cycle_profile", "Сменить профиль полотна (P)" },
            { "editor_fit_on", "A/B прилегают к рельефу" },
            { "editor_fit_off", "A/B без прилегания" },
            { "editor_smoothing", "Сглаживание X-Spline: {0}%" },
            { "editor_next_straight", "Следующий участок: ПРЯМОЙ" },
            { "editor_next_curved", "Следующий участок: КРИВОЙ" },
            { "editor_flag_help", "Выбери и тяни флагшток. Ctrl+ЛКМ добавляет новый; Delete удаляет выбранный." },
            { "editor_local_help", "Локальные параметры находятся рядом с выбранным флагштоком." },
            { "editor_height_profile", "Профиль высоты" },
            { "editor_point_summary", "Высота {0:F2} м · ширина {1:F2} / {2:F2} м" },
            { "editor_point", "ТОЧКА {0}" },
            { "editor_segment", "УЧАСТОК {0}" },
            { "editor_nothing_selected", "Ничего не выбрано" },
            { "editor_add_point", "+ точка" },
            { "editor_remove", "Удалить" },
            { "editor_height_exact", "Высота: точно" },
            { "editor_height_auto", "Высота: Auto" },
            { "editor_exact_y", "Точная Y..." },
            { "editor_drag_handles", "Тяни центр: положение · зелёную: Y (Isometric)\nоранжевые: ширина слева/справа" },
            { "editor_height", "Высота: {0:F2} м" },
            { "editor_exact_height", "Точная высота..." },
            { "editor_anchored", "Закреплена" },
            { "editor_anchor", "Закрепить" },
            { "editor_width_point", "Ширина выбранной точки" },
            { "editor_width_route", "Ширина всего маршрута" },
            { "editor_left", "Слева: {0:F2} м" },
            { "editor_right", "Справа: {0:F2} м" },
            { "editor_reset_width", "Вернуть ширину маршрута" },
            { "editor_surface_segment", "Покрытие выбранного участка" },
            { "editor_surface_route", "Покрытие всего маршрута" },
            { "editor_plan_pending", "Расчёт ещё не готов." },
            { "editor_plan_ready", "ПРОЕКТ ГОТОВ К СОЗДАНИЮ" },
            { "editor_plan_errors", "ЕСТЬ ОШИБКИ" },
            { "editor_plan_metrics", "Вершин: {0}\nВыемка: {1:F1} м³\nНасыпь: {2:F1} м³\nМакс. уклон: {3:F1}%" },
            { "editor_back", "Назад" },
            { "editor_next_draw", "Завершить маршрут (G)" },
            { "editor_next_surface", "К покрытию (G)" },
            { "editor_next_review", "К проверке (G)" },
            { "editor_next_create", "Создать проект (G)" },
            { "editor_next_continue", "Продолжить (G)" },
            { "editor_stage_route", "1. Маршрут" },
            { "editor_stage_edit", "2. Редактирование" },
            { "editor_stage_surface", "3. Покрытие" },
            { "editor_stage_review", "4. Проверка" },
            { "editor_stage_new", "Новый проект" },
            { "camera_pointer", "Точка под курсором: X {0:F1} · Y {1:F1} · Z {2:F1}" },
            { "camera_pointer_unloaded", "Точка под курсором: вне загруженного рельефа" },
            { "camera_editor_title", "РЕДАКТОР ПРОЕКТА · {0}" },
            { "camera_open", "F7 · открыть редактор проекта" },
            { "camera_active_help", "СКМ: двигать · Shift+СКМ: вращать · WASD/стрелки: двигать · колесо: масштаб · Numpad 7/5: вид" },
            { "camera_inactive_help", "F7: редактор Plan/Isometric" },
            { "camera_closed_threat", "Редактор закрыт: рядом враждебное существо." },
            { "camera_closed_damage", "Редактор закрыт: персонаж получил урон." },
            { "draft_selected_summary", "Точка {0}{1} | Y {2:F2} м | ширина {3:F2} / {4:F2} м" },
            { "draft_y_anchored", " · Y закреплена" },
            { "draft_y_auto", " · Y Auto" },
            { "draft_select_flag", "Высота: {0} | выбери флагшток или Ctrl+ЛКМ добавь новый." },
            { "board_plan_missing", "Проект дороги не рассчитан." },
            { "board_safe_place_missing", "Возле точки A нет безопасного места для автоматической таблички." },
            { "board_prefab_missing", "Префаб таблички проекта не зарегистрирован." },
            { "board_creation_failed", "Valheim не создал сохраняемую табличку проекта." },
            { "terrain_apply_rolled_back", "Изменение земли не применено; исходное состояние восстановлено." },
            { "terrain_apply_rollback_failed", "Ошибка применения земли и её отката. Останови тест и сохрани лог." },
            { "terrain_applied", "Земля изменена по сохранённому плану проекта." },
            { "surface_bare_no_paint", "Выбрана чистая земля; отдельное покрытие не требуется." },
            { "surface_apply_rolled_back", "Покрытие не применено; исходная окраска земли восстановлена." },
            { "surface_apply_rollback_failed", "Ошибка покрытия и его отката. Останови тест и сохрани лог." },
            { "clearing_applied", "Полотно очищено от травы. Крупные объекты пока требуют ручной расчистки." },
            { "surface_applied", "Покрытие дороги применено и сохранено." },
            { "terrain_plan_corrupt", "Сохранённый план проекта пуст или повреждён." },
            { "terrain_access_lost", "Доступ к части строительной площадки теперь запрещён." },
            { "terrain_comp_create_failed", "Valheim не создал сохранитель terrain для участка проекта." },
            { "terrain_structure_incompatible", "Структура terrain этой версии Valheim несовместима с EarthWorks." },
            { "terrain_unloaded", "Часть проекта выгружена или terrain-сетка мира изменилась." },
            { "terrain_prepare_failed", "Не удалось подготовить terrain-операцию проекта." },
            { "plan_need_two", "Для дороги нужны минимум две корректные точки." },
            { "plan_geometry_invalid", "Геометрия маршрута некорректна." },
            { "plan_loaded_required", "Весь маршрут и его обочины должны быть загружены." },
            { "plan_water_unsupported", "Эта версия маршрута не прокладывает дорогу через воду." },
            { "plan_endpoint_planes_failed", "Не удалось построить устойчивые плоскости рельефа у A/B. Отключи Alt или перенеси крайнюю точку с обрыва." },
            { "plan_endpoint_short", "Крайний участок маршрута слишком короткий для прилегания A/B." },
            { "plan_grade_exceeded", "Выбранный профиль полотна превышает допустимый продольный уклон." },
            { "plan_turn_tight", "Поворот слишком тесный для выбранной ширины. Уменьши ширину или увеличь радиус кривой." },
            { "plan_hits_water", "Маршрут или его обочина попадает в воду." },
            { "plan_height_limit", "Дорога выходит за разрешённый сервером предел изменения высоты." },
            { "plan_private_area", "Часть маршрута защищена охранным тотемом." },
            { "plan_no_build_zone", "Часть маршрута попадает в запретную зону мира." },
            { "plan_vertex_limit", "Проект превышает безопасный лимит terrain-вершин: {0}." },
            { "plan_no_vertices", "Маршрут не затрагивает загруженные terrain-вершины." },
            { "profile_height_limit", "Профиль выходит за разрешённый сервером предел изменения высоты." },
            { "profile_grade_impossible", "Между закреплёнными высотами невозможно выдержать допустимый уклон." },
            { "profile_failed", "Не удалось рассчитать вертикальный профиль маршрута." }
        };

        public static void Register()
        {
            CustomLocalization localization = LocalizationManager.Instance.GetLocalization();
            string english = "English";
            string russian = "Russian";
            localization.AddTranslation(in english, PrefixTokens(English));
            localization.AddTranslation(in russian, PrefixTokens(Russian));
        }

        public static string Token(string key)
        {
            return "$" + Prefix + key;
        }

        public static string Text(string key, params object[] arguments)
        {
            string fallback = English.TryGetValue(key, out string value) ? value : key;
            string localized = Localization.instance != null
                ? Localization.instance.Localize(Token(key))
                : fallback;
            if (localized == Token(key))
            {
                localized = fallback;
            }
            return arguments == null || arguments.Length == 0
                ? localized
                : string.Format(localized, arguments);
        }

        public static string StateName(RoadDraftState state)
        {
            return Text("state_" + DraftStateKey(state));
        }

        public static string Controls(RoadDraftState state)
        {
            return Text("controls_" + DraftStateKey(state));
        }

        private static string DraftStateKey(RoadDraftState state)
        {
            return state == RoadDraftState.Drawing
                ? "draw"
                : state.ToString().ToLowerInvariant();
        }

        public static string ElevationModeName(RoadElevationMode mode)
        {
            switch (mode)
            {
                case RoadElevationMode.Anchored:
                    return Text("mode_anchored");
                case RoadElevationMode.SingleElevation:
                    return Text("mode_single");
                case RoadElevationMode.UniformGrade:
                    return Text("mode_uniform");
                default:
                    return Text("mode_auto");
            }
        }

        public static string ControlModeName(RouteControlMode mode)
        {
            switch (mode)
            {
                case RouteControlMode.BSpline:
                    return Text("control_bspline");
                case RouteControlMode.Corner:
                    return Text("control_corner");
                case RouteControlMode.Bezier:
                    return Text("control_bezier");
                default:
                    return Text("control_xspline");
            }
        }

        public static string LongitudinalProfileName(RoadLongitudinalProfile profile)
        {
            switch (profile)
            {
                case RoadLongitudinalProfile.Linear:
                    return Text("profile_linear");
                case RoadLongitudinalProfile.SoftEnds:
                    return Text("profile_soft_ends");
                case RoadLongitudinalProfile.Smooth:
                    return Text("profile_smooth");
                default:
                    return Text("profile_linear_joined");
            }
        }

        public static string CameraModeName(RoadCameraMode mode)
        {
            switch (mode)
            {
                case RoadCameraMode.Plan:
                    return Text("camera_plan");
                case RoadCameraMode.Isometric:
                    return Text("camera_isometric");
                default:
                    return Text("camera_player");
            }
        }

        public static string SurfaceName(RoadSurface surface)
        {
            switch (surface)
            {
                case RoadSurface.Paved:
                    return Text("surface_paved");
                default:
                    return Text("surface_bare");
            }
        }

        public static string StageName(RoadProjectStage stage)
        {
            return Text("stage_" + stage.ToString().ToLowerInvariant());
        }

        public static string StageAction(RoadProjectStage stage)
        {
            return Text("stage_action_" + stage.ToString().ToLowerInvariant());
        }

        public static string StageResult(RoadProjectStage stage)
        {
            return Text("stage_result_" + stage.ToString().ToLowerInvariant());
        }

        private static Dictionary<string, string> PrefixTokens(Dictionary<string, string> source)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> token in source)
            {
                result.Add(Prefix + token.Key, token.Value);
            }
            return result;
        }
    }
}
