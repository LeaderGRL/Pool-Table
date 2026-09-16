using NUnit.Framework;
using PoolTable.Presentation.Audio;

namespace PoolTable.Tests.EditMode
{
    public sealed class ImpactLayerAudioModelTests
    {
        [Test]
        public void RailImpact_BelowAudibleEnergy_IsSilent()
        {
            var cue = ImpactLayerAudioModel.EvaluateRail(
                ImpactLayerAudioModel.MinimumAudibleRailEnergyJoules * 0.5f);

            Assert.That(cue.ShouldPlay, Is.False);
            Assert.That(cue.Volume, Is.Zero);
        }

        [Test]
        public void RailImpact_StrongerEnergy_IncreasesVolumeAndLowersPitch()
        {
            var mediumCue = ImpactLayerAudioModel.EvaluateRail(0.02f);
            var hardCue = ImpactLayerAudioModel.EvaluateRail(0.12f);

            Assert.That(mediumCue.ShouldPlay, Is.True);
            Assert.That(hardCue.Volume, Is.GreaterThan(mediumCue.Volume));
            Assert.That(hardCue.Pitch, Is.LessThan(mediumCue.Pitch));
        }

        [Test]
        public void CueStrike_PowerControlsLayerIntensity()
        {
            var weakCue = ImpactLayerAudioModel.EvaluateCueStrike(0.15f);
            var hardCue = ImpactLayerAudioModel.EvaluateCueStrike(1f);

            Assert.That(weakCue.ShouldPlay, Is.True);
            Assert.That(hardCue.Volume, Is.GreaterThan(weakCue.Volume));
            Assert.That(hardCue.Volume, Is.EqualTo(1f).Within(0.000001f));
        }

        [Test]
        public void PocketCapture_UsesDistinctLowPitchLayer()
        {
            var cue = ImpactLayerAudioModel.PocketCapture;

            Assert.That(cue.ShouldPlay, Is.True);
            Assert.That(cue.Volume, Is.InRange(0.3f, 0.6f));
            Assert.That(cue.Pitch, Is.LessThan(0.9f));
        }
    }
}
