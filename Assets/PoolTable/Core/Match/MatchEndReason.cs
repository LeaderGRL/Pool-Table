using System;

namespace PoolTable.Core.Match
{
    [Flags]
    public enum MatchEndReason
    {
        None = 0,
        EightBallLegallyPocketed = 1 << 0,
        EightBallPocketedWithFoul = 1 << 1,
        EightBallPocketedBeforeGroupCleared = 1 << 2,
        EightBallPocketedInUncalledPocket = 1 << 3,
        EightBallDrivenOffTable = 1 << 4,
    }
}
