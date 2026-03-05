namespace WildTamer
{
    /// <summary>
    /// Contract for objects that have a faction (read-only).
    /// Use ITargetable for targeting context, ITeamable when faction can be changed.
    /// </summary>
    public interface IHasFaction
    {
        FactionId Faction { get; }
    }
}
