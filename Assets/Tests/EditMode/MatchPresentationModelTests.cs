using NUnit.Framework;
using PoolTable.Core.Match;
using PoolTable.Presentation.UI;

namespace PoolTable.Tests.EditMode
{
    public sealed class MatchPresentationModelTests
    {
        [Test]
        public void SanitizeInput_EnforcesAllowedCharactersAndMaximumLength()
        {
            Assert.That(
                MatchNameRules.SanitizeInput("Al!ce_01-XYZ 789"),
                Is.EqualTo("Alce_01-XYZ "));
        }

        [TestCase(null, "PLAYER 1")]
        [TestCase("", "PLAYER 1")]
        [TestCase("   ", "PLAYER 1")]
        [TestCase("  Jordan  ", "Jordan")]
        public void NormalizeForMatch_UsesFallbackForBlankNames(string value, string expected)
        {
            Assert.That(MatchNameRules.NormalizeForMatch(value, "PLAYER 1"), Is.EqualTo(expected));
        }

        [Test]
        public void BreakerSelector_MapsBothUniformRandomOutcomesToDistinctPlayers()
        {
            Assert.That(MatchBreakerSelector.FromRandomIndex(0), Is.EqualTo(MatchPlayerId.PlayerOne));
            Assert.That(MatchBreakerSelector.FromRandomIndex(1), Is.EqualTo(MatchPlayerId.PlayerTwo));
        }

        [Test]
        public void FromMatchState_ProjectsActivePlayerAndAuthoritativeTour()
        {
            var state = MatchState.CreateInitial(MatchPlayerId.PlayerTwo);

            var model = MatchPresentationModel.FromMatchState("ALPHA", "BRAVO", state);

            Assert.That(model.Screen, Is.EqualTo(MatchPresentationScreen.Match));
            Assert.That(model.ActivePlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(model.PlayerOne.IsActive, Is.False);
            Assert.That(model.PlayerTwo.IsActive, Is.True);
            Assert.That(model.TourNumber, Is.EqualTo(0));
            Assert.That(model.IsBreak, Is.True);
        }
    }
}
