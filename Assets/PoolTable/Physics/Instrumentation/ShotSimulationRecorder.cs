using System;
using System.Collections.Generic;
using PoolTable.Core.Balls;
using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    public sealed class ShotSimulationRecorder
    {
        private readonly Dictionary<BallId, Registration> _registrations = new();
        private readonly Dictionary<RigidbodySimulationProbe, BallId> _ballByProbe = new();
        private readonly List<ShotCollisionSample> _collisions = new();
        private readonly HashSet<BallPairCollisionKey> _ballPairCollisions = new();
        private double _startTimeSeconds;

        public bool IsRecording { get; private set; }

        public int RegisteredBallCount => _registrations.Count;

        public void Register(BallId ball, RigidbodySimulationProbe probe)
        {
            if (probe == null)
            {
                throw new ArgumentNullException(nameof(probe));
            }

            if (IsRecording)
            {
                throw new InvalidOperationException("Cannot change simulation registrations while recording.");
            }

            if (_registrations.ContainsKey(ball))
            {
                throw new InvalidOperationException($"Ball {ball} is already registered for simulation instrumentation.");
            }

            if (_ballByProbe.ContainsKey(probe))
            {
                throw new InvalidOperationException("A Rigidbody simulation probe cannot represent more than one ball.");
            }

            _registrations.Add(ball, new Registration(probe));
            _ballByProbe.Add(probe, ball);
        }

        public void Begin(double simulationTimeSeconds)
        {
            ValidateTime(simulationTimeSeconds, nameof(simulationTimeSeconds));
            if (IsRecording)
            {
                throw new InvalidOperationException("Shot simulation instrumentation is already recording.");
            }

            if (_registrations.Count == 0)
            {
                throw new InvalidOperationException("At least one ball must be registered before recording a shot.");
            }

            _startTimeSeconds = simulationTimeSeconds;
            _collisions.Clear();
            _ballPairCollisions.Clear();
            foreach (var registration in _registrations.Values)
            {
                registration.Samples.Clear();
                registration.Probe.KinematicSampled += OnKinematicSampled;
                registration.Probe.CollisionObserved += OnCollisionObserved;
            }

            IsRecording = true;
            foreach (var registration in _registrations.Values)
            {
                registration.Probe.BeginRecording(simulationTimeSeconds);
            }
        }

        public ShotSimulationReport Complete(double simulationTimeSeconds)
        {
            EnsureRecording();
            ValidateTime(simulationTimeSeconds, nameof(simulationTimeSeconds));
            if (simulationTimeSeconds < _startTimeSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(simulationTimeSeconds), simulationTimeSeconds, "Completion time cannot precede the recording start time.");
            }

            foreach (var registration in _registrations.Values)
            {
                registration.Probe.StopRecording(simulationTimeSeconds);
            }

            DetachProbes();
            IsRecording = false;

            var tracks = new List<BallSimulationTrack>(_registrations.Count);
            foreach (var pair in _registrations)
            {
                if (pair.Value.Samples.Count > 0)
                {
                    tracks.Add(new BallSimulationTrack(pair.Key, pair.Value.Samples));
                }
            }

            tracks.Sort((left, right) => left.Ball.Number.CompareTo(right.Ball.Number));
            var collisions = new List<ShotCollisionSample>(_collisions);
            collisions.Sort(CompareCollisions);
            return new ShotSimulationReport(
                simulationTimeSeconds - _startTimeSeconds,
                tracks,
                collisions);
        }

        public void Cancel(double simulationTimeSeconds)
        {
            if (!IsRecording)
            {
                return;
            }

            ValidateTime(simulationTimeSeconds, nameof(simulationTimeSeconds));
            foreach (var registration in _registrations.Values)
            {
                registration.Probe.StopRecording(simulationTimeSeconds);
            }

            DetachProbes();
            IsRecording = false;
            _collisions.Clear();
            _ballPairCollisions.Clear();
            foreach (var registration in _registrations.Values)
            {
                registration.Samples.Clear();
            }
        }

        internal void RecordCollision(
            RigidbodySimulationProbe sourceProbe,
            ProbeCollisionObservation observation)
        {
            if (!IsRecording || sourceProbe == null || !_ballByProbe.TryGetValue(sourceProbe, out var sourceBall))
            {
                return;
            }

            BallId? otherBall = null;
            if (observation.OtherBallProbe != null
                && _ballByProbe.TryGetValue(observation.OtherBallProbe, out var mappedOtherBall))
            {
                otherBall = mappedOtherBall;
            }

            if (observation.Kind == SimulationCollisionKind.Ball && otherBall.HasValue)
            {
                var first = sourceBall.Number <= otherBall.Value.Number ? sourceBall : otherBall.Value;
                var second = sourceBall.Number <= otherBall.Value.Number ? otherBall.Value : sourceBall;
                var key = new BallPairCollisionKey(first, second, observation.SimulationTimeSeconds);
                if (!_ballPairCollisions.Add(key))
                {
                    return;
                }

                sourceBall = first;
                otherBall = second;
            }

            _collisions.Add(new ShotCollisionSample(
                ToElapsedTime(observation.SimulationTimeSeconds),
                sourceBall,
                otherBall,
                observation.Kind,
                observation.RelativeSpeedMetersPerSecond,
                observation.ImpulseNewtonSeconds,
                observation.ContactPointMeters));
        }

        private void OnKinematicSampled(RigidbodySimulationProbe probe, ProbeKinematicSample sample)
        {
            if (!IsRecording || !_ballByProbe.TryGetValue(probe, out var ball))
            {
                return;
            }

            _registrations[ball].Samples.Add(new BallTrajectorySample(
                ToElapsedTime(sample.SimulationTimeSeconds),
                sample.PositionMeters,
                sample.LinearVelocityMetersPerSecond,
                sample.AngularVelocityRadiansPerSecond,
                sample.KineticEnergy));
        }

        private void OnCollisionObserved(RigidbodySimulationProbe probe, ProbeCollisionObservation observation)
        {
            RecordCollision(probe, observation);
        }

        private double ToElapsedTime(double simulationTimeSeconds)
        {
            return Math.Max(0d, simulationTimeSeconds - _startTimeSeconds);
        }

        private void DetachProbes()
        {
            foreach (var registration in _registrations.Values)
            {
                registration.Probe.KinematicSampled -= OnKinematicSampled;
                registration.Probe.CollisionObserved -= OnCollisionObserved;
            }
        }

        private void EnsureRecording()
        {
            if (!IsRecording)
            {
                throw new InvalidOperationException("Shot simulation instrumentation is not recording.");
            }
        }

        private static void ValidateTime(double value, string parameterName)
        {
            if (value < 0d || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Simulation time must be finite and non-negative.");
            }
        }

        private static int CompareCollisions(ShotCollisionSample left, ShotCollisionSample right)
        {
            var timeComparison = left.ElapsedTimeSeconds.CompareTo(right.ElapsedTimeSeconds);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            var ballComparison = left.Ball.Number.CompareTo(right.Ball.Number);
            if (ballComparison != 0)
            {
                return ballComparison;
            }

            var leftOtherBallNumber = left.OtherBall?.Number ?? -1;
            var rightOtherBallNumber = right.OtherBall?.Number ?? -1;
            var otherBallComparison = leftOtherBallNumber.CompareTo(rightOtherBallNumber);
            if (otherBallComparison != 0)
            {
                return otherBallComparison;
            }

            return left.Kind.CompareTo(right.Kind);
        }

        private sealed class Registration
        {
            public Registration(RigidbodySimulationProbe probe)
            {
                Probe = probe;
            }

            public RigidbodySimulationProbe Probe { get; }
            public List<BallTrajectorySample> Samples { get; } = new();
        }

        private readonly struct BallPairCollisionKey : IEquatable<BallPairCollisionKey>
        {
            public BallPairCollisionKey(BallId first, BallId second, double simulationTimeSeconds)
            {
                First = first;
                Second = second;
                SimulationTimeSeconds = simulationTimeSeconds;
            }

            private BallId First { get; }
            private BallId Second { get; }
            private double SimulationTimeSeconds { get; }

            public bool Equals(BallPairCollisionKey other)
            {
                return First == other.First
                    && Second == other.Second
                    && SimulationTimeSeconds.Equals(other.SimulationTimeSeconds);
            }

            public override bool Equals(object obj)
            {
                return obj is BallPairCollisionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = First.GetHashCode();
                    hashCode = (hashCode * 397) ^ Second.GetHashCode();
                    hashCode = (hashCode * 397) ^ SimulationTimeSeconds.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
