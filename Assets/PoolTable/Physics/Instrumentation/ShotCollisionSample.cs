using PoolTable.Core.Balls;
using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    public readonly struct ShotCollisionSample
    {
        public ShotCollisionSample(
            double elapsedTimeSeconds,
            BallId ball,
            BallId? otherBall,
            SimulationCollisionKind kind,
            float relativeSpeedMetersPerSecond,
            float impulseNewtonSeconds,
            Vector3 contactPointMeters)
        {
            ElapsedTimeSeconds = elapsedTimeSeconds;
            Ball = ball;
            OtherBall = otherBall;
            Kind = kind;
            RelativeSpeedMetersPerSecond = relativeSpeedMetersPerSecond;
            ImpulseNewtonSeconds = impulseNewtonSeconds;
            ContactPointMeters = contactPointMeters;
        }

        public double ElapsedTimeSeconds { get; }

        public BallId Ball { get; }

        public BallId? OtherBall { get; }

        public SimulationCollisionKind Kind { get; }

        public float RelativeSpeedMetersPerSecond { get; }

        public float ImpulseNewtonSeconds { get; }

        public Vector3 ContactPointMeters { get; }
    }
}
