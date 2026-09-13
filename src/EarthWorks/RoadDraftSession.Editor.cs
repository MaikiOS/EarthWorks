using System;
using System.Globalization;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal sealed partial class RoadDraftSession
    {
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
            if (HasActiveDraft && state == RoadDraftState.Geometry && Input.GetKeyDown(KeyCode.H))
            {
                RequestExactHeight();
            }
            if (HasActiveDraft && state == RoadDraftState.Geometry && Input.GetKeyDown(KeyCode.F))
            {
                ToggleHeightAnchor();
            }
            if (HasActiveDraft && (int)state >= (int)RoadDraftState.Geometry &&
                Input.GetKeyDown(KeyCode.P))
            {
                CycleLongitudinalProfile();
            }
            if (HasActiveDraft && (int)state >= (int)RoadDraftState.Geometry &&
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
            if (state == RoadDraftState.Geometry && Input.GetKeyDown(KeyCode.H))
            {
                RequestExactHeight();
            }
            if (state == RoadDraftState.Geometry && Input.GetKeyDown(KeyCode.F))
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

    }
}

