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
    public sealed class EarthWorksPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ostrix.earthworks";
        public const string PluginName = "EarthWorks";
        public const string PluginVersion = "0.6.3";
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

        private void CreateHudStyles()
        {
            panelStyle = new GUIStyle(GUI.skin.box);
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.78f, 0.32f);

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = true
            };
            statusStyle.normal.textColor = Color.white;

            metricsStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                wordWrap = true
            };
            metricsStyle.normal.textColor = new Color(0.75f, 0.9f, 0.82f);

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                wordWrap = true
            };
            hintStyle.normal.textColor = new Color(0.72f, 0.78f, 0.82f);

            editorPanelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "EarthWorks_EditorPanel",
                hideFlags = HideFlags.HideAndDontSave
            };
            editorPanelTexture.SetPixel(0, 0, new Color(0.035f, 0.045f, 0.05f, 0.94f));
            editorPanelTexture.Apply();
            panelStyle.normal.background = editorPanelTexture;

            editorTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            editorTitleStyle.normal.textColor = new Color(1f, 0.78f, 0.28f);
            editorLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            editorLabelStyle.normal.textColor = Color.white;
            editorMutedStyle = new GUIStyle(editorLabelStyle) { fontSize = 14 };
            editorMutedStyle.normal.textColor = new Color(0.7f, 0.78f, 0.8f);
            editorButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            editorButtonActiveStyle = new GUIStyle(editorButtonStyle);
            editorButtonActiveStyle.normal.textColor = new Color(1f, 0.78f, 0.25f);
            editorButtonActiveStyle.fontStyle = FontStyle.Bold;
            editorNextStyle = new GUIStyle(editorButtonStyle)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawProjectEditor()
        {
            DrawEditorTop();
            DrawEditorStages();
            DrawEditorInspector();
            DrawEditorManipulatorPanel();
            DrawEditorBottom();
        }

        private void DrawEditorTop()
        {
            Rect panel = EditorTopRect();
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 5f, 210f, 40f), "EARTHWORKS", editorTitleStyle);
            float x = panel.x + 220f;
            EditorChoice(ref x, panel.y + 7f, 88f, "PLAN", editorCamera.Mode == RoadCameraMode.Plan,
                () => editorCamera.SetMode(RoadCameraMode.Plan));
            EditorChoice(ref x, panel.y + 7f, 110f, "ISOMETRIC", editorCamera.Mode == RoadCameraMode.Isometric,
                () => editorCamera.SetMode(RoadCameraMode.Isometric));
            x += 10f;
            EditorChoice(ref x, panel.y + 7f, 92f, EarthWorksLocalization.Text("editor_preview_current"), session.PreviewMode == RoadEditorPreviewMode.Current,
                () => session.SetPreviewMode(RoadEditorPreviewMode.Current));
            EditorChoice(ref x, panel.y + 7f, 92f, EarthWorksLocalization.Text("editor_preview_result"), session.PreviewMode == RoadEditorPreviewMode.Result,
                () => session.SetPreviewMode(RoadEditorPreviewMode.Result));
            EditorChoice(ref x, panel.y + 7f, 102f, EarthWorksLocalization.Text("editor_preview_difference"), session.PreviewMode == RoadEditorPreviewMode.Difference,
                () => session.SetPreviewMode(RoadEditorPreviewMode.Difference));
            x += 12f;
            x = panel.x + 220f;
            float layerY = panel.y + 51f;
            showRoadbed = GUI.Toggle(new Rect(x, layerY, 105f, 30f), showRoadbed, EarthWorksLocalization.Text("editor_layer_roadbed"),
                showRoadbed ? editorButtonActiveStyle : editorButtonStyle);
            x += 105f;
            showDifference = GUI.Toggle(new Rect(x, layerY, 105f, 30f), showDifference, "Cut/Fill",
                showDifference ? editorButtonActiveStyle : editorButtonStyle);
            x += 105f;
            bool gridVisible = GUI.Toggle(new Rect(x, layerY, 100f, 30f), editorCamera.ShowGrid, EarthWorksLocalization.Text("editor_layer_grid"),
                editorCamera.ShowGrid ? editorButtonActiveStyle : editorButtonStyle);
            editorCamera.SetGridVisible(gridVisible);
            x += 100f;
            showCharacters = GUI.Toggle(new Rect(x, layerY, 110f, 30f), showCharacters, EarthWorksLocalization.Text("editor_layer_characters"),
                showCharacters ? editorButtonActiveStyle : editorButtonStyle);
            x += 110f;
            showPieces = GUI.Toggle(new Rect(x, layerY, 105f, 30f), showPieces, EarthWorksLocalization.Text("editor_layer_pieces"),
                showPieces ? editorButtonActiveStyle : editorButtonStyle);
            x += 105f;
            showWorldObjects = GUI.Toggle(new Rect(x, layerY, 140f, 30f), showWorldObjects, EarthWorksLocalization.Text("editor_layer_world"),
                showWorldObjects ? editorButtonActiveStyle : editorButtonStyle);
            x += 148f;
            if (GUI.Button(new Rect(x, layerY, 105f, 30f), EarthWorksLocalization.Text("editor_clean_view"), editorButtonStyle))
            {
                showCharacters = false;
                showPieces = false;
                showWorldObjects = false;
            }
            x += 110f;
            if (GUI.Button(new Rect(x, layerY, 82f, 30f), EarthWorksLocalization.Text("editor_show_all"), editorButtonStyle))
            {
                showCharacters = true;
                showPieces = true;
                showWorldObjects = true;
            }
            session.SetPreviewLayers(showRoadbed, showDifference);
            editorCamera.SetSceneLayers(showCharacters, showPieces, showWorldObjects);
        }

        private void DrawEditorStages()
        {
            Rect panel = EditorLeftRect();
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 30f), EarthWorksLocalization.Text("editor_stages"), editorTitleStyle);
            RoadDraftState[] states =
            {
                RoadDraftState.Drawing,
                RoadDraftState.Geometry,
                RoadDraftState.Surface,
                RoadDraftState.Review
            };
            float y = panel.y + 52f;
            foreach (RoadDraftState item in states)
            {
                string prefix = item == session.State ? "▶ " : (int)item < (int)session.State ? "✓ " : "  ";
                GUIStyle style = item == session.State ? editorButtonActiveStyle : editorLabelStyle;
                GUI.Label(new Rect(panel.x + 16f, y, panel.width - 32f, 38f), prefix + EditorStageName(item), style);
                y += 42f;
            }
            GUI.Label(
                new Rect(panel.x + 14f, panel.yMax - 122f, panel.width - 28f, 104f),
                EarthWorksLocalization.Text("editor_controls"),
                editorMutedStyle);
        }

        private void DrawEditorInspector()
        {
            Rect panel = EditorRightRect();
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 30f), "INSPECTOR", editorTitleStyle);
            float y = panel.y + 50f;
            GUI.Label(new Rect(panel.x + 14f, y, panel.width - 28f, 50f), InspectorSelectionText(), editorLabelStyle);
            y += 58f;
            switch (session.State)
            {
                case RoadDraftState.Drawing:
                    EditorText(panel, ref y, EarthWorksLocalization.Text("editor_draw_help"));
                    EditorText(panel, ref y, editorCamera.PointerText);
                    break;
                case RoadDraftState.Geometry:
                    DrawCombinedEditorInspector(panel, ref y);
                    break;
                case RoadDraftState.Surface:
                    DrawSurfaceInspector(panel, ref y);
                    break;
                case RoadDraftState.Review:
                    DrawReviewInspector(panel, ref y);
                    break;
                default:
                    EditorText(panel, ref y, EarthWorksLocalization.Text("editor_start_help"));
                    break;
            }
            if ((int)session.State >= (int)RoadDraftState.Geometry && session.State != RoadDraftState.Idle)
            {
                y = Mathf.Max(y + 8f, panel.yMax - 190f);
                EditorText(panel, ref y, EarthWorksLocalization.Text("editor_profile", EarthWorksLocalization.LongitudinalProfileName(session.LongitudinalProfile)));
                if (GUI.Button(new Rect(panel.x + 14f, y, panel.width - 28f, 36f), EarthWorksLocalization.Text("editor_cycle_profile"), editorButtonStyle))
                {
                    session.CycleRoadbedProfileFromEditor();
                }
                y += 42f;
                string fit = EarthWorksLocalization.Text(session.FitEndpointPlanes ? "editor_fit_on" : "editor_fit_off");
                if (GUI.Button(new Rect(panel.x + 14f, y, panel.width - 28f, 36f), fit + " (Alt)",
                    session.FitEndpointPlanes ? editorButtonActiveStyle : editorButtonStyle))
                {
                    session.ToggleEndpointFit();
                }
            }
        }

        private void DrawCombinedEditorInspector(Rect panel, ref float y)
        {
            if (session.SelectedPointIndex < 0)
            {
                EditorText(panel, ref y, EarthWorksLocalization.Text("editor_flag_help"));
            }
            else
            {
                EditorText(panel, ref y, EarthWorksLocalization.Text("editor_local_help"));
            }
            EditorText(panel, ref y, EarthWorksLocalization.Text("editor_height_profile"));
            string[] labels = { EarthWorksLocalization.Text("mode_auto"), EarthWorksLocalization.Text("mode_anchored"), EarthWorksLocalization.Text("mode_single"), EarthWorksLocalization.Text("mode_uniform") };
            RoadElevationMode[] modes =
            {
                RoadElevationMode.Automatic,
                RoadElevationMode.Anchored,
                RoadElevationMode.SingleElevation,
                RoadElevationMode.UniformGrade
            };
            for (int i = 0; i < labels.Length; ++i)
            {
                if (GUI.Button(new Rect(panel.x + 14f + (i % 2) * 137f, y + (i / 2) * 38f, 130f, 32f),
                    labels[i], session.ElevationMode == modes[i] ? editorButtonActiveStyle : editorButtonStyle))
                {
                    session.SetElevationMode(modes[i]);
                }
            }
            y += 82f;
            if (session.SelectedPointIndex < 0)
            {
                DrawWidthInspector(panel, ref y);
            }
            else
            {
                EditorText(panel, ref y, EarthWorksLocalization.Text("editor_point_summary",
                    session.SelectedElevation, session.LeftWidth, session.RightWidth));
            }
        }

        private void DrawEditorManipulatorPanel()
        {
            if (!TryEditorManipulatorPanelRect(out Rect panel))
            {
                return;
            }
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 10f, panel.y + 5f, panel.width - 20f, 24f),
                EarthWorksLocalization.Text("editor_point", session.SelectedPointIndex + 1), editorTitleStyle);
            float y = panel.y + 31f;
            float buttonWidth = (panel.width - 25f) * 0.5f;
            if (GUI.Button(new Rect(panel.x + 8f, y, buttonWidth, 32f), EarthWorksLocalization.Text("editor_add_point"), editorButtonStyle))
            {
                session.InsertAfterSelected();
            }
            if (GUI.Button(new Rect(panel.x + 13f + buttonWidth, y, buttonWidth, 32f), EarthWorksLocalization.Text("editor_remove"), editorButtonStyle))
            {
                session.RemoveSelectedPoint();
            }
            y += 36f;
            string[] labels = { "X", "B", EarthWorksLocalization.Text("control_bezier"), EarthWorksLocalization.Text("control_corner") };
            RouteControlMode[] modes = { RouteControlMode.XSpline, RouteControlMode.BSpline, RouteControlMode.Bezier, RouteControlMode.Corner };
            float modeWidth = (panel.width - 20f) / 4f;
            for (int i = 0; i < labels.Length; ++i)
            {
                if (GUI.Button(new Rect(panel.x + 6f + i * modeWidth, y, modeWidth - 3f, 30f), labels[i],
                    session.SelectedControlMode == modes[i] ? editorButtonActiveStyle : editorButtonStyle))
                {
                    session.SetSelectedControlMode(modes[i]);
                }
            }
            y += 34f;
            if (GUI.Button(new Rect(panel.x + 8f, y, buttonWidth, 32f),
                EarthWorksLocalization.Text(session.SelectedElevationAnchored ? "editor_height_exact" : "editor_height_auto"),
                session.SelectedElevationAnchored ? editorButtonActiveStyle : editorButtonStyle))
            {
                session.ToggleSelectedHeightAnchor();
            }
            if (GUI.Button(new Rect(panel.x + 13f + buttonWidth, y, buttonWidth, 32f), EarthWorksLocalization.Text("editor_exact_y"), editorButtonStyle))
            {
                session.RequestExactHeightFromEditor();
            }
            y += 35f;
            GUI.Label(new Rect(panel.x + 8f, y, panel.width - 16f, 22f),
                EarthWorksLocalization.Text("editor_smoothing", Mathf.RoundToInt(session.SelectedSmoothing * 100f)),
                editorMutedStyle);
            y += 20f;
            bool controlsEnabled = GUI.enabled;
            GUI.enabled = controlsEnabled && session.SelectedControlMode == RouteControlMode.XSpline;
            float smoothing = GUI.HorizontalSlider(
                new Rect(panel.x + 10f, y, panel.width - 20f, 20f),
                session.SelectedSmoothing,
                0f,
                1f);
            if (Mathf.Abs(smoothing - session.SelectedSmoothing) > 0.001f)
            {
                session.SetSelectedSmoothing(smoothing);
            }
            GUI.enabled = controlsEnabled;
            y += 24f;
            if (session.SelectedPointIndex < session.SegmentCount && GUI.Button(
                new Rect(panel.x + 8f, y, panel.width - 16f, 30f),
                EarthWorksLocalization.Text(session.SelectedSegmentIsStraight ? "editor_next_straight" : "editor_next_curved"),
                session.SelectedSegmentIsStraight ? editorButtonActiveStyle : editorButtonStyle))
            {
                session.ToggleSelectedStraightSegment();
            }
            y += 34f;
            GUI.Label(new Rect(panel.x + 8f, y, panel.width - 16f, 31f),
                EarthWorksLocalization.Text("editor_drag_handles"),
                editorMutedStyle);
        }

        private bool TryEditorManipulatorPanelRect(out Rect panel)
        {
            panel = default(Rect);
            if (session == null || session.State != RoadDraftState.Geometry ||
                !session.TryGetEditorManipulators(out RoadEditorManipulators handles) ||
                !editorCamera.TryWorldToGuiPoint(handles.Center, out Vector2 point))
            {
                return false;
            }
            const float width = 270f;
            const float height = 249f;
            float x = Mathf.Clamp(point.x - width * 0.5f, EditorLeftRect().xMax + 8f, EditorRightRect().xMin - width - 8f);
            float below = point.y + 58f;
            float above = point.y - height - 58f;
            float y = below + height < EditorBottomRect().yMin
                ? below
                : Mathf.Max(EditorTopRect().yMax + 8f, above);
            panel = new Rect(x, y, width, height);
            return true;
        }

        private void DrawWidthInspector(Rect panel, ref float y)
        {
            EditorText(panel, ref y, EarthWorksLocalization.Text(session.SelectedPointIndex >= 0 ? "editor_width_point" : "editor_width_route"));
            EditorText(panel, ref y, EarthWorksLocalization.Text("editor_left", session.LeftWidth));
            EditorStepper(panel, ref y, "-1", "-0.25", "+0.25", "+1", amount => session.AdjustWidth(amount, 0f));
            EditorText(panel, ref y, EarthWorksLocalization.Text("editor_right", session.RightWidth));
            EditorStepper(panel, ref y, "-1", "-0.25", "+0.25", "+1", amount => session.AdjustWidth(0f, amount));
            if (session.SelectedPointIndex >= 0 && GUI.Button(
                new Rect(panel.x + 14f, y, panel.width - 28f, 38f),
                EarthWorksLocalization.Text("editor_reset_width"),
                editorButtonStyle))
            {
                session.ResetSelectedWidth();
            }
        }

        private void DrawSurfaceInspector(Rect panel, ref float y)
        {
            EditorText(panel, ref y, EarthWorksLocalization.Text(session.SelectedSegmentIndex >= 0 ? "editor_surface_segment" : "editor_surface_route"));
            if (GUI.Button(new Rect(panel.x + 14f, y, 130f, 42f), EarthWorksLocalization.Text("surface_bare"),
                session.Surface == RoadSurface.Bare ? editorButtonActiveStyle : editorButtonStyle))
            {
                session.SetSurface(RoadSurface.Bare);
            }
            if (GUI.Button(new Rect(panel.x + 151f, y, 130f, 42f), EarthWorksLocalization.Text("surface_paved"),
                session.Surface == RoadSurface.Paved ? editorButtonActiveStyle : editorButtonStyle))
            {
                session.SetSurface(RoadSurface.Paved);
            }
        }

        private void DrawReviewInspector(Rect panel, ref float y)
        {
            RoadBuildPlan plan = session.CurrentPlan;
            if (plan == null)
            {
                EditorText(panel, ref y, EarthWorksLocalization.Text("editor_plan_pending"));
                return;
            }
            EditorText(panel, ref y, EarthWorksLocalization.Text(plan.IsValid ? "editor_plan_ready" : "editor_plan_errors"));
            EditorText(panel, ref y, EarthWorksLocalization.Text("editor_plan_metrics",
                plan.Edits.Count, plan.CutVolume, plan.FillVolume, plan.MaximumGradePercent));
            if (!plan.IsValid)
            {
                EditorText(panel, ref y, plan.InvalidReason);
            }
        }

        private void DrawEditorBottom()
        {
            Rect panel = EditorBottomRect();
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, panel.width - 450f, panel.height - 16f),
                session.Status + "\n" + session.DetailText,
                editorLabelStyle);
            if (GUI.Button(new Rect(panel.xMax - 424f, panel.y + 11f, 130f, panel.height - 22f), EarthWorksLocalization.Text("editor_back"), editorButtonStyle))
            {
                session.GoBackFromEditor();
            }
            string next;
            switch (session.State)
            {
                case RoadDraftState.Drawing: next = EarthWorksLocalization.Text("editor_next_draw"); break;
                case RoadDraftState.Geometry: next = EarthWorksLocalization.Text("editor_next_surface"); break;
                case RoadDraftState.Surface: next = EarthWorksLocalization.Text("editor_next_review"); break;
                case RoadDraftState.Review: next = EarthWorksLocalization.Text("editor_next_create"); break;
                default: next = EarthWorksLocalization.Text("editor_next_continue"); break;
            }
            bool enabled = GUI.enabled;
            GUI.enabled = session.HasActiveDraft &&
                (session.State != RoadDraftState.Review || session.CurrentPlan?.IsValid == true);
            if (GUI.Button(new Rect(panel.xMax - 282f, panel.y + 11f, 268f, panel.height - 22f), next, editorNextStyle))
            {
                session.AdvanceFromEditor();
            }
            GUI.enabled = enabled;
        }

        private void EditorStepper(Rect panel, ref float y, string a, string b, string c, string d, Action<float> action)
        {
            string[] labels = { a, b, c, d };
            float[] values = { -1f, -0.25f, 0.25f, 1f };
            for (int i = 0; i < labels.Length; ++i)
            {
                if (GUI.Button(new Rect(panel.x + 14f + i * 68f, y, 62f, 34f), labels[i], editorButtonStyle))
                {
                    action(values[i]);
                }
            }
            y += 40f;
        }

        private void EditorText(Rect panel, ref float y, string text)
        {
            float height = editorLabelStyle.CalcHeight(new GUIContent(text), panel.width - 28f) + 5f;
            GUI.Label(new Rect(panel.x + 14f, y, panel.width - 28f, height), text, editorLabelStyle);
            y += height;
        }

        private void EditorChoice(ref float x, float y, float width, string text, bool active, Action action)
        {
            if (GUI.Button(new Rect(x, y, width, 36f), text, active ? editorButtonActiveStyle : editorButtonStyle))
            {
                action();
            }
            x += width + 5f;
        }

        private string InspectorSelectionText()
        {
            if (session.SelectedPointIndex >= 0)
            {
                return EarthWorksLocalization.Text("editor_point", session.SelectedPointIndex + 1);
            }
            if (session.SelectedSegmentIndex >= 0)
            {
                return EarthWorksLocalization.Text("editor_segment", session.SelectedSegmentIndex + 1);
            }
            return EarthWorksLocalization.Text("editor_nothing_selected");
        }

        private static string EditorStageName(RoadDraftState value)
        {
            switch (value)
            {
                case RoadDraftState.Drawing: return EarthWorksLocalization.Text("editor_stage_route");
                case RoadDraftState.Geometry: return EarthWorksLocalization.Text("editor_stage_edit");
                case RoadDraftState.Surface: return EarthWorksLocalization.Text("editor_stage_surface");
                case RoadDraftState.Review: return EarthWorksLocalization.Text("editor_stage_review");
                default: return EarthWorksLocalization.Text("editor_stage_new");
            }
        }

        private static Rect EditorTopRect() => new Rect(10f, 10f, Screen.width - 20f, 88f);
        private static Rect EditorLeftRect() => new Rect(10f, 108f, 230f, Screen.height - 202f);
        private static Rect EditorRightRect() => new Rect(Screen.width - 320f, 108f, 310f, Screen.height - 202f);
        private static Rect EditorBottomRect() => new Rect(10f, Screen.height - 84f, Screen.width - 20f, 74f);

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
