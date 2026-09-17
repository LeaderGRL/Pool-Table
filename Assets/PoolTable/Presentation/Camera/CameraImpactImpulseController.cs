using PoolTable.Gameplay.Feedback;
using PoolTable.Gameplay.Shots;
using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1050)]
    public sealed class CameraImpactImpulseController : MonoBehaviour
    {
        private ShotCameraController shotCameraController;
        private SpectateCameraController spectateCameraController;
        private CameraImpactImpulseCue activeCue;
        private float remainingSeconds;

        public bool IsPlaying => remainingSeconds > 0f && activeCue.ShouldPlay;

        public float RemainingSeconds => remainingSeconds;

        private void Awake()
        {
            shotCameraController = GetComponent<ShotCameraController>();
            spectateCameraController = GetComponent<SpectateCameraController>();
        }

        private void LateUpdate()
        {
            ApplyCurrentImpulse(Time.deltaTime);
        }

        internal void PlayCueStrike(CueStrikeObservation observation)
        {
            Trigger(CameraImpactImpulseModel.EvaluateCueStrike(observation.NormalizedPower));
        }

        internal void PlayRailImpact(RailImpactObservation observation)
        {
            Trigger(CameraImpactImpulseModel.EvaluateRailImpact(observation.ImpactEnergyJoules));
        }

        internal bool ApplyCurrentImpulse(float deltaTime)
        {
            if (!IsPlaying)
            {
                return false;
            }

            if (!CanApplyFeedback())
            {
                Clear();
                return false;
            }

            var duration = Mathf.Max(0.0001f, activeCue.DurationSeconds);
            var elapsed = duration - remainingSeconds;
            var normalizedRemaining = Mathf.Clamp01(remainingSeconds / duration);
            var envelope = normalizedRemaining * normalizedRemaining;
            var phase = elapsed * activeCue.FrequencyHz * Mathf.PI * 2f;

            var localPositionOffset = new Vector3(
                Mathf.Sin(phase * 1.17f),
                Mathf.Cos(phase * 1.61f) * 0.55f,
                0f) * (activeCue.PositionAmplitudeMeters * envelope);
            var localRotationOffset = new Vector3(
                Mathf.Cos(phase * 1.37f) * 0.45f,
                Mathf.Sin(phase * 0.83f) * 0.3f,
                Mathf.Sin(phase * 1.73f)) * (activeCue.RotationAmplitudeDegrees * envelope);

            transform.position += transform.TransformVector(localPositionOffset);
            transform.rotation *= Quaternion.Euler(localRotationOffset);

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Mathf.Max(0f, deltaTime));
            if (remainingSeconds <= 0f)
            {
                activeCue = CameraImpactImpulseCue.Silent;
            }

            return true;
        }

        private bool CanApplyFeedback()
        {
            return (shotCameraController != null && shotCameraController.isActiveAndEnabled)
                || (spectateCameraController != null && spectateCameraController.isActiveAndEnabled);
        }

        private void Trigger(CameraImpactImpulseCue cue)
        {
            if (!cue.ShouldPlay)
            {
                return;
            }

            if (!IsPlaying
                || cue.PositionAmplitudeMeters >= activeCue.PositionAmplitudeMeters
                || cue.RotationAmplitudeDegrees >= activeCue.RotationAmplitudeDegrees)
            {
                activeCue = cue;
            }

            remainingSeconds = Mathf.Max(remainingSeconds, cue.DurationSeconds);
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Clear()
        {
            activeCue = CameraImpactImpulseCue.Silent;
            remainingSeconds = 0f;
        }
    }
}
