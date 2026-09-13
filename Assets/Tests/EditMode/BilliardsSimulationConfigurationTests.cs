using NUnit.Framework;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class BilliardsSimulationConfigurationTests
    {
        [Test]
        public void Configuration_DefinesRegulationScaleBallRigidbodyBaseline()
        {
            Assert.That(BilliardsSimulationConfiguration.BallMassKilograms, Is.EqualTo(0.17f).Within(0.000001f));
            Assert.That(BilliardsSimulationConfiguration.BallLinearDamping, Is.EqualTo(0.35f).Within(0.000001f));
            Assert.That(BilliardsSimulationConfiguration.BallAngularDamping, Is.EqualTo(0.2f).Within(0.000001f));
            Assert.That(
                BilliardsSimulationConfiguration.BallCollisionDetectionMode,
                Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
            Assert.That(
                BilliardsSimulationConfiguration.BallInterpolation,
                Is.EqualTo(RigidbodyInterpolation.Interpolate));
        }

        [Test]
        public void Configuration_DefinesHighResolutionSimulationBaseline()
        {
            Assert.That(BilliardsSimulationConfiguration.DefaultContactOffsetMeters, Is.EqualTo(0.001f).Within(0.000001f));
            Assert.That(BilliardsSimulationConfiguration.GlobalSleepThreshold, Is.EqualTo(0.001f).Within(0.000001f));
            Assert.That(BilliardsSimulationConfiguration.DefaultSolverIterations, Is.EqualTo(12));
            Assert.That(BilliardsSimulationConfiguration.DefaultSolverVelocityIterations, Is.EqualTo(4));
            Assert.That(BilliardsSimulationConfiguration.FixedTimestepSeconds, Is.EqualTo(0.005f).Within(0.000001f));
        }
    }
}
