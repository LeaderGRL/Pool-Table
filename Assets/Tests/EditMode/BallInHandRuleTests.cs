using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;

namespace PoolTable.Tests.EditMode
{
    public sealed class BallInHandRuleTests
    {
        [Test]
        public void CreateInitial_HasNoBallInHand()
        {
            var state = MatchState.CreateInitial();

            Assert.That(state.HasBallInHand, Is.False);
            Assert.That(state.BallInHand, Is.EqualTo(BallInHandState.None));
            Assert.That(state.BallInHand.Recipient, Is.Null);
            Assert.That(state.BallInHand.PlacementArea, Is.EqualTo(CueBallPlacementArea.None));
        }

        [TestCase(MatchPlayerId.PlayerOne)]
        [TestCase(MatchPlayerId.PlayerTwo)]
        public void GrantAfterStandardFoul_GivesIncomingPlayerAnywhereBallInHand(MatchPlayerId shooter)
        {
            var state = OpenTableState(shooter);

            var resolved = BallInHandRule.GrantAfterStandardFoul(state, ScratchFoul());

            var incoming = shooter == MatchPlayerId.PlayerOne
                ? MatchPlayerId.PlayerTwo
                : MatchPlayerId.PlayerOne;

            Assert.That(state.CurrentPlayer, Is.EqualTo(shooter));
            Assert.That(state.HasBallInHand, Is.False);
            Assert.That(resolved.CurrentPlayer, Is.EqualTo(incoming));
            Assert.That(resolved.HasBallInHand, Is.True);
            Assert.That(resolved.BallInHand.Recipient, Is.EqualTo(incoming));
            Assert.That(resolved.BallInHand.PlacementArea, Is.EqualTo(CueBallPlacementArea.Anywhere));
            Assert.That(resolved.Phase, Is.EqualTo(MatchPhase.OpenTable));
        }

        [Test]
        public void GrantAfterStandardFoul_PreservesAssignedGroups()
        {
            var state = AssignedState();

            var resolved = BallInHandRule.GrantAfterStandardFoul(state, ScratchFoul());

            Assert.That(resolved.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolved.PlayerOne.Group, Is.EqualTo(BallGroup.Solids));
            Assert.That(resolved.PlayerTwo.Group, Is.EqualTo(BallGroup.Stripes));
            Assert.That(resolved.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolved.BallInHand.PlacementArea, Is.EqualTo(CueBallPlacementArea.Anywhere));
        }

        [Test]
        public void GrantAfterStandardFoul_RejectsCleanShot()
        {
            Assert.Throws<ArgumentException>(() =>
                BallInHandRule.GrantAfterStandardFoul(OpenTableState(), CleanResolution()));
        }

        [Test]
        public void GrantAfterStandardFoul_RejectsBreakPhase()
        {
            Assert.Throws<InvalidOperationException>(() =>
                BallInHandRule.GrantAfterStandardFoul(MatchState.CreateInitial(), ScratchFoul()));
        }

