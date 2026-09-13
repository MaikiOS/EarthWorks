using System;
using System.Collections.Generic;

namespace OstrixMods.EarthWorks.Geometry
{
    public readonly struct RouteSample
    {
        public RouteSample(int segmentIndex, double segmentT, double distance, PlanarPoint position)
        {
            SegmentIndex = segmentIndex;
            SegmentT = segmentT;
            Distance = distance;
            Position = position;
        }

        public int SegmentIndex { get; }
        public double SegmentT { get; }
        public double Distance { get; }
        public PlanarPoint Position { get; }
    }

    public static class RouteEvaluator
    {
        public static PlanarPoint Evaluate(RoadRoute route, int segmentIndex, double t)
        {
            ValidateSegment(route, segmentIndex);
            double amount = Clamp01(t);
            PlanarPoint start = route.GetPoint(segmentIndex).Position;
            PlanarPoint end = route.GetPoint(segmentIndex + 1).Position;
            if (route.IsStraightSegment(segmentIndex))
            {
                return PlanarPoint.Lerp(start, end, amount);
            }

            RouteControlPoint startPoint = route.GetPoint(segmentIndex);
            RouteControlPoint endPoint = route.GetPoint(segmentIndex + 1);
            if (startPoint.Mode != RouteControlMode.Bezier &&
                endPoint.Mode != RouteControlMode.Bezier)
            {
                return EvaluateXSpline(route, segmentIndex, amount);
            }

            PlanarVector startDerivative = startPoint.Mode == RouteControlMode.Bezier
                ? startPoint.OutgoingHandle * 3.0
                : EvaluateXSplineDerivative(route, segmentIndex, 0.0);
            PlanarVector endDerivative = endPoint.Mode == RouteControlMode.Bezier
                ? -endPoint.IncomingHandle * 3.0
                : EvaluateXSplineDerivative(route, segmentIndex, 1.0);
            double t2 = amount * amount;
            double t3 = t2 * amount;
            double h00 = 2.0 * t3 - 3.0 * t2 + 1.0;
            double h10 = t3 - 2.0 * t2 + amount;
            double h01 = -2.0 * t3 + 3.0 * t2;
            double h11 = t3 - t2;
            return new PlanarPoint(
                h00 * start.X + h10 * startDerivative.X +
                h01 * end.X + h11 * endDerivative.X,
                h00 * start.Z + h10 * startDerivative.Z +
                h01 * end.Z + h11 * endDerivative.Z);
        }

        public static PlanarVector EvaluateDerivative(RoadRoute route, int segmentIndex, double t)
        {
            ValidateSegment(route, segmentIndex);
            PlanarPoint start = route.GetPoint(segmentIndex).Position;
            PlanarPoint end = route.GetPoint(segmentIndex + 1).Position;
            if (route.IsStraightSegment(segmentIndex))
            {
                return end - start;
            }

            const double step = 1e-5;
            double amount = Clamp01(t);
            double before = Math.Max(0.0, amount - step);
            double after = Math.Min(1.0, amount + step);
            PlanarPoint left = Evaluate(route, segmentIndex, before);
            PlanarPoint right = Evaluate(route, segmentIndex, after);
            return (right - left) * (1.0 / (after - before));
        }

        public static IReadOnlyList<RouteSample> Sample(
            RoadRoute route,
            int subdivisionsPerSegment)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (subdivisionsPerSegment < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(subdivisionsPerSegment));
            }

            List<RouteSample> samples = new List<RouteSample>(
                route.SegmentCount * subdivisionsPerSegment + 1);
            double distance = 0.0;
            PlanarPoint previous = route.GetPoint(0).Position;
            samples.Add(new RouteSample(0, 0.0, 0.0, previous));

            for (int segment = 0; segment < route.SegmentCount; ++segment)
            {
                for (int step = 1; step <= subdivisionsPerSegment; ++step)
                {
                    double t = step / (double)subdivisionsPerSegment;
                    PlanarPoint position = Evaluate(route, segment, t);
                    distance += (position - previous).Length;
                    samples.Add(new RouteSample(segment, t, distance, position));
                    previous = position;
                }
            }

