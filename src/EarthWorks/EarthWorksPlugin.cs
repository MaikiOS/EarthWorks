using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace OstrixMods.EarthWorks
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public sealed partial class EarthWorksPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ostrix.earthworks";
        public const string PluginName = "EarthWorks";
        public const string PluginVersion = "0.6.4";
        internal const string RoadPrefabName = "OstrixEarthWorksRoadProjectTool";
        internal const string BoardPrefabName = "OstrixEarthWorksRoadProjectBoard";

        internal static ConfigEntry<int> MaximumControlPoints;
        internal static ConfigEntry<int> CurveSubdivisions;
        internal static ConfigEntry<int> MaximumVertices;
        internal static ConfigEntry<float> AuthorizedTerrainDelta;
        internal static ConfigEntry<float> MaximumGradePercent;
        internal static ConfigEntry<float> ShoulderWidth;
        internal static ConfigEntry<float> DefaultRoadWidth;
        internal static ConfigEntry<KeyCode> NextStageKey;
        internal static ConfigEntry<KeyCode> EditorCameraKey;
        internal static ManualLogSource Log;
        internal static EarthWorksPlugin Instance;
        internal static int EffectiveMaximumControlPoints =>
            Mathf.Clamp(MaximumControlPoints.Value, 2, 512);
        internal static int EffectiveCurveSubdivisions =>
            Mathf.Clamp(CurveSubdivisions.Value, 2, 64);
        internal static int EffectiveMaximumVertices =>
            Mathf.Clamp(MaximumVertices.Value, 128, 65536);
        internal static float EffectiveTerrainDelta =>
            Mathf.Clamp(AuthorizedTerrainDelta.Value, 0.25f, 100f);
        internal static float EffectiveMaximumGradeRatio =>
            Mathf.Clamp(MaximumGradePercent.Value, 1f, 200f) / 100f;
        internal static float EffectiveShoulderWidth =>
            Mathf.Clamp(ShoulderWidth.Value, 0.5f, 12f);
        internal static float EffectiveDefaultRoadWidth =>
            Mathf.Clamp(DefaultRoadWidth.Value, 2f, 20f);

        private static readonly FieldInfo PlacementGhostField =
            AccessTools.Field(typeof(Player), "m_placementGhost");

        private Harmony harmony;
        private RoadDraftSession session;
        private RoadEditorCamera editorCamera;
        private bool sessionCreationFailed;
        private bool pieceRegistered;
        private bool boardRegistered;
        private Sprite roadIcon;
        private Texture2D roadIconTexture;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle statusStyle;
        private GUIStyle metricsStyle;
        private GUIStyle hintStyle;
        private GUIStyle editorTitleStyle;
        private GUIStyle editorLabelStyle;
        private GUIStyle editorMutedStyle;
        private GUIStyle editorButtonStyle;
        private GUIStyle editorButtonActiveStyle;
        private GUIStyle editorNextStyle;
        private Texture2D editorPanelTexture;
        private bool showCharacters = true;
        private bool showPieces = true;
        private bool showWorldObjects = true;
        private bool showRoadbed = true;
        private bool showDifference = true;
        internal RoadEditorCamera EditorCamera => editorCamera;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            BindConfiguration();
            EarthWorksLocalization.Register();
            editorCamera = new RoadEditorCamera();
            PrefabManager.OnVanillaPrefabsAvailable += RegisterRoadPiece;
            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void Update()
        {
            RoadDraftSession current = EnsureSession();
            Player player = Player.m_localPlayer;
            PollLaboratoryReset(player, current);
            editorCamera?.Update(current, player && IsRoadPiece(player.GetSelectedPiece()));
            current?.Update();
        }

        private void OnGUI()
        {
            if (session == null || string.IsNullOrEmpty(session.Status))
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (!player || !IsRoadPiece(player.GetSelectedPiece()))
            {
                return;
            }
            if (IsDeveloperLab(player) && PlayerZdo(player)?.GetInt("ew_test_panel", 0) == 1)
            {
                return;
            }

            if (panelStyle == null)
            {
                CreateHudStyles();
            }

            if (editorCamera.Active)
            {
                DrawProjectEditor();
                return;
            }

            float width = Mathf.Min(700f, Screen.width - 24f);
            const float height = 168f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, 10f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            string header = EarthWorksLocalization.Text("hud_title") + " · " + session.StateText;
            GUI.Label(
                new Rect(panel.x + 10f, panel.y + 3f, width - 20f, 18f),
                header,
                titleStyle);
            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 21f, width - 24f, 24f),
                session.Status,
                statusStyle);

            string metrics = EarthWorksLocalization.Text(
                "metrics",
                session.PointCount,
                session.SegmentCount,
                session.PreviewLength);
            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 47f, width - 24f, 16f),
                metrics,
                metricsStyle);

            string detail = session.DetailText;
            if (!string.IsNullOrEmpty(detail))
            {
                GUI.Label(
                    new Rect(panel.x + 12f, panel.y + 63f, width - 24f, 32f),
                    detail,
                    metricsStyle);
            }

            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 98f, width - 24f, 16f),
                editorCamera.HudText,
                metricsStyle);

            string controls = session.ControlsText;
            GUI.Label(
                new Rect(panel.x + 10f, panel.y + 114f, width - 20f, 28f),
                controls,
                hintStyle);
            GUI.Label(
                new Rect(panel.x + 10f, panel.y + 143f, width - 20f, 20f),
                editorCamera.ControlsText,
                hintStyle);
        }

        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterRoadPiece;
            session?.Dispose();
            editorCamera?.Dispose();
            harmony?.UnpatchSelf();
            if (roadIcon)
            {
                Destroy(roadIcon);
            }
            if (roadIconTexture)
            {
                Destroy(roadIconTexture);
            }
            if (editorPanelTexture)
            {
                Destroy(editorPanelTexture);
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        internal static bool IsRoadPiece(Piece piece)
        {
            return piece && piece.GetComponent<RoadProjectToolMarker>();
        }

        internal static bool TryGetPlacementPoint(Player player, out Vector3 point)
        {
            point = default(Vector3);
            if (Instance?.editorCamera != null &&
                Instance.editorCamera.TryGetGroundPoint(out point))
            {
                return true;
            }
            if (!player || PlacementGhostField == null)
            {
                return false;
            }

            GameObject ghost = PlacementGhostField.GetValue(player) as GameObject;
            if (!ghost)
            {
                return false;
            }

            point = ghost.transform.position;
            return true;
        }

        internal static bool IsPointerOverEditorUi()
        {
            if (Instance?.editorCamera?.Active != true)
            {
                return false;
            }
            Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return EditorTopRect().Contains(point) || EditorLeftRect().Contains(point) ||
                EditorRightRect().Contains(point) || EditorBottomRect().Contains(point) ||
                Instance.TryEditorManipulatorPanelRect(out Rect floating) && floating.Contains(point);
        }

        private void BindConfiguration()
        {
            MaximumControlPoints = Config.Bind(
                "Road Draft",
                "MaximumControlPoints",
                64,
                "Maximum control points in one road draft.");
            CurveSubdivisions = Config.Bind(
                "Road Draft",
                "CurveSubdivisions",
                12,
                "Preview segments between neighboring control points.");
            MaximumVertices = Config.Bind(
                "Safety",
                "MaximumVertices",
                8192,
                "Maximum terrain vertices in one road project.");
            AuthorizedTerrainDelta = Config.Bind(
                "Server Rules",
                "AuthorizedTerrainDelta",
                8f,
                "Server-authorized terrain deviation from original height. Keep 8 unless a compatible extension is verified.");
            MaximumGradePercent = Config.Bind(
                "Road Geometry",
                "MaximumGradePercent",
                35f,
                "Maximum longitudinal road grade in percent.");
            ShoulderWidth = Config.Bind(
                "Road Geometry",
                "ShoulderWidth",
                2f,
                "Side transition width from the flat roadbed to existing terrain.");
            DefaultRoadWidth = Config.Bind(
                "Road Geometry",
                "DefaultRoadWidth",
                4f,
                "Default full road width.");
            NextStageKey = Config.Bind(
                "Input",
                "NextStageKey",
                KeyCode.G,
                "Advance the editor and create a validated project.");
            EditorCameraKey = Config.Bind(
                "Input",
                "EditorCameraKey",
                KeyCode.F7,
                "Enter or leave the EarthWorks project editor.");
        }

        private RoadDraftSession EnsureSession()
        {
            if (session != null || sessionCreationFailed || !Player.m_localPlayer)
            {
                return session;
            }

            try
            {
                session = new RoadDraftSession();
            }
            catch (Exception exception)
            {
                sessionCreationFailed = true;
                Log.LogError("EarthWorks could not create its road draft preview.");
                Log.LogError(exception);
            }

            return session;
        }

        private void RegisterRoadPiece()
        {
            if (pieceRegistered)
            {
                return;
            }

            bool previousDisable = TerrainOp.m_forceDisableTerrainOps;
            try
            {
                TerrainOp.m_forceDisableTerrainOps = true;
                RegisterProjectBoard();
                if (!roadIcon && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    roadIcon = EarthWorksAssets.LoadRoadIcon(out roadIconTexture);
                }

                PieceConfig config = new PieceConfig
                {
                    Name = EarthWorksLocalization.Token("piece_name"),
                    Description = EarthWorksLocalization.Token("piece_desc"),
                    PieceTable = "_HoePieceTable",
                    Category = PieceCategories.Misc,
                    Icon = roadIcon
                };

                CustomPiece customPiece = new CustomPiece(RoadPrefabName, "path", config);
                GameObject prefab = customPiece.PiecePrefab;
                foreach (TerrainOp terrainOp in prefab.GetComponents<TerrainOp>())
                {
                    DestroyImmediate(terrainOp);
                }
                foreach (TerrainModifier modifier in prefab.GetComponents<TerrainModifier>())
                {
                    DestroyImmediate(modifier);
                }
                foreach (ZNetView netView in prefab.GetComponents<ZNetView>())
                {
                    DestroyImmediate(netView);
                }
                if (!prefab.GetComponent<RoadProjectToolMarker>())
                {
                    prefab.AddComponent<RoadProjectToolMarker>();
                }

                if (!PieceManager.Instance.AddPiece(customPiece))
                {
                    throw new InvalidOperationException("Jotunn rejected the EarthWorks road tool.");
                }

                pieceRegistered = true;
                PrefabManager.OnVanillaPrefabsAvailable -= RegisterRoadPiece;
                Log.LogInfo("EarthWorks road project tool registered in the hoe table.");
            }
            finally
            {
                TerrainOp.m_forceDisableTerrainOps = previousDisable;
            }
        }

        private void RegisterProjectBoard()
        {
            if (boardRegistered)
            {
                return;
            }
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(
                BoardPrefabName,
                "sign");
            if (!prefab)
            {
                throw new InvalidOperationException("Could not clone the vanilla sign for the EarthWorks board.");
            }
            Sign[] signs = prefab.GetComponents<Sign>();
            float hoverOffset = signs.Length > 0 ? signs[0].m_hoverOffset : 0f;
            foreach (Sign sign in signs)
            {
                DestroyImmediate(sign);
            }
            RoadProjectBoard board = prefab.GetComponent<RoadProjectBoard>();
            if (!board)
            {
                board = prefab.AddComponent<RoadProjectBoard>();
            }
            board.SetHoverOffset(hoverOffset);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
            boardRegistered = true;
            Log.LogInfo("EarthWorks persistent road project board registered.");
        }

        internal static bool IsDeveloperLab(Player player)
        {
            return player && ZNet.instance &&
                string.Equals(player.GetPlayerName(), "Test", StringComparison.Ordinal) &&
                string.Equals(ZNet.instance.GetWorldName(), "TerrainRamp_Lab", StringComparison.Ordinal);
        }

        internal static int DeveloperProgress(Player player)
        {
            ZDO zdo = PlayerZdo(player);
            return IsDeveloperLab(player) && zdo != null
                ? Mathf.Clamp(zdo.GetInt("ew_test_progress", 100), 0, 100)
                : 100;
        }

        internal static bool IsSurfaceAvailable(RoadSurface surface)
        {
            if (surface != RoadSurface.Paved)
            {
                return true;
            }
            Player player = Player.m_localPlayer;
            return IsDeveloperLab(player)
                ? DeveloperProgress(player) >= 50
                : player && player.IsMaterialKnown("Stone");
        }

        private void PollLaboratoryReset(Player player, RoadDraftSession current)
        {
            ZDO zdo = PlayerZdo(player);
            if (!IsDeveloperLab(player) || zdo == null)
            {
                return;
            }
            int reset = zdo.GetInt("ew_test_reset", 0);
            if (reset == 0)
            {
                return;
            }
            current?.ResetForLaboratory();
            editorCamera?.DeactivateForLaboratoryReset();
            zdo.Set("ew_test_reset", 0);
        }

        internal static ZDO PlayerZdo(Player player)
        {
            ZNetView view = player ? player.GetComponent<ZNetView>() : null;
            return view && view.IsValid() ? view.GetZDO() : null;
        }

        private static void PollEditorInput()
        {
            EarthWorksPlugin plugin = Instance;
            Player player = Player.m_localPlayer;
            if (!plugin || !player || !IsRoadPiece(player.GetSelectedPiece()) ||
                !IsGameplayInputAvailable())
            {
                return;
            }

            if (plugin.editorCamera?.Active == true && Input.GetKeyDown(KeyCode.Escape))
            {
                plugin.EnsureSession()?.CancelEditorOperation();
                ZInput.ResetButtonStatus("Menu");
                return;
            }

            plugin.EnsureSession()?.HandleEarlyInput();
        }

        internal static bool IsGameplayInputAvailable()
        {
            if (Console.IsVisible() || Menu.IsVisible() || InventoryGui.IsVisible() ||
                TextInput.IsVisible())
            {
                return false;
            }

            Chat chat = Chat.instance;
            return !chat || !chat.HasFocus();
        }

        [HarmonyPatch(typeof(Player), "Update")]
        private static class PollEditorBeforePlayerUpdatePatch
        {
            private static void Prefix()
            {
                // EarthWorks must consume only editor-owned Menu/BuildMenu input before Player.Update.
                PollEditorInput();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static class TryPlacePiecePatch
        {
            private static bool Prefix(Player __instance, Piece piece, ref bool __result)
            {
                if (!IsRoadPiece(piece) || __instance != Player.m_localPlayer)
                {
                    return true;
                }

                if (Instance?.editorCamera?.Active == true)
                {
                    __result = false;
                    return false;
                }

                if ((Instance?.editorCamera == null || !Instance.editorCamera.Active) &&
                    __instance.GetPlacementStatus() != Player.PlacementStatus.Valid)
                {
                    __result = false;
                    return false;
                }

                EarthWorksPlugin plugin = Instance;
                RoadDraftSession currentSession = plugin ? plugin.EnsureSession() : null;
                if (currentSession == null ||
                    !TryGetPlacementPoint(__instance, out Vector3 point))
                {
                    __result = false;
                    return false;
                }

                __result = currentSession.HandlePlacement(point);
                return false;
            }
        }
    }

    internal sealed class RoadProjectToolMarker : MonoBehaviour
    {
    }
}
