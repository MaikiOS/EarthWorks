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
            { "state_height", "HEIGHT" },
            { "state_width", "WIDTH" },
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
            { "stage_height", "Height profile. Middle mouse cycles the profile mode." },
            { "stage_width", "Road width. Select a point for a local override." },
            { "stage_surface", "Road surface. Select a curved segment and choose its surface in Inspector." },
            { "height_help", "LMB selects a point; LMB on empty terrain clears selection. F sets an exact elevation anchor; RMB resets it." },
            { "height_point_selected", "Point {0} selected: {1}." },
            { "height_selection_cleared", "Point selection cleared. The calculated profile is unchanged." },
            { "height_select_first", "Select a control point first, or choose Single elevation." },
            { "height_mode_changed", "Height profile mode changed." },
            { "height_changed", "Height changed." },
            { "height_invalid", "That height is not a valid number." },
            { "height_exact_set", "Exact height set." },
            { "height_reset_auto", "Point height returned to Auto." },
            { "height_anchor_on", "Exact elevation anchor set. The road is smoothed between fixed anchors." },
            { "height_anchor_off", "Exact elevation anchor removed. This point is automatic again." },
            { "height_input_unavailable", "Valheim text input is unavailable." },
            { "height_input_title", "Exact road height" },
            { "width_help", "LMB selects a local point; LMB away from points clears selection and edits the whole route." },
            { "width_point_selected", "Local point width selected. LMB away from points returns to the whole route." },
            { "width_default_selected", "Point selection cleared. The wheel now edits the whole route." },
            { "width_changed", "Road width changed." },
            { "width_reset_default", "Point width returned to the route default." },
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
            { "height_detail_point", "Mode: {0} | Point {1}: {2} | Target {3:F2} m" },
            { "height_detail_uniform", "Mode: {0} | A {1:F2} m -> B {2:F2} m | Grade {3:F1}%" },
            { "height_detail_single", "Mode: {0} | Level {1:F2} m | Source: {2}" },
            { "height_detail_mode", "Mode: {0}" },
            { "height_locked", "LOCKED" },
            { "height_auto", "AUTO" },
            { "height_source_point", "point {0}" },
            { "height_source_manual", "manual value" },
            { "width_detail", "Target: {0} | Left {1:F2} m | Right {2:F2} m" },
            { "surface_detail", "Target: {0} | Here: {1} | Route default: {2}" },
            { "review_detail", "Vertices {0} | Cut {1:F0} m³ | Fill {2:F0} m³ | Grade {3:F1}%" },
            { "controls_idle", "[LMB] start" },
            { "controls_draw", "[LMB] point  [double LMB/G] geometry  [RMB] remove" },
            { "controls_geometry", "[LMB] select/drag  [Ctrl+LMB] add  [Delete] remove  [F/H] exact height  [G] surface  [RMB] cancel/back" },
            { "controls_height", "[LMB] point/clear  [F] anchor  [wheel/H] height  [MMB] elevation  [P] roadbed  [Alt] fit A/B  [G] width" },
            { "controls_width", "[LMB] point/route  [wheel] width  [Shift/Ctrl+wheel] side  [P] roadbed  [Alt] fit A/B  [G] surface" },
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
            { "stage_already_completed", "This road project is already complete." }
        };

        private static readonly Dictionary<string, string> Russian = new Dictionary<string, string>
        {
            { "piece_name", "Маршрут" },
            { "piece_desc", "Спроектировать и построить сохраняемую дорогу на terrain Valheim." },
            { "hud_title", "EARTHWORKS · МАРШРУТ" },
            { "state_idle", "ГОТОВ" },
            { "state_draw", "ТОЧКИ МАРШРУТА" },
            { "state_geometry", "РЕДАКТИРОВАНИЕ" },
            { "state_height", "ВЫСОТА" },
            { "state_width", "ШИРИНА" },
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
            { "stage_height", "Профиль высоты. СКМ переключает режим." },
            { "stage_width", "Ширина дороги. Выбери точку для локального переопределения." },
            { "stage_surface", "Покрытие дороги. Выбери участок кривой и его покрытие в Inspector." },
            { "height_help", "ЛКМ выбирает точку; ЛКМ в стороне снимает выбор. F ставит точный высотный флаг; ПКМ сбрасывает его." },
            { "height_point_selected", "Выбрана точка {0}: {1}." },
            { "height_selection_cleared", "Выбор точки снят. Рассчитанный профиль не изменён." },
            { "height_select_first", "Сначала выбери точку или режим «Одна высота»." },
            { "height_mode_changed", "Режим профиля высоты изменён." },
            { "height_changed", "Высота изменена." },
            { "height_invalid", "Введено некорректное число высоты." },
            { "height_exact_set", "Точная высота установлена." },
            { "height_reset_auto", "Высота точки возвращена в Auto." },
            { "height_anchor_on", "Поставлен точный высотный флаг. Между закреплёнными точками дорога сглаживается автоматически." },
            { "height_anchor_off", "Точный высотный флаг снят. Высота точки снова рассчитывается автоматически." },
            { "height_input_unavailable", "Окно числового ввода Valheim недоступно." },
            { "height_input_title", "Точная высота дороги" },
            { "width_help", "ЛКМ выбирает локальную точку; ЛКМ в стороне снимает выбор и включает весь маршрут." },
            { "width_point_selected", "Выбрана локальная ширина точки. ЛКМ в стороне вернёт весь маршрут." },
            { "width_default_selected", "Выбор точки снят. Колесо теперь меняет ширину всего маршрута." },
            { "width_changed", "Ширина дороги изменена." },
            { "width_reset_default", "Ширина точки возвращена к общей." },
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
            { "height_detail_point", "Режим: {0} | Точка {1}: {2} | Цель {3:F2} м" },
            { "height_detail_uniform", "Режим: {0} | A {1:F2} м -> B {2:F2} м | Уклон {3:F1}%" },
            { "height_detail_single", "Режим: {0} | Уровень {1:F2} м | Источник: {2}" },
            { "height_detail_mode", "Режим: {0}" },
            { "height_locked", "ЗАКРЕПЛЕНА" },
            { "height_auto", "AUTO" },
            { "height_source_point", "точка {0}" },
            { "height_source_manual", "ручное значение" },
            { "width_detail", "Цель: {0} | Слева {1:F2} м | Справа {2:F2} м" },
            { "surface_detail", "Цель: {0} | Здесь: {1} | Весь маршрут: {2}" },
            { "review_detail", "Вершин {0} | Срез {1:F0} м³ | Насыпь {2:F0} м³ | Уклон {3:F1}%" },
            { "controls_idle", "[ЛКМ] начать" },
            { "controls_draw", "[ЛКМ] точка  [двойной ЛКМ/G] геометрия  [ПКМ] убрать" },
            { "controls_geometry", "[ЛКМ] выбрать/тянуть  [Ctrl+ЛКМ] добавить  [Delete] удалить  [F/H] точная Y  [G] покрытие  [ПКМ] отмена/назад" },
            { "controls_height", "[ЛКМ] точка/снять  [F] флаг  [колесо/H] высота  [СКМ] режим высоты  [P] полотно  [Alt] A/B  [G] ширина" },
            { "controls_width", "[ЛКМ] точка/маршрут  [колесо] ширина  [Shift/Ctrl+колесо] сторона  [P] полотно  [Alt] A/B  [G] покрытие" },
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
            { "stage_already_completed", "Этот проект дороги уже завершён." }
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
            return arguments == null || arguments.Length == 0
                ? localized
                : string.Format(localized, arguments);
        }

        public static string StateName(RoadDraftState state)
        {
            return Text("state_" + state.ToString().ToLowerInvariant());
        }

        public static string Controls(RoadDraftState state)
        {
            return Text("controls_" + state.ToString().ToLowerInvariant());
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
