using PoolTable.Core.Balls;

namespace PoolTable.Core.Rules
{
    public readonly struct FirstContactEvaluation
    {
        public FirstContactEvaluation(
            bool isLegal,
            FirstContactEvaluationReason reason,
            BallId? firstObjectBallContact)
        {
            IsLegal = isLegal;
            Reason = reason;
            FirstObjectBallContact = firstObjectBallContact;
        }

        public bool IsLegal { get; }

        public FirstContactEvaluationReason Reason { get; }

        public BallId? FirstObjectBallContact { get; }
    }
}
