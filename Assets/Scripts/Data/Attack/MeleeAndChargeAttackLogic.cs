using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Runs two independent attack patterns while in Attack state:
    ///   Melee  — direct damage every _data.AttackCooldown seconds.
    ///   Charge — triggers below _chargeHpThreshold HP, every _chargeInterval seconds.
    ///
    /// Three-phase charge sequence (Update-loop timers, no coroutines):
    ///   1. Windup   — owner vibrates in place; direction locked toward target.
    ///   2. Charging — owner rockets along locked direction; deals damage once on contact.
    ///   3. Recovery — brief pause before charge cooldown resets.
    ///
    /// During the Charging phase, the AttackLogic moves the owner via
    /// MonsterUnit.ApplyDirectVelocity(), bypassing normal MovementLogic.
    /// The inAttackRange parameter is ignored while any charge phase is active.
    /// Used by: BossB.
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeAndChargeAttack", menuName = "WildTamer/Attack/MeleeAndCharge")]
    public class MeleeAndChargeAttackLogic : AttackLogic
    {
        // ── Inner type ───────────────────────────────────────────────────────

        private enum ChargePhase { None, Windup, Charging, Recovery }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Charge Pattern")]
        [Tooltip("Seconds between consecutive charges. Resets after recovery.")]
        [SerializeField] private float _chargeInterval = 6f;

        [Tooltip("HP fraction (0–1) below which the charge is allowed to trigger.")]
        [SerializeField] private float _chargeHpThreshold = 0.7f;

        [Tooltip("Seconds the owner vibrates before the charge launches.")]
        [SerializeField] private float _chargeWindupDuration = 0.8f;

        [Tooltip("Movement speed during the charge dash (world units / second).")]
        [SerializeField] private float _chargeSpeed = 15f;

        [Tooltip("Maximum seconds the charge can last before forced stop.")]
        [SerializeField] private float _chargeMaxDuration = 2f;

        [Tooltip("Maximum world-unit distance the charge travels before forced stop.")]
        [SerializeField] private float _chargeMaxDistance = 10f;

        [Tooltip("Damage dealt once on contact during the charge.")]
        [SerializeField] private float _chargeDamage = 30f;

        [Tooltip("Seconds the owner pauses after the charge ends.")]
        [SerializeField] private float _chargeRecoveryDuration = 0.5f;

        [Tooltip("XZ shake magnitude (world units) during the windup vibration.")]
        [SerializeField] private float _vibrateAmplitude = 0.15f;

        [Header("VFX")]
        [Tooltip("Effect played at the owner's position when charge windup begins.")]
        [SerializeField] private ParticleSystem _windupVfxPrefab;
        [Tooltip("Effect played at the target's position on charge impact.")]
        [SerializeField] private ParticleSystem _impactVfxPrefab;

        // ── Runtime ──────────────────────────────────────────────────────────

        private float _meleeTimer;
        private float _chargeCooldown;
        private float _chargeMaxDistanceSqr;

        private ChargePhase _chargePhase;

        // Windup state
        private float   _windupTimer;
        private Vector3 _windupAnchor;
        private Vector3 _chargeDirection;

        // Charging state
        private float   _chargeDuration;
        private Vector3 _chargeStartPos;
        private bool    _chargeDamageDealt;

        // Recovery state
        private float _recoveryTimer;

        // ── AttackLogic API ──────────────────────────────────────────────────

        public override void Initialize(MonsterUnit owner)
        {
            _chargeCooldown       = _chargeInterval;
            _chargeMaxDistanceSqr = _chargeMaxDistance * _chargeMaxDistance;
        }

        public override void OnEnterAttackState(MonsterUnit owner)
        {
            _meleeTimer = 0f;
            // Restore position if interrupted mid-windup vibration
            if (_chargePhase == ChargePhase.Windup)
                owner.transform.position = _windupAnchor;
            _chargePhase = ChargePhase.None;
            // _chargeCooldown intentionally NOT reset — carries across Chase↔Attack transitions
        }

        public override AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange)
        {
            if (target == null || !target.IsAlive)
            {
                // Clean up windup vibration before exiting
                if (_chargePhase == ChargePhase.Windup)
                    owner.transform.position = _windupAnchor;

                _chargePhase = ChargePhase.None;
                return AttackTickResult.EnterIdle;
            }

            // Charge takes full control while active; skip range check
            if (_chargePhase != ChargePhase.None)
            {
                UpdateChargeStateMachine(owner, target);
                return AttackTickResult.Continue;
            }

            // Normal melee loop
            if (!inAttackRange)
                return AttackTickResult.EnterChase;

            _meleeTimer -= Time.deltaTime;
            if (_meleeTimer <= 0f)
            {
                owner.DealMeleeDamage(target, owner.Data.AttackDamage);
                _meleeTimer = owner.Data.AttackCooldown;
            }

            // Charge cooldown — only below the HP threshold
            float hpRatio = owner.CurrentHP / (float)owner.Data.MaxHP;
            if (hpRatio < _chargeHpThreshold)
            {
                _chargeCooldown -= Time.deltaTime;
                if (_chargeCooldown <= 0f)
                    EnterChargeWindup(owner, target);
            }

            return AttackTickResult.Continue;
        }

        // ── Charge State Machine ─────────────────────────────────────────────

        private void UpdateChargeStateMachine(MonsterUnit owner, ICombatant target)
        {
            switch (_chargePhase)
            {
                case ChargePhase.Windup:   UpdateChargeWindup(owner);            break;
                case ChargePhase.Charging: UpdateCharging(owner, target);        break;
                case ChargePhase.Recovery: UpdateChargeRecovery();               break;
            }
        }

        private void EnterChargeWindup(MonsterUnit owner, ICombatant target)
        {
            if (target == null || !target.IsAlive) return;

            _chargePhase  = ChargePhase.Windup;
            _windupTimer  = _chargeWindupDuration;
            _windupAnchor = owner.transform.position;

            EffectManager.Instance?.PlayVfxAt(_windupVfxPrefab, owner.transform.position);

            // Lock direction now — player has _chargeWindupDuration seconds to sidestep
            Vector3 toTarget = target.Transform.position - owner.transform.position;
            toTarget.y = 0f;
            _chargeDirection = toTarget.magnitude > 0.001f
                ? toTarget.normalized
                : owner.transform.forward;
        }

        private void UpdateChargeWindup(MonsterUnit owner)
        {
            _windupTimer -= Time.deltaTime;

            // Vibrate around anchor using random offsets for a chaotic rumble
            owner.transform.position = _windupAnchor + new Vector3(
                Random.Range(-_vibrateAmplitude, _vibrateAmplitude),
                0f,
                Random.Range(-_vibrateAmplitude, _vibrateAmplitude)
            );

            if (_windupTimer <= 0f)
            {
                owner.transform.position = _windupAnchor; // snap back before launch
                EnterCharging(owner);
            }
        }

        private void EnterCharging(MonsterUnit owner)
        {
            _chargePhase       = ChargePhase.Charging;
            _chargeDuration    = 0f;
            _chargeStartPos    = owner.transform.position;
            _chargeDamageDealt = false;

            if (_chargeDirection.sqrMagnitude > 0.001f)
                owner.transform.rotation = Quaternion.LookRotation(_chargeDirection, Vector3.up);
        }

        private void UpdateCharging(MonsterUnit owner, ICombatant target)
        {
            _chargeDuration += Time.deltaTime;

            owner.ApplyDirectVelocity(_chargeDirection * _chargeSpeed);

            // Deal damage once on contact
            if (!_chargeDamageDealt && target != null && target.IsAlive)
            {
                float sqrAttackRange = owner.Data.AttackRange * owner.Data.AttackRange;
                float sqrDist = (target.Transform.position - owner.transform.position).sqrMagnitude;
                if (sqrDist <= sqrAttackRange)
                {
                    target.TakeDamage(_chargeDamage);
                    EffectManager.Instance?.TriggerHitstop(owner);
                    EffectManager.Instance?.PlayVfxAt(_impactVfxPrefab, target.Transform.position);
                    _chargeDamageDealt = true;
                }
            }

            float sqrTravelled = (owner.transform.position - _chargeStartPos).sqrMagnitude;
            bool  timeExpired  = _chargeDuration >= _chargeMaxDuration;
            bool  distReached  = sqrTravelled    >= _chargeMaxDistanceSqr;

            if (timeExpired || distReached)
                EnterChargeRecovery();
        }

        private void EnterChargeRecovery()
        {
            _chargePhase   = ChargePhase.Recovery;
            _recoveryTimer = _chargeRecoveryDuration;
        }

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
