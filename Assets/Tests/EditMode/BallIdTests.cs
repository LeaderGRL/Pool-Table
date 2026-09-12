using System;
using NUnit.Framework;
using PoolTable.Core.Balls;

namespace PoolTable.Tests.EditMode
{
    public sealed class BallIdTests
    {
        [TestCase(0, BallGroup.None)]
        [TestCase(1, BallGroup.Solids)]
        [TestCase(7, BallGroup.Solids)]
        [TestCase(8, BallGroup.None)]
        [TestCase(9, BallGroup.Stripes)]
        [TestCase(15, BallGroup.Stripes)]
        public void BallId_ClassifiesExpectedGroup(int number, BallGroup expectedGroup)
        {
            var id = new BallId(number);

            Assert.That(id.Group, Is.EqualTo(expectedGroup));
        }

        [Test]
        public void BallId_IdentifiesCueAndEightBalls()
        {
            var cueBall = new BallId(BallId.CueBallNumber);
            var eightBall = new BallId(BallId.EightBallNumber);

            Assert.That(cueBall.IsCueBall, Is.True);
            Assert.That(cueBall.IsEightBall, Is.False);
            Assert.That(eightBall.IsCueBall, Is.False);
            Assert.That(eightBall.IsEightBall, Is.True);
        }

        [TestCase(-1)]
        [TestCase(16)]
        public void BallId_RejectsNumbersOutsideRackRange(int number)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BallId(number));
        }

        [Test]
        public void BallId_UsesValueEquality()
        {
            Assert.That(new BallId(4), Is.EqualTo(new BallId(4)));
            Assert.That(new BallId(4), Is.Not.EqualTo(new BallId(5)));
        }
    }
}