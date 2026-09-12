using System;

namespace PoolTable.Core.Match
{
    public readonly struct BallInHandState : IEquatable<BallInHandState>
    {
        private readonly MatchPlayerId recipient;

        public BallInHandState(MatchPlayerId recipient, CueBallPlacementArea placementArea)
        {
            MatchPlayerState.ValidatePlayerId(recipient);

            if (placementArea != CueBallPlacementArea.Anywhere
                && placementArea != CueBallPlacementArea.AboveHeadString)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementArea),
                    placementArea,
                    "Active ball-in-hand placement must be Anywhere or AboveHeadString.");
            }

            this.recipient = recipient;
            PlacementArea = placementArea;
        }

        public static BallInHandState None => default;

        public CueBallPlacementArea PlacementArea { get; }

        public bool IsActive => PlacementArea != CueBallPlacementArea.None;

        public MatchPlayerId? Recipient => IsActive ? recipient : (MatchPlayerId?)null;

        public bool Equals(BallInHandState other)
        {
            return recipient == other.recipient && PlacementArea == other.PlacementArea;
        }

        public override bool Equals(object obj) => obj is BallInHandState other && Equals(other);

        public override int GetHashCode() => ((int)recipient * 397) ^ (int)PlacementArea;

        public static bool operator ==(BallInHandState left, BallInHandState right) => left.Equals(right);

        public static bool operator !=(BallInHandState left, BallInHandState right) => !left.Equals(right);
    }
}
