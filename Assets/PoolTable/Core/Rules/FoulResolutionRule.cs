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

            ValidateDrivenOffTableFacts(facts, tableBeforeShot);

            var fouls = facts.CueBallPocketed
                ? ShotFoul.CueBallScratch
                : ShotFoul.None;

            if (facts.CueBallDrivenOffTable)
            {
                fouls |= ShotFoul.CueBallOffTable;
            }

            if (HasObjectBallDrivenOffTable(facts))
            {
                fouls |= ShotFoul.ObjectBallOffTable;
            }

            if (state.Phase == MatchPhase.Break)
            {
                if (!facts.HasObjectBallContact)
                {
                    fouls |= ShotFoul.NoObjectBallContact;
                }

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

        private static bool HasObjectBallDrivenOffTable(ShotFacts facts)
        {
            for (var index = 0; index < facts.BallsDrivenOffTable.Count; index++)
            {
                if (!facts.BallsDrivenOffTable[index].IsCueBall)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateDrivenOffTableFacts(
            ShotFacts facts,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            for (var index = 0; index < facts.BallsDrivenOffTable.Count; index++)
            {
                var ball = facts.BallsDrivenOffTable[index];
                if (!ball.IsCueBall && !tableBeforeShot.Contains(ball))
                {
                    throw new ArgumentException(
                        $"Off-table ball {ball.Number} must exist in the pre-shot table snapshot.",
                        nameof(facts));
                }
            }
        }
    }
}
