using System;
using PoolTable.Core.Match;
using PoolTable.Core.Shots;

namespace PoolTable.Core.Rules
{
    public static class RailOrPocketRequirementRule
    {
        public static RailOrPocketEvaluation Evaluate(MatchState state, ShotFacts facts)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (facts == null)
            {
                throw new ArgumentNullException(nameof(facts));
            }

            if (state.Phase == MatchPhase.Break)
            {
                throw new InvalidOperationException(
                    "Normal-shot rail or pocket validation cannot run during the break. Use LegalBreakRule instead.");
            }

            if (state.Phase == MatchPhase.Finished)
            {
                throw new InvalidOperationException(
                    "Rail or pocket validation cannot run after the match has finished.");
            }

            var pocketedBallCount = facts.PocketedBalls.Count;
            var railContactBallCount = facts.RailContactBallsAfterFirstObjectBallContact.Count;

            if (pocketedBallCount > 0)
            {
                return new RailOrPocketEvaluation(
                    true,
                    RailOrPocketRequirementReason.BallPocketed,
                    pocketedBallCount,
                    railContactBallCount);
            }

            if (!facts.HasObjectBallContact)
            {
                return new RailOrPocketEvaluation(
                    false,
                    RailOrPocketRequirementReason.NoObjectBallContact,
                    0,
                    0);
            }

            if (railContactBallCount > 0)
            {
                return new RailOrPocketEvaluation(
                    true,
                    RailOrPocketRequirementReason.RailReachedAfterObjectBallContact,
                    0,
                    railContactBallCount);
            }

            return new RailOrPocketEvaluation(
                false,
                RailOrPocketRequirementReason.NoPocketOrRailAfterObjectBallContact,
                0,
                0);
        }
    }
}
