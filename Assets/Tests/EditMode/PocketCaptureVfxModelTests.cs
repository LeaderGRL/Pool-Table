using NUnit.Framework;
using PoolTable.Presentation.Vfx;

namespace PoolTable.Tests.EditMode
{
    public sealed class PocketCaptureVfxModelTests
    {
        [Test]
        public void Evaluate_ReturnsBriefBoundedPocketFeedback()
        {
            var cue = PocketCaptureVfxModel.Evaluate();

            Assert.That(cue.ShouldPlay, Is.True);
            Assert.That(cue.DustParticleCount, Is.InRange(12, 20));
            Assert.That(cue.DustLifetimeSeconds, Is.LessThanOrEqualTo(0.3f));
            Assert.That(cue.DustSizeMeters, Is.LessThanOrEqualTo(0.016f));
            Assert.That(cue.AccentParticleCount, Is.InRange(4, 8));
            Assert.That(cue.AccentLifetimeSeconds, Is.LessThanOrEqualTo(0.12f));
            Assert.That(cue.AccentSizeMeters, Is.LessThanOrEqualTo(0.01f));
        }
    }
}
