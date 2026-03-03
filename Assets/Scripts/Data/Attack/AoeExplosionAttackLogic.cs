using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Detonates a ground explosion centred on the target's position,
    /// dealing damage to all Ally combatants within the radius.
    /// Exits to Chase if the target moves out of attack range.
    /// Used by: MonsterC.
    /// </summary>
    [CreateAssetMenu(fileName = "AoeExplosionAttack", menuName = "WildTamer/Attack/AoeExplosion")]
    public class AoeExplosionAttackLogic : AttackLogic
    {
        [Header("AoE Explosion")]
        [Tooltip("Radius of the ground explosion centred on the target's position.")]
        [SerializeField] private float _explosionRadius = 3f;

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
                owner.DetonateAoe(
                    target.Transform.position,
                    _explosionRadius,
                    owner.Data.AttackDamage,
                    CombatTeam.Ally
                );
                _attackTimer = owner.Data.AttackCooldown;
            }

            return AttackTickResult.Continue;
        }
    }
}
