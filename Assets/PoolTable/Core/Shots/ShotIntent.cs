using System;
using PoolTable.Core.Match;

namespace PoolTable.Core.Shots
{
    public readonly struct ShotIntent : IEquatable<ShotIntent>
    {
        public ShotIntent(MatchPlayerId player, ShotDirection direction, float normalizedPower)
            : this(player, direction, normalizedPower, null)
        {
        }

        public ShotIntent(
            MatchPlayerId player,
            ShotDirection direction,
            float normalizedPower,
            CalledShot? calledShot)
        {
            if (player != MatchPlayerId.PlayerOne && player != MatchPlayerId.PlayerTwo)
            {
                throw new ArgumentOutOfRangeException(nameof(player), player, "Shot intent requires a valid match player.");
            }

            if (direction.X == 0f && direction.Y == 0f)
            {
                throw new ArgumentException("Shot intent requires a valid non-zero direction.", nameof(direction));
            }

            if (float.IsNaN(normalizedPower) || float.IsInfinity(normalizedPower) || normalizedPower <= 0f || normalizedPower > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedPower), normalizedPower, "Shot power must be finite and greater than 0 up to 1 inclusive.");
            }

            if (calledShot.HasValue && !calledShot.Value.IsValid)
            {
                throw new ArgumentException("Shot intent contains invalid called-shot information.", nameof(calledShot));
            }

            Player = player;
            Direction = direction;
            NormalizedPower = normalizedPower;
            CalledShot = calledShot;
        }

        public MatchPlayerId Player { get; }

        public ShotDirection Direction { get; }

        public float NormalizedPower { get; }

        public CalledShot? CalledShot { get; }

        public bool HasCalledShot => CalledShot.HasValue;

        public bool Equals(ShotIntent other)
        {
            return Player == other.Player
                && Direction.Equals(other.Direction)
                && NormalizedPower.Equals(other.NormalizedPower)
                && Nullable.Equals(CalledShot, other.CalledShot);
        }

        public override bool Equals(object obj) => obj is ShotIntent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Player;
                hashCode = (hashCode * 397) ^ Direction.GetHashCode();
                hashCode = (hashCode * 397) ^ NormalizedPower.GetHashCode();
                hashCode = (hashCode * 397) ^ (CalledShot?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public static bool operator ==(ShotIntent left, ShotIntent right) => left.Equals(right);

        public static bool operator !=(ShotIntent left, ShotIntent right) => !left.Equals(right);
    }
}
