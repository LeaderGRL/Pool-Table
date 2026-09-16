using System.Collections;
using System.Linq;
using System.Reflection;
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
        [Test]
        public void PlacementCamera_RunsAfterGameplayCameraControllers()
        {
            var placementOrder = typeof(BallInHandPlacementController)
                .GetCustomAttribute<DefaultExecutionOrder>()
                ?.order;
            var aimingOrder = typeof(AimingCameraController)
                .GetCustomAttribute<DefaultExecutionOrder>()
                ?.order;
            var shotOrder = typeof(ShotCameraController)
                .GetCustomAttribute<DefaultExecutionOrder>()
                ?.order;
            var spectateOrder = typeof(SpectateCameraController)
                .GetCustomAttribute<DefaultExecutionOrder>()
                ?.order;

            Assert.That(placementOrder, Is.Not.Null);
            Assert.That(aimingOrder, Is.Not.Null);
            Assert.That(shotOrder, Is.Not.Null);
            Assert.That(spectateOrder, Is.Not.Null);
            Assert.That(placementOrder.Value, Is.GreaterThan(aimingOrder.Value));
            Assert.That(placementOrder.Value, Is.GreaterThan(shotOrder.Value));
            Assert.That(placementOrder.Value, Is.GreaterThan(spectateOrder.Value));
        }

        [UnityTest]
        public IEnumerator GameplayCameras_RuntimeViewMatchesCueReferencePosition()
        {
            yield return LoadPoolTableScene();

            var aimingController = Object.FindFirstObjectByType<CueAimingController>();
            var outputCamera = Camera.main;
            var cameraController = outputCamera.GetComponent<AimingCameraController>();
            var shotCameraController = outputCamera.GetComponent<ShotCameraController>();

            Assert.That(aimingController, Is.Not.Null);
            Assert.That(cameraController, Is.Not.Null);
            Assert.That(shotCameraController, Is.Not.Null);
            Assert.That(cameraController.ForwardEyeOffsetMeters, Is.GreaterThan(0f));
            Assert.That(cameraController.EffectiveDistanceBehindCueBall, Is.LessThan(cameraController.DistanceBehindCueBall));
            Assert.That(
                cameraController.EffectiveDistanceBehindCueBall,
                Is.InRange(1f, 1.1f),
                "Runtime aiming view should sit near the player's head position along the cue instead of behind the cue butt.");
            Assert.That(
                cameraController.EffectiveDistanceBehindCueBall / aimingController.CueDistance,
                Is.InRange(0.58f, 0.68f),
                "The aiming eye should sit around the user-marked point slightly past the middle of the cue.");
            Assert.That(
                cameraController.EffectiveHeightAboveCueBall,
                Is.InRange(0.15f, 0.22f),
                "The close aiming eye should stay low enough to sight along the cue instead of looking over it.");
            Assert.That(
                shotCameraController.EffectiveDistanceBehindCueBall,
                Is.InRange(1.05f, 1.15f),
                "Shot-power presentation should keep the same close player-eye framing before pullback adds distance.");

            var cueBallPosition = aimingController.CueBall.transform.position;
            var planarOffset = Vector3.ProjectOnPlane(outputCamera.transform.position - cueBallPosition, Vector3.up);
            Assert.That(
                planarOffset.magnitude,
                Is.EqualTo(cameraController.EffectiveDistanceBehindCueBall).Within(0.01f),
                "Runtime aiming camera must move forward from the old cue-butt position.");
        }

        [UnityTest]
        public IEnumerator AimingCamera_FollowsCueElevationDuringPitchStage()
        {
            yield return LoadPoolTableScene();

            var aimingController = Object.FindFirstObjectByType<CueAimingController>();
            var outputCamera = Camera.main;
            var cameraController = outputCamera.GetComponent<AimingCameraController>();
            var originalAspect = outputCamera.aspect;
            outputCamera.aspect = 16f / 10f;
            outputCamera.ResetProjectionMatrix();

            Assert.That(aimingController, Is.Not.Null);
            Assert.That(cameraController, Is.Not.Null);
            Assert.That(aimingController.AimStage, Is.EqualTo(CueAimStage.Yaw));

            Assert.That(cameraController.ApplyRuntimeCameraPose(0f, true), Is.True);
            var directionBeforePitch = new Vector2(aimingController.Direction.X, aimingController.Direction.Y);
            var cameraPositionBeforePitch = outputCamera.transform.position;
            var cameraForwardBeforePitch = outputCamera.transform.forward;

            AssertCueReadableInFrame(outputCamera, aimingController);

            aimingController.AdvanceAimStage();
            Assert.That(aimingController.AimStage, Is.EqualTo(CueAimStage.Elevation));
            yield return null;

            aimingController.ProcessStagedInput(new PoolTable.Input.LocalPlayerInputSnapshot(new Vector2(0f, 100f), false));
            Assert.That(aimingController.ElevationDegrees, Is.GreaterThan(0f));
            Assert.That(
                Vector2.Angle(directionBeforePitch, new Vector2(aimingController.Direction.X, aimingController.Direction.Y)),
                Is.LessThan(0.001f),
                "Pitch adjustment must not alter the locked yaw direction.");

            Assert.That(cameraController.ApplyRuntimeCameraPose(0f, true), Is.True);

            Assert.That(
                outputCamera.transform.position.y,
                Is.GreaterThan(cameraPositionBeforePitch.y + 0.01f),
                "The aiming camera must rise with the elevated cue instead of staying on a planar orbit.");
            Assert.That(
                Quaternion.Angle(Quaternion.LookRotation(cameraForwardBeforePitch), outputCamera.transform.rotation),
                Is.GreaterThan(0.1f),
                "The aiming camera orientation must follow cue elevation.");
            Assert.That(
                outputCamera.transform.forward.y,
                Is.LessThan(cameraForwardBeforePitch.y - 0.001f),
                "Increasing cue elevation must pitch the camera downward along the cue line.");

            AssertCueReadableInFrame(outputCamera, aimingController);

            outputCamera.aspect = originalAspect;
            outputCamera.ResetProjectionMatrix();
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
            var originalAspect = outputCamera.aspect;
            controller.CursorStateAccessor = new TestCursorStateAccessor(CursorLockMode.Locked, false);

            outputCamera.aspect = 16f / 9f;

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

            AssertPlayingSurfaceCornersVisible(outputCamera, 0.8f);

            cueBall.position = new Vector3(
                BilliardsPhysicalSpecification.HeadStringX,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                0f);
            Assert.That(controller.TryConfirmPlacement(), Is.True);

            Assert.That(controller.IsPlacing, Is.False);
            Assert.That(Vector3.Distance(outputCamera.transform.position, originalPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(outputCamera.transform.rotation, originalRotation), Is.LessThan(0.001f));
            Assert.That(outputCamera.fieldOfView, Is.EqualTo(originalFieldOfView).Within(0.0001f));

            outputCamera.aspect = 32f / 9f;
            controller.BeginLegacyScratchPlacement();

            Assert.That(controller.IsPlacing, Is.True);
            AssertPlayingSurfaceCornersVisible(outputCamera, 0.6f);

            cueBall.position = new Vector3(
                BilliardsPhysicalSpecification.HeadStringX,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                0f);
            Assert.That(controller.TryConfirmPlacement(), Is.True);
            outputCamera.aspect = originalAspect;
        }

        private static void AssertPlayingSurfaceCornersVisible(Camera camera, float minimumHorizontalFill)
        {
            var halfLength = BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f;
            var halfWidth = BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f;
            var bedHeight = BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters;
            var corners = new[]
            {
                new Vector3(-halfLength, bedHeight, -halfWidth),
                new Vector3(-halfLength, bedHeight, halfWidth),
                new Vector3(halfLength, bedHeight, -halfWidth),
                new Vector3(halfLength, bedHeight, halfWidth),
            };

            var viewportPoints = corners.Select(camera.WorldToViewportPoint).ToArray();
            foreach (var point in viewportPoints)
            {
                Assert.That(point.z, Is.GreaterThan(0f));
                Assert.That(point.x, Is.InRange(0f, 1f));
                Assert.That(point.y, Is.InRange(0f, 1f));
            }

            var horizontalFill = viewportPoints.Max(point => point.x) - viewportPoints.Min(point => point.x);
            Assert.That(
                horizontalFill,
                Is.GreaterThanOrEqualTo(minimumHorizontalFill),
                "Placement view should keep the table large in frame while preserving every corner.");
        }

        private static void AssertCueReadableInFrame(Camera camera, CueAimingController aimingController)
        {
            var cueBallPosition = aimingController.CueBall.transform.position;
            var cueButtPosition = aimingController.transform.position;
            var visibleFractions = new[] { 0.15f, 0.25f, 0.35f };

            foreach (var fraction in visibleFractions)
            {
                var cueSample = Vector3.Lerp(cueBallPosition, cueButtPosition, fraction);
                var cueViewportPoint = camera.WorldToViewportPoint(cueSample);

                Assert.That(
                    cueViewportPoint.z,
                    Is.GreaterThan(camera.nearClipPlane),
                    $"Cue sample {fraction:P0} must stay in front of the camera near plane.");
                Assert.That(
                    cueViewportPoint.x,
                    Is.InRange(0.03f, 0.97f),
                    $"Cue sample {fraction:P0} must remain horizontally readable in the player view.");
                Assert.That(
                    cueViewportPoint.y,
                    Is.InRange(0.03f, 0.97f),
                    $"Cue sample {fraction:P0} must remain vertically readable in the player view.");
            }

            var cueButtCameraSpace = camera.transform.InverseTransformPoint(cueButtPosition);
            Assert.That(
                cueButtCameraSpace.z,
                Is.LessThanOrEqualTo(camera.nearClipPlane),
                "The cue butt must stay behind the player camera so the entire cue cannot be visible.");
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
