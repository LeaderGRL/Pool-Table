using NUnit.Framework;
using PoolTable.Presentation.Camera;

namespace PoolTable.Tests.EditMode
{
    public sealed class CameraImpactImpulseModelTests
    {
        [Test]
        public void CueStrike_BelowThreshold_IsSilent()
        {
            var cue = CameraImpactImpulseModel.EvaluateCueStrike(
                CameraImpactImpulseModel.MinimumCueStrikePower * 0.5f);

            Assert.That(cue.ShouldPlay, Is.False);
        }

        [Test]
        public void CueStrike_StrongerPower_IncreasesButCapsImpulse()
        {
            var medium = CameraImpactImpulseModel.EvaluateCueStrike(0.45f);
            var hard = CameraImpactImpulseModel.EvaluateCueStrike(1f);
            var overRange = CameraImpactImpulseModel.EvaluateCueStrike(5f);

            Assert.That(medium.ShouldPlay, Is.True);
            Assert.That(hard.PositionAmplitudeMeters, Is.GreaterThan(medium.PositionAmplitudeMeters));
            Assert.That(hard.RotationAmplitudeDegrees, Is.GreaterThan(medium.RotationAmplitudeDegrees));
            Assert.That(overRange.PositionAmplitudeMeters, Is.EqualTo(hard.PositionAmplitudeMeters).Within(0.000001f));
            Assert.That(overRange.RotationAmplitudeDegrees, Is.EqualTo(hard.RotationAmplitudeDegrees).Within(0.000001f));
            Assert.That(hard.PositionAmplitudeMeters, Is.LessThanOrEqualTo(0.012f));
            Assert.That(hard.RotationAmplitudeDegrees, Is.LessThanOrEqualTo(0.42f));
        }

        [Test]
        public void RailImpact_WeakEnergyIsSilentAndHardEnergyIsBounded()
        {
            var weak = CameraImpactImpulseModel.EvaluateRailImpact(
                CameraImpactImpulseModel.MinimumRailImpactEnergyJoules * 0.5f);
            var hard = CameraImpactImpulseModel.EvaluateRailImpact(1f);

            Assert.That(weak.ShouldPlay, Is.False);
            Assert.That(hard.ShouldPlay, Is.True);
            Assert.That(hard.PositionAmplitudeMeters, Is.LessThanOrEqualTo(0.008f));
            Assert.That(hard.RotationAmplitudeDegrees, Is.LessThanOrEqualTo(0.3f));
        }
    }
}
