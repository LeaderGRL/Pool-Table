using UnityEngine;
using UnityEngine.InputSystem;

internal static class LegacyMouseInput
{
    private const float LegacyAxisSensitivity = 0.1f;

    public static bool PointerPrimaryButtonIsPressed => Mouse.current?.leftButton.isPressed ?? false;

    public static bool GamepadPrimaryButtonIsPressed => Gamepad.current?.rightTrigger.isPressed ?? false;

    public static bool PrimaryButtonIsPressed =>
        PointerPrimaryButtonIsPressed || GamepadPrimaryButtonIsPressed;

    public static Vector2 Delta
    {
        get
        {
            var mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.delta.ReadValue() * LegacyAxisSensitivity;
        }
    }
}
