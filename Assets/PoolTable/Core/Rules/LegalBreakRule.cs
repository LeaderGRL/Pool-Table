using System;
using PoolTable.Core.Shots;

namespace PoolTable.Core.Rules
{
    public static class LegalBreakRule
    {
        public const int MinimumObjectBallRailContacts = 4;

        public static BreakEvaluation Evaluate(ShotFacts facts)
        {
            if (facts == null)
            {
                throw new ArgumentNullException(nameof(facts));
            }

            var hasPocketedObjectBall = HasPocketedObjectBall(facts);
            var objectBallRailContactCount = CountObjectBallRailContacts(facts);

            if (hasPocketedObjectBall)
            {
                return new BreakEvaluation(
                    true,
                    BreakEvaluationReason.ObjectBallPocketed,
                    true,
                    objectBallRailContactCount);
            }

            if (objectBallRailContactCount >= MinimumObjectBallRailContacts)
            {
                return new BreakEvaluation(
                    true,
                    BreakEvaluationReason.FourOrMoreObjectBallsReachedRails,
                    false,
                    objectBallRailContactCount);
            }

            return new BreakEvaluation(
                false,
                BreakEvaluationReason.InsufficientObjectBallsReachedRails,
                false,
                objectBallRailContactCount);
        }

        private static bool HasPocketedObjectBall(ShotFacts facts)
        {
            for (var index = 0; index < facts.PocketedBalls.Count; index++)
            {
                if (!facts.PocketedBalls[index].IsCueBall)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountObjectBallRailContacts(ShotFacts facts)
        {
            var count = 0;

            for (var index = 0; index < facts.RailContactBallsAfterFirstObjectBallContact.Count; index++)
            {
                if (!facts.RailContactBallsAfterFirstObjectBallContact[index].IsCueBall)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
