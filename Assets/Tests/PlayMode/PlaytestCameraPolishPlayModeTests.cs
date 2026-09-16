using System.Collections;
using System.Linq;
using NUnit.Framework;
using PoolTable.Gameplay.Aiming;
using PoolTable.Gameplay.BallInHand;
using PoolTable.Gameplay.Balls;
using PoolTable.Physics.Configuration;
using PoolTable.Presentation.Camera;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    [Category("Functional")]
    [Category("SceneSmoke")]
    public sealed class PlaytestCameraPolishPlayModeTests
    {
        [UnityTest]
        public IEnumerator AimingCamera_RuntimeViewUsesForwardEyeOffset()
        {
            yield return LoadPoolTableScene();

            var aimingController = Object.FindFirstObjectByType<CueAimingController>();
            var outputCamera = Camera.main;
            var cameraController = outputCamera.GetComponent<AimingCameraController>();

            Assert.That(aimingController, Is.Not.Null);
            Assert.That(cameraController, Is.Not.Null);
            Assert.That(cameraController.ForwardEyeOffsetMeters, Is.GreaterThan(0f));
            Assert.That(cameraController.EffectiveDistanceBehindCueBall, Is.LessThan(cameraController.DistanceBehindCueBall));

            var cueBallPosition = aimingController.CueBall.transform.position;
            var planarOffset = Vector3.ProjectOnPlane(outputCamera.transform.position - cueBallPosition, Vector3.up);
            Assert.That(
                planarOffset.magnitude,
                Is.EqualTo(cameraController.EffectiveDistanceBehindCueBall).Within(0.01f),
                "Runtime aiming camera must move forward from the old cue-butt position.");
        }

        [UnityTest]
        public IEnumerator BallInHandPlacement_UsesSideOverviewAndRestoresOutputCamera()
        {
            yield return LoadPoolTableScene();

            var controller = Object.FindObjectsByType<BallInHandPlacementController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single();
            var cueBall = Object.FindObjectsByType<BallIdentity>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(identity => identity.IsCueBall)
                .transform;
            var outputCamera = Camera.main;

            Assert.That(outputCamera, Is.Not.Null);
            Assert.That(controller.IsPlacing, Is.False);

            var originalPosition = outputCamera.transform.position;
            var originalRotation = outputCamera.transform.rotation;
            var originalFieldOfView = outputCamera.fieldOfView;
            controller.CursorStateAccessor = new TestCursorStateAccessor(CursorLockMode.Locked, false);

            controller.BeginLegacyScratchPlacement();

            Assert.That(controller.IsPlacing, Is.True);
            Assert.That(
                outputCamera.fieldOfView,
                Is.EqualTo(controller.PlacementCameraVerticalFov).Within(0.0001f));

            var tableCenter = new Vector3(
                0f,
                BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters,
                0f);
            var directionToCenter = (tableCenter - outputCamera.transform.position).normalized;
            Assert.That(Vector3.Dot(outputCamera.transform.forward, directionToCenter), Is.GreaterThan(0.999f));
            Assert.That(
                Mathf.Abs(outputCamera.transform.position.x),
                Is.LessThan(0.001f),
                "Ball-in-hand camera must stay centered on the long table axis.");
            Assert.That(
                outputCamera.transform.position.z,
                Is.LessThan(-BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f),
                "Ball-in-hand camera must sit outside the long side of the playing surface.");

            var halfLength = BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f;
            var leftEdge = outputCamera.WorldToViewportPoint(new Vector3(
                -halfLength,
                BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters,
                0f));
            var rightEdge = outputCamera.WorldToViewportPoint(new Vector3(
                halfLength,
                BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters,
                0f));

            Assert.That(leftEdge.z, Is.GreaterThan(0f));
            Assert.That(rightEdge.z, Is.GreaterThan(0f));
            Assert.That(leftEdge.x, Is.InRange(0.02f, 0.16f));
            Assert.That(rightEdge.x, Is.InRange(0.84f, 0.98f));

            cueBall.position = new Vector3(
                BilliardsPhysicalSpecification.HeadStringX,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                0f);
            Assert.That(controller.TryConfirmPlacement(), Is.True);

            Assert.That(controller.IsPlacing, Is.False);
            Assert.That(Vector3.Distance(outputCamera.transform.position, originalPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(outputCamera.transform.rotation, originalRotation), Is.LessThan(0.001f));
            Assert.That(outputCamera.fieldOfView, Is.EqualTo(originalFieldOfView).Within(0.0001f));
        }

        private static IEnumerator LoadPoolTableScene()
        {
            var loadOperation = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            yield return null;
        }

        private sealed class TestCursorStateAccessor : ICursorStateAccessor
        {
            public TestCursorStateAccessor(CursorLockMode lockState, bool visible)
            {
                LockState = lockState;
                Visible = visible;
            }

            public CursorLockMode LockState { get; set; }

            public bool Visible { get; set; }
        }
    }
}
