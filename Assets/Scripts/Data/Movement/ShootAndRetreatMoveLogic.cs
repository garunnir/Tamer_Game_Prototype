using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Approaches the target in a straight line until attack range, then retreats
    /// backward for the duration of the attack cooldown after each shot.
    ///
    /// Requires ProjectileAttackLogic with _retreatAfterFiring = true, which calls
    /// NotifyAttackFired() → OnAttackFired() here to start the cooldown retreat timer.
    /// </summary>
    [CreateAssetMenu(fileName = "ShootAndRetreatMove", menuName = "WildTamer/Movement/ShootAndRetreat")]
    public class ShootAndRetreatMoveLogic : MovementLogic
    {
        [Tooltip("How far behind to aim when retreating (multiplied with retreat direction).")]
        [SerializeField] private float _retreatAimDistance = 8f;

        // ── Runtime ──────────────────────────────────────────────────────────

        private float _sqrAttackRange;
        private float _cooldownRemaining;

        public override void Initialize(MonsterUnit owner)
        {
            _sqrAttackRange    = owner.Data.AttackRange * owner.Data.AttackRange;
            _cooldownRemaining = 0f;
        }

        public override MoveTickResult Tick(MonsterUnit owner, ICombatant target)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= Time.deltaTime;

            Vector3 toTarget = target.Transform.position - owner.transform.position;
            toTarget.y = 0f;

            // ── Cooldown retreat ─────────────────────────────────────────────
            if (_cooldownRemaining > 0f)
            {
                Vector3 retreatPoint = owner.transform.position
                                     - toTarget.normalized * _retreatAimDistance;
                owner.MoveToward(retreatPoint, owner.Data.MoveSpeed);
                return MoveTickResult.Continue;
            }

            // ── Straight-line approach ───────────────────────────────────────
            if (toTarget.sqrMagnitude <= _sqrAttackRange)
                return MoveTickResult.EnterAttack;

            owner.MoveToward(target.Transform.position, owner.Data.MoveSpeed);
            return MoveTickResult.Continue;
        }

        /// <summary>Called via MonsterUnit.NotifyAttackFired() from ProjectileAttackLogic.</summary>
        public override void OnAttackFired(MonsterUnit owner)
        {
            _cooldownRemaining = owner.Data.AttackCooldown;
        }
    }
}
