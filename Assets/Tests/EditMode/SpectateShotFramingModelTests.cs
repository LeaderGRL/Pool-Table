using NUnit.Framework;
using PoolTable.Presentation.Camera;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class SpectateShotFramingModelTests
    {
        [Test]
        public void StationaryAction_DoesNotLeadOrPullBack()
        {
            var adjustment = SpectateShotFramingModel.Evaluate(
                Vector3.zero,
                0.14f,
                0.32f,
                0.1f,
                0.35f);

            Assert.That(adjustment.MotionLead, Is.EqualTo(Vector3.zero));
            Assert.That(adjustment.AdditionalDistance, Is.EqualTo(0f));
        }

        [Test]
        public void FastAction_LeadsAlongMotionAndCapsExtraDistance()
        {
            var adjustment = SpectateShotFramingModel.Evaluate(
                new Vector3(8f, 3f, -4f),
                0.14f,
                0.32f,
                0.1f,
                0.35f);

            Assert.That(adjustment.MotionLead.y, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(adjustment.MotionLead.magnitude, Is.EqualTo(0.32f).Within(0.0001f));
            Assert.That(Vector3.Dot(adjustment.MotionLead, new Vector3(8f, 0f, -4f)), Is.GreaterThan(0f));
            Assert.That(adjustment.AdditionalDistance, Is.EqualTo(0.35f).Within(0.0001f));
        }
    }
}
