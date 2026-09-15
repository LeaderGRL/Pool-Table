#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PoolTable.Input
{
    public static class WindowsPlayerInputSmokeProbe
    {
        private static Gamepad smokeGamepad;

        public static void InjectGamepadState()
        {
            RemoveGamepad();

            smokeGamepad = InputSystem.AddDevice<Gamepad>();
            var state = new GamepadState
            {
                leftStick = new Vector2(0.75f, -0.25f),
                rightStick = new Vector2(-0.4f, 0.8f),
                leftTrigger = 1f,
                rightTrigger = 1f,
            };

            InputSystem.QueueStateEvent(smokeGamepad, state);
            InputSystem.Update();
        }

        public static void RemoveGamepad()
        {
            if (smokeGamepad == null)
            {
                return;
            }

            InputSystem.RemoveDevice(smokeGamepad);
            smokeGamepad = null;
        }
    }
}
#endif
