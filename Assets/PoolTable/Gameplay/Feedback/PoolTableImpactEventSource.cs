using System;
using System.Collections.Generic;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Pockets;
using PoolTable.Gameplay.Shots;
using PoolTable.Physics.Rails;
using UnityEngine;

namespace PoolTable.Gameplay.Feedback
{
    public sealed class PoolTableImpactEventSource : IDisposable
    {
        private readonly List<RailSubscription> railSubscriptions = new();
        private readonly PocketCaptureVolume[] pocketCaptureVolumes;
        private readonly ShotPowerController shotPowerController;
        private bool disposed;

        public PoolTableImpactEventSource(
            Transform ballsRoot,
            Transform pocketCaptureVolumesRoot,
            ShotPowerController shotPowerController)
        {
            if (ballsRoot == null)
            {
                throw new ArgumentNullException(nameof(ballsRoot));
            }

            if (pocketCaptureVolumesRoot == null)
            {
                throw new ArgumentNullException(nameof(pocketCaptureVolumesRoot));
            }

            this.shotPowerController = shotPowerController != null
                ? shotPowerController
                : throw new ArgumentNullException(nameof(shotPowerController));

            var railResponses = ballsRoot.GetComponentsInChildren<BallRailCollisionResponse>(true);
            if (railResponses.Length == 0)
            {
                throw new InvalidOperationException("No ball rail-collision responses were found under the Balls root.");
            }

            foreach (var railResponse in railResponses)
            {
                var rigidbody = railResponse.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    throw new InvalidOperationException(
                        $"{railResponse.name} has a rail-collision response without a Rigidbody.");
                }

                Action<RailCollisionObservation> handler = observation => OnRailCollision(rigidbody, observation);
                railResponse.RailCollisionResolved += handler;
                railSubscriptions.Add(new RailSubscription(railResponse, handler));
            }

            pocketCaptureVolumes = pocketCaptureVolumesRoot.GetComponentsInChildren<PocketCaptureVolume>(true);
            if (pocketCaptureVolumes.Length == 0)
            {
                throw new InvalidOperationException("No pocket capture volumes were found under the configured pocket root.");
            }

            foreach (var pocketCaptureVolume in pocketCaptureVolumes)
            {
                pocketCaptureVolume.BallCaptured += OnBallCaptured;
            }

            this.shotPowerController.CueStrikeApplied += OnCueStrikeApplied;
        }

        public event Action<RailImpactObservation> RailImpactObserved;

        public event Action<PocketedBall> BallPocketed;

        public event Action<CueStrikeObservation> CueStrikeApplied;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            foreach (var subscription in railSubscriptions)
            {
                if (subscription.Source != null)
                {
                    subscription.Source.RailCollisionResolved -= subscription.Handler;
                }
            }

            railSubscriptions.Clear();

            foreach (var pocketCaptureVolume in pocketCaptureVolumes)
            {
                if (pocketCaptureVolume != null)
                {
                    pocketCaptureVolume.BallCaptured -= OnBallCaptured;
                }
            }

            if (shotPowerController != null)
            {
                shotPowerController.CueStrikeApplied -= OnCueStrikeApplied;
            }
        }

        private void OnRailCollision(Rigidbody rigidbody, RailCollisionObservation observation)
        {
            var normalClosingSpeed = observation.NormalClosingSpeedMetersPerSecond;
            var impactEnergyJoules = 0.5f * rigidbody.mass * normalClosingSpeed * normalClosingSpeed;
            RailImpactObserved?.Invoke(new RailImpactObservation(
                impactEnergyJoules,
                normalClosingSpeed,
                observation.AppliedLinearImpulse.magnitude));
        }

        private void OnBallCaptured(PocketedBall pocketedBall)
        {
            BallPocketed?.Invoke(pocketedBall);
        }

        private void OnCueStrikeApplied(CueStrikeObservation observation)
        {
            CueStrikeApplied?.Invoke(observation);
        }

        private readonly struct RailSubscription
        {
            public RailSubscription(
                BallRailCollisionResponse source,
                Action<RailCollisionObservation> handler)
            {
                Source = source;
                Handler = handler;
            }

            public BallRailCollisionResponse Source { get; }

            public Action<RailCollisionObservation> Handler { get; }
        }
    }

    public readonly struct RailImpactObservation
    {
        public RailImpactObservation(
            float impactEnergyJoules,
            float normalClosingSpeedMetersPerSecond,
            float impulseNewtonSeconds)
        {
            ImpactEnergyJoules = Mathf.Max(0f, impactEnergyJoules);
            NormalClosingSpeedMetersPerSecond = Mathf.Max(0f, normalClosingSpeedMetersPerSecond);
            ImpulseNewtonSeconds = Mathf.Max(0f, impulseNewtonSeconds);
        }

        public float ImpactEnergyJoules { get; }

        public float NormalClosingSpeedMetersPerSecond { get; }

        public float ImpulseNewtonSeconds { get; }
    }
}
