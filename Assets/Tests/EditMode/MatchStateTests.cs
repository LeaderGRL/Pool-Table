using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;

namespace PoolTable.Tests.EditMode
{
    public sealed class MatchStateTests
    {
        [Test]
        public void CreateInitial_CreatesOpenPlayersAtBreakPhase()
        {
            var state = MatchState.CreateInitial();

            Assert.That(state.PlayerOne, Is.EqualTo(new MatchPlayerState(MatchPlayerId.PlayerOne)));
            Assert.That(state.PlayerTwo, Is.EqualTo(new MatchPlayerState(MatchPlayerId.PlayerTwo)));
            Assert.That(state.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Break));
            Assert.That(state.IsFinished, Is.False);
            Assert.That(state.Result, Is.Null);
            Assert.That(state.PlayerOne.HasAssignedGroup, Is.False);
            Assert.That(state.PlayerTwo.HasAssignedGroup, Is.False);
        }

        [Test]
        public void CreateInitial_AcceptsPlayerTwoAsStartingPlayer()
        {
            var state = MatchState.CreateInitial(MatchPlayerId.PlayerTwo);

            Assert.That(state.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
        }

        [Test]
        public void GetPlayer_ReturnsStateForRequestedPlayer()
        {
            var state = MatchState.CreateInitial();

            Assert.That(state.GetPlayer(MatchPlayerId.PlayerOne), Is.EqualTo(state.PlayerOne));
            Assert.That(state.GetPlayer(MatchPlayerId.PlayerTwo), Is.EqualTo(state.PlayerTwo));
        }

        [Test]
        public void AdvanceTurn_ReturnsNewStateWithoutMutatingOriginal()
        {
            var initial = MatchState.CreateInitial();

            var next = initial.AdvanceTurn();

            Assert.That(initial.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(next.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(next, Is.Not.SameAs(initial));
        }

        [TestCase(MatchPlayerId.PlayerOne, BallGroup.Solids, BallGroup.Stripes)]
        [TestCase(MatchPlayerId.PlayerTwo, BallGroup.Stripes, BallGroup.Solids)]
        public void WithAssignedGroups_AssignsComplementaryGroupsAndReturnsNewState(
            MatchPlayerId solidsPlayer,
            BallGroup expectedPlayerOneGroup,
            BallGroup expectedPlayerTwoGroup)
        {
            var initial = MatchState.CreateInitial().WithPhase(MatchPhase.OpenTable);

            var assigned = initial.WithAssignedGroups(solidsPlayer);

            Assert.That(initial.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(initial.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
            Assert.That(initial.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(assigned.PlayerOne.Group, Is.EqualTo(expectedPlayerOneGroup));
            Assert.That(assigned.PlayerTwo.Group, Is.EqualTo(expectedPlayerTwoGroup));
            Assert.That(assigned.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(assigned, Is.Not.SameAs(initial));
        }

        [Test]
        public void WithPhase_ReturnsNewStateWithoutMutatingOriginal()
        {
            var initial = MatchState.CreateInitial();

            var openTable = initial.WithPhase(MatchPhase.OpenTable);

            Assert.That(initial.Phase, Is.EqualTo(MatchPhase.Break));
            Assert.That(openTable.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(openTable, Is.Not.SameAs(initial));
        }

        [Test]
        public void WithResult_FinishesMatchWithoutMutatingOriginal()
        {
            var initial = MatchState.CreateInitial();
            var result = new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallLegallyPocketed);

            var finished = initial.WithResult(result);

            Assert.That(initial.IsFinished, Is.False);
            Assert.That(initial.Result, Is.Null);
            Assert.That(finished.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(finished.IsFinished, Is.True);
            Assert.That(finished.Result, Is.EqualTo(result));
            Assert.That(finished.HasBallInHand, Is.False);
        }

        [Test]
        public void FinishedMatch_RejectsTurnChanges()
        {
            var finished = MatchState.CreateInitial().WithResult(new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallLegallyPocketed));

            Assert.Throws<InvalidOperationException>(() =>
                finished.WithCurrentPlayer(MatchPlayerId.PlayerTwo));
            Assert.Throws<InvalidOperationException>(() => finished.AdvanceTurn());
        }

        [Test]
        public void Constructor_RejectsFinishedPhaseWithoutResult()
        {
            Assert.Throws<ArgumentException>(() => new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne),
                new MatchPlayerState(MatchPlayerId.PlayerTwo),
                MatchPlayerId.PlayerOne,
                MatchPhase.Finished));
        }

        [Test]
        public void MatchResult_RejectsSameWinnerAndLoser()
        {
            Assert.Throws<ArgumentException>(() => new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerOne,
                MatchEndReason.EightBallLegallyPocketed));
        }

        [Test]
        public void MatchResult_RejectsMissingReason()
        {
            Assert.Throws<ArgumentException>(() => new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.None));
        }

        [Test]
        public void MatchResult_RejectsUnknownReason()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                (MatchEndReason)(1 << 20)));
        }

