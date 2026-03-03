using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Deals direct contact damage each time the attack cooldown expires.
    /// Exits to Chase if the target moves out of attack range.
    /// Used by: MonsterA.
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeAttack", menuName = "WildTamer/Attack/Melee")]
    public class MeleeAttackLogic : AttackLogic
    {
        // ── Runtime ──────────────────────────────────────────────────────────

        private float _attackTimer;

        public override void OnEnterAttackState(MonsterUnit owner)
        {
            _attackTimer = 0f; // fire immediately on entering attack range
        }

        public override AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange)
        {
            if (!inAttackRange)
                return AttackTickResult.EnterChase;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                owner.DealMeleeDamage(target, owner.Data.AttackDamage);
                _attackTimer = owner.Data.AttackCooldown;
            }

            return AttackTickResult.Continue;
        }
    }
}
