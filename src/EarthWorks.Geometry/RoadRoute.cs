using System;
using System.Collections.Generic;

namespace OstrixMods.EarthWorks.Geometry
{
    public enum RouteControlMode
    {
        XSpline = 0,
        Corner = 1,
        Bezier = 2,
        BSpline = 3
    }

    public sealed class RouteControlPoint
    {
        public RouteControlPoint(
            PlanarPoint position,
            RouteControlMode mode = RouteControlMode.XSpline,
            PlanarVector incomingHandle = default(PlanarVector),
            PlanarVector outgoingHandle = default(PlanarVector),
            double smoothing = 0.5)
        {
            if (double.IsNaN(smoothing) || double.IsInfinity(smoothing) ||
                smoothing < 0.0 || smoothing > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(smoothing));
            }
            Position = position;
            Mode = mode;
            IncomingHandle = incomingHandle;
            OutgoingHandle = outgoingHandle;
            Smoothing = smoothing;
        }

        public PlanarPoint Position { get; }
        public RouteControlMode Mode { get; }
        public PlanarVector IncomingHandle { get; }
        public PlanarVector OutgoingHandle { get; }
        public double Smoothing { get; }
    }

    public sealed class RoadRoute
    {
        private readonly RouteControlPoint[] points;
        private readonly bool[] straightSegments;

        public RoadRoute(
            IReadOnlyList<RouteControlPoint> points,
            IReadOnlyList<bool> straightSegments = null)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }
            if (points.Count < 2)
            {
                throw new ArgumentException("A route needs at least two control points.", nameof(points));
            }

            this.points = new RouteControlPoint[points.Count];
            for (int i = 0; i < points.Count; ++i)
            {
                this.points[i] = points[i] ??
                    throw new ArgumentException("Route control points cannot be null.", nameof(points));
                if (i > 0 &&
                    (this.points[i].Position - this.points[i - 1].Position).LengthSquared <= 1e-12)
                {
                    throw new ArgumentException(
                        "Consecutive route control points must be different.",
                        nameof(points));
                }
            }

            this.straightSegments = new bool[points.Count - 1];
            if (straightSegments == null)
            {
                return;
            }
            if (straightSegments.Count != this.straightSegments.Length)
            {
                throw new ArgumentException(
                    "The straight-segment list must match the route segment count.",
                    nameof(straightSegments));
            }

            for (int i = 0; i < this.straightSegments.Length; ++i)
            {
                this.straightSegments[i] = straightSegments[i];
            }
        }

        public int PointCount => points.Length;
        public int SegmentCount => points.Length - 1;

        public RouteControlPoint GetPoint(int index)
        {
            return points[index];
        }

        public bool IsStraightSegment(int index)
        {
            return straightSegments[index];
        }
    }
}
