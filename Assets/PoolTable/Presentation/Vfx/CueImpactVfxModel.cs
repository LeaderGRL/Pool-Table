using UnityEngine;

namespace PoolTable.Presentation.Vfx
{
    public static class CueImpactVfxModel
    {
        public const float MinimumVisiblePower = 0.02f;

        public static CueImpactVfxCue Evaluate(float normalizedPower)
        {
            var power = Mathf.Clamp01(normalizedPower);
            if (power < MinimumVisiblePower)
            {
                return CueImpactVfxCue.Silent;
            }

            var intensity = Mathf.InverseLerp(MinimumVisiblePower, 1f, power);
            intensity = intensity * intensity * (3f - (2f * intensity));

            return new CueImpactVfxCue(
                Mathf.RoundToInt(Mathf.Lerp(4f, 10f, intensity)),
                Mathf.Lerp(0.10f, 0.26f, intensity),
                Mathf.Lerp(0.12f, 0.24f, intensity),
                Mathf.Lerp(0.0035f, 0.007f, intensity),
                Mathf.RoundToInt(Mathf.Lerp(2f, 5f, intensity)),
                Mathf.Lerp(0.14f, 0.34f, intensity),
                Mathf.Lerp(0.04f, 0.08f, intensity),
                Mathf.Lerp(0.0025f, 0.005f, intensity));
        }
    }

    public readonly struct CueImpactVfxCue
    {
        public CueImpactVfxCue(
            int chalkParticleCount,
            float chalkSpeedMetersPerSecond,
            float chalkLifetimeSeconds,
            float chalkSizeMeters,
            int accentParticleCount,
            float accentSpeedMetersPerSecond,
            float accentLifetimeSeconds,
            float accentSizeMeters)
        {
            ChalkParticleCount = Mathf.Max(0, chalkParticleCount);
            ChalkSpeedMetersPerSecond = Mathf.Max(0f, chalkSpeedMetersPerSecond);
            ChalkLifetimeSeconds = Mathf.Max(0f, chalkLifetimeSeconds);
            ChalkSizeMeters = Mathf.Max(0f, chalkSizeMeters);
            AccentParticleCount = Mathf.Max(0, accentParticleCount);
            AccentSpeedMetersPerSecond = Mathf.Max(0f, accentSpeedMetersPerSecond);
            AccentLifetimeSeconds = Mathf.Max(0f, accentLifetimeSeconds);
            AccentSizeMeters = Mathf.Max(0f, accentSizeMeters);
        }

        public int ChalkParticleCount { get; }

        public float ChalkSpeedMetersPerSecond { get; }

        public float ChalkLifetimeSeconds { get; }

        public float ChalkSizeMeters { get; }

        public int AccentParticleCount { get; }

        public float AccentSpeedMetersPerSecond { get; }

        public float AccentLifetimeSeconds { get; }

        public float AccentSizeMeters { get; }

        public bool ShouldPlay => ChalkParticleCount > 0 || AccentParticleCount > 0;

        public static CueImpactVfxCue Silent => default;
    }
}
