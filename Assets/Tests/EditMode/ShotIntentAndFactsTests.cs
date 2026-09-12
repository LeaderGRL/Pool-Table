using System;
using System.Collections.Generic;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class ShotIntentAndFactsTests
    {
        [Test]
        public void ShotDirection_NormalizesPlanarDirection()
        {
            var direction = new ShotDirection(3f, 4f);

            Assert.That(direction.X, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(direction.Y, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void ShotDirection_RejectsZeroDirection()
        {
            Assert.Throws<ArgumentException>(() => new ShotDirection(0f, 0f));
        }

        [TestCase(float.NaN, 1f)]
        [TestCase(1f, float.PositiveInfinity)]
        public void ShotDirection_RejectsNonFiniteComponents(float x, float y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShotDirection(x, y));
        }

        [Test]
        public void ShotIntent_CapturesPlayerDirectionAndNormalizedPower()
        {
            var direction = new ShotDirection(1f, 1f);
            var intent = new ShotIntent(MatchPlayerId.PlayerTwo, direction, 0.75f);

            Assert.That(intent.Player, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(intent.Direction, Is.EqualTo(direction));
            Assert.That(intent.NormalizedPower, Is.EqualTo(0.75f));
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void ShotIntent_RejectsInvalidPower(float power)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShotIntent(MatchPlayerId.PlayerOne, new ShotDirection(1f, 0f), power));
        }

        [Test]
        public void ShotIntent_RejectsUnknownPlayer()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShotIntent((MatchPlayerId)99, new ShotDirection(1f, 0f), 0.5f));
        }

        [Test]
        public void ShotIntent_RejectsDefaultDirection()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotIntent(MatchPlayerId.PlayerOne, default, 0.5f));
        }

        [Test]
        public void ShotIntent_UsesValueEquality()
        {
            var left = new ShotIntent(MatchPlayerId.PlayerOne, new ShotDirection(2f, 0f), 0.5f);
            var right = new ShotIntent(MatchPlayerId.PlayerOne, new ShotDirection(1f, 0f), 0.5f);

            Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void ShotFacts_CapturesObservedOutcome()
        {
            var facts = new ShotFacts(
                new BallId(3),
                new[] { new BallId(3), new BallId(11) },
                new[] { new BallId(3), new BallId(5), new BallId(3) });

            Assert.That(facts.HasObjectBallContact, Is.True);
            Assert.That(facts.FirstObjectBallContact, Is.EqualTo(new BallId(3)));
            Assert.That(facts.PocketedBalls, Is.EqualTo(new[] { new BallId(3), new BallId(11) }));
            Assert.That(facts.RailContactBallsAfterFirstObjectBallContact, Is.EqualTo(new[] { new BallId(3), new BallId(5) }));
            Assert.That(facts.CueBallPocketed, Is.False);
        }

        [Test]
        public void ShotFacts_DerivesCueBallPocketedFromPocketedBalls()
        {
            var facts = new ShotFacts(
                null,
                new[] { new BallId(BallId.CueBallNumber) },
                Array.Empty<BallId>());

            Assert.That(facts.CueBallPocketed, Is.True);
        }

        [Test]
        public void ShotFacts_RejectsCueBallAsFirstObjectBallContact()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(new BallId(BallId.CueBallNumber), Array.Empty<BallId>(), Array.Empty<BallId>()));
        }

        [Test]
        public void ShotFacts_RejectsDuplicatePocketedBalls()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(
                    new BallId(1),
                    new[] { new BallId(4), new BallId(4) },
                    Array.Empty<BallId>()));
        }

        [Test]
        public void ShotFacts_RejectsPostContactRailsWithoutObjectBallContact()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(
                    null,
                    Array.Empty<BallId>(),
                    new[] { new BallId(4) }));
        }

        [Test]
        public void ShotFacts_DefensivelyCopiesObservedCollections()
        {
            var pocketed = new List<BallId> { new BallId(2) };
            var rails = new List<BallId> { new BallId(6) };
            var facts = new ShotFacts(new BallId(2), pocketed, rails);

            pocketed.Add(new BallId(9));
            rails.Add(new BallId(10));

            Assert.That(facts.PocketedBalls, Is.EqualTo(new[] { new BallId(2) }));
            Assert.That(facts.RailContactBallsAfterFirstObjectBallContact, Is.EqualTo(new[] { new BallId(6) }));
        }

        [Test]
        public void ShotFacts_RejectsNullCollections()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ShotFacts(new BallId(1), null, Array.Empty<BallId>()));
            Assert.Throws<ArgumentNullException>(() =>
                new ShotFacts(new BallId(1), Array.Empty<BallId>(), null));
        }
    }
}
