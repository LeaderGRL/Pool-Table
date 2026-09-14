using NUnit.Framework;
using PoolTable.Gameplay.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class CueBallSpinStateTests
    {
        [Test]
        public void Set_StoresNormalizedCueTipContact()
        {
            var state = new CueBallSpinState();

            state.Set(0.25f, -0.5f);

            Assert.That(state.Spin.Side, Is.EqualTo(0.25f).Within(0.000001f));
            Assert.That(state.Spin.Vertical, Is.EqualTo(-0.5f).Within(0.000001f));
        }

        [Test]
        public void Set_ClampsContactToUnitDisc()
        {
            var state = new CueBallSpinState();

            state.Set(1f, 1f);

            Assert.That(state.Spin.Side, Is.EqualTo(0.70710677f).Within(0.000001f));
            Assert.That(state.Spin.Vertical, Is.EqualTo(0.70710677f).Within(0.000001f));
        }

        [Test]
        public void Adjust_AccumulatesAndKeepsContactInsideUnitDisc()
        {
            var state = new CueBallSpinState();

            state.Adjust(0.5f, 0f);
            state.Adjust(0f, -2f);

            var magnitudeSquared =
                (state.Spin.Side * state.Spin.Side) + (state.Spin.Vertical * state.Spin.Vertical);
            Assert.That(magnitudeSquared, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(state.Spin.Vertical, Is.LessThan(0f));
        }

        [Test]
        public void ResetToCenter_ClearsSpin()
        {
            var state = new CueBallSpinState();
            state.Set(-0.4f, 0.3f);

            state.ResetToCenter();

            Assert.That(state.Spin.IsCentered, Is.True);
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(0f, float.NegativeInfinity)]
        public void Set_RejectsNonFiniteInput(float side, float vertical)
        {
            var state = new CueBallSpinState();

            Assert.That(() => state.Set(side, vertical), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
