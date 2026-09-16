using System;
using System.Collections.Generic;
using PoolTable.Core.Match;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Pockets;
using PoolTable.Input;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Gameplay.BallInHand
{
    internal interface ICursorStateAccessor
    {
        CursorLockMode LockState { get; set; }

        bool Visible { get; set; }
    }

    internal sealed class UnityCursorStateAccessor : ICursorStateAccessor
    {
        public CursorLockMode LockState
        {
            get => Cursor.lockState;
            set => Cursor.lockState = value;
        }

        public bool Visible
        {
            get => Cursor.visible;
            set => Cursor.visible = value;
        }
    }

    public sealed class BallInHandPlacementController : MonoBehaviour
    {
        private const string LegacyPlacementCompletedMessage = "OnModernBallInHandPlacementCompleted";

        [SerializeField] private Transform cueBall;
        [SerializeField] private Transform ballsRoot;
        [SerializeField] private Camera placementCamera;
        [SerializeField, Min(0f)] private float pointerMetersPerPixel = 0.0015f;
        [SerializeField, Min(0f)] private float controllerMetersPerSecond = 0.75f;
        [SerializeField, Range(25f, 75f)] private float placementCameraVerticalFov = 50f;
        [SerializeField, Range(15f, 75f)] private float placementCameraDownAngleDegrees = 42f;
        [SerializeField, Min(1f)] private float placementCameraHorizontalMargin = 1.18f;

        private readonly LocalPlayerInputReader inputReader = new LocalPlayerInputReader();
        private readonly List<Vector2> occupiedBallCenters = new List<Vector2>(15);
        private ICursorStateAccessor cursorStateAccessor = new UnityCursorStateAccessor();

        private Rigidbody cueBallRigidbody;
        private BallPocketCapture cueBallPocketCapture;
        private BallInHandPlacementSession session;
        private CueBallPlacementArea placementArea;
        private bool primaryActionWasPressed;
        private bool originalIsKinematic;
        private bool originalDetectCollisions;
        private CursorLockMode originalCursorLockState;
        private bool originalCursorVisible;
        private bool cursorStateCaptured;
        private Camera activePlacementCamera;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float originalCameraFieldOfView;
        private bool cameraStateCaptured;

        public event Action<MatchState> PlacementCompleted;

        public bool IsPlacing { get; private set; }

        public bool IsCurrentPositionLegal => IsPlacing && IsCandidateLegal(CurrentPlanarPosition);

        public MatchState LastCompletedMatchState { get; private set; }

        public float PlacementCameraVerticalFov => placementCameraVerticalFov;

        public float PlacementCameraDownAngleDegrees => placementCameraDownAngleDegrees;

        public float PlacementCameraHorizontalMargin => placementCameraHorizontalMargin;

        internal Vector2 CurrentPlanarPosition => new Vector2(cueBall.position.x, cueBall.position.z);

        internal ICursorStateAccessor CursorStateAccessor
        {
            get => cursorStateAccessor;
            set => cursorStateAccessor = value ?? throw new ArgumentNullException(nameof(value));
        }

        private void Awake()
        {
            if (cueBall == null)
            {
                throw new InvalidOperationException("Ball-in-hand placement requires a cue-ball reference.");
            }

            if (ballsRoot == null)
            {
                throw new InvalidOperationException("Ball-in-hand placement requires the Balls root reference.");
            }

            cueBallRigidbody = cueBall.GetComponent<Rigidbody>();
            cueBallPocketCapture = cueBall.GetComponent<BallPocketCapture>();

            if (cueBallRigidbody == null || cueBallPocketCapture == null)
            {
                throw new InvalidOperationException(
                    "The cue ball must provide Rigidbody and BallPocketCapture components for placement.");
            }
        }

        private void Update()
        {
            if (!IsPlacing)
            {
                return;
            }

            var input = inputReader.Read();
            MoveCandidate(input, Time.deltaTime);

            if (input.PrimaryActionIsPressed && !primaryActionWasPressed)
            {
                TryConfirmPlacement();
            }

            primaryActionWasPressed = input.PrimaryActionIsPressed;
        }

        public void BeginPlacement(MatchState state)
        {
            EnsurePlacementIsInactive();
            session = new BallInHandPlacementSession(state);
            BeginPlacement(session.PlacementArea);
        }

        public void BeginLegacyScratchPlacement()
        {
            EnsurePlacementIsInactive();
            session = null;
            BeginPlacement(CueBallPlacementArea.Anywhere);
        }

        public bool TryConfirmPlacement()
        {
            if (!IsPlacing)
            {
                return false;
            }

            RefreshOccupiedBallCenters();
            var candidate = CurrentPlanarPosition;

            if (!BallInHandPlacementGeometry.IsLegal(candidate, placementArea, occupiedBallCenters))
            {
                return false;
            }

            MatchState completedState = null;
            if (session != null && !session.TryComplete(candidate, occupiedBallCenters, out completedState))
            {
                return false;
            }

            CompletePhysicalPlacement();
            cueBall.gameObject.SendMessage(
                LegacyPlacementCompletedMessage,
                SendMessageOptions.DontRequireReceiver);
            IsPlacing = false;
            RestoreCursorState();
            RestorePlacementCameraState();
            enabled = false;

            if (completedState != null)
            {
                LastCompletedMatchState = completedState;
                PlacementCompleted?.Invoke(completedState);
            }

            return true;
        }

        internal void MoveCandidate(LocalPlayerInputSnapshot input, float deltaTime)
        {
            if (!IsPlacing)
            {
                return;
            }

            var next = CurrentPlanarPosition;
            var pointerMoved = input.PointerDelta.sqrMagnitude > 0.000001f;

            if (pointerMoved && input.HasPointerPosition && TryProjectPointerToTable(input.PointerPosition, out var pointerTarget))
            {
                next = pointerTarget;
            }
            else if (pointerMoved && !input.HasPointerPosition)
            {
                next += new Vector2(
                    input.PointerDelta.y * pointerMetersPerPixel,
                    input.PointerDelta.x * pointerMetersPerPixel);
            }

            next += new Vector2(
                input.ActionAxis.y * controllerMetersPerSecond * Mathf.Max(0f, deltaTime),
                input.ActionAxis.x * controllerMetersPerSecond * Mathf.Max(0f, deltaTime));

            var clampedNext = BallInHandPlacementGeometry.ClampToPlacementArea(
                next,
                placementArea);

            cueBall.position = new Vector3(
                clampedNext.x,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                clampedNext.y);
        }

        internal bool TryProjectPointerToTable(Vector2 pointerPosition, out Vector2 planarPosition)
        {
            planarPosition = default;
            var camera = placementCamera != null ? placementCamera : Camera.main;

            if (camera == null || !camera.pixelRect.Contains(pointerPosition))
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(pointerPosition);
            var tablePlane = new Plane(
                Vector3.up,
                new Vector3(0f, BilliardsPhysicalSpecification.BallCenterHeightMeters, 0f));

            if (!tablePlane.Raycast(ray, out var distance) || distance < 0f)
            {
                return false;
            }

            var point = ray.GetPoint(distance);
            planarPosition = new Vector2(point.x, point.z);
            return true;
        }

        private void BeginPlacement(CueBallPlacementArea area)
        {
            EnsurePlacementIsInactive();

            placementArea = area;
            originalIsKinematic = cueBallRigidbody.isKinematic;
            originalDetectCollisions = cueBallRigidbody.detectCollisions;

            cueBall.gameObject.SetActive(true);
            cueBallRigidbody.linearVelocity = Vector3.zero;
            cueBallRigidbody.angularVelocity = Vector3.zero;
            cueBallRigidbody.isKinematic = true;
            cueBallRigidbody.detectCollisions = false;

            var start = cueBallPocketCapture.IsCaptured
                ? new Vector2(BilliardsPhysicalSpecification.HeadStringX, 0f)
                : CurrentPlanarPosition;
            start = BallInHandPlacementGeometry.ClampToPlacementArea(start, placementArea);
            cueBall.position = new Vector3(
                start.x,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                start.y);

            CaptureCursorStateForPlacement();
            CaptureAndApplyPlacementCameraState();
            primaryActionWasPressed = true;
            IsPlacing = true;
            enabled = true;
        }

        private void OnDisable()
        {
            RestoreCursorState();
            RestorePlacementCameraState();
        }

        private void EnsurePlacementIsInactive()
        {
            if (IsPlacing)
            {
                throw new InvalidOperationException("Cue-ball placement is already active.");
            }
        }

        private bool IsCandidateLegal(Vector2 candidate)
        {
            RefreshOccupiedBallCenters();
            return BallInHandPlacementGeometry.IsLegal(candidate, placementArea, occupiedBallCenters);
        }

        private void RefreshOccupiedBallCenters()
        {
            occupiedBallCenters.Clear();
            var identities = ballsRoot.GetComponentsInChildren<BallIdentity>(true);

            foreach (var identity in identities)
            {
                if (identity.IsCueBall || !identity.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var position = identity.transform.position;
                occupiedBallCenters.Add(new Vector2(position.x, position.z));
            }
        }

        private void CompletePhysicalPlacement()
        {
            var acceptedPosition = cueBall.position;
            cueBallRigidbody.isKinematic = originalIsKinematic;
            cueBallRigidbody.detectCollisions = originalDetectCollisions;

            if (cueBallPocketCapture.IsCaptured)
            {
                cueBallPocketCapture.Restore(acceptedPosition);
                return;
            }

            cueBallRigidbody.linearVelocity = Vector3.zero;
            cueBallRigidbody.angularVelocity = Vector3.zero;

            if (!cueBallRigidbody.isKinematic)
            {
                cueBallRigidbody.WakeUp();
            }
        }

        private void CaptureCursorStateForPlacement()
        {
            originalCursorLockState = cursorStateAccessor.LockState;
            originalCursorVisible = cursorStateAccessor.Visible;
            cursorStateCaptured = true;

            cursorStateAccessor.LockState = CursorLockMode.Confined;
            cursorStateAccessor.Visible = true;
        }

        private void RestoreCursorState()
        {
            if (!cursorStateCaptured)
            {
                return;
            }

            cursorStateAccessor.LockState = originalCursorLockState;
            cursorStateAccessor.Visible = originalCursorVisible;
            cursorStateCaptured = false;
        }

        private void CaptureAndApplyPlacementCameraState()
        {
            var camera = placementCamera != null ? placementCamera : Camera.main;
            if (camera == null)
            {
                return;
            }

            activePlacementCamera = camera;
            originalCameraPosition = camera.transform.position;
            originalCameraRotation = camera.transform.rotation;
            originalCameraFieldOfView = camera.fieldOfView;
            cameraStateCaptured = true;

            camera.fieldOfView = placementCameraVerticalFov;

            var verticalFovRadians = placementCameraVerticalFov * Mathf.Deg2Rad;
            var aspect = Mathf.Max(0.1f, camera.aspect);
            var horizontalFovRadians = 2f * Mathf.Atan(Mathf.Tan(verticalFovRadians * 0.5f) * aspect);
            var halfTableLength = BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f;
            var distanceToCenter = (halfTableLength * placementCameraHorizontalMargin)
                / Mathf.Max(0.01f, Mathf.Tan(horizontalFovRadians * 0.5f));

            var downAngleRadians = placementCameraDownAngleDegrees * Mathf.Deg2Rad;
            var forward = new Vector3(
                0f,
                -Mathf.Sin(downAngleRadians),
                Mathf.Cos(downAngleRadians)).normalized;
            var tableCenter = new Vector3(0f, BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters, 0f);
            var cameraPosition = tableCenter - (forward * distanceToCenter);

            camera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(forward, Vector3.up));
        }

        private void RestorePlacementCameraState()
        {
            if (!cameraStateCaptured || activePlacementCamera == null)
            {
                return;
            }

            activePlacementCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
            activePlacementCamera.fieldOfView = originalCameraFieldOfView;
            activePlacementCamera = null;
            cameraStateCaptured = false;
        }
    }
}
