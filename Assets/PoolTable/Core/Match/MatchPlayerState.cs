using System;
using PoolTable.Core.Balls;

namespace PoolTable.Core.Match
{
    public readonly struct MatchPlayerState : IEquatable<MatchPlayerState>
    {
        public MatchPlayerState(MatchPlayerId id, BallGroup group = BallGroup.None)
        {
            ValidatePlayerId(id);
            ValidateGroup(group);

            Id = id;
            Group = group;
        }

        public MatchPlayerId Id { get; }

        public BallGroup Group { get; }

        public bool HasAssignedGroup => Group != BallGroup.None;

        public MatchPlayerState WithGroup(BallGroup group)
        {
            if (group == BallGroup.None)
            {
                throw new ArgumentException("An assigned player group must be solids or stripes.", nameof(group));
            }

            return new MatchPlayerState(Id, group);
        }

        public bool Equals(MatchPlayerState other) => Id == other.Id && Group == other.Group;

        public override bool Equals(object obj) => obj is MatchPlayerState other && Equals(other);

        public override int GetHashCode() => ((int)Id * 397) ^ (int)Group;

        public static bool operator ==(MatchPlayerState left, MatchPlayerState right) => left.Equals(right);

        public static bool operator !=(MatchPlayerState left, MatchPlayerState right) => !left.Equals(right);

        internal static void ValidatePlayerId(MatchPlayerId id)
        {
            if (id != MatchPlayerId.PlayerOne && id != MatchPlayerId.PlayerTwo)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Match player must be PlayerOne or PlayerTwo.");
            }
        }

        private static void ValidateGroup(BallGroup group)
        {
            if (group != BallGroup.None && group != BallGroup.Solids && group != BallGroup.Stripes)
            {
                throw new ArgumentOutOfRangeException(nameof(group), group, "Player group must be None, Solids, or Stripes.");
            }
        }
    }
}
