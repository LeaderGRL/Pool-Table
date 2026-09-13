using System;

namespace PoolTable.Core.Match
{
    public readonly struct MatchResult : IEquatable<MatchResult>
    {
        private const MatchEndReason KnownReasons =
            MatchEndReason.EightBallLegallyPocketed
            | MatchEndReason.EightBallPocketedWithFoul
            | MatchEndReason.EightBallPocketedBeforeGroupCleared
            | MatchEndReason.EightBallPocketedInUncalledPocket
            | MatchEndReason.EightBallDrivenOffTable;

        internal MatchResult(MatchPlayerId winner, MatchPlayerId loser, MatchEndReason reasons)
        {
            Validate(winner, loser, reasons);

            Winner = winner;
            Loser = loser;
            Reasons = reasons;
        }

        public MatchPlayerId Winner { get; }

        public MatchPlayerId Loser { get; }

        public MatchEndReason Reasons { get; }

        public bool IsLegalEightBallWin => Reasons == MatchEndReason.EightBallLegallyPocketed;

        public bool HasReason(MatchEndReason reason)
        {
            return reason != MatchEndReason.None && (Reasons & reason) == reason;
        }

        public bool Equals(MatchResult other)
        {
            return Winner == other.Winner
                && Loser == other.Loser
                && Reasons == other.Reasons;
        }

        public override bool Equals(object obj) => obj is MatchResult other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Winner;
                hashCode = (hashCode * 397) ^ (int)Loser;
                hashCode = (hashCode * 397) ^ (int)Reasons;
                return hashCode;
            }
        }

        public static bool operator ==(MatchResult left, MatchResult right) => left.Equals(right);

        public static bool operator !=(MatchResult left, MatchResult right) => !left.Equals(right);

        internal static void Validate(MatchResult result)
        {
            Validate(result.Winner, result.Loser, result.Reasons);
        }

        private static void Validate(MatchPlayerId winner, MatchPlayerId loser, MatchEndReason reasons)
        {
            MatchPlayerState.ValidatePlayerId(winner);
            MatchPlayerState.ValidatePlayerId(loser);

            if (winner == loser)
            {
                throw new ArgumentException("Match winner and loser must be different players.");
            }

            if (reasons == MatchEndReason.None)
            {
                throw new ArgumentException("A finished match requires at least one end reason.", nameof(reasons));
            }

            if ((reasons & ~KnownReasons) != MatchEndReason.None)
            {
                throw new ArgumentOutOfRangeException(nameof(reasons), reasons, "Match result contains an unknown end reason.");
            }

            var legalWin = (reasons & MatchEndReason.EightBallLegallyPocketed) != 0;
            var lossReasons = reasons & ~MatchEndReason.EightBallLegallyPocketed;
            if (legalWin && lossReasons != MatchEndReason.None)
            {
                throw new ArgumentException(
                    "A legal eight-ball win cannot be combined with eight-ball loss reasons.",
                    nameof(reasons));
            }
        }
    }
}
