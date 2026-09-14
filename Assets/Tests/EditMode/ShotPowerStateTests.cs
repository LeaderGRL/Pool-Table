using System;
using NUnit.Framework;
using PoolTable.Gameplay.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class ShotPowerStateTests
    {
        [Test]
        public void AdjustPullback_MapsDistanceToNormalizedPower()
        {
            var state = new ShotPowerState(0.4f);

            state.AdjustPullback(0.1f);

            Assert.That(state.PullbackMeters, Is.EqualTo(0.1f).Within(0.000001f));
            Assert.That(state.NormalizedPower, Is.EqualTo(0.25f).Within(0.000001f));
            Assert.That(state.HasUsablePower, Is.True);
        }

        [Test]
        public void AdjustPullback_ClampsToValidRange()
        {
            var state = new ShotPowerState(0.35f);

            state.AdjustPullback(1f);

            Assert.That(state.PullbackMeters, Is.EqualTo(0.35f).Within(0.000001f));
            Assert.That(state.NormalizedPower, Is.EqualTo(1f).Within(0.000001f));

            state.AdjustPullback(-1f);

            Assert.That(state.PullbackMeters, Is.Zero.Within(0.000001f));
            Assert.That(state.NormalizedPower, Is.Zero.Within(0.000001f));
            Assert.That(state.HasUsablePower, Is.False);
        }

        [Test]
        public void Reset_ClearsAccumulatedPower()
        {
            var state = new ShotPowerState(0.35f);
            state.AdjustPullback(0.2f);

            state.Reset();

            Assert.That(state.PullbackMeters, Is.Zero);
            Assert.That(state.NormalizedPower, Is.Zero);
            Assert.That(state.HasUsablePower, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Constructor_RejectsInvalidMaximumPullback(float maximumPullbackMeters)
        {
            Assert.That(
                () => new ShotPowerState(maximumPullbackMeters),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void AdjustPullback_RejectsNonFiniteDelta(float deltaMeters)
        {
            var state = new ShotPowerState(0.35f);

            Assert.That(
                () => state.AdjustPullback(deltaMeters),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
