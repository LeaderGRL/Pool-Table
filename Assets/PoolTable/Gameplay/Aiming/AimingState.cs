using System;
using PoolTable.Core.Shots;

namespace PoolTable.Gameplay.Aiming
{
    public sealed class AimingState
    {
        private const double DegreesToRadians = Math.PI / 180.0;

        public AimingState(ShotDirection initialDirection)
        {
            Direction = initialDirection;
        }

        public ShotDirection Direction { get; private set; }

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
    }
}
