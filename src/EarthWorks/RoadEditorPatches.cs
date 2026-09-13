using HarmonyLib;

namespace OstrixMods.EarthWorks
{
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    internal static class RoadEditorCameraPatch
    {
        private static void Postfix(GameCamera __instance)
        {
            EarthWorksPlugin.Instance?.EditorCamera?.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    internal static class RoadEditorMouseCapturePatch
    {
        private static bool Prefix()
        {
            if (EarthWorksPlugin.Instance?.EditorCamera?.Active != true)
            {
                return true;
            }
            RoadEditorCamera.ReleaseCursor();
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class RoadEditorPlayerInputPatch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (__instance == Player.m_localPlayer &&
                EarthWorksPlugin.Instance?.EditorCamera?.Active == true)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnDamaged")]
    internal static class RoadEditorDamagePatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                EarthWorksPlugin.Instance?.EditorCamera?.ExitForDamage();
            }
        }
    }
}

