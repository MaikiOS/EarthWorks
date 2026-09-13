using System;
using System.Collections.Generic;
using System.Globalization;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal enum RoadDraftState
    {
        Idle,
        Drawing,
        Geometry,
        Height,
        Width,
        Surface,
        Review
    }

    internal sealed class RoadDraftSession : TextReceiver
    {
        private const float FinishDoubleClickWindow = 0.35f;
        private readonly List<RoadDraftPoint> points = new List<RoadDraftPoint>();
        private readonly List<bool> straightSegments = new List<bool>();
        private readonly List<int> segmentSurfaceOverrides = new List<int>();
        private readonly RoadDraftPreview preview = new RoadDraftPreview();
        private readonly RoadPlanPreview planPreview = new RoadPlanPreview();
        private RoadDraftState state;
        private int selectedPointIndex = -1;
        private int selectedSegmentIndex = -1;
        private RoadSelectionKind selectionKind;
        private int lastEarlyInputFrame = -1;
        private float lastPointClickTime = -10f;
        private float statusHoldUntil;
        private string status = string.Empty;
        private double previewLength;
        private Vector3 cursor;
        private bool hasCursor;
        private RoadElevationMode elevationMode;
        private RoadLongitudinalProfile longitudinalProfile;
        private bool fitEndpointPlanes;
        private float singleElevation;
        private float defaultLeftWidth;
        private float defaultRightWidth;
        private RoadSurface defaultSurface;
        private RoadBuildPlan currentPlan;
        private bool awaitingExactHeight;
        private int singleElevationSourceIndex;
        private RoadDraftPoint dragOriginal;
        private Vector3 dragOffset;
        private RoadEditorPreviewMode previewMode = RoadEditorPreviewMode.Result;
        private bool showRoadbed = true;
        private bool showDifference = true;

        public RoadDraftSession()
        {
            float halfWidth = EarthWorksPlugin.EffectiveDefaultRoadWidth * 0.5f;
            defaultLeftWidth = halfWidth;
            defaultRightWidth = halfWidth;
            elevationMode = RoadElevationMode.UniformGrade;
            longitudinalProfile = RoadLongitudinalProfile.LinearJoined;
            fitEndpointPlanes = false;
        }

        public RoadDraftState State => state;
        public string Status => status;
        public int PointCount => points.Count;
        public int SegmentCount => Mathf.Max(0, points.Count - 1);
        public double PreviewLength => previewLength;
        public bool HasActiveDraft => points.Count > 0;
        public RoadBuildPlan CurrentPlan => currentPlan;
        public int SelectedPointIndex => selectedPointIndex;
        public int SelectedSegmentIndex => selectedSegmentIndex;
        public RouteControlMode SelectedControlMode => selectedPointIndex >= 0
            ? points[selectedPointIndex].Mode
            : RouteControlMode.XSpline;
        public float SelectedSmoothing => selectedPointIndex >= 0
            ? points[selectedPointIndex].Smoothing
            : 0.5f;
        public bool SelectedElevationAnchored => selectedPointIndex >= 0 &&
            points[selectedPointIndex].ElevationAnchored;
        public float SelectedElevation => selectedPointIndex >= 0
            ? SelectedTargetElevation()
            : singleElevation;
        public float LeftWidth => CurrentLeftWidth;
        public float RightWidth => CurrentRightWidth;
        public RoadElevationMode ElevationMode => elevationMode;
        public RoadSurface Surface => CurrentSurface;
        public RoadLongitudinalProfile LongitudinalProfile => longitudinalProfile;
        public bool FitEndpointPlanes => fitEndpointPlanes;
        public RoadEditorPreviewMode PreviewMode => previewMode;
        public string StateText => EarthWorksLocalization.StateName(state);
        public string ControlsText => EarthWorksLocalization.Controls(state);

        public bool TryGetCameraFocus(out Vector3 result)
        {
            if (points.Count == 0)
            {
                result = Player.m_localPlayer ? Player.m_localPlayer.transform.position : Vector3.zero;
                return Player.m_localPlayer;
            }
            result = Vector3.zero;
            foreach (RoadDraftPoint point in points)
            {
                result += point.Position;
            }
            result /= points.Count;
            return true;
        }

        public string DetailText
        {
            get
            {
                switch (state)
                {
                    case RoadDraftState.Geometry:
                        return selectedPointIndex >= 0
                            ? WithRoadbed(
                                "Точка " + (selectedPointIndex + 1) +
                                (points[selectedPointIndex].ElevationAnchored ? " · Y закреплена" : " · Y Auto") +
                                " | Y " + SelectedTargetElevation().ToString("F2") + " м" +
                                " | ширина " + CurrentLeftWidth.ToString("F2") +
                                " / " + CurrentRightWidth.ToString("F2") + " м")
                            : WithRoadbed("Высота: " + EarthWorksLocalization.ElevationModeName(elevationMode) +
                                " | выбери флагшток или Ctrl+ЛКМ добавь новый.");
                    case RoadDraftState.Height:
                        if (selectedPointIndex >= 0)
                        {
                            return WithRoadbed(EarthWorksLocalization.Text(
                                "height_detail_point",
                                EarthWorksLocalization.ElevationModeName(elevationMode),
                                selectedPointIndex + 1,
                                EarthWorksLocalization.Text(
                                    points[selectedPointIndex].ElevationAnchored ? "height_locked" : "height_auto"),
                                SelectedTargetElevation()));
                        }
                        if (elevationMode == RoadElevationMode.UniformGrade)
                        {
                            return WithRoadbed(EarthWorksLocalization.Text(
                                "height_detail_uniform",
                                EarthWorksLocalization.ElevationModeName(elevationMode),
                                points[0].Elevation,
                                points[points.Count - 1].Elevation,
                                currentPlan?.MaximumGradePercent ?? 0f));
                        }
                        if (elevationMode == RoadElevationMode.SingleElevation)
                        {
                            string source = singleElevationSourceIndex >= 0
                                ? EarthWorksLocalization.Text("height_source_point", singleElevationSourceIndex + 1)
                                : EarthWorksLocalization.Text("height_source_manual");
                            return WithRoadbed(EarthWorksLocalization.Text(
                                "height_detail_single",
                                EarthWorksLocalization.ElevationModeName(elevationMode),
                                singleElevation,
                                source));
                        }
                        return WithRoadbed(EarthWorksLocalization.Text(
                            "height_detail_mode",
                            EarthWorksLocalization.ElevationModeName(elevationMode)));
                    case RoadDraftState.Width:
                        return WithRoadbed(PlanWarning(EarthWorksLocalization.Text(
                            "width_detail",
                            selectedPointIndex >= 0 ? (selectedPointIndex + 1).ToString() : EarthWorksLocalization.Text("whole_route"),
                            CurrentLeftWidth,
                            CurrentRightWidth)));
                    case RoadDraftState.Surface:
                        return WithRoadbed(PlanWarning(EarthWorksLocalization.Text(
                            "surface_detail",
                            selectedSegmentIndex >= 0 ? (selectedSegmentIndex + 1).ToString() : EarthWorksLocalization.Text("whole_route"),
                            EarthWorksLocalization.SurfaceName(CurrentSurface),
                            EarthWorksLocalization.SurfaceName(defaultSurface))));
                    case RoadDraftState.Review:
                        return currentPlan == null
                            ? string.Empty
                            : WithRoadbed(EarthWorksLocalization.Text(
                                "review_detail",
                                currentPlan.Edits.Count,
                                currentPlan.CutVolume,
                                currentPlan.FillVolume,
                                currentPlan.MaximumGradePercent));
                    default:
                        return string.Empty;
                }
            }
        }

        public void Update()
        {
            Player player = Player.m_localPlayer;
            if (!player)
            {
                ResetSilently();
                return;
            }
            if (!EarthWorksPlugin.IsRoadPiece(player.GetSelectedPiece()))
            {
                preview.Hide();
                planPreview.Hide();
                return;
            }

            hasCursor = EarthWorksPlugin.TryGetPlacementPoint(player, out Vector3 placement) &&
                RoadTerrain.TrySnapToVertex(placement, out cursor);
            if (state == RoadDraftState.Idle)
            {
                preview.Hide();
                planPreview.Hide();
                previewLength = 0.0;
                SetPassiveStatus("click_start");
                return;
            }

            if (state == RoadDraftState.Review)
            {
                RenderDraft(points, true);
                RenderTerrainPreview();
                if (Time.time >= statusHoldUntil)
                {
                    status = currentPlan != null && currentPlan.IsValid
                        ? EarthWorksLocalization.Text("review_ready")
                        : currentPlan?.InvalidReason ?? EarthWorksLocalization.Text("review_invalid");
                }
                return;
            }
            if ((int)state >= (int)RoadDraftState.Geometry && currentPlan == null)
            {
                currentPlan = CalculatePlan(false);
            }
            RenderTerrainPreview();

            if (!hasCursor)
            {
                RenderDraft(points, state != RoadDraftState.Drawing);
                SetPassiveStatus("aim_loaded");
                return;
            }

            List<RoadDraftPoint> display = ClonePoints(points);
            int committed = points.Count;
            if (state == RoadDraftState.Drawing &&
                !SameHorizontal(points[points.Count - 1].Position, cursor))
            {
                display.Add(new RoadDraftPoint(cursor));
            }
            else if (state == RoadDraftState.Geometry && selectedPointIndex >= 0)
            {
                ApplyLiveSelection(display[selectedPointIndex], cursor);
            }

            if (elevationMode == RoadElevationMode.SingleElevation &&
                (int)state >= (int)RoadDraftState.Geometry)
            {
                foreach (RoadDraftPoint point in display)
                {
                    point.Position.y = singleElevation;
                }
            }

            RenderDraft(display, state != RoadDraftState.Drawing, committed);
            SetPassiveStatus(DefaultStatusKey());
        }

        public void HandleEarlyInput()
        {
            if (lastEarlyInputFrame == Time.frameCount)
            {
                return;
            }
            lastEarlyInputFrame = Time.frameCount;
            Player player = Player.m_localPlayer;
            if (!player || !EarthWorksPlugin.IsRoadPiece(player.GetSelectedPiece()))
            {
                return;
            }
            if (EarthWorksPlugin.Instance?.EditorCamera?.Active == true)
            {
                return;
            }
            RefreshCursor(player);

            if (HasActiveDraft && Input.GetMouseButtonDown(1))
            {
                CancelOrGoBack();
                ZInput.ResetButtonStatus("BuildMenu");
            }
            if (HasActiveDraft && Input.GetKeyDown(EarthWorksPlugin.NextStageKey.Value))
            {
                Advance();
            }
            if (HasActiveDraft && Input.GetMouseButtonDown(2))
            {
                HandleMiddleClick();
            }
            if (HasActiveDraft && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
            {
                HandleScroll(Mathf.Sign(Input.mouseScrollDelta.y));
            }
            if (HasActiveDraft && state == RoadDraftState.Height && Input.GetKeyDown(KeyCode.H))
            {
                RequestExactHeight();
            }
            if (HasActiveDraft && state == RoadDraftState.Height && Input.GetKeyDown(KeyCode.F))
            {
                ToggleHeightAnchor();
            }
            if (HasActiveDraft && (int)state >= (int)RoadDraftState.Height &&
                Input.GetKeyDown(KeyCode.P))
            {
                CycleLongitudinalProfile();
            }
            if (HasActiveDraft && (int)state >= (int)RoadDraftState.Height &&
                (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt)))
            {
                fitEndpointPlanes = !fitEndpointPlanes;
                RecalculateRoadbed("endpoint_fit_changed");
            }
        }

        public bool HandlePlacement(Vector3 placement)
        {
            if (!RoadTerrain.TrySnapToVertex(placement, out Vector3 snapped))
            {
                HoldStatus("aim_loaded");
                return false;
            }
            cursor = snapped;
            hasCursor = true;

            switch (state)
            {
                case RoadDraftState.Geometry:
                    HandleGeometryPlacement(snapped);
                    return false;
                case RoadDraftState.Height:
                    selectedPointIndex = FindClosestPoint(snapped, 1.4f);
                    if (selectedPointIndex >= 0)
                    {
                        status = EarthWorksLocalization.Text(
                            "height_point_selected",
                            selectedPointIndex + 1,
                            EarthWorksLocalization.Text(
                                points[selectedPointIndex].ElevationAnchored ? "height_locked" : "height_auto"));
                        statusHoldUntil = Time.time + 2f;
                    }
                    else
                    {
                        HoldStatus("height_selection_cleared");
                    }
                    return false;
                case RoadDraftState.Width:
                    selectedPointIndex = FindClosestPoint(snapped, 1.4f);
                    HoldStatus(selectedPointIndex >= 0 ? "width_point_selected" : "width_default_selected");
                    return false;
                case RoadDraftState.Surface:
                    selectedSegmentIndex = FindClosestSegment(snapped, 1.8f);
                    HoldStatus(selectedSegmentIndex >= 0 ? "surface_segment_selected" : "select_segment");
                    return false;
                case RoadDraftState.Review:
                    HoldStatus("review_use_g");
                    return false;
            }

            if (state == RoadDraftState.Drawing && points.Count >= 2 &&
                SameHorizontal(points[points.Count - 1].Position, snapped))
            {
                float clickTime = Time.unscaledTime;
                if (clickTime - lastPointClickTime <= FinishDoubleClickWindow)
                {
                    FinishRoute();
                    lastPointClickTime = -10f;
                }
                else
                {
                    lastPointClickTime = clickTime;
                    HoldStatus("double_click_finish", 0.8f);
                }
                return false;
            }
            if (points.Count >= EarthWorksPlugin.EffectiveMaximumControlPoints)
            {
                status = EarthWorksLocalization.Text(
                    "too_many_points",
                    EarthWorksPlugin.EffectiveMaximumControlPoints);
                statusHoldUntil = Time.time + 2f;
                return false;
            }
            if (points.Count > 0 && SameHorizontal(points[points.Count - 1].Position, snapped))
            {
                HoldStatus("duplicate_point");
                return false;
            }

            points.Add(new RoadDraftPoint(snapped));
            if (points.Count > 1)
            {
                straightSegments.Add(false);
                segmentSurfaceOverrides.Add(-1);
            }
            lastPointClickTime = Time.unscaledTime;
            state = RoadDraftState.Drawing;
            status = EarthWorksLocalization.Text("point_added", points.Count);
            statusHoldUntil = Time.time + 1.2f;
            return false;
        }

        public void HandleEditorShortcuts()
        {
            if (!HasActiveDraft)
            {
                return;
            }
            if (Input.GetKeyDown(EarthWorksPlugin.NextStageKey.Value))
            {
                Advance();
            }
            if (state == RoadDraftState.Geometry &&
                (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
            {
                RemoveSelectedPoint();
            }
            if ((state == RoadDraftState.Geometry || state == RoadDraftState.Height) &&
                Input.GetKeyDown(KeyCode.H))
            {
                RequestExactHeight();
            }
            if ((state == RoadDraftState.Geometry || state == RoadDraftState.Height) &&
                Input.GetKeyDown(KeyCode.F))
            {
                ToggleHeightAnchor();
            }
            if ((int)state >= (int)RoadDraftState.Geometry && Input.GetKeyDown(KeyCode.P))
            {
                CycleLongitudinalProfile();
            }
            if ((int)state >= (int)RoadDraftState.Geometry &&
                (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt)))
            {
                ToggleEndpointFit();
            }
        }

        public void BeginEditorPointer(
            Vector3 placement,
            float selectionRadius,
            Camera camera,
            Vector2 mousePosition)
        {
            if (!RoadTerrain.TrySnapToVertex(placement, out Vector3 snapped))
            {
                HoldStatus("aim_loaded");
                return;
            }
            cursor = snapped;
            hasCursor = true;
            if (state == RoadDraftState.Geometry &&
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                InsertPoint(snapped);
                return;
            }
            if (state == RoadDraftState.Height)
            {
                selectedPointIndex = FindClosestPoint(snapped, selectionRadius);
                if (selectedPointIndex >= 0)
                {
                    status = EarthWorksLocalization.Text(
                        "height_point_selected",
                        selectedPointIndex + 1,
                        EarthWorksLocalization.Text(
                            points[selectedPointIndex].ElevationAnchored ? "height_locked" : "height_auto"));
                    statusHoldUntil = Time.time + 2f;
                }
                else
                {
                    HoldStatus("height_selection_cleared");
                }
                return;
            }
            if (state == RoadDraftState.Width)
            {
                selectedPointIndex = FindClosestPoint(snapped, selectionRadius);
                HoldStatus(selectedPointIndex >= 0 ? "width_point_selected" : "width_default_selected");
                return;
            }
            if (state == RoadDraftState.Surface)
            {
                selectedSegmentIndex = FindClosestSegment(snapped, selectionRadius * 1.25f);
                HoldStatus(selectedSegmentIndex >= 0 ? "surface_segment_selected" : "select_segment");
                return;
            }
            if (state != RoadDraftState.Geometry)
            {
                HandlePlacement(snapped);
                return;
            }

            if (!TryPickManipulator(camera, mousePosition, 18f, out selectionKind) &&
                !TryFindHandle(snapped, selectionRadius, out selectedPointIndex, out selectionKind))
            {
                selectedPointIndex = FindClosestPoint(snapped, selectionRadius);
                selectionKind = selectedPointIndex >= 0
                    ? RoadSelectionKind.Point
                    : RoadSelectionKind.None;
            }
            dragOriginal = selectedPointIndex >= 0 ? ClonePoint(points[selectedPointIndex]) : null;
            if (dragOriginal != null)
            {
                Vector3 selectedPosition = dragOriginal.Position;
                if (selectionKind == RoadSelectionKind.IncomingHandle)
                {
                    selectedPosition += new Vector3(dragOriginal.IncomingHandle.x, 0f, dragOriginal.IncomingHandle.y);
                }
                else if (selectionKind == RoadSelectionKind.OutgoingHandle)
                {
                    selectedPosition += new Vector3(dragOriginal.OutgoingHandle.x, 0f, dragOriginal.OutgoingHandle.y);
                }
                dragOffset = selectedPosition - snapped;
                dragOffset.y = 0f;
            }
            HoldStatus(selectedPointIndex >= 0 ? "point_selected" : "select_point");
        }

        public void DragEditorPointer(Vector3 placement, Vector2 screenDelta, float unitsPerPixel)
        {
            if (state != RoadDraftState.Geometry || selectionKind == RoadSelectionKind.None ||
                selectedPointIndex < 0)
            {
                return;
            }
            RoadDraftPoint point = points[selectedPointIndex];
            if (selectionKind == RoadSelectionKind.HeightHandle)
            {
                float elevation = point.ElevationAnchored ? point.Elevation : SelectedTargetElevation();
                point.ElevationAnchored = true;
                point.Elevation = elevation + screenDelta.y * unitsPerPixel;
                point.Position.y = point.Elevation;
                if (elevationMode == RoadElevationMode.UniformGrade &&
                    selectedPointIndex > 0 && selectedPointIndex < points.Count - 1)
                {
                    elevationMode = RoadElevationMode.Anchored;
                }
            }
            else if (RoadTerrain.TrySnapToVertex(placement, out Vector3 snapped))
            {
                cursor = snapped;
                hasCursor = true;
                if (selectionKind == RoadSelectionKind.LeftWidthHandle ||
                    selectionKind == RoadSelectionKind.RightWidthHandle)
                {
                    GetPointWidthAxes(selectedPointIndex, out Vector3 leftAxis, out Vector3 rightAxis);
                    Vector3 axis = selectionKind == RoadSelectionKind.LeftWidthHandle
                        ? leftAxis
                        : rightAxis;
                    float width = Vector3.Dot(snapped - point.Position, axis) / axis.sqrMagnitude;
                    if (point.LeftWidth <= 0f)
                    {
                        point.LeftWidth = defaultLeftWidth;
                        point.RightWidth = defaultRightWidth;
                    }
                    if (selectionKind == RoadSelectionKind.LeftWidthHandle)
                    {
                        point.LeftWidth = Mathf.Clamp(width, 1f, 10f);
                    }
                    else
                    {
                        point.RightWidth = Mathf.Clamp(width, 1f, 10f);
                    }
                }
                else
                {
                    ApplyLiveSelection(point, snapped + dragOffset);
                    if (!point.ElevationAnchored)
                    {
                        point.Elevation = point.Position.y;
                    }
                }
            }
            currentPlan = null;
        }

        public void EndEditorPointer()
        {
            if (selectionKind == RoadSelectionKind.Point && selectedPointIndex >= 0 &&
                ((selectedPointIndex > 0 && SameHorizontal(points[selectedPointIndex - 1].Position, points[selectedPointIndex].Position)) ||
                 (selectedPointIndex < points.Count - 1 && SameHorizontal(points[selectedPointIndex + 1].Position, points[selectedPointIndex].Position))))
            {
                CopyPoint(dragOriginal, points[selectedPointIndex]);
                HoldStatus("duplicate_point");
            }
            else if (selectionKind != RoadSelectionKind.None)
            {
                HoldStatus("geometry_changed");
            }
            selectionKind = RoadSelectionKind.None;
            dragOriginal = null;
            dragOffset = Vector3.zero;
        }

        public void CancelEditorOperation()
        {
            if (dragOriginal != null && selectedPointIndex >= 0)
            {
                CopyPoint(dragOriginal, points[selectedPointIndex]);
                selectionKind = RoadSelectionKind.None;
                dragOriginal = null;
                dragOffset = Vector3.zero;
                currentPlan = null;
                HoldStatus("move_cancelled");
                return;
            }
            CancelOrGoBack();
        }

        public void AdvanceFromEditor() => Advance();

        public void GoBackFromEditor()
        {
            selectedPointIndex = -1;
            selectedSegmentIndex = -1;
            selectionKind = RoadSelectionKind.None;
            dragOriginal = null;
            dragOffset = Vector3.zero;
            switch (state)
            {
                case RoadDraftState.Geometry: state = RoadDraftState.Drawing; break;
                case RoadDraftState.Height: state = RoadDraftState.Geometry; break;
                case RoadDraftState.Width: state = RoadDraftState.Height; break;
                case RoadDraftState.Surface: state = RoadDraftState.Geometry; break;
                case RoadDraftState.Review:
                    state = RoadDraftState.Surface;
                    currentPlan = null;
                    planPreview.Hide();
                    break;
                default: return;
            }
            HoldStatus(state == RoadDraftState.Drawing
                ? "drawing_resumed"
                : "stage_" + state.ToString().ToLowerInvariant());
        }

        public void SetPreviewMode(RoadEditorPreviewMode value)
        {
            previewMode = value;
        }

        public void SetPreviewLayers(bool roadbed, bool difference)
        {
            showRoadbed = roadbed;
            showDifference = difference;
        }

        public void SetSelectedControlMode(RouteControlMode mode)
        {
            if (selectedPointIndex >= 0)
            {
                SetPointMode(selectedPointIndex, mode);
            }
        }

        public void SetSelectedSmoothing(float value)
        {
            if (selectedPointIndex < 0)
            {
                return;
            }
            RoadDraftPoint point = points[selectedPointIndex];
            point.Mode = RouteControlMode.XSpline;
            point.Smoothing = Mathf.Clamp01(value);
            currentPlan = null;
        }

        public void RemoveSelectedPoint()
        {
            if (selectedPointIndex < 0 || points.Count <= 2)
            {
                HoldStatus(points.Count <= 2 ? "need_two" : "select_point");
                return;
            }
            int index = selectedPointIndex;
            points.RemoveAt(index);
            if (index == 0)
            {
                straightSegments.RemoveAt(0);
                segmentSurfaceOverrides.RemoveAt(0);
            }
            else if (index >= points.Count)
            {
                straightSegments.RemoveAt(straightSegments.Count - 1);
                segmentSurfaceOverrides.RemoveAt(segmentSurfaceOverrides.Count - 1);
            }
            else
            {
                straightSegments[index - 1] = straightSegments[index - 1] && straightSegments[index];
                straightSegments.RemoveAt(index);
                segmentSurfaceOverrides.RemoveAt(index);
            }
            selectedPointIndex = -1;
            selectionKind = RoadSelectionKind.None;
            currentPlan = null;
            HoldStatus("point_removed");
        }

        public bool TryGetEditorManipulators(out RoadEditorManipulators result)
        {
            result = default(RoadEditorManipulators);
            if (state != RoadDraftState.Geometry || selectedPointIndex < 0)
            {
                return false;
            }
            RoadDraftPoint point = points[selectedPointIndex];
            GetPointWidthAxes(selectedPointIndex, out Vector3 leftAxis, out Vector3 rightAxis);
            float elevation = SelectedTargetElevation();
            Vector3 center = new Vector3(point.Position.x, elevation, point.Position.z);
            result = new RoadEditorManipulators
            {
                Center = center,
                Height = center + Vector3.up * 3f,
                LeftWidth = center + leftAxis * CurrentLeftWidth,
                RightWidth = center + rightAxis * CurrentRightWidth
            };
            return true;
        }

        public void SetElevationMode(RoadElevationMode mode)
        {
            elevationMode = mode;
            if (mode == RoadElevationMode.SingleElevation)
            {
                singleElevationSourceIndex = selectedPointIndex >= 0 ? selectedPointIndex : 0;
                singleElevation = points[singleElevationSourceIndex].Elevation;
            }
            currentPlan = null;
            HoldStatus("height_mode_changed");
        }

        public void AdjustHeight(float amount)
        {
            if (elevationMode == RoadElevationMode.SingleElevation)
            {
                singleElevation += amount;
                singleElevationSourceIndex = -1;
            }
            else if (selectedPointIndex >= 0)
            {
                RoadDraftPoint point = points[selectedPointIndex];
                point.ElevationAnchored = true;
                point.Elevation += amount;
                point.Position.y = point.Elevation;
                if (elevationMode == RoadElevationMode.UniformGrade &&
                    selectedPointIndex > 0 && selectedPointIndex < points.Count - 1)
                {
                    elevationMode = RoadElevationMode.Anchored;
                }
            }
            else
            {
                HoldStatus("height_select_first");
                return;
            }
            currentPlan = null;
            HoldStatus("height_changed");
        }

        public void ToggleSelectedHeightAnchor() => ToggleHeightAnchor();
        public void RequestExactHeightFromEditor() => RequestExactHeight();

        public void AdjustWidth(float leftAmount, float rightAmount)
        {
            if (selectedPointIndex >= 0)
            {
                RoadDraftPoint point = points[selectedPointIndex];
                if (point.LeftWidth <= 0f)
                {
                    point.LeftWidth = defaultLeftWidth;
                    point.RightWidth = defaultRightWidth;
                }
                point.LeftWidth = Mathf.Clamp(point.LeftWidth + leftAmount, 1f, 10f);
                point.RightWidth = Mathf.Clamp(point.RightWidth + rightAmount, 1f, 10f);
            }
            else
            {
                defaultLeftWidth = Mathf.Clamp(defaultLeftWidth + leftAmount, 1f, 10f);
                defaultRightWidth = Mathf.Clamp(defaultRightWidth + rightAmount, 1f, 10f);
            }
            currentPlan = null;
            HoldStatus("width_changed");
        }

        public void ResetSelectedWidth()
        {
            if (selectedPointIndex < 0)
            {
                return;
            }
            points[selectedPointIndex].LeftWidth = -1f;
            points[selectedPointIndex].RightWidth = -1f;
            currentPlan = null;
        }

        public void SetSurface(RoadSurface value)
        {
            if (!EarthWorksPlugin.IsSurfaceAvailable(value))
            {
                return;
            }
            if (selectedSegmentIndex >= 0)
            {
                segmentSurfaceOverrides[selectedSegmentIndex] = (int)value;
            }
            else
            {
                defaultSurface = value;
            }
            currentPlan = null;
            HoldStatus("surface_changed");
        }

        public void CycleRoadbedProfileFromEditor() => CycleLongitudinalProfile();

        public void ToggleEndpointFit()
        {
            fitEndpointPlanes = !fitEndpointPlanes;
            RecalculateRoadbed("endpoint_fit_changed");
        }

        public void ToggleSelectedStraightSegment()
        {
            if (selectedPointIndex >= 0 && selectedPointIndex < straightSegments.Count)
            {
                straightSegments[selectedPointIndex] = !straightSegments[selectedPointIndex];
                currentPlan = null;
            }
        }

        public bool SelectedSegmentIsStraight => selectedPointIndex >= 0 &&
            selectedPointIndex < straightSegments.Count && straightSegments[selectedPointIndex];

        public bool TryGetSelectedPosition(out Vector3 position)
        {
            if (selectedPointIndex >= 0)
            {
                position = points[selectedPointIndex].Position;
                return true;
            }
            position = default(Vector3);
            return false;
        }

        public bool TryGetCameraBounds(out Vector3 center, out float radius)
        {
            if (!TryGetCameraFocus(out center))
            {
                radius = 20f;
                return false;
            }
            radius = 8f;
            foreach (RoadDraftPoint point in points)
            {
                Vector2 delta = new Vector2(point.Position.x - center.x, point.Position.z - center.z);
                radius = Mathf.Max(radius, delta.magnitude + Mathf.Max(CurrentLeftWidth, CurrentRightWidth));
            }
            return true;
        }

        public string GetText()
        {
            if (elevationMode == RoadElevationMode.SingleElevation)
            {
                return singleElevation.ToString("F2", CultureInfo.InvariantCulture);
            }
            return selectedPointIndex >= 0
                ? points[selectedPointIndex].Elevation.ToString("F2", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        public void SetText(string text)
        {
            if (!awaitingExactHeight)
            {
                return;
            }
            awaitingExactHeight = false;
            bool parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) ||
                float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            if (!parsed || float.IsNaN(value) || float.IsInfinity(value))
            {
                HoldStatus("height_invalid");
                return;
            }
            if (elevationMode == RoadElevationMode.SingleElevation)
            {
                singleElevation = value;
                singleElevationSourceIndex = -1;
            }
            else if (selectedPointIndex >= 0)
            {
                points[selectedPointIndex].ElevationAnchored = true;
                points[selectedPointIndex].Elevation = value;
                points[selectedPointIndex].Position.y = value;
                if (elevationMode == RoadElevationMode.UniformGrade &&
                    selectedPointIndex > 0 && selectedPointIndex < points.Count - 1)
                {
                    elevationMode = RoadElevationMode.Anchored;
                }
            }
            currentPlan = null;
            HoldStatus("height_exact_set");
        }

        public void Dispose()
        {
            preview.Dispose();
            planPreview.Dispose();
        }

        public void ResetForLaboratory()
        {
            ResetSilently();
        }

        private void RenderTerrainPreview()
        {
            if (currentPlan != null && previewMode == RoadEditorPreviewMode.Result && showRoadbed)
            {
                planPreview.Render(currentPlan, false);
            }
            else if (currentPlan != null && previewMode == RoadEditorPreviewMode.Difference && showDifference)
            {
                planPreview.Render(currentPlan, true);
            }
            else
            {
                planPreview.Hide();
            }
        }

        private void Advance()
        {
            switch (state)
            {
                case RoadDraftState.Drawing:
                    FinishRoute();
                    break;
                case RoadDraftState.Geometry:
                    if (selectionKind != RoadSelectionKind.None)
                    {
                        HoldStatus("finish_manipulation");
                        return;
                    }
                    state = RoadDraftState.Surface;
                    selectedPointIndex = -1;
                    selectedSegmentIndex = -1;
                    currentPlan = null;
                    HoldStatus("stage_surface");
                    break;
                case RoadDraftState.Height:
                    state = RoadDraftState.Width;
                    selectedPointIndex = -1;
                    HoldStatus("stage_width");
                    break;
                case RoadDraftState.Width:
                    state = RoadDraftState.Surface;
                    selectedPointIndex = -1;
                    selectedSegmentIndex = -1;
                    HoldStatus("stage_surface");
                    break;
                case RoadDraftState.Surface:
                    BuildReview();
                    break;
                case RoadDraftState.Review:
                    CreateProject();
                    break;
            }
        }

        private void FinishRoute()
        {
            if (points.Count < 2)
            {
                HoldStatus("need_two");
                return;
            }
            state = RoadDraftState.Geometry;
            selectedPointIndex = -1;
            selectionKind = RoadSelectionKind.None;
            lastPointClickTime = -10f;
            HoldStatus("route_finished", 2f);
        }

        private void BuildReview()
        {
            currentPlan = CalculatePlan();
            state = RoadDraftState.Review;
            if (currentPlan.IsValid)
            {
                HoldStatus("review_ready", 2.5f);
            }
            else
            {
                status = currentPlan.InvalidReason;
                statusHoldUntil = Time.time + 2.5f;
            }
        }

        private RoadBuildPlan CalculatePlan(bool includeTerrainEdits = true)
        {
            return RoadTerrainPlanner.Build(
                points,
                straightSegments,
                elevationMode,
                longitudinalProfile,
                fitEndpointPlanes,
                singleElevation,
                defaultLeftWidth,
                defaultRightWidth,
                defaultSurface,
                segmentSurfaceOverrides,
                EarthWorksPlugin.EffectiveCurveSubdivisions,
                includeTerrainEdits);
        }

        private void CreateProject()
        {
            if (currentPlan == null || currentPlan.Record == null)
            {
                currentPlan = CalculatePlan();
            }
            if (currentPlan == null || !currentPlan.IsValid)
            {
                status = currentPlan?.InvalidReason ?? EarthWorksLocalization.Text("review_invalid");
                statusHoldUntil = Time.time + 3f;
                return;
            }
            if (!RoadProjectFactory.TryCreate(currentPlan, out string reason))
            {
                status = reason;
                statusHoldUntil = Time.time + 4f;
                return;
            }
            Player.m_localPlayer?.Message(
                MessageHud.MessageType.Center,
                EarthWorksLocalization.Text("project_created"));
            ResetSilently();
        }

        private void CancelOrGoBack()
        {
            if (state == RoadDraftState.Drawing)
            {
                RemoveLastPoint();
                return;
            }
            if (state == RoadDraftState.Geometry)
            {
                if (selectionKind != RoadSelectionKind.None)
                {
                    selectionKind = RoadSelectionKind.None;
                    HoldStatus("move_cancelled");
                }
                else if (selectedPointIndex >= 0)
                {
                    selectedPointIndex = -1;
                    HoldStatus("height_selection_cleared");
                }
                else
                {
                    state = RoadDraftState.Drawing;
                    HoldStatus("drawing_resumed");
                }
                return;
            }
            if (state == RoadDraftState.Height)
            {
                if (selectedPointIndex >= 0 && points[selectedPointIndex].ElevationAnchored)
                {
                    RoadDraftPoint point = points[selectedPointIndex];
                    point.ElevationAnchored = false;
                    if (RoadTerrain.TryGetHeight(point.Position, out float ground))
                    {
                        point.Position.y = ground;
                        point.Elevation = ground;
                    }
                    selectedPointIndex = -1;
                    currentPlan = null;
                    HoldStatus("height_reset_auto");
                }
                else
                {
                    selectedPointIndex = -1;
                    state = RoadDraftState.Geometry;
                    HoldStatus("stage_geometry");
                }
                return;
            }
            if (state == RoadDraftState.Width)
            {
                if (selectedPointIndex >= 0 &&
                    (points[selectedPointIndex].LeftWidth > 0f || points[selectedPointIndex].RightWidth > 0f))
                {
                    points[selectedPointIndex].LeftWidth = -1f;
                    points[selectedPointIndex].RightWidth = -1f;
                    selectedPointIndex = -1;
                    currentPlan = null;
                    HoldStatus("width_reset_default");
                }
                else
                {
                    selectedPointIndex = -1;
                    state = RoadDraftState.Height;
                    HoldStatus("stage_height");
                }
                return;
            }
            if (state == RoadDraftState.Surface)
            {
                if (selectedSegmentIndex >= 0 && segmentSurfaceOverrides[selectedSegmentIndex] >= 0)
                {
                    segmentSurfaceOverrides[selectedSegmentIndex] = -1;
                    selectedSegmentIndex = -1;
                    currentPlan = null;
                    HoldStatus("surface_reset_default");
                }
                else
                {
                    selectedSegmentIndex = -1;
                    state = RoadDraftState.Geometry;
                    HoldStatus("stage_geometry");
                }
                return;
            }
            if (state == RoadDraftState.Review)
            {
                planPreview.Hide();
                currentPlan = null;
                state = RoadDraftState.Surface;
                HoldStatus("stage_surface");
            }
        }

        private void RemoveLastPoint()
        {
            if (points.Count == 0)
            {
                return;
            }
            points.RemoveAt(points.Count - 1);
            if (straightSegments.Count >= points.Count && straightSegments.Count > 0)
            {
                straightSegments.RemoveAt(straightSegments.Count - 1);
                segmentSurfaceOverrides.RemoveAt(segmentSurfaceOverrides.Count - 1);
            }
            lastPointClickTime = -10f;
            if (points.Count == 0)
            {
                state = RoadDraftState.Idle;
                preview.Hide();
                previewLength = 0.0;
                HoldStatus("draft_cancelled");
            }
            else
            {
                HoldStatus("point_removed");
            }
        }

        private void HandleGeometryPlacement(Vector3 snapped)
        {
            if (selectionKind == RoadSelectionKind.None)
            {
                if (TryFindHandle(snapped, out selectedPointIndex, out selectionKind))
                {
                    HoldStatus("handle_selected");
                    return;
                }
                selectedPointIndex = FindClosestPoint(snapped, 1.4f);
                selectionKind = selectedPointIndex >= 0
                    ? RoadSelectionKind.Point
                    : RoadSelectionKind.None;
                HoldStatus(selectedPointIndex >= 0 ? "point_selected" : "select_point");
                return;
            }

            RoadDraftPoint point = points[selectedPointIndex];
            if (selectionKind == RoadSelectionKind.Point)
            {
                if ((selectedPointIndex > 0 && SameHorizontal(points[selectedPointIndex - 1].Position, snapped)) ||
                    (selectedPointIndex < points.Count - 1 && SameHorizontal(points[selectedPointIndex + 1].Position, snapped)))
                {
                    HoldStatus("duplicate_point");
                    return;
                }
                point.Position = snapped;
                if (!point.ElevationAnchored)
                {
                    point.Elevation = snapped.y;
                }
            }
            else
            {
                Vector2 handle = new Vector2(snapped.x - point.Position.x, snapped.z - point.Position.z);
                if (selectionKind == RoadSelectionKind.IncomingHandle)
                {
                    point.IncomingHandle = handle;
                }
                else
                {
                    point.OutgoingHandle = handle;
                }
            }
            selectedPointIndex = -1;
            selectionKind = RoadSelectionKind.None;
            currentPlan = null;
            HoldStatus("geometry_changed");
        }

        private void HandleMiddleClick()
        {
            if (!hasCursor)
            {
                HoldStatus("aim_loaded");
                return;
            }
            if (state == RoadDraftState.Geometry)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    int segment = FindClosestSegment(cursor, 1.8f);
                    if (segment >= 0)
                    {
                        straightSegments[segment] = !straightSegments[segment];
                        HoldStatus(straightSegments[segment] ? "segment_straight" : "segment_curved");
                    }
                    return;
                }
                int pointIndex = FindClosestPoint(cursor, 1.5f);
                if (pointIndex < 0)
                {
                    HoldStatus("select_point");
                    return;
                }
                CyclePointMode(pointIndex);
                return;
            }
            if (state == RoadDraftState.Height)
            {
                elevationMode = (RoadElevationMode)(((int)elevationMode + 1) % 4);
                if (elevationMode == RoadElevationMode.SingleElevation)
                {
                    singleElevationSourceIndex = selectedPointIndex >= 0 ? selectedPointIndex : 0;
                    singleElevation = points[singleElevationSourceIndex].Elevation;
                }
                currentPlan = null;
                HoldStatus("height_mode_changed");
                return;
            }
            if (state == RoadDraftState.Surface)
            {
                CycleSurface();
            }
        }

        private void HandleScroll(float direction)
        {
            if (state == RoadDraftState.Height)
            {
                float step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                    ? 1f
                    : 0.25f;
                if (elevationMode == RoadElevationMode.SingleElevation)
                {
                    singleElevation += direction * step;
                    singleElevationSourceIndex = -1;
                    HoldStatus("height_changed");
                }
                else if (selectedPointIndex >= 0)
                {
                    RoadDraftPoint point = points[selectedPointIndex];
                    point.ElevationAnchored = true;
                    point.Elevation += direction * step;
                    point.Position.y = point.Elevation;
                    if (elevationMode == RoadElevationMode.UniformGrade &&
                        selectedPointIndex > 0 && selectedPointIndex < points.Count - 1)
                    {
                        elevationMode = RoadElevationMode.Anchored;
                    }
                    HoldStatus("height_changed");
                }
                else
                {
                    HoldStatus("height_select_first");
                }
                currentPlan = null;
                return;
            }
            if (state != RoadDraftState.Width)
            {
                return;
            }

            bool leftOnly = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool rightOnly = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            float amount = direction * 0.25f;
            if (selectedPointIndex >= 0)
            {
                RoadDraftPoint point = points[selectedPointIndex];
                if (point.LeftWidth <= 0f)
                {
                    point.LeftWidth = defaultLeftWidth;
                    point.RightWidth = defaultRightWidth;
                }
                if (!rightOnly)
                {
                    point.LeftWidth = Mathf.Clamp(point.LeftWidth + amount, 1f, 10f);
                }
                if (!leftOnly)
                {
                    point.RightWidth = Mathf.Clamp(point.RightWidth + amount, 1f, 10f);
                }
            }
            else
            {
                if (!rightOnly)
                {
                    defaultLeftWidth = Mathf.Clamp(defaultLeftWidth + amount, 1f, 10f);
                }
                if (!leftOnly)
                {
                    defaultRightWidth = Mathf.Clamp(defaultRightWidth + amount, 1f, 10f);
                }
            }
            currentPlan = null;
            HoldStatus("width_changed");
        }

        private void RequestExactHeight()
        {
            if (elevationMode != RoadElevationMode.SingleElevation && selectedPointIndex < 0)
            {
                HoldStatus("height_select_first");
                return;
            }
            if (!TextInput.instance)
            {
                HoldStatus("height_input_unavailable");
                return;
            }
            awaitingExactHeight = true;
            TextInput.instance.RequestText(this, EarthWorksLocalization.Text("height_input_title"), 16);
        }

        private void ToggleHeightAnchor()
        {
            if (selectedPointIndex < 0)
            {
                HoldStatus("height_select_first");
                return;
            }

            RoadDraftPoint point = points[selectedPointIndex];
            point.ElevationAnchored = !point.ElevationAnchored;
            if (point.ElevationAnchored)
            {
                point.Elevation = SelectedTargetElevation();
                point.Position.y = point.Elevation;
                if (elevationMode != RoadElevationMode.SingleElevation &&
                    (elevationMode != RoadElevationMode.UniformGrade ||
                     (selectedPointIndex > 0 && selectedPointIndex < points.Count - 1)))
                {
                    elevationMode = RoadElevationMode.Anchored;
                }
                HoldStatus("height_anchor_on");
            }
            else
            {
                HoldStatus("height_anchor_off");
            }
            currentPlan = null;
        }

        private float SelectedTargetElevation()
        {
            if (selectedPointIndex >= 0 && currentPlan != null && currentPlan.IsValid &&
                selectedPointIndex < currentPlan.ControlPointElevations.Count &&
                !float.IsNaN(currentPlan.ControlPointElevations[selectedPointIndex]))
            {
                return currentPlan.ControlPointElevations[selectedPointIndex];
            }
            return selectedPointIndex >= 0 ? points[selectedPointIndex].Elevation : singleElevation;
        }

        private string PlanWarning(string detail)
        {
            return currentPlan != null && !currentPlan.IsValid
                ? detail + "\n" + currentPlan.InvalidReason
                : detail;
        }

        private void CyclePointMode(int index)
        {
            RoadDraftPoint point = points[index];
            RouteControlMode next = point.Mode == RouteControlMode.XSpline
                ? RouteControlMode.BSpline
                : point.Mode == RouteControlMode.BSpline
                    ? RouteControlMode.Bezier
                    : point.Mode == RouteControlMode.Bezier
                        ? RouteControlMode.Corner
                        : RouteControlMode.XSpline;
            SetPointMode(index, next);
        }

        private void SetPointMode(int index, RouteControlMode next)
        {
            RoadDraftPoint point = points[index];
            if (next == RouteControlMode.Bezier && point.Mode != RouteControlMode.Bezier)
            {
                RoadRoute route = RoadTerrainPlanner.BuildRoute(points, straightSegments);
                PlanarVector incoming = index > 0
                    ? RouteEvaluator.EvaluateDerivative(route, index - 1, 1.0)
                    : default(PlanarVector);
                PlanarVector outgoing = index < points.Count - 1
                    ? RouteEvaluator.EvaluateDerivative(route, index, 0.0)
                    : default(PlanarVector);
                point.IncomingHandle = new Vector2((float)-incoming.X, (float)-incoming.Z) / 3f;
                point.OutgoingHandle = new Vector2((float)outgoing.X, (float)outgoing.Z) / 3f;
            }
            point.Mode = next;
            if (next == RouteControlMode.XSpline &&
                (point.Smoothing < 0f || point.Smoothing > 1f))
            {
                point.Smoothing = 0.5f;
            }
            currentPlan = null;
            status = EarthWorksLocalization.Text(
                "point_mode_changed",
                index + 1,
                EarthWorksLocalization.ControlModeName(point.Mode));
            statusHoldUntil = Time.time + 2f;
        }

        private void CycleSurface()
        {
            RoadSurface current = CurrentSurface;
            RoadSurface next = current;
            for (int i = 0; i < 2; ++i)
            {
                next = (RoadSurface)(((int)next + 1) % 2);
                if (EarthWorksPlugin.IsSurfaceAvailable(next))
                {
                    break;
                }
            }
            if (selectedSegmentIndex >= 0)
            {
                segmentSurfaceOverrides[selectedSegmentIndex] = (int)next;
            }
            else
            {
                defaultSurface = next;
            }
            currentPlan = null;
            HoldStatus("surface_changed");
        }

        private void CycleLongitudinalProfile()
        {
            longitudinalProfile = (RoadLongitudinalProfile)(
                ((int)longitudinalProfile + 1) % 4);
            RecalculateRoadbed("road_profile_changed");
        }

        private void RecalculateRoadbed(string statusKey)
        {
            currentPlan = state == RoadDraftState.Review ? CalculatePlan() : null;
            if (currentPlan != null && !currentPlan.IsValid)
            {
                status = currentPlan.InvalidReason;
                statusHoldUntil = Time.time + 2.5f;
                return;
            }
            HoldStatus(statusKey, 2f);
        }

        private string WithRoadbed(string detail)
        {
            return detail + "\n" + EarthWorksLocalization.Text(
                "roadbed_detail",
                EarthWorksLocalization.LongitudinalProfileName(longitudinalProfile),
                EarthWorksLocalization.Text(fitEndpointPlanes ? "endpoint_fit_on" : "endpoint_fit_off"));
        }

        private float CurrentLeftWidth => selectedPointIndex >= 0 && points[selectedPointIndex].LeftWidth > 0f
            ? points[selectedPointIndex].LeftWidth
            : defaultLeftWidth;

        private float CurrentRightWidth => selectedPointIndex >= 0 && points[selectedPointIndex].RightWidth > 0f
            ? points[selectedPointIndex].RightWidth
            : defaultRightWidth;

        private RoadSurface CurrentSurface => selectedSegmentIndex >= 0 &&
            segmentSurfaceOverrides[selectedSegmentIndex] >= 0
                ? (RoadSurface)segmentSurfaceOverrides[selectedSegmentIndex]
                : defaultSurface;

        private int FindClosestPoint(Vector3 target, float maximumDistance)
        {
            int closest = -1;
            float closestSquared = maximumDistance * maximumDistance;
            for (int i = 0; i < points.Count; ++i)
            {
                float dx = points[i].Position.x - target.x;
                float dz = points[i].Position.z - target.z;
                float squared = dx * dx + dz * dz;
                if (squared <= closestSquared)
                {
                    closest = i;
                    closestSquared = squared;
                }
            }
            return closest;
        }

        private int FindClosestSegment(Vector3 target, float maximumDistance)
        {
            int closest = -1;
            float best = maximumDistance * maximumDistance;
            if (points.Count < 2)
            {
                return closest;
            }
            RoadRoute route = RoadTerrainPlanner.BuildRoute(points, straightSegments);
            IReadOnlyList<RouteSample> samples = RouteEvaluator.Sample(
                route,
                Mathf.Max(6, EarthWorksPlugin.EffectiveCurveSubdivisions));
            Vector2 p = new Vector2(target.x, target.z);
            for (int i = 0; i < samples.Count - 1; ++i)
            {
                Vector2 a = new Vector2((float)samples[i].Position.X, (float)samples[i].Position.Z);
                Vector2 b = new Vector2((float)samples[i + 1].Position.X, (float)samples[i + 1].Position.Z);
                Vector2 axis = b - a;
                if (axis.sqrMagnitude < 0.0001f)
                {
                    continue;
                }
                float t = Mathf.Clamp01(Vector2.Dot(p - a, axis) / axis.sqrMagnitude);
                float squared = (p - (a + axis * t)).sqrMagnitude;
                if (squared <= best)
                {
                    closest = samples[i + 1].SegmentIndex;
                    best = squared;
                }
            }
            return closest;
        }

        private bool TryFindHandle(
            Vector3 target,
            out int pointIndex,
            out RoadSelectionKind kind)
        {
            return TryFindHandle(target, 1.4f, out pointIndex, out kind);
        }

        private bool TryFindHandle(
            Vector3 target,
            float maximumDistance,
            out int pointIndex,
            out RoadSelectionKind kind)
        {
            pointIndex = -1;
            kind = RoadSelectionKind.None;
            float best = maximumDistance * maximumDistance;
            for (int i = 0; i < points.Count; ++i)
            {
                RoadDraftPoint point = points[i];
                if (point.Mode != RouteControlMode.Bezier)
                {
                    continue;
                }
                Vector2 target2 = new Vector2(target.x, target.z);
                Vector2 origin = new Vector2(point.Position.x, point.Position.z);
                float incoming = (target2 - (origin + point.IncomingHandle)).sqrMagnitude;
                if (incoming <= best)
                {
                    best = incoming;
                    pointIndex = i;
                    kind = RoadSelectionKind.IncomingHandle;
                }
                float outgoing = (target2 - (origin + point.OutgoingHandle)).sqrMagnitude;
                if (outgoing <= best)
                {
                    best = outgoing;
                    pointIndex = i;
                    kind = RoadSelectionKind.OutgoingHandle;
                }
            }
            return pointIndex >= 0;
        }

        private void InsertPoint(Vector3 snapped)
        {
            int segment = FindClosestSegment(snapped, Mathf.Max(4f, CurrentLeftWidth + CurrentRightWidth));
            if (segment < 0)
            {
                HoldStatus("select_segment");
                return;
            }
            if (points.Count >= EarthWorksPlugin.EffectiveMaximumControlPoints)
            {
                status = EarthWorksLocalization.Text(
                    "too_many_points",
                    EarthWorksPlugin.EffectiveMaximumControlPoints);
                statusHoldUntil = Time.time + 2f;
                return;
            }
            bool wasStraight = straightSegments[segment];
            int surface = segmentSurfaceOverrides[segment];
            points.Insert(segment + 1, new RoadDraftPoint(snapped));
            straightSegments.Insert(segment, wasStraight);
            segmentSurfaceOverrides.Insert(segment, surface);
            selectedPointIndex = segment + 1;
            selectedSegmentIndex = -1;
            selectionKind = RoadSelectionKind.None;
            currentPlan = null;
            status = EarthWorksLocalization.Text("point_added", selectedPointIndex + 1);
            statusHoldUntil = Time.time + 1.2f;
        }

        public void InsertAfterSelected()
        {
            if (selectedPointIndex < 0)
            {
                HoldStatus("select_point");
                return;
            }
            int segment = selectedPointIndex < points.Count - 1
                ? selectedPointIndex
                : selectedPointIndex - 1;
            PlanarPoint routePoint = RouteEvaluator.Evaluate(
                RoadTerrainPlanner.BuildRoute(points, straightSegments),
                segment,
                0.5);
            Vector3 midpoint = new Vector3((float)routePoint.X, 0f, (float)routePoint.Z);
            if (RoadTerrain.TrySnapToVertex(midpoint, out Vector3 snapped))
            {
                InsertPoint(snapped);
            }
        }

        private bool TryPickManipulator(
            Camera camera,
            Vector2 mousePosition,
            float maximumPixels,
            out RoadSelectionKind kind)
        {
            kind = RoadSelectionKind.None;
            if (!camera || !TryGetEditorManipulators(out RoadEditorManipulators handles))
            {
                return false;
            }
            float best = maximumPixels * maximumPixels;
            TryScreenHandle(camera, mousePosition, handles.LeftWidth, RoadSelectionKind.LeftWidthHandle, ref best, ref kind);
            TryScreenHandle(camera, mousePosition, handles.RightWidth, RoadSelectionKind.RightWidthHandle, ref best, ref kind);
            if (EarthWorksPlugin.Instance?.EditorCamera?.Mode == RoadCameraMode.Isometric)
            {
                TryScreenHandle(camera, mousePosition, handles.Height, RoadSelectionKind.HeightHandle, ref best, ref kind);
            }
            return kind != RoadSelectionKind.None;
        }

        private static void TryScreenHandle(
            Camera camera,
            Vector2 mousePosition,
            Vector3 position,
            RoadSelectionKind candidate,
            ref float best,
            ref RoadSelectionKind kind)
        {
            Vector3 screen = camera.WorldToScreenPoint(position);
            if (screen.z <= 0f)
            {
                return;
            }
            float squared = (new Vector2(screen.x, screen.y) - mousePosition).sqrMagnitude;
            if (squared <= best)
            {
                best = squared;
                kind = candidate;
            }
        }

        private void GetPointWidthAxes(int index, out Vector3 left, out Vector3 right)
        {
            Vector3 before = index > 0 ? points[index - 1].Position : points[index].Position;
            Vector3 after = index + 1 < points.Count ? points[index + 1].Position : points[index].Position;
            RoadRibbonMath.GetOffsets(before, points[index].Position, after, 1f, 1f, out left, out right);
        }

        private void ApplyLiveSelection(RoadDraftPoint point, Vector3 liveCursor)
        {
            if (selectionKind == RoadSelectionKind.Point)
            {
                point.Position = new Vector3(
                    liveCursor.x,
                    point.ElevationAnchored ? point.Elevation : liveCursor.y,
                    liveCursor.z);
            }
            else if (selectionKind == RoadSelectionKind.IncomingHandle)
            {
                point.IncomingHandle = new Vector2(
                    liveCursor.x - point.Position.x,
                    liveCursor.z - point.Position.z);
            }
            else if (selectionKind == RoadSelectionKind.OutgoingHandle)
            {
                point.OutgoingHandle = new Vector2(
                    liveCursor.x - point.Position.x,
                    liveCursor.z - point.Position.z);
            }
        }

        private void RenderDraft(
            IReadOnlyList<RoadDraftPoint> display,
            bool editing,
            int committed = -1)
        {
            if (display.Count == 0)
            {
                preview.Hide();
                previewLength = 0.0;
                return;
            }
            previewLength = preview.Render(
                display,
                committed < 0 ? points.Count : committed,
                selectedPointIndex,
                selectedSegmentIndex,
                selectionKind,
                editing,
                EarthWorksPlugin.EffectiveCurveSubdivisions,
                straightSegments,
                defaultLeftWidth,
                defaultRightWidth,
                showRoadbed && previewMode != RoadEditorPreviewMode.Current &&
                    (int)state >= (int)RoadDraftState.Geometry,
                state,
                previewMode == RoadEditorPreviewMode.Current ? null : currentPlan);
        }

        private static List<RoadDraftPoint> ClonePoints(IReadOnlyList<RoadDraftPoint> source)
        {
            List<RoadDraftPoint> result = new List<RoadDraftPoint>(source.Count);
            foreach (RoadDraftPoint point in source)
            {
                result.Add(new RoadDraftPoint(point.Position)
                {
                    Mode = point.Mode,
                    Smoothing = point.Smoothing,
                    IncomingHandle = point.IncomingHandle,
                    OutgoingHandle = point.OutgoingHandle,
                    ElevationAnchored = point.ElevationAnchored,
                    Elevation = point.Elevation,
                    LeftWidth = point.LeftWidth,
                    RightWidth = point.RightWidth
                });
            }
            return result;
        }

        private static RoadDraftPoint ClonePoint(RoadDraftPoint point)
        {
            return new RoadDraftPoint(point.Position)
            {
                Mode = point.Mode,
                Smoothing = point.Smoothing,
                IncomingHandle = point.IncomingHandle,
                OutgoingHandle = point.OutgoingHandle,
                ElevationAnchored = point.ElevationAnchored,
                Elevation = point.Elevation,
                LeftWidth = point.LeftWidth,
                RightWidth = point.RightWidth
            };
        }

        private static void CopyPoint(RoadDraftPoint source, RoadDraftPoint target)
        {
            if (source == null || target == null)
            {
                return;
            }
            target.Position = source.Position;
            target.Mode = source.Mode;
            target.Smoothing = source.Smoothing;
            target.IncomingHandle = source.IncomingHandle;
            target.OutgoingHandle = source.OutgoingHandle;
            target.ElevationAnchored = source.ElevationAnchored;
            target.Elevation = source.Elevation;
            target.LeftWidth = source.LeftWidth;
            target.RightWidth = source.RightWidth;
        }

        private void RefreshCursor(Player player)
        {
            hasCursor = EarthWorksPlugin.TryGetPlacementPoint(player, out Vector3 placement) &&
                RoadTerrain.TrySnapToVertex(placement, out cursor);
        }

        private string DefaultStatusKey()
        {
            switch (state)
            {
                case RoadDraftState.Drawing:
                    return "draw_route";
                case RoadDraftState.Geometry:
                    return selectionKind == RoadSelectionKind.None ? "geometry_help" : "geometry_place";
                case RoadDraftState.Height:
                    return "height_help";
                case RoadDraftState.Width:
                    return "width_help";
                case RoadDraftState.Surface:
                    return "surface_help";
                default:
                    return "click_start";
            }
        }

        private void SetPassiveStatus(string key)
        {
            if (Time.time >= statusHoldUntil)
            {
                status = EarthWorksLocalization.Text(key);
            }
        }

        private void HoldStatus(string key, float seconds = 1.4f)
        {
            status = EarthWorksLocalization.Text(key);
            statusHoldUntil = Time.time + seconds;
        }

        private void ResetSilently()
        {
            points.Clear();
            straightSegments.Clear();
            segmentSurfaceOverrides.Clear();
            state = RoadDraftState.Idle;
            selectedPointIndex = -1;
            selectedSegmentIndex = -1;
            selectionKind = RoadSelectionKind.None;
            dragOriginal = null;
            dragOffset = Vector3.zero;
            lastPointClickTime = -10f;
            previewLength = 0.0;
            status = string.Empty;
            currentPlan = null;
            awaitingExactHeight = false;
            elevationMode = RoadElevationMode.UniformGrade;
            longitudinalProfile = RoadLongitudinalProfile.LinearJoined;
            fitEndpointPlanes = false;
            singleElevationSourceIndex = 0;
            defaultSurface = RoadSurface.Bare;
            float halfWidth = EarthWorksPlugin.EffectiveDefaultRoadWidth * 0.5f;
            defaultLeftWidth = halfWidth;
            defaultRightWidth = halfWidth;
            preview.Hide();
            planPreview.Hide();
        }

        private static bool SameHorizontal(Vector3 left, Vector3 right)
        {
            float dx = left.x - right.x;
            float dz = left.z - right.z;
            return dx * dx + dz * dz < 0.0001f;
        }
    }
}
