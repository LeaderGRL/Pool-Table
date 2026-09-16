using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PoolTable.Gameplay.Aiming;
using PoolTable.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    [Category("Functional")]
    [Category("Input")]
    public sealed class LocalPlayerInputSystemPlayModeTests : InputTestFixture
    {
        [Test]
        public void LocalPlayerInputReader_ReadsVirtualMouseThroughUnityInputSystem()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var reader = new LocalPlayerInputReader();
            var expectedDelta = new Vector2(18.5f, -6.25f);
            var expectedPosition = new Vector2(640f, 360f);

            var state = new MouseState
                {
                    delta = expectedDelta,
                    position = expectedPosition,
                }
                .WithButton(MouseButton.Left)
                .WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse, state);
            InputSystem.Update();

            var snapshot = reader.Read();

            Assert.That(snapshot.PointerDelta, Is.EqualTo(expectedDelta));
            Assert.That(snapshot.PointerPosition, Is.EqualTo(expectedPosition));
            Assert.That(snapshot.HasPointerPosition, Is.True);
            Assert.That(snapshot.PrimaryActionIsPressed, Is.True);
            Assert.That(snapshot.SecondaryActionIsPressed, Is.True);
        }

        [Test]
        public void LocalPlayerInputReader_ReadsVirtualGamepadThroughUnityInputSystem()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var reader = new LocalPlayerInputReader();
            var aim = new Vector2(0.7f, -0.2f);
            var action = new Vector2(-0.35f, 0.8f);

            Set(gamepad.leftStick, aim);
            Set(gamepad.rightStick, action);
            Press(gamepad.rightTrigger);
            Press(gamepad.leftTrigger);

            var snapshot = reader.Read();
            var processedAim = gamepad.leftStick.ReadValue();
            var processedAction = gamepad.rightStick.ReadValue();

            Assert.That(Vector2.Distance(snapshot.AimAxis, processedAim), Is.LessThan(0.0001f));
            Assert.That(Vector2.Distance(snapshot.ActionAxis, processedAction), Is.LessThan(0.0001f));
            Assert.That(snapshot.AimAxis.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(snapshot.ActionAxis.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(snapshot.PrimaryActionIsPressed, Is.True);
            Assert.That(snapshot.SecondaryActionIsPressed, Is.True);
        }

        [UnityTest]
        public IEnumerator CueAimingController_UpdateConsumesStagedVirtualGamepadInput()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var cueBall = new GameObject("FunctionalTestCueBall");
            var cue = new GameObject("FunctionalTestCue");
            cueBall.transform.position = Vector3.zero;
            cue.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1f), Quaternion.identity);

            var controller = cue.AddComponent<CueAimingController>();
            SetPrivateField(controller, "cueBall", cueBall);
            controller.enabled = false;
            controller.enabled = true;

            var initialDirection = new Vector2(controller.Direction.X, controller.Direction.Y);
            var initialElevation = controller.ElevationDegrees;
            Assert.That(controller.AimStage, Is.EqualTo(CueAimStage.Elevation));

            Set(gamepad.leftStick, Vector2.up);
            yield return null;

            var elevationDirection = new Vector2(controller.Direction.X, controller.Direction.Y);
            Assert.That(Vector2.Angle(initialDirection, elevationDirection), Is.LessThan(0.001f));
            Assert.That(controller.ElevationDegrees, Is.GreaterThan(initialElevation));

            Set(gamepad.leftStick, Vector2.zero);
            yield return null;
            var lockedElevation = controller.ElevationDegrees;

            controller.AdvanceAimStage();
            Assert.That(controller.AimStage, Is.EqualTo(CueAimStage.Yaw));
            Set(gamepad.leftStick, Vector2.right);
            yield return null;

            var yawDirection = new Vector2(controller.Direction.X, controller.Direction.Y);
            Assert.That(
                Vector2.Angle(elevationDirection, yawDirection),
                Is.GreaterThan(0.01f),
                "Yaw input must become active only after elevation is confirmed.");
            Assert.That(controller.ElevationDegrees, Is.EqualTo(lockedElevation).Within(0.0001f));

            controller.AdvanceAimStage();
            Assert.That(controller.AimStage, Is.EqualTo(CueAimStage.Locked));
            var lockedDirection = new Vector2(controller.Direction.X, controller.Direction.Y);
            Set(gamepad.leftStick, new Vector2(-1f, -1f));
            yield return null;

            Assert.That(Vector2.Angle(lockedDirection, new Vector2(controller.Direction.X, controller.Direction.Y)), Is.LessThan(0.001f));
            Assert.That(controller.ElevationDegrees, Is.EqualTo(lockedElevation).Within(0.0001f));

            Object.Destroy(cue);
            Object.Destroy(cueBall);
            yield return null;
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {typeof(T).Name}.{fieldName}.");
            field.SetValue(target, value);
        }
    }
}
