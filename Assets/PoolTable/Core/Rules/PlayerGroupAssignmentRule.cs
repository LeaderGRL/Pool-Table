using System;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;

namespace PoolTable.Core.Rules
{
    public static class PlayerGroupAssignmentRule
    {
        public static MatchState AssignFromLegallyPocketedBall(
            MatchState state,
            MatchPlayerId actingPlayer,
            BallId pocketedBall)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            MatchPlayerState.ValidatePlayerId(actingPlayer);

            if (!state.IsTableOpen)
            {
                throw new InvalidOperationException("Player groups can only be assigned while the table is open.");
            }

            if (actingPlayer != state.CurrentPlayer)
            {
                throw new InvalidOperationException("Only the current player can receive a group assignment.");
            }

            if (pocketedBall.Group == BallGroup.None)
            {
                throw new ArgumentException(
                    "Group assignment requires a legally pocketed solids or stripes ball.",
                    nameof(pocketedBall));
            }

            var solidsPlayer = pocketedBall.Group == BallGroup.Solids
                ? actingPlayer
                : OpponentOf(actingPlayer);

            return state.WithAssignedGroups(solidsPlayer);
        }

        private static MatchPlayerId OpponentOf(MatchPlayerId player)
        {
            return player == MatchPlayerId.PlayerOne
                ? MatchPlayerId.PlayerTwo
                : MatchPlayerId.PlayerOne;
        }
    }
}
