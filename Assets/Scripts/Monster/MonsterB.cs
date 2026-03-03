using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Mid-range projectile monster with zigzag approach and post-fire retreat.
    ///
    /// Behaviour loop:
    ///   1. Chase (zigzag) — weaves left/right toward target using a sine wave offset.
    ///   2. Attack          — fires one projectile via ProjectilePool (inherited FireProjectile).
    ///   3. Retreat         — immediately backs away until _retreatDistance is reached.
    ///   4. Repeat from 1.
    ///
    /// Overrides: Awake, EnterChase, UpdateChase, PerformAttack.
    /// Idle, Patrol, Dead, TakeDamage, OnTargetAssigned all inherited from MonsterBase.
    ///
    /// Inspector setup:
    ///   _data → MonsterB ScriptableObject asset
    ///     moveSpeed     : 7
    ///     attackRange   : 4   (must be > _zigzagAmplitude to guarantee approach succeeds)
    ///     detectionRange: 10
    ///     attackCooldown: 1.5
    /// </summary>
    public class MonsterB : MonsterBase
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Zigzag")]
        [Tooltip("Peak lateral offset while approaching. Keep below attackRange.")]
        [SerializeField] private float _zigzagAmplitude = 3f;

        [Tooltip("Full oscillation cycles per second.")]
        [SerializeField] private float _zigzagFrequency = 2f;

        [Header("Retreat")]
        [Tooltip("After firing, backs away until this far from the target.")]
        [SerializeField] private float _retreatDistance = 6f;

        // ── Private runtime ──────────────────────────────────────────────────

        private bool  _retreating;
        private float _zigzagTimer;

        /// <summary>
        /// Local cache of AttackRange² — mirrors MonsterBase._sqrAttackRange which is private.
        /// </summary>
        private float _mySqrAttackRange;
        private float _sqrRetreatDistance;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();   // initialises _currentHP, logs warning if _data null

            if (_data == null) return;

            _mySqrAttackRange   = _data.AttackRange  * _data.AttackRange;
            _sqrRetreatDistance = _retreatDistance    * _retreatDistance;
        }

        // ── State Overrides ──────────────────────────────────────────────────

        protected override void EnterChase()
        {
            base.EnterChase();      // sets _state = MonsterState.Chase
            _zigzagTimer = 0f;      // restart wave from zero for a clean approach
        }

        /// <summary>
        /// Full override of the Chase update tick.
        ///
        /// Retreat branch  — active immediately after a shot lands.
        ///   Moves directly away from target until _sqrRetreatDistance is reached,
        ///   then clears the flag and falls through to the approach branch.
        ///
        /// Approach branch — zigzag toward target until within attack range.
        ///   Sine-wave offset is applied perpendicular to the approach vector,
        ///   causing the monster to weave left/right as it closes distance.
        /// </summary>
        protected override void UpdateChase()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                EnterIdle();
                return;
            }

            Vector3 toTarget = _currentTarget.Transform.position - transform.position;
            toTarget.y = 0f;

            float sqrDist = toTarget.sqrMagnitude;

            // ── Retreat branch ───────────────────────────────────────────────
            if (_retreating)
            {
                if (sqrDist >= _sqrRetreatDistance)
                {
                    // Far enough — clear flag and fall through to approach
                    _retreating = false;
                }
                else
                {
                    // Still too close — keep backing away
                    // retreatPoint is always well behind the current position so
                    // MoveToward never stalls due to arrival.
                    Vector3 retreatPoint = transform.position
                                        - toTarget.normalized * (_retreatDistance * 2f);
                    MoveToward(retreatPoint, _data.MoveSpeed);
                    return;
                }
            }

            // ── Approach branch (zigzag) ─────────────────────────────────────
            if (sqrDist <= _mySqrAttackRange)
            {
                EnterAttack();
                return;
            }

            _zigzagTimer += Time.deltaTime;

            // Build a perpendicular (right) vector relative to the approach direction.
            // Cross(up, forward) gives the horizontal right-hand side of the approach.
            Vector3 forward  = toTarget.normalized;
            Vector3 right    = Vector3.Cross(Vector3.up, forward).normalized;

            // Sine wave oscillates the destination laterally around the target position.
            // Using target.pos as the oscillation anchor keeps the weave centred on the target
            // rather than the monster's current side, so approach remains net-convergent.
            float   side      = Mathf.Sin(_zigzagTimer * _zigzagFrequency * Mathf.PI * 2f)
                              * _zigzagAmplitude;
            Vector3 zigTarget = _currentTarget.Transform.position + right * side;

            MoveToward(zigTarget, _data.MoveSpeed);
        }

        /// <summary>
        /// Fires one projectile then immediately begins retreat.
        /// Called by MonsterBase.UpdateAttack() whenever the attack cooldown expires.
        /// EnterChase() is called here to transition state; UpdateAttack() will still
        /// assign _attackTimer = AttackCooldown after this returns, which is harmless
        /// because EnterAttack() resets it to 0 on the next entry anyway.
        /// </summary>
        protected override void PerformAttack()
        {
            FireProjectile();

            _retreating = true;
            EnterChase();   // returns to Chase state; UpdateChase will see _retreating = true
        }
    }
}
