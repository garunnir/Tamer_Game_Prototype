using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Approaches with a sine-wave zigzag, then retreats after firing.
    /// OnAttackFired() sets the retreat flag; the next Tick() backs away
    /// until _retreatDistance is reached, then resumes the zigzag approach.
    /// Used by: MonsterB.
    /// </summary>
    [CreateAssetMenu(fileName = "ZigzagRetreatMove", menuName = "WildTamer/Movement/ZigzagRetreat")]
    public class ZigzagRetreatMoveLogic : MovementLogic
    {
        [Header("Zigzag")]
        [Tooltip("Peak lateral offset while approaching. Keep below AttackRange.")]
        [SerializeField] private float _zigzagAmplitude = 3f;

        [Tooltip("Full oscillation cycles per second.")]
        [SerializeField] private float _zigzagFrequency = 2f;

        [Header("Retreat")]
        [Tooltip("After firing, backs away until this far from the target.")]
        [SerializeField] private float _retreatDistance = 6f;

        // ── Runtime (safe after Instantiate per unit) ────────────────────────

        private float _sqrAttackRange;
        private float _sqrRetreatDistance;
        private float _zigzagTimer;
        private bool  _retreating;

        public override void Initialize(MonsterUnit owner)
        {
            _sqrAttackRange     = owner.Data.AttackRange * owner.Data.AttackRange;
            _sqrRetreatDistance = _retreatDistance * _retreatDistance;
            _zigzagTimer        = 0f;
            _retreating         = false;
        }

        public override MoveTickResult Tick(MonsterUnit owner, ICombatant target)
        {
            Vector3 toTarget = target.Transform.position - owner.transform.position;
            toTarget.y = 0f;
            float sqrDist = toTarget.sqrMagnitude;

            // ── Retreat branch ───────────────────────────────────────────────
            if (_retreating)
            {
                if (sqrDist >= _sqrRetreatDistance)
                {
                    _retreating = false;
                }
                else
                {
                    // Back away: move toward a point well behind current position.
                    Vector3 retreatPoint = owner.transform.position
                                        - toTarget.normalized * (_retreatDistance * 2f);
                    owner.MoveToward(retreatPoint, owner.Data.MoveSpeed);
                    return MoveTickResult.Continue;
                }
            }

            // ── Approach branch (zigzag) ─────────────────────────────────────
            if (sqrDist <= _sqrAttackRange)
                return MoveTickResult.EnterAttack;

            _zigzagTimer += Time.deltaTime;

            // Perpendicular right vector relative to the approach direction.
            Vector3 forward = toTarget.normalized;
            Vector3 right   = Vector3.Cross(Vector3.up, forward).normalized;

            // Sine wave centered on target position so approach remains net-convergent.
            float   side      = Mathf.Sin(_zigzagTimer * _zigzagFrequency * Mathf.PI * 2f)
                              * _zigzagAmplitude;
            Vector3 zigTarget = target.Transform.position + right * side;

            owner.MoveToward(zigTarget, owner.Data.MoveSpeed);
            return MoveTickResult.Continue;
        }

        /// <summary>Called by ProjectileAttackLogic via MonsterUnit.NotifyAttackFired().</summary>
        public override void OnAttackFired(MonsterUnit owner)
        {
            _retreating = true;
        }
    }
}
