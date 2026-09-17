using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    public static class ImpactLayerAudioModel
    {
        public const float MinimumAudibleRailEnergyJoules = 0.001f;
        public const float FullScaleRailEnergyJoules = 0.12f;

        public static ImpactLayerAudioCue EvaluateRail(float impactEnergyJoules)
        {
            if (!IsFinitePositive(impactEnergyJoules)
                || impactEnergyJoules < MinimumAudibleRailEnergyJoules)
            {
                return ImpactLayerAudioCue.Silent;
            }

            var normalizedEnergy = Mathf.InverseLerp(
                MinimumAudibleRailEnergyJoules,
                FullScaleRailEnergyJoules,
                impactEnergyJoules);
            var intensity = Mathf.Sqrt(normalizedEnergy);

            return new ImpactLayerAudioCue(
                Mathf.Lerp(0.08f, 0.9f, intensity),
                Mathf.Lerp(1.06f, 0.92f, intensity));
        }

        public static ImpactLayerAudioCue EvaluateCueStrike(float normalizedPower)
        {
            if (float.IsNaN(normalizedPower) || float.IsInfinity(normalizedPower) || normalizedPower <= 0f)
            {
                return ImpactLayerAudioCue.Silent;
            }

            var intensity = Mathf.Sqrt(Mathf.Clamp01(normalizedPower));
            return new ImpactLayerAudioCue(
                Mathf.Lerp(0.18f, 1f, intensity),
                Mathf.Lerp(1.04f, 0.94f, intensity));
        }

        public static ImpactLayerAudioCue PocketCapture => new ImpactLayerAudioCue(0.42f, 0.78f);

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct ImpactLayerAudioCue
    {
        public ImpactLayerAudioCue(float volume, float pitch)
        {
            Volume = Mathf.Clamp01(volume);
            Pitch = Mathf.Clamp(pitch, -3f, 3f);
        }

        public float Volume { get; }

        public float Pitch { get; }

        public bool ShouldPlay => Volume > 0f;

        public static ImpactLayerAudioCue Silent => new ImpactLayerAudioCue(0f, 1f);
    }
}
