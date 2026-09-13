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
            Assert.That(intent.HasCalledShot, Is.False);
            Assert.That(intent.CalledShot, Is.Null);
        }

        [Test]
        public void ShotIntent_CapturesOptionalCalledShot()
        {
            var calledShot = new CalledShot(new BallId(8), new PocketId(4));

            var intent = new ShotIntent(
                MatchPlayerId.PlayerOne,
                new ShotDirection(1f, 0f),
                0.6f,
                calledShot);

            Assert.That(intent.HasCalledShot, Is.True);
            Assert.That(intent.CalledShot, Is.EqualTo(calledShot));
        }

        [Test]
        public void ShotIntent_RejectsDefaultCalledShot()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotIntent(
                    MatchPlayerId.PlayerOne,
                    new ShotDirection(1f, 0f),
                    0.5f,
                    default(CalledShot)));
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
            var call = new CalledShot(new BallId(3), new PocketId(2));
            var left = new ShotIntent(MatchPlayerId.PlayerOne, new ShotDirection(2f, 0f), 0.5f, call);
            var right = new ShotIntent(MatchPlayerId.PlayerOne, new ShotDirection(1f, 0f), 0.5f, call);

            Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void ShotIntent_DifferentCalledShotsAreNotEqual()
        {
            var direction = new ShotDirection(1f, 0f);
            var left = new ShotIntent(
                MatchPlayerId.PlayerOne,
                direction,
                0.5f,
                new CalledShot(new BallId(3), new PocketId(2)));
            var right = new ShotIntent(
                MatchPlayerId.PlayerOne,
                direction,
                0.5f,
                new CalledShot(new BallId(3), new PocketId(5)));

            Assert.That(left, Is.Not.EqualTo(right));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void PocketId_RejectsUnknownPocket(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PocketId(index));
        }

        [Test]
        public void CalledShot_RejectsCueBall()
        {
            Assert.Throws<ArgumentException>(() =>
                new CalledShot(new BallId(BallId.CueBallNumber), new PocketId(1)));
        }

        [Test]
        public void CalledShot_RejectsDefaultPocket()
        {
            Assert.Throws<ArgumentException>(() =>
                new CalledShot(new BallId(8), default));
        }

        [Test]
        public void ShotFacts_CapturesObservedOutcome()
        {
            var facts = new ShotFacts(
                new BallId(3),
                new[]
                {
                    new PocketedBall(new BallId(3), new PocketId(1)),
                    new PocketedBall(new BallId(11), new PocketId(6)),
                },
                new[] { new BallId(3), new BallId(5), new BallId(3) });

            Assert.That(facts.HasObjectBallContact, Is.True);
            Assert.That(facts.FirstObjectBallContact, Is.EqualTo(new BallId(3)));
            Assert.That(facts.PocketedBalls, Is.EqualTo(new[] { new BallId(3), new BallId(11) }));
            Assert.That(facts.PocketedBallEvents, Has.Count.EqualTo(2));
            Assert.That(facts.WasPocketedIn(new BallId(3), new PocketId(1)), Is.True);
            Assert.That(facts.WasPocketedIn(new BallId(3), new PocketId(2)), Is.False);
            Assert.That(facts.RailContactBallsAfterFirstObjectBallContact, Is.EqualTo(new[] { new BallId(3), new BallId(5) }));
            Assert.That(facts.CueBallPocketed, Is.False);
        }

        [Test]
        public void ShotFacts_DerivesCueBallPocketedFromPocketedBalls()
        {
            var facts = new ShotFacts(
                null,
                new[] { new PocketedBall(new BallId(BallId.CueBallNumber), new PocketId(3)) },
                Array.Empty<BallId>());

            Assert.That(facts.CueBallPocketed, Is.True);
        }

        [Test]
        public void ShotFacts_RejectsCueBallAsFirstObjectBallContact()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(new BallId(BallId.CueBallNumber), Array.Empty<PocketedBall>(), Array.Empty<BallId>()));
        }

        [Test]
        public void ShotFacts_RejectsDuplicatePocketedBalls()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(
                    new BallId(1),
                    new[]
                    {
                        new PocketedBall(new BallId(4), new PocketId(1)),
                        new PocketedBall(new BallId(4), new PocketId(2)),
                    },
                    Array.Empty<BallId>()));
        }

        [Test]
        public void ShotFacts_RejectsDefaultPocketedBallObservation()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(
                    new BallId(1),
                    new[] { default(PocketedBall) },
                    Array.Empty<BallId>()));
        }

        [Test]
        public void ShotFacts_RejectsPostContactRailsWithoutObjectBallContact()
        {
            Assert.Throws<ArgumentException>(() =>
                new ShotFacts(
                    null,
                    Array.Empty<PocketedBall>(),
                    new[] { new BallId(4) }));
        }

        [Test]
        public void ShotFacts_DefensivelyCopiesObservedCollections()
        {
            var pocketed = new List<PocketedBall>
            {
                new PocketedBall(new BallId(2), new PocketId(1)),
            };
            var rails = new List<BallId> { new BallId(6) };
            var facts = new ShotFacts(new BallId(2), pocketed, rails);

            pocketed.Add(new PocketedBall(new BallId(9), new PocketId(2)));
            rails.Add(new BallId(10));

            Assert.That(facts.PocketedBalls, Is.EqualTo(new[] { new BallId(2) }));
            Assert.That(facts.PocketedBallEvents, Has.Count.EqualTo(1));
            Assert.That(facts.RailContactBallsAfterFirstObjectBallContact, Is.EqualTo(new[] { new BallId(6) }));
        }

        [Test]
        public void ShotFacts_RejectsNullCollections()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ShotFacts(new BallId(1), null, Array.Empty<BallId>()));
            Assert.Throws<ArgumentNullException>(() =>
                new ShotFacts(new BallId(1), Array.Empty<PocketedBall>(), null));
        }

        [Test]
        public void ShotFacts_RejectsInvalidPocketLookup()
        {
            var facts = new ShotFacts(
                new BallId(1),
                Array.Empty<PocketedBall>(),
                Array.Empty<BallId>());

            Assert.Throws<ArgumentException>(() => facts.WasPocketedIn(new BallId(1), default));
        }
    }
}
