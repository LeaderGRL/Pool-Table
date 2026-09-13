using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoolTable.Core.Balls;
using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    public sealed class ShotSimulationReport
    {
        private readonly ReadOnlyCollection<BallSimulationTrack> _tracks;
        private readonly ReadOnlyCollection<ShotCollisionSample> _collisions;
        private readonly Dictionary<BallId, BallSimulationTrack> _tracksByBall;

        internal ShotSimulationReport(
            double durationSeconds,
            IEnumerable<BallSimulationTrack> tracks,
            IEnumerable<ShotCollisionSample> collisions)
        {
            if (durationSeconds < 0d || double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Duration must be finite and non-negative.");
            }

            if (tracks == null)
            {
                throw new ArgumentNullException(nameof(tracks));
            }

            if (collisions == null)
            {
                throw new ArgumentNullException(nameof(collisions));
            }

            DurationSeconds = durationSeconds;
            var trackCopy = new List<BallSimulationTrack>(tracks);
            var collisionCopy = new List<ShotCollisionSample>(collisions);
            _tracksByBall = new Dictionary<BallId, BallSimulationTrack>(trackCopy.Count);

            foreach (var track in trackCopy)
            {
                if (track == null)
                {
                    throw new ArgumentException("Simulation tracks cannot contain null entries.", nameof(tracks));
                }

                if (!_tracksByBall.TryAdd(track.Ball, track))
                {
                    throw new ArgumentException($"Duplicate simulation track for ball {track.Ball}.", nameof(tracks));
                }
            }

            _tracks = trackCopy.AsReadOnly();
            _collisions = collisionCopy.AsReadOnly();
            TotalDistanceTraveledMeters = CalculateTotalDistance(trackCopy);
            InitialTotalKineticEnergyJoules = CalculateBoundaryEnergy(trackCopy, first: true);
            FinalTotalKineticEnergyJoules = CalculateBoundaryEnergy(trackCopy, first: false);
            PeakTotalKineticEnergyJoules = CalculatePeakTotalEnergy(trackCopy);
        }

        public double DurationSeconds { get; }

        public IReadOnlyList<BallSimulationTrack> Tracks => _tracks;

        public IReadOnlyList<ShotCollisionSample> Collisions => _collisions;

        public int CollisionCount => _collisions.Count;

        public float TotalDistanceTraveledMeters { get; }

        public float InitialTotalKineticEnergyJoules { get; }

        public float FinalTotalKineticEnergyJoules { get; }

        public float PeakTotalKineticEnergyJoules { get; }

        public bool TryGetTrack(BallId ball, out BallSimulationTrack track)
        {
            return _tracksByBall.TryGetValue(ball, out track);
        }

        private static float CalculateTotalDistance(IReadOnlyList<BallSimulationTrack> tracks)
        {
            var total = 0f;
            for (var index = 0; index < tracks.Count; index++)
            {
                total += tracks[index].DistanceTraveledMeters;
            }

            return total;
        }

        private static float CalculateBoundaryEnergy(IReadOnlyList<BallSimulationTrack> tracks, bool first)
        {
            var total = 0f;
            for (var index = 0; index < tracks.Count; index++)
            {
                total += first
                    ? tracks[index].InitialKineticEnergyJoules
                    : tracks[index].FinalKineticEnergyJoules;
            }

            return total;
        }

        private static float CalculatePeakTotalEnergy(IReadOnlyList<BallSimulationTrack> tracks)
        {
            if (tracks.Count == 0)
            {
                return 0f;
            }

            var timestamps = new SortedSet<double>();
            for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                var samples = tracks[trackIndex].Samples;
                for (var sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
                {
                    timestamps.Add(samples[sampleIndex].ElapsedTimeSeconds);
                }
            }

            var cursors = new int[tracks.Count];
            var hasSample = new bool[tracks.Count];
            var peak = 0f;

            foreach (var timestamp in timestamps)
            {
                var total = 0f;
                for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                {
                    var samples = tracks[trackIndex].Samples;
                    while (cursors[trackIndex] < samples.Count
                        && samples[cursors[trackIndex]].ElapsedTimeSeconds <= timestamp)
                    {
                        hasSample[trackIndex] = true;
                        cursors[trackIndex]++;
                    }

                    if (hasSample[trackIndex])
                    {
                        total += samples[cursors[trackIndex] - 1].KineticEnergy.TotalJoules;
                    }
                }

                peak = Mathf.Max(peak, total);
            }

            return peak;
        }
    }
}
