using System;
using PoolTable.Core.Shots;

namespace PoolTable.Gameplay.Aiming
{
    public sealed class AimingState
    {
        private const double DegreesToRadians = Math.PI / 180.0;

        public AimingState(
            ShotDirection initialDirection,
            float initialElevationDegrees = 0f,
            float minimumElevationDegrees = 0f,
            float maximumElevationDegrees = 20f)
        {
            ValidateElevationBounds(minimumElevationDegrees, maximumElevationDegrees);
            ValidateFinite(initialElevationDegrees, nameof(initialElevationDegrees));

            Direction = initialDirection;
            MinimumElevationDegrees = minimumElevationDegrees;
            MaximumElevationDegrees = maximumElevationDegrees;
            ElevationDegrees = Clamp(initialElevationDegrees, minimumElevationDegrees, maximumElevationDegrees);
        }

        public ShotDirection Direction { get; private set; }

        public float ElevationDegrees { get; private set; }

        public float MinimumElevationDegrees { get; }

        public float MaximumElevationDegrees { get; }

        public void RotateDegrees(float yawDegrees)
        {
            if (float.IsNaN(yawDegrees) || float.IsInfinity(yawDegrees))
            {
                throw new ArgumentOutOfRangeException(nameof(yawDegrees), yawDegrees, "Yaw delta must be finite.");
            }

            if (yawDegrees == 0f)
            {
                return;
            }

            var radians = yawDegrees * DegreesToRadians;
            var cosine = Math.Cos(radians);
            var sine = Math.Sin(radians);
            var x = (Direction.X * cosine) + (Direction.Y * sine);
            var y = (-Direction.X * sine) + (Direction.Y * cosine);

            Direction = new ShotDirection((float)x, (float)y);
        }

        public void AdjustElevationDegrees(float deltaDegrees)
        {
            ValidateFinite(deltaDegrees, nameof(deltaDegrees));
            ElevationDegrees = Clamp(
                ElevationDegrees + deltaDegrees,
                MinimumElevationDegrees,
                MaximumElevationDegrees);
        }

        private static void ValidateElevationBounds(float minimumElevationDegrees, float maximumElevationDegrees)
        {
            ValidateFinite(minimumElevationDegrees, nameof(minimumElevationDegrees));
            ValidateFinite(maximumElevationDegrees, nameof(maximumElevationDegrees));

            if (minimumElevationDegrees < 0f || maximumElevationDegrees < minimumElevationDegrees || maximumElevationDegrees >= 90f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumElevationDegrees),
                    maximumElevationDegrees,
                    "Cue elevation bounds must satisfy 0 <= minimum <= maximum < 90 degrees.");
            }
        }

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
            }
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Min(maximum, Math.Max(minimum, value));
        }
    }
}
