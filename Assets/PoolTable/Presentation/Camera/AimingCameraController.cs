using PoolTable.Gameplay.Aiming;
using PoolTable.Gameplay.BallInHand;
using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DefaultExecutionOrder(1000)]
    public sealed class AimingCameraController : MonoBehaviour
    {
        [SerializeField] private CueAimingController aimingController;
        [SerializeField, Min(0.01f)] private float distanceBehindCueBall = 2.4f;
        [SerializeField, Min(0f)] private float heightAboveCueBall = 1.05f;
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.2f;
        [SerializeField, Min(0f)] private float targetHeightOffset = 0.08f;
        [SerializeField, Min(0f)] private float positionSharpness = 12f;
        [SerializeField, Min(0f)] private float rotationSharpness = 16f;
        [Header("Player stance")]
        [SerializeField, Min(0.01f)] private float playerViewDistanceBehindCueBall = 1.2f;
        [SerializeField, Min(0f)] private float playerViewHeightAboveCueBall = 0.55f;
        [SerializeField, Min(0f)] private float playerViewLookAheadDistance = 1.25f;
        [SerializeField, Range(20f, 100f)] private float playerViewFieldOfView = 50f;
        [Header("Ball in hand")]
        [SerializeField] private BallInHandPlacementController ballInHandPlacementController;
        [SerializeField, Range(15f, 80f)] private float placementViewElevationDegrees = 38f;
        [SerializeField, Range(20f, 100f)] private float placementViewFieldOfView = 55f;
        [SerializeField, Min(1f)] private float placementFramePadding = 1.18f;

        private UnityEngine.Camera outputCamera;
        private float defaultFieldOfView;
        private bool placementWasActive;

        public CueAimingController AimingController => aimingController;

        public float DistanceBehindCueBall => distanceBehindCueBall;

        public float HeightAboveCueBall => heightAboveCueBall;

        public float LookAheadDistance => lookAheadDistance;

        public float TargetHeightOffset => targetHeightOffset;

        public float PlayerViewDistanceBehindCueBall => playerViewDistanceBehindCueBall;

        public float PlayerViewHeightAboveCueBall => playerViewHeightAboveCueBall;

        public float PlayerViewLookAheadDistance => playerViewLookAheadDistance;

        public float PlayerViewFieldOfView => playerViewFieldOfView;

        public float PlacementViewElevationDegrees => placementViewElevationDegrees;

        public float PlacementViewFieldOfView => placementViewFieldOfView;

        public float PlacementFramePadding => placementFramePadding;

        private void Awake()
        {
            outputCamera = GetComponent<UnityEngine.Camera>();
            defaultFieldOfView = outputCamera.fieldOfView;

            if (ballInHandPlacementController == null)
            {
                var placementControllers = Object.FindObjectsByType<BallInHandPlacementController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                if (placementControllers.Length > 0)
                {
                    ballInHandPlacementController = placementControllers[0];
                }
            }
        }

        private void Start()
        {
            ApplyPresentationCameraPose(0f, true);
        }

        private void LateUpdate()
        {
            var placementIsActive = ballInHandPlacementController != null
                && ballInHandPlacementController.IsPlacing;
            var snap = placementIsActive && !placementWasActive;

            if (!ApplyPresentationCameraPose(Time.deltaTime, snap))
            {
                RestoreDefaultFieldOfView();
            }

            placementWasActive = placementIsActive;
        }

        private void OnDisable()
        {
            RestoreDefaultFieldOfView();
            placementWasActive = false;
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
            if (ballInHandPlacementController != null && ballInHandPlacementController.IsPlacing)
            {
                return TryGetBallInHandPose(out desiredPosition, out desiredRotation, out desiredFieldOfView);
            }

            desiredPosition = default;
            desiredRotation = default;
            desiredFieldOfView = playerViewFieldOfView;

            if (aimingController == null || !aimingController.isActiveAndEnabled || aimingController.CueBall == null)
            {
                return false;
            }

            var direction = aimingController.Direction;
            var planarDirection = new Vector3(direction.X, 0f, direction.Y).normalized;
            if (planarDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            var cueBallPosition = aimingController.CueBall.transform.position;
            desiredPosition = cueBallPosition
                - (planarDirection * playerViewDistanceBehindCueBall)
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

            if (aimingController == null || !aimingController.isActiveAndEnabled || aimingController.CueBall == null)
            {
                return false;
            }

            var direction = aimingController.Direction;
            var planarDirection = new Vector3(direction.X, 0f, direction.Y).normalized;
            var cueBallPosition = aimingController.CueBall.transform.position;
            desiredPosition = cueBallPosition
                - (planarDirection * distanceBehindCueBall)
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

        private bool TryGetBallInHandPose(
            out Vector3 desiredPosition,
            out Quaternion desiredRotation,
            out float desiredFieldOfView)
        {
            desiredPosition = default;
            desiredRotation = default;
            desiredFieldOfView = placementViewFieldOfView;

            if (outputCamera == null)
            {
                return false;
            }

            var centerX = (BallInHandPlacementGeometry.MinimumCenterX + BallInHandPlacementGeometry.MaximumCenterX) * 0.5f;
            var centerZ = (BallInHandPlacementGeometry.MinimumCenterZ + BallInHandPlacementGeometry.MaximumCenterZ) * 0.5f;
            var targetHeight = aimingController != null && aimingController.CueBall != null
                ? aimingController.CueBall.transform.position.y
                : 0f;
            var target = new Vector3(centerX, targetHeight, centerZ);

            var halfLength = (BallInHandPlacementGeometry.MaximumCenterX - BallInHandPlacementGeometry.MinimumCenterX)
                * 0.5f
                * placementFramePadding;
            var halfWidth = (BallInHandPlacementGeometry.MaximumCenterZ - BallInHandPlacementGeometry.MinimumCenterZ)
                * 0.5f
                * placementFramePadding;
            var verticalHalfFovRadians = placementViewFieldOfView * Mathf.Deg2Rad * 0.5f;
            var aspect = outputCamera.aspect > 0.01f ? outputCamera.aspect : (16f / 9f);
            var horizontalHalfFovRadians = Mathf.Atan(Mathf.Tan(verticalHalfFovRadians) * aspect);
            var elevationRadians = placementViewElevationDegrees * Mathf.Deg2Rad;
            var horizontalDepth = halfLength / Mathf.Max(0.01f, Mathf.Tan(horizontalHalfFovRadians));
            var verticalDepth = (halfWidth * Mathf.Sin(elevationRadians))
                / Mathf.Max(0.01f, Mathf.Tan(verticalHalfFovRadians));
            var nearEdgeDepthOffset = halfWidth * Mathf.Cos(elevationRadians);
            var distanceToCenter = nearEdgeDepthOffset + Mathf.Max(horizontalDepth, verticalDepth);

            var heightOffset = Mathf.Sin(elevationRadians) * distanceToCenter;
            var sideOffset = Mathf.Cos(elevationRadians) * distanceToCenter;
            desiredPosition = target + new Vector3(0f, heightOffset, -sideOffset);

            var forward = target - desiredPosition;
            if (forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            desiredRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            return true;
        }

        private void RestoreDefaultFieldOfView()
        {
            if (outputCamera != null && defaultFieldOfView > 0f)
            {
                outputCamera.fieldOfView = defaultFieldOfView;
            }
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
