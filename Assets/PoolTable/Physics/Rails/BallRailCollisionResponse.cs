using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Rails
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallRailCollisionResponse : MonoBehaviour
    {
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.TryGetComponent<RailSurface>(out _)
                || collision.contactCount == 0)
            {
                return;
            }

            // Unity reports the contacted body's velocity relative to this Rigidbody.
            // The response model needs the ball velocity relative to the static rail.
            var incomingLinearVelocity = -collision.relativeVelocity;
            var incomingAngularVelocity = _rigidbody.angularVelocity;
            var railNormal = FindMostOpposingPlanarNormal(collision, incomingLinearVelocity);
            var response = RailCollisionResponseModel.CalculateResponse(
                incomingLinearVelocity,
                incomingAngularVelocity,
                railNormal,
                BilliardsPhysicalSpecification.BallRadiusMeters);
            if (!response.WasApplied)
            {
                return;
            }

            ApplyRailCorrection(
                _rigidbody,
                incomingLinearVelocity,
                incomingAngularVelocity,
                collision.impulse,
                response);
        }

        internal static void ApplyRailCorrection(
            Rigidbody rigidbody,
            Vector3 incomingLinearVelocity,
            Vector3 incomingAngularVelocity,
            Vector3 nativeRailImpulse,
            RailCollisionResponse response)
        {
            // PhysX has already resolved this rail contact before the callback runs.
            // Replace only this rail's native impulse so impulses from other contacts remain intact.
            var nativeRailVelocityChange = nativeRailImpulse / rigidbody.mass;
            var customRailVelocityChange = response.LinearVelocity - incomingLinearVelocity;
            rigidbody.linearVelocity += customRailVelocityChange - nativeRailVelocityChange;
            rigidbody.angularVelocity += response.AngularVelocity - incomingAngularVelocity;
        }

        private static Vector3 FindMostOpposingPlanarNormal(Collision collision, Vector3 incomingLinearVelocity)
        {
            var selectedNormal = collision.GetContact(0).normal;
            var selectedDot = PlanarDot(incomingLinearVelocity, selectedNormal);

            for (var index = 1; index < collision.contactCount; index++)
            {
                var candidateNormal = collision.GetContact(index).normal;
                var candidateDot = PlanarDot(incomingLinearVelocity, candidateNormal);
                if (candidateDot < selectedDot)
                {
                    selectedNormal = candidateNormal;
                    selectedDot = candidateDot;
                }
            }

            return selectedNormal;
        }

        private static float PlanarDot(Vector3 velocity, Vector3 normal)
        {
            return (velocity.x * normal.x) + (velocity.z * normal.z);
        }
    }
}
