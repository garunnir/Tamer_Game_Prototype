namespace WildTamer
{
    // Extends ITeamable: a tameable unit can also have its faction changed.
    public interface ITameable : ITeamable
    {
        bool IsTaming { get; }
        void Tame();
    }
}
