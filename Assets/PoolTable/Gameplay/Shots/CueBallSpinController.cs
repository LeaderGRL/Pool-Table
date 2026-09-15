using PoolTable.Core.Shots;
using PoolTable.Input;
using UnityEngine;

namespace PoolTable.Gameplay.Shots
{
    [DisallowMultipleComponent]
    public sealed class CueBallSpinController : MonoBehaviour
    {
        [SerializeField] private float normalizedUnitsPerPointerUnit = 0.01f;

        private LocalPlayerInputReader localPlayerInputReader;
        private CueBallSpinState spinState;

        public float NormalizedUnitsPerPointerUnit => normalizedUnitsPerPointerUnit;

        public CueBallSpin Spin => spinState?.Spin ?? default;

        private void Awake()
        {
            localPlayerInputReader = new LocalPlayerInputReader();
            spinState = new CueBallSpinState();
        }

        private void OnEnable()
        {
            if (localPlayerInputReader == null)
            {
                localPlayerInputReader = new LocalPlayerInputReader();
            }

            if (spinState == null)
            {
                spinState = new CueBallSpinState();
            }
        }

        private void Update()
        {
            ProcessInput(localPlayerInputReader.Read());
        }

        internal void ProcessInput(LocalPlayerInputSnapshot input)
        {
            if (spinState == null || !input.SecondaryActionIsPressed)
            {
                return;
            }

            spinState.Adjust(
                (input.PointerDelta.x + input.ActionAxis.x * 100f * Time.deltaTime) * normalizedUnitsPerPointerUnit,
                (input.PointerDelta.y + input.ActionAxis.y * 100f * Time.deltaTime) * normalizedUnitsPerPointerUnit);
        }

        public void ResetToCenter()
        {
            spinState?.ResetToCenter();
        }
    }
}
