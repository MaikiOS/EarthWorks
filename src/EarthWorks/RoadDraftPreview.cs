using System;
using System.Collections.Generic;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace OstrixMods.EarthWorks
{
    internal sealed class RoadDraftPreview
    {
        private readonly GameObject root;
        private readonly Material material;
        private readonly LineRenderer routeLine;
        private readonly LineRenderer leftLine;
        private readonly LineRenderer rightLine;
        private readonly LineRenderer heightManipulator;
        private readonly LineRenderer leftWidthManipulator;
        private readonly LineRenderer rightWidthManipulator;
        private readonly Mesh ribbonMesh;
        private readonly List<LineRenderer> pointMarkers = new List<LineRenderer>();
        private readonly List<LineRenderer> handleLines = new List<LineRenderer>();

        public RoadDraftPreview()
        {
            root = new GameObject("EarthWorks_RoadDraftPreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Shader shader = Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Standard");
            if (!shader)
            {
                throw new InvalidOperationException("No compatible preview shader is available.");
            }
            material = new Material(shader)
            {
                name = "EarthWorks_RoadDraftMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            routeLine = CreateLine("EarthWorks_RouteCenter", 0.1f);
            leftLine = CreateLine("EarthWorks_RouteLeft", 0.055f);
            rightLine = CreateLine("EarthWorks_RouteRight", 0.055f);
            heightManipulator = CreateLine("EarthWorks_HeightManipulator", 0.11f);
            leftWidthManipulator = CreateLine("EarthWorks_LeftWidthManipulator", 0.11f);
            rightWidthManipulator = CreateLine("EarthWorks_RightWidthManipulator", 0.11f);
            GameObject ribbonObject = new GameObject("EarthWorks_RoadRibbon")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            ribbonObject.transform.SetParent(root.transform, false);
            MeshFilter ribbonFilter = ribbonObject.AddComponent<MeshFilter>();
            MeshRenderer ribbonRenderer = ribbonObject.AddComponent<MeshRenderer>();
            ribbonMesh = new Mesh { name = "EarthWorks_RoadRibbonMesh" };
            ribbonMesh.MarkDynamic();
            ribbonMesh.indexFormat = IndexFormat.UInt32;
            ribbonFilter.sharedMesh = ribbonMesh;
            ribbonRenderer.sharedMaterial = material;
            ribbonRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ribbonRenderer.receiveShadows = false;
            Hide();
        }

        public double Render(
            IReadOnlyList<RoadDraftPoint> displayPoints,
            int committedPointCount,
            int selectedPointIndex,
            int selectedSegmentIndex,
            RoadSelectionKind selectionKind,
            bool editing,
            int subdivisionsPerSegment,
            IReadOnlyList<bool> straightSegments,
            float defaultLeftWidth,
            float defaultRightWidth,
            bool showWidths,
            RoadDraftState state,
            RoadBuildPlan plan)
        {
            if (displayPoints == null || displayPoints.Count == 0)
            {
                Hide();
                return 0.0;
            }

            EnsureMarkerCount(displayPoints.Count);
            EnsureHandleCount(displayPoints.Count * 2);
            for (int i = 0; i < pointMarkers.Count; ++i)
            {
                LineRenderer marker = pointMarkers[i];
                if (i >= displayPoints.Count)
                {
                    marker.positionCount = 0;
                    continue;
                }
                Vector3 point = displayPoints[i].Position;
                if (plan != null && i < plan.ControlPointElevations.Count &&
                    !float.IsNaN(plan.ControlPointElevations[i]))
                {
                    point.y = plan.ControlPointElevations[i];
                }
                marker.positionCount = 2;
                marker.SetPosition(0, point + Vector3.up * 0.08f);
                marker.SetPosition(1, point + Vector3.up * 1.2f);
                Color color = i == selectedPointIndex
                    ? new Color(1f, 0.45f, 0.9f, 1f)
                    : state == RoadDraftState.Geometry &&
                        displayPoints[i].ElevationAnchored
                        ? new Color(1f, 0.72f, 0.12f, 1f)
                    : i >= committedPointCount
                        ? new Color(0.2f, 1f, 0.3f, 0.95f)
                        : ControlColor(displayPoints[i].Mode);
                SetLineColor(marker, color);
                RenderHandles(displayPoints, i, selectedPointIndex, selectionKind);
            }
            RenderManipulators(
                displayPoints,
                selectedPointIndex,
                selectionKind,
                defaultLeftWidth,
                defaultRightWidth,
                state,
                plan);

            double length = 0.0;
            bool usePlan = plan != null && plan.PreviewSamples.Count >= 2 &&
                (int)state >= (int)RoadDraftState.Geometry;
            if (displayPoints.Count < 2)
            {
                routeLine.positionCount = 0;
                leftLine.positionCount = 0;
                rightLine.positionCount = 0;
                ribbonMesh.Clear();
            }
            else if (usePlan)
            {
                RenderPlanProfile(plan, selectedSegmentIndex, state, showWidths);
                length = plan.Length;
            }
            else
            {
                ribbonMesh.Clear();
                RoadRoute route = BuildRoute(displayPoints, straightSegments);
                IReadOnlyList<RouteSample> samples = RouteEvaluator.Sample(
                    route,
                    Mathf.Max(2, subdivisionsPerSegment));
                routeLine.positionCount = samples.Count;
                leftLine.positionCount = showWidths ? samples.Count : 0;
                rightLine.positionCount = showWidths ? samples.Count : 0;
                for (int i = 0; i < samples.Count; ++i)
                {
                    RouteSample sample = samples[i];
                    int segment = sample.SegmentIndex;
                    float t = (float)sample.SegmentT;
                    float y = Mathf.Lerp(
                        displayPoints[segment].Position.y,
                        displayPoints[segment + 1].Position.y,
                        t) + 0.18f;
                    Vector3 center = new Vector3(
                        (float)sample.Position.X,
                        y,
                        (float)sample.Position.Z);
                    routeLine.SetPosition(i, center);
                    if (showWidths)
                    {
                        PlanarVector derivative = RouteEvaluator.EvaluateDerivative(route, segment, sample.SegmentT);
                        Vector2 direction = new Vector2((float)derivative.X, (float)derivative.Z).normalized;
                        Vector3 normal = new Vector3(-direction.y, 0f, direction.x);
                        float left = Mathf.Lerp(
                            Width(displayPoints[segment].LeftWidth, defaultLeftWidth),
                            Width(displayPoints[segment + 1].LeftWidth, defaultLeftWidth),
                            t);
                        float right = Mathf.Lerp(
                            Width(displayPoints[segment].RightWidth, defaultRightWidth),
                            Width(displayPoints[segment + 1].RightWidth, defaultRightWidth),
                            t);
                        leftLine.SetPosition(i, center + normal * left);
                        rightLine.SetPosition(i, center - normal * right);
                    }
                }
                length = samples[samples.Count - 1].Distance;
            }

            Color routeColor = plan != null && !plan.IsValid
                ? new Color(1f, 0.18f, 0.12f, 0.98f)
                : editing
                ? new Color(0.2f, 0.82f, 1f, 0.95f)
                : new Color(0.25f, 1f, 0.35f, 0.95f);
            SetLineColor(routeLine, routeColor);
            Color edge = new Color(1f, 0.78f, 0.25f, 0.78f);
            SetLineColor(leftLine, edge);
            SetLineColor(rightLine, edge);
            root.SetActive(true);
            return length;
        }

        public void Hide()
        {
            if (root)
            {
                root.SetActive(false);
            }
        }

        public void Dispose()
        {
            if (ribbonMesh)
            {
                UnityEngine.Object.Destroy(ribbonMesh);
            }
            if (material)
            {
                UnityEngine.Object.Destroy(material);
            }
            if (root)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private void RenderPlanProfile(
            RoadBuildPlan plan,
            int selectedSegmentIndex,
            RoadDraftState state,
            bool showWidths)
        {
            IReadOnlyList<RoadPreviewSample> samples = plan.PreviewSamples;
            routeLine.positionCount = samples.Count;
            leftLine.positionCount = showWidths ? samples.Count : 0;
            rightLine.positionCount = showWidths ? samples.Count : 0;
            List<Vector3> vertices = showWidths ? new List<Vector3>(samples.Count * 2) : null;
            List<Color> colors = showWidths ? new List<Color>(samples.Count * 2) : null;
            List<int> triangles = showWidths ? new List<int>((samples.Count - 1) * 6) : null;

            for (int i = 0; i < samples.Count; ++i)
            {
                RoadPreviewSample sample = samples[i];
                Vector3 center = sample.Position + Vector3.up * 0.18f;
                Vector3 before = i > 0 ? samples[i - 1].Position : sample.Position;
                Vector3 after = i + 1 < samples.Count ? samples[i + 1].Position : sample.Position;
                RoadRibbonMath.GetOffsets(
                    before,
                    sample.Position,
                    after,
                    sample.LeftWidth,
                    sample.RightWidth,
                    out Vector3 leftOffset,
                    out Vector3 rightOffset);
                Vector3 left = center + leftOffset;
                Vector3 right = center + rightOffset;
                left.y = sample.LeftHeight + 0.18f;
                right.y = sample.RightHeight + 0.18f;
                routeLine.SetPosition(i, center);
                if (!showWidths)
                {
                    continue;
                }
                leftLine.SetPosition(i, left);
                rightLine.SetPosition(i, right);
                Color color = RibbonColor(plan, sample, selectedSegmentIndex, state);
                vertices.Add(left);
                vertices.Add(right);
                colors.Add(color);
                colors.Add(color);
                if (i == 0)
                {
                    continue;
                }
                int previous = (i - 1) * 2;
                int current = i * 2;
                triangles.Add(previous);
                triangles.Add(previous + 1);
                triangles.Add(current);
                triangles.Add(previous + 1);
                triangles.Add(current + 1);
                triangles.Add(current);
            }

            ribbonMesh.Clear();
            if (showWidths)
            {
                ribbonMesh.SetVertices(vertices);
                ribbonMesh.SetColors(colors);
                ribbonMesh.SetTriangles(triangles, 0);
                ribbonMesh.RecalculateBounds();
            }
        }

        private static Color RibbonColor(
            RoadBuildPlan plan,
            RoadPreviewSample sample,
            int selectedSegmentIndex,
            RoadDraftState state)
        {
            if (!plan.IsValid)
            {
                return new Color(1f, 0.12f, 0.08f, 0.38f);
            }
            if (state == RoadDraftState.Surface && sample.SegmentIndex == selectedSegmentIndex)
            {
                return new Color(1f, 0.28f, 0.78f, 0.52f);
            }
            return sample.Surface == RoadSurface.Paved
                ? new Color(0.78f, 0.82f, 0.86f, 0.46f)
                : new Color(0.42f, 0.28f, 0.12f, 0.42f);
        }

        private void RenderHandles(
            IReadOnlyList<RoadDraftPoint> points,
            int index,
            int selectedPoint,
            RoadSelectionKind selectionKind)
        {
            RoadDraftPoint point = points[index];
            LineRenderer incoming = handleLines[index * 2];
            LineRenderer outgoing = handleLines[index * 2 + 1];
            if (point.Mode != RouteControlMode.Bezier)
            {
                incoming.positionCount = 0;
                outgoing.positionCount = 0;
                return;
            }

            Vector3 origin = point.Position + Vector3.up * 0.25f;
            Vector3 incomingEnd = origin + new Vector3(point.IncomingHandle.x, 0f, point.IncomingHandle.y);
            Vector3 outgoingEnd = origin + new Vector3(point.OutgoingHandle.x, 0f, point.OutgoingHandle.y);
            incoming.positionCount = 2;
            incoming.SetPosition(0, origin);
            incoming.SetPosition(1, incomingEnd);
            outgoing.positionCount = 2;
            outgoing.SetPosition(0, origin);
            outgoing.SetPosition(1, outgoingEnd);
            SetLineColor(
                incoming,
                index == selectedPoint && selectionKind == RoadSelectionKind.IncomingHandle
                    ? new Color(1f, 0.45f, 0.9f, 1f)
                    : new Color(0.75f, 0.45f, 1f, 0.9f));
            SetLineColor(
                outgoing,
                index == selectedPoint && selectionKind == RoadSelectionKind.OutgoingHandle
                    ? new Color(1f, 0.45f, 0.9f, 1f)
                    : new Color(0.75f, 0.45f, 1f, 0.9f));
        }

        private void RenderManipulators(
            IReadOnlyList<RoadDraftPoint> points,
            int selectedPoint,
            RoadSelectionKind selectionKind,
            float defaultLeftWidth,
            float defaultRightWidth,
            RoadDraftState state,
            RoadBuildPlan plan)
        {
            if (state != RoadDraftState.Geometry || selectedPoint < 0 || selectedPoint >= points.Count)
            {
                heightManipulator.positionCount = 0;
                leftWidthManipulator.positionCount = 0;
                rightWidthManipulator.positionCount = 0;
                return;
            }
            RoadDraftPoint point = points[selectedPoint];
            float elevation = plan != null && selectedPoint < plan.ControlPointElevations.Count &&
                !float.IsNaN(plan.ControlPointElevations[selectedPoint])
                    ? plan.ControlPointElevations[selectedPoint]
                    : point.Elevation;
            Vector3 center = new Vector3(point.Position.x, elevation + 0.22f, point.Position.z);
            Vector3 before = selectedPoint > 0 ? points[selectedPoint - 1].Position : point.Position;
            Vector3 after = selectedPoint + 1 < points.Count ? points[selectedPoint + 1].Position : point.Position;
            RoadRibbonMath.GetOffsets(
                before,
                point.Position,
                after,
                Width(point.LeftWidth, defaultLeftWidth),
                Width(point.RightWidth, defaultRightWidth),
                out Vector3 left,
                out Vector3 right);
            if (EarthWorksPlugin.Instance?.EditorCamera?.Mode == RoadCameraMode.Isometric)
            {
                SetManipulator(heightManipulator, center, center + Vector3.up * 3f,
                    selectionKind == RoadSelectionKind.HeightHandle
                        ? new Color(1f, 1f, 1f, 1f)
                        : new Color(0.2f, 1f, 0.45f, 1f));
            }
            else
            {
                heightManipulator.positionCount = 0;
            }
            SetManipulator(leftWidthManipulator, center, center + left,
                selectionKind == RoadSelectionKind.LeftWidthHandle
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 0.68f, 0.15f, 1f));
            SetManipulator(rightWidthManipulator, center, center + right,
                selectionKind == RoadSelectionKind.RightWidthHandle
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 0.68f, 0.15f, 1f));
        }

        private static void SetManipulator(LineRenderer line, Vector3 a, Vector3 b, Color color)
        {
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            SetLineColor(line, color);
        }

        private static RoadRoute BuildRoute(
            IReadOnlyList<RoadDraftPoint> points,
            IReadOnlyList<bool> straightSegments)
        {
            RouteControlPoint[] controls = new RouteControlPoint[points.Count];
            for (int i = 0; i < points.Count; ++i)
            {
                RoadDraftPoint point = points[i];
                controls[i] = new RouteControlPoint(
                    new PlanarPoint(point.Position.x, point.Position.z),
                    point.Mode,
                    new PlanarVector(point.IncomingHandle.x, point.IncomingHandle.y),
                    new PlanarVector(point.OutgoingHandle.x, point.OutgoingHandle.y),
                    point.Smoothing);
            }
            bool[] segments = new bool[points.Count - 1];
            if (straightSegments != null)
            {
                for (int i = 0; i < segments.Length && i < straightSegments.Count; ++i)
                {
                    segments[i] = straightSegments[i];
                }
            }
            return new RoadRoute(controls, segments);
        }

        private void EnsureMarkerCount(int count)
        {
            while (pointMarkers.Count < count)
            {
                pointMarkers.Add(CreateLine("EarthWorks_ControlPoint_" + pointMarkers.Count, 0.09f));
            }
        }

        private void EnsureHandleCount(int count)
        {
            while (handleLines.Count < count)
            {
                handleLines.Add(CreateLine("EarthWorks_Handle_" + handleLines.Count, 0.055f));
            }
        }

        private LineRenderer CreateLine(string name, float width)
        {
            GameObject lineObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineObject.transform.SetParent(root.transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            line.positionCount = 0;
            return line;
        }

        private static Color ControlColor(RouteControlMode mode)
        {
            switch (mode)
            {
                case RouteControlMode.Corner:
                    return new Color(1f, 0.72f, 0.2f, 0.95f);
                case RouteControlMode.Bezier:
                    return new Color(0.75f, 0.45f, 1f, 0.95f);
                case RouteControlMode.BSpline:
                    return new Color(0.38f, 1f, 0.56f, 0.95f);
                default:
                    return new Color(0.25f, 0.85f, 1f, 0.95f);
            }
        }

        private static float Width(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }
    }
}
