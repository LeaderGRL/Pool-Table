using PoolTable.Core.Shots;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Cue
{
    public readonly struct CueBallStrikeMotion
    {
        public CueBallStrikeMotion(Vector3 linearVelocityChange, Vector3 angularVelocityChange)
        {
            LinearVelocityChange = linearVelocityChange;
            AngularVelocityChange = angularVelocityChange;
        }

        public Vector3 LinearVelocityChange { get; }

        public Vector3 AngularVelocityChange { get; }
    }

    public static class CueBallStrikeModel
    {
        private const float SolidSphereInertiaFactor = 0.4f;
        private const float MinimumDirectionMagnitudeSquared = 0.000000000001f;

        public static CueBallStrikeMotion CalculateVelocityChange(
            Vector3 shotDirection,
            float shotSpeedChangeMetersPerSecond,
            CueBallSpin spin,
            float ballRadiusMeters)
        {
            ValidatePositiveFinite(shotSpeedChangeMetersPerSecond, nameof(shotSpeedChangeMetersPerSecond));
            ValidatePositiveFinite(ballRadiusMeters, nameof(ballRadiusMeters));

            var planarDirection = new Vector3(shotDirection.x, 0f, shotDirection.z);
            if (!IsFinite(planarDirection) || planarDirection.sqrMagnitude <= MinimumDirectionMagnitudeSquared)
            {
                throw new System.ArgumentException("Cue strike direction must be finite and non-zero on the table plane.", nameof(shotDirection));
            }

            planarDirection.Normalize();
            var linearVelocityChange = planarDirection * shotSpeedChangeMetersPerSecond;

            var cueFaceRight = Vector3.Cross(planarDirection, Vector3.up).normalized;
            var maximumContactOffset =
                ballRadiusMeters * BilliardsSimulationConfiguration.CueTipMaximumContactOffsetRatio;
            var contactOffset = maximumContactOffset
                * ((cueFaceRight * spin.Side) + (Vector3.up * spin.Vertical));

            var inverseInertiaPerUnitMass =
                1f / (SolidSphereInertiaFactor * ballRadiusMeters * ballRadiusMeters);
            var angularVelocityChange =
                Vector3.Cross(contactOffset, linearVelocityChange) * inverseInertiaPerUnitMass;

            return new CueBallStrikeMotion(linearVelocityChange, angularVelocityChange);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void ValidatePositiveFinite(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Cue strike parameters must be finite and greater than zero.");
            }
        }
    }
}
