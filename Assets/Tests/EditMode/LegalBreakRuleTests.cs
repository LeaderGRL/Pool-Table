using System;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;

namespace PoolTable.Tests.EditMode
{
    public sealed class LegalBreakRuleTests
    {
        [Test]
        public void Evaluate_ObjectBallPocketed_MakesBreakLegal()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: new[] { 3 },
                railContactBallNumbers: Array.Empty<int>());

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.True);
            Assert.That(result.Reason, Is.EqualTo(BreakEvaluationReason.ObjectBallPocketed));
            Assert.That(result.HasPocketedObjectBall, Is.True);
            Assert.That(result.ObjectBallRailContactCount, Is.Zero);
        }

        [Test]
        public void Evaluate_EightBallPocketed_MakesBreakLegal()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: new[] { BallId.EightBallNumber },
                railContactBallNumbers: Array.Empty<int>());

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.True);
            Assert.That(result.Reason, Is.EqualTo(BreakEvaluationReason.ObjectBallPocketed));
            Assert.That(result.HasPocketedObjectBall, Is.True);
        }

        [Test]
        public void Evaluate_FourDistinctObjectBallsReachRails_MakesBreakLegal()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: Array.Empty<int>(),
                railContactBallNumbers: new[] { 1, 2, 3, 4 });

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.True);
            Assert.That(result.Reason, Is.EqualTo(BreakEvaluationReason.FourOrMoreObjectBallsReachedRails));
            Assert.That(result.HasPocketedObjectBall, Is.False);
            Assert.That(result.ObjectBallRailContactCount, Is.EqualTo(4));
        }

        [Test]
        public void Evaluate_DuplicateRailObservations_CountEachObjectBallOnce()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: Array.Empty<int>(),
                railContactBallNumbers: new[] { 1, 2, 2, 3, 4 });

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.True);
            Assert.That(result.ObjectBallRailContactCount, Is.EqualTo(4));
        }

        [Test]
        public void Evaluate_CueBallRailContact_DoesNotCountTowardFourObjectBalls()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: Array.Empty<int>(),
                railContactBallNumbers: new[] { BallId.CueBallNumber, 1, 2, 3 });

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(BreakEvaluationReason.InsufficientObjectBallsReachedRails));
            Assert.That(result.ObjectBallRailContactCount, Is.EqualTo(3));
        }

        [Test]
        public void Evaluate_CueBallPocketedAlone_DoesNotSatisfyObjectBallPocketRequirement()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: new[] { BallId.CueBallNumber },
                railContactBallNumbers: new[] { 1, 2, 3 });

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.HasPocketedObjectBall, Is.False);
            Assert.That(result.ObjectBallRailContactCount, Is.EqualTo(3));
        }

        [Test]
        public void Evaluate_FewerThanFourObjectBallsReachRailsWithoutPocket_IsIllegal()
        {
            var facts = CreateFacts(
                firstObjectBallContact: 1,
                pocketedBallNumbers: Array.Empty<int>(),
                railContactBallNumbers: new[] { 1, 2, 3 });

            var result = LegalBreakRule.Evaluate(facts);

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(BreakEvaluationReason.InsufficientObjectBallsReachedRails));
            Assert.That(result.ObjectBallRailContactCount, Is.EqualTo(3));
        }

        [Test]
        public void Evaluate_NullFacts_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LegalBreakRule.Evaluate(null));
        }

        private static ShotFacts CreateFacts(
            int firstObjectBallContact,
            int[] pocketedBallNumbers,
            int[] railContactBallNumbers)
        {
            return new ShotFacts(
                new BallId(firstObjectBallContact),
                CreateBallIds(pocketedBallNumbers),
                CreateBallIds(railContactBallNumbers));
        }

        private static BallId[] CreateBallIds(int[] numbers)
        {
            var result = new BallId[numbers.Length];

            for (var index = 0; index < numbers.Length; index++)
            {
                result[index] = new BallId(numbers[index]);
            }

            return result;
        }
    }
}
