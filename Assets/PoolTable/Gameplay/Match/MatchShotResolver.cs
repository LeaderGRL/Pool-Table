using System;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;

namespace PoolTable.Gameplay.Match
{
    public sealed class MatchShotResolver
    {
        public ShotResolution Resolve(
            MatchState state,
            ShotIntent intent,
            ShotFacts facts,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            ValidateInputs(state, intent, facts, tableBeforeShot);

            var terminalState = EightBallRule.Resolve(state, intent, facts, tableBeforeShot);
            if (terminalState.IsFinished)
            {
                var terminalFoul = FoulResolutionRule.Evaluate(state, facts, tableBeforeShot);
                return new ShotResolution(
                    terminalState,
                    terminalFoul,
                    breakEvaluation: null,
                    requiresBreakFollowUp: false,
                    calledShotSucceeded: terminalFoul.IsClean
                        && WasCalledShotSuccessful(state, intent, facts, tableBeforeShot),
                    shooterContinues: false,
                    turnAdvanced: false,
                    groupAssigned: false);
            }

            var foulResolution = FoulResolutionRule.Evaluate(state, facts, tableBeforeShot);

            if (state.Phase == MatchPhase.Break)
            {
                var breakEvaluation = LegalBreakRule.Evaluate(facts);
                ClassifyPocketedGroups(
                    facts,
                    out var breakHasPocketedSolids,
                    out var breakHasPocketedStripes,
                    out var firstBreakGroupedBall);

                var breakState = OpenTableRule.EnterAfterBreak(state);
                var groupAssignedOnBreak = breakHasPocketedSolids != breakHasPocketedStripes;
                if (groupAssignedOnBreak)
                {
                    breakState = PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                        breakState,
                        state.CurrentPlayer,
                        firstBreakGroupedBall);
                }

                if (foulResolution.HasFoul)
                {
                    var grantsBallInHand = foulResolution.Has(ShotFoul.CueBallScratch)
                        || foulResolution.Has(ShotFoul.CueBallOffTable);
                    breakState = grantsBallInHand
                        ? BallInHandRule.GrantAfterOpeningBreakCueBallFoul(breakState, foulResolution)
                        : OpenTableRule.PassOpeningControl(breakState);
                    breakState = DeuxCoupsRule.Grant(breakState);

                    return new ShotResolution(
                        breakState,
                        foulResolution,
                        breakEvaluation,
                        requiresBreakFollowUp: false,
                        calledShotSucceeded: false,
                        shooterContinues: false,
                        turnAdvanced: true,
                        groupAssigned: groupAssignedOnBreak,
                        ballsToRespot: CollectBallsToRespot(facts));
                }

                var shooterContinuesAfterBreak = groupAssignedOnBreak;
                if (!shooterContinuesAfterBreak)
                {
                    breakState = OpenTableRule.PassOpeningControl(breakState);
                }

                return new ShotResolution(
                    breakState,
                    foulResolution,
                    breakEvaluation,
                    requiresBreakFollowUp: false,
                    calledShotSucceeded: false,
                    shooterContinues: shooterContinuesAfterBreak,
                    turnAdvanced: !shooterContinuesAfterBreak,
                    groupAssigned: groupAssignedOnBreak);
            }

            if (foulResolution.HasFoul)
            {
                var losesCueBall = foulResolution.Has(ShotFoul.CueBallScratch)
                    || foulResolution.Has(ShotFoul.CueBallOffTable);
                var foulState = losesCueBall
                    ? BallInHandRule.GrantAfterStandardFoul(state, foulResolution)
                    : state.AdvanceTurn();
                foulState = DeuxCoupsRule.Grant(foulState);

                return new ShotResolution(
                    foulState,
                    foulResolution,
                    breakEvaluation: null,
                    requiresBreakFollowUp: false,
                    calledShotSucceeded: false,
                    shooterContinues: false,
                    turnAdvanced: true,
                    groupAssigned: false,
                    ballsToRespot: CollectBallsToRespot(facts));
            }

            var calledShotSucceeded = WasCalledShotSuccessful(state, intent, facts, tableBeforeShot);
            var resolvedState = state;
            var groupAssigned = false;
            var shooterContinues = false;
            var pocketedOwnGroup = false;
            var pocketedOpponentGroup = false;
            ClassifyPocketedGroups(
                facts,
                out var hasPocketedSolids,
                out var hasPocketedStripes,
                out var firstGroupedBall);

