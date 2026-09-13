using NUnit.Framework;
using PoolTable.Physics.Cloth;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class ClothResistanceModelTests
    {
        [Test]
        public void CalculateMotionAfterStep_SlidingBallReducesSlipAndDevelopsRollingSpin()
        {
            var linearVelocity = new Vector3(2f, 0.4f, 0f);
            var angularVelocity = Vector3.zero;
            var initialSlip = ClothResistanceModel.CalculateContactPointSlipVelocity(
                linearVelocity,
                angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                linearVelocity,
                angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                0.05f);
            var resultSlip = ClothResistanceModel.CalculateContactPointSlipVelocity(
                result.LinearVelocity,
                result.AngularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(result.LinearVelocity.x, Is.LessThan(linearVelocity.x));
            Assert.That(result.LinearVelocity.x, Is.GreaterThan(0f));
            Assert.That(result.AngularVelocity.z, Is.LessThan(0f));
            Assert.That(resultSlip.magnitude, Is.LessThan(initialSlip.magnitude));
            Assert.That(result.LinearVelocity.y, Is.EqualTo(linearVelocity.y).Within(0.000001f));
        }

        [Test]
        public void CalculateMotionAfterStep_ReachesClassicalRollingStateWithoutSlipOvershoot()
        {
            const float initialSpeed = 2f;
            var slipSpeed = initialSpeed;
            var translationalSpeedChangeToRolling = (2f / 7f) * slipSpeed;
            var timeToRolling = translationalSpeedChangeToRolling
                / BilliardsSimulationConfiguration.ClothSlidingDecelerationMetersPerSecondSquared;

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                Vector3.right * initialSpeed,
                Vector3.zero,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                timeToRolling);
            var slip = ClothResistanceModel.CalculateContactPointSlipVelocity(
                result.LinearVelocity,
                result.AngularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters);
            var expectedRollingSpeed = initialSpeed * (5f / 7f);

            Assert.That(result.LinearVelocity.x, Is.EqualTo(expectedRollingSpeed).Within(0.00001f));
            Assert.That(
                result.AngularVelocity.z,
                Is.EqualTo(-expectedRollingSpeed / BilliardsPhysicalSpecification.BallRadiusMeters).Within(0.0001f));
            Assert.That(slip.magnitude, Is.LessThan(0.00001f));
        }

        [Test]
        public void CalculateMotionAfterStep_UsesRemainingStepForRollingResistance()
        {
            const float initialSpeed = 2f;
            var timeToRolling = ((2f / 7f) * initialSpeed)
                / BilliardsSimulationConfiguration.ClothSlidingDecelerationMetersPerSecondSquared;
            const float rollingDuration = 0.1f;

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                Vector3.right * initialSpeed,
                Vector3.zero,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                timeToRolling + rollingDuration);
            var expectedSpeed = (initialSpeed * (5f / 7f))
                - (BilliardsSimulationConfiguration.ClothRollingDecelerationMetersPerSecondSquared * rollingDuration);
            var slip = ClothResistanceModel.CalculateContactPointSlipVelocity(
                result.LinearVelocity,
                result.AngularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(result.LinearVelocity.x, Is.EqualTo(expectedSpeed).Within(0.00001f));
            Assert.That(result.LinearVelocity.x, Is.GreaterThan(0f));
            Assert.That(slip.magnitude, Is.LessThan(0.00001f));
        }

        [Test]
        public void CalculateMotionAfterStep_RollingResistancePreservesNoSlipAndDecaysSideSpin()
        {
            const float initialSpeed = 1f;
            const float sideSpin = 3f;
            var angularVelocity = new Vector3(
                0f,
                sideSpin,
                -initialSpeed / BilliardsPhysicalSpecification.BallRadiusMeters);

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                new Vector3(initialSpeed, -0.2f, 0f),
                angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                0.1f);
            var slip = ClothResistanceModel.CalculateContactPointSlipVelocity(
                result.LinearVelocity,
                result.AngularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            Assert.That(result.LinearVelocity.x, Is.EqualTo(0.98f).Within(0.000001f));
            Assert.That(result.LinearVelocity.y, Is.EqualTo(-0.2f).Within(0.000001f));
            Assert.That(result.AngularVelocity.y, Is.EqualTo(2.5f).Within(0.000001f));
            Assert.That(slip.magnitude, Is.LessThan(0.00001f));
        }

        [TestCase(0.2f)]
        [TestCase(-0.2f)]
        public void CalculateMotionAfterStep_SideSpinStopsWithoutReversing(float sideSpin)
        {
            const float initialSpeed = 1f;
            var angularVelocity = new Vector3(
                0f,
                sideSpin,
                -initialSpeed / BilliardsPhysicalSpecification.BallRadiusMeters);

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                Vector3.right * initialSpeed,
                angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                1f);

            Assert.That(result.AngularVelocity.y, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void CalculateMotionAfterStep_SideSpinDecayDoesNotAffectSlidingSolution()
        {
            const float initialSpeed = 2f;
            const float sideSpin = 3f;
            const float deltaTime = 0.1f;
            var linearVelocity = Vector3.right * initialSpeed;

            var withoutSideSpin = ClothResistanceModel.CalculateMotionAfterStep(
                linearVelocity,
                Vector3.zero,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                deltaTime);
            var withSideSpin = ClothResistanceModel.CalculateMotionAfterStep(
                linearVelocity,
                Vector3.up * sideSpin,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                deltaTime);

            Assert.That(withSideSpin.LinearVelocity, Is.EqualTo(withoutSideSpin.LinearVelocity));
            Assert.That(withSideSpin.AngularVelocity.x, Is.EqualTo(withoutSideSpin.AngularVelocity.x).Within(0.000001f));
            Assert.That(withSideSpin.AngularVelocity.z, Is.EqualTo(withoutSideSpin.AngularVelocity.z).Within(0.000001f));
            Assert.That(withSideSpin.AngularVelocity.y, Is.EqualTo(2.5f).Within(0.000001f));
        }

        [Test]
        public void CalculateMotionAfterStep_SideSpinDecayIsIndependentOfFixedStepSize()
        {
            const float initialSpeed = 1f;
            var initial = new ClothMotionState(
                Vector3.right * initialSpeed,
                new Vector3(
                    0f,
                    3f,
                    -initialSpeed / BilliardsPhysicalSpecification.BallRadiusMeters));

            var oneStep = ClothResistanceModel.CalculateMotionAfterStep(
                initial.LinearVelocity,
                initial.AngularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                0.2f);

            var repeatedSteps = initial;
            for (var index = 0; index < 40; index++)
            {
                repeatedSteps = ClothResistanceModel.CalculateMotionAfterStep(
                    repeatedSteps.LinearVelocity,
                    repeatedSteps.AngularVelocity,
                    BilliardsPhysicalSpecification.BallRadiusMeters,
                    0.005f);
            }

            Assert.That(repeatedSteps.AngularVelocity.y, Is.EqualTo(oneStep.AngularVelocity.y).Within(0.00001f));
        }

        [Test]
        public void CalculateMotionAfterStep_StopsRollingBallWithoutReversing()
        {
            const float speed = 0.001f;
            var angularVelocity = new Vector3(
                0f,
                0f,
                -speed / BilliardsPhysicalSpecification.BallRadiusMeters);

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                new Vector3(speed, 0.3f, 0f),
                angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                1f);

            Assert.That(result.LinearVelocity.x, Is.Zero.Within(0.000001f));
            Assert.That(result.LinearVelocity.z, Is.Zero.Within(0.000001f));
            Assert.That(result.LinearVelocity.y, Is.EqualTo(0.3f).Within(0.000001f));
            Assert.That(result.AngularVelocity.x, Is.Zero.Within(0.000001f));
            Assert.That(result.AngularVelocity.z, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void CalculateMotionAfterStep_IsIndependentOfFixedStepSizeAcrossTransition()
        {
            var initial = new ClothMotionState(new Vector3(2f, 0.1f, 0.5f), new Vector3(0f, 2f, 0f));

            var oneStep = ClothResistanceModel.CalculateMotionAfterStep(
                initial.LinearVelocity,
                initial.AngularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                0.4f);

            var repeatedSteps = initial;
            for (var index = 0; index < 80; index++)
            {
                repeatedSteps = ClothResistanceModel.CalculateMotionAfterStep(
                    repeatedSteps.LinearVelocity,
                    repeatedSteps.AngularVelocity,
                    BilliardsPhysicalSpecification.BallRadiusMeters,
                    0.005f);
            }

            Assert.That(Vector3.Distance(repeatedSteps.LinearVelocity, oneStep.LinearVelocity), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(repeatedSteps.AngularVelocity, oneStep.AngularVelocity), Is.LessThan(0.001f));
        }

        [Test]
        public void CalculateMotionAfterStep_WithInvalidStepLeavesMotionUnchanged()
        {
            var linearVelocity = new Vector3(1f, 2f, 3f);
            var angularVelocity = new Vector3(4f, 5f, 6f);

            var result = ClothResistanceModel.CalculateMotionAfterStep(
                linearVelocity,
                angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters,
                0f);

            Assert.That(result.LinearVelocity, Is.EqualTo(linearVelocity));
            Assert.That(result.AngularVelocity, Is.EqualTo(angularVelocity));
        }
    }
}
