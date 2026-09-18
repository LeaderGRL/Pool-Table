using System;
using PoolTable.Core.Match;

namespace PoolTable.Core.Rules
{
    public static class BallInHandRule
    {
        public static MatchState GrantAfterStandardFoul(MatchState state, FoulResolution foulResolution)
        {
            ValidateGrantState(state);

            if (state.Phase == MatchPhase.Break)
            {
                throw new InvalidOperationException(
                    "Standard-foul ball-in-hand cannot resolve a break foul. Use the explicit break-foul option instead.");
            }

            if (!foulResolution.HasFoul)
            {
                throw new ArgumentException("Ball-in-hand requires a foul.", nameof(foulResolution));
            }

            var incomingPlayer = OtherPlayer(state.CurrentPlayer);
            var incomingState = state.WithCurrentPlayer(incomingPlayer);

            return incomingState.WithBallInHand(
                new BallInHandState(incomingPlayer, CueBallPlacementArea.Anywhere));
        }

        public static MatchState GrantAfterOpeningBreakCueBallFoul(
            MatchState state,
            FoulResolution foulResolution)
        {
            ValidateGrantState(state);

            if (state.Phase == MatchPhase.Break || state.Phase == MatchPhase.Finished || state.TourNumber != 1)
            {
                throw new InvalidOperationException(
                    "Opening-break ball-in-hand requires the first post-break Tour state.");
            }

            if (!foulResolution.Has(ShotFoul.CueBallScratch)
                && !foulResolution.Has(ShotFoul.CueBallOffTable))
            {
                throw new ArgumentException(
                    "Opening-break ball-in-hand requires a cue-ball scratch or off-table foul.",
                    nameof(foulResolution));
            }

            var incomingPlayer = OtherPlayer(state.CurrentPlayer);
            var incomingState = state.WithCurrentPlayerPreservingTour(incomingPlayer);

            return incomingState.WithBallInHand(
                new BallInHandState(incomingPlayer, CueBallPlacementArea.Anywhere));
        }

        public static MatchState ChooseAboveHeadStringAfterBreakFoul(
            MatchState state,
            FoulResolution foulResolution)
        {
            ValidateGrantState(state);

            if (state.Phase != MatchPhase.Break)
            {
                throw new InvalidOperationException(
                    "Above-Head-String ball-in-hand is only a selectable consequence of a break foul.");
            }

            if (!foulResolution.HasFoul)
            {
                throw new ArgumentException("The break-foul option requires a foul.", nameof(foulResolution));
            }

            var incomingPlayer = OtherPlayer(state.CurrentPlayer);
            var incomingBreakState = state.WithCurrentPlayer(incomingPlayer);
            var openTableState = OpenTableRule.EnterAfterBreak(incomingBreakState);

            return openTableState.WithBallInHand(
                new BallInHandState(incomingPlayer, CueBallPlacementArea.AboveHeadString));
        }

        public static MatchState CompletePlacement(MatchState state, MatchPlayerId player)
        {
            ValidateMutableState(state);
            MatchPlayerState.ValidatePlayerId(player);

            if (!state.BallInHand.IsActive)
            {
                throw new InvalidOperationException("Cue-ball placement cannot complete without active ball-in-hand.");
            }

            if (state.CurrentPlayer != player || state.BallInHand.Recipient != player)
            {
                throw new InvalidOperationException("Only the current ball-in-hand recipient can complete cue-ball placement.");
            }

            return state.WithBallInHand(BallInHandState.None);
        }

        private static void ValidateGrantState(MatchState state)
        {
            ValidateMutableState(state);

            if (state.BallInHand.IsActive)
            {
                throw new InvalidOperationException("The match already has active ball-in-hand.");
            }
        }

        private static void ValidateMutableState(MatchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Phase == MatchPhase.Finished)
            {
                throw new InvalidOperationException("Ball-in-hand cannot change after the match has finished.");
            }
        }

        private static MatchPlayerId OtherPlayer(MatchPlayerId player)
        {
            return player == MatchPlayerId.PlayerOne
                ? MatchPlayerId.PlayerTwo
                : MatchPlayerId.PlayerOne;
        }
    }
}
