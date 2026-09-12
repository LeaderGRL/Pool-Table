using System;
using PoolTable.Core.Match;

namespace PoolTable.Core.Rules
{
    public static class OpenTableRule
    {
        public static MatchState EnterAfterBreak(MatchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Phase != MatchPhase.Break)
            {
                throw new InvalidOperationException("Open table can only begin when leaving the break phase.");
            }

            return state.WithPhase(MatchPhase.OpenTable);
        }
    }
}
