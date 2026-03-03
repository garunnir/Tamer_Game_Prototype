using UnityEngine;

namespace WildTamer
{
    public enum CombatTeam { Ally, Enemy }

    /// <summary>
    /// Shared contract for any object that participates in auto-combat.
    /// Implemented by FlockUnitCombat (allies) and MonsterBase (enemies).
    /// CombatSystem uses this interface exclusively for targeting and damage dispatch.
    /// </summary>
    public interface ICombatant
    {
        CombatTeam Team           { get; }
        Transform  Transform      { get; }
        bool       IsAlive        { get; }
        float      DetectionRange { get; }

        void TakeDamage(float amount);

        /// <summary>
        /// Called by CombatSystem every 0.2 s with the nearest valid target in range,
        /// or null if no target qualifies. Implementors switch state accordingly.
        /// </summary>
        void OnTargetAssigned(ICombatant target);
    }
}
