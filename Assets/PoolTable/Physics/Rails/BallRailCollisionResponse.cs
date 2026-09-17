using System;
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
        private RailCollisionObservation _pendingObservation;
        private bool _hasPendingObservation;

        public event Action<RailCollisionObservation> RailCollisionResolved;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            FlushPendingObservation();
            _processedRailColliders.Clear();
            _railNormals.Clear();
            _pendingNativeRailImpulse = Vector3.zero;
        }

        private void OnDisable()
        {
            FlushPendingObservation();
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

            var appliedLinearImpulse = ApplyRailCorrection(
                _rigidbody,
                _pendingNativeRailImpulse,
                _stepAppliedResponse,
                response);

            _pendingNativeRailImpulse = Vector3.zero;
            _stepAppliedResponse = response;

            var normalClosingSpeedMetersPerSecond = CalculateMaximumNormalClosingSpeed(
                _stepIncomingLinearVelocity,
                _railNormals);

            if (!_hasPendingObservation)
            {
                _pendingObservation = new RailCollisionObservation(
                    Time.fixedTimeAsDouble,
                    collision.relativeVelocity.magnitude,
                    normalClosingSpeedMetersPerSecond,
                    collision.GetContact(0).point,
                    appliedLinearImpulse);
                _hasPendingObservation = true;
                return;
            }

            _pendingObservation = _pendingObservation.WithAdditionalImpulse(
                appliedLinearImpulse,
                normalClosingSpeedMetersPerSecond);
        }

        internal void FlushPendingObservation()
        {
            if (!_hasPendingObservation)
            {
                return;
            }

            var observation = _pendingObservation;
            _pendingObservation = default;
            _hasPendingObservation = false;
            RailCollisionResolved?.Invoke(observation);
        }

        internal static Vector3 ApplyRailCorrection(
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

            var appliedRailVelocityChange = customRailVelocityChange
                + new Vector3(0f, nativeRailVelocityChange.y, 0f);
            return appliedRailVelocityChange * rigidbody.mass;
        }

        internal static float CalculateMaximumNormalClosingSpeed(
            Vector3 incomingLinearVelocity,
            IReadOnlyList<Vector3> railNormals)
        {
            var maximumClosingSpeed = 0f;
            var planarIncomingLinearVelocity = new Vector3(
                incomingLinearVelocity.x,
                0f,
                incomingLinearVelocity.z);

            for (var index = 0; index < railNormals.Count; index++)
            {
                var planarNormal = new Vector3(
                    railNormals[index].x,
                    0f,
                    railNormals[index].z);
                if (planarNormal.sqrMagnitude <= Mathf.Epsilon)
                {
                    continue;
                }

                var incomingNormalSpeed = Vector3.Dot(
                    planarIncomingLinearVelocity,
                    planarNormal.normalized);
                maximumClosingSpeed = Mathf.Max(
                    maximumClosingSpeed,
                    -incomingNormalSpeed);
            }

            return maximumClosingSpeed;
        }
    }

    public readonly struct RailCollisionObservation
    {
        public RailCollisionObservation(
            double simulationTimeSeconds,
            float relativeSpeedMetersPerSecond,
            float normalClosingSpeedMetersPerSecond,
            Vector3 contactPointMeters,
            Vector3 appliedLinearImpulse)
        {
            SimulationTimeSeconds = simulationTimeSeconds;
            RelativeSpeedMetersPerSecond = relativeSpeedMetersPerSecond;
            NormalClosingSpeedMetersPerSecond = normalClosingSpeedMetersPerSecond;
            ContactPointMeters = contactPointMeters;
            AppliedLinearImpulse = appliedLinearImpulse;
        }

        public double SimulationTimeSeconds { get; }
        public float RelativeSpeedMetersPerSecond { get; }
        public float NormalClosingSpeedMetersPerSecond { get; }
        public Vector3 ContactPointMeters { get; }
        public Vector3 AppliedLinearImpulse { get; }

        public RailCollisionObservation WithAdditionalImpulse(
            Vector3 appliedLinearImpulse,
            float normalClosingSpeedMetersPerSecond)
        {
            return new RailCollisionObservation(
                SimulationTimeSeconds,
                RelativeSpeedMetersPerSecond,
                Mathf.Max(NormalClosingSpeedMetersPerSecond, normalClosingSpeedMetersPerSecond),
                ContactPointMeters,
                AppliedLinearImpulse + appliedLinearImpulse);
        }
    }
}
