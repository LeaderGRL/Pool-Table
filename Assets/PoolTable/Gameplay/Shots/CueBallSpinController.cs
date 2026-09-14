using PoolTable.Core.Shots;
using PoolTable.Input;
using UnityEngine;

namespace PoolTable.Gameplay.Shots
{
    [DisallowMultipleComponent]
    public sealed class CueBallSpinController : MonoBehaviour
    {
        [SerializeField] private float normalizedUnitsPerPointerUnit = 0.01f;

        private MouseInputReader mouseInputReader;
        private CueBallSpinState spinState;

        public float NormalizedUnitsPerPointerUnit => normalizedUnitsPerPointerUnit;

        public CueBallSpin Spin => spinState?.Spin ?? default;

        private void Awake()
        {
            mouseInputReader = new MouseInputReader();
            spinState = new CueBallSpinState();
        }

        private void OnEnable()
        {
            if (mouseInputReader == null)
            {
                mouseInputReader = new MouseInputReader();
            }

            if (spinState == null)
            {
                spinState = new CueBallSpinState();
            }
        }

        private void Update()
        {
            ProcessInput(mouseInputReader.Read());
        }

        internal void ProcessInput(PointerInputSnapshot input)
        {
            if (spinState == null || !input.SecondaryButtonIsPressed)
            {
                return;
            }

            spinState.Adjust(
                input.Delta.x * normalizedUnitsPerPointerUnit,
                input.Delta.y * normalizedUnitsPerPointerUnit);
        }

        public void ResetToCenter()
        {
            spinState?.ResetToCenter();
        }
    }
}
