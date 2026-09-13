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

    internal sealed class RoadProjectRecord
    {
        private const int FormatVersion = 4;
        private const int MaximumSerializedPoints = 512;
        private const int MaximumSerializedEdits = 65536;
        private const int MaximumCompressedBytes = 4 * 1024 * 1024;

        public readonly List<RoadProjectPoint> Points = new List<RoadProjectPoint>();
        public readonly List<RoadProjectEdit> Edits = new List<RoadProjectEdit>();
        public readonly List<Vector3> CenterLine = new List<Vector3>();
        public readonly List<RoadPreviewSample> PreviewSamples = new List<RoadPreviewSample>();
        public RoadElevationMode ElevationMode;
        public RoadSurface DefaultSurface;
        public float DefaultLeftWidth;
        public float DefaultRightWidth;
        public float SingleElevation;
        public RoadLongitudinalProfile LongitudinalProfile;
        public bool FitEndpointPlanes;
        public Vector3 Center;
        public float Radius;
        public double Length;

        public byte[] Serialize()
        {
            ZPackage package = new ZPackage();
            package.Write(FormatVersion);
            package.Write((int)ElevationMode);
            package.Write((int)DefaultSurface);
            package.Write(DefaultLeftWidth);
            package.Write(DefaultRightWidth);
            package.Write(SingleElevation);
            package.Write(Center);
            package.Write(Radius);
            package.Write(Length);
            package.Write((int)LongitudinalProfile);
            package.Write(FitEndpointPlanes);
            package.Write(Points.Count);
            foreach (RoadProjectPoint point in Points)
            {
                package.Write(point.Position);
                package.Write((int)point.Mode);
                package.Write(point.Smoothing);
                package.Write(point.IncomingHandle.x);
                package.Write(point.IncomingHandle.y);
                package.Write(point.OutgoingHandle.x);
                package.Write(point.OutgoingHandle.y);
                package.Write(point.StraightAfter);
                package.Write(point.ElevationAnchored);
                package.Write(point.Elevation);
                package.Write(point.LeftWidth);
                package.Write(point.RightWidth);
            }

            package.Write(CenterLine.Count);
            foreach (Vector3 sample in CenterLine)
            {
                package.Write(sample);
            }

            package.Write(PreviewSamples.Count);
            foreach (RoadPreviewSample sample in PreviewSamples)
            {
                package.Write(sample.Position);
                package.Write(sample.LeftWidth);
                package.Write(sample.RightWidth);
                package.Write(sample.LeftHeight);
                package.Write(sample.RightHeight);
                package.Write((int)sample.Surface);
                package.Write(sample.SegmentIndex);
            }

            package.Write(Edits.Count);
            foreach (RoadProjectEdit edit in Edits)
            {
                package.Write(edit.Position);
                package.Write(edit.TargetHeight);
                package.Write(edit.PaintRoadbed);
                package.Write((int)edit.Surface);
            }
            ZPackage compressed = new ZPackage();
            compressed.WriteCompressed(package);
            return compressed.GetArray();
        }

        public static bool TryDeserialize(byte[] bytes, out RoadProjectRecord record)
        {
            record = null;
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaximumCompressedBytes)
            {
                return false;
            }

            try
            {
                ZPackage compressed = new ZPackage(bytes);
                ZPackage package = compressed.ReadCompressedPackage();
                int version = package.ReadInt();
                if (version < 1 || version > FormatVersion)
                {
                    return false;
                }

                RoadProjectRecord parsed = new RoadProjectRecord
                {
                    ElevationMode = ReadEnum<RoadElevationMode>(package.ReadInt()),
                    DefaultSurface = ReadEnum<RoadSurface>(package.ReadInt()),
                    DefaultLeftWidth = package.ReadSingle(),
                    DefaultRightWidth = package.ReadSingle(),
                    SingleElevation = package.ReadSingle(),
                    Center = package.ReadVector3(),
                    Radius = package.ReadSingle(),
                    Length = package.ReadDouble(),
                    LongitudinalProfile = RoadLongitudinalProfile.LinearJoined
                };
                if (version >= 3)
                {
                    parsed.LongitudinalProfile = ReadEnum<RoadLongitudinalProfile>(package.ReadInt());
                    parsed.FitEndpointPlanes = package.ReadBool();
                }
                if (!Finite(parsed.DefaultLeftWidth) || parsed.DefaultLeftWidth <= 0f ||
                    !Finite(parsed.DefaultRightWidth) || parsed.DefaultRightWidth <= 0f ||
                    !Finite(parsed.SingleElevation))
                {
                    return false;
                }

                int pointCount = package.ReadInt();
                if (pointCount < 2 || pointCount > MaximumSerializedPoints)
                {
                    return false;
                }
                for (int i = 0; i < pointCount; ++i)
                {
                    RoadProjectPoint point = new RoadProjectPoint
                    {
                        Position = package.ReadVector3(),
                        Mode = ReadEnum<RouteControlMode>(package.ReadInt()),
                        Smoothing = version >= 4 ? package.ReadSingle() : 0.5f,
                        IncomingHandle = new Vector2(package.ReadSingle(), package.ReadSingle()),
                        OutgoingHandle = new Vector2(package.ReadSingle(), package.ReadSingle()),
                        StraightAfter = package.ReadBool(),
                        ElevationAnchored = package.ReadBool(),
                        Elevation = package.ReadSingle(),
                        LeftWidth = package.ReadSingle(),
                        RightWidth = package.ReadSingle()
                    };
                    if (!Finite(point.Position) || !Finite(point.Smoothing) ||
                        point.Smoothing < 0f || point.Smoothing > 1f ||
                        !Finite(point.IncomingHandle) ||
                        !Finite(point.OutgoingHandle) || !Finite(point.Elevation) ||
                        !Finite(point.LeftWidth) || !Finite(point.RightWidth))
                    {
                        return false;
                    }
                    parsed.Points.Add(point);
                }

                int centerCount = package.ReadInt();
                if (centerCount < 2 || centerCount > MaximumSerializedEdits)
                {
                    return false;
                }
                for (int i = 0; i < centerCount; ++i)
                {
                    Vector3 sample = package.ReadVector3();
                    if (!Finite(sample))
                    {
                        return false;
                    }
                    parsed.CenterLine.Add(sample);
                }

                if (version >= 2)
                {
                    int previewCount = package.ReadInt();
                    if (previewCount < 2 || previewCount > MaximumSerializedEdits)
                    {
                        return false;
                    }
                    for (int i = 0; i < previewCount; ++i)
                    {
                        RoadPreviewSample sample = new RoadPreviewSample
                        {
                            Position = package.ReadVector3(),
                            LeftWidth = package.ReadSingle(),
                            RightWidth = package.ReadSingle(),
                            LeftHeight = 0f,
                            RightHeight = 0f
                        };
                        if (version >= 3)
                        {
                            sample.LeftHeight = package.ReadSingle();
                            sample.RightHeight = package.ReadSingle();
                        }
                        else
                        {
                            sample.LeftHeight = sample.Position.y;
                            sample.RightHeight = sample.Position.y;
                        }
                        sample.Surface = ReadEnum<RoadSurface>(package.ReadInt());
                        sample.SegmentIndex = package.ReadInt();
                        if (!Finite(sample.Position) || !Finite(sample.LeftWidth) ||
                            !Finite(sample.RightWidth) || !Finite(sample.LeftHeight) ||
                            !Finite(sample.RightHeight) || sample.LeftWidth <= 0f ||
                            sample.RightWidth <= 0f || sample.SegmentIndex < 0 ||
                            sample.SegmentIndex >= parsed.Points.Count - 1)
                        {
                            return false;
                        }
                        parsed.PreviewSamples.Add(sample);
                    }
                }

                int editCount = package.ReadInt();
                if (editCount < 1 || editCount > MaximumSerializedEdits)
                {
                    return false;
                }
                for (int i = 0; i < editCount; ++i)
                {
                    RoadProjectEdit edit = new RoadProjectEdit
                    {
                        Position = package.ReadVector3(),
                        TargetHeight = package.ReadSingle(),
                        PaintRoadbed = package.ReadBool(),
                        Surface = ReadEnum<RoadSurface>(package.ReadInt())
                    };
                    if (!Finite(edit.Position) || !Finite(edit.TargetHeight))
                    {
                        return false;
                    }
                    parsed.Edits.Add(edit);
                }

                if (!Finite(parsed.Center.x) || !Finite(parsed.Center.y) || !Finite(parsed.Center.z) ||
                    !Finite(parsed.Radius) || parsed.Radius <= 0f ||
                    double.IsNaN(parsed.Length) || double.IsInfinity(parsed.Length) || parsed.Length <= 0.0)
                {
                    return false;
                }

                record = parsed;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static T ReadEnum<T>(int value) where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new InvalidOperationException("Invalid project enum value.");
            }
            return (T)Enum.ToObject(typeof(T), value);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool Finite(Vector2 value)
        {
            return Finite(value.x) && Finite(value.y);
        }

        private static bool Finite(Vector3 value)
        {
            return Finite(value.x) && Finite(value.y) && Finite(value.z);
        }
    }
}
