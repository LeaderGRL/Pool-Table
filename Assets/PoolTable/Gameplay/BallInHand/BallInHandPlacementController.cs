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
    public sealed class BallInHandPlacementController : MonoBehaviour
    {
        private const string LegacyPlacementCompletedMessage = "OnModernBallInHandPlacementCompleted";

        [SerializeField] private Transform cueBall;
        [SerializeField] private Transform ballsRoot;
        [SerializeField, Min(0f)] private float pointerMetersPerPixel = 0.0015f;
        [SerializeField, Min(0f)] private float controllerMetersPerSecond = 0.75f;

        private readonly LocalPlayerInputReader inputReader = new LocalPlayerInputReader();
        private readonly List<Vector2> occupiedBallCenters = new List<Vector2>(15);

        private Rigidbody cueBallRigidbody;
        private BallPocketCapture cueBallPocketCapture;
        private BallInHandPlacementSession session;
        private CueBallPlacementArea placementArea;
        private bool primaryActionWasPressed;
        private bool originalIsKinematic;
        private bool originalDetectCollisions;

        public event Action<MatchState> PlacementCompleted;

        public bool IsPlacing { get; private set; }

        public bool IsCurrentPositionLegal => IsPlacing && IsCandidateLegal(CurrentPlanarPosition);

        public MatchState LastCompletedMatchState { get; private set; }

        internal Vector2 CurrentPlanarPosition => new Vector2(cueBall.position.x, cueBall.position.z);

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
            session = new BallInHandPlacementSession(state);
            BeginPlacement(session.PlacementArea);
        }

        public void BeginLegacyScratchPlacement()
        {
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

            var tableLengthDelta = (input.PointerDelta.y * pointerMetersPerPixel)
                + (input.ActionAxis.y * controllerMetersPerSecond * Mathf.Max(0f, deltaTime));
            var tableWidthDelta = (input.PointerDelta.x * pointerMetersPerPixel)
                + (input.ActionAxis.x * controllerMetersPerSecond * Mathf.Max(0f, deltaTime));
            var next = BallInHandPlacementGeometry.ClampToPlacementArea(
                CurrentPlanarPosition + new Vector2(tableLengthDelta, tableWidthDelta),
                placementArea);

            cueBall.position = new Vector3(
                next.x,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                next.y);
        }

        private void BeginPlacement(CueBallPlacementArea area)
        {
            if (IsPlacing)
            {
                throw new InvalidOperationException("Cue-ball placement is already active.");
            }

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

            primaryActionWasPressed = true;
            IsPlacing = true;
            enabled = true;
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
    }
}
