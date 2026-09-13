using System;

namespace PoolTable.Core.Shots
{
    public readonly struct CueBallSpin : IEquatable<CueBallSpin>
    {
        private const double MaximumNormalizedMagnitudeSquared = 1.0;
        private const double NormalizedMagnitudeSquaredTolerance = 0.000001;

        public CueBallSpin(float side, float vertical)
        {
            ValidateFinite(side, nameof(side));
            ValidateFinite(vertical, nameof(vertical));

            var magnitudeSquared = ((double)side * side) + ((double)vertical * vertical);
            if (magnitudeSquared > MaximumNormalizedMagnitudeSquared + NormalizedMagnitudeSquaredTolerance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(side),
                    "Cue-ball spin must stay inside the normalized cue-tip contact disc.");
            }

            Side = side;
            Vertical = vertical;
        }

        public float Side { get; }

        public float Vertical { get; }

        public bool IsCentered => Side == 0f && Vertical == 0f;

        public bool Equals(CueBallSpin other) => Side.Equals(other.Side) && Vertical.Equals(other.Vertical);

        public override bool Equals(object obj) => obj is CueBallSpin other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Side.GetHashCode() * 397) ^ Vertical.GetHashCode();
            }
        }

        public static bool operator ==(CueBallSpin left, CueBallSpin right) => left.Equals(right);

        public static bool operator !=(CueBallSpin left, CueBallSpin right) => !left.Equals(right);

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Cue-ball spin components must be finite.");
            }
        }
    }
}
