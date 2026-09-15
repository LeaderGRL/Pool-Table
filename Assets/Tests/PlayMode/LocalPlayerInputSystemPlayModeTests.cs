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
        public IEnumerator CueAimingController_UpdateConsumesVirtualGamepadInput()
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
            Set(gamepad.leftStick, Vector2.right);

            yield return null;

            var updatedDirection = new Vector2(controller.Direction.X, controller.Direction.Y);
            Assert.That(
                Vector2.Angle(initialDirection, updatedDirection),
                Is.GreaterThan(0.01f),
                "The controller Update loop must consume the current Unity Input System gamepad state.");

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