            if (state.IsTableOpen)
            {
                if (hasPocketedSolids != hasPocketedStripes)
                {
                    resolvedState = PlayerGroupAssignmentRule.AssignFromLegallyPocketedBall(
                        state,
                        state.CurrentPlayer,
                        firstGroupedBall);
                    groupAssigned = true;
                    shooterContinues = true;
                }
            }
            else if (state.Phase == MatchPhase.GroupsAssigned)
            {
                var shooterGroup = state.GetPlayer(state.CurrentPlayer).Group;
                pocketedOwnGroup = shooterGroup == BallGroup.Solids
                    ? hasPocketedSolids
                    : hasPocketedStripes;
                pocketedOpponentGroup = shooterGroup == BallGroup.Solids
                    ? hasPocketedStripes
                    : hasPocketedSolids;

                shooterContinues = pocketedOwnGroup && !pocketedOpponentGroup;
            }

            var assignedCoupMixte = state.Phase == MatchPhase.GroupsAssigned
                && pocketedOwnGroup
                && pocketedOpponentGroup;
            var mixedDuringBonus = state.DeuxCoups != DeuxCoupsState.Inactive
                && assignedCoupMixte;
            var openTableMixedFamilies = state.IsTableOpen
                && hasPocketedSolids
                && hasPocketedStripes;

            if (state.DeuxCoups == DeuxCoupsState.TwoRemaining)
            {
                if (mixedDuringBonus)
                {
                    var incomingState = DeuxCoupsRule.Grant(resolvedState.AdvanceTurn());
                    return new ShotResolution(
                        incomingState,
                        foulResolution,
                        breakEvaluation: null,
                        requiresBreakFollowUp: false,
                        calledShotSucceeded: calledShotSucceeded,
                        shooterContinues: false,
                        turnAdvanced: true,
                        groupAssigned: groupAssigned);
                }

                if (openTableMixedFamilies)
                {
                    var incomingState = resolvedState.AdvanceTurn();
                    return new ShotResolution(
                        incomingState,
                        foulResolution,
                        breakEvaluation: null,
                        requiresBreakFollowUp: false,
                        calledShotSucceeded: calledShotSucceeded,
                        shooterContinues: false,
                        turnAdvanced: true,
                        groupAssigned: groupAssigned);
                }

                if (state.Phase == MatchPhase.GroupsAssigned
                    && pocketedOpponentGroup
                    && !pocketedOwnGroup)
                {
                    var incomingState = resolvedState.AdvanceTurn();
                    return new ShotResolution(
                        incomingState,
                        foulResolution,
                        breakEvaluation: null,
                        requiresBreakFollowUp: false,
                        calledShotSucceeded: calledShotSucceeded,
                        shooterContinues: false,
                        turnAdvanced: true,
                        groupAssigned: groupAssigned);
                }

                resolvedState = DeuxCoupsRule.Consume(resolvedState);
                return new ShotResolution(
                    resolvedState,
                    foulResolution,
                    breakEvaluation: null,
                    requiresBreakFollowUp: false,
                    calledShotSucceeded: calledShotSucceeded,
                    shooterContinues: true,
                    turnAdvanced: false,
                    groupAssigned: groupAssigned);
            }

            if (state.DeuxCoups == DeuxCoupsState.OneRemaining)
            {
                resolvedState = DeuxCoupsRule.Consume(resolvedState);
            }

            if (shooterContinues)
            {
                return new ShotResolution(
                    resolvedState,
                    foulResolution,
                    breakEvaluation: null,
                    requiresBreakFollowUp: false,
                    calledShotSucceeded: calledShotSucceeded,
                    shooterContinues: true,
                    turnAdvanced: false,
                    groupAssigned: groupAssigned);
            }

            resolvedState = resolvedState.AdvanceTurn();
            if (assignedCoupMixte)
            {
                resolvedState = DeuxCoupsRule.Grant(resolvedState);
            }

            return new ShotResolution(
                resolvedState,
                foulResolution,
                breakEvaluation: null,
                requiresBreakFollowUp: false,
                calledShotSucceeded: calledShotSucceeded,
                shooterContinues: false,
                turnAdvanced: true,
                groupAssigned: groupAssigned);
        }

        private static BallId[] CollectBallsToRespot(ShotFacts facts)
        {
            var result = new BallId[facts.BallsDrivenOffTable.Count];
            var count = 0;

            for (var index = 0; index < facts.BallsDrivenOffTable.Count; index++)
            {
                var ball = facts.BallsDrivenOffTable[index];
                if (ball.IsCueBall || ball.IsEightBall)
                {
                    continue;
                }

                result[count++] = ball;
            }

            if (count == result.Length)
            {
                return result;
            }

            var trimmed = new BallId[count];
            Array.Copy(result, trimmed, count);
            return trimmed;
        }

