using NUnit.Framework;
using PoolTable.Core.Shots;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Cue;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class CueBallStrikeModelTests
    {
        [Test]
        public void CalculateVelocityChange_CenteredHitAddsOnlyLinearVelocity()
        {
            var result = CueBallStrikeModel.CalculateVelocityChange(
                Vector3.right,
                2f,
                default,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(result.LinearVelocityChange, Is.EqualTo(Vector3.right * 2f));
            Assert.That(result.AngularVelocityChange, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CalculateVelocityChange_PositiveVerticalOffsetProducesFollowSpin()
        {
            const float speedChange = 2f;
            var result = CueBallStrikeModel.CalculateVelocityChange(
                Vector3.right,
                speedChange,
                new CueBallSpin(0f, 1f),
                BilliardsPhysicalSpecification.BallRadiusMeters);
            var expectedAngularSpeed = ExpectedMaximumSpinAngularSpeed(speedChange);

            Assert.That(result.AngularVelocityChange.x, Is.Zero.Within(0.000001f));
            Assert.That(result.AngularVelocityChange.y, Is.Zero.Within(0.000001f));
            Assert.That(result.AngularVelocityChange.z, Is.EqualTo(-expectedAngularSpeed).Within(0.0001f));
        }

        [Test]
        public void CalculateVelocityChange_NegativeVerticalOffsetProducesDrawSpin()
        {
            const float speedChange = 2f;
            var result = CueBallStrikeModel.CalculateVelocityChange(
                Vector3.right,
                speedChange,
                new CueBallSpin(0f, -1f),
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(
                result.AngularVelocityChange.z,
                Is.EqualTo(ExpectedMaximumSpinAngularSpeed(speedChange)).Within(0.0001f));
        }

        [Test]
        public void CalculateVelocityChange_PositiveSideOffsetProducesPositiveVerticalSpin()
        {
            const float speedChange = 2f;
            var result = CueBallStrikeModel.CalculateVelocityChange(
                Vector3.right,
                speedChange,
                new CueBallSpin(1f, 0f),
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(
                result.AngularVelocityChange.y,
                Is.EqualTo(ExpectedMaximumSpinAngularSpeed(speedChange)).Within(0.0001f));
            Assert.That(result.AngularVelocityChange.x, Is.Zero.Within(0.000001f));
            Assert.That(result.AngularVelocityChange.z, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void CalculateVelocityChange_FollowSpinRotatesWithShotDirection()
        {
            const float speedChange = 2f;
            var result = CueBallStrikeModel.CalculateVelocityChange(
                Vector3.forward,
                speedChange,
                new CueBallSpin(0f, 1f),
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(
                result.AngularVelocityChange.x,
                Is.EqualTo(ExpectedMaximumSpinAngularSpeed(speedChange)).Within(0.0001f));
            Assert.That(result.AngularVelocityChange.y, Is.Zero.Within(0.000001f));
            Assert.That(result.AngularVelocityChange.z, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void CalculateVelocityChange_IgnoresVerticalComponentOfShotDirection()
        {
            var result = CueBallStrikeModel.CalculateVelocityChange(
                new Vector3(2f, 5f, 0f),
                1f,
                default,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(result.LinearVelocityChange, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void CalculateVelocityChange_RejectsInvalidPhysicalInputs()
        {
            Assert.Throws<System.ArgumentException>(() => CueBallStrikeModel.CalculateVelocityChange(
                Vector3.up,
                1f,
                default,
                BilliardsPhysicalSpecification.BallRadiusMeters));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CueBallStrikeModel.CalculateVelocityChange(
                Vector3.right,
                0f,
                default,
                BilliardsPhysicalSpecification.BallRadiusMeters));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CueBallStrikeModel.CalculateVelocityChange(
                Vector3.right,
                1f,
                default,
                0f));
        }

        private static float ExpectedMaximumSpinAngularSpeed(float speedChangeMetersPerSecond)
        {
            return BilliardsSimulationConfiguration.CueTipMaximumContactOffsetRatio
                * speedChangeMetersPerSecond
                / (0.4f * BilliardsPhysicalSpecification.BallRadiusMeters);
        }
    }
}
