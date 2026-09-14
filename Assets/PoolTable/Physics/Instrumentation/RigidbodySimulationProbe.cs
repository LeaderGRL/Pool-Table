using System;
using PoolTable.Physics.Cloth;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Rails;
using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodySimulationProbe : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private BallRailCollisionResponse _railCollisionResponse;
        private bool _isRecording;

        internal event Action<RigidbodySimulationProbe, ProbeKinematicSample> KinematicSampled;
        internal event Action<RigidbodySimulationProbe, ProbeCollisionObservation> CollisionObserved;

        public Rigidbody Body => _rigidbody != null ? _rigidbody : GetComponent<Rigidbody>();

        internal bool IsRecording => _isRecording;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _railCollisionResponse = GetComponent<BallRailCollisionResponse>();
            if (_railCollisionResponse != null)
            {
                _railCollisionResponse.RailCollisionResolved += OnRailCollisionResolved;
            }
        }

        private void FixedUpdate()
        {
            if (_isRecording)
            {
                EmitKinematicSample(Time.fixedTimeAsDouble);
            }
        }

        private void OnDisable()
        {
            if (!_isRecording)
            {
                return;
            }

            _railCollisionResponse?.FlushPendingObservation();
            EmitKinematicSample(Time.fixedTimeAsDouble);
            _isRecording = false;
        }

        private void OnDestroy()
        {
            if (_railCollisionResponse != null)
            {
                _railCollisionResponse.RailCollisionResolved -= OnRailCollisionResolved;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ObserveCollision(collision, requireImpulse: false);
        }

        private void OnCollisionStay(Collision collision)
        {
            ObserveCollision(collision, requireImpulse: true);
        }

        private void ObserveCollision(Collision collision, bool requireImpulse)
        {
            if (!_isRecording || (requireImpulse && collision.impulse.sqrMagnitude <= Mathf.Epsilon))
            {
                return;
            }

            var otherProbe = ResolveOtherBallProbe(collision);
            var kind = ClassifyCollision(collision, otherProbe);
            if (kind == SimulationCollisionKind.Rail && _railCollisionResponse != null)
            {
                return;
            }

            if (requireImpulse
                && kind != SimulationCollisionKind.Ball
                && kind != SimulationCollisionKind.Rail)
            {
                return;
            }

            EmitCollisionObservation(
                collision,
                otherProbe,
                kind,
                collision.impulse.magnitude);
        }

        private void OnRailCollisionResolved(RailCollisionObservation observation)
        {
            if (!_isRecording)
            {
                return;
            }

            CollisionObserved?.Invoke(
                this,
                new ProbeCollisionObservation(
                    observation.SimulationTimeSeconds,
                    null,
                    SimulationCollisionKind.Rail,
                    observation.RelativeSpeedMetersPerSecond,
                    observation.AppliedLinearImpulse.magnitude,
                    observation.ContactPointMeters));
        }

        private void EmitCollisionObservation(
            Collision collision,
            RigidbodySimulationProbe otherProbe,
            SimulationCollisionKind kind,
            float impulseNewtonSeconds)
        {
            var contactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : Body.position;

            CollisionObserved?.Invoke(
                this,
                new ProbeCollisionObservation(
                    Time.fixedTimeAsDouble,
                    otherProbe,
                    kind,
                    collision.relativeVelocity.magnitude,
                    impulseNewtonSeconds,
                    contactPoint));
        }

        internal void BeginRecording(double simulationTimeSeconds)
        {
            if (_isRecording || !gameObject.activeInHierarchy)
            {
                return;
            }

            _rigidbody ??= GetComponent<Rigidbody>();
            _isRecording = true;
            EmitKinematicSample(simulationTimeSeconds);
        }

        internal void StopRecording(double simulationTimeSeconds)
        {
            if (!_isRecording)
            {
                return;
            }

            _railCollisionResponse?.FlushPendingObservation();
            EmitKinematicSample(simulationTimeSeconds);
            _isRecording = false;
        }

        private void EmitKinematicSample(double simulationTimeSeconds)
        {
            _rigidbody ??= GetComponent<Rigidbody>();
            var energy = KineticEnergy.Calculate(
                _rigidbody.mass,
                _rigidbody.linearVelocity,
                _rigidbody.angularVelocity,
                BilliardsPhysicalSpecification.BallRadiusMeters);

            KinematicSampled?.Invoke(
                this,
                new ProbeKinematicSample(
                    simulationTimeSeconds,
                    _rigidbody.position,
                    _rigidbody.linearVelocity,
                    _rigidbody.angularVelocity,
                    energy));
        }

        private static RigidbodySimulationProbe ResolveOtherBallProbe(Collision collision)
        {
            var otherBody = collision.collider.attachedRigidbody;
            if (otherBody == null)
            {
                return null;
            }

            return otherBody.TryGetComponent<RigidbodySimulationProbe>(out var probe)
                ? probe
                : null;
        }

        private static SimulationCollisionKind ClassifyCollision(
            Collision collision,
            RigidbodySimulationProbe otherProbe)
        {
            if (otherProbe != null)
            {
                return SimulationCollisionKind.Ball;
            }

            if (collision.collider.TryGetComponent<RailSurface>(out _))
            {
                return SimulationCollisionKind.Rail;
            }

            if (collision.collider.TryGetComponent<ClothSurface>(out _))
            {
                return SimulationCollisionKind.Cloth;
            }

            return SimulationCollisionKind.Other;
        }
    }

    internal readonly struct ProbeKinematicSample
    {
        public ProbeKinematicSample(
            double simulationTimeSeconds,
            Vector3 positionMeters,
            Vector3 linearVelocityMetersPerSecond,
            Vector3 angularVelocityRadiansPerSecond,
            KineticEnergyMeasurement kineticEnergy)
        {
            SimulationTimeSeconds = simulationTimeSeconds;
            PositionMeters = positionMeters;
            LinearVelocityMetersPerSecond = linearVelocityMetersPerSecond;
            AngularVelocityRadiansPerSecond = angularVelocityRadiansPerSecond;
            KineticEnergy = kineticEnergy;
        }

        public double SimulationTimeSeconds { get; }
        public Vector3 PositionMeters { get; }
        public Vector3 LinearVelocityMetersPerSecond { get; }
        public Vector3 AngularVelocityRadiansPerSecond { get; }
        public KineticEnergyMeasurement KineticEnergy { get; }
    }

    internal readonly struct ProbeCollisionObservation
    {
        public ProbeCollisionObservation(
            double simulationTimeSeconds,
            RigidbodySimulationProbe otherBallProbe,
            SimulationCollisionKind kind,
            float relativeSpeedMetersPerSecond,
            float impulseNewtonSeconds,
            Vector3 contactPointMeters)
        {
            SimulationTimeSeconds = simulationTimeSeconds;
            OtherBallProbe = otherBallProbe;
            Kind = kind;
            RelativeSpeedMetersPerSecond = relativeSpeedMetersPerSecond;
            ImpulseNewtonSeconds = impulseNewtonSeconds;
            ContactPointMeters = contactPointMeters;
        }

        public double SimulationTimeSeconds { get; }
        public RigidbodySimulationProbe OtherBallProbe { get; }
        public SimulationCollisionKind Kind { get; }
        public float RelativeSpeedMetersPerSecond { get; }
        public float ImpulseNewtonSeconds { get; }
        public Vector3 ContactPointMeters { get; }
    }
}
