using System;
using System.Collections.Generic;

namespace OstrixMods.EarthWorks.Geometry
{
    public readonly struct ElevationStation
    {
        public ElevationStation(
            double distance,
            double groundElevation,
            double? fixedElevation = null)
        {
            Distance = distance;
            GroundElevation = groundElevation;
            FixedElevation = fixedElevation;
        }

        public double Distance { get; }
        public double GroundElevation { get; }
        public double? FixedElevation { get; }
    }

    public enum ElevationFailure
    {
        None,
        InvalidInput,
        TerrainLimitExceeded,
        GradeLimitExceeded
    }

    public sealed class ElevationSolution
    {
        internal ElevationSolution(
            ElevationFailure failure,
            double[] elevations,
            double maximumGradeRatio)
        {
            Failure = failure;
            Elevations = elevations ?? Array.Empty<double>();
            MaximumGradeRatio = maximumGradeRatio;
        }

        public bool IsValid => Failure == ElevationFailure.None;
        public ElevationFailure Failure { get; }
        public IReadOnlyList<double> Elevations { get; }
        public double MaximumGradeRatio { get; }
    }

    public static class ElevationSolver
    {
        public static ElevationSolution SolveOptimized(
            IReadOnlyList<ElevationStation> stations,
            double maximumTerrainDelta,
            double maximumGradeRatio)
        {
            if (!TryValidate(stations, maximumTerrainDelta, maximumGradeRatio))
            {
                return Failure(ElevationFailure.InvalidInput);
            }

            int count = stations.Count;
            double[] lower = new double[count];
            double[] upper = new double[count];
            bool[] fixedPoints = new bool[count];
            for (int i = 0; i < count; ++i)
            {
                ElevationStation station = stations[i];
                double terrainLow = station.GroundElevation - maximumTerrainDelta;
                double terrainHigh = station.GroundElevation + maximumTerrainDelta;
                bool isEndpoint = i == 0 || i == count - 1;
                if (station.FixedElevation.HasValue || isEndpoint)
                {
                    double fixedHeight = station.FixedElevation ?? station.GroundElevation;
                    if (fixedHeight < terrainLow - 1e-9 || fixedHeight > terrainHigh + 1e-9)
                    {
                        return Failure(ElevationFailure.TerrainLimitExceeded);
                    }
                    lower[i] = fixedHeight;
                    upper[i] = fixedHeight;
                    fixedPoints[i] = true;
                }
                else
                {
                    lower[i] = terrainLow;
                    upper[i] = terrainHigh;
                }
            }

            if (!TightenGradeRanges(stations, maximumGradeRatio, lower, upper))
            {
                return Failure(ElevationFailure.GradeLimitExceeded);
            }

            double[] heights = new double[count];
            heights[0] = Clamp(stations[0].GroundElevation, lower[0], upper[0]);
            for (int i = 1; i < count; ++i)
            {
                double maximumRise = maximumGradeRatio *
                    (stations[i].Distance - stations[i - 1].Distance);
                double localLow = Math.Max(lower[i], heights[i - 1] - maximumRise);
                double localHigh = Math.Min(upper[i], heights[i - 1] + maximumRise);
                if (localLow > localHigh + 1e-9)
                {
                    return Failure(ElevationFailure.GradeLimitExceeded);
                }
                heights[i] = Clamp(stations[i].GroundElevation, localLow, localHigh);
            }

            RelaxProfile(stations, maximumGradeRatio, lower, upper, fixedPoints, heights);
            return Success(stations, heights);
        }

        public static ElevationSolution SolveSingleElevation(
            IReadOnlyList<ElevationStation> stations,
            double elevation,
            double maximumTerrainDelta)
        {
            if (!TryValidate(stations, maximumTerrainDelta, 0.0))
            {
                return Failure(ElevationFailure.InvalidInput);
            }

            double[] heights = new double[stations.Count];
            for (int i = 0; i < stations.Count; ++i)
            {
                ElevationStation station = stations[i];
                if (Math.Abs(elevation - station.GroundElevation) > maximumTerrainDelta + 1e-9)
                {
                    return Failure(ElevationFailure.TerrainLimitExceeded);
                }
                if (station.FixedElevation.HasValue &&
                    Math.Abs(elevation - station.FixedElevation.Value) > 1e-9)
                {
                    return Failure(ElevationFailure.InvalidInput);
                }
                heights[i] = elevation;
            }

            return new ElevationSolution(ElevationFailure.None, heights, 0.0);
        }

        public static ElevationSolution SolveUniformGrade(
            IReadOnlyList<ElevationStation> stations,
            double startElevation,
            double endElevation,
            double maximumTerrainDelta,
            double maximumGradeRatio)
        {
            if (!TryValidate(stations, maximumTerrainDelta, maximumGradeRatio) ||
                double.IsNaN(startElevation) || double.IsInfinity(startElevation) ||
                double.IsNaN(endElevation) || double.IsInfinity(endElevation))
            {
                return Failure(ElevationFailure.InvalidInput);
            }

            double length = stations[stations.Count - 1].Distance - stations[0].Distance;
            double grade = Math.Abs(endElevation - startElevation) / length;
            if (grade > maximumGradeRatio + 1e-9)
            {
                return Failure(ElevationFailure.GradeLimitExceeded);
            }

            double[] heights = new double[stations.Count];
            for (int i = 0; i < stations.Count; ++i)
            {
                ElevationStation station = stations[i];
                double amount = (station.Distance - stations[0].Distance) / length;
                double height = startElevation + (endElevation - startElevation) * amount;
                if (Math.Abs(height - station.GroundElevation) > maximumTerrainDelta + 1e-9)
                {
                    return Failure(ElevationFailure.TerrainLimitExceeded);
                }
                if (station.FixedElevation.HasValue &&
                    Math.Abs(height - station.FixedElevation.Value) > 1e-9)
                {
                    return Failure(ElevationFailure.InvalidInput);
                }
                heights[i] = height;
            }

            return new ElevationSolution(ElevationFailure.None, heights, grade);
        }