        [Test]
        public void MatchResult_RejectsLegalWinCombinedWithLossReason()
        {
            Assert.Throws<ArgumentException>(() => new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallLegallyPocketed | MatchEndReason.EightBallPocketedWithFoul));
        }

        [Test]
        public void Constructor_RejectsMismatchedPlayerIdentifiers()
        {
            var wrongPlayerOne = new MatchPlayerState(MatchPlayerId.PlayerTwo);
            var playerTwo = new MatchPlayerState(MatchPlayerId.PlayerTwo);

            Assert.Throws<ArgumentException>(() => new MatchState(
                wrongPlayerOne,
                playerTwo,
                MatchPlayerId.PlayerOne,
                MatchPhase.Break));
        }

        [Test]
        public void Constructor_RejectsOnlyOneAssignedGroup()
        {
            var playerOne = new MatchPlayerState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var playerTwo = new MatchPlayerState(MatchPlayerId.PlayerTwo);

            Assert.Throws<ArgumentException>(() => new MatchState(
                playerOne,
                playerTwo,
                MatchPlayerId.PlayerOne,
                MatchPhase.GroupsAssigned));
        }

        [Test]
        public void Constructor_RejectsDuplicateAssignedGroup()
        {
            var playerOne = new MatchPlayerState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var playerTwo = new MatchPlayerState(MatchPlayerId.PlayerTwo, BallGroup.Solids);

            Assert.Throws<ArgumentException>(() => new MatchState(
                playerOne,
                playerTwo,
                MatchPlayerId.PlayerOne,
                MatchPhase.GroupsAssigned));
        }

        [Test]
        public void Constructor_RejectsGroupsAssignedPhaseWithoutAssignedGroups()
        {
            Assert.Throws<ArgumentException>(() => new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne),
                new MatchPlayerState(MatchPlayerId.PlayerTwo),
                MatchPlayerId.PlayerOne,
                MatchPhase.GroupsAssigned));
        }

        [Test]
        public void Constructor_RejectsOpenTablePhaseWithAssignedGroups()
        {
            Assert.Throws<ArgumentException>(() => new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne, BallGroup.Solids),
                new MatchPlayerState(MatchPlayerId.PlayerTwo, BallGroup.Stripes),
                MatchPlayerId.PlayerOne,
                MatchPhase.OpenTable));
        }

        [TestCase((MatchPlayerId)0)]
        [TestCase((MatchPlayerId)3)]
        public void MatchPlayerState_RejectsUnknownPlayerIdentifier(MatchPlayerId id)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MatchPlayerState(id));
        }

        [Test]
        public void MatchPlayerState_WithGroupRejectsUnassignedGroup()
        {
            var player = new MatchPlayerState(MatchPlayerId.PlayerOne);

            Assert.Throws<ArgumentException>(() => player.WithGroup(BallGroup.None));
        }
    }
}
