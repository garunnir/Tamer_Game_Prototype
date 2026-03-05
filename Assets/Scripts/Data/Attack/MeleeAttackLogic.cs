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
        [Header("VFX")]
        [Tooltip("Hit effect played at the target's position on each melee strike.")]
        [SerializeField] private ParticleSystem _hitVfxPrefab;

        // ── Runtime ──────────────────────────────────────────────────────────

        private float _attackTimer;
        private bool _committed; // true while cooldown is counting down after a strike

        public override void OnEnterAttackState(MonsterUnit owner)
        {
            _attackTimer = 0f; // fire immediately on entering attack range
            _committed = false;
        }

        public override AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange)
        {
            // Only allow Chase transition when not mid-attack cooldown
            if (!inAttackRange && !_committed)
                return AttackTickResult.EnterChase;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                // Always land the hit — committed attack completes regardless of current range
                owner.DealMeleeDamage(target, owner.Data.AttackDamage);
                EffectManager.Instance?.PlayVfxAt(_hitVfxPrefab, target.Transform.position);

                _committed = false;

                if (!inAttackRange)
                    return AttackTickResult.EnterChase;

                _attackTimer = owner.Data.AttackCooldown;
                _committed = true;
            }

            return AttackTickResult.Continue;
        }
    }
}
