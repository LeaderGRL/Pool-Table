using System;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Shots;

namespace PoolTable.Core.Rules
{
    public static class FoulResolutionRule
    {
        public static FoulResolution Evaluate(
            MatchState state,
            ShotFacts facts,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (facts == null)
            {
                throw new ArgumentNullException(nameof(facts));
            }

            if (tableBeforeShot == null)
            {
                throw new ArgumentNullException(nameof(tableBeforeShot));
            }

            if (state.Phase == MatchPhase.Finished)
            {
                throw new InvalidOperationException("Foul resolution cannot run after the match has finished.");
            }

            var fouls = facts.CueBallPocketed
                ? ShotFoul.CueBallScratch
                : ShotFoul.None;

            if (state.Phase == MatchPhase.Break)
            {
                return new FoulResolution(fouls);
            }

            var firstContact = LegalFirstContactRule.Evaluate(state, facts, tableBeforeShot);
            var railOrPocket = RailOrPocketRequirementRule.Evaluate(state, facts);

            if (!firstContact.IsLegal)
            {
                fouls |= firstContact.Reason == FirstContactEvaluationReason.NoObjectBallContact
                    ? ShotFoul.NoObjectBallContact
                    : ShotFoul.IllegalFirstContact;
            }

            if (facts.HasObjectBallContact && !railOrPocket.IsSatisfied)
            {
                fouls |= ShotFoul.NoRailOrPocketAfterObjectBallContact;
            }

            return new FoulResolution(fouls);
        }
    }
}
