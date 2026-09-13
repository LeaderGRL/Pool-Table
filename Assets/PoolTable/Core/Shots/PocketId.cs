using System;

namespace PoolTable.Core.Shots
{
    public readonly struct PocketId : IEquatable<PocketId>
    {
        public const int MinimumIndex = 1;
        public const int MaximumIndex = 6;

        public PocketId(int index)
        {
            if (index < MinimumIndex || index > MaximumIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"Pocket index must be between {MinimumIndex} and {MaximumIndex}.");
            }

            Index = index;
        }

        public int Index { get; }

        public bool IsValid => Index >= MinimumIndex && Index <= MaximumIndex;

        public bool Equals(PocketId other) => Index == other.Index;

        public override bool Equals(object obj) => obj is PocketId other && Equals(other);

        public override int GetHashCode() => Index;

        public override string ToString() => Index.ToString();

        public static bool operator ==(PocketId left, PocketId right) => left.Equals(right);

        public static bool operator !=(PocketId left, PocketId right) => !left.Equals(right);
    }
}
