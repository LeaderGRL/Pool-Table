using UnityEngine;
using UnityEngine.InputSystem;

internal static class LegacyMouseInput
{
    private const float LegacyAxisSensitivity = 0.1f;

    public static bool PrimaryButtonIsPressed =>
        (Mouse.current?.leftButton.isPressed ?? false)
        || (Gamepad.current?.rightTrigger.isPressed ?? false);

    public static Vector2 Delta
    {
        get
        {
            var mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.delta.ReadValue() * LegacyAxisSensitivity;
        }
    }
}
