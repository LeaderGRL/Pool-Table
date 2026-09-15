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
        [SerializeField, Min(0f)] private float targetHeightOffset = 0.05f;
        [SerializeField] private Vector2 tableViewDirection = new Vector2(-1f, -1f);
        [SerializeField, Min(0f)] private float positionSharpness = 5f;
        [SerializeField, Min(0f)] private float rotationSharpness = 7f;

        private Rigidbody[] observedBalls;

        public Transform BallsRoot => ballsRoot;

        public Rigidbody CueBall => cueBall;

        public float MinimumMovingSpeedMetersPerSecond => minimumMovingSpeedMetersPerSecond;

        private void OnEnable()
        {
            RefreshObservedBalls();
            ApplyCameraPose(0f, true);
        }

        private void LateUpdate()
        {
            ApplyCameraPose(Time.deltaTime, false);
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

            if (!TryGetActionFrame(out var actionCenter, out var spreadRadius))
            {
                return false;
            }

            var planarViewDirection = new Vector3(tableViewDirection.x, 0f, tableViewDirection.y);
            if (planarViewDirection.sqrMagnitude <= 0.000001f)
            {
                planarViewDirection = new Vector3(-1f, 0f, -1f);
            }

            planarViewDirection.Normalize();
            var cameraDistance = distanceFromAction + (spreadRadius * distancePerMeterSpread);
            var cameraHeight = heightAboveAction + (spreadRadius * heightPerMeterSpread);
            desiredPosition = actionCenter
                - (planarViewDirection * cameraDistance)
                + (Vector3.up * cameraHeight);

            var focusPoint = actionCenter + (Vector3.up * targetHeightOffset);
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
            actionCenter = default;
            spreadRadius = 0f;

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
                movingCount++;
            }

            if (movingCount == 0)
            {
                if (cueBall == null || !cueBall.gameObject.activeInHierarchy)
                {
                    return false;
                }

                actionCenter = cueBall.position;
                return true;
            }

            actionCenter /= movingCount;
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
