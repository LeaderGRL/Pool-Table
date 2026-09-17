using UnityEngine;

namespace PoolTable.Presentation.Vfx
{
    public static class PocketCaptureVfxModel
    {
        public static PocketCaptureVfxCue Evaluate()
        {
            return new PocketCaptureVfxCue(
                16,
                0.22f,
                0.26f,
                0.014f,
                6,
                0.34f,
                0.11f,
                0.009f);
        }
    }

    public readonly struct PocketCaptureVfxCue
    {
        public PocketCaptureVfxCue(
            int dustParticleCount,
            float dustSpeedMetersPerSecond,
            float dustLifetimeSeconds,
            float dustSizeMeters,
            int accentParticleCount,
            float accentSpeedMetersPerSecond,
            float accentLifetimeSeconds,
            float accentSizeMeters)
        {
            DustParticleCount = Mathf.Max(0, dustParticleCount);
            DustSpeedMetersPerSecond = Mathf.Max(0f, dustSpeedMetersPerSecond);
            DustLifetimeSeconds = Mathf.Max(0f, dustLifetimeSeconds);
            DustSizeMeters = Mathf.Max(0f, dustSizeMeters);
            AccentParticleCount = Mathf.Max(0, accentParticleCount);
            AccentSpeedMetersPerSecond = Mathf.Max(0f, accentSpeedMetersPerSecond);
            AccentLifetimeSeconds = Mathf.Max(0f, accentLifetimeSeconds);
            AccentSizeMeters = Mathf.Max(0f, accentSizeMeters);
        }

        public int DustParticleCount { get; }

        public float DustSpeedMetersPerSecond { get; }

        public float DustLifetimeSeconds { get; }

        public float DustSizeMeters { get; }

        public int AccentParticleCount { get; }

        public float AccentSpeedMetersPerSecond { get; }

        public float AccentLifetimeSeconds { get; }

        public float AccentSizeMeters { get; }

        public bool ShouldPlay => DustParticleCount > 0 || AccentParticleCount > 0;
    }
}
