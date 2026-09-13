using System;
using PoolTable.Core.Balls;

namespace PoolTable.Core.Shots
{
    public readonly struct CalledShot : IEquatable<CalledShot>
    {
        public CalledShot(BallId objectBall, PocketId pocket)
        {
            if (objectBall.IsCueBall)
            {
                throw new ArgumentException("A called shot must target an object ball.", nameof(objectBall));
            }

            if (!pocket.IsValid)
            {
                throw new ArgumentException("A called shot requires a valid table pocket.", nameof(pocket));
            }

            ObjectBall = objectBall;
            Pocket = pocket;
        }

        public BallId ObjectBall { get; }

        public PocketId Pocket { get; }

        public bool IsValid => !ObjectBall.IsCueBall && Pocket.IsValid;

        public bool Equals(CalledShot other)
        {
            return ObjectBall == other.ObjectBall && Pocket == other.Pocket;
        }

        public override bool Equals(object obj) => obj is CalledShot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (ObjectBall.GetHashCode() * 397) ^ Pocket.GetHashCode();
            }
        }

        public static bool operator ==(CalledShot left, CalledShot right) => left.Equals(right);

        public static bool operator !=(CalledShot left, CalledShot right) => !left.Equals(right);
    }
}
