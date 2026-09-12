using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;

namespace PoolTable.Tests.EditMode
{
    public sealed class PlayerGroupAssignmentRuleTests
    {
        [TestCase(MatchPlayerId.PlayerOne, 1, BallGroup.Solids, BallGroup.Stripes)]
        [TestCase(MatchPlayerId.PlayerOne, 9, BallGroup.Stripes, BallGroup.Solids)]
        [TestCase(MatchPlayerId.PlayerTwo, 1, BallGroup.Stripes, BallGroup.Solids)]
        [TestCase(MatchPlayerId.PlayerTwo, 9, BallGroup.Solids, BallGroup.Stripes)]
        public void AssignFromLegallyPocketedBall_AssignsComplementaryGroups(
            MatchPlayerId actingPlayer,
            int pocketedBallNumber,
            BallGroup expectedPlayerOneGroup,
            BallGroup expectedPlayerTwoGroup)
        {
            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(actingPlayer));

            var assigned = PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                openTable,
                actingPlayer,
                new BallId(pocketedBallNumber));

            Assert.That(assigned.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(assigned.IsTableOpen, Is.False);
            Assert.That(assigned.CurrentPlayer, Is.EqualTo(actingPlayer));
            Assert.That(assigned.PlayerOne.Group, Is.EqualTo(expectedPlayerOneGroup));
            Assert.That(assigned.PlayerTwo.Group, Is.EqualTo(expectedPlayerTwoGroup));
        }

        [Test]
        public void AssignFromLegallyPocketedBall_DoesNotMutateOpenTableState()
        {
            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());

            var assigned = PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                openTable,
                MatchPlayerId.PlayerOne,
                new BallId(3));

            Assert.That(openTable.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(openTable.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(openTable.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
            Assert.That(assigned, Is.Not.SameAs(openTable));
        }

        [Test]
        public void AssignFromLegallyPocketedBall_RejectsNullState()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                    null,
                    MatchPlayerId.PlayerOne,
                    new BallId(1)));
        }

        [Test]
        public void AssignFromLegallyPocketedBall_RejectsBreakPhase()
        {
            Assert.Throws<InvalidOperationException>(() =>
                PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                    MatchState.CreateInitial(),
                    MatchPlayerId.PlayerOne,
                    new BallId(1)));
        }

        [Test]
        public void AssignFromLegallyPocketedBall_RejectsAlreadyAssignedGroups()
        {
            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var assigned = PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                openTable,
                MatchPlayerId.PlayerOne,
                new BallId(1));

            Assert.Throws<InvalidOperationException>(() =>
                PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                    assigned,
                    MatchPlayerId.PlayerOne,
                    new BallId(2)));
        }

        [Test]
        public void AssignFromLegallyPocketedBall_RejectsNonCurrentPlayer()
        {
            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(MatchPlayerId.PlayerOne));

            Assert.Throws<InvalidOperationException>(() =>
                PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                    openTable,
                    MatchPlayerId.PlayerTwo,
                    new BallId(1)));
        }

        [TestCase(BallId.CueBallNumber)]
        [TestCase(BallId.EightBallNumber)]
        public void AssignFromLegallyPocketedBall_RejectsBallsWithoutGroup(int ballNumber)
        {
            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());

            Assert.Throws<ArgumentException>(() =>
                PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                    openTable,
                    MatchPlayerId.PlayerOne,
                    new BallId(ballNumber)));
        }

        [TestCase((MatchPlayerId)0)]
        [TestCase((MatchPlayerId)3)]
        public void AssignFromLegallyPocketedBall_RejectsUnknownPlayer(MatchPlayerId player)
        {
            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                    openTable,
                    player,
                    new BallId(1)));
        }
    }
}
