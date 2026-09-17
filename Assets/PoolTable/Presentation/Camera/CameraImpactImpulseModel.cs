using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    public static class CameraImpactImpulseModel
    {
        public const float MinimumCueStrikePower = 0.08f;
        public const float MinimumRailImpactEnergyJoules = 0.012f;
        public const float FullScaleRailImpactEnergyJoules = 0.12f;

        public static CameraImpactImpulseCue EvaluateCueStrike(float normalizedPower)
        {
            normalizedPower = Mathf.Clamp01(normalizedPower);
            if (normalizedPower < MinimumCueStrikePower)
            {
                return CameraImpactImpulseCue.Silent;
            }

            var intensity = Mathf.InverseLerp(MinimumCueStrikePower, 1f, normalizedPower);
            intensity = Mathf.SmoothStep(0f, 1f, intensity);
            return new CameraImpactImpulseCue(
                Mathf.Lerp(0.0015f, 0.012f, intensity),
                Mathf.Lerp(0.05f, 0.42f, intensity),
                Mathf.Lerp(0.075f, 0.14f, intensity),
                Mathf.Lerp(19f, 28f, intensity));
        }

        public static CameraImpactImpulseCue EvaluateRailImpact(float impactEnergyJoules)
        {
            impactEnergyJoules = Mathf.Max(0f, impactEnergyJoules);
            if (impactEnergyJoules < MinimumRailImpactEnergyJoules)
            {
                return CameraImpactImpulseCue.Silent;
            }

            var intensity = Mathf.InverseLerp(
                MinimumRailImpactEnergyJoules,
                FullScaleRailImpactEnergyJoules,
                impactEnergyJoules);
            intensity = Mathf.SmoothStep(0f, 1f, intensity);
            return new CameraImpactImpulseCue(
                Mathf.Lerp(0.001f, 0.008f, intensity),
                Mathf.Lerp(0.04f, 0.3f, intensity),
                Mathf.Lerp(0.065f, 0.115f, intensity),
                Mathf.Lerp(22f, 32f, intensity));
        }
    }

    public readonly struct CameraImpactImpulseCue
    {
        public CameraImpactImpulseCue(
            float positionAmplitudeMeters,
            float rotationAmplitudeDegrees,
            float durationSeconds,
            float frequencyHz)
        {
            PositionAmplitudeMeters = Mathf.Max(0f, positionAmplitudeMeters);
            RotationAmplitudeDegrees = Mathf.Max(0f, rotationAmplitudeDegrees);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            FrequencyHz = Mathf.Max(0f, frequencyHz);
        }

        public float PositionAmplitudeMeters { get; }

        public float RotationAmplitudeDegrees { get; }

        public float DurationSeconds { get; }

        public float FrequencyHz { get; }

        public bool ShouldPlay => DurationSeconds > 0f
            && (PositionAmplitudeMeters > 0f || RotationAmplitudeDegrees > 0f);

        public static CameraImpactImpulseCue Silent => default;
    }
}
