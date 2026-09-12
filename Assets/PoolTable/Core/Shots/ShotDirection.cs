using System;

namespace PoolTable.Core.Shots
{
    public readonly struct ShotDirection : IEquatable<ShotDirection>
    {
        private const double MinimumMagnitudeSquared = 1e-12;

        public ShotDirection(float x, float y)
        {
            ValidateFinite(x, nameof(x));
            ValidateFinite(y, nameof(y));

            var magnitudeSquared = ((double)x * x) + ((double)y * y);
            if (magnitudeSquared <= MinimumMagnitudeSquared)
            {
                throw new ArgumentException("Shot direction must have a non-zero magnitude.");
            }

            var magnitude = Math.Sqrt(magnitudeSquared);
            X = (float)(x / magnitude);
            Y = (float)(y / magnitude);
        }

        public float X { get; }

        public float Y { get; }

        public bool Equals(ShotDirection other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is ShotDirection other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(ShotDirection left, ShotDirection right) => left.Equals(right);

        public static bool operator !=(ShotDirection left, ShotDirection right) => !left.Equals(right);

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Shot direction components must be finite.");
            }
        }
    }
}
