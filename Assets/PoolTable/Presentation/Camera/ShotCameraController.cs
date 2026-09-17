using PoolTable.Gameplay.Shots;
using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1001)]
    public sealed class ShotCameraController : MonoBehaviour
    {
        [SerializeField] private ShotPowerController shotPowerController;
        [SerializeField, Min(0.01f)] private float distanceBehindCueBall = 2.6f;
        [SerializeField, Min(0f)] private float forwardEyeOffsetMeters = 0.6f;
        [SerializeField, Min(0.05f)] private float preferredRuntimeDistanceBehindCueBall = 1.1f;
        [SerializeField, Min(0f)] private float heightAboveCueBall = 0.55f;
        [SerializeField, Min(0f)] private float preferredRuntimeHeightAboveCueBall = 0.2f;
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.4f;
        [SerializeField, Min(0f)] private float preferredRuntimeLookAheadDistance = 0.15f;
        [SerializeField, Min(0f)] private float targetHeightOffset = 0.08f;
        [SerializeField, Min(0f)] private float preferredRuntimeTargetHeightOffset = 0f;
        [SerializeField, Min(0f)] private float additionalDistanceAtFullPower = 0.35f;
        [SerializeField, Min(0f)] private float positionSharpness = 14f;
        [SerializeField, Min(0f)] private float rotationSharpness = 18f;

        private CameraImpactImpulseController impactImpulseController;

        public ShotPowerController ShotPowerController => shotPowerController;

        public float DistanceBehindCueBall => distanceBehindCueBall;

        public float ForwardEyeOffsetMeters => forwardEyeOffsetMeters;

        public float EffectiveDistanceBehindCueBall => Mathf.Min(
            preferredRuntimeDistanceBehindCueBall,
            Mathf.Max(0.05f, distanceBehindCueBall - forwardEyeOffsetMeters));

        public float PreferredRuntimeDistanceBehindCueBall => preferredRuntimeDistanceBehindCueBall;

        public float HeightAboveCueBall => heightAboveCueBall;

        public float EffectiveHeightAboveCueBall => Mathf.Min(heightAboveCueBall, preferredRuntimeHeightAboveCueBall);

        public float LookAheadDistance => lookAheadDistance;

        public float EffectiveLookAheadDistance => Mathf.Min(lookAheadDistance, preferredRuntimeLookAheadDistance);

        public float TargetHeightOffset => targetHeightOffset;

        public float EffectiveTargetHeightOffset => Mathf.Min(targetHeightOffset, preferredRuntimeTargetHeightOffset);

        public float AdditionalDistanceAtFullPower => additionalDistanceAtFullPower;

        private void OnEnable()
        {
            impactImpulseController = GetComponent<CameraImpactImpulseController>();
            impactImpulseController?.RemoveLastAppliedOffset();
            ApplyRuntimeCameraPose(0f, true);
        }

        private void LateUpdate()
        {
            impactImpulseController ??= GetComponent<CameraImpactImpulseController>();
            impactImpulseController?.RemoveLastAppliedOffset();
            if (ApplyRuntimeCameraPose(Time.deltaTime, false))
            {
                impactImpulseController?.ApplyCurrentImpulse(Time.deltaTime);
            }
        }

        internal bool ApplyCameraPose(float deltaTime, bool snap)
        {
            var powerDistance = shotPowerController == null
                ? 0f
                : shotPowerController.NormalizedPower * additionalDistanceAtFullPower;
            return ApplyCameraPose(deltaTime, snap, distanceBehindCueBall + powerDistance);
        }

        internal bool TryGetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation)
        {
            var powerDistance = shotPowerController == null
                ? 0f
                : shotPowerController.NormalizedPower * additionalDistanceAtFullPower;
            return TryGetDesiredPose(
                distanceBehindCueBall + powerDistance,
                out desiredPosition,
                out desiredRotation);
        }

        internal bool ApplyRuntimeCameraPose(float deltaTime, bool snap)
        {
            var powerDistance = shotPowerController == null
                ? 0f
                : shotPowerController.NormalizedPower * additionalDistanceAtFullPower;
            return ApplyCameraPose(
                deltaTime,
                snap,
                EffectiveDistanceBehindCueBall + powerDistance);
        }

        private bool ApplyCameraPose(float deltaTime, bool snap, float cameraDistance)
        {
            if (!TryGetDesiredPose(cameraDistance, out var desiredPosition, out var desiredRotation))
            {
                return false;
            }

            var positionWeight = snap ? 1f : CalculateDampingWeight(positionSharpness, deltaTime);
            var rotationWeight = snap ? 1f : CalculateDampingWeight(rotationSharpness, deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionWeight);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationWeight);
            return true;
        }

        private bool TryGetDesiredPose(
            float cameraDistance,
            out Vector3 desiredPosition,
            out Quaternion desiredRotation)
        {
            desiredPosition = default;
            desiredRotation = default;

            if (shotPowerController == null
                || !shotPowerController.isActiveAndEnabled
                || shotPowerController.CueBall == null
                || shotPowerController.AimingController == null)
            {
                return false;
            }

            if (!CueCameraRig.TryGetBasis(
                    shotPowerController.AimingController.StrikeDirection,
                    out var cueForward,
                    out var cueUp))
            {
                return false;
            }

            var cueBallPosition = shotPowerController.CueBall.position;
            desiredPosition = CueCameraRig.GetCameraPosition(
                cueBallPosition,
                cueForward,
                cueUp,
                cameraDistance,
                EffectiveHeightAboveCueBall);

            var focusPoint = CueCameraRig.GetFocusPoint(
                cueBallPosition,
                cueForward,
                cueUp,
                EffectiveLookAheadDistance,
                EffectiveTargetHeightOffset);
            var forward = focusPoint - desiredPosition;
            if (forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            desiredRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            return true;
        }

        private static float CalculateDampingWeight(float sharpness, float deltaTime)
        {
            if (sharpness <= 0f || deltaTime <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }
    }
}
