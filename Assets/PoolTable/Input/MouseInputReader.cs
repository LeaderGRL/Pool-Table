using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PoolTable.Input
{
    public readonly struct PointerInputSnapshot
    {
        public PointerInputSnapshot(Vector2 delta, bool primaryButtonIsPressed)
        {
            Delta = delta;
            PrimaryButtonIsPressed = primaryButtonIsPressed;
        }

        public Vector2 Delta { get; }

        public bool PrimaryButtonIsPressed { get; }
    }

    public sealed class MouseInputReader
    {
        private readonly Func<Vector2> readDelta;
        private readonly Func<bool> readPrimaryButton;

        public MouseInputReader()
            : this(ReadCurrentDelta, ReadCurrentPrimaryButton)
        {
        }

        internal MouseInputReader(Func<Vector2> readDelta, Func<bool> readPrimaryButton)
        {
            this.readDelta = readDelta ?? throw new ArgumentNullException(nameof(readDelta));
            this.readPrimaryButton = readPrimaryButton ?? throw new ArgumentNullException(nameof(readPrimaryButton));
        }

        public PointerInputSnapshot Read()
        {
            return new PointerInputSnapshot(readDelta(), readPrimaryButton());
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
    }
}
