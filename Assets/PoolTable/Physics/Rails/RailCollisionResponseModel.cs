using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Rails
{
    internal readonly struct RailCollisionResponse
    {
        internal RailCollisionResponse(Vector3 linearVelocity, Vector3 angularVelocity, bool wasApplied)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            WasApplied = wasApplied;
        }

        internal Vector3 LinearVelocity { get; }

        internal Vector3 AngularVelocity { get; }

        internal bool WasApplied { get; }
    }

    internal static class RailCollisionResponseModel
    {
        private const float SolidSphereInertiaFactor = 0.4f;
        private const float MinimumPlanarNormalSquaredMagnitude = 0.000001f;
        private const float MinimumApproachSpeedMetersPerSecond = 0.000001f;

        internal static RailCollisionResponse CalculateResponse(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            Vector3 railNormal,
            float ballRadiusMeters)
        {
            var unchanged = new RailCollisionResponse(linearVelocity, angularVelocity, false);
            if (!IsFinite(linearVelocity)
                || !IsFinite(angularVelocity)
                || !IsFinite(railNormal)
                || !IsFinite(ballRadiusMeters)
                || ballRadiusMeters <= 0f)
            {
                return unchanged;
            }

            var planarNormal = new Vector3(railNormal.x, 0f, railNormal.z);
            if (planarNormal.sqrMagnitude < MinimumPlanarNormalSquaredMagnitude)
            {
                return unchanged;
            }

            planarNormal.Normalize();
            var planarLinearVelocity = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
            var incomingNormalSpeed = Vector3.Dot(planarLinearVelocity, planarNormal);
            if (incomingNormalSpeed >= -MinimumApproachSpeedMetersPerSecond)
            {
                return unchanged;
            }

            var normalImpulseSpeed =
                -(1f + BilliardsSimulationConfiguration.RailNormalRestitution) * incomingNormalSpeed;
            var nextPlanarVelocity = planarLinearVelocity + (planarNormal * normalImpulseSpeed);

            var contactOffset = -planarNormal * ballRadiusMeters;
            var railTangent = Vector3.Cross(planarNormal, Vector3.up);
            var sideSpinContactVelocity = Vector3.Cross(
                Vector3.up * angularVelocity.y,
                contactOffset);
            var tangentialContactSpeed = Vector3.Dot(
                planarLinearVelocity + sideSpinContactVelocity,
                railTangent);

            var effectiveTangentialInverseMassFactor = 1f + (1f / SolidSphereInertiaFactor);
            var unconstrainedTangentialImpulseSpeed =
                -tangentialContactSpeed / effectiveTangentialInverseMassFactor;
            var maximumTangentialImpulseSpeed =
                BilliardsSimulationConfiguration.RailTangentialFrictionCoefficient * normalImpulseSpeed;
            var tangentialImpulseSpeed = Mathf.Clamp(
                unconstrainedTangentialImpulseSpeed,
                -maximumTangentialImpulseSpeed,
                maximumTangentialImpulseSpeed);
            var tangentialVelocityChange = railTangent * tangentialImpulseSpeed;
            nextPlanarVelocity += tangentialVelocityChange;

            var inverseInertiaPerUnitMass =
                1f / (SolidSphereInertiaFactor * ballRadiusMeters * ballRadiusMeters);
            var angularVelocityChange =
                Vector3.Cross(contactOffset, tangentialVelocityChange) * inverseInertiaPerUnitMass;
            var nextAngularVelocity = angularVelocity + angularVelocityChange;
            var nextLinearVelocity = new Vector3(
                nextPlanarVelocity.x,
                linearVelocity.y,
                nextPlanarVelocity.z);

            return new RailCollisionResponse(nextLinearVelocity, nextAngularVelocity, true);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
