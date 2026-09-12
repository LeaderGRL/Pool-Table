namespace PoolTable.Core.Rules
{
    public enum FirstContactEvaluationReason
    {
        NoObjectBallContact = 0,
        BreakObjectBallContact = 1,
        OpenTableGroupedBallContact = 2,
        OpenTableEightBallTooEarly = 4,
        AssignedGroupBallContact = 5,
        EightBallAfterAssignedGroupCleared = 6,
        WrongAssignedGroupFirstContact = 7,
        EightBallBeforeAssignedGroupCleared = 8,
    }
}
