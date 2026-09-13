using System;
using OstrixMods.EarthWorks;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

internal static class Program
{
    private static int failures;

    private static int Main()
    {
        Run("Persisted enum IDs remain stable", PersistedEnumIdsRemainStable);
        Run("Road project formats v1-v4 remain readable", RoadProjectFormatsRemainReadable);
        Run("Road project v4 round-trips", RoadProjectRoundTrips);
        Run("Invalid road project payloads are rejected", InvalidPayloadsAreRejected);
        Run("Embedded English localization loads", EmbeddedEnglishLocalizationLoads);
        System.Console.WriteLine(failures == 0
            ? "All EarthWorks persistence tests passed."
            : failures + " EarthWorks persistence test(s) failed.");
        return failures == 0 ? 0 : 1;
    }

    private static void PersistedEnumIdsRemainStable()
    {
        Equal(0, (int)RoadElevationMode.Automatic);
        Equal(1, (int)RoadElevationMode.Anchored);
        Equal(2, (int)RoadElevationMode.SingleElevation);
        Equal(3, (int)RoadElevationMode.UniformGrade);
        Equal(0, (int)RoadSurface.Bare);
        Equal(1, (int)RoadSurface.Paved);
        Equal(0, (int)RouteControlMode.XSpline);
        Equal(1, (int)RouteControlMode.Corner);
        Equal(2, (int)RouteControlMode.Bezier);
        Equal(3, (int)RouteControlMode.BSpline);
        Equal(0, (int)RoadLongitudinalProfile.Linear);
        Equal(1, (int)RoadLongitudinalProfile.LinearJoined);
        Equal(2, (int)RoadLongitudinalProfile.SoftEnds);
        Equal(3, (int)RoadLongitudinalProfile.Smooth);
        Equal(0, (int)RoadProjectStage.Setup);
        Equal(6, (int)RoadProjectStage.Completed);
    }

    private static void RoadProjectFormatsRemainReadable()
    {
        for (int version = 1; version <= 4; ++version)
        {
            bool parsed = RoadProjectRecord.TryDeserialize(
                SerializeVersion(CreateRecord(), version),
                out RoadProjectRecord record);
            True(parsed, "v" + version + " rejected");
            Equal(2, record.Points.Count);
            Equal(2, record.CenterLine.Count);
            Equal(1, record.Edits.Count);
            Equal(version >= 2 ? 2 : 0, record.PreviewSamples.Count);
            Equal(
                version >= 3 ? RoadLongitudinalProfile.Smooth : RoadLongitudinalProfile.LinearJoined,
                record.LongitudinalProfile);
            Equal(version >= 4 ? 0.25f : 0.5f, record.Points[0].Smoothing);
        }
    }

    private static void RoadProjectRoundTrips()
    {
        RoadProjectRecord source = CreateRecord();
        True(RoadProjectRecord.TryDeserialize(source.Serialize(), out RoadProjectRecord parsed));
        Equal(source.Points.Count, parsed.Points.Count);
        Equal(source.PreviewSamples.Count, parsed.PreviewSamples.Count);
        Equal(source.Edits.Count, parsed.Edits.Count);
        Equal(source.Points[0].Mode, parsed.Points[0].Mode);
        Equal(source.Points[0].Smoothing, parsed.Points[0].Smoothing);
        Equal(source.LongitudinalProfile, parsed.LongitudinalProfile);
        Equal(source.FitEndpointPlanes, parsed.FitEndpointPlanes);
    }

    private static void InvalidPayloadsAreRejected()
    {
        True(!RoadProjectRecord.TryDeserialize(null, out _));
        True(!RoadProjectRecord.TryDeserialize(Array.Empty<byte>(), out _));
        True(!RoadProjectRecord.TryDeserialize(new byte[4 * 1024 * 1024 + 1], out _));
        byte[] valid = CreateRecord().Serialize();
        Array.Resize(ref valid, valid.Length / 2);
        True(!RoadProjectRecord.TryDeserialize(valid, out _));
    }

    private static void EmbeddedEnglishLocalizationLoads()
    {
        var english = EarthWorksTranslationCatalog.LoadBuiltIn("English");
        var russian = EarthWorksTranslationCatalog.LoadBuiltIn("Russian");
        Equal("Route", english["piece_name"]);
        Equal("Маршрут", russian["piece_name"]);
        Equal(english.Count, russian.Count);
    }

