using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using OstrixMods.EarthWorks.Geometry;

namespace OstrixMods.EarthWorks
{
    internal static class EarthWorksLocalization
    {
        private const string Prefix = "earthworks_";
        private static Dictionary<string, string> English;
        private static Dictionary<string, string> Russian;
        private static bool loaded;

        public static void Register()
        {
            EnsureLoaded();
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
            EnsureLoaded();
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

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }
            English = EarthWorksTranslationCatalog.LoadBuiltIn("English");
            Russian = EarthWorksTranslationCatalog.LoadBuiltIn("Russian");
            ApplyExternalOverrides("English", English);
            ApplyExternalOverrides("Russian", Russian);
            loaded = true;
        }

        private static void ApplyExternalOverrides(
            string language,
            Dictionary<string, string> translations)
        {
            try
            {
                EarthWorksTranslationCatalog.ApplyExternalOverrides(language, translations);
            }
            catch (Exception exception)
            {
                EarthWorksPlugin.Log?.LogWarning(
                    "Ignored invalid EarthWorks " + language +
                    " translations: " + exception.Message);
            }
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