            return samples;
        }

        public static double ApproximateLength(RoadRoute route, int subdivisionsPerSegment)
        {
            IReadOnlyList<RouteSample> samples = Sample(route, subdivisionsPerSegment);
            return samples[samples.Count - 1].Distance;
        }

        private static PlanarPoint EvaluateXSpline(RoadRoute route, int segmentIndex, double t)
        {
            PlanarPoint p0 = route.GetPoint(Math.Max(0, segmentIndex - 1)).Position;
            PlanarPoint p1 = route.GetPoint(segmentIndex).Position;
            PlanarPoint p2 = route.GetPoint(segmentIndex + 1).Position;
            PlanarPoint p3 = route.GetPoint(Math.Min(route.PointCount - 1, segmentIndex + 2)).Position;
            double[] weights = new double[4];

            StartInfluence(
                t,
                segmentIndex == 0 ? null : route.GetPoint(segmentIndex),
                out weights[0],
                out weights[2]);
            EndInfluence(
                t,
                segmentIndex + 1 == route.PointCount - 1
                    ? null
                    : route.GetPoint(segmentIndex + 1),
                out weights[1],
                out weights[3]);

            double sum = weights[0] + weights[1] + weights[2] + weights[3];
            if (Math.Abs(sum) < 1e-12)
            {
                return PlanarPoint.Lerp(p1, p2, t);
            }
            return new PlanarPoint(
                (weights[0] * p0.X + weights[1] * p1.X + weights[2] * p2.X + weights[3] * p3.X) / sum,
                (weights[0] * p0.Z + weights[1] * p1.Z + weights[2] * p2.Z + weights[3] * p3.Z) / sum);
        }

        private static PlanarVector EvaluateXSplineDerivative(
            RoadRoute route,
            int segmentIndex,
            double t)
        {
            const double step = 1e-5;
            double before = Math.Max(0.0, t - step);
            double after = Math.Min(1.0, t + step);
            return (EvaluateXSpline(route, segmentIndex, after) -
                    EvaluateXSpline(route, segmentIndex, before)) *
                (1.0 / (after - before));
        }

        private static void StartInfluence(
            double t,
            RouteControlPoint point,
            out double previous,
            out double next)
        {
            if (point == null)
            {
                NegativeStartInfluence(t, out previous, out next);
                return;
            }
            if (point.Mode == RouteControlMode.XSpline)
            {
                BlendedStartInfluence(t, point.Smoothing, out previous, out next);
                return;
            }
            StartInfluenceAt(t, ShapeFactor(point.Mode), out previous, out next);
        }

        private static void EndInfluence(
            double t,
            RouteControlPoint point,
            out double current,
            out double following)
        {
            if (point == null)
            {
                NegativeEndInfluence(t, out current, out following);
                return;
            }
            if (point.Mode == RouteControlMode.XSpline)
            {
                BlendedEndInfluence(t, point.Smoothing, out current, out following);
                return;
            }
            EndInfluenceAt(t, ShapeFactor(point.Mode), out current, out following);
        }

        private static void BlendedStartInfluence(
            double t,
            double smoothing,
            out double previous,
            out double next)
        {
            StartInfluenceAt(t, 0.0, out double cornerPrevious, out double cornerNext);
            NegativeStartInfluence(t, out double throughPrevious, out double throughNext);
            if (smoothing <= 0.5)
            {
                double amount = smoothing * 2.0;
                previous = Lerp(cornerPrevious, throughPrevious, amount);
                next = Lerp(cornerNext, throughNext, amount);
                return;
            }
            StartInfluenceAt(t, 1.0, out double splinePrevious, out double splineNext);
            double blend = (smoothing - 0.5) * 2.0;
            previous = Lerp(throughPrevious, splinePrevious, blend);
            next = Lerp(throughNext, splineNext, blend);
        }

        private static void BlendedEndInfluence(
            double t,
            double smoothing,
            out double current,
            out double following)
        {
            EndInfluenceAt(t, 0.0, out double cornerCurrent, out double cornerFollowing);
            NegativeEndInfluence(t, out double throughCurrent, out double throughFollowing);
            if (smoothing <= 0.5)
            {
                double amount = smoothing * 2.0;
                current = Lerp(cornerCurrent, throughCurrent, amount);
                following = Lerp(cornerFollowing, throughFollowing, amount);
                return;
            }
            EndInfluenceAt(t, 1.0, out double splineCurrent, out double splineFollowing);
            double blend = (smoothing - 0.5) * 2.0;
            current = Lerp(throughCurrent, splineCurrent, blend);
            following = Lerp(throughFollowing, splineFollowing, blend);
        }

        private static void StartInfluenceAt(
            double t,
            double shape,
            out double previous,
            out double next)
        {
            if (shape < 0.0)
            {
                previous = HBlend(-t, -shape);
                next = GBlend(t, -shape);
            }
            else
            {
                PositiveStartInfluence(t, shape, out previous, out next);
            }
        }

        private static void EndInfluenceAt(
            double t,
            double shape,
            out double current,
            out double following)
        {
            if (shape < 0.0)
            {
                current = GBlend(1.0 - t, -shape);
                following = HBlend(t - 1.0, -shape);
            }
            else
            {
                PositiveEndInfluence(t, shape, out current, out following);
            }
        }

        private static void NegativeStartInfluence(double t, out double previous, out double next)
        {
            StartInfluenceAt(t, -1.0, out previous, out next);
        }

        private static void NegativeEndInfluence(double t, out double current, out double following)
        {
            EndInfluenceAt(t, -1.0, out current, out following);
        }

        private static double ShapeFactor(RouteControlMode mode)
        {
            return mode == RouteControlMode.BSpline ? 1.0 : 0.0;
        }

        // Blanc-Schlick X-Spline blending functions, SIGGRAPH 1995.
        private static double FBlend(double numerator, double denominator)
        {
            double p = 2.0 * denominator * denominator;
            double u = numerator / denominator;
            double u2 = u * u;
            return u * u2 * (10.0 - p + (2.0 * p - 15.0) * u + (6.0 - p) * u2);
        }

        private static double GBlend(double u, double q)
        {
            return u * (q + u * (2.0 * q + u *
                (8.0 - 12.0 * q + u * (14.0 * q - 11.0 + u * (4.0 - 5.0 * q)))));
        }

        private static double HBlend(double u, double q)
        {
            double u2 = u * u;
            return u * (q + u * (2.0 * q + u2 * (-2.0 * q - u * q)));
        }

        private static void PositiveStartInfluence(double t, double s, out double a0, out double a2)
        {
            a0 = t < s ? FBlend(t - s, -1.0 - s) : 0.0;
            a2 = FBlend(t + s, 1.0 + s);
        }

        private static void PositiveEndInfluence(double t, double s, out double a1, out double a3)
        {
            a1 = FBlend(t - 1.0 - s, -1.0 - s);
            a3 = t > 1.0 - s ? FBlend(t - 1.0 + s, 1.0 + s) : 0.0;
        }

        private static void ValidateSegment(RoadRoute route, int segmentIndex)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (segmentIndex < 0 || segmentIndex >= route.SegmentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentIndex));
            }
        }

        private static double Clamp01(double value)
        {
            return value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
        }

        private static double Lerp(double from, double to, double amount)
        {
            return from + (to - from) * amount;
        }
    }
}
