using System;
using PoolTable.Core.Shots;

namespace PoolTable.Gameplay.Shots
{
    public sealed class CueBallSpinState
    {
        private const float MaximumMagnitude = 1f;

        private float side;
        private float vertical;

        public CueBallSpin Spin => new CueBallSpin(side, vertical);

        public void Set(float sideValue, float verticalValue)
        {
            ValidateFinite(sideValue, nameof(sideValue));
            ValidateFinite(verticalValue, nameof(verticalValue));

            var magnitudeSquared = (sideValue * sideValue) + (verticalValue * verticalValue);
            if (magnitudeSquared > MaximumMagnitude * MaximumMagnitude)
            {
                var inverseMagnitude = MaximumMagnitude / (float)Math.Sqrt(magnitudeSquared);
                sideValue *= inverseMagnitude;
                verticalValue *= inverseMagnitude;
            }

            side = sideValue;
            vertical = verticalValue;
        }

        public void Adjust(float sideDelta, float verticalDelta)
        {
            ValidateFinite(sideDelta, nameof(sideDelta));
            ValidateFinite(verticalDelta, nameof(verticalDelta));
            Set(side + sideDelta, vertical + verticalDelta);
        }

        public void ResetToCenter()
        {
            side = 0f;
            vertical = 0f;
        }

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Cue-ball spin input must be finite.");
            }
        }
    }
}