    private static RoadProjectRecord CreateRecord()
    {
        RoadProjectRecord record = new RoadProjectRecord
        {
            ElevationMode = RoadElevationMode.Anchored,
            DefaultSurface = RoadSurface.Paved,
            DefaultLeftWidth = 2f,
            DefaultRightWidth = 3f,
            SingleElevation = 42f,
            LongitudinalProfile = RoadLongitudinalProfile.Smooth,
            FitEndpointPlanes = true,
            Center = new Vector3(5f, 6f, 7f),
            Radius = 12f,
            Length = 20.0
        };
        record.Points.Add(new RoadProjectPoint
        {
            Position = new Vector3(0f, 1f, 0f),
            Mode = RouteControlMode.XSpline,
            Smoothing = 0.25f,
            IncomingHandle = new Vector2(-1f, 0f),
            OutgoingHandle = new Vector2(1f, 0f),
            StraightAfter = false,
            ElevationAnchored = true,
            Elevation = 1f,
            LeftWidth = 2f,
            RightWidth = 3f
        });
        record.Points.Add(new RoadProjectPoint
        {
            Position = new Vector3(10f, 2f, 0f),
            Mode = RouteControlMode.Corner,
            Smoothing = 0.75f,
            Elevation = 2f,
            LeftWidth = 2f,
            RightWidth = 3f
        });
        record.CenterLine.Add(new Vector3(0f, 1f, 0f));
        record.CenterLine.Add(new Vector3(10f, 2f, 0f));
        record.PreviewSamples.Add(new RoadPreviewSample
        {
            Position = new Vector3(0f, 1f, 0f),
            LeftWidth = 2f,
            RightWidth = 3f,
            LeftHeight = 1f,
            RightHeight = 1.1f,
            Surface = RoadSurface.Paved,
            SegmentIndex = 0
        });
        record.PreviewSamples.Add(new RoadPreviewSample
        {
            Position = new Vector3(10f, 2f, 0f),
            LeftWidth = 2f,
            RightWidth = 3f,
            LeftHeight = 2f,
            RightHeight = 2.1f,
            Surface = RoadSurface.Paved,
            SegmentIndex = 0
        });
        record.Edits.Add(new RoadProjectEdit
        {
            Position = new Vector3(1f, 1f, 1f),
            TargetHeight = 2f,
            PaintRoadbed = true,
            Surface = RoadSurface.Paved
        });
        return record;
    }

    private static byte[] SerializeVersion(RoadProjectRecord record, int version)
    {
        ZPackage package = new ZPackage();
        package.Write(version);
        package.Write((int)record.ElevationMode);
        package.Write((int)record.DefaultSurface);
        package.Write(record.DefaultLeftWidth);
        package.Write(record.DefaultRightWidth);
        package.Write(record.SingleElevation);
        package.Write(record.Center);
        package.Write(record.Radius);
        package.Write(record.Length);
        if (version >= 3)
        {
            package.Write((int)record.LongitudinalProfile);
            package.Write(record.FitEndpointPlanes);
        }
        package.Write(record.Points.Count);
        foreach (RoadProjectPoint point in record.Points)
        {
            package.Write(point.Position);
            package.Write((int)point.Mode);
            if (version >= 4)
            {
                package.Write(point.Smoothing);
            }
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
        package.Write(record.CenterLine.Count);
        foreach (Vector3 point in record.CenterLine)
        {
            package.Write(point);
        }
        if (version >= 2)
        {
            package.Write(record.PreviewSamples.Count);
            foreach (RoadPreviewSample sample in record.PreviewSamples)
            {
                package.Write(sample.Position);
                package.Write(sample.LeftWidth);
                package.Write(sample.RightWidth);
                if (version >= 3)
                {
                    package.Write(sample.LeftHeight);
                    package.Write(sample.RightHeight);
                }
                package.Write((int)sample.Surface);
                package.Write(sample.SegmentIndex);
            }
        }
        package.Write(record.Edits.Count);
        foreach (RoadProjectEdit edit in record.Edits)
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

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            System.Console.WriteLine("PASS: " + name);
        }
        catch (Exception exception)
        {
            ++failures;
            System.Console.Error.WriteLine("FAIL: " + name + ": " + exception.Message);
        }
    }

    private static void True(bool value, string message = "Expected true")
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException("Expected " + expected + ", got " + actual);
        }
    }
}
