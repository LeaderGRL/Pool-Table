using UnityEngine;

namespace PoolTable.Physics.Configuration
{
    public static class BilliardsSimulationConfiguration
    {
        public const float BallMassKilograms = 0.17f;

        public const float BallLinearDamping = 0f;

        public const float BallAngularDamping = 0f;

        public const float BallMaxAngularVelocityRadiansPerSecond = 400f;

        public const float CueTipMaximumContactOffsetRatio = 0.5f;

        public const float ClothSlidingDecelerationMetersPerSecondSquared = 1.962f;

        public const float ClothRollingDecelerationMetersPerSecondSquared = 0.2f;

        public const float ClothSideSpinDecelerationRadiansPerSecondSquared = 5f;

        public const float RailNormalRestitution = 0.82f;

        public const float RailTangentialFrictionCoefficient = 0.2f;

        public const CollisionDetectionMode BallCollisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        public const RigidbodyInterpolation BallInterpolation = RigidbodyInterpolation.Interpolate;

        public const float DefaultContactOffsetMeters = 0.001f;

        public const float GlobalSleepThreshold = 0.001f;

        public const int DefaultSolverIterations = 12;

        public const int DefaultSolverVelocityIterations = 4;

        public const float FixedTimestepSeconds = 0.005f;
    }
}
