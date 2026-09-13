using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class RailOrPocketRequirementRuleTests
    {
        [TestCase(MatchPhase.OpenTable)]
        [TestCase(MatchPhase.GroupsAssigned)]
        public void Evaluate_ObjectBallPocketed_SatisfiesRequirement(MatchPhase phase)
        {
            var facts = CreateFacts(1, new[] { 3 }, Array.Empty<int>());

            var result = RailOrPocketRequirementRule.Evaluate(StateFor(phase), facts);

            Assert.That(result.IsSatisfied, Is.True);
            Assert.That(result.Reason, Is.EqualTo(RailOrPocketRequirementReason.BallPocketed));
            Assert.That(result.PocketedBallCount, Is.EqualTo(1));
            Assert.That(result.RailContactBallCount, Is.Zero);
        }

        [Test]
        public void Evaluate_CueBallPocketedWithoutObjectContact_SatisfiesOnlyThisRequirement()
        {
            var facts = CreateFacts(
                null,
                new[] { BallId.CueBallNumber },
                Array.Empty<int>());

            var result = RailOrPocketRequirementRule.Evaluate(OpenTableState(), facts);

            Assert.That(result.IsSatisfied, Is.True);
            Assert.That(result.Reason, Is.EqualTo(RailOrPocketRequirementReason.BallPocketed));
            Assert.That(result.PocketedBallCount, Is.EqualTo(1));
        }

        [TestCase(BallId.CueBallNumber)]
        [TestCase(1)]
        [TestCase(BallId.EightBallNumber)]
        public void Evaluate_AnyBallReachesRailAfterObjectContact_SatisfiesRequirement(int railBallNumber)
        {
            var facts = CreateFacts(1, Array.Empty<int>(), new[] { railBallNumber });

            var result = RailOrPocketRequirementRule.Evaluate(OpenTableState(), facts);

            Assert.That(result.IsSatisfied, Is.True);
            Assert.That(result.Reason, Is.EqualTo(RailOrPocketRequirementReason.RailReachedAfterObjectBallContact));
            Assert.That(result.PocketedBallCount, Is.Zero);
            Assert.That(result.RailContactBallCount, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_NoPocketOrRailAfterObjectContact_FailsRequirement()
        {
            var facts = CreateFacts(1, Array.Empty<int>(), Array.Empty<int>());

            var result = RailOrPocketRequirementRule.Evaluate(OpenTableState(), facts);

            Assert.That(result.IsSatisfied, Is.False);
            Assert.That(result.Reason, Is.EqualTo(RailOrPocketRequirementReason.NoPocketOrRailAfterObjectBallContact));
            Assert.That(result.PocketedBallCount, Is.Zero);
            Assert.That(result.RailContactBallCount, Is.Zero);
        }

        [Test]
        public void Evaluate_NoPocketAndNoObjectContact_FailsRequirement()
        {
            var facts = CreateFacts(null, Array.Empty<int>(), Array.Empty<int>());

            var result = RailOrPocketRequirementRule.Evaluate(OpenTableState(), facts);

            Assert.That(result.IsSatisfied, Is.False);
            Assert.That(result.Reason, Is.EqualTo(RailOrPocketRequirementReason.NoObjectBallContact));
        }

        [Test]
        public void Evaluate_PocketTakesPrecedenceOverRailRequirement()
        {
            var facts = CreateFacts(1, new[] { 4 }, new[] { 0, 2, 6 });

            var result = RailOrPocketRequirementRule.Evaluate(OpenTableState(), facts);

            Assert.That(result.IsSatisfied, Is.True);
            Assert.That(result.Reason, Is.EqualTo(RailOrPocketRequirementReason.BallPocketed));
            Assert.That(result.PocketedBallCount, Is.EqualTo(1));
            Assert.That(result.RailContactBallCount, Is.EqualTo(3));
        }

        [Test]
        public void Evaluate_BreakPhase_RejectsNormalShotRule()
        {
            var facts = CreateFacts(1, Array.Empty<int>(), new[] { 2 });

            Assert.Throws<InvalidOperationException>(() =>
                RailOrPocketRequirementRule.Evaluate(MatchState.CreateInitial(), facts));
        }

        [Test]
        public void Evaluate_FinishedPhase_RejectsEvaluation()
        {
            var state = MatchState.CreateInitial().WithPhase(MatchPhase.Finished);
            var facts = CreateFacts(1, Array.Empty<int>(), new[] { 2 });

            Assert.Throws<InvalidOperationException>(() =>
                RailOrPocketRequirementRule.Evaluate(state, facts));
        }

        [Test]
        public void Evaluate_NullState_Throws()
        {
            var facts = CreateFacts(1, Array.Empty<int>(), new[] { 2 });

            Assert.Throws<ArgumentNullException>(() =>
                RailOrPocketRequirementRule.Evaluate(null, facts));
        }

        [Test]
        public void Evaluate_NullFacts_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                RailOrPocketRequirementRule.Evaluate(OpenTableState(), null));
        }

        private static MatchState StateFor(MatchPhase phase)
        {
            if (phase == MatchPhase.OpenTable)
            {
                return OpenTableState();
            }

            var open = OpenTableState();
            return PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                open,
                MatchPlayerId.PlayerOne,
                new BallId(1));
        }

        private static MatchState OpenTableState()
        {
            return OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
        }

        private static ShotFacts CreateFacts(
            int? firstObjectBallContact,
            int[] pocketedBallNumbers,
            int[] railContactBallNumbers)
        {
            BallId? firstContact = firstObjectBallContact.HasValue
                ? new BallId(firstObjectBallContact.Value)
                : (BallId?)null;

            var pocketed = Array.ConvertAll(
                pocketedBallNumbers,
                number => new PocketedBall(new BallId(number), new PocketId(1)));
            var rails = Array.ConvertAll(railContactBallNumbers, number => new BallId(number));

            return new ShotFacts(firstContact, pocketed, rails);
        }
    }
}
