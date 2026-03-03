using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// BossB — Charge pattern boss.
    ///
    /// Attack behaviour (both run independently in Attack state):
    ///   Melee  — direct damage every _data.AttackCooldown seconds (default 2s).
    ///   Charge — triggers below _chargeHpThreshold HP, every _chargeInterval seconds.
    ///            Three phases run entirely inside UpdateAttack() via Update-loop timers
    ///            (no coroutines):
    ///
    ///     1. Windup  (0.8 s) — boss vibrates in place; charge direction locked toward
    ///                          player's position at windup start. Player has this window
    ///                          to sidestep.
    ///     2. Charging (≤2 s / ≤10 u) — boss rockets along the locked direction at
    ///                          _chargeSpeed. Deals _chargeDamage once on contact.
    ///                          Stops when max time or max distance is reached.
    ///     3. Recovery (0.5 s) — brief pause; then charge cooldown resets.
    ///
    /// Overrides: Awake, EnterAttack, UpdateAttack, PerformAttack.
    /// Patrol, Idle, Chase, Dead, TakeDamage, OnTargetAssigned all inherited from MonsterBase.
    ///
    /// Inspector setup:
    ///   _data → BossB ScriptableObject asset
    ///     attackCooldown  : 2    (melee)
    ///     attackRange     : 2
    ///     moveSpeed       : 3
    ///     maxHP           : 350
    ///     detectionRange  : 15
    /// </summary>
    public class BossB : MonsterBase
    {
        // ── Inner type ───────────────────────────────────────────────────────

        private enum ChargePhase { None, Windup, Charging, Recovery }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Charge Pattern")]
        [Tooltip("Seconds between consecutive charges. Resets at end of recovery.")]
        [SerializeField] private float _chargeInterval = 6f;

        [Tooltip("HP fraction (0–1) below which the charge is allowed to trigger.")]
        [SerializeField] private float _chargeHpThreshold = 0.7f;

        [Tooltip("Seconds the boss vibrates before the charge launches.")]
        [SerializeField] private float _chargeWindupDuration = 0.8f;

        [Tooltip("Movement speed during the charge dash (world units / second).")]
        [SerializeField] private float _chargeSpeed = 15f;

        [Tooltip("Maximum seconds the charge can last before forced stop.")]
        [SerializeField] private float _chargeMaxDuration = 2f;

        [Tooltip("Maximum world-unit distance the charge travels before forced stop.")]
        [SerializeField] private float _chargeMaxDistance = 10f;

        [Tooltip("Damage dealt once on contact during the charge.")]
        [SerializeField] private float _chargeDamage = 30f;

        [Tooltip("Seconds the boss pauses after the charge ends.")]
        [SerializeField] private float _chargeRecoveryDuration = 0.5f;

        [Tooltip("XZ shake magnitude (world units) during the windup vibration.")]
        [SerializeField] private float _vibrateAmplitude = 0.15f;

        // ── Private runtime ──────────────────────────────────────────────────

        /// <summary>Local cache of AttackRange² — base._sqrAttackRange is private.</summary>
        private float _mySqrAttackRange;

        /// <summary>Pre-cached _chargeMaxDistance² — avoids per-frame multiply in UpdateCharging.</summary>
        private float _chargeMaxDistanceSqr;

        // Melee timer (mirrors base._attackTimer which is private).
        private float _meleeTimer;

        // Charge cooldown — ticks only when HP is below threshold and not charging.
        private float _chargeCooldown;

        // Charge state machine.
        private ChargePhase _chargePhase;

        // Windup state.
        private float   _windupTimer;
        private Vector3 _windupAnchor;    // saved world position; vibration oscillates around this
        private Vector3 _chargeDirection; // unit vector locked at windup start

        // Charging state.
        private float   _chargeDuration;
        private Vector3 _chargeStartPos;
        private bool    _chargeDamageDealt; // ensures at most one hit per charge

        // Recovery state.
        private float _recoveryTimer;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake(); // initialises _currentHP; logs warning if _data null

            if (_data == null) return;

            _mySqrAttackRange    = _data.AttackRange * _data.AttackRange;
            _chargeMaxDistanceSqr = _chargeMaxDistance * _chargeMaxDistance;

            // Charge cooldown begins counting immediately so the first charge fires
            // _chargeInterval seconds after the boss first enters Attack state.
            _chargeCooldown = _chargeInterval;
        }

        // ── State Overrides ──────────────────────────────────────────────────

        protected override void EnterAttack()
        {
            base.EnterAttack(); // sets _state = Attack; resets base private _attackTimer (unused)
            _meleeTimer = 0f;   // fire melee immediately on entering attack range
            // _chargeCooldown intentionally NOT reset here — carries across Chase↔Attack transitions
        }

        /// <summary>
        /// Drives two independent sub-systems while in Attack state:
        ///   • _meleeTimer  → PerformAttack() (direct hit)
        ///   • Charge state machine → windup → charge → recovery
        ///
        /// When _chargePhase is not None the range check is skipped so the charge
        /// can run to completion uninterrupted, even if the boss moves out of melee range.
        ///
        /// Fully overrides base.UpdateAttack() because base._attackTimer is private.
        /// Target-alive and range checks are re-implemented identically to base.
        /// </summary>
        protected override void UpdateAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                // If target is lost during windup, restore position before exiting.
                // Without this, _chargePhase stays non-None and restarts mid-windup
                // the next time the boss re-enters Attack state.
                if (_chargePhase == ChargePhase.Windup)
                    transform.position = _windupAnchor;

                _chargePhase = ChargePhase.None;
                EnterIdle();
                return;
            }

            // ── Charge state machine (takes full control while active) ────────
            if (_chargePhase != ChargePhase.None)
            {
                UpdateChargeStateMachine();
                return;
            }

            // ── Normal attack loop ────────────────────────────────────────────

            float sqrDist = (_currentTarget.Transform.position - transform.position).sqrMagnitude;
            if (sqrDist > _mySqrAttackRange)
            {
                EnterChase();
                return;
            }

            // Melee timer.
            _meleeTimer -= Time.deltaTime;
            if (_meleeTimer <= 0f)
            {
                PerformAttack();
                _meleeTimer = _data.AttackCooldown;
            }

            // Charge cooldown — only active below the HP threshold.
            if ((_currentHP / (float)_data.MaxHP) < _chargeHpThreshold)
            {
                _chargeCooldown -= Time.deltaTime;
                if (_chargeCooldown <= 0f)
                    EnterChargeWindup();
            }
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

        // ── Charge State Machine ─────────────────────────────────────────────

        private void UpdateChargeStateMachine()
        {
            switch (_chargePhase)
            {
                case ChargePhase.Windup:   UpdateChargeWindup();   break;
                case ChargePhase.Charging: UpdateCharging();       break;
                case ChargePhase.Recovery: UpdateChargeRecovery(); break;
            }
        }

        /// <summary>
        /// Enters the windup phase. Saves the current position as the vibration anchor
        /// and locks the charge direction toward the target's current position.
        /// If the target is already invalid at windup start, the charge is cancelled.
        /// </summary>
        private void EnterChargeWindup()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;

            _chargePhase  = ChargePhase.Windup;
            _windupTimer  = _chargeWindupDuration;
            _windupAnchor = transform.position;

            // Lock direction now — player has _chargeWindupDuration seconds to sidestep.
            Vector3 toTarget = _currentTarget.Transform.position - transform.position;
            toTarget.y = 0f;
            _chargeDirection = toTarget.magnitude > 0.001f
                ? toTarget.normalized
                : transform.forward;
        }

        /// <summary>
        /// Vibrates the boss around <see cref="_windupAnchor"/> for <see cref="_chargeWindupDuration"/>
        /// seconds, then snaps back to the anchor and transitions to Charging.
        /// Using Random.Range per frame produces a chaotic rumble rather than a smooth sine wave,
        /// which better telegraphs an imminent burst.
        /// </summary>
        private void UpdateChargeWindup()
        {
            _windupTimer -= Time.deltaTime;

            // Vibrate around anchor each frame.
            transform.position = _windupAnchor + new Vector3(
                Random.Range(-_vibrateAmplitude, _vibrateAmplitude),
                0f,
                Random.Range(-_vibrateAmplitude, _vibrateAmplitude)
            );

            if (_windupTimer <= 0f)
            {
                transform.position = _windupAnchor; // snap back before launching
                EnterCharging();
            }
        }

        /// <summary>
        /// Locks the charge origin and rotates the boss to face the locked direction.
        /// </summary>
        private void EnterCharging()
        {
            _chargePhase       = ChargePhase.Charging;
            _chargeDuration    = 0f;
            _chargeStartPos    = transform.position;
            _chargeDamageDealt = false;

            if (_chargeDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(_chargeDirection, Vector3.up);
        }

        /// <summary>
        /// Moves the boss along <see cref="_chargeDirection"/> at <see cref="_chargeSpeed"/>.
        /// Deals <see cref="_chargeDamage"/> once when the target enters attack range.
        /// Terminates when max duration or max distance is exceeded.
        /// </summary>
        private void UpdateCharging()
        {
            _chargeDuration += Time.deltaTime;

            // Advance along the locked direction — Y is kept constant (boss stays grounded).
            Vector3 move = _chargeDirection * (_chargeSpeed * Time.deltaTime);
            transform.position += move;

            // Contact damage: checked every frame; only deals damage once per charge.
            if (!_chargeDamageDealt && _currentTarget != null && _currentTarget.IsAlive)
            {
                float sqrDist = (_currentTarget.Transform.position - transform.position).sqrMagnitude;
                if (sqrDist <= _mySqrAttackRange)
                {
                    _currentTarget.TakeDamage(_chargeDamage);
                    _chargeDamageDealt = true;
                    EffectManager.Instance?.TriggerHitstop(this);
                }
            }

            // Stop conditions: max flight time OR max distance travelled.
            float sqrTravelled = (transform.position - _chargeStartPos).sqrMagnitude;
            bool  timeExpired  = _chargeDuration >= _chargeMaxDuration;
            bool  distReached  = sqrTravelled    >= _chargeMaxDistanceSqr;

            if (timeExpired || distReached)
                EnterChargeRecovery();
        }

        /// <summary>
        /// Halts the boss for a short recovery pause after the charge ends.
        /// </summary>
        private void EnterChargeRecovery()
        {
            _chargePhase   = ChargePhase.Recovery;
            _recoveryTimer = _chargeRecoveryDuration;
        }

        /// <summary>
        /// Waits for the recovery timer, then clears the charge phase and resets the
        /// cooldown so the next charge can trigger after _chargeInterval seconds.
        /// UpdateAttack() will resume its normal range check on the next frame.
        /// </summary>
        private void UpdateChargeRecovery()
        {
            _recoveryTimer -= Time.deltaTime;
            if (_recoveryTimer <= 0f)
            {
                _chargePhase    = ChargePhase.None;
                _chargeCooldown = _chargeInterval;
            }
        }
    }
}
