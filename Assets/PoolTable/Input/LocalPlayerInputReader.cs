using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PoolTable.Input
{
    public readonly struct LocalPlayerInputSnapshot
    {
        public LocalPlayerInputSnapshot(
            Vector2 pointerDelta,
            bool primaryActionIsPressed,
            bool secondaryActionIsPressed = false,
            Vector2 aimAxis = default,
            Vector2 actionAxis = default,
            Vector2 pointerPosition = default,
            bool hasPointerPosition = false)
        {
            PointerDelta = pointerDelta;
            PrimaryActionIsPressed = primaryActionIsPressed;
            SecondaryActionIsPressed = secondaryActionIsPressed;
            AimAxis = aimAxis;
            ActionAxis = actionAxis;
            PointerPosition = pointerPosition;
            HasPointerPosition = hasPointerPosition;
        }

        public Vector2 PointerDelta { get; }

        public bool PrimaryActionIsPressed { get; }

        public bool SecondaryActionIsPressed { get; }

        public Vector2 AimAxis { get; }

        public Vector2 ActionAxis { get; }

        public Vector2 PointerPosition { get; }

        public bool HasPointerPosition { get; }
    }

    public sealed class LocalPlayerInputReader
    {
        internal const float DefaultStickDeadzone = 0.15f;

        private readonly Func<Vector2> readPointerDelta;
        private readonly Func<bool> readPointerPrimaryAction;
        private readonly Func<bool> readPointerSecondaryAction;
        private readonly Func<Vector2> readAimAxis;
        private readonly Func<Vector2> readActionAxis;
        private readonly Func<bool> readGamepadPrimaryAction;
        private readonly Func<bool> readGamepadSecondaryAction;
        private readonly Func<Vector2> readPointerPosition;
        private readonly Func<bool> readPointerPositionAvailability;
        private readonly float stickDeadzone;

        public LocalPlayerInputReader()
            : this(
                ReadCurrentPointerDelta,
                ReadCurrentPointerPrimaryAction,
                ReadCurrentPointerSecondaryAction,
                ReadCurrentAimAxis,
                ReadCurrentActionAxis,
                ReadCurrentGamepadPrimaryAction,
                ReadCurrentGamepadSecondaryAction,
                DefaultStickDeadzone,
                ReadCurrentPointerPosition,
                HasCurrentPointerPosition)
        {
        }

        internal LocalPlayerInputReader(
            Func<Vector2> readPointerDelta,
            Func<bool> readPointerPrimaryAction,
            Func<bool> readPointerSecondaryAction,
            Func<Vector2> readAimAxis,
            Func<Vector2> readActionAxis,
            Func<bool> readGamepadPrimaryAction,
            Func<bool> readGamepadSecondaryAction,
            float stickDeadzone = DefaultStickDeadzone,
            Func<Vector2> readPointerPosition = null,
            Func<bool> readPointerPositionAvailability = null)
        {
            this.readPointerDelta = readPointerDelta ?? throw new ArgumentNullException(nameof(readPointerDelta));
            this.readPointerPrimaryAction = readPointerPrimaryAction ?? throw new ArgumentNullException(nameof(readPointerPrimaryAction));
            this.readPointerSecondaryAction = readPointerSecondaryAction ?? throw new ArgumentNullException(nameof(readPointerSecondaryAction));
            this.readAimAxis = readAimAxis ?? throw new ArgumentNullException(nameof(readAimAxis));
            this.readActionAxis = readActionAxis ?? throw new ArgumentNullException(nameof(readActionAxis));
            this.readGamepadPrimaryAction = readGamepadPrimaryAction ?? throw new ArgumentNullException(nameof(readGamepadPrimaryAction));
            this.readGamepadSecondaryAction = readGamepadSecondaryAction ?? throw new ArgumentNullException(nameof(readGamepadSecondaryAction));
            this.readPointerPosition = readPointerPosition ?? ReadCurrentPointerPosition;
            this.readPointerPositionAvailability = readPointerPositionAvailability ?? HasCurrentPointerPosition;

            if (!float.IsFinite(stickDeadzone) || stickDeadzone < 0f || stickDeadzone >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(stickDeadzone));
            }

            this.stickDeadzone = stickDeadzone;
        }

        public LocalPlayerInputSnapshot Read()
        {
            return new LocalPlayerInputSnapshot(
                readPointerDelta(),
                readPointerPrimaryAction() || readGamepadPrimaryAction(),
                readPointerSecondaryAction() || readGamepadSecondaryAction(),
                readAimAxis(),
                readActionAxis(),
                readPointerPosition(),
                readPointerPositionAvailability());
        }

        internal static Vector2 ApplyRadialDeadzone(Vector2 value, float deadzone)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
            {
                return Vector2.zero;
            }

            var magnitude = value.magnitude;
            if (magnitude <= deadzone)
            {
                return Vector2.zero;
            }

            var normalizedMagnitude = Mathf.Clamp01((magnitude - deadzone) / (1f - deadzone));
            return value.normalized * normalizedMagnitude;
        }

        private static Vector2 ReadCurrentPointerDelta()
        {
            var mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.delta.ReadValue();
        }

        private static Vector2 ReadCurrentPointerPosition()
        {
            var mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.position.ReadValue();
        }

        private static bool HasCurrentPointerPosition()
        {
            return Mouse.current != null;
        }

        private static bool ReadCurrentPointerPrimaryAction()
        {
            return Mouse.current?.leftButton.isPressed ?? false;
        }

        private static bool ReadCurrentPointerSecondaryAction()
        {
            return Mouse.current?.rightButton.isPressed ?? false;
        }

        private static Vector2 ReadCurrentAimAxis()
        {
            return Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero;
        }

        private static Vector2 ReadCurrentActionAxis()
        {
            return Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
        }

        private static bool ReadCurrentGamepadPrimaryAction()
        {
            return Gamepad.current?.rightTrigger.isPressed ?? false;
        }

        private static bool ReadCurrentGamepadSecondaryAction()
        {
            return Gamepad.current?.leftTrigger.isPressed ?? false;
        }
    }
}
