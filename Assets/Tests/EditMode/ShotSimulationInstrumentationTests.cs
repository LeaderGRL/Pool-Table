using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Instrumentation;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    [Category("Physics")]
    public sealed class ShotSimulationInstrumentationTests
    {
        [Test]
        public void KineticEnergy_CalculatesTranslationAndSolidSphereRotation()
        {
            var measurement = KineticEnergy.Calculate(
                2f,
                new Vector3(3f, 4f, 0f),
                new Vector3(0f, 2f, 0f),
                0.5f);

            Assert.That(measurement.LinearJoules, Is.EqualTo(25f).Within(0.000001f));
            Assert.That(measurement.AngularJoules, Is.EqualTo(0.4f).Within(0.000001f));
            Assert.That(measurement.TotalJoules, Is.EqualTo(25.4f).Within(0.000001f));
        }

        [Test]
        public void BallSimulationTrack_MeasuresDistancePeakEnergyAndStoppingTime()
        {
            var angularMotionAboveStopThreshold =
                Vector3.up * BilliardsPhysicalSpecification.BallStoppedAngularSpeedRadiansPerSecond * 2f;
            var track = new BallSimulationTrack(
                new BallId(3),
                new[]
                {
                    Sample(0d, Vector3.zero, Vector3.right, Vector3.zero, 2f),
                    Sample(0.1d, new Vector3(0.1f, 0f, 0f), Vector3.zero, angularMotionAboveStopThreshold, 1f),
                    Sample(0.2d, new Vector3(0.15f, 0f, 0f), Vector3.zero, Vector3.zero, 0f),
                });

            Assert.That(track.DistanceTraveledMeters, Is.EqualTo(0.15f).Within(0.000001f));
            Assert.That(track.InitialKineticEnergyJoules, Is.EqualTo(2f).Within(0.000001f));
            Assert.That(track.FinalKineticEnergyJoules, Is.Zero.Within(0.000001f));
            Assert.That(track.PeakKineticEnergyJoules, Is.EqualTo(2f).Within(0.000001f));
            Assert.That(track.StoppingTimeSeconds, Is.EqualTo(0.2d).Within(0.000001d));
        }

        [Test]
        public void BallSimulationTrack_ReportsNullStoppingTimeWhileBallIsStillMoving()
        {
            var track = new BallSimulationTrack(
                new BallId(4),
                new[]
                {
                    Sample(0d, Vector3.zero, Vector3.right, Vector3.zero, 1f),
                    Sample(0.1d, new Vector3(0.1f, 0f, 0f), Vector3.right * 0.5f, Vector3.zero, 0.25f),
                });

            Assert.That(track.StoppingTimeSeconds, Is.Null);
        }

        [Test]
        public void ShotSimulationReport_CopiesInputsAndAggregatesTracks()
        {
            var cueTrack = new BallSimulationTrack(
                new BallId(0),
                new[]
                {
                    Sample(0d, Vector3.zero, Vector3.zero, Vector3.zero, 2f),
                    Sample(0.1d, Vector3.right, Vector3.zero, Vector3.zero, 0f),
                });
            var objectTrack = new BallSimulationTrack(
                new BallId(1),
                new[]
                {
                    Sample(0d, Vector3.zero, Vector3.zero, Vector3.zero, 1f),
                    Sample(0.1d, Vector3.forward * 0.5f, Vector3.zero, Vector3.zero, 0.5f),
                });
            var sourceTracks = new List<BallSimulationTrack> { cueTrack, objectTrack };
            var sourceCollisions = new List<ShotCollisionSample>
            {
                new ShotCollisionSample(
                    0.05d,
                    new BallId(0),
                    new BallId(1),
                    SimulationCollisionKind.Ball,
                    1.5f,
                    0.2f,
                    Vector3.zero),
            };

            var report = new ShotSimulationReport(0.1d, sourceTracks, sourceCollisions);
            sourceTracks.Clear();
            sourceCollisions.Clear();

            Assert.That(report.Tracks, Has.Count.EqualTo(2));
            Assert.That(report.Collisions, Has.Count.EqualTo(1));
            Assert.That(report.TotalDistanceTraveledMeters, Is.EqualTo(1.5f).Within(0.000001f));
            Assert.That(report.InitialTotalKineticEnergyJoules, Is.EqualTo(3f).Within(0.000001f));
            Assert.That(report.FinalTotalKineticEnergyJoules, Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(report.PeakTotalKineticEnergyJoules, Is.EqualTo(3f).Within(0.000001f));
            Assert.That(report.TryGetTrack(new BallId(1), out var indexedTrack), Is.True);
            Assert.That(indexedTrack, Is.SameAs(objectTrack));
            Assert.Throws<NotSupportedException>(() => ((IList<BallSimulationTrack>)report.Tracks).Add(cueTrack));
            Assert.Throws<NotSupportedException>(() => ((IList<ShotCollisionSample>)report.Collisions).Clear());
        }

        [Test]
        public void ShotSimulationRecorder_DeduplicatesRepeatedBallCollisionCallbacksWithinStep()
        {
            var firstObject = new GameObject("InstrumentationBall1");
            var secondObject = new GameObject("InstrumentationBall2");
            ShotSimulationRecorder recorder = null;

            try
            {
                firstObject.AddComponent<Rigidbody>().mass = BilliardsSimulationConfiguration.BallMassKilograms;
                secondObject.AddComponent<Rigidbody>().mass = BilliardsSimulationConfiguration.BallMassKilograms;
                var firstProbe = firstObject.AddComponent<RigidbodySimulationProbe>();
                var secondProbe = secondObject.AddComponent<RigidbodySimulationProbe>();
                recorder = new ShotSimulationRecorder();
                recorder.Register(new BallId(1), firstProbe);
                recorder.Register(new BallId(2), secondProbe);
                recorder.Begin(10d);

                recorder.RecordCollision(
                    firstProbe,
                    new ProbeCollisionObservation(
                        10.005d,
                        secondProbe,
                        SimulationCollisionKind.Ball,
                        2f,
                        0.15f,
                        Vector3.zero));
                recorder.RecordCollision(
                    secondProbe,
                    new ProbeCollisionObservation(
                        10.005d,
                        firstProbe,
                        SimulationCollisionKind.Ball,
                        2f,
                        0.15f,
                        Vector3.zero));
                recorder.RecordCollision(
                    firstProbe,
                    new ProbeCollisionObservation(
                        10.005d,
                        secondProbe,
                        SimulationCollisionKind.Ball,
                        2f,
                        0.15f,
                        Vector3.zero));

                var report = recorder.Complete(10.01d);

                Assert.That(report.Collisions, Has.Count.EqualTo(1));
                Assert.That(report.Collisions[0].Ball, Is.EqualTo(new BallId(1)));
                Assert.That(report.Collisions[0].OtherBall, Is.EqualTo(new BallId(2)));
                Assert.That(report.Collisions[0].ElapsedTimeSeconds, Is.EqualTo(0.005d).Within(0.000001d));
            }
            finally
            {
                if (recorder != null && recorder.IsRecording)
                {
                    recorder.Cancel(10.02d);
                }

                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void ShotSimulationRecorder_ReturnsCollisionsInStableElapsedTimeOrder()
        {
            var ballObject = new GameObject("InstrumentationBall");
            ShotSimulationRecorder recorder = null;

            try
            {
                ballObject.AddComponent<Rigidbody>().mass = BilliardsSimulationConfiguration.BallMassKilograms;
                var probe = ballObject.AddComponent<RigidbodySimulationProbe>();
                recorder = new ShotSimulationRecorder();
                recorder.Register(new BallId(5), probe);
                recorder.Begin(2d);

                recorder.RecordCollision(
                    probe,
                    new ProbeCollisionObservation(
                        2.02d,
                        null,
                        SimulationCollisionKind.Rail,
                        1f,
                        0.1f,
                        Vector3.right));
                recorder.RecordCollision(
                    probe,
                    new ProbeCollisionObservation(
                        2.01d,
                        null,
                        SimulationCollisionKind.Other,
                        0.5f,
                        0.05f,
                        Vector3.left));

                var report = recorder.Complete(2.03d);

                Assert.That(report.Collisions.Select(collision => collision.ElapsedTimeSeconds), Is.Ordered.Ascending);
                Assert.That(report.Collisions[0].Kind, Is.EqualTo(SimulationCollisionKind.Other));
                Assert.That(report.Collisions[1].Kind, Is.EqualTo(SimulationCollisionKind.Rail));
            }
            finally
            {
                if (recorder != null && recorder.IsRecording)
                {
                    recorder.Cancel(2.04d);
                }

                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        private static BallTrajectorySample Sample(
            double elapsedTimeSeconds,
            Vector3 positionMeters,
            Vector3 linearVelocityMetersPerSecond,
            Vector3 angularVelocityRadiansPerSecond,
            float totalEnergyJoules)
        {
            return new BallTrajectorySample(
                elapsedTimeSeconds,
                positionMeters,
                linearVelocityMetersPerSecond,
                angularVelocityRadiansPerSecond,
                new KineticEnergyMeasurement(totalEnergyJoules, 0f));
        }
    }
}
