using System;
using System.Collections.Generic;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal enum RoadElevationMode
    {
        // Persisted in RoadProjectRecord. Never renumber existing values.
        Automatic = 0,
        Anchored = 1,
        SingleElevation = 2,
        UniformGrade = 3
    }

    internal enum RoadSurface
    {
        // Persisted in RoadProjectRecord. Never renumber existing values.
        Bare = 0,
        Paved = 1
    }

    internal enum RoadEditorPreviewMode
    {
        Current,
        Result,
        Difference
    }

    internal enum RoadProjectStage
    {
        // Stored in the board ZDO. Never renumber existing values.
        Setup = 0,
        Marking = 1,
        Clearing = 2,
        Earthworks = 3,
        Surfacing = 4,
        Completion = 5,
        Completed = 6
    }

    internal enum RoadSelectionKind
    {
        None,
        Point,
        IncomingHandle,
        OutgoingHandle,
        HeightHandle,
        LeftWidthHandle,
        RightWidthHandle
    }

    internal struct RoadEditorManipulators
    {
        public Vector3 Center;
        public Vector3 Height;
        public Vector3 LeftWidth;
        public Vector3 RightWidth;
    }

    internal static class RoadRibbonMath
    {
        public static void GetOffsets(
            Vector3 before,
            Vector3 center,
            Vector3 after,
            float leftWidth,
            float rightWidth,
            out Vector3 left,
            out Vector3 right)
        {
            PlanarPoint previous = new PlanarPoint(before.x, before.z);
            PlanarPoint point = new PlanarPoint(center.x, center.z);
            PlanarPoint next = new PlanarPoint(after.x, after.z);
            PlanarVector leftJoin = OffsetJoin.At(previous, point, next, leftWidth);
            PlanarVector rightJoin = OffsetJoin.At(previous, point, next, -rightWidth);
            left = new Vector3((float)leftJoin.X, 0f, (float)leftJoin.Z);
            right = new Vector3((float)rightJoin.X, 0f, (float)rightJoin.Z);
        }
    }

    internal sealed class RoadDraftPoint
    {
        public Vector3 Position;
        public RouteControlMode Mode;
        public Vector2 IncomingHandle;
        public Vector2 OutgoingHandle;
        public float Smoothing = 0.5f;
        public bool ElevationAnchored;
        public float Elevation;
        public float LeftWidth = -1f;
        public float RightWidth = -1f;

        public RoadDraftPoint(Vector3 position)
        {
            Position = position;
            Elevation = position.y;
        }
    }

    internal sealed class RoadVertexEdit
    {
        public Heightmap Heightmap;
        public int GridX;
        public int GridZ;
        public Vector3 WorldPosition;
        public float OriginalHeight;
        public float TargetHeight;
        public bool PaintRoadbed;
        public RoadSurface Surface;
    }

    internal sealed class RoadBuildPlan
    {
        public readonly List<RoadVertexEdit> Edits = new List<RoadVertexEdit>();
        public readonly List<Vector3> CenterLine = new List<Vector3>();
        public readonly List<RoadPreviewSample> PreviewSamples = new List<RoadPreviewSample>();
        public readonly List<float> ControlPointElevations = new List<float>();
        public bool IsValid = true;
        public string InvalidReason = string.Empty;
        public double Length;
        public float MaximumGradePercent;
        public float CutVolume;
        public float FillVolume;
        public Vector3 Center;
        public float Radius;
        public RoadProjectRecord Record;
    }

    internal sealed class RoadPreviewSample
    {
        public Vector3 Position;
        public float LeftWidth;
        public float RightWidth;
        public float LeftHeight;
        public float RightHeight;
        public RoadSurface Surface;
        public int SegmentIndex;
    }

    internal sealed class RoadProjectPoint
    {
        public Vector3 Position;
        public RouteControlMode Mode;
        public Vector2 IncomingHandle;
        public Vector2 OutgoingHandle;
        public float Smoothing;
        public bool StraightAfter;
        public bool ElevationAnchored;
        public float Elevation;
        public float LeftWidth;
        public float RightWidth;
    }

    internal sealed class RoadProjectEdit
    {
        public Vector3 Position;
        public float TargetHeight;
        public bool PaintRoadbed;
        public RoadSurface Surface;
    }

}
