using System;
using PoolTable.Core.Shots;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Pockets
{
    public static class PocketCaptureLayout
    {
        public static Vector3 GetCenter(PocketId pocket)
        {
            if (!pocket.IsValid)
            {
                throw new ArgumentException("Pocket layout requires a valid table pocket.", nameof(pocket));
            }

            var halfLength = BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters * 0.5f;
            var halfWidth = BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f;
            var height = BilliardsPhysicalSpecification.PocketCaptureCenterHeightMeters;

            return pocket.Index switch
            {
                1 => new Vector3(-halfLength, height, -halfWidth),
                2 => new Vector3(0f, height, -halfWidth),
                3 => new Vector3(halfLength, height, -halfWidth),
                4 => new Vector3(halfLength, height, halfWidth),
                5 => new Vector3(0f, height, halfWidth),
                6 => new Vector3(-halfLength, height, halfWidth),
                _ => throw new ArgumentOutOfRangeException(nameof(pocket)),
            };
        }
    }
}
