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

            if (!IsFinite(shotDirection) || shotDirection.sqrMagnitude <= MinimumDirectionMagnitudeSquared)
            {
                throw new System.ArgumentException("Cue strike direction must be finite and non-zero.", nameof(shotDirection));
            }

            var normalizedStrikeDirection = shotDirection.normalized;
            var planarDirection = new Vector3(normalizedStrikeDirection.x, 0f, normalizedStrikeDirection.z);
            var planarMagnitude = planarDirection.magnitude;
            if (planarMagnitude * planarMagnitude <= MinimumDirectionMagnitudeSquared)
            {
                throw new System.ArgumentException("Cue strike direction must be finite and non-zero on the table plane.", nameof(shotDirection));
            }

            planarDirection.Normalize();
            var linearVelocityChange = planarDirection * (shotSpeedChangeMetersPerSecond * planarMagnitude);

            var contactOffset = CalculateTangentContactOffset(
                normalizedStrikeDirection,
                spin,
                ballRadiusMeters);

            var inverseInertiaPerUnitMass =
                1f / (SolidSphereInertiaFactor * ballRadiusMeters * ballRadiusMeters);
            var angularVelocityChange =
                Vector3.Cross(contactOffset, linearVelocityChange) * inverseInertiaPerUnitMass;

            return new CueBallStrikeMotion(linearVelocityChange, angularVelocityChange);
        }

        public static Vector3 CalculateContactPointOffset(
            Vector3 shotDirection,
            CueBallSpin spin,
            float ballRadiusMeters)
        {
            ValidatePositiveFinite(ballRadiusMeters, nameof(ballRadiusMeters));

            if (!IsFinite(shotDirection) || shotDirection.sqrMagnitude <= MinimumDirectionMagnitudeSquared)
            {
                throw new System.ArgumentException("Cue strike direction must be finite and non-zero.", nameof(shotDirection));
            }

            var normalizedStrikeDirection = shotDirection.normalized;
            var tangentContactOffset = CalculateTangentContactOffset(
                normalizedStrikeDirection,
                spin,
                ballRadiusMeters);
            var axialDistance = Mathf.Sqrt(Mathf.Max(
                0f,
                (ballRadiusMeters * ballRadiusMeters) - tangentContactOffset.sqrMagnitude));

            return tangentContactOffset - (normalizedStrikeDirection * axialDistance);
        }

        private static Vector3 CalculateTangentContactOffset(
            Vector3 normalizedStrikeDirection,
            CueBallSpin spin,
            float ballRadiusMeters)
        {
            var cueFaceRight = Vector3.Cross(normalizedStrikeDirection, Vector3.up).normalized;
            if (cueFaceRight.sqrMagnitude <= MinimumDirectionMagnitudeSquared)
            {
                cueFaceRight = Vector3.Cross(normalizedStrikeDirection, Vector3.right).normalized;
            }

            var cueFaceUp = Vector3.Cross(cueFaceRight, normalizedStrikeDirection).normalized;
            var maximumContactOffset =
                ballRadiusMeters * BilliardsSimulationConfiguration.CueTipMaximumContactOffsetRatio;
            return maximumContactOffset
                * ((cueFaceRight * spin.Side) + (cueFaceUp * spin.Vertical));
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
