using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Cloth
{
    internal readonly struct ClothMotionState
    {
        internal ClothMotionState(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
        }

        internal Vector3 LinearVelocity { get; }

        internal Vector3 AngularVelocity { get; }
    }

    internal static class ClothResistanceModel
    {
        private const float SolidSphereInertiaFactor = 0.4f;
        private const float SlipSpeedEpsilonMetersPerSecond = 0.000001f;

        internal static ClothMotionState CalculateMotionAfterStep(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float ballRadiusMeters,
            float deltaTimeSeconds)
        {
            var motion = new ClothMotionState(linearVelocity, angularVelocity);
            if (deltaTimeSeconds <= 0f || ballRadiusMeters <= 0f)
            {
                return motion;
            }

            var slipVelocity = CalculateContactPointSlipVelocity(
                linearVelocity,
                angularVelocity,
                ballRadiusMeters);
            var slipSpeed = slipVelocity.magnitude;
            if (slipSpeed <= SlipSpeedEpsilonMetersPerSecond)
            {
                return ApplyRollingResistance(motion, ballRadiusMeters, deltaTimeSeconds);
            }

            var slidingDeceleration =
                BilliardsSimulationConfiguration.ClothSlidingDecelerationMetersPerSecondSquared;
            if (slidingDeceleration <= 0f)
            {
                return motion;
            }

            var translationalSpeedChangeToRolling =
                (SolidSphereInertiaFactor / (1f + SolidSphereInertiaFactor)) * slipSpeed;
            var timeToRolling = translationalSpeedChangeToRolling / slidingDeceleration;
            var slidingDuration = Mathf.Min(deltaTimeSeconds, timeToRolling);
            var translationalSpeedChange = slidingDeceleration * slidingDuration;
            var planarVelocityChange = -slipVelocity * (translationalSpeedChange / slipSpeed);

            motion = ApplySlidingVelocityChange(motion, planarVelocityChange, ballRadiusMeters);

            var rollingDuration = deltaTimeSeconds - slidingDuration;
            if (rollingDuration > 0f)
            {
                motion = ApplyRollingResistance(motion, ballRadiusMeters, rollingDuration);
            }

            return motion;
        }

        internal static Vector3 CalculateContactPointSlipVelocity(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float ballRadiusMeters)
        {
            if (ballRadiusMeters <= 0f)
            {
                return new Vector3(linearVelocity.x, 0f, linearVelocity.z);
            }

            var planarLinearVelocity = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
            var contactOffset = Vector3.down * ballRadiusMeters;
            return planarLinearVelocity + Vector3.Cross(angularVelocity, contactOffset);
        }

        private static ClothMotionState ApplySlidingVelocityChange(
            ClothMotionState motion,
            Vector3 planarVelocityChange,
            float ballRadiusMeters)
        {
            var nextLinearVelocity = motion.LinearVelocity + planarVelocityChange;
            nextLinearVelocity.y = motion.LinearVelocity.y;

            var contactOffset = Vector3.down * ballRadiusMeters;
            var inverseInertiaPerUnitMass = 1f / (SolidSphereInertiaFactor * ballRadiusMeters * ballRadiusMeters);
            var angularVelocityChange =
                Vector3.Cross(contactOffset, planarVelocityChange) * inverseInertiaPerUnitMass;

            return new ClothMotionState(
                nextLinearVelocity,
                motion.AngularVelocity + angularVelocityChange);
        }

        private static ClothMotionState ApplyRollingResistance(
            ClothMotionState motion,
            float ballRadiusMeters,
            float deltaTimeSeconds)
        {
            var planarVelocity = new Vector3(motion.LinearVelocity.x, 0f, motion.LinearVelocity.z);
            var planarSpeed = planarVelocity.magnitude;
            var speedReduction =
                BilliardsSimulationConfiguration.ClothRollingDecelerationMetersPerSecondSquared * deltaTimeSeconds;
            var nextPlanarSpeed = Mathf.Max(0f, planarSpeed - speedReduction);
            var nextPlanarVelocity = planarSpeed > 0f
                ? planarVelocity * (nextPlanarSpeed / planarSpeed)
                : Vector3.zero;

            var nextLinearVelocity = new Vector3(
                nextPlanarVelocity.x,
                motion.LinearVelocity.y,
                nextPlanarVelocity.z);
            var rollingAngularVelocity = Vector3.Cross(Vector3.up, nextPlanarVelocity) / ballRadiusMeters;
            var nextAngularVelocity = new Vector3(
                rollingAngularVelocity.x,
                motion.AngularVelocity.y,
                rollingAngularVelocity.z);

            return new ClothMotionState(nextLinearVelocity, nextAngularVelocity);
        }
    }
}
