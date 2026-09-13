using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal static class RoadTerrainPlanner
    {
        private const float DeltaSafetyMargin = 0.02f;
        private static readonly MethodInfo GetWorldBaseHeightMethod =
            AccessTools.Method(typeof(Heightmap), "GetWorldBaseHeight");

        public static RoadBuildPlan Build(
            IReadOnlyList<RoadDraftPoint> points,
            IReadOnlyList<bool> straightSegments,
            RoadElevationMode elevationMode,
            RoadLongitudinalProfile longitudinalProfile,
            bool fitEndpointPlanes,
            float singleElevation,
            float defaultLeftWidth,
            float defaultRightWidth,
            RoadSurface defaultSurface,
            IReadOnlyList<int> segmentSurfaceOverrides,
            int subdivisionsPerSegment,
            bool includeTerrainEdits = true)
        {
            RoadBuildPlan plan = new RoadBuildPlan();
            if (points == null || points.Count < 2 || straightSegments == null ||
                straightSegments.Count != points.Count - 1)
            {
                return Invalidate(plan, "Для дороги нужны минимум две корректные точки.");
            }

            RoadRoute route;
            try
            {
                route = BuildRoute(points, straightSegments);
            }
            catch (Exception exception)
            {
                EarthWorksPlugin.Log.LogWarning(exception);
                return Invalidate(plan, "Геометрия маршрута некорректна.");
            }

            int subdivisions = Mathf.Max(2, subdivisionsPerSegment);
            IReadOnlyList<RouteSample> routeSamples = RouteEvaluator.Sample(route, subdivisions);
            List<ElevationStation> elevationStations = new List<ElevationStation>(routeSamples.Count);
            for (int i = 0; i < routeSamples.Count; ++i)
            {
                RouteSample sample = routeSamples[i];
                Vector3 probe = new Vector3((float)sample.Position.X, 0f, (float)sample.Position.Z);
                if (!FootprintLoaded(probe) || !RoadTerrain.TryGetHeight(probe, out float ground))
                {
                    return Invalidate(plan, "Весь маршрут и его обочины должны быть загружены.");
                }
                if (ZoneSystem.instance && ground < ZoneSystem.instance.m_waterLevel - 0.05f)
                {
                    return Invalidate(plan, "Первая версия маршрута не прокладывает дорогу через воду.");
                }

                double? fixedElevation = null;
                int controlPointIndex = ControlPointAtSample(sample, i, routeSamples.Count, points.Count);
                if (controlPointIndex >= 0 && points[controlPointIndex].ElevationAnchored)
                {
                    fixedElevation = points[controlPointIndex].Elevation;
                }
                elevationStations.Add(new ElevationStation(sample.Distance, ground, fixedElevation));
            }

            ElevationSolution elevation;
            if (elevationMode == RoadElevationMode.SingleElevation)
            {
                elevation = ElevationSolver.SolveSingleElevation(
                    elevationStations,
                    singleElevation,
                    EarthWorksPlugin.EffectiveTerrainDelta);
            }
            else if (elevationMode == RoadElevationMode.UniformGrade)
            {
                elevation = ElevationSolver.SolveUniformGrade(
                    elevationStations,
                    points[0].Elevation,
                    points[points.Count - 1].Elevation,
                    EarthWorksPlugin.EffectiveTerrainDelta,
                    EarthWorksPlugin.EffectiveMaximumGradeRatio);
            }
            else
            {
                elevation = ElevationSolver.SolveOptimized(
                    elevationStations,
                    EarthWorksPlugin.EffectiveTerrainDelta,
                    EarthWorksPlugin.EffectiveMaximumGradeRatio);
            }
            if (!elevation.IsValid)
            {
                return Invalidate(plan, ElevationFailureText(elevation.Failure));
            }

            double[] elevations = new double[elevation.Elevations.Count];
            for (int i = 0; i < elevations.Length; ++i)
            {
                elevations[i] = elevation.Elevations[i];
            }
            ApplyLongitudinalProfile(
                routeSamples,
                elevations,
                subdivisions,
                longitudinalProfile,
                EarthWorksPlugin.EffectiveShoulderWidth);

            RoadEndpointPlane startPlane = default(RoadEndpointPlane);
            RoadEndpointPlane endPlane = default(RoadEndpointPlane);
            float endpointTransition = Mathf.Max(2f, EarthWorksPlugin.EffectiveShoulderWidth);
            if (fitEndpointPlanes)
            {
                Vector2 startAxis = new Vector2(
                    (float)(routeSamples[1].Position.X - routeSamples[0].Position.X),
                    (float)(routeSamples[1].Position.Z - routeSamples[0].Position.Z));
                int last = routeSamples.Count - 1;
                Vector2 endAxis = new Vector2(
                    (float)(routeSamples[last].Position.X - routeSamples[last - 1].Position.X),
                    (float)(routeSamples[last].Position.Z - routeSamples[last - 1].Position.Z));
                Vector3 start = new Vector3(
                    (float)routeSamples[0].Position.X,
                    points[0].Position.y,
                    (float)routeSamples[0].Position.Z);
                Vector3 end = new Vector3(
                    (float)routeSamples[last].Position.X,
                    points[points.Count - 1].Position.y,
                    (float)routeSamples[last].Position.Z);
                if (!RoadTerrain.TryFitEndpointPlane(start, startAxis, true, out startPlane, out float startGrid) ||
                    !RoadTerrain.TryFitEndpointPlane(end, endAxis, false, out endPlane, out float endGrid))
                {
                    return Invalidate(
                        plan,
                        "Не удалось построить устойчивые плоскости рельефа у A/B. Отключи Alt или перенеси крайнюю точку с обрыва.");
                }
                endpointTransition = Mathf.Max(endpointTransition, Mathf.Max(startGrid, endGrid) * 2f);
                double startDistance = routeSamples[1].Distance - routeSamples[0].Distance;
                double endDistance = routeSamples[last].Distance - routeSamples[last - 1].Distance;
                if (startDistance <= 0.0001 || endDistance <= 0.0001)
                {
                    return Invalidate(plan, "Крайний участок маршрута слишком короткий для прилегания A/B.");
                }
                double startSlope = (elevations[1] - elevations[0]) / startDistance;
                double endSlope = (elevations[last] - elevations[last - 1]) / endDistance;
                double totalLength = routeSamples[last].Distance;
                for (int i = 0; i < elevations.Length; ++i)
                {
                    elevations[i] = RoadProfileMath.CorrectEndpointSlope(
                        elevations[i],
                        routeSamples[i].Distance,
                        totalLength,
                        endpointTransition,
                        startSlope,
                        endSlope,
                        startPlane,
                        endPlane);
                }
            }

            double maximumGradeRatio = CalculateMaximumGrade(routeSamples, elevations);
            if (maximumGradeRatio > EarthWorksPlugin.EffectiveMaximumGradeRatio + 0.0001)
            {
                return Invalidate(
                    plan,
                    "Выбранный профиль полотна превышает допустимый продольный уклон.");
            }

            List<CenterSample> center = new List<CenterSample>(routeSamples.Count);
            for (int i = 0; i < routeSamples.Count; ++i)
            {
                RouteSample sample = routeSamples[i];
                RoadDraftPoint start = points[sample.SegmentIndex];
                RoadDraftPoint end = points[sample.SegmentIndex + 1];
                float t = (float)sample.SegmentT;
                float left = Mathf.Lerp(Width(start.LeftWidth, defaultLeftWidth), Width(end.LeftWidth, defaultLeftWidth), t);
                float right = Mathf.Lerp(Width(start.RightWidth, defaultRightWidth), Width(end.RightWidth, defaultRightWidth), t);
                RoadSurface surface = ResolveSurface(defaultSurface, segmentSurfaceOverrides, sample.SegmentIndex);
                Vector3 position = new Vector3(
                    (float)sample.Position.X,
                    (float)elevations[i],
                    (float)sample.Position.Z);
                float crossSlope = fitEndpointPlanes
                    ? (float)RoadProfileMath.CrossSlope(
                        sample.Distance,
                        routeSamples[routeSamples.Count - 1].Distance,
                        endpointTransition,
                        startPlane,
                        endPlane)
                    : 0f;
                center.Add(new CenterSample(
                    position,
                    left,
                    right,
                    crossSlope,
                    surface,
                    sample.SegmentIndex));
                plan.CenterLine.Add(position + Vector3.up * 0.14f);
                plan.PreviewSamples.Add(new RoadPreviewSample
                {
                    Position = position,
                    LeftWidth = left,
                    RightWidth = right,
                    LeftHeight = position.y + left * crossSlope,
                    RightHeight = position.y - right * crossSlope,
                    Surface = surface,
                    SegmentIndex = sample.SegmentIndex
                });
                int pointIndex = ControlPointAtSample(sample, i, routeSamples.Count, points.Count);
                if (pointIndex >= 0)
                {
                    while (plan.ControlPointElevations.Count <= pointIndex)
                    {
                        plan.ControlPointElevations.Add(float.NaN);
                    }
                    plan.ControlPointElevations[pointIndex] = position.y;
                }
            }

            if (!ValidateTurnWidths(center))
            {
                return Invalidate(
                    plan,
                    "Поворот слишком тесный для выбранной ширины. Уменьши ширину или увеличь радиус кривой.");
            }

            float endBlendLength = fitEndpointPlanes ||
                longitudinalProfile != RoadLongitudinalProfile.Linear
                    ? Mathf.Max(2f, EarthWorksPlugin.EffectiveShoulderWidth)
                    : 0f;
            if (!ValidateLoadedCorridor(center, endBlendLength))
            {
                return Invalidate(plan, "Весь маршрут и его обочины должны быть загружены.");
            }

            CalculateBounds(center, endBlendLength, out Vector3 planCenter, out float radius);
            plan.Center = planCenter;
            plan.Radius = radius;
            plan.Length = routeSamples[routeSamples.Count - 1].Distance;
            plan.MaximumGradePercent = (float)(maximumGradeRatio * 100.0);

            if (!includeTerrainEdits)
            {
                return plan;
            }

            Dictionary<WorldVertexKey, SharedTarget> sharedTargets =
                new Dictionary<WorldVertexKey, SharedTarget>();
            foreach (Heightmap heightmap in Heightmap.GetAllHeightmaps())
            {
                if (!heightmap || !IntersectsCircle(heightmap, planCenter, radius))
                {
                    continue;
                }

                for (int z = 0; z <= heightmap.m_width; ++z)
                {
                    for (int x = 0; x <= heightmap.m_width; ++x)
                    {
                        Vector3 vertex = RoadTerrain.GetWorldVertex(heightmap, x, z);
                        if (!TryEvaluate(center, vertex, endBlendLength, out CorridorSample corridor))
                        {
                            continue;
                        }
                        if (ZoneSystem.instance && vertex.y < ZoneSystem.instance.m_waterLevel - 0.05f)
                        {
                            Invalidate(plan, "Маршрут или его обочина попадает в воду.");
                        }

                        WorldVertexKey key = new WorldVertexKey(vertex.x, vertex.z);
                        if (!sharedTargets.TryGetValue(key, out SharedTarget shared))
                        {
                            shared = new SharedTarget(
                                corridor.TargetHeight,
                                corridor.PaintRoadbed,
                                corridor.Surface);
                            sharedTargets.Add(key, shared);
                        }

                        if (TryGetWorldBaseHeight(heightmap, vertex, out float baseHeight) &&
                            Mathf.Abs(shared.TargetHeight - baseHeight) >
                            EarthWorksPlugin.EffectiveTerrainDelta - DeltaSafetyMargin)
                        {
                            Invalidate(plan, "Дорога выходит за разрешённый сервером предел изменения высоты.");
                        }

                        if (!PrivateArea.CheckAccess(vertex, 0f, false, false))
                        {
                            Invalidate(plan, "Часть маршрута защищена охранным тотемом.");
                        }
                        if (Location.IsInsideNoBuildLocation(vertex))
                        {
                            Invalidate(plan, "Часть маршрута попадает в запретную зону мира.");
                        }

                        RoadVertexEdit edit = new RoadVertexEdit
                        {
                            Heightmap = heightmap,
                            GridX = x,
                            GridZ = z,
                            WorldPosition = new Vector3(vertex.x, shared.TargetHeight, vertex.z),
                            OriginalHeight = vertex.y,
                            TargetHeight = shared.TargetHeight,
                            PaintRoadbed = shared.PaintRoadbed,
                            Surface = shared.Surface
                        };
                        plan.Edits.Add(edit);
                        float volume = Mathf.Abs(edit.TargetHeight - edit.OriginalHeight) *
                            heightmap.m_scale * heightmap.m_scale;
                        if (edit.TargetHeight < edit.OriginalHeight)
                        {
                            plan.CutVolume += volume;
                        }
                        else
                        {
                            plan.FillVolume += volume;
                        }

                        if (plan.Edits.Count > EarthWorksPlugin.EffectiveMaximumVertices)
                        {
                            return Invalidate(
                                plan,
                                "Проект превышает безопасный лимит terrain-вершин: " +
                                EarthWorksPlugin.EffectiveMaximumVertices + ".");
                        }
                    }
                }
            }

            if (plan.Edits.Count == 0)
            {
                return Invalidate(plan, "Маршрут не затрагивает загруженные terrain-вершины.");
            }

            plan.Record = BuildRecord(
                points,
                straightSegments,
                elevationMode,
                longitudinalProfile,
                fitEndpointPlanes,
                singleElevation,
                defaultLeftWidth,
                defaultRightWidth,
                defaultSurface,
                plan);
            return plan;
        }

        internal static RoadRoute BuildRoute(
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
            return new RoadRoute(controls, straightSegments);
        }

        private static void ApplyLongitudinalProfile(
            IReadOnlyList<RouteSample> samples,
            double[] elevations,
            int subdivisions,
            RoadLongitudinalProfile profile,
            float transitionLength)
        {
            int segmentCount = (samples.Count - 1) / subdivisions;
            for (int segment = 0; segment < segmentCount; ++segment)
            {
                int start = segment * subdivisions;
                int end = start + subdivisions;
                double length = samples[end].Distance - samples[start].Distance;
                if (length <= 0.001)
                {
                    continue;
                }
                double startHeight = elevations[start];
                double delta = elevations[end] - startHeight;
                double transitionFraction = transitionLength / length;
                for (int i = start + 1; i < end; ++i)
                {
                    double t = (samples[i].Distance - samples[start].Distance) / length;
                    elevations[i] = startHeight + delta * RoadProfileMath.Evaluate(
                        profile,
                        t,
                        transitionFraction);
                }
            }
        }

        private static double CalculateMaximumGrade(
            IReadOnlyList<RouteSample> samples,
            IReadOnlyList<double> elevations)
        {
            double maximum = 0.0;
            for (int i = 1; i < samples.Count; ++i)
            {
                double distance = samples[i].Distance - samples[i - 1].Distance;
                if (distance <= 0.0001)
                {
                    continue;
                }
                maximum = Math.Max(
                    maximum,
                    Math.Abs(elevations[i] - elevations[i - 1]) / distance);
            }
            return maximum;
        }

        private static bool ValidateLoadedCorridor(
            IReadOnlyList<CenterSample> center,
            float endBlendLength)
        {
            float shoulder = EarthWorksPlugin.EffectiveShoulderWidth;
            for (int i = 0; i < center.Count; ++i)
            {
                Vector3 direction = i + 1 < center.Count
                    ? center[i + 1].Position - center[i].Position
                    : center[i].Position - center[i - 1].Position;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    return false;
                }
                direction.Normalize();
                if (!CrossSectionLoaded(center[i], center[i].Position, direction, shoulder))
                {
                    return false;
                }
            }
            if (endBlendLength > 0f)
            {
                Vector3 startDirection = center[1].Position - center[0].Position;
                Vector3 endDirection = center[center.Count - 1].Position -
                    center[center.Count - 2].Position;
                startDirection.y = 0f;
                endDirection.y = 0f;
                if (!CrossSectionLoaded(
                        center[0],
                        center[0].Position - startDirection.normalized * endBlendLength,
                        startDirection,
                        shoulder) ||
                    !CrossSectionLoaded(
                        center[center.Count - 1],
                        center[center.Count - 1].Position + endDirection.normalized * endBlendLength,
                        endDirection,
                        shoulder))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CrossSectionLoaded(
            CenterSample sample,
            Vector3 position,
            Vector3 direction,
            float shoulder)
        {
            direction.y = 0f;
            direction.Normalize();
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
            return FootprintLoaded(position) &&
                FootprintLoaded(position + normal * (sample.LeftWidth + shoulder)) &&
                FootprintLoaded(position - normal * (sample.RightWidth + shoulder));
        }

        private static bool ValidateTurnWidths(IReadOnlyList<CenterSample> center)
        {
            for (int i = 1; i < center.Count - 1; ++i)
            {
                CenterSample previous = center[i - 1];
                CenterSample current = center[i];
                CenterSample next = center[i + 1];
                if (previous.SegmentIndex != current.SegmentIndex ||
                    current.SegmentIndex != next.SegmentIndex)
                {
                    continue;
                }
                if (!OffsetCurveValidator.SupportsTurn(
                    new PlanarPoint(previous.Position.x, previous.Position.z),
                    new PlanarPoint(current.Position.x, current.Position.z),
                    new PlanarPoint(next.Position.x, next.Position.z),
                    current.LeftWidth,
                    current.RightWidth))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool FootprintLoaded(Vector3 point)
        {
            return (!ZoneSystem.instance || ZoneSystem.instance.IsZoneLoaded(point)) &&
                Heightmap.FindHeightmap(point);
        }

        private static bool TryEvaluate(
            IReadOnlyList<CenterSample> center,
            Vector3 point,
            float endBlendLength,
            out CorridorSample result)
        {
            result = default(CorridorSample);
            float bestSquared = float.MaxValue;
            int bestIndex = -1;
            float bestT = 0f;
            Vector3 bestDirection = Vector3.zero;

            // ponytail: bounded linear scan; add a spatial index only if long-route profiling needs it.
            for (int i = 0; i < center.Count - 1; ++i)
            {
                Vector3 start = center[i].Position;
                Vector3 end = center[i + 1].Position;
                Vector2 axis = new Vector2(end.x - start.x, end.z - start.z);
                float squaredLength = axis.sqrMagnitude;
                if (squaredLength < 0.0001f)
                {
                    continue;
                }
                Vector2 offset = new Vector2(point.x - start.x, point.z - start.z);
                float rawT = Vector2.Dot(offset, axis) / squaredLength;
                float segmentLength = Mathf.Sqrt(squaredLength);
                float minimumT = i == 0 && endBlendLength > 0f
                    ? -endBlendLength / segmentLength
                    : 0f;
                float maximumT = i == center.Count - 2 && endBlendLength > 0f
                    ? 1f + endBlendLength / segmentLength
                    : 1f;
                if ((i == 0 && rawT < minimumT) ||
                    (i == center.Count - 2 && rawT > maximumT))
                {
                    continue;
                }
                float t = Mathf.Clamp(rawT, minimumT, maximumT);
                Vector2 nearest = new Vector2(start.x, start.z) + axis * t;
                float squared = (new Vector2(point.x, point.z) - nearest).sqrMagnitude;
                if (squared < bestSquared)
                {
                    bestSquared = squared;
                    bestIndex = i;
                    bestT = t;
                    bestDirection = new Vector3(axis.x, 0f, axis.y).normalized;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            CenterSample a = center[bestIndex];
            CenterSample b = center[bestIndex + 1];
            Vector3 nearest3 = Vector3.LerpUnclamped(a.Position, b.Position, bestT);
            Vector3 normal3 = new Vector3(-bestDirection.z, 0f, bestDirection.x);
            float signedLateral = Vector3.Dot(point - nearest3, normal3);
            float widthT = Mathf.Clamp01(bestT);
            float roadWidth = signedLateral >= 0f
                ? Mathf.Lerp(a.LeftWidth, b.LeftWidth, widthT)
                : Mathf.Lerp(a.RightWidth, b.RightWidth, widthT);
            float shoulder = EarthWorksPlugin.EffectiveShoulderWidth;
            float distance = Mathf.Sqrt(bestSquared);
            if (distance > roadWidth + shoulder)
            {
                return false;
            }

            float lateralWeight = distance <= roadWidth || shoulder <= 0.001f
                ? 1f
                : 1f - Mathf.SmoothStep(0f, 1f, (distance - roadWidth) / shoulder);
            float outsideDistance = bestT < 0f
                ? -bestT * new Vector2(b.Position.x - a.Position.x, b.Position.z - a.Position.z).magnitude
                : bestT > 1f
                    ? (bestT - 1f) * new Vector2(
                        b.Position.x - a.Position.x,
                        b.Position.z - a.Position.z).magnitude
                    : 0f;
            float longitudinalWeight = outsideDistance <= 0f
                ? 1f
                : endBlendLength <= 0.001f
                    ? 0f
                    : Mathf.SmoothStep(0f, 1f, 1f - outsideDistance / endBlendLength);
            float crossSlope = Mathf.Lerp(a.CrossSlope, b.CrossSlope, widthT);
            float targetHeight = nearest3.y + signedLateral * crossSlope;
            result = new CorridorSample(
                Mathf.Lerp(point.y, targetHeight, lateralWeight * longitudinalWeight),
                distance <= roadWidth && longitudinalWeight > 0.001f,
                widthT < 0.5f ? a.Surface : b.Surface);
            return true;
        }

        private static RoadProjectRecord BuildRecord(
            IReadOnlyList<RoadDraftPoint> points,
            IReadOnlyList<bool> straightSegments,
            RoadElevationMode elevationMode,
            RoadLongitudinalProfile longitudinalProfile,
            bool fitEndpointPlanes,
            float singleElevation,
            float defaultLeftWidth,
            float defaultRightWidth,
            RoadSurface defaultSurface,
            RoadBuildPlan plan)
        {
            RoadProjectRecord record = new RoadProjectRecord
            {
                ElevationMode = elevationMode,
                LongitudinalProfile = longitudinalProfile,
                FitEndpointPlanes = fitEndpointPlanes,
                SingleElevation = singleElevation,
                DefaultLeftWidth = defaultLeftWidth,
                DefaultRightWidth = defaultRightWidth,
                DefaultSurface = defaultSurface,
                Center = plan.Center,
                Radius = plan.Radius,
                Length = plan.Length
            };
            for (int i = 0; i < points.Count; ++i)
            {
                RoadDraftPoint source = points[i];
                record.Points.Add(new RoadProjectPoint
                {
                    Position = source.Position,
                    Mode = source.Mode,
                    Smoothing = source.Smoothing,
                    IncomingHandle = source.IncomingHandle,
                    OutgoingHandle = source.OutgoingHandle,
                    StraightAfter = i < straightSegments.Count && straightSegments[i],
                    ElevationAnchored = source.ElevationAnchored,
                    Elevation = source.Elevation,
                    LeftWidth = source.LeftWidth,
                    RightWidth = source.RightWidth
                });
            }
            foreach (RoadVertexEdit edit in plan.Edits)
            {
                record.Edits.Add(new RoadProjectEdit
                {
                    Position = edit.WorldPosition,
                    TargetHeight = edit.TargetHeight,
                    PaintRoadbed = edit.PaintRoadbed,
                    Surface = edit.Surface
                });
            }
            foreach (Vector3 sample in plan.CenterLine)
            {
                record.CenterLine.Add(sample);
            }
            foreach (RoadPreviewSample sample in plan.PreviewSamples)
            {
                record.PreviewSamples.Add(new RoadPreviewSample
                {
                    Position = sample.Position,
                    LeftWidth = sample.LeftWidth,
                    RightWidth = sample.RightWidth,
                    LeftHeight = sample.LeftHeight,
                    RightHeight = sample.RightHeight,
                    Surface = sample.Surface,
                    SegmentIndex = sample.SegmentIndex
                });
            }
            return record;
        }

        private static void CalculateBounds(
            IReadOnlyList<CenterSample> center,
            float endBlendLength,
            out Vector3 resultCenter,
            out float radius)
        {
            Vector3 minimum = center[0].Position;
            Vector3 maximum = minimum;
            float padding = EarthWorksPlugin.EffectiveShoulderWidth + endBlendLength;
            foreach (CenterSample sample in center)
            {
                minimum = Vector3.Min(minimum, sample.Position);
                maximum = Vector3.Max(maximum, sample.Position);
                padding = Mathf.Max(padding, Mathf.Max(sample.LeftWidth, sample.RightWidth) +
                    EarthWorksPlugin.EffectiveShoulderWidth);
            }
            resultCenter = (minimum + maximum) * 0.5f;
            resultCenter.y = center[0].Position.y;
            radius = new Vector2(maximum.x - minimum.x, maximum.z - minimum.z).magnitude * 0.5f + padding + 1f;
        }

        private static bool IntersectsCircle(Heightmap heightmap, Vector3 center, float radius)
        {
            Vector3 mapCenter = heightmap.transform.position;
            float half = heightmap.m_width * heightmap.m_scale * 0.5f;
            float x = Mathf.Clamp(center.x, mapCenter.x - half, mapCenter.x + half);
            float z = Mathf.Clamp(center.z, mapCenter.z - half, mapCenter.z + half);
            float dx = center.x - x;
            float dz = center.z - z;
            return dx * dx + dz * dz <= radius * radius;
        }

        private static bool TryGetWorldBaseHeight(Heightmap heightmap, Vector3 point, out float height)
        {
            height = 0f;
            if (GetWorldBaseHeightMethod == null)
            {
                return false;
            }
            object[] arguments = { point, 0f };
            bool result = (bool)GetWorldBaseHeightMethod.Invoke(heightmap, arguments);
            height = (float)arguments[1];
            return result;
        }

        private static int ControlPointAtSample(
            RouteSample sample,
            int sampleIndex,
            int sampleCount,
            int pointCount)
        {
            if (sampleIndex == 0)
            {
                return 0;
            }
            if (sampleIndex == sampleCount - 1)
            {
                return pointCount - 1;
            }
            return Math.Abs(sample.SegmentT - 1.0) < 1e-9 ? sample.SegmentIndex + 1 : -1;
        }

        private static float Width(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

        private static RoadSurface ResolveSurface(
            RoadSurface defaultSurface,
            IReadOnlyList<int> overrides,
            int segment)
        {
            return overrides != null && segment < overrides.Count && overrides[segment] >= 0
                ? (RoadSurface)overrides[segment]
                : defaultSurface;
        }

        private static string ElevationFailureText(ElevationFailure failure)
        {
            switch (failure)
            {
                case ElevationFailure.TerrainLimitExceeded:
                    return "Профиль выходит за разрешённый сервером предел изменения высоты.";
                case ElevationFailure.GradeLimitExceeded:
                    return "Между закреплёнными высотами невозможно выдержать допустимый уклон.";
                default:
                    return "Не удалось рассчитать вертикальный профиль маршрута.";
            }
        }

        private static RoadBuildPlan Invalidate(RoadBuildPlan plan, string reason)
        {
            if (plan.IsValid)
            {
                plan.InvalidReason = reason;
            }
            plan.IsValid = false;
            return plan;
        }

        private readonly struct CenterSample
        {
            public readonly Vector3 Position;
            public readonly float LeftWidth;
            public readonly float RightWidth;
            public readonly float CrossSlope;
            public readonly RoadSurface Surface;
            public readonly int SegmentIndex;

            public CenterSample(
                Vector3 position,
                float leftWidth,
                float rightWidth,
                float crossSlope,
                RoadSurface surface,
                int segmentIndex)
            {
                Position = position;
                LeftWidth = leftWidth;
                RightWidth = rightWidth;
                CrossSlope = crossSlope;
                Surface = surface;
                SegmentIndex = segmentIndex;
            }
        }

        private readonly struct CorridorSample
        {
            public readonly float TargetHeight;
            public readonly bool PaintRoadbed;
            public readonly RoadSurface Surface;

            public CorridorSample(float targetHeight, bool paintRoadbed, RoadSurface surface)
            {
                TargetHeight = targetHeight;
                PaintRoadbed = paintRoadbed;
                Surface = surface;
            }
        }

        private readonly struct SharedTarget
        {
            public readonly float TargetHeight;
            public readonly bool PaintRoadbed;
            public readonly RoadSurface Surface;

            public SharedTarget(float targetHeight, bool paintRoadbed, RoadSurface surface)
            {
                TargetHeight = targetHeight;
                PaintRoadbed = paintRoadbed;
                Surface = surface;
            }
        }

        private readonly struct WorldVertexKey : IEquatable<WorldVertexKey>
        {
            private readonly int x;
            private readonly int z;

            public WorldVertexKey(float worldX, float worldZ)
            {
                x = Mathf.RoundToInt(worldX * 1000f);
                z = Mathf.RoundToInt(worldZ * 1000f);
            }

            public bool Equals(WorldVertexKey other)
            {
                return x == other.x && z == other.z;
            }

            public override bool Equals(object obj)
            {
                return obj is WorldVertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return x * 397 ^ z;
                }
            }
        }
    }
}
