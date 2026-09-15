using System;
using System.Collections.Generic;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using UnityEngine;

namespace PoolTable.Gameplay.BallInHand
{
    public sealed class BallInHandPlacementSession
    {
        public BallInHandPlacementSession(MatchState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));

            if (!state.HasBallInHand || !state.BallInHand.Recipient.HasValue)
            {
                throw new InvalidOperationException("Cue-ball placement requires active ball-in-hand state.");
            }

            Recipient = state.BallInHand.Recipient.Value;
            PlacementArea = state.BallInHand.PlacementArea;
        }

        public MatchState State { get; private set; }

        public MatchPlayerId Recipient { get; }

        public CueBallPlacementArea PlacementArea { get; }

        public bool IsCompleted { get; private set; }

        public bool TryComplete(
            Vector2 candidate,
            IReadOnlyList<Vector2> occupiedBallCenters,
            out MatchState completedState)
        {
            if (IsCompleted
                || !BallInHandPlacementGeometry.IsLegal(candidate, PlacementArea, occupiedBallCenters))
            {
                completedState = State;
                return false;
            }

            State = BallInHandRule.CompletePlacement(State, Recipient);
            IsCompleted = true;
            completedState = State;
            return true;
        }
    }
}
