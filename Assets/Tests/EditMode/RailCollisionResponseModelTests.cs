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

        [Test]
        public void CalculateManifoldResponse_TwoDistinctNormalsReboundFromBothFaces()
        {
            var result = RailCollisionResponseModel.CalculateManifoldResponse(
                new Vector3(2f, 0f, 2f),
                Vector3.zero,
                new[] { Vector3.left, Vector3.back },
                Radius);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(result.LinearVelocity.x, Is.LessThan(0f));
            Assert.That(result.LinearVelocity.z, Is.LessThan(0f));
        }

        [Test]
        public void CalculateManifoldResponse_DuplicateNormalsAreAppliedOnlyOnce()
        {
            var linearVelocity = new Vector3(2f, 0f, 0.75f);
            var angularVelocity = Vector3.up * 4f;
            var singleResponse = RailCollisionResponseModel.CalculateResponse(
                linearVelocity,
                angularVelocity,
                Vector3.left,
                Radius);
            var manifoldResponse = RailCollisionResponseModel.CalculateManifoldResponse(
                linearVelocity,
                angularVelocity,
                new[] { Vector3.left, Vector3.left, Vector3.left },
                Radius);

            Assert.That(manifoldResponse.WasApplied, Is.True);
            Assert.That(manifoldResponse.LinearVelocity, Is.EqualTo(singleResponse.LinearVelocity));
            Assert.That(manifoldResponse.AngularVelocity, Is.EqualTo(singleResponse.AngularVelocity));
        }

        [Test]
        public void CalculateManifoldResponse_IsIndependentOfContactOrdering()
        {
            var linearVelocity = new Vector3(2f, 0f, 1.5f);
            var angularVelocity = Vector3.up * 2f;
            var forwardOrder = RailCollisionResponseModel.CalculateManifoldResponse(
                linearVelocity,
                angularVelocity,
                new[] { Vector3.left, Vector3.back },
                Radius);
            var reverseOrder = RailCollisionResponseModel.CalculateManifoldResponse(
                linearVelocity,
                angularVelocity,
                new[] { Vector3.back, Vector3.left },
                Radius);

            Assert.That(reverseOrder.WasApplied, Is.EqualTo(forwardOrder.WasApplied));
            Assert.That(reverseOrder.LinearVelocity, Is.EqualTo(forwardOrder.LinearVelocity));
            Assert.That(reverseOrder.AngularVelocity, Is.EqualTo(forwardOrder.AngularVelocity));
        }

        [Test]
        public void CalculateManifoldResponse_WideAngleFacesEndSeparatingFromEveryFace()
        {
            var firstNormal = Vector3.left;
            var secondNormal = new Vector3(0.5f, 0f, Mathf.Sqrt(3f) * 0.5f);
            var result = RailCollisionResponseModel.CalculateManifoldResponse(
                new Vector3(1f, 0f, -Mathf.Sqrt(3f)),
                Vector3.zero,
                new[] { firstNormal, secondNormal },
                Radius);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(Vector3.Dot(result.LinearVelocity, firstNormal), Is.GreaterThanOrEqualTo(0f));
            Assert.That(Vector3.Dot(result.LinearVelocity, secondNormal), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void CalculateManifoldResponse_WideAngleStabilizationDoesNotReapplyTangentialImpulse()
        {
            var firstNormal = Vector3.left;
            var secondNormal = new Vector3(0.5f, 0f, Mathf.Sqrt(3f) * 0.5f);
            var linearVelocity = new Vector3(1.2f, 0f, -1.5f);
            var angularVelocity = new Vector3(0f, 2f, 0f);
            var firstResponse = RailCollisionResponseModel.CalculateResponse(
                linearVelocity,
                angularVelocity,
                firstNormal,
                Radius);
            var secondResponse = RailCollisionResponseModel.CalculateResponse(
                firstResponse.LinearVelocity,
                firstResponse.AngularVelocity,
                secondNormal,
                Radius);

            var result = RailCollisionResponseModel.CalculateManifoldResponse(
                linearVelocity,
                angularVelocity,
                new[] { firstNormal, secondNormal },
                Radius);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(result.AngularVelocity, Is.EqualTo(secondResponse.AngularVelocity));
            Assert.That(Vector3.Dot(result.LinearVelocity, firstNormal), Is.GreaterThanOrEqualTo(0f));
            Assert.That(Vector3.Dot(result.LinearVelocity, secondNormal), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ApplyRailCorrection_PreservesOtherResolvedVelocityContributions()
        {
            var ballObject = new GameObject("RailCorrectionTestBall");

            try
            {
                var rigidbody = ballObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;

                var incomingLinearVelocity = new Vector3(2f, 0f, 0.5f);
                var incomingAngularVelocity = Vector3.up * 3f;
                var response = RailCollisionResponseModel.CalculateResponse(
                    incomingLinearVelocity,
                    incomingAngularVelocity,
                    Vector3.left,
                    Radius);
                var nativeRailVelocityChange = new Vector3(-1.75f, 0f, 0f);
                var otherLinearVelocityChange = new Vector3(0f, 0f, 0.75f);
                var otherAngularVelocityChange = Vector3.up * 4f;
                var resolvedLinearVelocity =
                    incomingLinearVelocity + nativeRailVelocityChange + otherLinearVelocityChange;
                var resolvedAngularVelocity = incomingAngularVelocity + otherAngularVelocityChange;
                rigidbody.linearVelocity = resolvedLinearVelocity;
                rigidbody.angularVelocity = resolvedAngularVelocity;

                BallRailCollisionResponse.ApplyRailCorrection(
                    rigidbody,
                    incomingLinearVelocity,
                    incomingAngularVelocity,
                    nativeRailVelocityChange * rigidbody.mass,
                    response);

                Assert.That(
                    rigidbody.linearVelocity,
                    Is.EqualTo(response.LinearVelocity + otherLinearVelocityChange));
                Assert.That(
                    rigidbody.angularVelocity,
                    Is.EqualTo(response.AngularVelocity + otherAngularVelocityChange));
            }
            finally
            {
                Object.DestroyImmediate(ballObject);
            }
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
