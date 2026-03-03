using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// BossA — AoeSlam pattern boss.
    ///
    /// Attack behaviour (both run independently in Attack state):
    ///   Melee  — direct damage to target every _data.AttackCooldown seconds (default 2s).
    ///   AoeSlam — every _slamInterval seconds, places _slamZoneCount ground-slam warning
    ///             zones sequentially at the player's position. Each zone shows a red circle
    ///             for _warningDuration seconds before detonating for _slamDamage damage.
    ///
    /// Slam zones are pre-allocated in Awake() as a small pool (size = _slamZoneCount).
    /// AoeSlamZone deactivates itself on detonation; GetAvailableZone() finds it by
    /// checking !activeInHierarchy — no pool class required at this scale.
    ///
    /// Overrides: Awake, EnterAttack, UpdateAttack, PerformAttack.
    /// Patrol, Idle, Chase, Dead, TakeDamage, OnTargetAssigned all inherited from MonsterBase.
    ///
    /// Inspector setup:
    ///   _data → BossA ScriptableObject asset
    ///     attackCooldown  : 2    (melee cooldown)
    ///     attackRange     : 2    (melee range)
    ///     moveSpeed       : 3
    ///     maxHP           : 300
    ///     detectionRange  : 15
    ///   BossA-specific SerializeFields have sensible defaults (see below).
    /// </summary>
    public class BossA : MonsterBase
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("AoeSlam Pattern")]
        [Tooltip("Seconds between AoeSlam activations. Counts only while in Attack state.")]
        [SerializeField] private float _slamInterval = 8f;

        [Tooltip("Number of ground-slam zones placed per slam activation.")]
        [SerializeField] private int _slamZoneCount = 3;

        [Tooltip("Damage dealt by each zone on detonation.")]
        [SerializeField] private float _slamDamage = 25f;

        [Tooltip("Radius of each explosion zone in world units.")]
        [SerializeField] private float _slamRadius = 3f;

        [Tooltip("Seconds between sequential zone placements during one slam.")]
        [SerializeField] private float _zoneSpawnInterval = 0.5f;

        [Tooltip("Seconds the red warning circle is visible before the zone detonates.")]
        [SerializeField] private float _warningDuration = 1.5f;

        // ── Private runtime ──────────────────────────────────────────────────

        /// <summary>Local cache of AttackRange² — base._sqrAttackRange is private.</summary>
        private float _mySqrAttackRange;

        // Melee timer (mirrors base._attackTimer which is private).
        private float _meleeTimer;

        // Slam sequence state.
        private float        _slamTimer;
        private bool         _isSlamming;
        private int          _slamPhase;
        private float        _nextZoneTimer;

        // Pre-allocated zone pool.
        private AoeSlamZone[] _zonePool;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake(); // initialises _currentHP; logs warning if _data null

            if (_data == null) return;

            _mySqrAttackRange = _data.AttackRange * _data.AttackRange;

            // Initialise slam timer so first slam fires after _slamInterval seconds
            // of Attack state rather than immediately on first engagement.
            _slamTimer = _slamInterval;

            // Pre-allocate the zone pool. Pool size matches the slam count so that
            // all zones for one slam can be active simultaneously without contention.
            _zonePool = new AoeSlamZone[_slamZoneCount];
            for (int i = 0; i < _slamZoneCount; i++)
            {
                var go = new GameObject($"BossA_SlamZone_{i}");
                go.transform.SetParent(transform.parent); // sibling, not child (boss moves)
                _zonePool[i] = go.AddComponent<AoeSlamZone>();
                // AoeSlamZone.Awake() runs immediately via AddComponent and calls
                // gameObject.SetActive(false) at the end of Awake — pool starts inactive.
            }
        }

        // ── State Overrides ──────────────────────────────────────────────────

        protected override void EnterAttack()
        {
            base.EnterAttack(); // sets _state = Attack; resets base private _attackTimer (unused)
            _meleeTimer = 0f;   // fire melee immediately on entering attack range
            // _slamTimer intentionally NOT reset here — it carries across state changes
        }

        /// <summary>
        /// Runs two independent timers while in Attack state:
        ///   • _meleeTimer  → PerformAttack() (direct melee hit)
        ///   • _slamTimer   → starts a slam sequence (3 staggered AoeSlamZones)
        ///
        /// Fully overrides base.UpdateAttack() because base._attackTimer is private.
        /// The target-alive and range checks are re-implemented identically to base.
        /// </summary>
        protected override void UpdateAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                EnterIdle();
                return;
            }

            float sqrDist = (_currentTarget.Transform.position - transform.position).sqrMagnitude;
            if (sqrDist > _mySqrAttackRange)
            {
                EnterChase();
                return;
            }

            // ── Melee timer ──────────────────────────────────────────────────
            _meleeTimer -= Time.deltaTime;
            if (_meleeTimer <= 0f)
            {
                PerformAttack();
                _meleeTimer = _data.AttackCooldown;
            }

            // ── Slam timer ───────────────────────────────────────────────────
            if (!_isSlamming)
            {
                _slamTimer -= Time.deltaTime;
                if (_slamTimer <= 0f)
                {
                    _slamTimer     = _slamInterval;
                    _isSlamming    = true;
                    _slamPhase     = 0;
                    _nextZoneTimer = 0f; // first zone spawns this frame
                }
            }

            if (_isSlamming)
                UpdateSlamSequence();
        }

        /// <summary>
        /// Direct melee hit. Called by the melee timer branch of UpdateAttack().
        /// Mirrors MonsterA.PerformAttack() — no projectile required.
        /// </summary>
        protected override void PerformAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;

            _currentTarget.TakeDamage(_data.AttackDamage);
            EffectManager.Instance?.TriggerHitstop(this);
        }

        // ── Slam Sequence ────────────────────────────────────────────────────

        /// <summary>
        /// Advances the zone-spawn sequence. Each tick past _nextZoneTimer ≤ 0:
        ///   • Places a zone at the current target position.
        ///   • Increments _slamPhase.
        ///   • Resets _nextZoneTimer for the next zone (or ends the sequence).
        /// </summary>
        private void UpdateSlamSequence()
        {
            _nextZoneTimer -= Time.deltaTime;
            if (_nextZoneTimer > 0f) return;

            if (_slamPhase < _slamZoneCount)
            {
                SpawnSlamZone();
                _slamPhase++;
            }

            if (_slamPhase >= _slamZoneCount)
            {
                // All zones spawned — sequence complete.
                _isSlamming = false;
            }
            else
            {
                // Queue next zone after the spawn interval.
                _nextZoneTimer = _zoneSpawnInterval;
            }
        }

        /// <summary>
        /// Retrieves an available zone from the pool and initialises it at the
        /// target's current world position.
        /// Position is snapshotted at spawn time — the warning circle shows where
        /// the player was when the zone appeared, incentivising the player to dodge.
        /// </summary>
        private void SpawnSlamZone()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;

            AoeSlamZone zone = GetAvailableZone();
            if (zone == null)
            {
                Debug.LogWarning("[BossA] No available AoeSlamZone in pool — skipping zone.");
                return;
            }

            zone.Init(
                _currentTarget.Transform.position,
                _slamRadius,
                _slamDamage,
                _warningDuration
            );
        }

        /// <summary>
        /// Returns the first inactive zone from the pre-allocated pool, or null
        /// if all zones are currently active (should not happen under normal play).
        /// </summary>
        private AoeSlamZone GetAvailableZone()
        {
            if (_zonePool == null) return null;

            for (int i = 0; i < _zonePool.Length; i++)
            {
                if (!_zonePool[i].gameObject.activeInHierarchy)
                    return _zonePool[i];
            }
            return null;
        }
    }
}
