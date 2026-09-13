using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Pockets;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Pockets;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class PocketCaptureTests
    {
        [Test]
        public void PocketCaptureLayout_MapsSixStablePocketsClockwiseAroundTable()
        {
            var halfLength = BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f;
            var halfWidth = BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f;
            var height = BilliardsPhysicalSpecification.PocketCaptureCenterHeightMeters;
            var expected = new[]
            {
                new Vector3(-halfLength, height, -halfWidth),
                new Vector3(0f, height, -halfWidth),
                new Vector3(halfLength, height, -halfWidth),
                new Vector3(halfLength, height, halfWidth),
                new Vector3(0f, height, halfWidth),
                new Vector3(-halfLength, height, halfWidth),
            };

            var observed = new List<Vector3>();
            for (var index = PocketId.MinimumIndex; index <= PocketId.MaximumIndex; index++)
            {
                observed.Add(PocketCaptureLayout.GetCenter(new PocketId(index)));
            }

            Assert.That(observed, Is.EqualTo(expected));
        }

        [Test]
        public void PocketCaptureVolume_CapturesTypedBallOnceAndSupportsExplicitRestore()
        {
            var pocketObject = new GameObject("PocketCaptureTestVolume");
            var ballObject = new GameObject("PocketCaptureTestBall");

            try
            {
                var volume = pocketObject.AddComponent<PocketCaptureVolume>();
                var identity = ballObject.AddComponent<BallIdentity>();
                SetBallNumber(identity, 5);
                ballObject.AddComponent<SphereCollider>();
                var rigidbody = ballObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.linearVelocity = Vector3.one;
                rigidbody.angularVelocity = Vector3.one;
                var capture = ballObject.AddComponent<BallPocketCapture>();

                var observations = new List<PocketedBall>();
                volume.BallCaptured += observations.Add;

                Assert.That(volume.TryCapture(capture), Is.True);
                Assert.That(capture.IsCaptured, Is.True);
                Assert.That(capture.CapturedPocket, Is.EqualTo(new PocketId(1)));
                Assert.That(ballObject.activeSelf, Is.False);
                Assert.That(rigidbody.linearVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(rigidbody.angularVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(observations, Has.Count.EqualTo(1));
                Assert.That(observations[0].Ball, Is.EqualTo(new BallId(5)));
                Assert.That(observations[0].Pocket, Is.EqualTo(new PocketId(1)));

                Assert.That(volume.TryCapture(capture), Is.False);
                Assert.That(observations, Has.Count.EqualTo(1));

                var restoredPosition = new Vector3(0.25f, 0.8f, -0.1f);
                capture.Restore(restoredPosition);

                Assert.That(ballObject.activeSelf, Is.True);
                Assert.That(capture.IsCaptured, Is.False);
                Assert.That(capture.CapturedPocket, Is.Null);
                Assert.That(ballObject.transform.position, Is.EqualTo(restoredPosition));
                Assert.That(volume.TryCapture(capture), Is.True);
                Assert.That(observations, Has.Count.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(ballObject);
                Object.DestroyImmediate(pocketObject);
            }
        }

        [Test]
        public void CueBallCapture_PreservesObjectForExplicitBallInHandRestore()
        {
            var pocketObject = new GameObject("CueBallPocketCaptureTestVolume");
            var cueBallObject = new GameObject("CueBallPocketCaptureTestBall");

            try
            {
                var volume = pocketObject.AddComponent<PocketCaptureVolume>();
                var identity = cueBallObject.AddComponent<BallIdentity>();
                SetBallNumber(identity, BallId.CueBallNumber);
                cueBallObject.AddComponent<SphereCollider>();
                var rigidbody = cueBallObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                var capture = cueBallObject.AddComponent<BallPocketCapture>();

                Assert.That(volume.TryCapture(capture), Is.True);
                Assert.That(identity.IsCueBall, Is.True);
                Assert.That(cueBallObject, Is.Not.Null);
                Assert.That(cueBallObject.activeSelf, Is.False);

                var ballInHandPosition = new Vector3(-0.4f, 0.8f, 0.15f);
                capture.Restore(ballInHandPosition);

                Assert.That(cueBallObject.activeSelf, Is.True);
                Assert.That(identity.Id, Is.EqualTo(new BallId(BallId.CueBallNumber)));
                Assert.That(cueBallObject.transform.position, Is.EqualTo(ballInHandPosition));
                Assert.That(capture.IsCaptured, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cueBallObject);
                Object.DestroyImmediate(pocketObject);
            }
        }

        [Test]
        public void PocketCaptureVolume_UsesMetricTriggerGeometry()
        {
            var pocketObject = new GameObject("PocketCaptureGeometryTest");

            try
            {
                var volume = pocketObject.AddComponent<PocketCaptureVolume>();
                Assert.That(volume.TriggerCollider.isTrigger, Is.True);
                Assert.That(
                    volume.TriggerCollider.radius,
                    Is.EqualTo(BilliardsPhysicalSpecification.PocketCaptureRadiusMeters).Within(0.000001f));
                Assert.That(
                    BilliardsPhysicalSpecification.PocketCaptureDepthBelowBedMeters,
                    Is.GreaterThan(BilliardsPhysicalSpecification.PocketCaptureRadiusMeters),
                    "A ball resting on the bed must not overlap a pocket capture volume before it drops below the mouth.");
            }
            finally
            {
                Object.DestroyImmediate(pocketObject);
            }
        }

        private static void SetBallNumber(BallIdentity identity, int ballNumber)
        {
            var field = typeof(BallIdentity).GetField("ballNumber", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(identity, ballNumber);
        }
    }
}
