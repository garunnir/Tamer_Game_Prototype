using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Standard melee monster.
    ///
    /// Behaviour inherited from MonsterBase (no overrides needed):
    ///   • Idle  — waits _idleDuration seconds, then picks a patrol point
    ///   • Patrol — walks at half speed within _patrolRadius of spawn position
    ///   • Chase  — runs straight toward CombatSystem-assigned target at full speed
    ///   • Dead   — fires OnMonsterDied event, deactivates GameObject
    ///
    /// Only PerformAttack() is overridden: instead of launching a projectile,
    /// MonsterA calls TakeDamage() directly on the target for instant melee contact damage.
    ///
    /// Inspector setup:
    ///   • _data  → MonsterA ScriptableObject asset
    ///     - moveSpeed   : 4
    ///     - attackRange : 1.5
    ///     - detectionRange : 8 (suggested)
    ///     - attackCooldown : 1.5 (default)
    /// </summary>
    public class MonsterA : MonsterBase
    {
        /// <summary>
        /// Deals melee damage directly to the current target.
        /// Called by MonsterBase.UpdateAttack() whenever the attack cooldown expires
        /// and the target is within attackRange.
        /// </summary>
        protected override void PerformAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;

            _currentTarget.TakeDamage(_data.AttackDamage);
            EffectManager.Instance?.TriggerHitstop(this);
        }
    }
}
