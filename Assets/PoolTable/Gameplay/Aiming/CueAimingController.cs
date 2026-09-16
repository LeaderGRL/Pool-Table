using System;
using PoolTable.Core.Shots;
using PoolTable.Input;
using UnityEngine;

namespace PoolTable.Gameplay.Aiming
{
    public enum PointerAimPhase
    {
        Elevation,
        Yaw,
    }

    [DisallowMultipleComponent]
    public sealed class CueAimingController : MonoBehaviour
    {
        [SerializeField] private GameObject cueBall;
        [SerializeField] private float cueDistance = 1.6666667f;
        [SerializeField] private float yawDegreesPerPointerUnit = 0.1f;
        [SerializeField] private float pitchDegreesPerPointerUnit = 0.1f;
        [SerializeField] private float minimumElevationDegrees = 0f;
        [SerializeField] private float maximumElevationDegrees = 20f;
        [SerializeField] private float controllerPitchDegreesPerSecond = 30f;

        private AimingState aimingState;
        private LocalPlayerInputReader localPlayerInputReader;
        private bool secondaryButtonWasPressedLastFrame;
        private PointerAimPhase pointerAimPhase = PointerAimPhase.Elevation;

        public GameObject CueBall => cueBall;

        public float CueDistance => cueDistance;

        public float YawDegreesPerPointerUnit => yawDegreesPerPointerUnit;

        public float PitchDegreesPerPointerUnit => pitchDegreesPerPointerUnit;

        public float MinimumElevationDegrees => minimumElevationDegrees;

        public float MaximumElevationDegrees => maximumElevationDegrees;

        public PointerAimPhase PointerAdjustmentPhase => pointerAimPhase;

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
            secondaryButtonWasPressedLastFrame = false;
            ResetPointerAdjustmentSequence();

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
            ProcessRuntimeInput(localPlayerInputReader.Read());
        }

        public void BeginPointerYawAdjustment()
        {
            pointerAimPhase = PointerAimPhase.Yaw;
        }

        public void ResetPointerAdjustmentSequence()
        {
            pointerAimPhase = PointerAimPhase.Elevation;
        }

        internal void ProcessRuntimeInput(LocalPlayerInputSnapshot input)
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var suppressAim = ShouldSuppressAim(input);
            if (!suppressAim)
            {
                var yawDegrees = input.AimAxis.x * Time.deltaTime * 120f;
                var pitchDegrees = input.AimAxis.y * Time.deltaTime * controllerPitchDegreesPerSecond;

                if (pointerAimPhase == PointerAimPhase.Elevation)
                {
                    pitchDegrees += input.PointerDelta.y * pitchDegreesPerPointerUnit;
                }
                else
                {
                    yawDegrees += input.PointerDelta.x * yawDegreesPerPointerUnit;
                }

                aimingState.RotateDegrees(yawDegrees);
                aimingState.AdjustElevationDegrees(pitchDegrees);
            }

            ApplyCuePose();
        }

        internal void ProcessInput(LocalPlayerInputSnapshot input)
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var suppressAim = ShouldSuppressAim(input);
            if (!suppressAim)
            {
                aimingState.RotateDegrees((input.PointerDelta.x * yawDegreesPerPointerUnit) + (input.AimAxis.x * Time.deltaTime * 120f));
                aimingState.AdjustElevationDegrees(
                    (input.PointerDelta.y * pitchDegreesPerPointerUnit)
                    + (input.AimAxis.y * Time.deltaTime * controllerPitchDegreesPerSecond));
            }

            ApplyCuePose();
        }

        private bool ShouldSuppressAim(LocalPlayerInputSnapshot input)
        {
            var suppressAim = input.SecondaryActionIsPressed || secondaryButtonWasPressedLastFrame;
            secondaryButtonWasPressedLastFrame = input.SecondaryActionIsPressed;
            return suppressAim;
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
                minimumElevationDegrees,
                maximumElevationDegrees);
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
