namespace PoolTable.Physics.Configuration
{
    public static class BilliardsPhysicalSpecification
    {
        public const float UnityUnitsPerMeter = 1f;

        public const float BallDiameterMeters = 0.05715f;

        public const float BallRadiusMeters = BallDiameterMeters * 0.5f;

        public const float BallStoppedSpeedMetersPerSecond = 0.01f;

        public const float BallStoppedAngularSpeedRadiansPerSecond =
            BallStoppedSpeedMetersPerSecond / BallRadiusMeters;

        public const float NineFootPlayingSurfaceLengthMeters = 2.54f;

        public const float NineFootPlayingSurfaceWidthMeters = 1.27f;

        public const float MinimumTableBedHeightMeters = 0.74295f;

        public const float MaximumTableBedHeightMeters = 0.7874f;

        public const float ReferenceTableBedHeightMeters =
            (MinimumTableBedHeightMeters + MaximumTableBedHeightMeters) * 0.5f;

        public const float HeadStringX = -NineFootPlayingSurfaceLengthMeters * 0.25f;

        public const float FootSpotX = NineFootPlayingSurfaceLengthMeters * 0.25f;

        public const float CushionNoseHeightRatio = 0.635f;

        public const float CushionNoseHeightMeters = BallDiameterMeters * CushionNoseHeightRatio;

        public const float PocketCaptureRadiusMeters = BallDiameterMeters;

        public const float PocketCaptureDepthBelowBedMeters = 0.063f;

        public const float PocketCaptureCenterHeightMeters =
            ReferenceTableBedHeightMeters - PocketCaptureDepthBelowBedMeters;

        public const float TriangularRackRowSpacingMeters = BallDiameterMeters * 0.8660254f;

        public const float BallCenterHeightMeters = ReferenceTableBedHeightMeters + BallRadiusMeters;
    }
}
