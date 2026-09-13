using System.Collections.Generic;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Rails
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallRailCollisionResponse : MonoBehaviour
    {
        private readonly Dictionary<EntityId, int> _lastProcessedStepByCollider = new();
        private readonly List<Vector3> _railNormals = new(4);
        private Rigidbody _rigidbody;
        private int _physicsStep;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            _physicsStep++;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            _lastProcessedStepByCollider.Remove(collision.collider.GetEntityId());
        }

        private void ProcessCollision(Collision collision)
        {
            if (!collision.collider.TryGetComponent<RailSurface>(out _)
                || collision.contactCount == 0)
            {
                return;
            }

            var colliderId = collision.collider.GetEntityId();
            if (_lastProcessedStepByCollider.TryGetValue(colliderId, out var processedStep)
                && processedStep == _physicsStep)
            {
                return;
            }

            _lastProcessedStepByCollider[colliderId] = _physicsStep;

            // Unity reports the contacted body's velocity relative to this Rigidbody.
            // The response model needs the ball velocity relative to the static rail.
            var incomingLinearVelocity = -collision.relativeVelocity;
            var incomingAngularVelocity = _rigidbody.angularVelocity;

            _railNormals.Clear();
            for (var index = 0; index < collision.contactCount; index++)
            {
                _railNormals.Add(collision.GetContact(index).normal);
            }

            var response = RailCollisionResponseModel.CalculateManifoldResponse(
                incomingLinearVelocity,
                incomingAngularVelocity,
                _railNormals,
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
    }
}
