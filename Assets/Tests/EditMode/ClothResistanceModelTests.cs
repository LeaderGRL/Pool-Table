using NUnit.Framework;
using PoolTable.Physics.Cloth;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class ClothResistanceModelTests
    {
        [Test]
        public void CalculateVelocityAfterStep_ReducesPlanarSpeedAndPreservesVerticalVelocity()
        {
            var velocity = new Vector3(3f, 0.4f, 4f);

            var result = ClothResistanceModel.CalculateVelocityAfterStep(velocity, 0.5f);

            var expectedPlanarSpeed =
                5f - (BilliardsSimulationConfiguration.ClothRollingDecelerationMetersPerSecondSquared * 0.5f);
            Assert.That(new Vector2(result.x, result.z).magnitude, Is.EqualTo(expectedPlanarSpeed).Within(0.000001f));
            Assert.That(result.y, Is.EqualTo(velocity.y).Within(0.000001f));
            Assert.That(Vector2.Dot(new Vector2(result.x, result.z), new Vector2(velocity.x, velocity.z)), Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateVelocityAfterStep_StopsWithoutReversingAtLowSpeed()
        {
            var velocity = new Vector3(0.001f, -0.2f, 0f);

            var result = ClothResistanceModel.CalculateVelocityAfterStep(velocity, 1f);

            Assert.That(result.x, Is.Zero.Within(0.000001f));
            Assert.That(result.z, Is.Zero.Within(0.000001f));
            Assert.That(result.y, Is.EqualTo(velocity.y).Within(0.000001f));
        }

        [Test]
        public void CalculateVelocityAfterStep_IsIndependentOfFixedStepSize()
        {
            var velocity = new Vector3(2f, 0f, 1f);

            var oneStep = ClothResistanceModel.CalculateVelocityAfterStep(velocity, 0.02f);
            var firstHalfStep = ClothResistanceModel.CalculateVelocityAfterStep(velocity, 0.01f);
            var twoHalfSteps = ClothResistanceModel.CalculateVelocityAfterStep(firstHalfStep, 0.01f);

            Assert.That(Vector3.Distance(twoHalfSteps, oneStep), Is.LessThan(0.000001f));
        }
    }
}
