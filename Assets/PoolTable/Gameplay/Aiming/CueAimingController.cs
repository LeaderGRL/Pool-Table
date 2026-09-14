using System;
using PoolTable.Core.Shots;
using PoolTable.Input;
using UnityEngine;

namespace PoolTable.Gameplay.Aiming
{
    [DisallowMultipleComponent]
    public sealed class CueAimingController : MonoBehaviour
    {
        [SerializeField] private GameObject cueBall;
        [SerializeField] private float cueDistance = 1.6666667f;
        [SerializeField] private float yawDegreesPerPointerUnit = 0.1f;

        private AimingState aimingState;
        private MouseInputReader mouseInputReader;
        private float cueHeightOffset;

        public GameObject CueBall => cueBall;

        public float CueDistance => cueDistance;

        public float YawDegreesPerPointerUnit => yawDegreesPerPointerUnit;

        public ShotDirection Direction => aimingState?.Direction
            ?? throw new InvalidOperationException("Aiming state is not initialized.");

        private void Awake()
        {
            mouseInputReader = new MouseInputReader();
            InitializeFromCurrentPose();
        }

        private void OnEnable()
        {
            if (mouseInputReader == null)
            {
                mouseInputReader = new MouseInputReader();
            }

            if (aimingState == null)
            {
                InitializeFromCurrentPose();
            }

            ApplyCuePose();
        }

        private void Update()
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var input = mouseInputReader.Read();
            aimingState.RotateDegrees(input.Delta.x * yawDegreesPerPointerUnit);
            ApplyCuePose();
        }

        private void InitializeFromCurrentPose()
        {
            if (cueBall == null)
            {
                aimingState = null;
                return;
            }

            var forward = transform.forward;
            var planarDirection = new Vector2(forward.x, forward.z);
            if (planarDirection.sqrMagnitude <= 0.000001f)
            {
                var towardBall = cueBall.transform.position - transform.position;
                planarDirection = new Vector2(towardBall.x, towardBall.z);
            }

            if (planarDirection.sqrMagnitude <= 0.000001f)
            {
                planarDirection = Vector2.up;
            }

            cueHeightOffset = transform.position.y - cueBall.transform.position.y;
            aimingState = new AimingState(new ShotDirection(planarDirection.x, planarDirection.y));
        }

        private void ApplyCuePose()
        {
            if (aimingState == null || cueBall == null)
            {
                return;
            }

            var direction = new Vector3(Direction.X, 0f, Direction.Y);
            transform.position = cueBall.transform.position
                - (direction * cueDistance)
                + (Vector3.up * cueHeightOffset);

            var directionToCueBall = cueBall.transform.position - transform.position;
            if (directionToCueBall.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToCueBall, Vector3.up);
            }
        }
    }
}
