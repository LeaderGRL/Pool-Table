using System;
using System.Collections.Generic;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class LegalFirstContactRuleTests
    {
        [TestCase(1)]
        [TestCase(9)]
        [TestCase(8)]
        public void Evaluate_BreakAllowsAnyObjectBallFirst(int ballNumber)
        {
            var evaluation = Evaluate(MatchState.CreateInitial(), ballNumber, StandardTable());

            Assert.That(evaluation.IsLegal, Is.True);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.BreakObjectBallContact));
        }

        [TestCase(1)]
        [TestCase(9)]
        public void Evaluate_OpenTableAllowsGroupedBallFirst(int ballNumber)
        {
            var evaluation = Evaluate(OpenTableState(), ballNumber, StandardTable());

            Assert.That(evaluation.IsLegal, Is.True);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.OpenTableGroupedBallContact));
        }

        [Test]
        public void Evaluate_OpenTableRejectsEightBallWhileBothGroupsRemain()
        {
            var evaluation = Evaluate(OpenTableState(), 8, StandardTable());

            Assert.That(evaluation.IsLegal, Is.False);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.OpenTableEightBallTooEarly));
        }

        [TestCase(BallGroup.Solids)]
        [TestCase(BallGroup.Stripes)]
        public void Evaluate_OpenTableRejectsEightBallEvenWhenAGroupIsCleared(BallGroup clearedGroup)
        {
            var table = clearedGroup == BallGroup.Solids
                ? Balls(8, 9, 10, 11, 12, 13, 14, 15)
                : Balls(1, 2, 3, 4, 5, 6, 7, 8);

            var evaluation = Evaluate(OpenTableState(), 8, table);

            Assert.That(evaluation.IsLegal, Is.False);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.OpenTableEightBallTooEarly));
        }

        [Test]
        public void Evaluate_AssignedGroupsAllowsShooterGroupFirst()
        {
            var evaluation = Evaluate(AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids), 3, StandardTable());

            Assert.That(evaluation.IsLegal, Is.True);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.AssignedGroupBallContact));
        }

        [Test]
        public void Evaluate_AssignedGroupsRejectsOpponentGroupFirst()
        {
            var evaluation = Evaluate(AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids), 11, StandardTable());

            Assert.That(evaluation.IsLegal, Is.False);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.WrongAssignedGroupFirstContact));
        }

        [Test]
        public void Evaluate_AssignedGroupsRejectsEightBallBeforeShooterGroupIsCleared()
        {
            var evaluation = Evaluate(AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids), 8, StandardTable());

            Assert.That(evaluation.IsLegal, Is.False);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.EightBallBeforeAssignedGroupCleared));
        }

        [Test]
        public void Evaluate_AssignedGroupsRequiresEightBallAfterShooterGroupIsCleared()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(8, 9, 10, 11, 12, 13, 14, 15);

            var eightBallEvaluation = Evaluate(state, 8, table);
            var stripeEvaluation = Evaluate(state, 11, table);

            Assert.That(eightBallEvaluation.IsLegal, Is.True);
            Assert.That(eightBallEvaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.EightBallAfterAssignedGroupCleared));
            Assert.That(stripeEvaluation.IsLegal, Is.False);
            Assert.That(stripeEvaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.WrongAssignedGroupFirstContact));
        }

        [Test]
        public void Evaluate_UsesCurrentPlayersAssignedGroup()
        {
            var state = AssignedState(MatchPlayerId.PlayerTwo, BallGroup.Stripes);

            Assert.That(Evaluate(state, 12, StandardTable()).IsLegal, Is.True);
            Assert.That(Evaluate(state, 2, StandardTable()).IsLegal, Is.False);
        }

        [Test]
        public void Evaluate_NoFirstContactIsIllegal()
        {
            var facts = new ShotFacts(null, Array.Empty<BallId>(), Array.Empty<BallId>());

            var evaluation = LegalFirstContactRule.Evaluate(OpenTableState(), facts, StandardTable());

            Assert.That(evaluation.IsLegal, Is.False);
            Assert.That(evaluation.Reason, Is.EqualTo(FirstContactEvaluationReason.NoObjectBallContact));
            Assert.That(evaluation.FirstObjectBallContact, Is.Null);
        }

        [Test]
        public void Evaluate_RejectsContactMissingFromPreShotTableSnapshot()
        {
            var facts = FactsWithFirstContact(7);

            Assert.Throws<ArgumentException>(() => LegalFirstContactRule.Evaluate(
                OpenTableState(),
                facts,
                Balls(1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15)));
        }

        [Test]
        public void Evaluate_RejectsFinishedMatch()
        {
            var state = MatchState.CreateInitial().WithPhase(MatchPhase.Finished);

            Assert.Throws<InvalidOperationException>(() => Evaluate(state, 1, StandardTable()));
        }

        [Test]
        public void ObjectBallTableSnapshot_DefensivelyCopiesSource()
        {
            var source = BallsList(1, 8, 9);
            var snapshot = new ObjectBallTableSnapshot(source);

            source.Clear();

            Assert.That(snapshot.Balls, Has.Count.EqualTo(3));
            Assert.That(snapshot.Contains(new BallId(1)), Is.True);
            Assert.That(snapshot.HasRemainingBalls(BallGroup.Solids), Is.True);
            Assert.That(snapshot.HasRemainingBalls(BallGroup.Stripes), Is.True);
        }

        [Test]
        public void ObjectBallTableSnapshot_RejectsCueBall()
        {
            Assert.Throws<ArgumentException>(() => new ObjectBallTableSnapshot(BallsList(0, 1, 8)));
        }

        [Test]
        public void ObjectBallTableSnapshot_RejectsDuplicateObjectBall()
        {
            Assert.Throws<ArgumentException>(() => new ObjectBallTableSnapshot(BallsList(1, 1, 8)));
        }

        private static FirstContactEvaluation Evaluate(
            MatchState state,
            int firstContactNumber,
            ObjectBallTableSnapshot table)
        {
            return LegalFirstContactRule.Evaluate(state, FactsWithFirstContact(firstContactNumber), table);
        }

        private static ShotFacts FactsWithFirstContact(int ballNumber)
        {
            return new ShotFacts(new BallId(ballNumber), Array.Empty<BallId>(), Array.Empty<BallId>());
        }

        private static MatchState OpenTableState()
        {
            return OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
        }

        private static MatchState AssignedState(MatchPlayerId currentPlayer, BallGroup currentPlayerGroup)
        {
            var open = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(currentPlayer));
            var legallyPocketedBall = currentPlayerGroup == BallGroup.Solids ? new BallId(1) : new BallId(9);
            return PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(open, currentPlayer, legallyPocketedBall);
        }

        private static ObjectBallTableSnapshot StandardTable()
        {
            return Balls(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        }

        private static ObjectBallTableSnapshot Balls(params int[] ballNumbers)
        {
            return new ObjectBallTableSnapshot(BallsList(ballNumbers));
        }

        private static List<BallId> BallsList(params int[] ballNumbers)
        {
            var balls = new List<BallId>(ballNumbers.Length);
            for (var index = 0; index < ballNumbers.Length; index++)
            {
                balls.Add(new BallId(ballNumbers[index]));
            }

            return balls;
        }
    }
}
