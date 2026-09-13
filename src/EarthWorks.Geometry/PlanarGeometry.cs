using System;

namespace OstrixMods.EarthWorks.Geometry
{
    public readonly struct PlanarPoint
    {
        public PlanarPoint(double x, double z)
        {
            X = x;
            Z = z;
        }

        public double X { get; }
        public double Z { get; }

        public static PlanarPoint Lerp(PlanarPoint from, PlanarPoint to, double amount)
        {
            return new PlanarPoint(
                from.X + (to.X - from.X) * amount,
                from.Z + (to.Z - from.Z) * amount);
        }

        public static PlanarPoint operator +(PlanarPoint point, PlanarVector vector)
        {
            return new PlanarPoint(point.X + vector.X, point.Z + vector.Z);
        }

        public static PlanarVector operator -(PlanarPoint to, PlanarPoint from)
        {
            return new PlanarVector(to.X - from.X, to.Z - from.Z);
        }
    }

    public readonly struct PlanarVector
    {
        public PlanarVector(double x, double z)
        {
            X = x;
            Z = z;
        }

        public double X { get; }
        public double Z { get; }
        public double Length => Math.Sqrt(X * X + Z * Z);
        public double LengthSquared => X * X + Z * Z;

        public PlanarVector Normalized()
        {
            double length = Length;
            return length <= 1e-12
                ? new PlanarVector(0.0, 0.0)
                : this / length;
        }

        public static double Dot(PlanarVector left, PlanarVector right)
        {
            return left.X * right.X + left.Z * right.Z;
        }

        public static PlanarVector operator +(PlanarVector left, PlanarVector right)
        {
            return new PlanarVector(left.X + right.X, left.Z + right.Z);
        }

        public static PlanarVector operator -(PlanarVector left, PlanarVector right)
        {
            return new PlanarVector(left.X - right.X, left.Z - right.Z);
        }

        public static PlanarVector operator -(PlanarVector value)
        {
            return new PlanarVector(-value.X, -value.Z);
        }

        public static PlanarVector operator *(PlanarVector value, double scale)
        {
            return new PlanarVector(value.X * scale, value.Z * scale);
        }

        public static PlanarVector operator /(PlanarVector value, double scale)
        {
            return new PlanarVector(value.X / scale, value.Z / scale);
        }
    }

    public static class OffsetCurveValidator
    {
        public static bool SupportsTurn(
            PlanarPoint previous,
            PlanarPoint current,
            PlanarPoint next,
            double leftWidth,
            double rightWidth)
        {
            PlanarVector incoming = current - previous;
            PlanarVector outgoing = next - current;
            if (incoming.Length <= 1e-9 || outgoing.Length <= 1e-9)
            {
                return false;
            }

            PlanarVector inDirection = incoming.Normalized();
            PlanarVector outDirection = outgoing.Normalized();
            double dot = Math.Max(-1.0, Math.Min(1.0, PlanarVector.Dot(inDirection, outDirection)));
            double angle = Math.Acos(dot);
            if (angle <= 1e-4)
            {
                return true;
            }

            double radius = (incoming.Length + outgoing.Length) * 0.25 / Math.Sin(angle * 0.5);
            double cross = inDirection.X * outDirection.Z - inDirection.Z * outDirection.X;
            double innerWidth = cross >= 0.0 ? leftWidth : rightWidth;
            return innerWidth <= radius * 0.9;
        }
    }

    public static class OffsetJoin
    {
        public static PlanarVector At(
            PlanarPoint previous,
            PlanarPoint current,
            PlanarPoint next,
            double width)
        {
            PlanarVector incoming = current - previous;
            PlanarVector outgoing = next - current;
            if (incoming.LengthSquared <= 1e-9) incoming = outgoing;
            if (outgoing.LengthSquared <= 1e-9) outgoing = incoming;
            incoming = incoming.Normalized();
            outgoing = outgoing.Normalized();
            PlanarVector normalIn = new PlanarVector(-incoming.Z, incoming.X);
            PlanarVector normalOut = new PlanarVector(-outgoing.Z, outgoing.X);
            PlanarVector miter = normalIn + normalOut;
            if (miter.LengthSquared <= 1e-9) miter = normalOut;
            miter = miter.Normalized();
            double denominator = Math.Max(0.34, Math.Min(1.0, Math.Abs(PlanarVector.Dot(miter, normalIn))));
            return miter * (width / denominator);
        }
    }
}
