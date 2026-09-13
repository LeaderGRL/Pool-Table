using NUnit.Framework;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Rails;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class RailCollisionResponseModelTests
    {
        private const float Radius = BilliardsPhysicalSpecification.BallRadiusMeters;

        [Test]
        public void CalculateResponse_HeadOnImpactReflectsNormalSpeedWithConfiguredRestitution()
        {
            var result = RailCollisionResponseModel.CalculateResponse(
                new Vector3(2f, 0.25f, 0f),
                Vector3.zero,
                Vector3.left,
                Radius);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(
                result.LinearVelocity.x,
                Is.EqualTo(-2f * BilliardsSimulationConfiguration.RailNormalRestitution).Within(0.000001f));
            Assert.That(result.LinearVelocity.y, Is.EqualTo(0.25f).Within(0.000001f));
            Assert.That(result.LinearVelocity.z, Is.Zero.Within(0.000001f));
            Assert.That(result.AngularVelocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CalculateResponse_ObliqueImpactReducesTangentialSlipAndCreatesSideSpin()
        {
            var initialLinearVelocity = new Vector3(2f, 0f, 1f);
            var railNormal = Vector3.left;
            var initialSlip = CalculateRailTangentialContactSpeed(
                initialLinearVelocity,
                Vector3.zero,
                railNormal);

            var result = RailCollisionResponseModel.CalculateResponse(
                initialLinearVelocity,
                Vector3.zero,
                railNormal,
                Radius);
            var resultSlip = CalculateRailTangentialContactSpeed(
                result.LinearVelocity,
                result.AngularVelocity,
                railNormal);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(Mathf.Abs(resultSlip), Is.LessThan(Mathf.Abs(initialSlip)));
            Assert.That(result.LinearVelocity.z, Is.LessThan(initialLinearVelocity.z));
            Assert.That(result.AngularVelocity.y, Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateResponse_SideSpinParticipatesInTangentialRailFriction()
        {
            var withoutSpin = RailCollisionResponseModel.CalculateResponse(
                new Vector3(2f, 0f, 0.2f),
                Vector3.zero,
                Vector3.left,
                Radius);
            var withSpin = RailCollisionResponseModel.CalculateResponse(
                new Vector3(2f, 0f, 0.2f),
                Vector3.up * 20f,
                Vector3.left,
                Radius);

            Assert.That(withSpin.LinearVelocity.z, Is.Not.EqualTo(withoutSpin.LinearVelocity.z).Within(0.000001f));
            Assert.That(withSpin.AngularVelocity.y, Is.Not.EqualTo(20f).Within(0.000001f));
        }

        [Test]
        public void CalculateResponse_FrictionImpulseDoesNotIncreasePlanarAndSideSpinEnergy()
        {
            var initialLinearVelocity = new Vector3(2f, 0f, 1.5f);
            var initialAngularVelocity = Vector3.up * 18f;
            var initialEnergy = CalculateEnergyPerUnitMass(initialLinearVelocity, initialAngularVelocity);

            var result = RailCollisionResponseModel.CalculateResponse(
                initialLinearVelocity,
                initialAngularVelocity,
                Vector3.left,
                Radius);
            var resultEnergy = CalculateEnergyPerUnitMass(result.LinearVelocity, result.AngularVelocity);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(resultEnergy, Is.LessThan(initialEnergy));
        }

        [Test]
        public void CalculateResponse_ShallowImpactCapsTangentialImpulseByCoulombLimit()
        {
            var result = RailCollisionResponseModel.CalculateResponse(
                new Vector3(0.1f, 0f, 4f),
                Vector3.zero,
                Vector3.left,
                Radius);
            var normalImpulseSpeed =
                (1f + BilliardsSimulationConfiguration.RailNormalRestitution) * 0.1f;
            var maximumTangentialSpeedChange =
                BilliardsSimulationConfiguration.RailTangentialFrictionCoefficient * normalImpulseSpeed;

            Assert.That(result.WasApplied, Is.True);
            Assert.That(
                4f - result.LinearVelocity.z,
                Is.EqualTo(maximumTangentialSpeedChange).Within(0.000001f));
        }

        [Test]
        public void CalculateResponse_SeparatingBallLeavesMotionUnchanged()
        {
            var linearVelocity = new Vector3(-1f, 0.1f, 0.5f);
            var angularVelocity = new Vector3(2f, 3f, 4f);

            var result = RailCollisionResponseModel.CalculateResponse(
                linearVelocity,
                angularVelocity,
                Vector3.left,
                Radius);

            Assert.That(result.WasApplied, Is.False);
            Assert.That(result.LinearVelocity, Is.EqualTo(linearVelocity));
            Assert.That(result.AngularVelocity, Is.EqualTo(angularVelocity));
        }

        [TestCase(0f, 1f, 0f)]
        [TestCase(float.NaN, 0f, 0f)]
        public void CalculateResponse_InvalidPlanarNormalLeavesMotionUnchanged(float x, float y, float z)
        {
            var linearVelocity = new Vector3(1f, 0f, 0f);
            var angularVelocity = Vector3.up * 5f;

            var result = RailCollisionResponseModel.CalculateResponse(
                linearVelocity,
                angularVelocity,
                new Vector3(x, y, z),
                Radius);

            Assert.That(result.WasApplied, Is.False);
            Assert.That(result.LinearVelocity, Is.EqualTo(linearVelocity));
            Assert.That(result.AngularVelocity, Is.EqualTo(angularVelocity));
        }

        private static float CalculateRailTangentialContactSpeed(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            Vector3 railNormal)
        {
            var planarNormal = new Vector3(railNormal.x, 0f, railNormal.z).normalized;
            var railTangent = Vector3.Cross(planarNormal, Vector3.up);
            var contactOffset = -planarNormal * Radius;
            var sideSpinContactVelocity = Vector3.Cross(
                Vector3.up * angularVelocity.y,
                contactOffset);
            return Vector3.Dot(linearVelocity + sideSpinContactVelocity, railTangent);
        }

        private static float CalculateEnergyPerUnitMass(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            var planarSpeedSquared = (linearVelocity.x * linearVelocity.x) + (linearVelocity.z * linearVelocity.z);
            var rotationalEnergyPerUnitMass =
                0.5f * 0.4f * Radius * Radius * angularVelocity.y * angularVelocity.y;
            return (0.5f * planarSpeedSquared) + rotationalEnergyPerUnitMass;
        }
    }
}
