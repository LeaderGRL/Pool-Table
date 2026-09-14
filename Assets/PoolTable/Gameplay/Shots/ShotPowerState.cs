using System;

namespace PoolTable.Gameplay.Shots
{
    public sealed class ShotPowerState
    {
        public ShotPowerState(float maximumPullbackMeters)
        {
            if (!IsFinite(maximumPullbackMeters) || maximumPullbackMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPullbackMeters),
                    maximumPullbackMeters,
                    "Maximum cue pullback must be finite and greater than zero.");
            }

            MaximumPullbackMeters = maximumPullbackMeters;
        }

        public float MaximumPullbackMeters { get; }

        public float PullbackMeters { get; private set; }

        public float NormalizedPower => PullbackMeters / MaximumPullbackMeters;

        public bool HasUsablePower => PullbackMeters > 0f;

        public void AdjustPullback(float deltaMeters)
        {
            if (!IsFinite(deltaMeters))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaMeters),
                    deltaMeters,
                    "Cue pullback delta must be finite.");
            }

            PullbackMeters = Math.Clamp(PullbackMeters + deltaMeters, 0f, MaximumPullbackMeters);
        }

        public void Reset()
        {
            PullbackMeters = 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
