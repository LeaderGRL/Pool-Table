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
        [SerializeField] private CueBallSpinController spinController;
        [SerializeField] private float maximumCuePullbackMeters = 0.35f;
        [SerializeField] private float pointerDeltaSensitivity = 0.1f;
        [SerializeField] private float cueStrokeMetersPerPointerUnit = 0.02f;
        [SerializeField] private float controllerPullbackMetersPerSecond = 0.5f;
        [SerializeField] private float maximumShotSpeedMetersPerSecond = 6.6666667f;

        private LocalPlayerInputReader localPlayerInputReader;
        private ShotPowerState shotPowerState;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private bool hasRestPose;
        private bool previousPrimaryButtonPressed;
        private bool strikeQueued;
        private CueBallStrikeMotion queuedStrike;

        public Rigidbody CueBall => cueBall;

        public CueAimingController AimingController => aimingController;

        public CueBallSpinController SpinController => spinController;

        public float MaximumCuePullbackMeters => maximumCuePullbackMeters;

        public float PointerDeltaSensitivity => pointerDeltaSensitivity;

        public float CueStrokeMetersPerPointerUnit => cueStrokeMetersPerPointerUnit;

        public float MaximumShotSpeedMetersPerSecond => maximumShotSpeedMetersPerSecond;

        public float PullbackMeters => shotPowerState?.PullbackMeters ?? 0f;

        public float NormalizedPower => shotPowerState?.NormalizedPower ?? 0f;

        public bool ShotCommitted { get; private set; }

        private void Awake()
        {
            localPlayerInputReader = new LocalPlayerInputReader();
            shotPowerState = new ShotPowerState(maximumCuePullbackMeters);
        }

        private void OnEnable()
        {
            if (localPlayerInputReader == null)
            {
                localPlayerInputReader = new LocalPlayerInputReader();
            }

            shotPowerState = new ShotPowerState(maximumCuePullbackMeters);
            ShotCommitted = false;
            strikeQueued = false;
            queuedStrike = default;
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

            ProcessInput(localPlayerInputReader.Read());
        }

        private void FixedUpdate()
        {
            if (!strikeQueued || ShotCommitted || cueBall == null)
            {
                return;
            }

            cueBall.AddForce(queuedStrike.LinearVelocityChange, ForceMode.VelocityChange);
            cueBall.angularVelocity += queuedStrike.AngularVelocityChange;
            spinController?.ResetToCenter();
            strikeQueued = false;
            ShotCommitted = true;
            enabled = false;
        }

        internal void ProcessInput(LocalPlayerInputSnapshot input)
        {
            if (shotPowerState == null || cueBall == null || aimingController == null || strikeQueued || ShotCommitted)
            {
                return;
            }

            if (input.PrimaryButtonIsPressed)
            {
                var pointerInput = Mathf.Clamp(input.PointerDelta.y * pointerDeltaSensitivity, -1f, 1f);
                var pointerPullback = pointerInput * cueStrokeMetersPerPointerUnit;
                var controllerPullback = input.ActionAxis.y * controllerPullbackMetersPerSecond * Time.deltaTime;
                shotPowerState.AdjustPullback(pointerPullback + controllerPullback);
                ApplyCuePullbackPose();
            }
            else if (previousPrimaryButtonPressed)
            {
                TryQueueShot();
            }

            previousPrimaryButtonPressed = input.PrimaryButtonIsPressed;
        }

        private bool TryQueueShot()
        {
            if (strikeQueued || ShotCommitted || !shotPowerState.HasUsablePower)
            {
                return false;
            }

            var direction = aimingController.Direction;
            var shotSpeed = shotPowerState.NormalizedPower * maximumShotSpeedMetersPerSecond;
            queuedStrike = CueBallStrikeModel.CalculateVelocityChange(
                new Vector3(direction.X, 0f, direction.Y),
                shotSpeed,
                spinController?.Spin ?? default,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            RestoreCuePose();
            strikeQueued = true;
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

