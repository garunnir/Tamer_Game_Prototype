using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Straight-line chase that doubles move speed below an HP threshold (enrage).
    /// Returns EnterAttack when within attack range.
    /// Used by: MonsterC.
    /// </summary>
    [CreateAssetMenu(fileName = "EnrageChaseMove", menuName = "WildTamer/Movement/EnrageChase")]
    public class EnrageChaseMoveLogic : MovementLogic
    {
        [Header("Enrage")]
        [Tooltip("HP fraction (0–1) below which the speed multiplier activates.")]
        [SerializeField] private float _enrageHpThreshold = 0.3f;

        [Tooltip("Speed multiplier applied when HP drops below the threshold.")]
        [SerializeField] private float _enrageSpeedMultiplier = 2f;

        // ── Runtime ──────────────────────────────────────────────────────────

        private float _sqrAttackRange;
        private float _maxHp;

        public override void Initialize(MonsterUnit owner)
        {
            _sqrAttackRange = owner.Data.AttackRange * owner.Data.AttackRange;
            _maxHp          = owner.Data.MaxHP;
        }

        public override MoveTickResult Tick(MonsterUnit owner, ICombatant target)
        {
            Vector3 toTarget = target.Transform.position - owner.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= _sqrAttackRange)
                return MoveTickResult.EnterAttack;

            bool  enraged       = (owner.CurrentHP / _maxHp) < _enrageHpThreshold;
            float effectiveSpeed = enraged
                ? owner.Data.MoveSpeed * _enrageSpeedMultiplier
                : owner.Data.MoveSpeed;

            owner.MoveToward(target.Transform.position, effectiveSpeed);
            return MoveTickResult.Continue;
        }
    }
}
