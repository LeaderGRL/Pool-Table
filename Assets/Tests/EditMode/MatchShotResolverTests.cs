using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Match;

namespace PoolTable.Tests.EditMode
{
    public sealed class MatchShotResolverTests
    {
        private static readonly ShotDirection Direction = new ShotDirection(1f, 0f);
        private static readonly PocketId PocketOne = new PocketId(1);
        private readonly MatchShotResolver resolver = new MatchShotResolver();

        [Test]
        public void Resolve_CleanSuccessfulCalledShotContinuesTurn()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var calledBall = new BallId(2);
            var snapshot = Snapshot(2, 3, 8, 9);
            var intent = Intent(MatchPlayerId.PlayerOne, calledBall, PocketOne);
            var facts = Facts(calledBall, new PocketedBall(calledBall, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.CalledShotSucceeded, Is.True);
            Assert.That(resolution.ShooterContinues, Is.True);
            Assert.That(resolution.TurnAdvanced, Is.False);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_CleanOwnGroupPocketWithoutCalledShotContinuesTurn()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var snapshot = Snapshot(2, 3, 8, 9);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(solid, new PocketedBall(solid, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.ShooterContinues, Is.True);
            Assert.That(resolution.TurnAdvanced, Is.False);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_OpenTableSingleFamilyPocketWithoutCalledShotAssignsGroupsAndContinues()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var solid = new BallId(2);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(solid, new PocketedBall(solid, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.GroupAssigned, Is.True);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.Solids));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.Stripes));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.ShooterContinues, Is.True);
            Assert.That(resolution.TurnAdvanced, Is.False);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_OpenTableMixedFamiliesKeepTableOpenAndAdvanceTurn()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var solid = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(
                solid,
                new PocketedBall(solid, PocketOne),
                new PocketedBall(stripe, new PocketId(2)));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.GroupAssigned, Is.False);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_OpenTableMixedFamiliesIgnoreSuccessfulLegacyCallAndAdvanceTurn()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var solid = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, solid, PocketOne);
            var facts = Facts(
                solid,
                new PocketedBall(solid, PocketOne),
                new PocketedBall(stripe, new PocketId(2)));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.CalledShotSucceeded, Is.True);
            Assert.That(resolution.GroupAssigned, Is.False);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_OpponentOnlyPocketAdvancesTurnWithoutFoul()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(solid, new PocketedBall(stripe, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
            Assert.That(resolution.State.HasBallInHand, Is.False);
        }

        [Test]
        public void Resolve_MixedGroupsAdvanceTurnWithoutFoul()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(
                solid,
                new PocketedBall(solid, PocketOne),
                new PocketedBall(stripe, new PocketId(2)));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
            Assert.That(resolution.State.HasBallInHand, Is.False);
        }

        [Test]
        public void Resolve_MixedGroupsIgnoreSuccessfulLegacyCallAndAdvanceTurn()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, solid, PocketOne);
            var facts = Facts(
                solid,
                new PocketedBall(solid, PocketOne),
                new PocketedBall(stripe, new PocketId(2)));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.CalledShotSucceeded, Is.True);
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_LegalNoPocketAdvancesTurnWithoutFoul()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = new ShotFacts(solid, Array.Empty<PocketedBall>(), new[] { solid });

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
            Assert.That(resolution.State.HasBallInHand, Is.False);
        }

        [Test]
        public void Resolve_MissedCallDoesNotOverrideObservedOwnGroupPocket()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var snapshot = Snapshot(2, 3, 8, 9, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, solid, new PocketId(2));
            var facts = Facts(solid, new PocketedBall(solid, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.CalledShotSucceeded, Is.False);
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.ShooterContinues, Is.True);
            Assert.That(resolution.TurnAdvanced, Is.False);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_CleanMissedCalledShotAdvancesTurn()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var calledBall = new BallId(2);
            var snapshot = Snapshot(2, 3, 8, 9);
            var intent = Intent(MatchPlayerId.PlayerOne, calledBall, PocketOne);
            var facts = new ShotFacts(calledBall, Array.Empty<PocketedBall>(), new[] { calledBall });

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.CalledShotSucceeded, Is.False);
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.State.HasBallInHand, Is.False);
        }

        [Test]
        public void Resolve_StandardFoulAdvancesTurnAndGrantsBallInHand()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var snapshot = Snapshot(2, 3, 8, 9);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = new ShotFacts(null, Array.Empty<PocketedBall>(), Array.Empty<BallId>());

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.FoulResolution.Has(ShotFoul.NoObjectBallContact), Is.True);
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.State.HasBallInHand, Is.True);
            Assert.That(resolution.State.BallInHand.Recipient, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.State.BallInHand.PlacementArea, Is.EqualTo(CueBallPlacementArea.Anywhere));
            Assert.That(resolution.TurnAdvanced, Is.True);
        }

        [TestCase(2, BallGroup.Solids, BallGroup.Stripes)]
        [TestCase(10, BallGroup.Stripes, BallGroup.Solids)]
        public void Resolve_OpenTableSuccessfulCalledGroupedBallAssignsGroups(
            int ballNumber,
            BallGroup expectedShooterGroup,
            BallGroup expectedOpponentGroup)
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var calledBall = new BallId(ballNumber);
            var snapshot = Snapshot(2, 8, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, calledBall, PocketOne);
            var facts = Facts(calledBall, new PocketedBall(calledBall, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.GroupAssigned, Is.True);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(expectedShooterGroup));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(expectedOpponentGroup));
            Assert.That(resolution.ShooterContinues, Is.True);
        }

        [Test]
        public void Resolve_FoulPreventsOpenTableGroupAssignment()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var calledBall = new BallId(2);
            var cueBall = new BallId(BallId.CueBallNumber);
            var snapshot = Snapshot(2, 8, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, calledBall, PocketOne);
            var facts = new ShotFacts(
                calledBall,
                new[]
                {
                    new PocketedBall(calledBall, PocketOne),
                    new PocketedBall(cueBall, new PocketId(2)),
                },
                Array.Empty<BallId>());

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.FoulResolution.Has(ShotFoul.CueBallScratch), Is.True);
            Assert.That(resolution.GroupAssigned, Is.False);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
        }

        [Test]
        public void Resolve_EightBallWithoutDeclarationAfterGroupClearedFinishesWithShooterAsWinner()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var eightBall = new BallId(BallId.EightBallNumber);
            var snapshot = Snapshot(8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(eightBall, new PocketedBall(eightBall, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.MatchFinished, Is.True);
            Assert.That(resolution.State.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.State.Result.Value.Loser, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.State.Result.Value.Reasons, Is.EqualTo(MatchEndReason.EightBallLegallyPocketed));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.False);
        }

        [Test]
        public void Resolve_EarlyCalledEightBallFinishesWithOpponentAsWinner()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var solid = new BallId(2);
            var eightBall = new BallId(BallId.EightBallNumber);
            var snapshot = Snapshot(2, 8, 9);
            var intent = Intent(MatchPlayerId.PlayerOne, eightBall, PocketOne);
            var facts = Facts(solid, new PocketedBall(eightBall, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.MatchFinished, Is.True);
            Assert.That(resolution.State.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(
                resolution.State.Result.Value.HasReason(MatchEndReason.EightBallPocketedBeforeGroupCleared),
                Is.True);
        }

        [Test]
        public void Resolve_CalledEightBallWithFoulDoesNotReportSuccessfulCall()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var cueBall = new BallId(BallId.CueBallNumber);
            var eightBall = new BallId(BallId.EightBallNumber);
            var snapshot = Snapshot(8, 9, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, eightBall, PocketOne);
            var facts = new ShotFacts(
                eightBall,
                new[]
                {
                    new PocketedBall(eightBall, PocketOne),
                    new PocketedBall(cueBall, new PocketId(2)),
                },
                Array.Empty<BallId>());

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.MatchFinished, Is.True);
            Assert.That(resolution.State.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(
                resolution.State.Result.Value.HasReason(MatchEndReason.EightBallPocketedWithFoul),
                Is.True);
            Assert.That(resolution.FoulResolution.Has(ShotFoul.CueBallScratch), Is.True);
            Assert.That(resolution.CalledShotSucceeded, Is.False);
        }

        [Test]
        public void Resolve_DoesNotMutateOriginalMatchState()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var calledBall = new BallId(2);
            var snapshot = Snapshot(2, 8, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, calledBall, PocketOne);
            var facts = Facts(calledBall, new PocketedBall(calledBall, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(state.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(state.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(state.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(state.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State, Is.Not.SameAs(state));
        }

        [TestCase(2, BallGroup.Solids)]
        [TestCase(10, BallGroup.Stripes)]
        public void Resolve_CleanBreakSingleFamilyAssignsThatGroupAndContinues(
            int ballNumber,
            BallGroup expectedShooterGroup)
        {
            var state = MatchState.CreateInitial();
            var groupedBall = new BallId(ballNumber);
            var snapshot = Snapshot(2, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.8f);
            var facts = Facts(groupedBall, new PocketedBall(groupedBall, PocketOne));
            var expectedOpponentGroup = expectedShooterGroup == BallGroup.Solids
                ? BallGroup.Stripes
                : BallGroup.Solids;

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.RequiresBreakFollowUp, Is.False);
            Assert.That(resolution.GroupAssigned, Is.True);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(expectedShooterGroup));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(expectedOpponentGroup));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.ShooterContinues, Is.True);
            Assert.That(resolution.TurnAdvanced, Is.False);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_CleanBreakMixedFamiliesKeepsTableOpenAndAdvancesTurn()
        {
            var state = MatchState.CreateInitial();
            var solid = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(2, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.8f);
            var facts = Facts(
                solid,
                new PocketedBall(solid, PocketOne),
                new PocketedBall(stripe, new PocketId(2)));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.RequiresBreakFollowUp, Is.False);
            Assert.That(resolution.GroupAssigned, Is.False);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.None));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
        }

        [Test]
        public void Resolve_BreakScratchWithSingleFamilyPreservesAssignmentAndGrantsOpponentBallInHand()
        {
            var state = MatchState.CreateInitial();
            var cueBall = new BallId(BallId.CueBallNumber);
            var solid = new BallId(2);
            var snapshot = Snapshot(2, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.8f);
            var facts = new ShotFacts(
                solid,
                new[]
                {
                    new PocketedBall(solid, PocketOne),
                    new PocketedBall(cueBall, new PocketId(2)),
                },
                Array.Empty<BallId>());

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.RequiresBreakFollowUp, Is.False);
            Assert.That(resolution.GroupAssigned, Is.True);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.Solids));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.Stripes));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.State.HasBallInHand, Is.True);
            Assert.That(resolution.State.BallInHand.Recipient, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.State.BallInHand.PlacementArea, Is.EqualTo(CueBallPlacementArea.Anywhere));
            Assert.That(resolution.FoulResolution.Has(ShotFoul.CueBallScratch), Is.True);
            Assert.That(resolution.GrantsTwoShotEntitlement, Is.True);
            Assert.That(resolution.ShooterContinues, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.True);
        }

        [Test]
        public void Resolve_BreakEightBallLossTakesPrecedenceOverAssignmentAndScratch()
        {
            var state = MatchState.CreateInitial();
            var cueBall = new BallId(BallId.CueBallNumber);
            var solid = new BallId(2);
            var eightBall = new BallId(BallId.EightBallNumber);
            var snapshot = Snapshot(2, 8, 9, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.8f);
            var facts = new ShotFacts(
                solid,
                new[]
                {
                    new PocketedBall(solid, PocketOne),
                    new PocketedBall(eightBall, new PocketId(2)),
                    new PocketedBall(cueBall, new PocketId(3)),
                },
                Array.Empty<BallId>());

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.MatchFinished, Is.True);
            Assert.That(resolution.State.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.State.Result.Value.Loser, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.State.HasBallInHand, Is.False);
            Assert.That(resolution.GroupAssigned, Is.False);
            Assert.That(resolution.TurnAdvanced, Is.False);
            Assert.That(resolution.RequiresBreakFollowUp, Is.False);
        }

        [Test]
        public void Resolve_CleanBreakWithoutPocketOpensTableAndAdvancesTurn()
        {
            var state = MatchState.CreateInitial();
            var one = new BallId(1);
            var snapshot = Snapshot(1, 2, 3, 4, 8, 9);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.8f);
            var facts = new ShotFacts(
                one,
                Array.Empty<PocketedBall>(),
                new[] { one, new BallId(2), new BallId(3), new BallId(4) });

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.RequiresBreakFollowUp, Is.False);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.OpenTable));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolution.FoulResolution.IsClean, Is.True);
            Assert.That(resolution.BreakEvaluation.HasValue, Is.True);
            Assert.That(resolution.BreakEvaluation.Value.IsLegal, Is.True);
            Assert.That(
                resolution.BreakEvaluation.Value.Reason,
                Is.EqualTo(BreakEvaluationReason.FourOrMoreObjectBallsReachedRails));
            Assert.That(resolution.TurnAdvanced, Is.True);
            Assert.That(resolution.ShooterContinues, Is.False);
        }

        [Test]
        public void Resolve_RejectsIntentFromNonCurrentPlayer()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var snapshot = Snapshot(2, 8, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerTwo, Direction, 0.5f);
            var facts = new ShotFacts(new BallId(2), Array.Empty<PocketedBall>(), new[] { new BallId(2) });

            Assert.Throws<ArgumentException>(() => resolver.Resolve(state, intent, facts, snapshot));
        }

        [Test]
        public void Resolve_RejectsDefaultIntent()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var snapshot = Snapshot(2, 8, 10);
            var facts = new ShotFacts(new BallId(2), Array.Empty<PocketedBall>(), new[] { new BallId(2) });

            Assert.Throws<ArgumentException>(() => resolver.Resolve(state, default, facts, snapshot));
        }

        [Test]
        public void Resolve_StaleLegacyCallDoesNotOverrideObservedOpenTablePocket()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var calledBall = new BallId(2);
            var stripe = new BallId(10);
            var snapshot = Snapshot(3, 8, 10);
            var intent = Intent(MatchPlayerId.PlayerOne, calledBall, PocketOne);
            var facts = Facts(stripe, new PocketedBall(stripe, PocketOne));

            var resolution = resolver.Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.CalledShotSucceeded, Is.False);
            Assert.That(resolution.GroupAssigned, Is.True);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.Stripes));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.Solids));
            Assert.That(resolution.State.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolution.ShooterContinues, Is.True);
            Assert.That(resolution.TurnAdvanced, Is.False);
        }

        [Test]
        public void Resolve_RejectsPocketedObjectBallMissingFromPreShotSnapshot()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var observedBall = new BallId(2);
            var snapshot = Snapshot(3, 8, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = Facts(new BallId(3), new PocketedBall(observedBall, PocketOne));

            Assert.Throws<ArgumentException>(() => resolver.Resolve(state, intent, facts, snapshot));
        }

        [Test]
        public void Resolve_RejectsRailObservationForObjectBallMissingFromPreShotSnapshot()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var firstContact = new BallId(3);
            var snapshot = Snapshot(3, 8, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = new ShotFacts(
                firstContact,
                Array.Empty<PocketedBall>(),
                new[] { new BallId(4) });

            Assert.Throws<ArgumentException>(() => resolver.Resolve(state, intent, facts, snapshot));
        }

        [Test]
        public void Resolve_RejectsFinishedMatch()
        {
            var finished = CreateFinishedState();
            var eightBall = new BallId(BallId.EightBallNumber);
            var snapshot = Snapshot(8, 9);
            var intent = Intent(MatchPlayerId.PlayerOne, eightBall, PocketOne);
            var facts = Facts(eightBall, new PocketedBall(eightBall, PocketOne));

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve(finished, intent, facts, snapshot));
        }

        [Test]
        public void Resolve_RejectsNullReferenceInputs()
        {
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var snapshot = Snapshot(2, 8, 10);
            var intent = new ShotIntent(MatchPlayerId.PlayerOne, Direction, 0.5f);
            var facts = new ShotFacts(new BallId(2), Array.Empty<PocketedBall>(), new[] { new BallId(2) });

            Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null, intent, facts, snapshot));
            Assert.Throws<ArgumentNullException>(() => resolver.Resolve(state, intent, null, snapshot));
            Assert.Throws<ArgumentNullException>(() => resolver.Resolve(state, intent, facts, null));
        }

        private static MatchState CreateAssignedState(BallGroup playerOneGroup, MatchPlayerId currentPlayer)
        {
            var playerTwoGroup = playerOneGroup == BallGroup.Solids ? BallGroup.Stripes : BallGroup.Solids;
            return new MatchState(
                new MatchPlayerState(MatchPlayerId.PlayerOne, playerOneGroup),
                new MatchPlayerState(MatchPlayerId.PlayerTwo, playerTwoGroup),
                currentPlayer,
                MatchPhase.GroupsAssigned);
        }

        private static MatchState CreateFinishedState()
        {
            var state = CreateAssignedState(BallGroup.Solids, MatchPlayerId.PlayerOne);
            var eightBall = new BallId(BallId.EightBallNumber);
            var snapshot = Snapshot(8, 9);
            var intent = Intent(MatchPlayerId.PlayerOne, eightBall, PocketOne);
            var facts = Facts(eightBall, new PocketedBall(eightBall, PocketOne));
            return EightBallRule.Resolve(state, intent, facts, snapshot);
        }

        private static ShotIntent Intent(MatchPlayerId player, BallId ball, PocketId pocket)
        {
            return new ShotIntent(player, Direction, 0.5f, new CalledShot(ball, pocket));
        }

        private static ShotFacts Facts(BallId firstContact, params PocketedBall[] pocketed)
        {
            return new ShotFacts(firstContact, pocketed, Array.Empty<BallId>());
        }

        private static ObjectBallTableSnapshot Snapshot(params int[] ballNumbers)
        {
            var balls = new BallId[ballNumbers.Length];
            for (var index = 0; index < ballNumbers.Length; index++)
            {
                balls[index] = new BallId(ballNumbers[index]);
            }

            return new ObjectBallTableSnapshot(balls);
        }
    }
}
