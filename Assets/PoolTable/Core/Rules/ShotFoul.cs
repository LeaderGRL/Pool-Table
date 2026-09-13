using System;

namespace PoolTable.Core.Rules
{
    [Flags]
    public enum ShotFoul
    {
        None = 0,
        CueBallScratch = 1 << 0,
        NoObjectBallContact = 1 << 1,
        IllegalFirstContact = 1 << 2,
        NoRailOrPocketAfterObjectBallContact = 1 << 3,
        CueBallOffTable = 1 << 4,
        ObjectBallOffTable = 1 << 5,
    }
}
