using System;
using PoolTable.Core.Balls;

namespace PoolTable.Core.Match
{
    public sealed class MatchState : IEquatable<MatchState>
    {
        public MatchState(
            MatchPlayerState playerOne,
            MatchPlayerState playerTwo,
            MatchPlayerId currentPlayer,
            MatchPhase phase)
            : this(
                playerOne,
                playerTwo,
                currentPlayer,
                phase,
                BallInHandState.None,
                null,
                DefaultTourNumber(phase),
                DeuxCoupsState.Inactive)
        {
        }

        internal MatchState(
            MatchPlayerState playerOne,
            MatchPlayerState playerTwo,
            MatchPlayerId currentPlayer,
            MatchPhase phase,
            BallInHandState ballInHand)
            : this(
                playerOne,
                playerTwo,
                currentPlayer,
                phase,
                ballInHand,
                null,
                DefaultTourNumber(phase),
                DeuxCoupsState.Inactive)
        {
        }

        internal MatchState(
            MatchPlayerState playerOne,
            MatchPlayerState playerTwo,
            MatchPlayerId currentPlayer,
            MatchPhase phase,
            BallInHandState ballInHand,
            MatchResult? result)
            : this(
                playerOne,
                playerTwo,
                currentPlayer,
                phase,
                ballInHand,
                result,
                DefaultTourNumber(phase),
                DeuxCoupsState.Inactive)
        {
        }

        private MatchState(
            MatchPlayerState playerOne,
            MatchPlayerState playerTwo,
            MatchPlayerId currentPlayer,
            MatchPhase phase,
            BallInHandState ballInHand,
            MatchResult? result,
            int tourNumber,
            DeuxCoupsState deuxCoups)
        {
            if (playerOne.Id != MatchPlayerId.PlayerOne)
            {
                throw new ArgumentException("Player one state must use the PlayerOne identifier.", nameof(playerOne));
            }

            if (playerTwo.Id != MatchPlayerId.PlayerTwo)
            {
                throw new ArgumentException("Player two state must use the PlayerTwo identifier.", nameof(playerTwo));
            }

            MatchPlayerState.ValidatePlayerId(currentPlayer);
            ValidatePhase(phase);
            ValidateGroupAssignments(playerOne, playerTwo);
            ValidatePhaseConsistency(playerOne, playerTwo, phase);
            ValidateBallInHand(currentPlayer, phase, ballInHand);
            ValidateResult(phase, result);
            ValidateTourNumber(phase, tourNumber);
            ValidateDeuxCoups(phase, deuxCoups);

            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
            CurrentPlayer = currentPlayer;
            Phase = phase;
            BallInHand = ballInHand;
            Result = result;
            TourNumber = tourNumber;
            DeuxCoups = deuxCoups;
        }

        public MatchPlayerState PlayerOne { get; }

        public MatchPlayerState PlayerTwo { get; }

        public MatchPlayerId CurrentPlayer { get; }

        public MatchPhase Phase { get; }

        public BallInHandState BallInHand { get; }

        public MatchResult? Result { get; }

        public int TourNumber { get; }

        public DeuxCoupsState DeuxCoups { get; }

        public bool HasBallInHand => BallInHand.IsActive;

        public bool IsFinished => Result.HasValue;

        public bool IsTableOpen => Phase == MatchPhase.OpenTable;

