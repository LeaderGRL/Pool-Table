using NUnit.Framework;
using PoolTable.Presentation.Vfx;

namespace PoolTable.Tests.EditMode
{
    public sealed class CueImpactVfxModelTests
    {
        [Test]
        public void Evaluate_BelowVisiblePower_IsSilent()
        {
            var cue = CueImpactVfxModel.Evaluate(CueImpactVfxModel.MinimumVisiblePower * 0.5f);

            Assert.That(cue.ShouldPlay, Is.False);
            Assert.That(cue.ChalkParticleCount, Is.Zero);
            Assert.That(cue.AccentParticleCount, Is.Zero);
        }

        [Test]
        public void Evaluate_StrongerPower_IncreasesBothVisualLayers()
        {
            var weakCue = CueImpactVfxModel.Evaluate(0.2f);
            var hardCue = CueImpactVfxModel.Evaluate(1f);

            Assert.That(weakCue.ShouldPlay, Is.True);
            Assert.That(hardCue.ChalkParticleCount, Is.GreaterThan(weakCue.ChalkParticleCount));
            Assert.That(hardCue.ChalkSpeedMetersPerSecond, Is.GreaterThan(weakCue.ChalkSpeedMetersPerSecond));
            Assert.That(hardCue.ChalkSizeMeters, Is.GreaterThan(weakCue.ChalkSizeMeters));
            Assert.That(hardCue.AccentParticleCount, Is.GreaterThan(weakCue.AccentParticleCount));
            Assert.That(hardCue.AccentSpeedMetersPerSecond, Is.GreaterThan(weakCue.AccentSpeedMetersPerSecond));
            Assert.That(hardCue.AccentSizeMeters, Is.GreaterThan(weakCue.AccentSizeMeters));
        }

        [Test]
        public void Evaluate_FullPower_RemainsWithinShortSubtleBounds()
        {
            var cue = CueImpactVfxModel.Evaluate(1f);

            Assert.That(cue.ChalkParticleCount, Is.InRange(4, 10));
            Assert.That(cue.ChalkLifetimeSeconds, Is.InRange(0.1f, 0.25f));
            Assert.That(cue.ChalkSizeMeters, Is.LessThanOrEqualTo(0.007f));
            Assert.That(cue.AccentParticleCount, Is.InRange(2, 5));
            Assert.That(cue.AccentLifetimeSeconds, Is.LessThanOrEqualTo(0.08f));
            Assert.That(cue.AccentSizeMeters, Is.LessThanOrEqualTo(0.005f));
        }
    }
}
