using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PoolTable.Gameplay.Aiming;
using PoolTable.Gameplay.BallInHand;
using PoolTable.Gameplay.Shots;
using PoolTable.Input;
using PoolTable.Presentation.Camera;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    [Category("Functional")]
    public sealed class AimingInteractionPlayModeTests : InputTestFixture
    {
        private const string PoolTableScenePath = "Assets/Scenes/PoolTable.unity";

        [Test]
        [Category("Input")]
        public void CueAimingController_RuntimePointerInputStagesElevationBeforeYaw()
        {
            var cueBall = new GameObject("StagedAimCueBall");
            var cue = new GameObject("StagedAimCue");

            try
            {
                cueBall.transform.position = Vector3.zero;
                cue.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1f), Quaternion.identity);

                var controller = cue.AddComponent<CueAimingController>();
                SetPrivateField(controller, "cueBall", cueBall);
                controller.enabled = false;
                controller.enabled = true;

                var initialDirection = new Vector2(controller.Direction.X, controller.Direction.Y);
                var initialElevation = controller.ElevationDegrees;

                controller.ProcessRuntimeInput(new LocalPlayerInputSnapshot(new Vector2(20f, 5f), false));

                Assert.That(controller.PointerAdjustmentPhase, Is.EqualTo(PointerAimPhase.Elevation));
                Assert.That(controller.ElevationDegrees, Is.GreaterThan(initialElevation));
                Assert.That(new Vector2(controller.Direction.X, controller.Direction.Y), Is.EqualTo(initialDirection));

                controller.BeginPointerYawAdjustment();
                var confirmedElevation = controller.ElevationDegrees;
                controller.ProcessRuntimeInput(new LocalPlayerInputSnapshot(new Vector2(20f, 5f), false));

                Assert.That(controller.PointerAdjustmentPhase, Is.EqualTo(PointerAimPhase.Yaw));
                Assert.That(controller.ElevationDegrees, Is.EqualTo(confirmedElevation).Within(0.0001f));
                Assert.That(new Vector2(controller.Direction.X, controller.Direction.Y), Is.EqualTo(initialDirection));

                controller.ProcessRuntimeInput(new LocalPlayerInputSnapshot(new Vector2(20f, 5f), false));
                Assert.That(controller.ElevationDegrees, Is.EqualTo(confirmedElevation).Within(0.0001f));
                Assert.That(
                    Vector2.Angle(initialDirection, new Vector2(controller.Direction.X, controller.Direction.Y)),
                    Is.GreaterThan(0.01f));

                controller.ResetPointerAdjustmentSequence();
                Assert.That(controller.PointerAdjustmentPhase, Is.EqualTo(PointerAimPhase.Elevation));
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(cueBall);
            }
        }

        [UnityTest]
        [Category("Input")]
        public IEnumerator PoolTableScene_MousePrimaryActionConfirmsElevationThenYawBeforeShotPower()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            yield return LoadPoolTableScene();

            var player = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(component => component.GetType().Name == "PlayersStateManagement");
            var aiming = player.GetComponent<CueAimingController>();
            var shotPower = player.GetComponent<ShotPowerController>();

            Assert.That(aiming, Is.Not.Null);
            Assert.That(shotPower, Is.Not.Null);
            Assert.That(aiming.PointerAdjustmentPhase, Is.EqualTo(PointerAimPhase.Elevation));
            Assert.That(shotPower.enabled, Is.False);
            Assert.That(GetCurrentPlayerStateName(player), Is.EqualTo("PlayersPlayState"));

            Press(mouse.leftButton);
            yield return null;

            Assert.That(aiming.PointerAdjustmentPhase, Is.EqualTo(PointerAimPhase.Yaw));
            Assert.That(GetCurrentPlayerStateName(player), Is.EqualTo("PlayersPlayState"));
            Assert.That(shotPower.enabled, Is.False, "The first click must only confirm cue elevation.");

            yield return null;
            Assert.That(GetCurrentPlayerStateName(player), Is.EqualTo("PlayersPlayState"), "Holding the first click must not skip the yaw phase.");

            Release(mouse.leftButton);
            yield return null;
            Press(mouse.leftButton);
            yield return null;

            Assert.That(GetCurrentPlayerStateName(player), Is.EqualTo("PlayersShootState"));
            Assert.That(shotPower.enabled, Is.True, "The second click must enter the existing shot-power state.");

            Release(mouse.leftButton);
        }

        [Test]
        [Category("Camera")]
        public void AimingCamera_UsesCloserPlayerStanceAndFramesBallInHandFromTableSide()
        {
            var cueBall = new GameObject("CameraTestCueBall");
            var cue = new GameObject("CameraTestCue");
            var cameraHost = new GameObject("CameraTestOutput");

            try
            {
                cueBall.transform.position = new Vector3(0f, 0.85f, 0f);
                cue.transform.SetPositionAndRotation(new Vector3(0f, 0.85f, -1f), Quaternion.identity);
                var aiming = cue.AddComponent<CueAimingController>();
                SetPrivateField(aiming, "cueBall", cueBall);
                aiming.enabled = false;
                aiming.enabled = true;

                var outputCamera = cameraHost.AddComponent<UnityEngine.Camera>();
                outputCamera.aspect = 16f / 9f;
                outputCamera.fieldOfView = 60f;
                var cameraController = cameraHost.AddComponent<AimingCameraController>();
                SetPrivateField(cameraController, "aimingController", aiming);
                SetPrivateField(cameraController, "ballInHandPlacementController", null);

                Assert.That(
                    cameraController.TryGetPresentationDesiredPose(out var aimingPosition, out _, out var aimingFieldOfView),
                    Is.True);
                var planarStanceDistance = Vector3.ProjectOnPlane(
                    aimingPosition - cueBall.transform.position,
                    Vector3.up).magnitude;
                Assert.That(planarStanceDistance, Is.EqualTo(cameraController.PlayerViewDistanceBehindCueBall).Within(0.0001f));
                Assert.That(planarStanceDistance, Is.LessThan(aiming.CueDistance));
                Assert.That(aimingFieldOfView, Is.EqualTo(cameraController.PlayerViewFieldOfView).Within(0.0001f));

                Assert.That(
                    cameraController.TryGetBallInHandPose(out var placementPosition, out var placementRotation, out var placementFieldOfView),
                    Is.True);
                Assert.That(placementPosition.z, Is.LessThan(BallInHandPlacementGeometry.MinimumCenterZ));
                Assert.That(placementPosition.x, Is.EqualTo(0f).Within(0.0001f));

                cameraHost.transform.SetPositionAndRotation(placementPosition, placementRotation);
                outputCamera.fieldOfView = placementFieldOfView;

                var tableHeight = cueBall.transform.position.y;
                var corners = new[]
                {
                    new Vector3(BallInHandPlacementGeometry.MinimumCenterX, tableHeight, BallInHandPlacementGeometry.MinimumCenterZ),
                    new Vector3(BallInHandPlacementGeometry.MinimumCenterX, tableHeight, BallInHandPlacementGeometry.MaximumCenterZ),
                    new Vector3(BallInHandPlacementGeometry.MaximumCenterX, tableHeight, BallInHandPlacementGeometry.MinimumCenterZ),
                    new Vector3(BallInHandPlacementGeometry.MaximumCenterX, tableHeight, BallInHandPlacementGeometry.MaximumCenterZ),
                };

                foreach (var corner in corners)
                {
                    var viewportPoint = outputCamera.WorldToViewportPoint(corner);
                    Assert.That(viewportPoint.z, Is.GreaterThan(0f));
                    Assert.That(viewportPoint.x, Is.InRange(0f, 1f));
                    Assert.That(viewportPoint.y, Is.InRange(0f, 1f));
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraHost);
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(cueBall);
            }
        }

        private static IEnumerator LoadPoolTableScene()
        {
            var load = SceneManager.LoadSceneAsync(PoolTableScenePath, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static string GetCurrentPlayerStateName(MonoBehaviour player)
        {
            var field = player.GetType().GetField("currentPlayerState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(player)?.GetType().Name;
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {typeof(T).Name}.{fieldName}.");
            field.SetValue(target, value);
        }
    }
}
