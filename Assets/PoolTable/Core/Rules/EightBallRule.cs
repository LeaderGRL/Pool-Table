using System;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Shots;

namespace PoolTable.Core.Rules
{
    public static class EightBallRule
    {
        private static readonly BallId EightBall = new BallId(BallId.EightBallNumber);

        public static MatchState Resolve(
            MatchState state,
            ShotIntent intent,
            ShotFacts facts,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            ValidateInputs(state, intent, facts, tableBeforeShot);

            if (state.Phase == MatchPhase.Break)
            {
                return state;
            }

            var eightBallPocketed = WasEightBallPocketed(facts);
            var eightBallDrivenOffTable = facts.WasDrivenOffTable(EightBall);

            if (!eightBallPocketed && !eightBallDrivenOffTable)
            {
                return state;
            }

            if (!tableBeforeShot.Contains(EightBall))
            {
                throw new ArgumentException(
                    "Eight-ball terminal resolution requires the 8-ball in the pre-shot table snapshot.",
                    nameof(tableBeforeShot));
            }

            var shooter = state.CurrentPlayer;
            var opponent = OtherPlayer(shooter);
            var lossReasons = MatchEndReason.None;
            var foulResolution = FoulResolutionRule.Evaluate(state, facts, tableBeforeShot);

            if (eightBallDrivenOffTable)
            {
                lossReasons |= MatchEndReason.EightBallDrivenOffTable;
            }

            if (eightBallPocketed)
            {
                if (foulResolution.HasFoul)
                {
                    lossReasons |= MatchEndReason.EightBallPocketedWithFoul;
                }

                if (!WasShooterGroupClearedBeforeShot(state, tableBeforeShot))
                {
                    lossReasons |= MatchEndReason.EightBallPocketedBeforeGroupCleared;
                }

                if (!WasEightBallPocketedInCalledPocket(intent, facts))
                {
                    lossReasons |= MatchEndReason.EightBallPocketedInUncalledPocket;
                }
            }

            if (lossReasons != MatchEndReason.None)
            {
                return state.WithResult(new MatchResult(opponent, shooter, lossReasons));
            }

            return state.WithResult(new MatchResult(
                shooter,
                opponent,
                MatchEndReason.EightBallLegallyPocketed));
        }

        private static void ValidateInputs(
            MatchState state,
            ShotIntent intent,
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
                throw new InvalidOperationException("Eight-ball resolution cannot run after the match has finished.");
            }

            if (intent.Player != state.CurrentPlayer)
            {
                throw new ArgumentException(
                    "Shot intent player must match the current match player.",
                    nameof(intent));
            }
        }

        private static bool WasEightBallPocketed(ShotFacts facts)
        {
            for (var index = 0; index < facts.PocketedBalls.Count; index++)
            {
                if (facts.PocketedBalls[index].IsEightBall)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WasShooterGroupClearedBeforeShot(
            MatchState state,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            if (state.Phase != MatchPhase.GroupsAssigned)
            {
                return false;
            }

            var shooterGroup = state.GetPlayer(state.CurrentPlayer).Group;
            return !tableBeforeShot.HasRemainingBalls(shooterGroup);
        }

        private static bool WasEightBallPocketedInCalledPocket(ShotIntent intent, ShotFacts facts)
        {
            if (!intent.CalledShot.HasValue)
            {
                return false;
            }

            var calledShot = intent.CalledShot.Value;
            return calledShot.ObjectBall.IsEightBall
                && facts.WasPocketedIn(EightBall, calledShot.Pocket);
        }

        private static MatchPlayerId OtherPlayer(MatchPlayerId player)
        {
            return player == MatchPlayerId.PlayerOne
                ? MatchPlayerId.PlayerTwo
                : MatchPlayerId.PlayerOne;
        }
    }
}
