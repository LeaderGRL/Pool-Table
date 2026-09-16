using System;
using PoolTable.Core.Shots;
using PoolTable.Input;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Gameplay.Aiming
{
    public enum CueAimStage
    {
        Yaw = 0,
        Elevation = 1,
        Locked = 2,
    }

    [DisallowMultipleComponent]
    public sealed class CueAimingController : MonoBehaviour
    {
        private const float RuntimeMinimumElevationDegrees = 3f;
        private const float CueShaftRadiusMeters = 0.01f;
        private const float RailClearanceMeters = 0.003f;

        [SerializeField] private GameObject cueBall;
        [SerializeField] private float cueDistance = 1.6666667f;
        [SerializeField] private float yawDegreesPerPointerUnit = 0.1f;
        [SerializeField] private float pitchDegreesPerPointerUnit = 0.1f;
        [SerializeField] private float minimumElevationDegrees = 0f;
        [SerializeField] private float maximumElevationDegrees = 20f;
        [SerializeField] private float controllerPitchDegreesPerSecond = 30f;

        private AimingState aimingState;
        private LocalPlayerInputReader localPlayerInputReader;
        private bool primaryButtonWasPressedLastFrame;
        private bool secondaryButtonWasPressedLastFrame;
        private int stagedPointerSuppressionFrame = -1;

        public GameObject CueBall => cueBall;

        public float CueDistance => cueDistance;

        public float YawDegreesPerPointerUnit => yawDegreesPerPointerUnit;

        public float PitchDegreesPerPointerUnit => pitchDegreesPerPointerUnit;

        public float MinimumElevationDegrees => Mathf.Max(minimumElevationDegrees, RuntimeMinimumElevationDegrees);

        public float MaximumElevationDegrees => maximumElevationDegrees;

        public float EffectiveMinimumElevationDegrees => CalculateEffectiveMinimumElevationDegrees();

        public CueAimStage AimStage { get; private set; } = CueAimStage.Yaw;

        public float ElevationDegrees => aimingState?.ElevationDegrees
            ?? throw new InvalidOperationException("Aiming state is not initialized.");

        public ShotDirection Direction => aimingState?.Direction
            ?? throw new InvalidOperationException("Aiming state is not initialized.");

        public Vector3 StrikeDirection
        {
            get
            {
                var elevationRadians = ElevationDegrees * Mathf.Deg2Rad;
                var planarScale = Mathf.Cos(elevationRadians);
                return new Vector3(
                    Direction.X * planarScale,
                    -Mathf.Sin(elevationRadians),
                    Direction.Y * planarScale);
            }
        }

        private void Awake()
        {
            localPlayerInputReader = new LocalPlayerInputReader();
            InitializeFromCurrentPose();
        }

        private void OnEnable()
        {
            primaryButtonWasPressedLastFrame = false;
            secondaryButtonWasPressedLastFrame = false;
            stagedPointerSuppressionFrame = -1;
            AimStage = CueAimStage.Yaw;

            if (localPlayerInputReader == null)
            {
                localPlayerInputReader = new LocalPlayerInputReader();
            }

            if (aimingState == null)
            {
                InitializeFromCurrentPose();
            }

            ApplyCuePose();
        }

        private void Update()
        {
            ProcessInputInternal(localPlayerInputReader.Read(), true);
        }

        public void AdvanceAimStage()
        {
            stagedPointerSuppressionFrame = Time.frameCount;

            switch (AimStage)
            {
                case CueAimStage.Yaw:
                    AimStage = CueAimStage.Elevation;
                    break;
                case CueAimStage.Elevation:
                    AimStage = CueAimStage.Locked;
                    break;
            }
        }

        internal void ProcessInput(LocalPlayerInputSnapshot input)
        {
            ProcessInputInternal(input, false);
        }

        internal void ProcessStagedInput(LocalPlayerInputSnapshot input)
        {
            ProcessInputInternal(input, true);
        }

        private void ProcessInputInternal(LocalPlayerInputSnapshot input, bool stagePointerInput)
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var primaryButtonWasPressedThisFrame = input.PrimaryActionIsPressed && !primaryButtonWasPressedLastFrame;
            primaryButtonWasPressedLastFrame = input.PrimaryActionIsPressed;
            var suppressAim = input.SecondaryActionIsPressed || secondaryButtonWasPressedLastFrame;
            secondaryButtonWasPressedLastFrame = input.SecondaryActionIsPressed;

            if (!suppressAim)
            {
                aimingState.RotateDegrees(input.AimAxis.x * Time.deltaTime * 120f);
                aimingState.AdjustElevationDegrees(input.AimAxis.y * Time.deltaTime * controllerPitchDegreesPerSecond);

                if (stagePointerInput)
                {
                    if (primaryButtonWasPressedThisFrame || stagedPointerSuppressionFrame == Time.frameCount)
                    {
                        EnforceTableClearance();
                        ApplyCuePose();
                        return;
                    }

                    switch (AimStage)
                    {
                        case CueAimStage.Yaw:
                            aimingState.RotateDegrees(input.PointerDelta.x * yawDegreesPerPointerUnit);
                            break;
                        case CueAimStage.Elevation:
                            aimingState.AdjustElevationDegrees(input.PointerDelta.y * pitchDegreesPerPointerUnit);
                            break;
                    }
                }
                else
                {
                    aimingState.RotateDegrees(input.PointerDelta.x * yawDegreesPerPointerUnit);
                    aimingState.AdjustElevationDegrees(input.PointerDelta.y * pitchDegreesPerPointerUnit);
                }
            }

            EnforceTableClearance();
            ApplyCuePose();
        }

        private void InitializeFromCurrentPose()
        {
            if (cueBall == null)
            {
                aimingState = null;
                return;
            }

            var towardBall = cueBall.transform.position - transform.position;
            var forward = towardBall.sqrMagnitude > 0.000001f ? towardBall.normalized : transform.forward.normalized;
            var planarDirection = new Vector2(forward.x, forward.z);
            if (planarDirection.sqrMagnitude <= 0.000001f)
            {
                planarDirection = Vector2.up;
            }

            var initialElevationDegrees = Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            aimingState = new AimingState(
                new ShotDirection(planarDirection.x, planarDirection.y),
                initialElevationDegrees,
                MinimumElevationDegrees,
                maximumElevationDegrees);

            EnforceTableClearance();
        }

        private void EnforceTableClearance()
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var minimumElevation = EffectiveMinimumElevationDegrees;
            if (aimingState.ElevationDegrees < minimumElevation)
            {
                aimingState.AdjustElevationDegrees(minimumElevation - aimingState.ElevationDegrees);
            }
        }

        private float CalculateEffectiveMinimumElevationDegrees()
        {
            if (aimingState == null || cueBall == null)
            {
                return MinimumElevationDegrees;
            }

            var planarBackward = new Vector2(-Direction.X, -Direction.Y);
            if (planarBackward.sqrMagnitude <= 0.000001f)
            {
                return MinimumElevationDegrees;
            }

            planarBackward.Normalize();
            var cueBallPosition = cueBall.transform.position;
            var distanceToRail = DistanceToPlayingSurfaceBoundary(
                new Vector2(cueBallPosition.x, cueBallPosition.z),
                planarBackward);

            if (!float.IsFinite(distanceToRail) || distanceToRail <= 0.0001f)
            {
                return maximumElevationDegrees;
            }

            var requiredCueAxisHeight = BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters
                + BilliardsPhysicalSpecification.CushionNoseHeightMeters
                + CueShaftRadiusMeters
                + RailClearanceMeters;
            var requiredRise = Mathf.Max(0f, requiredCueAxisHeight - cueBallPosition.y);
            if (requiredRise <= 0f)
            {
                return MinimumElevationDegrees;
            }

            var clearanceElevation = Mathf.Atan2(requiredRise, distanceToRail) * Mathf.Rad2Deg;
            return Mathf.Clamp(
                Mathf.Max(MinimumElevationDegrees, clearanceElevation),
                MinimumElevationDegrees,
                maximumElevationDegrees);
        }

        private static float DistanceToPlayingSurfaceBoundary(Vector2 position, Vector2 direction)
        {
            var halfLength = BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f;
            var halfWidth = BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f;
            var distance = float.PositiveInfinity;

            if (Mathf.Abs(direction.x) > 0.000001f)
            {
                var boundaryX = direction.x > 0f ? halfLength : -halfLength;
                var candidate = (boundaryX - position.x) / direction.x;
                if (candidate >= 0f)
                {
                    distance = Mathf.Min(distance, candidate);
                }
            }

            if (Mathf.Abs(direction.y) > 0.000001f)
            {
                var boundaryY = direction.y > 0f ? halfWidth : -halfWidth;
                var candidate = (boundaryY - position.y) / direction.y;
                if (candidate >= 0f)
                {
                    distance = Mathf.Min(distance, candidate);
                }
            }

            return distance;
        }

        private void ApplyCuePose()
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var strikeDirection = StrikeDirection;
            transform.position = cueBall.transform.position - (strikeDirection * cueDistance);

            var directionToCueBall = cueBall.transform.position - transform.position;
            if (directionToCueBall.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToCueBall, Vector3.up);
            }
        }
    }
}
