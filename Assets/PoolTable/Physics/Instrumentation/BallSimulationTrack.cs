using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoolTable.Core.Balls;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    public sealed class BallSimulationTrack
    {
        private readonly ReadOnlyCollection<BallTrajectorySample> _samples;

        internal BallSimulationTrack(BallId ball, IEnumerable<BallTrajectorySample> samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            Ball = ball;
            var copy = new List<BallTrajectorySample>(samples);
            if (copy.Count == 0)
            {
                throw new ArgumentException("A simulation track requires at least one sample.", nameof(samples));
            }

            for (var index = 1; index < copy.Count; index++)
            {
                if (copy[index].ElapsedTimeSeconds < copy[index - 1].ElapsedTimeSeconds)
                {
                    throw new ArgumentException("Simulation samples must be ordered by elapsed time.", nameof(samples));
                }
            }

            _samples = copy.AsReadOnly();
            DistanceTraveledMeters = CalculateDistance(copy);
            PeakKineticEnergyJoules = CalculatePeakEnergy(copy);
            StoppingTimeSeconds = CalculateStoppingTime(copy);
        }

        public BallId Ball { get; }

        public IReadOnlyList<BallTrajectorySample> Samples => _samples;

        public float DistanceTraveledMeters { get; }

        public float InitialKineticEnergyJoules => _samples[0].KineticEnergy.TotalJoules;

        public float FinalKineticEnergyJoules => _samples[_samples.Count - 1].KineticEnergy.TotalJoules;

        public float PeakKineticEnergyJoules { get; }

        public double? StoppingTimeSeconds { get; }

        private static float CalculateDistance(IReadOnlyList<BallTrajectorySample> samples)
        {
            var distance = 0f;
            for (var index = 1; index < samples.Count; index++)
            {
                distance += Vector3.Distance(samples[index - 1].PositionMeters, samples[index].PositionMeters);
            }

            return distance;
        }

        private static float CalculatePeakEnergy(IReadOnlyList<BallTrajectorySample> samples)
        {
            var peak = 0f;
            for (var index = 0; index < samples.Count; index++)
            {
                peak = Mathf.Max(peak, samples[index].KineticEnergy.TotalJoules);
            }

            return peak;
        }

        private static double? CalculateStoppingTime(IReadOnlyList<BallTrajectorySample> samples)
        {
            var lastMovingIndex = -1;
            for (var index = samples.Count - 1; index >= 0; index--)
            {
                if (IsMoving(samples[index]))
                {
                    lastMovingIndex = index;
                    break;
                }
            }

            if (lastMovingIndex < 0)
            {
                return 0d;
            }

            if (lastMovingIndex == samples.Count - 1)
            {
                return null;
            }

            return samples[lastMovingIndex + 1].ElapsedTimeSeconds;
        }

        private static bool IsMoving(BallTrajectorySample sample)
        {
            return sample.LinearVelocityMetersPerSecond.magnitude > BilliardsPhysicalSpecification.BallStoppedSpeedMetersPerSecond
                || sample.AngularVelocityRadiansPerSecond.magnitude > BilliardsPhysicalSpecification.BallStoppedAngularSpeedRadiansPerSecond;
        }
    }
}
