using System;
using System.Collections.Generic;
using OstrixMods.EarthWorks.Geometry;

namespace OstrixMods.EarthWorks.GeometryTests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Run("Route segments preserve exact endpoints", RouteEndpointsAreExact);
            Run("Forced straight segments ignore curve handles", ForcedStraightIsExact);
            Run("X-Spline control points keep tangent continuity", XSplineTangentsAreContinuous);
            Run("X-Spline smoothing exposes corner, through and B-Spline presets", XSplineSmoothingPresets);
            Run("B-Spline keeps route endpoints exact", BSplineKeepsRouteEndpointsExact);
            Run("B-Spline ignores Bezier handles", BSplineIgnoresHandles);
            Run("Corner control points keep a sharp turn", CornerTangentsAreIndependent);
            Run("Manual handles shape the curve", ManualHandlesShapeCurve);
            Run("Route samples are ordered by distance", SamplesAreDistanceOrdered);
            Run("Duplicate consecutive points are rejected", DuplicatePointsAreRejected);
            Run("Automatic elevation preserves endpoint joins", AutomaticElevationPreservesEnds);
            Run("Fixed elevation anchors remain exact", FixedAnchorIsExact);
            Run("Single elevation produces a level route", SingleElevationIsLevel);
            Run("Uniform grade interpolates exactly from A to B", UniformGradeIsExact);
            Run("Uniform grade preserves a compatible locked point", UniformGradePreservesAnchor);
            Run("Terrain delta rejects an unreachable anchor", TerrainLimitRejectsAnchor);
            Run("Grade limit rejects incompatible endpoints", GradeLimitRejectsEndpoints);
            Run("Expanded server terrain range allows the same anchor", ExpandedRangeAllowsAnchor);
            Run("Invalid station ordering is rejected", InvalidStationOrderIsRejected);
            Run("Wide inside offset rejects a pinched turn", WideOffsetRejectsPinchedTurn);
            Run("Wide offset accepts a broad turn", WideOffsetAcceptsBroadTurn);
            Run("Longitudinal profiles keep exact endpoints", ProfilesKeepExactEndpoints);
            Run("Terrain endpoint plane fit recovers both slopes", EndpointPlaneFitRecoversSlopes);
            Run("Offset joins keep full width through a corner", OffsetJoinsKeepWidth);
            Run("Terrain paint grid maps zone seams and corners", TerrainPaintGridMapsZoneSeamsAndCorners);
            Run("Terrain paint grid rejects outside coordinates", TerrainPaintGridRejectsOutsideCoordinates);

            if (failures == 0)
            {
                Console.WriteLine("All EarthWorks geometry tests passed.");
                return 0;
            }

            Console.Error.WriteLine($"{failures} EarthWorks geometry test(s) failed.");
            return 1;
        }

        private static void RouteEndpointsAreExact()
        {
            RoadRoute route = NewRoute(
                Point(0.0, 0.0),
                Point(10.0, 4.0),
                Point(20.0, 0.0));
            Near(new PlanarPoint(0.0, 0.0), RouteEvaluator.Evaluate(route, 0, 0.0));
            Near(new PlanarPoint(10.0, 4.0), RouteEvaluator.Evaluate(route, 0, 1.0));
            Near(new PlanarPoint(20.0, 0.0), RouteEvaluator.Evaluate(route, 1, 1.0));
        }

        private static void ForcedStraightIsExact()
        {
            RouteControlPoint start = new RouteControlPoint(
                new PlanarPoint(0.0, 0.0),
                RouteControlMode.Bezier,
                outgoingHandle: new PlanarVector(0.0, 20.0));
            RouteControlPoint end = new RouteControlPoint(
                new PlanarPoint(10.0, 0.0),
                RouteControlMode.Bezier,
                incomingHandle: new PlanarVector(0.0, 20.0));
            RoadRoute route = new RoadRoute(
                new[] { start, end },
                new[] { true });
            Near(new PlanarPoint(2.5, 0.0), RouteEvaluator.Evaluate(route, 0, 0.25));
            Near(new PlanarPoint(7.5, 0.0), RouteEvaluator.Evaluate(route, 0, 0.75));
        }

        private static void XSplineTangentsAreContinuous()
        {
            RoadRoute route = NewRoute(
                Point(0.0, 0.0),
                Point(10.0, 0.0),
                Point(18.0, 10.0));
            PlanarVector incoming = RouteEvaluator.EvaluateDerivative(route, 0, 1.0);
            PlanarVector outgoing = RouteEvaluator.EvaluateDerivative(route, 1, 0.0);
            Near(incoming.X, outgoing.X, 0.001);
            Near(incoming.Z, outgoing.Z, 0.001);
        }

        private static void XSplineSmoothingPresets()
        {
            RouteControlPoint start = Point(0.0, 0.0);
            RouteControlPoint end = Point(20.0, 0.0);
            RoadRoute corner = NewRoute(
                start,
                new RouteControlPoint(new PlanarPoint(10.0, 6.0), smoothing: 0.0),
                end);
            RoadRoute through = NewRoute(
                start,
                new RouteControlPoint(new PlanarPoint(10.0, 6.0), smoothing: 0.5),
                end);
            RoadRoute smooth = NewRoute(
                start,
                new RouteControlPoint(new PlanarPoint(10.0, 6.0), smoothing: 1.0),
                end);
            RoadRoute bspline = NewRoute(
                start,
                new RouteControlPoint(new PlanarPoint(10.0, 6.0), RouteControlMode.BSpline),
                end);

            PlanarVector cornerIn = RouteEvaluator.EvaluateDerivative(corner, 0, 1.0).Normalized();
            PlanarVector cornerOut = RouteEvaluator.EvaluateDerivative(corner, 1, 0.0).Normalized();
            True(PlanarVector.Dot(cornerIn, cornerOut) < 0.95);
            PlanarVector throughIn = RouteEvaluator.EvaluateDerivative(through, 0, 1.0).Normalized();
            PlanarVector throughOut = RouteEvaluator.EvaluateDerivative(through, 1, 0.0).Normalized();
            Near(throughIn.X, throughOut.X, 0.001);
            Near(throughIn.Z, throughOut.Z, 0.001);
            Near(RouteEvaluator.Evaluate(smooth, 0, 0.65), RouteEvaluator.Evaluate(bspline, 0, 0.65));
        }

        private static void BSplineKeepsRouteEndpointsExact()
        {
            RoadRoute route = NewRoute(
                new RouteControlPoint(new PlanarPoint(0.0, 0.0), RouteControlMode.BSpline),
                new RouteControlPoint(new PlanarPoint(8.0, 6.0), RouteControlMode.BSpline),
                new RouteControlPoint(new PlanarPoint(20.0, 0.0), RouteControlMode.BSpline));
            Near(new PlanarPoint(0.0, 0.0), RouteEvaluator.Evaluate(route, 0, 0.0));
            Near(new PlanarPoint(20.0, 0.0), RouteEvaluator.Evaluate(route, 1, 1.0));
            Near(
                RouteEvaluator.Evaluate(route, 0, 1.0),
                RouteEvaluator.Evaluate(route, 1, 0.0));
        }

        private static void BSplineIgnoresHandles()
        {
            RoadRoute plain = NewRoute(
                Point(0.0, 0.0),
                new RouteControlPoint(new PlanarPoint(10.0, 5.0), RouteControlMode.BSpline),
                Point(20.0, 0.0));
            RoadRoute handled = NewRoute(
                Point(0.0, 0.0),
                new RouteControlPoint(
                    new PlanarPoint(10.0, 5.0),
                    RouteControlMode.BSpline,
                    new PlanarVector(-100.0, 40.0),
                    new PlanarVector(100.0, -40.0)),
                Point(20.0, 0.0));
            Near(
                RouteEvaluator.Evaluate(plain, 0, 0.6),
                RouteEvaluator.Evaluate(handled, 0, 0.6));
        }

        private static void CornerTangentsAreIndependent()
        {
            RoadRoute route = NewRoute(
                Point(0.0, 0.0),
                new RouteControlPoint(new PlanarPoint(10.0, 0.0), RouteControlMode.Corner),
                Point(10.0, 10.0));
            PlanarVector incoming = RouteEvaluator.EvaluateDerivative(route, 0, 1.0).Normalized();
            PlanarVector outgoing = RouteEvaluator.EvaluateDerivative(route, 1, 0.0).Normalized();
            Near(0.0, PlanarVector.Dot(incoming, outgoing));
            Near(1.0, incoming.X);
            Near(1.0, outgoing.Z);
        }

        private static void ManualHandlesShapeCurve()
        {
            RouteControlPoint start = new RouteControlPoint(
                new PlanarPoint(0.0, 0.0),
                RouteControlMode.Bezier,
                outgoingHandle: new PlanarVector(3.0, 6.0));
            RouteControlPoint end = new RouteControlPoint(
                new PlanarPoint(10.0, 0.0),
                RouteControlMode.Bezier,
                incomingHandle: new PlanarVector(-3.0, 6.0));
            RoadRoute route = NewRoute(start, end);
            PlanarPoint middle = RouteEvaluator.Evaluate(route, 0, 0.5);
            Near(5.0, middle.X);
            Near(4.5, middle.Z);
        }

        private static void SamplesAreDistanceOrdered()
        {
            RoadRoute route = NewRoute(
                Point(0.0, 0.0),
                Point(10.0, 5.0),
                Point(20.0, 0.0));
            IReadOnlyList<RouteSample> samples = RouteEvaluator.Sample(route, 8);
            Equal(17, samples.Count);
            Near(new PlanarPoint(0.0, 0.0), samples[0].Position);
            Near(new PlanarPoint(20.0, 0.0), samples[samples.Count - 1].Position);
            for (int i = 1; i < samples.Count; ++i)
            {
                True(samples[i].Distance > samples[i - 1].Distance);
            }
        }

        private static void DuplicatePointsAreRejected()
        {
            Throws<ArgumentException>(() => NewRoute(Point(1.0, 2.0), Point(1.0, 2.0)));
        }

        private static void AutomaticElevationPreservesEnds()
        {
            ElevationSolution solution = ElevationSolver.SolveOptimized(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(5.0, 4.0),
                    new ElevationStation(10.0, 0.0)
                },
                8.0,
                1.0);
            True(solution.IsValid);
            Near(0.0, solution.Elevations[0]);
            Near(0.0, solution.Elevations[2]);
            True(solution.Elevations[1] > 0.0 && solution.Elevations[1] < 4.0);
            True(solution.MaximumGradeRatio <= 1.0 + 1e-9);
        }

        private static void FixedAnchorIsExact()
        {
            ElevationSolution solution = ElevationSolver.SolveOptimized(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(10.0, 1.0, 4.0),
                    new ElevationStation(20.0, 0.0)
                },
                8.0,
                0.5);
            True(solution.IsValid);
            Near(4.0, solution.Elevations[1]);
        }

        private static void SingleElevationIsLevel()
        {
            ElevationSolution solution = ElevationSolver.SolveSingleElevation(
                new[]
                {
                    new ElevationStation(0.0, 1.0),
                    new ElevationStation(5.0, 4.0),
                    new ElevationStation(12.0, -1.0)
                },
                3.0,
                8.0);
            True(solution.IsValid);
            Near(3.0, solution.Elevations[0]);
            Near(3.0, solution.Elevations[1]);
            Near(3.0, solution.Elevations[2]);
            Near(0.0, solution.MaximumGradeRatio);
        }

        private static void TerrainLimitRejectsAnchor()
        {
            ElevationSolution solution = ElevationSolver.SolveOptimized(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(10.0, 0.0, 9.0),
                    new ElevationStation(20.0, 0.0)
                },
                8.0,
                2.0);
            False(solution.IsValid);
            Equal(ElevationFailure.TerrainLimitExceeded, solution.Failure);
        }

        private static void UniformGradeIsExact()
        {
            ElevationSolution solution = ElevationSolver.SolveUniformGrade(
                new[]
                {
                    new ElevationStation(0.0, 2.0),
                    new ElevationStation(5.0, 8.0),
                    new ElevationStation(20.0, 5.0)
                },
                2.0,
                6.0,
                8.0,
                0.35);
            True(solution.IsValid);
            Near(2.0, solution.Elevations[0]);
            Near(3.0, solution.Elevations[1]);
            Near(6.0, solution.Elevations[2]);
            Near(0.2, solution.MaximumGradeRatio);
        }

        private static void UniformGradePreservesAnchor()
        {
            ElevationSolution valid = ElevationSolver.SolveUniformGrade(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(5.0, 1.0, 2.0),
                    new ElevationStation(10.0, 0.0)
                },
                0.0,
                4.0,
                8.0,
                0.5);
            True(valid.IsValid);
            Near(2.0, valid.Elevations[1]);

            ElevationSolution invalid = ElevationSolver.SolveUniformGrade(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(5.0, 1.0, 3.0),
                    new ElevationStation(10.0, 0.0)
                },
                0.0,
                4.0,
                8.0,
                0.5);
            False(invalid.IsValid);
            Equal(ElevationFailure.InvalidInput, invalid.Failure);
        }

        private static void GradeLimitRejectsEndpoints()
        {
            ElevationSolution solution = ElevationSolver.SolveOptimized(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(5.0, 10.0)
                },
                20.0,
                1.0);
            False(solution.IsValid);
            Equal(ElevationFailure.GradeLimitExceeded, solution.Failure);
        }

        private static void ExpandedRangeAllowsAnchor()
        {
            ElevationSolution solution = ElevationSolver.SolveOptimized(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(10.0, 0.0, 9.0),
                    new ElevationStation(20.0, 0.0)
                },
                12.0,
                1.0);
            True(solution.IsValid);
            Near(9.0, solution.Elevations[1]);
        }

        private static void InvalidStationOrderIsRejected()
        {
            ElevationSolution solution = ElevationSolver.SolveOptimized(
                new[]
                {
                    new ElevationStation(0.0, 0.0),
                    new ElevationStation(0.0, 1.0)
                },
                8.0,
                1.0);
            False(solution.IsValid);
            Equal(ElevationFailure.InvalidInput, solution.Failure);
        }

        private static void WideOffsetRejectsPinchedTurn()
        {
            False(OffsetCurveValidator.SupportsTurn(
                new PlanarPoint(0.0, 0.0),
                new PlanarPoint(1.0, 0.0),
                new PlanarPoint(1.0, 1.0),
                1.0,
                1.0));
        }

        private static void WideOffsetAcceptsBroadTurn()
        {
            True(OffsetCurveValidator.SupportsTurn(
                new PlanarPoint(0.0, 0.0),
                new PlanarPoint(10.0, 0.0),
                new PlanarPoint(20.0, 1.0),
                5.5,
                5.5));
        }

        private static void ProfilesKeepExactEndpoints()
        {
            foreach (RoadLongitudinalProfile profile in
                (RoadLongitudinalProfile[])Enum.GetValues(typeof(RoadLongitudinalProfile)))
            {
                Near(0.0, RoadProfileMath.Evaluate(profile, 0.0, 0.2));
                Near(1.0, RoadProfileMath.Evaluate(profile, 1.0, 0.2));
            }
            Near(0.5, RoadProfileMath.Evaluate(RoadLongitudinalProfile.Linear, 0.5, 0.2));
            Near(0.5, RoadProfileMath.Evaluate(RoadLongitudinalProfile.Smooth, 0.5, 0.2));
        }

        private static void EndpointPlaneFitRecoversSlopes()
        {
            List<RoadPlaneSample> samples = new List<RoadPlaneSample>();
            for (int along = -1; along <= 1; ++along)
            {
                for (int cross = -1; cross <= 1; ++cross)
                {
                    samples.Add(new RoadPlaneSample(
                        along,
                        cross,
                        4.0 + along * 0.25 - cross * 0.4));
                }
            }
            True(RoadProfileMath.TryFitPlane(samples, out RoadEndpointPlane plane, 0.001));
            Near(0.25, plane.AlongSlope);
            Near(-0.4, plane.CrossSlope);
            Near(-0.4, RoadProfileMath.CrossSlope(0.0, 100.0, 10.0, plane, plane));
            Near(0.0, RoadProfileMath.CrossSlope(10.0, 100.0, 10.0, plane, plane));
            Near(0.0, RoadProfileMath.CrossSlope(50.0, 100.0, 10.0, plane, plane));
            Near(-0.4, RoadProfileMath.CrossSlope(100.0, 100.0, 10.0, plane, plane));
        }

        private static void OffsetJoinsKeepWidth()
        {
            PlanarVector straight = OffsetJoin.At(
                new PlanarPoint(-10.0, 0.0),
                new PlanarPoint(0.0, 0.0),
                new PlanarPoint(10.0, 0.0),
                3.0);
            Near(0.0, straight.X);
            Near(3.0, straight.Z);

            PlanarVector corner = OffsetJoin.At(
                new PlanarPoint(-10.0, 0.0),
                new PlanarPoint(0.0, 0.0),
                new PlanarPoint(0.0, 10.0),
                3.0);
            Near(-3.0, corner.X);
            Near(3.0, corner.Z);
        }

        private static void TerrainPaintGridMapsZoneSeamsAndCorners()
        {
            const int width = 64;
            True(TerrainGrid.TryGetIndex(width, width, 17, out int westZoneEastEdge));
            True(TerrainGrid.TryGetIndex(width, 0, 17, out int eastZoneWestEdge));
            Equal(1169, westZoneEastEdge);
            Equal(1105, eastZoneWestEdge);

            True(TerrainGrid.TryGetIndex(width, 0, 0, out int southWest));
            True(TerrainGrid.TryGetIndex(width, width, 0, out int southEast));
            True(TerrainGrid.TryGetIndex(width, 0, width, out int northWest));
            True(TerrainGrid.TryGetIndex(width, width, width, out int northEast));
            Equal(0, southWest);
            Equal(64, southEast);
            Equal(4160, northWest);
            Equal(4224, northEast);
        }

        private static void TerrainPaintGridRejectsOutsideCoordinates()
        {
            False(TerrainGrid.TryGetIndex(64, -1, 0, out int negative));
            False(TerrainGrid.TryGetIndex(64, 65, 0, out int east));
            False(TerrainGrid.TryGetIndex(64, 0, 65, out int north));
            Equal(-1, negative);
            Equal(-1, east);
            Equal(-1, north);
        }

        private static RouteControlPoint Point(double x, double z)
        {
            return new RouteControlPoint(new PlanarPoint(x, z));
        }

        private static RoadRoute NewRoute(params RouteControlPoint[] points)
        {
            return new RoadRoute(points);
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception exception)
            {
                ++failures;
                Console.Error.WriteLine($"FAIL: {name}: {exception.Message}");
            }
        }

        private static void Near(double expected, double actual, double tolerance = 1e-7)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException($"Expected {expected}, got {actual}.");
            }
        }

        private static void Near(PlanarPoint expected, PlanarPoint actual)
        {
            Near(expected.X, actual.X);
            Near(expected.Z, actual.Z);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"Expected {expected}, got {actual}.");
            }
        }

        private static void True(bool condition)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Expected true.");
            }
        }

        private static void False(bool condition)
        {
            if (condition)
            {
                throw new InvalidOperationException("Expected false.");
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException($"Expected {typeof(T).Name}.");
        }
    }
}
