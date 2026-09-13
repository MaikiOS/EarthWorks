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
        Surface,
        Review
    }

    internal sealed partial class RoadDraftSession : TextReceiver
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
                            ? WithRoadbed(EarthWorksLocalization.Text(
                                "draft_selected_summary",
                                selectedPointIndex + 1,
                                EarthWorksLocalization.Text(points[selectedPointIndex].ElevationAnchored ? "draft_y_anchored" : "draft_y_auto"),
                                SelectedTargetElevation(),
                                CurrentLeftWidth,
                                CurrentRightWidth))
                            : WithRoadbed(EarthWorksLocalization.Text(
                                "draft_select_flag",
                                EarthWorksLocalization.ElevationModeName(elevationMode)));
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
                RoadBuildSettings.FromCurrentConfig(),
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
            if (state == RoadDraftState.Surface)
            {
                CycleSurface();
            }
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
