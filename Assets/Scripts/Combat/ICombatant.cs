namespace WildTamer
{
    /// <summary>
    /// Contract for objects that participate in auto-combat.
    /// Extends IDamageable, ITargetable, and ITeamable; adds CombatSystem-specific members.
    /// </summary>
    public interface ICombatant : IDamageable, ITargetable, ITeamable
    {
        float DetectionRange { get; }

        /// <summary>
        /// Called by CombatSystem every 0.2 s with the nearest valid target in range,
        /// or null if no target qualifies. Implementors switch state accordingly.
        /// </summary>
        void OnTargetAssigned(ICombatant target);
    }
}
