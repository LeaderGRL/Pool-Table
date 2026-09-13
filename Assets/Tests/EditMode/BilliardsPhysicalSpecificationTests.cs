using NUnit.Framework;
using PoolTable.Physics.Configuration;

namespace PoolTable.Tests.EditMode
{
    public sealed class BilliardsPhysicalSpecificationTests
    {
        [Test]
        public void Specification_UsesMetersAsUnityWorldUnits()
        {
            Assert.That(BilliardsPhysicalSpecification.UnityUnitsPerMeter, Is.EqualTo(1f));
            Assert.That(BilliardsPhysicalSpecification.BallDiameterMeters, Is.EqualTo(0.05715f).Within(0.000001f));
            Assert.That(BilliardsPhysicalSpecification.BallRadiusMeters, Is.EqualTo(0.028575f).Within(0.000001f));
        }

        [Test]
        public void Specification_DefinesRegulationNineFootPlayingSurface()
        {
            Assert.That(
                BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters,
                Is.EqualTo(2.54f).Within(0.000001f));
            Assert.That(
                BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters,
                Is.EqualTo(1.27f).Within(0.000001f));
            Assert.That(
                BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters
                / BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters,
                Is.EqualTo(2f).Within(0.000001f));
        }

        [Test]
        public void Specification_UsesAValidReferenceBedHeight()
        {
            Assert.That(
                BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters,
                Is.InRange(
                    BilliardsPhysicalSpecification.MinimumTableBedHeightMeters,
                    BilliardsPhysicalSpecification.MaximumTableBedHeightMeters));
            Assert.That(
                BilliardsPhysicalSpecification.BallCenterHeightMeters
                - BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters,
                Is.EqualTo(BilliardsPhysicalSpecification.BallRadiusMeters).Within(0.000001f));
        }

        [Test]
        public void Specification_DefinesTouchingTriangularRackGeometry()
        {
            var halfBall = BilliardsPhysicalSpecification.BallRadiusMeters;
            var rowSpacing = BilliardsPhysicalSpecification.TriangularRackRowSpacingMeters;
            var centerDistanceSquared = (rowSpacing * rowSpacing) + (halfBall * halfBall);
            var diameterSquared = BilliardsPhysicalSpecification.BallDiameterMeters
                * BilliardsPhysicalSpecification.BallDiameterMeters;

            Assert.That(centerDistanceSquared, Is.EqualTo(diameterSquared).Within(0.000001f));
        }
    }
}
