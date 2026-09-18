using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;

namespace PoolTable.Gameplay.Match
{
    public sealed class ShotResolution
    {
        internal ShotResolution(
            MatchState state,
            FoulResolution foulResolution,
            BreakEvaluation? breakEvaluation,
            bool requiresBreakFollowUp,
            bool calledShotSucceeded,
            bool shooterContinues,
            bool turnAdvanced,
            bool groupAssigned,
            IEnumerable<BallId> ballsToRespot = null)
        {
            State = state;
            FoulResolution = foulResolution;
            BreakEvaluation = breakEvaluation;
            RequiresBreakFollowUp = requiresBreakFollowUp;
            CalledShotSucceeded = calledShotSucceeded;
            ShooterContinues = shooterContinues;
            TurnAdvanced = turnAdvanced;
            GroupAssigned = groupAssigned;
            BallsToRespot = new List<BallId>(ballsToRespot ?? Array.Empty<BallId>()).AsReadOnly();
        }

        public MatchState State { get; }

        public FoulResolution FoulResolution { get; }

        public BreakEvaluation? BreakEvaluation { get; }

        public bool RequiresBreakFollowUp { get; }

        public bool CalledShotSucceeded { get; }

        public bool ShooterContinues { get; }

        public bool TurnAdvanced { get; }

        public bool GroupAssigned { get; }

        public bool GrantsTwoShotEntitlement => State.DeuxCoups == DeuxCoupsState.TwoRemaining;

        public ReadOnlyCollection<BallId> BallsToRespot { get; }

        public bool MatchFinished => State.IsFinished;
    }
}