        public static MatchState CreateInitial(MatchPlayerId startingPlayer = MatchPlayerId.PlayerOne)
        {
            return new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne),
                new MatchPlayerState(MatchPlayerId.PlayerTwo),
                startingPlayer,
                MatchPhase.Break);
        }

        public MatchPlayerState GetPlayer(MatchPlayerId id)
        {
            MatchPlayerState.ValidatePlayerId(id);
            return id == MatchPlayerId.PlayerOne ? PlayerOne : PlayerTwo;
        }

        public MatchState WithCurrentPlayer(MatchPlayerId currentPlayer)
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("Current player cannot change after the match has finished.");
            }

            MatchPlayerState.ValidatePlayerId(currentPlayer);

            var playerChanged = currentPlayer != CurrentPlayer;
            var nextTourNumber = playerChanged && Phase != MatchPhase.Break
                ? TourNumber + 1
                : TourNumber;
            var nextDeuxCoups = playerChanged ? DeuxCoupsState.Inactive : DeuxCoups;

            return new MatchState(
                PlayerOne,
                PlayerTwo,
                currentPlayer,
                Phase,
                BallInHand,
                Result,
                nextTourNumber,
                nextDeuxCoups);
        }

        public MatchState AdvanceTurn()
        {
            var nextPlayer = CurrentPlayer == MatchPlayerId.PlayerOne
                ? MatchPlayerId.PlayerTwo
                : MatchPlayerId.PlayerOne;

            return WithCurrentPlayer(nextPlayer);
        }

        internal MatchState WithPhase(MatchPhase phase)
        {
            if (phase == MatchPhase.Finished)
            {
                throw new InvalidOperationException("Finished phase requires a match result.");
            }

            var nextTourNumber = Phase == MatchPhase.Break && phase != MatchPhase.Break
                ? 1
                : TourNumber;

            return new MatchState(
                PlayerOne,
                PlayerTwo,
                CurrentPlayer,
                phase,
                BallInHand,
                Result,
                nextTourNumber,
                DeuxCoups);
        }

        internal MatchState WithAssignedGroups(MatchPlayerId solidsPlayer)
        {
            MatchPlayerState.ValidatePlayerId(solidsPlayer);

            var playerOneGroup = solidsPlayer == MatchPlayerId.PlayerOne
                ? BallGroup.Solids
                : BallGroup.Stripes;
            var playerTwoGroup = playerOneGroup == BallGroup.Solids
                ? BallGroup.Stripes
                : BallGroup.Solids;

            return new MatchState(
                PlayerOne.WithGroup(playerOneGroup),
                PlayerTwo.WithGroup(playerTwoGroup),
                CurrentPlayer,
                MatchPhase.GroupsAssigned,
                BallInHand,
                Result,
                TourNumber,
                DeuxCoups);
        }

        internal MatchState WithBallInHand(BallInHandState ballInHand)
        {
            return new MatchState(
                PlayerOne,
                PlayerTwo,
                CurrentPlayer,
                Phase,
                ballInHand,
                Result,
                TourNumber,
                DeuxCoups);
        }

        internal MatchState WithCurrentPlayerPreservingTour(MatchPlayerId currentPlayer)
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("Current player cannot change after the match has finished.");
            }

            MatchPlayerState.ValidatePlayerId(currentPlayer);

            return new MatchState(
                PlayerOne,
                PlayerTwo,
                currentPlayer,
                Phase,
                BallInHand,
                Result,
                TourNumber,
                currentPlayer == CurrentPlayer ? DeuxCoups : DeuxCoupsState.Inactive);
        }

        internal MatchState WithDeuxCoups(DeuxCoupsState deuxCoups)
        {
            return new MatchState(
                PlayerOne,
                PlayerTwo,
                CurrentPlayer,
                Phase,
                BallInHand,
                Result,
                TourNumber,
                deuxCoups);
        }

        internal MatchState WithResult(MatchResult result)
        {
            return new MatchState(
                PlayerOne,
                PlayerTwo,
                CurrentPlayer,
                MatchPhase.Finished,
                BallInHandState.None,
                result,
                TourNumber,
                DeuxCoupsState.Inactive);
        }

        public bool Equals(MatchState other)
        {
            return other != null
                && PlayerOne == other.PlayerOne
                && PlayerTwo == other.PlayerTwo
                && CurrentPlayer == other.CurrentPlayer
                && Phase == other.Phase
                && BallInHand == other.BallInHand
                && Nullable.Equals(Result, other.Result)
                && TourNumber == other.TourNumber
                && DeuxCoups == other.DeuxCoups;
        }

        public override bool Equals(object obj) => Equals(obj as MatchState);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PlayerOne.GetHashCode();
                hashCode = (hashCode * 397) ^ PlayerTwo.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)CurrentPlayer;
                hashCode = (hashCode * 397) ^ (int)Phase;
                hashCode = (hashCode * 397) ^ BallInHand.GetHashCode();
                hashCode = (hashCode * 397) ^ (Result?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ TourNumber;
                hashCode = (hashCode * 397) ^ (int)DeuxCoups;
                return hashCode;
            }
        }

        private static int DefaultTourNumber(MatchPhase phase)
        {
            return phase == MatchPhase.Break ? 0 : 1;
        }

        private static void ValidateTourNumber(MatchPhase phase, int tourNumber)
        {
            if (tourNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tourNumber), tourNumber, "Tour number cannot be negative.");
            }

            if (phase == MatchPhase.Break && tourNumber != 0)
            {
                throw new ArgumentException("Break phase must use Tour 0.", nameof(tourNumber));
            }

            if (phase != MatchPhase.Break && phase != MatchPhase.Finished && tourNumber < 1)
            {
                throw new ArgumentException("Active post-break play must start at Tour 1.", nameof(tourNumber));
            }
        }

        private static void ValidateDeuxCoups(MatchPhase phase, DeuxCoupsState deuxCoups)
        {
            if (deuxCoups < DeuxCoupsState.Inactive || deuxCoups > DeuxCoupsState.TwoRemaining)
            {
                throw new ArgumentOutOfRangeException(nameof(deuxCoups), deuxCoups, "Unknown Deux coups state.");
            }

            if ((phase == MatchPhase.Break || phase == MatchPhase.Finished)
                && deuxCoups != DeuxCoupsState.Inactive)
            {
                throw new ArgumentException("Deux coups cannot be active during the break or after the match finishes.", nameof(deuxCoups));
            }
        }

        private static void ValidateGroupAssignments(MatchPlayerState playerOne, MatchPlayerState playerTwo)
        {
            if (playerOne.HasAssignedGroup != playerTwo.HasAssignedGroup)
            {
                throw new ArgumentException("Player groups must be either both unassigned or both assigned.");
            }

            if (playerOne.HasAssignedGroup && playerOne.Group == playerTwo.Group)
            {
                throw new ArgumentException("Assigned player groups must be complementary.");
            }
        }

        private static void ValidatePhase(MatchPhase phase)
        {
            if (phase < MatchPhase.Break || phase > MatchPhase.Finished)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown match phase.");
            }
        }

        private static void ValidatePhaseConsistency(
            MatchPlayerState playerOne,
            MatchPlayerState playerTwo,
            MatchPhase phase)
        {
            var groupsAssigned = playerOne.HasAssignedGroup && playerTwo.HasAssignedGroup;

            if (phase == MatchPhase.GroupsAssigned && !groupsAssigned)
            {
                throw new ArgumentException("GroupsAssigned phase requires both player groups to be assigned.");
            }

            if ((phase == MatchPhase.Break || phase == MatchPhase.OpenTable) && groupsAssigned)
            {
                throw new ArgumentException("Break and OpenTable phases require player groups to remain unassigned.");
            }
        }

        private static void ValidateBallInHand(
            MatchPlayerId currentPlayer,
            MatchPhase phase,
            BallInHandState ballInHand)
        {
            if (!ballInHand.IsActive)
            {
                return;
            }

            if (phase == MatchPhase.Finished)
            {
                throw new ArgumentException("Finished matches cannot have active ball-in-hand.", nameof(ballInHand));
            }

            if (phase == MatchPhase.Break)
            {
                throw new ArgumentException("Break phase cannot have active ball-in-hand.", nameof(ballInHand));
            }

            if (ballInHand.Recipient != currentPlayer)
            {
                throw new ArgumentException(
                    "The active ball-in-hand recipient must be the current player.",
                    nameof(ballInHand));
            }


            if (ballInHand.PlacementArea == CueBallPlacementArea.AboveHeadString
                && phase != MatchPhase.OpenTable)
            {
                throw new ArgumentException(
                    "Above-head-string ball-in-hand is only valid immediately after a break foul.",
                    nameof(ballInHand));
            }
        }

        private static void ValidateResult(MatchPhase phase, MatchResult? result)
        {
            if (phase == MatchPhase.Finished && !result.HasValue)
            {
                throw new ArgumentException("Finished matches require a match result.", nameof(result));
            }

            if (phase != MatchPhase.Finished && result.HasValue)
            {
                throw new ArgumentException("Only finished matches may carry a match result.", nameof(result));
            }

            if (result.HasValue)
            {
                MatchResult.Validate(result.Value);
            }
        }
    }
}
