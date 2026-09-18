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
            bool grantsTwoShotEntitlement)
        {
            State = state;
            FoulResolution = foulResolution;
            BreakEvaluation = breakEvaluation;
            RequiresBreakFollowUp = requiresBreakFollowUp;
            CalledShotSucceeded = calledShotSucceeded;
            ShooterContinues = shooterContinues;
            TurnAdvanced = turnAdvanced;
            GroupAssigned = groupAssigned;
            GrantsTwoShotEntitlement = grantsTwoShotEntitlement;
        }

        public MatchState State { get; }

        public FoulResolution FoulResolution { get; }

        public BreakEvaluation? BreakEvaluation { get; }

        public bool RequiresBreakFollowUp { get; }

        public bool CalledShotSucceeded { get; }

        public bool ShooterContinues { get; }

        public bool TurnAdvanced { get; }

        public bool GroupAssigned { get; }

        public bool GrantsTwoShotEntitlement { get; }

        public bool MatchFinished => State.IsFinished;
    }
}
