using System;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    public sealed partial class EarthWorksPlugin
    {
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

    }
}

