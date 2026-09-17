using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    public static class SpectateShotFramingModel
    {
        public static SpectateShotFrameAdjustment Evaluate(
            Vector3 averageLinearVelocity,
            float motionLeadSeconds,
            float maximumMotionLeadDistance,
            float distancePerMeterPerSecond,
            float maximumSpeedFramingDistance)
        {
            var planarVelocity = Vector3.ProjectOnPlane(averageLinearVelocity, Vector3.up);
            var speed = planarVelocity.magnitude;
            var motionLead = Vector3.ClampMagnitude(
                planarVelocity * Mathf.Max(0f, motionLeadSeconds),
                Mathf.Max(0f, maximumMotionLeadDistance));
            var additionalDistance = Mathf.Min(
                speed * Mathf.Max(0f, distancePerMeterPerSecond),
                Mathf.Max(0f, maximumSpeedFramingDistance));

            return new SpectateShotFrameAdjustment(motionLead, additionalDistance);
        }
    }

    public readonly struct SpectateShotFrameAdjustment
    {
        public SpectateShotFrameAdjustment(Vector3 motionLead, float additionalDistance)
        {
            MotionLead = motionLead;
            AdditionalDistance = Mathf.Max(0f, additionalDistance);
        }

        public Vector3 MotionLead { get; }

        public float AdditionalDistance { get; }
    }
}
