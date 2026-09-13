using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Cloth
{
    internal static class ClothResistanceModel
    {
        internal static Vector3 CalculateVelocityAfterStep(Vector3 velocity, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
            {
                return velocity;
            }

            var planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            var planarSpeed = planarVelocity.magnitude;
            if (planarSpeed <= 0f)
            {
                return velocity;
            }

            var speedReduction =
                BilliardsSimulationConfiguration.ClothRollingDecelerationMetersPerSecondSquared * deltaTimeSeconds;
            var nextPlanarSpeed = Mathf.Max(0f, planarSpeed - speedReduction);
            var retainedSpeed = nextPlanarSpeed / planarSpeed;

            return new Vector3(
                planarVelocity.x * retainedSpeed,
                velocity.y,
                planarVelocity.z * retainedSpeed);
        }
    }
}
