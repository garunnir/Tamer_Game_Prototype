using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Moves straight toward the target at full speed.
    /// Returns EnterAttack when within attack range.
    /// Used by: MonsterA, BossA, BossB.
    /// </summary>
    [CreateAssetMenu(fileName = "DirectChaseMove", menuName = "WildTamer/Movement/DirectChase")]
    public class DirectChaseMoveLogic : MovementLogic
    {
        private float _sqrAttackRange;

        public override void Initialize(MonsterUnit owner)
        {
            _sqrAttackRange = owner.Data.AttackRange * owner.Data.AttackRange;
        }

        public override MoveTickResult Tick(MonsterUnit owner, ICombatant target)
        {
            Vector3 toTarget = target.Transform.position - owner.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= _sqrAttackRange)
                return MoveTickResult.EnterAttack;

            owner.MoveToward(target.Transform.position, owner.Data.MoveSpeed);
            return MoveTickResult.Continue;
        }
    }
}
