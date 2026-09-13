using System;
using System.Collections.Generic;

namespace OstrixMods.EarthWorks.Geometry
{
    public enum RoadLongitudinalProfile
    {
        // Persisted in RoadProjectRecord. Never renumber existing values.
        Linear = 0,
        LinearJoined = 1,
        SoftEnds = 2,
        Smooth = 3
    }

    public readonly struct RoadEndpointPlane
    {
        public RoadEndpointPlane(double alongSlope, double crossSlope)
        {
            AlongSlope = alongSlope;
            CrossSlope = crossSlope;
        }

        public double AlongSlope { get; }
        public double CrossSlope { get; }
    }

    public readonly struct RoadPlaneSample
    {
        public RoadPlaneSample(double along, double cross, double height)
        {
            Along = along;
            Cross = cross;
            Height = height;
        }

        public double Along { get; }
        public double Cross { get; }
        public double Height { get; }
    }

    public static class RoadProfileMath
    {
        public static double Evaluate(
            RoadLongitudinalProfile profile,
            double normalized,
            double transitionFraction)
        {
            double t = Clamp01(normalized);
            switch (profile)
            {
                case RoadLongitudinalProfile.SoftEnds:
                    double transition = Math.Min(0.25, Math.Max(0.0, transitionFraction));
                    if (transition <= 0.0001)
                    {
                        return t;
                    }
                    if (t < transition)
                    {
                        double local = t / transition;
                        return transition * (2.0 * local * local - local * local * local);
                    }
                    if (t > 1.0 - transition)
                    {
                        double local = (1.0 - t) / transition;
                        return 1.0 - transition *
                            (2.0 * local * local - local * local * local);
                    }
                    return t;
                case RoadLongitudinalProfile.Smooth:
                    return Smooth01(t);
                default:
                    return t;
            }
        }

        public static double CorrectEndpointSlope(
            double centerHeight,
            double distance,
            double totalLength,
            double transitionLength,
            double routeStartSlope,
            double routeEndSlope,
            RoadEndpointPlane startPlane,
            RoadEndpointPlane endPlane)
        {
            double transition = Math.Min(Math.Max(0.001, transitionLength), totalLength * 0.5);
            if (distance < transition)
            {
                centerHeight += (startPlane.AlongSlope - routeStartSlope) *
                    transition * EndpointCorrection(distance / transition);
            }
            else if (distance > totalLength - transition)
            {
                centerHeight -= (endPlane.AlongSlope - routeEndSlope) *
                    transition * EndpointCorrection((totalLength - distance) / transition);
            }
            return centerHeight;
        }

        public static double CrossSlope(
            double distance,
            double totalLength,
            double transitionLength,
            RoadEndpointPlane startPlane,
            RoadEndpointPlane endPlane)
        {
            if (totalLength <= 0.001)
            {
                return 0.0;
            }
            double transition = Math.Min(Math.Max(0.001, transitionLength), totalLength * 0.5);
            if (distance < transition)
            {
                return startPlane.CrossSlope * (1.0 - Smooth01(distance / transition));
            }
            if (distance > totalLength - transition)
            {
                return endPlane.CrossSlope *
                    (1.0 - Smooth01((totalLength - distance) / transition));
            }
            return 0.0;
        }

        public static bool TryFitPlane(
            IList<RoadPlaneSample> samples,
            out RoadEndpointPlane plane,
            double maximumResidual = double.PositiveInfinity)
        {
            plane = default(RoadEndpointPlane);
            if (samples == null || samples.Count < 3)
            {
                return false;
            }

            double meanAlong = 0.0;
            double meanCross = 0.0;
            double meanHeight = 0.0;
            foreach (RoadPlaneSample sample in samples)
            {
                meanAlong += sample.Along;
                meanCross += sample.Cross;
                meanHeight += sample.Height;
            }
            meanAlong /= samples.Count;
            meanCross /= samples.Count;
            meanHeight /= samples.Count;

            double alongAlong = 0.0;
            double alongCross = 0.0;
            double crossCross = 0.0;
            double alongHeight = 0.0;
            double crossHeight = 0.0;
            foreach (RoadPlaneSample sample in samples)
            {
                double along = sample.Along - meanAlong;
                double cross = sample.Cross - meanCross;
                double height = sample.Height - meanHeight;
                alongAlong += along * along;
                alongCross += along * cross;
                crossCross += cross * cross;
                alongHeight += along * height;
                crossHeight += cross * height;
            }

            double determinant = alongAlong * crossCross - alongCross * alongCross;
            if (Math.Abs(determinant) < 0.000001)
            {
                return false;
            }
            plane = new RoadEndpointPlane(
                (alongHeight * crossCross - crossHeight * alongCross) / determinant,
                (crossHeight * alongAlong - alongHeight * alongCross) / determinant);

            double intercept = meanHeight - plane.AlongSlope * meanAlong -
                plane.CrossSlope * meanCross;
            foreach (RoadPlaneSample sample in samples)
            {
                double fitted = intercept + plane.AlongSlope * sample.Along +
                    plane.CrossSlope * sample.Cross;
                if (Math.Abs(sample.Height - fitted) > maximumResidual)
                {
                    plane = default(RoadEndpointPlane);
                    return false;
                }
            }
            return true;
        }

        private static double EndpointCorrection(double normalized)
        {
            double u = Clamp01(normalized);
            double u2 = u * u;
            double u3 = u2 * u;
            double u4 = u3 * u;
            double u5 = u4 * u;
            return u - 6.0 * u3 + 8.0 * u4 - 3.0 * u5;
        }

        private static double Smooth01(double value)
        {
            double t = Clamp01(value);
            return t * t * (3.0 - 2.0 * t);
        }

        private static double Clamp01(double value)
        {
            return value <= 0.0 ? 0.0 : value >= 1.0 ? 1.0 : value;
        }
    }
}
