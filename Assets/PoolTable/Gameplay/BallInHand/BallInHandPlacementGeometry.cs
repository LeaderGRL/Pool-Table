using System;
using System.Collections.Generic;
using PoolTable.Core.Match;
using PoolTable.Core.Shots;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Pockets;
using UnityEngine;

namespace PoolTable.Gameplay.BallInHand
{
    public static class BallInHandPlacementGeometry
    {
        public static float MinimumCenterX =>
            (-BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f)
            + BilliardsPhysicalSpecification.BallRadiusMeters;

        public static float MaximumCenterX =>
            (BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f)
            - BilliardsPhysicalSpecification.BallRadiusMeters;

        public static float MinimumCenterZ =>
            (-BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f)
            + BilliardsPhysicalSpecification.BallRadiusMeters;

        public static float MaximumCenterZ =>
            (BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f)
            - BilliardsPhysicalSpecification.BallRadiusMeters;

        public static bool IsLegal(
            Vector2 candidate,
            CueBallPlacementArea placementArea,
            IReadOnlyList<Vector2> occupiedBallCenters)
        {
            ValidatePlacementArea(placementArea);

            if (!float.IsFinite(candidate.x) || !float.IsFinite(candidate.y))
            {
                return false;
            }

            if (candidate.x < MinimumCenterX
                || candidate.x > MaximumCenterX
                || candidate.y < MinimumCenterZ
                || candidate.y > MaximumCenterZ)
            {
                return false;
            }

            if (placementArea == CueBallPlacementArea.AboveHeadString
                && candidate.x > BilliardsPhysicalSpecification.HeadStringX)
            {
                return false;
            }

            if (IsInsidePocketOpeningClearance(candidate))
            {
                return false;
            }

            if (occupiedBallCenters == null)
            {
                return true;
            }

            var minimumSeparationSquared = BilliardsPhysicalSpecification.BallDiameterMeters
                * BilliardsPhysicalSpecification.BallDiameterMeters;

            for (var index = 0; index < occupiedBallCenters.Count; index++)
            {
                var occupied = occupiedBallCenters[index];
                if (!float.IsFinite(occupied.x) || !float.IsFinite(occupied.y))
                {
                    continue;
                }

                if ((candidate - occupied).sqrMagnitude < minimumSeparationSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInsidePocketOpeningClearance(Vector2 candidate)
        {
            // The scene has no separate pocket-mouth radius. Reuse the capture radius as a
            // conservative planar clearance around the authoritative six pocket centers.
            var clearanceSquared = BilliardsPhysicalSpecification.PocketCaptureRadiusMeters
                * BilliardsPhysicalSpecification.PocketCaptureRadiusMeters;

            for (var index = PocketId.MinimumIndex; index <= PocketId.MaximumIndex; index++)
            {
                var pocketCenter = PocketCaptureLayout.GetCenter(new PocketId(index));
                var offset = candidate - new Vector2(pocketCenter.x, pocketCenter.z);
                if (offset.sqrMagnitude < clearanceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public static Vector2 ClampToPlacementArea(Vector2 candidate, CueBallPlacementArea placementArea)
        {
            ValidatePlacementArea(placementArea);

            var maximumX = placementArea == CueBallPlacementArea.AboveHeadString
                ? Math.Min(MaximumCenterX, BilliardsPhysicalSpecification.HeadStringX)
                : MaximumCenterX;

            return new Vector2(
                Mathf.Clamp(candidate.x, MinimumCenterX, maximumX),
                Mathf.Clamp(candidate.y, MinimumCenterZ, MaximumCenterZ));
        }

        private static void ValidatePlacementArea(CueBallPlacementArea placementArea)
        {
            if (placementArea != CueBallPlacementArea.Anywhere
                && placementArea != CueBallPlacementArea.AboveHeadString)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementArea),
                    placementArea,
                    "Ball-in-hand placement requires an active placement area.");
            }
        }
    }
}
