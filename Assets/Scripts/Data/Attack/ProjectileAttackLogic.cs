using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Fires a pooled projectile each time the attack cooldown expires.
    ///
    /// _ignoreRangeCheck  (default true) — fires regardless of distance.
    ///   true : for ranged units (tamed flock) that should always fire.
    ///   false: exits to Chase when out of attack range.
    ///
    /// _retreatAfterFiring (default false) — immediately exits to Chase after firing
    ///   and notifies MovementLogic (sets ZigzagRetreat's retreat flag).
    ///   Set to true for MonsterB's hit-and-run pattern.
    ///
    /// Used by: MonsterB (ignoreRange=false, retreat=true),
    ///          tamed flock units (ignoreRange=true, retreat=false).
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileAttack", menuName = "WildTamer/Attack/Projectile")]
    public class ProjectileAttackLogic : AttackLogic
    {
        [Tooltip("Fire regardless of distance. Set false to exit Chase when out of range.")]
        [SerializeField] private bool _ignoreRangeCheck = true;

        [Tooltip("Immediately return to Chase (with retreat) after each shot.")]
        [SerializeField] private bool _retreatAfterFiring = false;

        // ── Runtime ──────────────────────────────────────────────────────────

        private float _attackTimer;

        public override void OnEnterAttackState(MonsterUnit owner)
        {
            _attackTimer = 0f; // fire immediately on entering attack state
        }

        public override AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange)
        {
            if (!_ignoreRangeCheck && !inAttackRange)
                return AttackTickResult.EnterChase;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f)
                return AttackTickResult.Continue;

            owner.FireProjectileAt(target);
            _attackTimer = owner.Data.AttackCooldown;

            if (_retreatAfterFiring)
            {
                owner.NotifyAttackFired(); // triggers ZigzagRetreatMoveLogic._retreating = true
                return AttackTickResult.EnterChase;
            }

            return AttackTickResult.Continue;
        }
    }
}
