using System.Collections.Generic;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Rails
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallRailCollisionResponse : MonoBehaviour
    {
        private readonly HashSet<EntityId> _processedRailColliders = new();
        private readonly List<Vector3> _railNormals = new(8);
        private Rigidbody _rigidbody;
        private Vector3 _stepIncomingLinearVelocity;
        private Vector3 _stepIncomingAngularVelocity;
        private RailCollisionResponse _stepAppliedResponse;
        private Vector3 _pendingNativeRailImpulse;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            _processedRailColliders.Clear();
            _railNormals.Clear();
            _pendingNativeRailImpulse = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void ProcessCollision(Collision collision)
        {
            if (!collision.collider.TryGetComponent<RailSurface>(out _)
                || collision.contactCount == 0)
            {
                return;
            }

            var colliderId = collision.collider.GetEntityId();
            if (!_processedRailColliders.Add(colliderId))
            {
                return;
            }

            if (_railNormals.Count == 0)
            {
                // Unity reports the contacted body's velocity relative to this Rigidbody.
                // Every static rail pair in this solver step shares the same incoming ball velocity.
                _stepIncomingLinearVelocity = -collision.relativeVelocity;
                _stepIncomingAngularVelocity = _rigidbody.angularVelocity;
                _stepAppliedResponse = new RailCollisionResponse(
                    _stepIncomingLinearVelocity,
                    _stepIncomingAngularVelocity,
                    false);
            }

            for (var index = 0; index < collision.contactCount; index++)
            {
                _railNormals.Add(collision.GetContact(index).normal);
            }

            _pendingNativeRailImpulse += collision.impulse;

            var response = RailCollisionResponseModel.CalculateManifoldResponse(
                _stepIncomingLinearVelocity,
                _stepIncomingAngularVelocity,
                _railNormals,
                BilliardsPhysicalSpecification.BallRadiusMeters);
            if (!response.WasApplied)
            {
                return;
            }

            ApplyRailCorrection(
                _rigidbody,
                _pendingNativeRailImpulse,
                _stepAppliedResponse,
                response);

            _pendingNativeRailImpulse = Vector3.zero;
            _stepAppliedResponse = response;
        }

        internal static void ApplyRailCorrection(
            Rigidbody rigidbody,
            Vector3 nativeRailImpulse,
            RailCollisionResponse previousResponse,
            RailCollisionResponse combinedResponse)
        {
            // PhysX has already resolved this rail contact before the callback runs.
            // Replace only the planar portion owned by the custom rail model. PhysX keeps the
            // vertical impulse from beveled or sloped faces while the manifold response stays XZ-only.
            var nativeRailVelocityChange = nativeRailImpulse / rigidbody.mass;
            var planarNativeRailVelocityChange = new Vector3(
                nativeRailVelocityChange.x,
                0f,
                nativeRailVelocityChange.z);
            var customRailVelocityChange = combinedResponse.LinearVelocity - previousResponse.LinearVelocity;
            rigidbody.linearVelocity += customRailVelocityChange - planarNativeRailVelocityChange;
            rigidbody.angularVelocity += combinedResponse.AngularVelocity - previousResponse.AngularVelocity;
        }
    }
}
