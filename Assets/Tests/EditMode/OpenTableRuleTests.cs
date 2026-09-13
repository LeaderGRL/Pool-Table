using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;

namespace PoolTable.Tests.EditMode
{
    public sealed class OpenTableRuleTests
    {
        [TestCase(MatchPlayerId.PlayerOne)]
        [TestCase(MatchPlayerId.PlayerTwo)]
        public void EnterAfterBreak_OpensTableAndPreservesCurrentPlayer(MatchPlayerId currentPlayer)
        {
            var breakState = MatchState.CreateInitial(currentPlayer);

            var openTable = OpenTableRule.EnterAfterBreak(breakState);

            Assert.That(openTable.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(openTable.IsTableOpen, Is.True);
            Assert.That(openTable.CurrentPlayer, Is.EqualTo(currentPlayer));
            Assert.That(openTable.PlayerOne, Is.EqualTo(breakState.PlayerOne));
            Assert.That(openTable.PlayerTwo, Is.EqualTo(breakState.PlayerTwo));
            Assert.That(openTable.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(openTable.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
        }

        [Test]
        public void EnterAfterBreak_DoesNotMutateBreakState()
        {
            var breakState = MatchState.CreateInitial();

            var openTable = OpenTableRule.EnterAfterBreak(breakState);

            Assert.That(breakState.Phase, Is.EqualTo(MatchPhase.Break));
            Assert.That(breakState.IsTableOpen, Is.False);
            Assert.That(openTable, Is.Not.SameAs(breakState));
        }

        [Test]
        public void EnterAfterBreak_RejectsNullState()
        {
            Assert.Throws<ArgumentNullException>(() => OpenTableRule.EnterAfterBreak(null));
        }

        [Test]
        public void EnterAfterBreak_RejectsAlreadyOpenTable()
        {
            var openTable = MatchState.CreateInitial().WithPhase(MatchPhase.OpenTable);

            Assert.Throws<InvalidOperationException>(() => OpenTableRule.EnterAfterBreak(openTable));
        }

        [Test]
        public void EnterAfterBreak_RejectsAssignedGroupsPhase()
        {
            var groupsAssigned = MatchState.CreateInitial()
                .WithPhase(MatchPhase.OpenTable)
                .WithAssignedGroups(MatchPlayerId.PlayerOne);

            Assert.Throws<InvalidOperationException>(() => OpenTableRule.EnterAfterBreak(groupsAssigned));
        }

        [Test]
        public void EnterAfterBreak_RejectsFinishedPhase()
        {
            var finished = MatchState.CreateInitial().WithResult(new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallLegallyPocketed));

            Assert.Throws<InvalidOperationException>(() => OpenTableRule.EnterAfterBreak(finished));
        }
    }
}
