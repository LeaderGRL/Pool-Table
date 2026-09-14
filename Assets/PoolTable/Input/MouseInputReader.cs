using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PoolTable.Input
{
    public readonly struct PointerInputSnapshot
    {
        public PointerInputSnapshot(
            Vector2 delta,
            bool primaryButtonIsPressed,
            bool secondaryButtonIsPressed = false)
        {
            Delta = delta;
            PrimaryButtonIsPressed = primaryButtonIsPressed;
            SecondaryButtonIsPressed = secondaryButtonIsPressed;
        }

        public Vector2 Delta { get; }

        public bool PrimaryButtonIsPressed { get; }

        public bool SecondaryButtonIsPressed { get; }
    }

    public sealed class MouseInputReader
    {
        private readonly Func<Vector2> readDelta;
        private readonly Func<bool> readPrimaryButton;
        private readonly Func<bool> readSecondaryButton;

        public MouseInputReader()
            : this(ReadCurrentDelta, ReadCurrentPrimaryButton, ReadCurrentSecondaryButton)
        {
        }

        internal MouseInputReader(Func<Vector2> readDelta, Func<bool> readPrimaryButton)
            : this(readDelta, readPrimaryButton, () => false)
        {
        }

        internal MouseInputReader(
            Func<Vector2> readDelta,
            Func<bool> readPrimaryButton,
            Func<bool> readSecondaryButton)
        {
            this.readDelta = readDelta ?? throw new ArgumentNullException(nameof(readDelta));
            this.readPrimaryButton = readPrimaryButton ?? throw new ArgumentNullException(nameof(readPrimaryButton));
            this.readSecondaryButton = readSecondaryButton ?? throw new ArgumentNullException(nameof(readSecondaryButton));
        }

        public PointerInputSnapshot Read()
        {
            return new PointerInputSnapshot(readDelta(), readPrimaryButton(), readSecondaryButton());
        }

        private static Vector2 ReadCurrentDelta()
        {
            var mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.delta.ReadValue();
        }

        private static bool ReadCurrentPrimaryButton()
        {
            return Mouse.current?.leftButton.isPressed ?? false;
        }

        private static bool ReadCurrentSecondaryButton()
        {
            return Mouse.current?.rightButton.isPressed ?? false;
        }
    }
}
