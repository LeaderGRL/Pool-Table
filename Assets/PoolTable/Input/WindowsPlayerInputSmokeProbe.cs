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
            SetGamepadState(0.75f, -0.25f, -0.4f, 0.8f, 1f, 1f);
        }

        public static void SetGamepadState(
            float aimX,
            float aimY,
            float actionX,
            float actionY,
            float secondaryAction,
            float primaryAction)
        {
            if (smokeGamepad == null)
            {
                smokeGamepad = InputSystem.AddDevice<Gamepad>();
            }

            var state = new GamepadState
            {
                leftStick = new Vector2(aimX, aimY),
                rightStick = new Vector2(actionX, actionY),
                leftTrigger = secondaryAction,
                rightTrigger = primaryAction,
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
