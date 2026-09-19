using System;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;

namespace PoolTable.Presentation.UI
{
    public sealed class MatchUiSession
    {
        private readonly string playerOneName;
        private readonly string playerTwoName;
        private MatchState state;
        private int openingResolutionFrame = -1;

        private MatchUiSession(string playerOneName, string playerTwoName, MatchState state)
        {
            this.playerOneName = playerOneName;
            this.playerTwoName = playerTwoName;
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            Model = BuildModel();
        }

        public MatchState State => state;

        public MatchPresentationModel Model { get; private set; }

        public int StartingPlayerIndex => state.CurrentPlayer == MatchPlayerId.PlayerOne ? 0 : 1;

        public static MatchUiSession Start(string playerOneName, string playerTwoName, int breakerIndex)
        {
            var normalizedPlayerOne = MatchNameRules.NormalizeForMatch(playerOneName, "PLAYER 1");
            var normalizedPlayerTwo = MatchNameRules.NormalizeForMatch(playerTwoName, "PLAYER 2");
            var breaker = MatchBreakerSelector.FromRandomIndex(breakerIndex);

            return new MatchUiSession(
                normalizedPlayerOne,
                normalizedPlayerTwo,
                MatchState.CreateInitial(breaker));
        }

        public void ResolveOpeningBreak(int frameCount)
        {
            if (state.Phase != MatchPhase.Break)
            {
                return;
            }

            state = OpenTableRule.EnterAfterBreak(state);
            openingResolutionFrame = frameCount;
            Model = BuildModel();
        }

        public void ApplyActivePlayer(int playerIndex, int frameCount)
        {
            var player = MatchBreakerSelector.FromRandomIndex(playerIndex);
            if (player == state.CurrentPlayer)
            {
                return;
            }

            if (state.Phase == MatchPhase.OpenTable
                && state.TourNumber == 1
                && openingResolutionFrame == frameCount)
            {
                state = OpenTableRule.PassOpeningControl(state);
            }
            else
            {
                state = state.WithCurrentPlayer(player);
            }

            Model = BuildModel();
        }

        private MatchPresentationModel BuildModel()
        {
            return MatchPresentationModel.FromMatchState(playerOneName, playerTwoName, state);
        }
    }
}
