using PoolTable.Gameplay.Aiming;
using PoolTable.Input;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Cue;
using UnityEngine;

namespace PoolTable.Gameplay.Shots
{
    [DisallowMultipleComponent]
    public sealed class ShotPowerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody cueBall;
        [SerializeField] private CueAimingController aimingController;
        [SerializeField] private float maximumCuePullbackMeters = 0.35f;
        [SerializeField] private float pointerDeltaSensitivity = 0.1f;
        [SerializeField] private float cueStrokeMetersPerPointerUnit = 0.02f;
        [SerializeField] private float maximumShotSpeedMetersPerSecond = 6.6666667f;

        private MouseInputReader mouseInputReader;
        private ShotPowerState shotPowerState;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private bool hasRestPose;
        private bool previousPrimaryButtonPressed;

        public Rigidbody CueBall => cueBall;

        public CueAimingController AimingController => aimingController;

        public float MaximumCuePullbackMeters => maximumCuePullbackMeters;

        public float PointerDeltaSensitivity => pointerDeltaSensitivity;

        public float CueStrokeMetersPerPointerUnit => cueStrokeMetersPerPointerUnit;

        public float MaximumShotSpeedMetersPerSecond => maximumShotSpeedMetersPerSecond;

        public float PullbackMeters => shotPowerState?.PullbackMeters ?? 0f;

        public float NormalizedPower => shotPowerState?.NormalizedPower ?? 0f;

        public bool ShotCommitted { get; private set; }

        private void Awake()
        {
            mouseInputReader = new MouseInputReader();
            shotPowerState = new ShotPowerState(maximumCuePullbackMeters);
        }

        private void OnEnable()
        {
            if (mouseInputReader == null)
            {
                mouseInputReader = new MouseInputReader();
            }

            shotPowerState = new ShotPowerState(maximumCuePullbackMeters);
            ShotCommitted = false;
            restPosition = transform.position;
            restRotation = transform.rotation;
            hasRestPose = true;
            previousPrimaryButtonPressed = true;
            ApplyCuePullbackPose();
        }

        private void OnDisable()
        {
            RestoreCuePose();
        }

        private void Update()
        {
            if (shotPowerState == null || cueBall == null || aimingController == null)
            {
                return;
            }

            ProcessInput(mouseInputReader.Read());
        }

        internal void ProcessInput(PointerInputSnapshot input)
        {
            if (shotPowerState == null || cueBall == null || aimingController == null || ShotCommitted)
            {
                return;
            }

            if (input.PrimaryButtonIsPressed)
            {
                var pointerDelta = Mathf.Clamp(input.Delta.y * pointerDeltaSensitivity, -1f, 1f);
                shotPowerState.AdjustPullback(pointerDelta * cueStrokeMetersPerPointerUnit);
                ApplyCuePullbackPose();
            }
            else if (previousPrimaryButtonPressed)
            {
                TryCommitShot();
            }

            previousPrimaryButtonPressed = input.PrimaryButtonIsPressed;
        }

        private bool TryCommitShot()
        {
            if (ShotCommitted || !shotPowerState.HasUsablePower)
            {
                return false;
            }

            var direction = aimingController.Direction;
            var shotSpeed = shotPowerState.NormalizedPower * maximumShotSpeedMetersPerSecond;
            var motion = CueBallStrikeModel.CalculateVelocityChange(
                new Vector3(direction.X, 0f, direction.Y),
                shotSpeed,
                default,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            RestoreCuePose();
            cueBall.AddForce(motion.LinearVelocityChange, ForceMode.VelocityChange);
            cueBall.angularVelocity += motion.AngularVelocityChange;
            ShotCommitted = true;
            enabled = false;
            return true;
        }

        private void ApplyCuePullbackPose()
        {
            if (!hasRestPose || shotPowerState == null)
            {
                return;
            }

            transform.position = restPosition
                - ((restRotation * Vector3.forward) * shotPowerState.PullbackMeters);
            transform.rotation = restRotation;
        }

        private void RestoreCuePose()
        {
            if (!hasRestPose)
            {
                return;
            }

            transform.SetPositionAndRotation(restPosition, restRotation);
        }
    }
}
