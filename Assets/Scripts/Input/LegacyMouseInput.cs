using UnityEngine;
using UnityEngine.InputSystem;

internal static class LegacyMouseInput
{
    private const float LegacyAxisSensitivity = 0.1f;

    public static bool PrimaryButtonIsPressed =>
        (Mouse.current?.leftButton.isPressed ?? false)
        || GamepadPrimaryButtonIsPressed;

    public static bool GamepadPrimaryButtonIsPressed =>
        Gamepad.current?.rightTrigger.isPressed ?? false;

    public static bool MousePrimaryButtonWasPressedThisFrame =>
        Mouse.current?.leftButton.wasPressedThisFrame ?? false;

    public static bool GamepadPrimaryButtonWasPressedThisFrame =>
        Gamepad.current?.rightTrigger.wasPressedThisFrame ?? false;

    public static bool PrimaryButtonWasPressedThisFrame =>
        MousePrimaryButtonWasPressedThisFrame || GamepadPrimaryButtonWasPressedThisFrame;

    public static Vector2 Delta
    {
        get
        {
            var mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.delta.ReadValue() * LegacyAxisSensitivity;
        }
    }
}
