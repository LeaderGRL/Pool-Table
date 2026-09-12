namespace PoolTable.Core.Rules
{
    public readonly struct RailOrPocketEvaluation
    {
        internal RailOrPocketEvaluation(
            bool isSatisfied,
            RailOrPocketRequirementReason reason,
            int pocketedBallCount,
            int railContactBallCount)
        {
            IsSatisfied = isSatisfied;
            Reason = reason;
            PocketedBallCount = pocketedBallCount;
            RailContactBallCount = railContactBallCount;
        }

        public bool IsSatisfied { get; }

        public RailOrPocketRequirementReason Reason { get; }

        public int PocketedBallCount { get; }

        public int RailContactBallCount { get; }
    }
}
