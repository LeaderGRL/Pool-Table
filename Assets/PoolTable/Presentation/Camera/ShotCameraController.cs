using PoolTable.Gameplay.Shots;
using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DefaultExecutionOrder(1001)]
    public sealed class ShotCameraController : MonoBehaviour
    {
        [SerializeField] private ShotPowerController shotPowerController;
        [SerializeField, Min(0.01f)] private float distanceBehindCueBall = 2.6f;
        [SerializeField, Min(0f)] private float heightAboveCueBall = 1.1f;
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.4f;
        [SerializeField, Min(0f)] private float targetHeightOffset = 0.08f;
        [SerializeField, Min(0f)] private float additionalDistanceAtFullPower = 0.35f;
        [SerializeField, Min(0f)] private float positionSharpness = 14f;
        [SerializeField, Min(0f)] private float rotationSharpness = 18f;
        [Header("Player stance")]
        [SerializeField, Min(0.01f)] private float playerViewDistanceBehindCueBall = 1.35f;
        [SerializeField, Min(0f)] private float playerViewHeightAboveCueBall = 0.62f;
        [SerializeField, Min(0f)] private float playerViewLookAheadDistance = 1.35f;
        [SerializeField, Min(0f)] private float playerViewAdditionalDistanceAtFullPower = 0.1f;
        [SerializeField, Range(20f, 100f)] private float playerViewFieldOfView = 50f;

        private UnityEngine.Camera outputCamera;
        private float defaultFieldOfView;

        public ShotPowerController ShotPowerController => shotPowerController;

        public float DistanceBehindCueBall => distanceBehindCueBall;

        public float HeightAboveCueBall => heightAboveCueBall;

        public float LookAheadDistance => lookAheadDistance;

        public float TargetHeightOffset => targetHeightOffset;

        public float AdditionalDistanceAtFullPower => additionalDistanceAtFullPower;

        public float PlayerViewDistanceBehindCueBall => playerViewDistanceBehindCueBall;

        public float PlayerViewHeightAboveCueBall => playerViewHeightAboveCueBall;

        public float PlayerViewAdditionalDistanceAtFullPower => playerViewAdditionalDistanceAtFullPower;

        public float PlayerViewFieldOfView => playerViewFieldOfView;

        private void Awake()
        {
            outputCamera = GetComponent<UnityEngine.Camera>();
            defaultFieldOfView = outputCamera.fieldOfView;
        }

        private void OnEnable()
        {
            ApplyPresentationCameraPose(0f, true);
        }

        private void OnDisable()
        {
            if (outputCamera != null && defaultFieldOfView > 0f)
            {
                outputCamera.fieldOfView = defaultFieldOfView;
            }
        }

        private void LateUpdate()
        {
            ApplyPresentationCameraPose(Time.deltaTime, false);
        }

        internal bool ApplyPresentationCameraPose(float deltaTime, bool snap)
        {
            if (!TryGetPresentationDesiredPose(out var desiredPosition, out var desiredRotation, out var desiredFieldOfView))
            {
                return false;
            }

            var positionWeight = snap ? 1f : CalculateDampingWeight(positionSharpness, deltaTime);
            var rotationWeight = snap ? 1f : CalculateDampingWeight(rotationSharpness, deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionWeight);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationWeight);
            if (outputCamera != null)
            {
                outputCamera.fieldOfView = Mathf.Lerp(outputCamera.fieldOfView, desiredFieldOfView, positionWeight);
            }

            return true;
        }

        internal bool TryGetPresentationDesiredPose(
            out Vector3 desiredPosition,
            out Quaternion desiredRotation,
            out float desiredFieldOfView)
        {
            desiredPosition = default;
            desiredRotation = default;
            desiredFieldOfView = playerViewFieldOfView;

            if (shotPowerController == null
                || !shotPowerController.isActiveAndEnabled
                || shotPowerController.CueBall == null
                || shotPowerController.AimingController == null)
            {
                return false;
            }

            var direction = shotPowerController.AimingController.Direction;
            var planarDirection = new Vector3(direction.X, 0f, direction.Y).normalized;
            if (planarDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            var cueBallPosition = shotPowerController.CueBall.position;
            var powerDistance = shotPowerController.NormalizedPower * playerViewAdditionalDistanceAtFullPower;
            desiredPosition = cueBallPosition
                - (planarDirection * (playerViewDistanceBehindCueBall + powerDistance))
                + (Vector3.up * playerViewHeightAboveCueBall);

            var focusPoint = cueBallPosition
                + (planarDirection * playerViewLookAheadDistance)
                + (Vector3.up * targetHeightOffset);
            var forward = focusPoint - desiredPosition;
            if (forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            desiredRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            return true;
        }

        internal bool ApplyCameraPose(float deltaTime, bool snap)
        {
            if (!TryGetDesiredPose(out var desiredPosition, out var desiredRotation))
            {
                return false;
            }

            var positionWeight = snap ? 1f : CalculateDampingWeight(positionSharpness, deltaTime);
            var rotationWeight = snap ? 1f : CalculateDampingWeight(rotationSharpness, deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionWeight);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationWeight);
            return true;
        }

        internal bool TryGetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation)
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

            var direction = shotPowerController.AimingController.Direction;
            var planarDirection = new Vector3(direction.X, 0f, direction.Y).normalized;
            if (planarDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            var cueBallPosition = shotPowerController.CueBall.position;
            var powerDistance = shotPowerController.NormalizedPower * additionalDistanceAtFullPower;
            desiredPosition = cueBallPosition
                - (planarDirection * (distanceBehindCueBall + powerDistance))
                + (Vector3.up * heightAboveCueBall);

            var focusPoint = cueBallPosition
                + (planarDirection * lookAheadDistance)
                + (Vector3.up * targetHeightOffset);
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
