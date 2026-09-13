using System;
using PoolTable.Core.Balls;

namespace PoolTable.Core.Shots
{
    public readonly struct PocketedBall : IEquatable<PocketedBall>
    {
        public PocketedBall(BallId ball, PocketId pocket)
        {
            if (!pocket.IsValid)
            {
                throw new ArgumentException("A pocketed-ball observation requires a valid table pocket.", nameof(pocket));
            }

            Ball = ball;
            Pocket = pocket;
        }

        public BallId Ball { get; }

        public PocketId Pocket { get; }

        public bool IsValid => Pocket.IsValid;

        public bool Equals(PocketedBall other)
        {
            return Ball == other.Ball && Pocket == other.Pocket;
        }

        public override bool Equals(object obj) => obj is PocketedBall other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Ball.GetHashCode() * 397) ^ Pocket.GetHashCode();
            }
        }

        public static bool operator ==(PocketedBall left, PocketedBall right) => left.Equals(right);

        public static bool operator !=(PocketedBall left, PocketedBall right) => !left.Equals(right);
    }
}