        private static bool TryValidate(
            IReadOnlyList<ElevationStation> stations,
            double maximumTerrainDelta,
            double maximumGradeRatio)
        {
            if (stations == null || stations.Count < 2 ||
                double.IsNaN(maximumTerrainDelta) || maximumTerrainDelta < 0.0 ||
                double.IsNaN(maximumGradeRatio) || maximumGradeRatio < 0.0)
            {
                return false;
            }

            for (int i = 0; i < stations.Count; ++i)
            {
                ElevationStation station = stations[i];
                if (double.IsNaN(station.Distance) || double.IsInfinity(station.Distance) ||
                    double.IsNaN(station.GroundElevation) ||
                    double.IsInfinity(station.GroundElevation) ||
                    (station.FixedElevation.HasValue &&
                     (double.IsNaN(station.FixedElevation.Value) ||
                      double.IsInfinity(station.FixedElevation.Value))))
                {
                    return false;
                }
                if (i > 0 && station.Distance <= stations[i - 1].Distance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TightenGradeRanges(
            IReadOnlyList<ElevationStation> stations,
            double maximumGradeRatio,
            double[] lower,
            double[] upper)
        {
            for (int pass = 0; pass < stations.Count; ++pass)
            {
                bool changed = false;
                for (int i = 1; i < stations.Count; ++i)
                {
                    double rise = maximumGradeRatio *
                        (stations[i].Distance - stations[i - 1].Distance);
                    changed |= Tighten(
                        i,
                        lower[i - 1] - rise,
                        upper[i - 1] + rise,
                        lower,
                        upper);
                    if (lower[i] > upper[i] + 1e-9)
                    {
                        return false;
                    }
                }

                for (int i = stations.Count - 2; i >= 0; --i)
                {
                    double rise = maximumGradeRatio *
                        (stations[i + 1].Distance - stations[i].Distance);
                    changed |= Tighten(
                        i,
                        lower[i + 1] - rise,
                        upper[i + 1] + rise,
                        lower,
                        upper);
                    if (lower[i] > upper[i] + 1e-9)
                    {
                        return false;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            return true;
        }

        private static bool Tighten(
            int index,
            double minimum,
            double maximum,
            double[] lower,
            double[] upper)
        {
            double newLower = Math.Max(lower[index], minimum);
            double newUpper = Math.Min(upper[index], maximum);
            bool changed = newLower > lower[index] + 1e-12 ||
                newUpper < upper[index] - 1e-12;
            lower[index] = newLower;
            upper[index] = newUpper;
            return changed;
        }

        private static void RelaxProfile(
            IReadOnlyList<ElevationStation> stations,
            double maximumGradeRatio,
            double[] lower,
            double[] upper,
            bool[] fixedPoints,
            double[] heights)
        {
            for (int pass = 0; pass < 48; ++pass)
            {
                double maximumChange = 0.0;
                for (int i = 1; i < stations.Count - 1; ++i)
                {
                    if (fixedPoints[i])
                    {
                        continue;
                    }

                    double beforeDistance = stations[i].Distance - stations[i - 1].Distance;
                    double afterDistance = stations[i + 1].Distance - stations[i].Distance;
                    double beforeRise = maximumGradeRatio * beforeDistance;
                    double afterRise = maximumGradeRatio * afterDistance;
                    double localLow = Math.Max(
                        lower[i],
                        Math.Max(heights[i - 1] - beforeRise, heights[i + 1] - afterRise));
                    double localHigh = Math.Min(
                        upper[i],
                        Math.Min(heights[i - 1] + beforeRise, heights[i + 1] + afterRise));
                    double linear =
                        (heights[i - 1] * afterDistance + heights[i + 1] * beforeDistance) /
                        (beforeDistance + afterDistance);
                    double target = linear * 0.75 + stations[i].GroundElevation * 0.25;
                    double next = Clamp(target, localLow, localHigh);
                    maximumChange = Math.Max(maximumChange, Math.Abs(next - heights[i]));
                    heights[i] = next;
                }

                if (maximumChange < 1e-7)
                {
                    break;
                }
            }
        }

        private static ElevationSolution Success(
            IReadOnlyList<ElevationStation> stations,
            double[] heights)
        {
            double maximumGrade = 0.0;
            for (int i = 1; i < stations.Count; ++i)
            {
                maximumGrade = Math.Max(
                    maximumGrade,
                    Math.Abs(heights[i] - heights[i - 1]) /
                    (stations[i].Distance - stations[i - 1].Distance));
            }
            return new ElevationSolution(ElevationFailure.None, heights, maximumGrade);
        }

        private static ElevationSolution Failure(ElevationFailure failure)
        {
            return new ElevationSolution(failure, null, 0.0);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }
}
