using System;

namespace PoolTable.Core.Balls
{
    public readonly struct BallId : IEquatable<BallId>
    {
        public const int CueBallNumber = 0;
        public const int EightBallNumber = 8;
        public const int MinimumNumber = 0;
        public const int MaximumNumber = 15;

        public BallId(int number)
        {
            if (number < MinimumNumber || number > MaximumNumber)
            {
                throw new ArgumentOutOfRangeException(nameof(number), number, $"Ball number must be between {MinimumNumber} and {MaximumNumber}.");
            }

            Number = number;
        }

        public int Number { get; }

        public bool IsCueBall => Number == CueBallNumber;

        public bool IsEightBall => Number == EightBallNumber;

        public BallGroup Group
        {
            get
            {
                if (Number >= 1 && Number <= 7)
                {
                    return BallGroup.Solids;
                }

                if (Number >= 9 && Number <= 15)
                {
                    return BallGroup.Stripes;
                }

                return BallGroup.None;
            }
        }

        public bool Equals(BallId other) => Number == other.Number;

        public override bool Equals(object obj) => obj is BallId other && Equals(other);

        public override int GetHashCode() => Number;

        public override string ToString() => Number.ToString();

        public static bool operator ==(BallId left, BallId right) => left.Equals(right);

        public static bool operator !=(BallId left, BallId right) => !left.Equals(right);
    }
}