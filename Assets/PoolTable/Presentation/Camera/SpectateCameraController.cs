using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1002)]
    public sealed class SpectateCameraController : MonoBehaviour
    {
        [SerializeField] private Transform ballsRoot;
        [SerializeField] private Rigidbody cueBall;
        [SerializeField, Min(0f)] private float minimumMovingSpeedMetersPerSecond = 0.01f;
        [SerializeField, Min(0.01f)] private float distanceFromAction = 3.2f;
        [SerializeField, Min(0f)] private float heightAboveAction = 2.2f;
        [SerializeField, Min(0f)] private float distancePerMeterSpread = 0.75f;
        [SerializeField, Min(0f)] private float heightPerMeterSpread = 0.5f;
        [SerializeField, Min(0f)] private float motionLeadSeconds = 0.14f;
        [SerializeField, Min(0f)] private float maximumMotionLeadDistance = 0.32f;
        [SerializeField, Min(0f)] private float distancePerMeterPerSecond = 0.1f;
        [SerializeField, Min(0f)] private float maximumSpeedFramingDistance = 0.35f;
        [SerializeField, Min(0f)] private float targetHeightOffset = 0.05f;
        [SerializeField] private Vector2 tableViewDirection = new Vector2(-1f, -1f);
        [SerializeField, Min(0f)] private float positionSharpness = 5f;
        [SerializeField, Min(0f)] private float rotationSharpness = 7f;

        private Rigidbody[] observedBalls;
        private CameraImpactImpulseController impactImpulseController;

        public Transform BallsRoot => ballsRoot;

        public Rigidbody CueBall => cueBall;

        public float MinimumMovingSpeedMetersPerSecond => minimumMovingSpeedMetersPerSecond;

        public float MotionLeadSeconds => motionLeadSeconds;

        public float MaximumMotionLeadDistance => maximumMotionLeadDistance;

        public float DistancePerMeterPerSecond => distancePerMeterPerSecond;

        public float MaximumSpeedFramingDistance => maximumSpeedFramingDistance;

        private void OnEnable()
        {
            impactImpulseController = GetComponent<CameraImpactImpulseController>();
            impactImpulseController?.RemoveLastAppliedOffset();
            RefreshObservedBalls();
            ApplyCameraPose(0f, true);
        }

        private void LateUpdate()
        {
            impactImpulseController ??= GetComponent<CameraImpactImpulseController>();
            impactImpulseController?.RemoveLastAppliedOffset();
            if (ApplyCameraPose(Time.deltaTime, false))
            {
                impactImpulseController?.ApplyCurrentImpulse(Time.deltaTime);
            }
        }

        internal void RefreshObservedBalls()
        {
            observedBalls = ballsRoot == null
                ? System.Array.Empty<Rigidbody>()
                : ballsRoot.GetComponentsInChildren<Rigidbody>(true);
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

            if (!TryGetActionFrame(
                    out var actionCenter,
                    out var spreadRadius,
                    out var averageLinearVelocity,
                    out var averagePlanarSpeedMetersPerSecond))
            {
                return false;
            }

            var framingAdjustment = SpectateShotFramingModel.Evaluate(
                averageLinearVelocity,
                averagePlanarSpeedMetersPerSecond,
                motionLeadSeconds,
                maximumMotionLeadDistance,
                distancePerMeterPerSecond,
                maximumSpeedFramingDistance);
            var framedCenter = actionCenter + framingAdjustment.MotionLead;

            var planarViewDirection = new Vector3(tableViewDirection.x, 0f, tableViewDirection.y);
            if (planarViewDirection.sqrMagnitude <= 0.000001f)
            {
                planarViewDirection = new Vector3(-1f, 0f, -1f);
            }

            planarViewDirection.Normalize();
            var cameraDistance = distanceFromAction
                + (spreadRadius * distancePerMeterSpread)
                + framingAdjustment.AdditionalDistance;
            var cameraHeight = heightAboveAction + (spreadRadius * heightPerMeterSpread);
            desiredPosition = framedCenter
                - (planarViewDirection * cameraDistance)
                + (Vector3.up * cameraHeight);

            var focusPoint = framedCenter + (Vector3.up * targetHeightOffset);
            var forward = focusPoint - desiredPosition;
            if (forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            desiredRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            return true;
        }

        internal bool TryGetActionFrame(out Vector3 actionCenter, out float spreadRadius)
        {
            return TryGetActionFrame(out actionCenter, out spreadRadius, out _, out _);
        }

        internal bool TryGetActionFrame(
            out Vector3 actionCenter,
            out float spreadRadius,
            out Vector3 averageLinearVelocity)
        {
            return TryGetActionFrame(
                out actionCenter,
                out spreadRadius,
                out averageLinearVelocity,
                out _);
        }

        internal bool TryGetActionFrame(
            out Vector3 actionCenter,
            out float spreadRadius,
            out Vector3 averageLinearVelocity,
            out float averagePlanarSpeedMetersPerSecond)
        {
            actionCenter = default;
            spreadRadius = 0f;
            averageLinearVelocity = default;
            averagePlanarSpeedMetersPerSecond = 0f;

            if (observedBalls == null)
            {
                RefreshObservedBalls();
            }

            var movingSpeedSquared = minimumMovingSpeedMetersPerSecond * minimumMovingSpeedMetersPerSecond;
            var movingCount = 0;

            foreach (var body in observedBalls)
            {
                if (body == null
                    || !body.gameObject.activeInHierarchy
                    || body.linearVelocity.sqrMagnitude < movingSpeedSquared)
                {
                    continue;
                }

                actionCenter += body.position;
                var planarLinearVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
                averageLinearVelocity += planarLinearVelocity;
                averagePlanarSpeedMetersPerSecond += planarLinearVelocity.magnitude;
                movingCount++;
            }

            if (movingCount == 0)
            {
                if (cueBall == null || !cueBall.gameObject.activeInHierarchy)
                {
                    return false;
                }

                actionCenter = cueBall.position;
                averageLinearVelocity = Vector3.ProjectOnPlane(cueBall.linearVelocity, Vector3.up);
                averagePlanarSpeedMetersPerSecond = averageLinearVelocity.magnitude;
                return true;
            }

            actionCenter /= movingCount;
            averageLinearVelocity = Vector3.ProjectOnPlane(averageLinearVelocity / movingCount, Vector3.up);
            averagePlanarSpeedMetersPerSecond /= movingCount;
            foreach (var body in observedBalls)
            {
                if (body == null
                    || !body.gameObject.activeInHierarchy
                    || body.linearVelocity.sqrMagnitude < movingSpeedSquared)
                {
                    continue;
                }

                var planarOffset = Vector3.ProjectOnPlane(body.position - actionCenter, Vector3.up);
                spreadRadius = Mathf.Max(spreadRadius, planarOffset.magnitude);
            }

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
