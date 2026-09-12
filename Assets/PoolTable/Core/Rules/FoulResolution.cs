namespace PoolTable.Core.Rules
{
    public readonly struct FoulResolution
    {
        internal FoulResolution(ShotFoul fouls)
        {
            Fouls = fouls;
        }

        public ShotFoul Fouls { get; }

        public bool HasFoul => Fouls != ShotFoul.None;

        public bool IsClean => !HasFoul;

        public bool Has(ShotFoul foul)
        {
            return foul != ShotFoul.None && (Fouls & foul) == foul;
        }
    }
}
