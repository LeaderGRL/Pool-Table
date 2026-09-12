namespace PoolTable.Core.Rules
{
    public readonly struct BreakEvaluation
    {
        internal BreakEvaluation(
            bool isLegal,
            BreakEvaluationReason reason,
            bool hasPocketedObjectBall,
            int objectBallRailContactCount)
        {
            IsLegal = isLegal;
            Reason = reason;
            HasPocketedObjectBall = hasPocketedObjectBall;
            ObjectBallRailContactCount = objectBallRailContactCount;
        }

        public bool IsLegal { get; }

        public BreakEvaluationReason Reason { get; }

        public bool HasPocketedObjectBall { get; }

        public int ObjectBallRailContactCount { get; }
    }
}
