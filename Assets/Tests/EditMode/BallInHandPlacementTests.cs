using System.Collections.Generic;
using NUnit.Framework;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.BallInHand;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Pockets;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class BallInHandPlacementTests
    {
        [Test]
        public void IsLegal_AnywhereRequiresCueBallCenterInsidePlayingSurface()
        {
            var legalLengthEdge = new Vector2(BallInHandPlacementGeometry.MaximumCenterX, 0f);
            var legalWidthEdge = new Vector2(-0.4f, BallInHandPlacementGeometry.MaximumCenterZ);
            var outsideLength = legalLengthEdge + new Vector2(0.0001f, 0f);
            var outsideWidth = legalWidthEdge + new Vector2(0f, 0.0001f);

            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    legalLengthEdge,
                    CueBallPlacementArea.Anywhere,
                    new List<Vector2>()),
                Is.True);
            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    legalWidthEdge,
                    CueBallPlacementArea.Anywhere,
                    new List<Vector2>()),
                Is.True);
            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    outsideLength,
                    CueBallPlacementArea.Anywhere,
                    new List<Vector2>()),
                Is.False);
            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    outsideWidth,
                    CueBallPlacementArea.Anywhere,
                    new List<Vector2>()),
                Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void IsLegal_RejectsCueBallCenterBesidePocketOpening(int pocketIndex)
        {
            var pocketCenter = PocketCaptureLayout.GetCenter(new PocketId(pocketIndex));
            var candidate = new Vector2(
                Mathf.Clamp(
                    pocketCenter.x,
                    BallInHandPlacementGeometry.MinimumCenterX,
                    BallInHandPlacementGeometry.MaximumCenterX),
                Mathf.Clamp(
                    pocketCenter.z,
                    BallInHandPlacementGeometry.MinimumCenterZ,
                    BallInHandPlacementGeometry.MaximumCenterZ));

            Assert.That(candidate.x, Is.InRange(
                BallInHandPlacementGeometry.MinimumCenterX,
                BallInHandPlacementGeometry.MaximumCenterX));
            Assert.That(candidate.y, Is.InRange(
                BallInHandPlacementGeometry.MinimumCenterZ,
                BallInHandPlacementGeometry.MaximumCenterZ));
            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    candidate,
                    CueBallPlacementArea.Anywhere,
                    new List<Vector2>()),
                Is.False);
        }

        [Test]
        public void IsLegal_AllowsRailAdjacentPlacementOutsidePocketClearance()
        {
            var candidate = new Vector2(
                BilliardsPhysicalSpecification.PocketCaptureRadiusMeters * 2f,
                BallInHandPlacementGeometry.MaximumCenterZ);

            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    candidate,
                    CueBallPlacementArea.Anywhere,
                    new List<Vector2>()),
                Is.True);
        }

        [Test]
        public void IsLegal_AboveHeadStringRejectsFootSideOfHeadString()
        {
            var onHeadString = new Vector2(BilliardsPhysicalSpecification.HeadStringX, 0f);
            var footSide = onHeadString + new Vector2(0.0001f, 0f);

            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    onHeadString,
                    CueBallPlacementArea.AboveHeadString,
                    new List<Vector2>()),
                Is.True);
            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    footSide,
                    CueBallPlacementArea.AboveHeadString,
                    new List<Vector2>()),
                Is.False);
        }

        [Test]
        public void IsLegal_RejectsOverlapWithActiveBallCenter()
        {
            var candidate = new Vector2(-0.4f, 0.1f);
            var occupied = new List<Vector2>
            {
                candidate + new Vector2(BilliardsPhysicalSpecification.BallRadiusMeters, 0f),
            };

            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    candidate,
                    CueBallPlacementArea.Anywhere,
                    occupied),
                Is.False);
        }

        [Test]
        public void IsLegal_AllowsTangentBallWithoutOverlap()
        {
            var candidate = new Vector2(-0.4f, 0.1f);
            var occupied = new List<Vector2>
            {
                candidate + new Vector2(BilliardsPhysicalSpecification.BallDiameterMeters, 0f),
            };

            Assert.That(
                BallInHandPlacementGeometry.IsLegal(
                    candidate,
                    CueBallPlacementArea.Anywhere,
                    occupied),
                Is.True);
        }

        [Test]
        public void ClampToPlacementArea_EnforcesRestrictedHeadSide()
        {
            var clamped = BallInHandPlacementGeometry.ClampToPlacementArea(
                new Vector2(BallInHandPlacementGeometry.MaximumCenterX, 0f),
                CueBallPlacementArea.AboveHeadString);

            Assert.That(clamped.x, Is.EqualTo(BilliardsPhysicalSpecification.HeadStringX));
        }

        [Test]
        public void Session_InvalidCandidateDoesNotConsumeBallInHand()
        {
            var granted = CreateStandardBallInHand();
            var session = new BallInHandPlacementSession(granted);
            var occupied = new List<Vector2>();
            var invalid = new Vector2(BallInHandPlacementGeometry.MaximumCenterX + 0.1f, 0f);

            var completed = session.TryComplete(invalid, occupied, out var state);

            Assert.That(completed, Is.False);
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(state, Is.SameAs(granted));
            Assert.That(state.HasBallInHand, Is.True);
        }

        [Test]
        public void Session_PocketOpeningCandidateDoesNotConsumeBallInHand()
        {
            var granted = CreateStandardBallInHand();
            var session = new BallInHandPlacementSession(granted);
            var candidate = new Vector2(0f, BallInHandPlacementGeometry.MaximumCenterZ);

            var completed = session.TryComplete(candidate, new List<Vector2>(), out var state);

            Assert.That(completed, Is.False);
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(state, Is.SameAs(granted));
            Assert.That(state.HasBallInHand, Is.True);
        }

        [Test]
        public void Session_ValidCandidateConsumesBallInHandForRecipient()
        {
            var granted = CreateStandardBallInHand();
            var session = new BallInHandPlacementSession(granted);

            var completed = session.TryComplete(Vector2.zero, new List<Vector2>(), out var state);

            Assert.That(completed, Is.True);
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(state.HasBallInHand, Is.False);
            Assert.That(state.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.OpenTable));
        }

        private static MatchState CreateStandardBallInHand()
        {
            var open = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(MatchPlayerId.PlayerOne));
            return BallInHandRule.GrantAfterStandardFoul(
                open,
                new FoulResolution(ShotFoul.CueBallScratch));
        }
    }
}
