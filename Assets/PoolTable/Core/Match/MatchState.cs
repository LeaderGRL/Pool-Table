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
            : this(playerOne, playerTwo, currentPlayer, phase, BallInHandState.None)
        {
        }

        internal MatchState(
            MatchPlayerState playerOne,
            MatchPlayerState playerTwo,
            MatchPlayerId currentPlayer,
            MatchPhase phase,
            BallInHandState ballInHand)
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

            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
            CurrentPlayer = currentPlayer;
            Phase = phase;
            BallInHand = ballInHand;
        }

        public MatchPlayerState PlayerOne { get; }

        public MatchPlayerState PlayerTwo { get; }

        public MatchPlayerId CurrentPlayer { get; }

        public MatchPhase Phase { get; }

        public BallInHandState BallInHand { get; }

        public bool HasBallInHand => BallInHand.IsActive;

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
            return new MatchState(PlayerOne, PlayerTwo, currentPlayer, Phase, BallInHand);
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
            return new MatchState(PlayerOne, PlayerTwo, CurrentPlayer, phase, BallInHand);
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
                BallInHand);
        }

        internal MatchState WithBallInHand(BallInHandState ballInHand)
        {
            return new MatchState(PlayerOne, PlayerTwo, CurrentPlayer, Phase, ballInHand);
        }

        public bool Equals(MatchState other)
        {
            return other != null
                && PlayerOne == other.PlayerOne
                && PlayerTwo == other.PlayerTwo
                && CurrentPlayer == other.CurrentPlayer
                && Phase == other.Phase
                && BallInHand == other.BallInHand;
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
                return hashCode;
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
    }
}
