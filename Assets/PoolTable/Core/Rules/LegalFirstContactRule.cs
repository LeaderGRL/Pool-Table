using System;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Shots;

namespace PoolTable.Core.Rules
{
    public static class LegalFirstContactRule
    {
        public static FirstContactEvaluation Evaluate(
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
                throw new InvalidOperationException("First-contact validation cannot run after the match has finished.");
            }

            if (!facts.FirstObjectBallContact.HasValue)
            {
                return new FirstContactEvaluation(
                    false,
                    FirstContactEvaluationReason.NoObjectBallContact,
                    null);
            }

            var firstContact = facts.FirstObjectBallContact.Value;
            if (!tableBeforeShot.Contains(firstContact))
            {
                throw new ArgumentException(
                    $"First-contact ball {firstContact.Number} must exist in the pre-shot table snapshot.",
                    nameof(facts));
            }

            if (state.Phase == MatchPhase.Break)
            {
                return Legal(firstContact, FirstContactEvaluationReason.BreakObjectBallContact);
            }

            if (state.Phase == MatchPhase.OpenTable)
            {
                return EvaluateOpenTable(firstContact);
            }

            return EvaluateAssignedGroups(state, firstContact, tableBeforeShot);
        }

        private static FirstContactEvaluation EvaluateOpenTable(BallId firstContact)
        {
            if (!firstContact.IsEightBall)
            {
                return Legal(firstContact, FirstContactEvaluationReason.OpenTableGroupedBallContact);
            }

            return Illegal(firstContact, FirstContactEvaluationReason.OpenTableEightBallTooEarly);
        }

        private static FirstContactEvaluation EvaluateAssignedGroups(
            MatchState state,
            BallId firstContact,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            var shooterGroup = state.GetPlayer(state.CurrentPlayer).Group;
            var shooterGroupRemains = tableBeforeShot.HasRemainingBalls(shooterGroup);

            if (firstContact.IsEightBall)
            {
                return shooterGroupRemains
                    ? Illegal(firstContact, FirstContactEvaluationReason.EightBallBeforeAssignedGroupCleared)
                    : Legal(firstContact, FirstContactEvaluationReason.EightBallAfterAssignedGroupCleared);
            }

            if (shooterGroupRemains && firstContact.Group == shooterGroup)
            {
                return Legal(firstContact, FirstContactEvaluationReason.AssignedGroupBallContact);
            }

            return Illegal(firstContact, FirstContactEvaluationReason.WrongAssignedGroupFirstContact);
        }

        private static FirstContactEvaluation Legal(BallId ball, FirstContactEvaluationReason reason)
        {
            return new FirstContactEvaluation(true, reason, ball);
        }

        private static FirstContactEvaluation Illegal(BallId ball, FirstContactEvaluationReason reason)
        {
            return new FirstContactEvaluation(false, reason, ball);
        }
    }
}
