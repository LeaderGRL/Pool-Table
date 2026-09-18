using System;
using System.Collections.Generic;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class EightBallRuleTests
    {
        [Test]
        public void Resolve_EightBallAfterGroupClearedWithoutDeclaration_ShooterWins()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(8, 9, 10, 11, 12, 13, 14, 15);
            var intent = Intent(MatchPlayerId.PlayerOne);
            var facts = Facts(
                firstContact: 8,
                pocketed: new[] { Pocketed(8, 3) });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);

            Assert.That(state.IsFinished, Is.False);
            Assert.That(resolved.IsFinished, Is.True);
            Assert.That(resolved.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(resolved.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolved.Result.HasValue, Is.True);
            Assert.That(resolved.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolved.Result.Value.Loser, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolved.Result.Value.IsLegalEightBallWin, Is.True);
            Assert.That(resolved.Result.Value.Reasons, Is.EqualTo(MatchEndReason.EightBallLegallyPocketed));
        }

        [Test]
        public void Resolve_EightBallPocketedWithFoul_ShooterLoses()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(8, 9, 10, 11, 12, 13, 14, 15);
            var intent = Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(8), new PocketId(2)));
            var facts = Facts(
                firstContact: 8,
                pocketed: new[]
                {
                    Pocketed(8, 2),
                    Pocketed(BallId.CueBallNumber, 4),
                });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);

            AssertLoss(resolved, MatchPlayerId.PlayerTwo, MatchEndReason.EightBallPocketedWithFoul);
        }

        [Test]
        public void Resolve_EightBallPocketedBeforeGroupCleared_ShooterLoses()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(1, 8, 9, 10, 11, 12, 13, 14, 15);
            var intent = Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(8), new PocketId(1)));
            var facts = Facts(
                firstContact: 1,
                pocketed: new[] { Pocketed(8, 1) });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);

            AssertLoss(
                resolved,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallPocketedBeforeGroupCleared);
        }

        [Test]
        public void Resolve_StaleCalledPocketDoesNotOverridePocheLibreEightBallWin()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(8, 9, 10, 11, 12, 13, 14, 15);
            var intent = Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(8), new PocketId(1)));
            var facts = Facts(
                firstContact: 8,
                pocketed: new[] { Pocketed(8, 6) });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);

            Assert.That(resolved.IsFinished, Is.True);
            Assert.That(resolved.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(resolved.Result.Value.Reasons, Is.EqualTo(MatchEndReason.EightBallLegallyPocketed));
        }

        [Test]
        public void Resolve_PlayerTwoEightBallAfterGroupClearedWithoutDeclaration_ShooterWins()
        {
            var state = AssignedState(MatchPlayerId.PlayerTwo, BallGroup.Stripes);
            var table = Balls(1, 2, 3, 4, 5, 6, 7, 8);
            var intent = Intent(MatchPlayerId.PlayerTwo);
            var facts = Facts(
                firstContact: 8,
                pocketed: new[] { Pocketed(8, 5) });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);

            Assert.That(resolved.IsFinished, Is.True);
            Assert.That(resolved.Result.Value.Winner, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(resolved.Result.Value.Reasons, Is.EqualTo(MatchEndReason.EightBallLegallyPocketed));
        }

        [Test]
        public void Resolve_EightBallDrivenOffTable_ShooterLoses()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(8, 9, 10, 11, 12, 13, 14, 15);
            var intent = Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(8), new PocketId(1)));
            var facts = Facts(
                firstContact: 8,
                offTable: new[] { 8 });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);

            AssertLoss(resolved, MatchPlayerId.PlayerTwo, MatchEndReason.EightBallDrivenOffTable);
        }

        [Test]
        public void Resolve_MultipleEightBallLossConditions_PreservesAllReasons()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var table = Balls(1, 8, 9, 10, 11, 12, 13, 14, 15);
            var intent = Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(8), new PocketId(1)));
            var facts = Facts(
                firstContact: 9,
                pocketed: new[]
                {
                    Pocketed(8, 2),
                    Pocketed(BallId.CueBallNumber, 4),
                });

            var resolved = EightBallRule.Resolve(state, intent, facts, table);
            var result = resolved.Result.Value;

            Assert.That(result.Winner, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(result.HasReason(MatchEndReason.EightBallPocketedWithFoul), Is.True);
            Assert.That(result.HasReason(MatchEndReason.EightBallPocketedBeforeGroupCleared), Is.True);
        }

        [Test]
        public void Resolve_EightBallPocketedOnBreak_ShooterLosesImmediately()
        {
            var state = MatchState.CreateInitial(MatchPlayerId.PlayerOne);
            var facts = Facts(
                firstContact: 1,
                pocketed: new[] { Pocketed(8, 3) });

            var resolved = EightBallRule.Resolve(
                state,
                Intent(MatchPlayerId.PlayerOne),
                facts,
                StandardTable());

            AssertLoss(
                resolved,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallPocketedBeforeGroupCleared);
        }

        [Test]
        public void Resolve_EightBallDrivenOffTableOnBreak_ShooterLosesImmediately()
        {
            var state = MatchState.CreateInitial(MatchPlayerId.PlayerTwo);
            var facts = Facts(firstContact: 1, offTable: new[] { 8 });

            var resolved = EightBallRule.Resolve(
                state,
                Intent(MatchPlayerId.PlayerTwo),
                facts,
                StandardTable());

            AssertLoss(
                resolved,
                MatchPlayerId.PlayerOne,
                MatchEndReason.EightBallDrivenOffTable);
        }

        [Test]
        public void Resolve_NoEightBallTerminalEvent_LeavesMatchUnchanged()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var facts = Facts(
                firstContact: 1,
                pocketed: new[] { Pocketed(1, 1) });

            var resolved = EightBallRule.Resolve(
                state,
                Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(1), new PocketId(1))),
                facts,
                StandardTable());

            Assert.That(resolved, Is.SameAs(state));
            Assert.That(resolved.IsFinished, Is.False);
        }

        [Test]
        public void Resolve_RejectsIntentFromNonCurrentPlayer()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);

            Assert.Throws<ArgumentException>(() => EightBallRule.Resolve(
                state,
                Intent(MatchPlayerId.PlayerTwo),
                Facts(firstContact: 1),
                StandardTable()));
        }

        [Test]
        public void Resolve_RejectsEightBallObservationMissingFromPreShotTable()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var facts = Facts(firstContact: 1, pocketed: new[] { Pocketed(8, 1) });

            Assert.Throws<ArgumentException>(() => EightBallRule.Resolve(
                state,
                Intent(MatchPlayerId.PlayerOne, new CalledShot(new BallId(8), new PocketId(1))),
                facts,
                Balls(1, 9, 10, 11, 12, 13, 14, 15)));
        }

        [Test]
        public void Resolve_RejectsFinishedMatch()
        {
            var finished = MatchState.CreateInitial().WithResult(new MatchResult(
                MatchPlayerId.PlayerOne,
                MatchPlayerId.PlayerTwo,
                MatchEndReason.EightBallLegallyPocketed));

            Assert.Throws<InvalidOperationException>(() => EightBallRule.Resolve(
                finished,
                Intent(MatchPlayerId.PlayerOne),
                Facts(firstContact: 1),
                StandardTable()));
        }

        [Test]
        public void Resolve_RejectsNullReferenceInputs()
        {
            var state = AssignedState(MatchPlayerId.PlayerOne, BallGroup.Solids);
            var intent = Intent(MatchPlayerId.PlayerOne);
            var facts = Facts(firstContact: 1);
            var table = StandardTable();

            Assert.Throws<ArgumentNullException>(() => EightBallRule.Resolve(null, intent, facts, table));
            Assert.Throws<ArgumentNullException>(() => EightBallRule.Resolve(state, intent, null, table));
            Assert.Throws<ArgumentNullException>(() => EightBallRule.Resolve(state, intent, facts, null));
        }

        private static void AssertLoss(
            MatchState state,
            MatchPlayerId expectedWinner,
            MatchEndReason expectedReason)
        {
            Assert.That(state.IsFinished, Is.True);
            Assert.That(state.Result.HasValue, Is.True);
            Assert.That(state.Result.Value.Winner, Is.EqualTo(expectedWinner));
            Assert.That(state.Result.Value.IsLegalEightBallWin, Is.False);
            Assert.That(state.Result.Value.HasReason(expectedReason), Is.True);
        }

        private static MatchState AssignedState(MatchPlayerId currentPlayer, BallGroup currentPlayerGroup)
        {
            var open = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(currentPlayer));
            var assignmentBall = currentPlayerGroup == BallGroup.Solids ? new BallId(1) : new BallId(9);
            return PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                open,
                currentPlayer,
                assignmentBall);
        }

        private static ShotIntent Intent(MatchPlayerId player, CalledShot? calledShot = null)
        {
            return new ShotIntent(player, new ShotDirection(1f, 0f), 0.5f, calledShot);
        }

        private static ShotFacts Facts(
            int? firstContact,
            PocketedBall[] pocketed = null,
            int[] rails = null,
            int[] offTable = null)
        {
            BallId? first = firstContact.HasValue ? new BallId(firstContact.Value) : (BallId?)null;
            return new ShotFacts(
                first,
                pocketed ?? Array.Empty<PocketedBall>(),
                CreateBallIds(rails ?? Array.Empty<int>()),
                CreateBallIds(offTable ?? Array.Empty<int>()));
        }

        private static PocketedBall Pocketed(int ballNumber, int pocketIndex)
        {
            return new PocketedBall(new BallId(ballNumber), new PocketId(pocketIndex));
        }

        private static ObjectBallTableSnapshot StandardTable()
        {
            return Balls(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        }

        private static ObjectBallTableSnapshot Balls(params int[] ballNumbers)
        {
            return new ObjectBallTableSnapshot(CreateBallIds(ballNumbers));
        }

        private static BallId[] CreateBallIds(int[] numbers)
        {
            var result = new BallId[numbers.Length];
            for (var index = 0; index < numbers.Length; index++)
            {
                result[index] = new BallId(numbers[index]);
            }

            return result;
        }
    }
}
