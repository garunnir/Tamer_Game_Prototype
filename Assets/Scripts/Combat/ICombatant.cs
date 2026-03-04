namespace WildTamer
{
    public enum CombatTeam { Ally, Enemy }

    /// <summary>
    /// Contract for objects that participate in auto-combat.
    /// Extends IDamageable and ITargetable; adds CombatSystem-specific members.
    /// </summary>
    public interface ICombatant : IDamageable, ITargetable
    {
        CombatTeam Team           { get; }
        float      DetectionRange { get; }

        /// <summary>
        /// Called by CombatSystem every 0.2 s with the nearest valid target in range,
        /// or null if no target qualifies. Implementors switch state accordingly.
        /// </summary>
        void OnTargetAssigned(ICombatant target);
    }
}
