using System;
using PoolTable.Core.Match;

namespace PoolTable.Presentation.UI
{
    public enum MatchPresentationScreen
    {
        Setup = 0,
        Match = 1,
    }

    public readonly struct MatchPlayerPresentation
    {
        public MatchPlayerPresentation(MatchPlayerId id, string displayName, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Player display name cannot be empty.", nameof(displayName));
            }

            Id = id;
            DisplayName = displayName;
            IsActive = isActive;
        }

        public MatchPlayerId Id { get; }

        public string DisplayName { get; }

        public bool IsActive { get; }
    }

    public sealed class MatchPresentationModel
    {
        private MatchPresentationModel(
            MatchPresentationScreen screen,
            MatchPlayerPresentation playerOne,
            MatchPlayerPresentation playerTwo,
            int tourNumber,
            MatchPhase phase)
        {
            Screen = screen;
            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
            TourNumber = tourNumber;
            Phase = phase;
        }

        public MatchPresentationScreen Screen { get; }

        public MatchPlayerPresentation PlayerOne { get; }

        public MatchPlayerPresentation PlayerTwo { get; }

        public int TourNumber { get; }

        public MatchPhase Phase { get; }

        public MatchPlayerId ActivePlayer => PlayerOne.IsActive
            ? MatchPlayerId.PlayerOne
            : MatchPlayerId.PlayerTwo;

        public bool IsBreak => Screen == MatchPresentationScreen.Match && Phase == MatchPhase.Break;

        public static MatchPresentationModel CreateSetup(string playerOneName = "PLAYER 1", string playerTwoName = "PLAYER 2")
        {
            return new MatchPresentationModel(
                MatchPresentationScreen.Setup,
                new MatchPlayerPresentation(MatchPlayerId.PlayerOne, playerOneName, false),
                new MatchPlayerPresentation(MatchPlayerId.PlayerTwo, playerTwoName, false),
                0,
                MatchPhase.Break);
        }

        public static MatchPresentationModel FromMatchState(
            string playerOneName,
            string playerTwoName,
            MatchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new MatchPresentationModel(
                MatchPresentationScreen.Match,
                new MatchPlayerPresentation(
                    MatchPlayerId.PlayerOne,
                    playerOneName,
                    state.CurrentPlayer == MatchPlayerId.PlayerOne),
                new MatchPlayerPresentation(
                    MatchPlayerId.PlayerTwo,
                    playerTwoName,
                    state.CurrentPlayer == MatchPlayerId.PlayerTwo),
                state.TourNumber,
                state.Phase);
        }
    }

    public static class MatchBreakerSelector
    {
        public static MatchPlayerId FromRandomIndex(int index)
        {
            return index switch
            {
                0 => MatchPlayerId.PlayerOne,
                1 => MatchPlayerId.PlayerTwo,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Breaker index must be 0 or 1."),
            };
        }
    }
}
