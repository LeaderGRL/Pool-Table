using PoolTable.Core.Balls;
using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    public static class BallImpactAudioModel
    {
        public const float MinimumAudibleEnergyJoules = 0.0015f;
        public const float FullScaleEnergyJoules = 0.25f;
        public const float MinimumAudibleVolume = 0.08f;

        public static BallImpactAudioCue Evaluate(
            float relativeSpeedMetersPerSecond,
            float firstBallMassKilograms,
            float secondBallMassKilograms,
            int clipCount)
        {
            if (!IsFinitePositive(relativeSpeedMetersPerSecond)
                || !IsFinitePositive(firstBallMassKilograms)
                || !IsFinitePositive(secondBallMassKilograms)
                || clipCount <= 0)
            {
                return BallImpactAudioCue.Silent;
            }

            var reducedMassKilograms =
                firstBallMassKilograms * secondBallMassKilograms
                / (firstBallMassKilograms + secondBallMassKilograms);
            var impactEnergyJoules =
                0.5f
                * reducedMassKilograms
                * relativeSpeedMetersPerSecond
                * relativeSpeedMetersPerSecond;

            if (impactEnergyJoules < MinimumAudibleEnergyJoules)
            {
                return BallImpactAudioCue.SilentWithEnergy(impactEnergyJoules);
            }

            var normalizedEnergy = Mathf.InverseLerp(
                MinimumAudibleEnergyJoules,
                FullScaleEnergyJoules,
                impactEnergyJoules);

            // Audio amplitude scales with the square root of energy, which keeps
            // ordinary billiard contacts audible without making hard impacts clip.
            var intensity = Mathf.Sqrt(normalizedEnergy);
            var volume = Mathf.Lerp(MinimumAudibleVolume, 1f, intensity);
            var clipIndex = Mathf.Min(
                Mathf.FloorToInt(intensity * clipCount),
                clipCount - 1);

            return new BallImpactAudioCue(
                impactEnergyJoules,
                intensity,
                clipIndex,
                volume);
        }

        public static bool IsPrimaryEmitter(BallId firstBall, BallId secondBall)
        {
            return firstBall.Number < secondBall.Number;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct BallImpactAudioCue
    {
        public BallImpactAudioCue(
            float impactEnergyJoules,
            float intensity,
            int clipIndex,
            float volume)
        {
            ImpactEnergyJoules = impactEnergyJoules;
            Intensity = intensity;
            ClipIndex = clipIndex;
            Volume = volume;
        }

        public float ImpactEnergyJoules { get; }

        public float Intensity { get; }

        public int ClipIndex { get; }

        public float Volume { get; }

        public bool ShouldPlay => ClipIndex >= 0 && Volume > 0f;

        public static BallImpactAudioCue Silent => new BallImpactAudioCue(0f, 0f, -1, 0f);

        public static BallImpactAudioCue SilentWithEnergy(float impactEnergyJoules)
        {
            return new BallImpactAudioCue(impactEnergyJoules, 0f, -1, 0f);
        }
    }
}
