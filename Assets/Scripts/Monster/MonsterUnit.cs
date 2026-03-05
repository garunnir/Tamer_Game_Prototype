using System.Collections;
using UnityEngine;

namespace WildTamer
{
    public enum MonsterState { Idle, Patrol, Chase, Attack, Follow, Dead }

    /// <summary>
    /// Unified monster unit. Replaces MonsterBase, MonsterA/B/C, BossA/B,
    /// FlockUnit, and FlockUnitCombat.
    ///
    /// Passive executor: this class owns the state machine framework and exposes
    /// helper methods, but contains no hardcoded behavior logic. All decisions
    /// are delegated to the injected MovementLogic and AttackLogic SOs.
    ///
    /// States:
    ///   Idle/Patrol/Chase/Attack — standard enemy loop.
    ///   Follow                   — tamed unit; movement driven by FlockManager,
    ///                              attack logic ticked here.
    ///   Dead                     — terminal; fires GlobalEvents.OnUnitDied then deactivates.
    ///
    /// Faction change (taming): call SetFaction(FactionId.Player).
    /// </summary>
    public class MonsterUnit : MonoBehaviour, ICombatant, ITeamable, ITameable, IHitReactive
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Data")]
        [SerializeField] private MonsterData _data;

        [Header("Faction")]
        [SerializeField] private FactionId _factionId = FactionId.Enemy;

        [Header("Strategy Overrides")]
        [Tooltip("Leave empty to use the defaults defined in MonsterData.")]
        [SerializeField] private MovementLogic _movementLogicOverride;
        [SerializeField] private AttackLogic   _attackLogicOverride;

        [Header("Patrol")]
        [SerializeField] private float _patrolRadius = 5f;
        [SerializeField] private float _idleDuration = 2f;

        [Header("Squash & Stretch")]
        [SerializeField] private float _stretchFactor  = 0.20f;
        [SerializeField] private float _squashFactor   = 0.30f;
        [SerializeField] private float _squashDuration = 0.15f;
        [SerializeField] private float _scaleSmoothing = 12f;

        // ── ICombatant / ITargetable / ITeamable ─────────────────────────────

        public Transform  Transform      => transform;
        public bool       IsAlive        => _currentHP > 0f;
        public float      DetectionRange => _data != null ? _data.DetectionRange : 0f;
        public FactionId  Faction        => _factionId;

        // ── Public (read by strategy SOs) ────────────────────────────────────

        public MonsterData   Data                => _data;
        public float         CurrentHP           => _currentHP;
        public MovementLogic ActiveMovementLogic => _movementLogic;
        public bool          IsMotionSuspended   => _suspendTimer > 0f;
        public bool          IsFollowing         => _state == MonsterState.Follow;

        /// <summary>Normalized heading; read by FlockMoveLogic for alignment.</summary>
        public Vector3 VelocityDirection { get; private set; }

        private const float TamingHealFraction   = 0.3f;
        private const float ReleaseDespawnDelay  = 5f;
        private const float TamingAnimDuration   = 0.9f;

        // ── Runtime state ────────────────────────────────────────────────────

        private MonsterState  _state = MonsterState.Idle;
        private ICombatant    _currentTarget;
        private float         _currentHP;
        private float         _suspendTimer;
        private float         _releaseTimer;
        private float         _idleTimer;
        private float         _sqrAttackRange;
        private Vector3       _spawnPoint;
        private Vector3       _patrolTarget;
        private MovementLogic _movementLogic;
        private AttackLogic   _attackLogic;
        private Vector3       _baseScale;
        private float         _currentSpeed;
        private float         _prevSpeed;
        private float         _squashTimer;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _spawnPoint   = transform.position;
            _patrolTarget = _spawnPoint;
            _baseScale    = transform.localScale;

            if (_data == null)
            {
                Debug.LogWarning($"[{name}] MonsterData is not assigned.");
                _currentHP = 1f;
                return;
            }

            _currentHP      = _data.MaxHP;
            _sqrAttackRange = _data.AttackRange * _data.AttackRange;

            // Clone SOs so each unit has independent runtime state.
            MovementLogic moveSource   = _movementLogicOverride != null
                ? _movementLogicOverride : _data.DefaultMovementLogic;
            AttackLogic   attackSource = _attackLogicOverride  != null
                ? _attackLogicOverride  : _data.DefaultAttackLogic;

