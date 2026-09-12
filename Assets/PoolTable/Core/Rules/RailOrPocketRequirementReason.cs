namespace PoolTable.Core.Rules
{
    public enum RailOrPocketRequirementReason
    {
        NoObjectBallContact = 0,
        BallPocketed = 1,
        RailReachedAfterObjectBallContact = 2,
        NoPocketOrRailAfterObjectBallContact = 3,
    }
}
