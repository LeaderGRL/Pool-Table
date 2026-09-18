using System;
using PoolTable.Core.Match;

namespace PoolTable.Core.Rules
{
    public static class DeuxCoupsRule
    {
        public static MatchState Grant(MatchState state)
        {
            ValidateActivePlay(state);
            return state.WithDeuxCoups(DeuxCoupsState.TwoRemaining);
        }

        public static MatchState Consume(MatchState state)
        {
            ValidateActivePlay(state);

            switch (state.DeuxCoups)
            {
                case DeuxCoupsState.TwoRemaining:
                    return state.WithDeuxCoups(DeuxCoupsState.OneRemaining);
                case DeuxCoupsState.OneRemaining:
                    return state.WithDeuxCoups(DeuxCoupsState.Inactive);
                default:
                    throw new InvalidOperationException("Deux coups cannot be consumed while inactive.");
            }
        }

        private static void ValidateActivePlay(MatchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Phase == MatchPhase.Break || state.Phase == MatchPhase.Finished)
            {
                throw new InvalidOperationException("Deux coups is only valid during active post-break play.");
            }
        }
    }
}