            if (moveSource   != null) _movementLogic = Instantiate(moveSource);
            if (attackSource != null) _attackLogic   = Instantiate(attackSource);

            _movementLogic?.Initialize(this);
            _attackLogic?.Initialize(this);
        }

        private void Start()
        {
            if (CombatSystem.Instance != null)
                CombatSystem.Instance.RegisterCombatant(this);
            else
                Debug.LogWarning($"[{name}] CombatSystem not found in scene.");

            MinimapUnitTracker.Instance?.Register(this);

            EnterIdle();
        }

        private void OnDisable()
        {
            CombatSystem.Instance?.UnregisterCombatant(this);
            MinimapUnitTracker.Instance?.Unregister(this);
        }

        private void Update()
        {
            _currentSpeed = 0f;

            if (_suspendTimer > 0f)
            {
                _suspendTimer -= Time.deltaTime;
                return;
            }

            if (_releaseTimer > 0f)
            {
                _releaseTimer -= Time.deltaTime;
                if (_releaseTimer <= 0f)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }

            switch (_state)
            {
                case MonsterState.Idle:   UpdateIdle();   break;
                case MonsterState.Patrol: UpdatePatrol(); break;
                case MonsterState.Chase:  UpdateChase();  break;
                case MonsterState.Attack: UpdateAttack(); break;
                case MonsterState.Follow: UpdateFollow(); break;
                case MonsterState.Dead:   break;
            }

            UpdateSquashStretch();
        }

        // ── State Updates ────────────────────────────────────────────────────

        private void UpdateIdle()
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
                EnterPatrol();
        }

        private void UpdatePatrol()
        {
            Vector3 toTarget = _patrolTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude < 0.3f)
            {
                EnterIdle();
                return;
            }

            MoveToward(_patrolTarget, _data.MoveSpeed * 0.5f);
        }

        private void UpdateChase()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                EnterIdleOrFollow();
                return;
            }

            MoveTickResult result = _movementLogic != null
                ? _movementLogic.Tick(this, _currentTarget)
                : MoveTickResult.Continue;

            if (result == MoveTickResult.EnterAttack)
                EnterAttack();
        }

        private void UpdateAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                EnterIdleOrFollow();
                return;
            }

            bool inRange = (_currentTarget.Transform.position - transform.position).sqrMagnitude
                           <= _sqrAttackRange;

            if (_attackLogic == null) return;

            AttackTickResult result = _attackLogic.Tick(this, _currentTarget, inRange);
            switch (result)
            {
                case AttackTickResult.EnterChase: EnterChase();       break;
                case AttackTickResult.EnterIdle:  EnterIdleOrFollow(); break;
            }
        }

        /// <summary>
        /// Follow state: movement is driven externally by FlockManager each frame.
        /// This method only ticks the attack logic; state transitions are ignored
        /// so the unit never leaves Follow due to attack results.
        /// </summary>
        private void UpdateFollow()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _currentTarget = null;
                return;
            }

            bool inRange = (_currentTarget.Transform.position - transform.position).sqrMagnitude
                           <= _sqrAttackRange;
            _attackLogic?.Tick(this, _currentTarget, inRange);
        }

        // ── State Transitions ────────────────────────────────────────────────

        private void EnterIdle()
        {
            _state     = MonsterState.Idle;
            _idleTimer = _idleDuration;
        }

        private void EnterIdleOrFollow()
        {
            if (_factionId == FactionId.Player) EnterFollow();
            else EnterIdle();
        }

        private void EnterPatrol()
        {
            _state = MonsterState.Patrol;
            Vector2 circle = Random.insideUnitCircle * _patrolRadius;
            _patrolTarget  = _spawnPoint + new Vector3(circle.x, 0f, circle.y);
        }

        

        private void EnterChase()
        {
            _state = MonsterState.Chase;
        }

        private void EnterAttack()
        {
            _state = MonsterState.Attack;
            _attackLogic?.OnEnterAttackState(this);
        }

        private void EnterFollow()
        {
            _state         = MonsterState.Follow;
            _currentTarget = null;
        }

        private void EnterDead()
        {
            _state         = MonsterState.Dead;
            _currentHP     = 0f;
            _currentTarget = null;
            FlockManager.Instance?.RemoveUnit(this);
            GlobalEvents.FireUnitDied(this);
            gameObject.SetActive(false);
        }

        // ── ICombatant Implementation ────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            _currentHP -= amount;
            EffectManager.Instance?.TriggerHitEffect(this, transform.position);
            SoundManager.Instance?.PlayHit();
            if (_currentHP <= 0f)
                TryTameOrDie();
        }

        private void TryTameOrDie()
        {
            float chance = _data != null ? _data.TamingChance : 0f;
            if (Random.value < chance)
            {
                _currentTarget = null;
                _currentHP     = (_data != null ? _data.MaxHP : 1f) * TamingHealFraction;
                Tame();
            }
            else
            {
                EnterDead();
            }
        }

        public void OnTargetAssigned(ICombatant target)
        {
            _currentTarget = target;

            if (target != null && target.IsAlive)
            {
                if (_state == MonsterState.Idle || _state == MonsterState.Patrol || _state == MonsterState.Follow)
                    EnterChase();
            }
            else if (_state == MonsterState.Chase || _state == MonsterState.Attack)
            {
                EnterIdleOrFollow();
            }
        }

        // ── IHitReactive Implementation ──────────────────────────────────────

        public void SuspendMotion(float duration)
        {
            _suspendTimer = Mathf.Max(_suspendTimer, duration);
        }

        // ── Taming / Faction ─────────────────────────────────────────────────

        /// <summary>
        /// Changes this unit's faction. On becoming Player, swaps to flock movement
        /// and registers with FlockManager. Re-registers with CombatSystem as Ally.
        /// </summary>
        public void SetFaction(FactionId newFaction)
        {
            if (_factionId == newFaction) return;

            CombatSystem.Instance?.UnregisterCombatant(this);
            _factionId = newFaction;
            CombatSystem.Instance?.RegisterCombatant(this);

            if (newFaction == FactionId.Player)
                BecomeFlockUnit();
        }

        /// <summary>
        /// ITameable: plays the taming absorption animation, then switches faction
        /// to Player and fires the global taming event at the end of the animation.
        /// </summary>
        public void Tame()
        {
            _suspendTimer = TamingAnimDuration + 0.1f;
            StartCoroutine(TamingAbsorptionCoroutine());
        }

        /// <summary>
        /// Called by FlockManager when the squad is full and this unit is evicted (FIFO).
        /// Clears targeting, switches to Neutral so FactionSystem ignores it,
        /// enters Patrol, then despawns after ReleaseDespawnDelay seconds.
        /// </summary>
        public void ReleaseFromFlock()
        {
            _currentTarget = null;
            _spawnPoint    = transform.position;   // patrol around current location
            SetFaction(FactionId.Neutral);
            EnterPatrol();
            _releaseTimer = ReleaseDespawnDelay;
        }

        private void BecomeFlockUnit()
        {
            FlockManager.Instance?.AddUnit(this);
            EnterFollow();
        }

        // ── Strategy Helpers ─────────────────────────────────────────────────

        /// <summary>Moves toward a world position at speed, updating VelocityDirection.</summary>
        public void MoveToward(Vector3 targetPosition, float speed)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude < 0.001f) return;

            _currentSpeed = speed;
            direction.Normalize();
            transform.position += direction * speed * Time.deltaTime;
            VelocityDirection   = direction;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                360f * Time.deltaTime
            );
        }

        /// <summary>
        /// Applies a pre-computed velocity vector directly.
        /// Used by FlockMoveLogic (boids) and MeleeAndChargeAttackLogic (charge dash).
        /// </summary>
        public void ApplyDirectVelocity(Vector3 velocity)
        {
            if (velocity.magnitude < 0.001f) return;

            _currentSpeed       = velocity.magnitude;
            transform.position += velocity * Time.deltaTime;
            VelocityDirection   = velocity.normalized;

            // Y 성분 제거 후 LookRotation: XZ 평면 회전만 적용
            Vector3 flatDir = new Vector3(velocity.x, 0f, velocity.z);
            if (flatDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(flatDir.normalized, Vector3.up),
                    360f * Time.deltaTime
                );
            }
        }

        /// <summary>Fires a pooled projectile at the target.</summary>
        public void FireProjectileAt(ICombatant target)
        {
            if (target == null || !target.IsAlive) return;
            SoundManager.Instance?.PlayAttack();
            ProjectilePool.Instance?.Get(transform.position, _data.AttackDamage, target);
        }

        /// <summary>Deals direct melee damage to target and triggers hitstop on self.</summary>
        public void DealMeleeDamage(ICombatant target, float damage)
        {
            if (target == null || !target.IsAlive) return;
            SoundManager.Instance?.PlayAttack();
            target.TakeDamage(damage);
            EffectManager.Instance?.TriggerHitstop(this);
        }

        /// <summary>Delegates an AoE explosion to CombatSystem.</summary>
        public void DetonateAoe(Vector3 center, float radius, float damage, FactionId targetFaction)
        {
            CombatSystem.Instance?.DealAoeDamage(center, radius, damage, targetFaction);
        }

        /// <summary>
        /// Notifies the active MovementLogic that an attack was fired.
        /// Called by AttackLogic implementations before returning EnterChase.
        /// </summary>
        public void NotifyAttackFired()
        {
            _movementLogic?.OnAttackFired(this);
        }

        /// <summary>Directly sets HP (used by SaveManager on load, before faction assignment).</summary>
        public void SetHP(float value) =>
            _currentHP = Mathf.Clamp(value, 1f, _data != null ? _data.MaxHP : value);

        // ── Squash & Stretch ─────────────────────────────────────────────────

        private void UpdateSquashStretch()
        {
            if (_baseScale == Vector3.zero) return;

            float maxSpeed = _data != null ? _data.MoveSpeed : 1f;
            float speedT   = Mathf.Clamp01(_currentSpeed / Mathf.Max(maxSpeed, 0.001f));

            // Detect stop event: was moving, now stopped.
            if (_prevSpeed > maxSpeed * 0.3f && _currentSpeed < maxSpeed * 0.05f)
                _squashTimer = _squashDuration;

            Vector3 targetScale;
            if (_squashTimer > 0f)
            {
                _squashTimer -= Time.deltaTime;
                float squashY  = 1f - _squashFactor;
                float squashXZ = 1f + _squashFactor * 0.5f;
                targetScale = new Vector3(
                    _baseScale.x * squashXZ,
                    _baseScale.y * squashY,
                    _baseScale.z * squashXZ
                );
            }
            else
            {
                float stretchY  = 1f + _stretchFactor * speedT;
                float stretchXZ = 1f - _stretchFactor * 0.5f * speedT;
                targetScale = new Vector3(
                    _baseScale.x * stretchXZ,
                    _baseScale.y * stretchY,
                    _baseScale.z * stretchXZ
                );
            }

            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                _scaleSmoothing * Time.deltaTime
            );

            _prevSpeed = _currentSpeed;
        }

        // ── Taming Absorption ────────────────────────────────────────────────

        private IEnumerator TamingAbsorptionCoroutine()
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            Transform playerTransform = player != null ? player.transform : null;

            Vector3    startPos = transform.position;
            Quaternion startRot = transform.rotation;

            Vector3 toPlayer = playerTransform != null
                ? (playerTransform.position - startPos)
                : transform.forward;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f) toPlayer = transform.forward;

            Quaternion facePlayerRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

            float elapsed = 0f;
            while (elapsed < TamingAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / TamingAnimDuration;

                Vector3 playerPos = playerTransform != null ? playerTransform.position : startPos;

                // First half: slerp to face player.
                transform.rotation = Quaternion.Slerp(startRot, facePlayerRot, Mathf.Clamp01(t * 2f));

                // Pulse scale: 0 → peak → 0.
                transform.localScale = _baseScale * (1f + 0.6f * Mathf.Sin(t * Mathf.PI));

                // Second half: fly toward player.
                transform.position = Vector3.Lerp(startPos, playerPos, Mathf.Clamp01((t - 0.5f) * 2f));

                yield return null;
            }

            transform.localScale = _baseScale;
            SetFaction(FactionId.Player);
            GlobalEvents.FireTamingSucceeded(this);
        }
    }
}
