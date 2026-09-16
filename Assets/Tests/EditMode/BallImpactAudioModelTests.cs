using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Presentation.Audio;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class BallImpactAudioModelTests
    {
        private const float RegulationBallMassKilograms = 0.17f;

        [Test]
        public void Evaluate_UsesReducedMassCollisionEnergy()
        {
            var cue = BallImpactAudioModel.Evaluate(
                1f,
                RegulationBallMassKilograms,
                RegulationBallMassKilograms,
                3);

            Assert.That(cue.ImpactEnergyJoules, Is.EqualTo(0.0425f).Within(0.000001f));
            Assert.That(cue.ShouldPlay, Is.True);
        }

        [Test]
        public void CalculateNormalClosingSpeed_IgnoresTangentialVelocity()
        {
            var closingSpeed = BallImpactAudioModel.CalculateNormalClosingSpeed(
                new Vector3(1f, 0f, 5f),
                Vector3.right);

            Assert.That(closingSpeed, Is.EqualTo(1f).Within(0.000001f));
        }

        [Test]
        public void CalculateNormalClosingSpeed_ReturnsZeroForPurelyTangentialContact()
        {
            var closingSpeed = BallImpactAudioModel.CalculateNormalClosingSpeed(
                new Vector3(0f, 0f, 5f),
                Vector3.right);

            Assert.That(closingSpeed, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void CalculateNormalClosingSpeed_IsIndependentOfNormalOrientation()
        {
            var forward = BallImpactAudioModel.CalculateNormalClosingSpeed(
                Vector3.right * 2f,
                Vector3.right);
            var reversed = BallImpactAudioModel.CalculateNormalClosingSpeed(
                Vector3.right * 2f,
                Vector3.left);

            Assert.That(forward, Is.EqualTo(2f).Within(0.000001f));
            Assert.That(reversed, Is.EqualTo(forward).Within(0.000001f));
        }

        [Test]
        public void Evaluate_LeavesVeryWeakContactsSilent()
        {
            var cue = BallImpactAudioModel.Evaluate(
                0.1f,
                RegulationBallMassKilograms,
                RegulationBallMassKilograms,
                3);

            Assert.That(cue.ImpactEnergyJoules, Is.LessThan(BallImpactAudioModel.MinimumAudibleEnergyJoules));
            Assert.That(cue.ShouldPlay, Is.False);
            Assert.That(cue.ClipIndex, Is.EqualTo(-1));
            Assert.That(cue.Volume, Is.Zero);
        }

        [Test]
        public void Evaluate_StrongerImpactsIncreaseIntensityVolumeAndClipBand()
        {
            var mediumCue = BallImpactAudioModel.Evaluate(
                1f,
                RegulationBallMassKilograms,
                RegulationBallMassKilograms,
                3);
            var hardCue = BallImpactAudioModel.Evaluate(
                2.5f,
                RegulationBallMassKilograms,
                RegulationBallMassKilograms,
                3);

            Assert.That(hardCue.ImpactEnergyJoules, Is.GreaterThan(mediumCue.ImpactEnergyJoules));
            Assert.That(hardCue.Intensity, Is.GreaterThan(mediumCue.Intensity));
            Assert.That(hardCue.Volume, Is.GreaterThan(mediumCue.Volume));
            Assert.That(hardCue.ClipIndex, Is.GreaterThan(mediumCue.ClipIndex));
        }

        [Test]
        public void Evaluate_SaturatesPredictablyForHardImpacts()
        {
            var cue = BallImpactAudioModel.Evaluate(
                6.6666667f,
                RegulationBallMassKilograms,
                RegulationBallMassKilograms,
                3);

            Assert.That(cue.ImpactEnergyJoules, Is.GreaterThan(BallImpactAudioModel.FullScaleEnergyJoules));
            Assert.That(cue.Intensity, Is.EqualTo(1f));
            Assert.That(cue.Volume, Is.EqualTo(1f));
            Assert.That(cue.ClipIndex, Is.EqualTo(2));
        }

        [TestCase(0f, 0.17f, 0.17f, 3)]
        [TestCase(float.NaN, 0.17f, 0.17f, 3)]
        [TestCase(1f, 0f, 0.17f, 3)]
        [TestCase(1f, 0.17f, 0f, 3)]
        [TestCase(1f, 0.17f, 0.17f, 0)]
        public void Evaluate_InvalidOrUnplayableInputReturnsSilence(
            float relativeSpeedMetersPerSecond,
            float firstBallMassKilograms,
            float secondBallMassKilograms,
            int clipCount)
        {
            var cue = BallImpactAudioModel.Evaluate(
                relativeSpeedMetersPerSecond,
                firstBallMassKilograms,
                secondBallMassKilograms,
                clipCount);

            Assert.That(cue.ShouldPlay, Is.False);
        }

        [Test]
        public void IsPrimaryEmitter_SelectsExactlyOneBallForEachTypedPair()
        {
            var cueBall = new BallId(0);
            var oneBall = new BallId(1);

            Assert.That(BallImpactAudioModel.IsPrimaryEmitter(cueBall, oneBall), Is.True);
            Assert.That(BallImpactAudioModel.IsPrimaryEmitter(oneBall, cueBall), Is.False);
            Assert.That(BallImpactAudioModel.IsPrimaryEmitter(cueBall, cueBall), Is.False);
        }
    }
}
