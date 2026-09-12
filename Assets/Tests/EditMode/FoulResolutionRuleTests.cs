using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class FoulResolutionRuleTests
    {
        [Test]
        public void Evaluate_CleanOpenTableShot_HasNoFoul()
        {
            var result = Evaluate(
                OpenTableState(),
                firstContact: 1,
                pocketed: Array.Empty<int>(),
                rails: new[] { 1 },
                table: StandardTable());

            Assert.That(result.HasFoul, Is.False);
            Assert.That(result.IsClean, Is.True);
            Assert.That(result.Fouls, Is.EqualTo(ShotFoul.None));
        }

        [Test]
        public void Evaluate_CueBallPocketed_ReportsScratch()
        {
            var result = Evaluate(
                OpenTableState(),
                firstContact: 1,
                pocketed: new[] { BallId.CueBallNumber },
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.Has(ShotFoul.CueBallScratch), Is.True);
            Assert.That(result.Fouls, Is.EqualTo(ShotFoul.CueBallScratch));
        }

        [Test]
        public void Evaluate_NoObjectBallContact_ReportsSingleContactFoul()
        {
            var result = Evaluate(
                OpenTableState(),
                firstContact: null,
                pocketed: Array.Empty<int>(),
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.Has(ShotFoul.NoObjectBallContact), Is.True);
            Assert.That(result.Has(ShotFoul.NoRailOrPocketAfterObjectBallContact), Is.False);
            Assert.That(result.Fouls, Is.EqualTo(ShotFoul.NoObjectBallContact));
        }

        [Test]
        public void Evaluate_WrongAssignedGroupFirst_ReportsIllegalFirstContact()
        {
            var result = Evaluate(
                AssignedState(BallGroup.Solids),
                firstContact: 9,
                pocketed: Array.Empty<int>(),
                rails: new[] { 9 },
                table: StandardTable());

            Assert.That(result.Has(ShotFoul.IllegalFirstContact), Is.True);
            Assert.That(result.Has(ShotFoul.NoRailOrPocketAfterObjectBallContact), Is.False);
        }

        [Test]
        public void Evaluate_NoRailOrPocketAfterObjectContact_ReportsRailFoul()
        {
            var result = Evaluate(
                OpenTableState(),
                firstContact: 1,
                pocketed: Array.Empty<int>(),
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.Fouls, Is.EqualTo(ShotFoul.NoRailOrPocketAfterObjectBallContact));
        }

        [Test]
        public void Evaluate_WrongFirstContactAndNoRail_ReportsBothFouls()
        {
            var result = Evaluate(
                AssignedState(BallGroup.Solids),
                firstContact: 9,
                pocketed: Array.Empty<int>(),
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.Has(ShotFoul.IllegalFirstContact), Is.True);
            Assert.That(result.Has(ShotFoul.NoRailOrPocketAfterObjectBallContact), Is.True);
            Assert.That(result.Fouls, Is.EqualTo(
                ShotFoul.IllegalFirstContact | ShotFoul.NoRailOrPocketAfterObjectBallContact));
        }

        [Test]
        public void Evaluate_ScratchAndWrongFirstContact_ReportsBothWithoutRailFoul()
        {
            var result = Evaluate(
                AssignedState(BallGroup.Solids),
                firstContact: 9,
                pocketed: new[] { BallId.CueBallNumber },
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.Has(ShotFoul.CueBallScratch), Is.True);
            Assert.That(result.Has(ShotFoul.IllegalFirstContact), Is.True);
            Assert.That(result.Has(ShotFoul.NoRailOrPocketAfterObjectBallContact), Is.False);
        }

        [Test]
        public void Evaluate_BreakScratch_ReportsScratchWithoutApplyingNormalShotRules()
        {
            var result = Evaluate(
                MatchState.CreateInitial(),
                firstContact: null,
                pocketed: new[] { BallId.CueBallNumber },
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.Fouls, Is.EqualTo(ShotFoul.CueBallScratch));
        }

        [Test]
        public void Evaluate_BreakWithoutScratch_HasNoFoulInThisResolver()
        {
            var result = Evaluate(
                MatchState.CreateInitial(),
                firstContact: 1,
                pocketed: Array.Empty<int>(),
                rails: Array.Empty<int>(),
                table: StandardTable());

            Assert.That(result.IsClean, Is.True);
        }

        [Test]
        public void Evaluate_FinishedMatch_Throws()
        {
            var state = MatchState.CreateInitial().WithPhase(MatchPhase.Finished);
            var facts = Facts(1, Array.Empty<int>(), new[] { 1 });

            Assert.Throws<InvalidOperationException>(() =>
                FoulResolutionRule.Evaluate(state, facts, StandardTable()));
        }

        [Test]
        public void Evaluate_NullArguments_Throw()
        {
            var state = OpenTableState();
            var facts = Facts(1, Array.Empty<int>(), new[] { 1 });
            var table = StandardTable();

            Assert.Throws<ArgumentNullException>(() => FoulResolutionRule.Evaluate(null, facts, table));
            Assert.Throws<ArgumentNullException>(() => FoulResolutionRule.Evaluate(state, null, table));
            Assert.Throws<ArgumentNullException>(() => FoulResolutionRule.Evaluate(state, facts, null));
        }

        private static FoulResolution Evaluate(
            MatchState state,
            int? firstContact,
            int[] pocketed,
            int[] rails,
            ObjectBallTableSnapshot table)
        {
            return FoulResolutionRule.Evaluate(state, Facts(firstContact, pocketed, rails), table);
        }

        private static ShotFacts Facts(int? firstContact, int[] pocketed, int[] rails)
        {
            BallId? first = firstContact.HasValue ? new BallId(firstContact.Value) : (BallId?)null;
            return new ShotFacts(
                first,
                Array.ConvertAll(pocketed, value => new BallId(value)),
                Array.ConvertAll(rails, value => new BallId(value)));
        }

        private static MatchState OpenTableState()
        {
            return OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
        }

        private static MatchState AssignedState(BallGroup currentPlayerGroup)
        {
            var open = OpenTableState();
            var assignmentBall = currentPlayerGroup == BallGroup.Solids ? new BallId(1) : new BallId(9);
            return PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                open,
                MatchPlayerId.PlayerOne,
                assignmentBall);
        }

        private static ObjectBallTableSnapshot StandardTable()
        {
            var balls = new BallId[15];
            for (var number = 1; number <= 15; number++)
            {
                balls[number - 1] = new BallId(number);
            }

            return new ObjectBallTableSnapshot(balls);
        }
    }
}
