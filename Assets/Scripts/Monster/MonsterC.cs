using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Long-range AoE artillery monster.
    ///
    /// Behaviour:
    ///   • Patrols slowly until a target is detected (inherited).
    ///   • Approaches target at _data.MoveSpeed (2 u/s by default).
    ///   • Stops at attackRange (8 u) and fires a ground explosion centred on
    ///     the target, hitting all allies within _explosionRadius.
    ///   • Below 30% HP the approach speed doubles (enrage).
    ///   • Does NOT retreat after firing — stays at stand-off range.
    ///
    /// Overrides: Awake, UpdateChase, PerformAttack.
    /// Patrol, Idle, Dead, TakeDamage, OnTargetAssigned all inherited from MonsterBase.
    ///
    /// AoE damage is dispatched through CombatSystem.DealAoeDamage(), which owns
    /// both registries and uses a snapshot to prevent list-mutation bugs.
    ///
    /// Inspector setup:
    ///   _data → MonsterC ScriptableObject asset
    ///     moveSpeed     : 2
    ///     attackRange   : 8
    ///     detectionRange: 12
    ///     attackCooldown: 3
    /// </summary>
    public class MonsterC : MonsterBase
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("AoE Explosion")]
        [Tooltip("Radius of the ground explosion centred on the target's position.")]
        [SerializeField] private float _explosionRadius = 3f;

        [Header("Enrage")]
        [Tooltip("HP fraction (0–1) below which move speed is multiplied.")]
        [SerializeField] private float _enrageHpThreshold = 0.3f;

        [Tooltip("Speed multiplier applied when HP drops below the threshold.")]
        [SerializeField] private float _enrageSpeedMultiplier = 2f;

        // ── Private runtime ──────────────────────────────────────────────────

        /// <summary>
        /// Local cache of AttackRange² — mirrors MonsterBase._sqrAttackRange which is private.
        /// Same pattern used by MonsterB.
        /// </summary>
        private float _mySqrAttackRange;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();   // initialises _currentHP; logs warning if _data null

            if (_data == null) return;

            _mySqrAttackRange = _data.AttackRange * _data.AttackRange;
        }

        // ── State Overrides ──────────────────────────────────────────────────

        /// <summary>
        /// Approaches the target at normal or enraged speed, then enters Attack
        /// once within attackRange.
        ///
        /// TakeDamage() is not virtual so the enrage threshold cannot be caught
        /// there. Instead, the HP ratio is computed each frame from _currentHP
        /// (protected in MonsterBase) and _data.MaxHP (int, cast to float).
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

            if (sqrDist <= _mySqrAttackRange)
            {
                EnterAttack();
                return;
            }

            // Enrage: speed doubles when remaining HP is below the threshold.
            // Cast MaxHP (int) to float to prevent integer division truncating to 0.
            bool  enraged       = (_currentHP / (float)_data.MaxHP) < _enrageHpThreshold;
            float effectiveSpeed = enraged
                ? _data.MoveSpeed * _enrageSpeedMultiplier
                : _data.MoveSpeed;

            MoveToward(_currentTarget.Transform.position, effectiveSpeed);
        }

        /// <summary>
        /// Detonates a ground explosion centred on the target's current position.
        /// All living allies within _explosionRadius receive _data.AttackDamage.
        /// Called by MonsterBase.UpdateAttack() when the cooldown expires.
        /// </summary>
        protected override void PerformAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;

            CombatSystem.Instance?.DealAoeDamage(
                _currentTarget.Transform.position,
                _explosionRadius,
                _data.AttackDamage,
                CombatTeam.Ally
            );
        }
    }
}
