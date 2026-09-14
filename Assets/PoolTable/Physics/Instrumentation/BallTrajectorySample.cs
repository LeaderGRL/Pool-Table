using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    public readonly struct BallTrajectorySample
    {
        public BallTrajectorySample(
            double elapsedTimeSeconds,
            Vector3 positionMeters,
            Vector3 linearVelocityMetersPerSecond,
            Vector3 angularVelocityRadiansPerSecond,
            KineticEnergyMeasurement kineticEnergy)
        {
            ElapsedTimeSeconds = elapsedTimeSeconds;
            PositionMeters = positionMeters;
            LinearVelocityMetersPerSecond = linearVelocityMetersPerSecond;
            AngularVelocityRadiansPerSecond = angularVelocityRadiansPerSecond;
            KineticEnergy = kineticEnergy;
        }

        public double ElapsedTimeSeconds { get; }

        public Vector3 PositionMeters { get; }

        public Vector3 LinearVelocityMetersPerSecond { get; }

        public Vector3 AngularVelocityRadiansPerSecond { get; }

        public KineticEnergyMeasurement KineticEnergy { get; }
    }
}