        [Test]
        public void ChooseAboveHeadStringAfterBreakFoul_OpensTableAndGrantsIncomingPlayerRestrictedBallInHand()
        {
            var breakState = MatchState.CreateInitial(MatchPlayerId.PlayerOne);

            var resolved = BallInHandRule.ChooseAboveHeadStringAfterBreakFoul(breakState, ScratchFoul());

            Assert.That(breakState.Phase, Is.EqualTo(MatchPhase.Break));
            Assert.That(breakState.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(breakState.HasBallInHand, Is.False);
            Assert.That(resolved.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(resolved.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolved.BallInHand.Recipient, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolved.BallInHand.PlacementArea, Is.EqualTo(CueBallPlacementArea.AboveHeadString));
        }

        [Test]
        public void ChooseAboveHeadStringAfterBreakFoul_RejectsCleanBreak()
        {
            Assert.Throws<ArgumentException>(() =>
                BallInHandRule.ChooseAboveHeadStringAfterBreakFoul(
                    MatchState.CreateInitial(),
                    CleanResolution()));
        }

        [Test]
        public void ChooseAboveHeadStringAfterBreakFoul_RejectsNonBreakPhase()
        {
            Assert.Throws<InvalidOperationException>(() =>
                BallInHandRule.ChooseAboveHeadStringAfterBreakFoul(
                    OpenTableState(),
                    ScratchFoul()));
        }

        [Test]
        public void CompletePlacement_ClearsBallInHandWithoutChangingTurnOrPhase()
        {
            var granted = BallInHandRule.GrantAfterStandardFoul(OpenTableState(), ScratchFoul());

            var completed = BallInHandRule.CompletePlacement(granted, MatchPlayerId.PlayerTwo);

            Assert.That(granted.HasBallInHand, Is.True);
            Assert.That(completed.HasBallInHand, Is.False);
            Assert.That(completed.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(completed.Phase, Is.EqualTo(MatchPhase.OpenTable));
        }

        [Test]
        public void CompletePlacement_RejectsWrongPlayer()
        {
            var granted = BallInHandRule.GrantAfterStandardFoul(OpenTableState(), ScratchFoul());

            Assert.Throws<InvalidOperationException>(() =>
                BallInHandRule.CompletePlacement(granted, MatchPlayerId.PlayerOne));
        }

        [Test]
        public void CompletePlacement_RejectsMissingBallInHand()
        {
            Assert.Throws<InvalidOperationException>(() =>
                BallInHandRule.CompletePlacement(OpenTableState(), MatchPlayerId.PlayerOne));
        }

        [Test]
        public void MatchState_RejectsBallInHandOwnedByNonCurrentPlayer()
        {
            var ballInHand = new BallInHandState(
                MatchPlayerId.PlayerTwo,
                CueBallPlacementArea.Anywhere);

            Assert.Throws<ArgumentException>(() => new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne),
                new MatchPlayerState(MatchPlayerId.PlayerTwo),
                MatchPlayerId.PlayerOne,
                MatchPhase.OpenTable,
                ballInHand));
        }

        [Test]
        public void MatchState_RejectsBallInHandAfterMatchFinished()
        {
            var ballInHand = new BallInHandState(
                MatchPlayerId.PlayerOne,
                CueBallPlacementArea.Anywhere);

            Assert.Throws<ArgumentException>(() => new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne),
                new MatchPlayerState(MatchPlayerId.PlayerTwo),
                MatchPlayerId.PlayerOne,
                MatchPhase.Finished,
                ballInHand));
        }

        [Test]
        public void MatchState_RejectsBallInHandDuringBreak()
        {
            var ballInHand = new BallInHandState(
                MatchPlayerId.PlayerOne,
                CueBallPlacementArea.Anywhere);

            Assert.Throws<ArgumentException>(() => new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne),
                new MatchPlayerState(MatchPlayerId.PlayerTwo),
                MatchPlayerId.PlayerOne,
                MatchPhase.Break,
                ballInHand));
        }

        [Test]
        public void MatchState_RejectsAboveHeadStringAfterGroupsAreAssigned()
        {
            var assigned = AssignedState();
            var ballInHand = new BallInHandState(
                assigned.CurrentPlayer,
                CueBallPlacementArea.AboveHeadString);

            Assert.Throws<ArgumentException>(() => new MatchState(
                assigned.PlayerOne,
                assigned.PlayerTwo,
                assigned.CurrentPlayer,
                assigned.Phase,
                ballInHand));
        }

        [TestCase(CueBallPlacementArea.None)]
        [TestCase((CueBallPlacementArea)3)]
        public void BallInHandState_RejectsInvalidActivePlacementArea(CueBallPlacementArea placementArea)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BallInHandState(MatchPlayerId.PlayerOne, placementArea));
        }

        private static FoulResolution ScratchFoul()
        {
            return new FoulResolution(ShotFoul.CueBallScratch);
        }

        private static FoulResolution CleanResolution()
        {
            return new FoulResolution(ShotFoul.None);
        }

        private static MatchState OpenTableState(MatchPlayerId player = MatchPlayerId.PlayerOne)
        {
            return OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(player));
        }

        private static MatchState AssignedState()
        {
            return PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                OpenTableState(),
                MatchPlayerId.PlayerOne,
                new BallId(1));
        }
    }
}
