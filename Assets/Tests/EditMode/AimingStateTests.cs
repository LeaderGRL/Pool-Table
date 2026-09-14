using System;
using NUnit.Framework;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Aiming;

namespace PoolTable.Tests.EditMode
{
    public sealed class AimingStateTests
    {
        [Test]
        public void Constructor_PreservesNormalizedShotDirection()
        {
            var state = new AimingState(new ShotDirection(3f, 4f));

            Assert.That(state.Direction.X, Is.EqualTo(0.6f).Within(0.000001f));
            Assert.That(state.Direction.Y, Is.EqualTo(0.8f).Within(0.000001f));
            AssertDirectionIsNormalized(state.Direction);
        }

        [TestCase(90f, 1f, 0f)]
        [TestCase(-90f, -1f, 0f)]
        public void RotateDegrees_RotatesAroundTableVerticalAxis(
            float yawDegrees,
            float expectedX,
            float expectedY)
        {
            var state = new AimingState(new ShotDirection(0f, 1f));

            state.RotateDegrees(yawDegrees);

            Assert.That(state.Direction.X, Is.EqualTo(expectedX).Within(0.000001f));
            Assert.That(state.Direction.Y, Is.EqualTo(expectedY).Within(0.000001f));
            AssertDirectionIsNormalized(state.Direction);
        }

        [Test]
        public void RotateDegrees_ZeroDeltaKeepsDirectionUnchanged()
        {
            var initialDirection = new ShotDirection(1f, 1f);
            var state = new AimingState(initialDirection);

            state.RotateDegrees(0f);

            Assert.That(state.Direction, Is.EqualTo(initialDirection));
        }

        [Test]
        public void RotateDegrees_RepeatedUpdatesRemainNormalized()
        {
            var state = new AimingState(new ShotDirection(0f, 1f));

            for (var index = 0; index < 1000; index++)
            {
                state.RotateDegrees(0.36f);
            }

            AssertDirectionIsNormalized(state.Direction);
            Assert.That(state.Direction.X, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(state.Direction.Y, Is.EqualTo(1f).Within(0.0001f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void RotateDegrees_RejectsNonFiniteDelta(float yawDegrees)
        {
            var state = new AimingState(new ShotDirection(0f, 1f));

            Assert.That(
                () => state.RotateDegrees(yawDegrees),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static void AssertDirectionIsNormalized(ShotDirection direction)
        {
            var magnitudeSquared = (direction.X * direction.X) + (direction.Y * direction.Y);
            Assert.That(magnitudeSquared, Is.EqualTo(1f).Within(0.00001f));
        }
    }
}