        private static void ClassifyPocketedGroups(
            ShotFacts facts,
            out bool hasSolids,
            out bool hasStripes,
            out BallId firstGroupedBall)
        {
            hasSolids = false;
            hasStripes = false;
            firstGroupedBall = default;
            var hasGroupedBall = false;

            for (var index = 0; index < facts.PocketedBalls.Count; index++)
            {
                var ball = facts.PocketedBalls[index];
                if (ball.Group == BallGroup.Solids)
                {
                    hasSolids = true;
                }
                else if (ball.Group == BallGroup.Stripes)
                {
                    hasStripes = true;
                }

                if (!hasGroupedBall && ball.Group != BallGroup.None)
                {
                    firstGroupedBall = ball;
                    hasGroupedBall = true;
                }
            }
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

            if (state.IsFinished)
            {
                throw new InvalidOperationException("Shots cannot be resolved after the match has finished.");
            }

            if (state.HasBallInHand)
            {
                throw new InvalidOperationException("Ball-in-hand placement must be completed before resolving a shot.");
            }

            if (intent.Player != state.CurrentPlayer)
            {
                throw new ArgumentException(
                    "Shot intent player must match the current match player.",
                    nameof(intent));
            }

            if ((intent.Direction.X == 0f && intent.Direction.Y == 0f)
                || float.IsNaN(intent.NormalizedPower)
                || float.IsInfinity(intent.NormalizedPower)
                || intent.NormalizedPower <= 0f
                || intent.NormalizedPower > 1f
                || (intent.CalledShot.HasValue && !intent.CalledShot.Value.IsValid))
            {
                throw new ArgumentException("Shot intent must be a valid constructed intent.", nameof(intent));
            }

            ValidateObservedBallsExistBeforeShot(facts, tableBeforeShot);
        }

        private static void ValidateObservedBallsExistBeforeShot(
            ShotFacts facts,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            if (facts.FirstObjectBallContact.HasValue)
            {
                ValidateObservedObjectBall(facts.FirstObjectBallContact.Value, tableBeforeShot, nameof(facts));
            }

            for (var index = 0; index < facts.PocketedBalls.Count; index++)
            {
                var ball = facts.PocketedBalls[index];
                ValidateObservedObjectBall(ball, tableBeforeShot, nameof(facts));
            }

            for (var index = 0; index < facts.RailContactBallsAfterFirstObjectBallContact.Count; index++)
            {
                ValidateObservedObjectBall(
                    facts.RailContactBallsAfterFirstObjectBallContact[index],
                    tableBeforeShot,
                    nameof(facts));
            }

            for (var index = 0; index < facts.BallsDrivenOffTable.Count; index++)
            {
                ValidateObservedObjectBall(facts.BallsDrivenOffTable[index], tableBeforeShot, nameof(facts));
            }
        }

        private static void ValidateObservedObjectBall(
            BallId ball,
            ObjectBallTableSnapshot tableBeforeShot,
            string parameterName)
        {
            if (!ball.IsCueBall && !tableBeforeShot.Contains(ball))
            {
                throw new ArgumentException(
                    $"Observed object ball {ball.Number} must exist in the pre-shot table snapshot.",
                    parameterName);
            }
        }

        private static bool WasCalledShotSuccessful(
            MatchState state,
            ShotIntent intent,
            ShotFacts facts,
            ObjectBallTableSnapshot tableBeforeShot)
        {
            if (!intent.CalledShot.HasValue)
            {
                return false;
            }

            var calledShot = intent.CalledShot.Value;
            if (!facts.WasPocketedIn(calledShot.ObjectBall, calledShot.Pocket))
            {
                return false;
            }

            if (state.IsTableOpen)
            {
                return calledShot.ObjectBall.Group == BallGroup.Solids
                    || calledShot.ObjectBall.Group == BallGroup.Stripes;
            }

            if (state.Phase != MatchPhase.GroupsAssigned)
            {
                return false;
            }

            var shooterGroup = state.GetPlayer(state.CurrentPlayer).Group;
            if (tableBeforeShot.HasRemainingBalls(shooterGroup))
            {
                return calledShot.ObjectBall.Group == shooterGroup;
            }

            return calledShot.ObjectBall.IsEightBall;
        }
    }
}
