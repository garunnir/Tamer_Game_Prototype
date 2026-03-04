namespace WildTamer
{
    public interface ITeamable
    {
        FactionId Faction { get; }
        void SetFaction(FactionId team);
    }
}
