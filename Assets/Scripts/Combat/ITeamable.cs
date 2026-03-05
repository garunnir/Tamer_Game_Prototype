namespace WildTamer
{
    /// <summary>Contract for objects that have a faction and can have it changed (e.g. taming).</summary>
    public interface ITeamable : IHasFaction
    {
        void SetFaction(FactionId team);
    }
}
