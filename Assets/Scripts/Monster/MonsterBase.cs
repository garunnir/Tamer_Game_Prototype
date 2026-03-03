using UnityEngine;

namespace WildTamer
{
    public enum MonsterState { Idle, Patrol, Chase, Attack, Dead }

    /// <summary>
    /// Abstract base for all monsters. Implements ICombatant as an enemy.
    /// All stats are read from an assigned MonsterData ScriptableObject.
    ///
    /// State machine: Idle → Patrol → Chase → Attack → Dead
    /// Each state has a matching Update* and Enter* method — all virtual so child
    /// classes (MonsterA/B/C, BossA/B) can extend or override individual states.
    ///
    /// Fires the static OnMonsterDied event on death; TamingSystem subscribes to this.
    /// Movement is direct Transform manipulation (no NavMesh).
    /// </summary>
    public abstract class MonsterBase : MonoBehaviour, ICombatant, IHitReactive
    {
        // ── Static Events ────────────────────────────────────────────────────

        /// <summary>Fired when this monster's HP reaches zero. TamingSystem subscribes here.</summary>
        public static event System.Action<MonsterBase> OnMonsterDied;

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Data")]
        [SerializeField] protected MonsterData _data;

        [Header("Patrol")]
        [SerializeField] private float _patrolRadius = 5f;
        [SerializeField] private float _idleDuration = 2f;

        // ── ICombatant ───────────────────────────────────────────────────────

        public CombatTeam Team           => CombatTeam.Enemy;
        public Transform  Transform      => transform;
        public bool       IsAlive        => _currentHP > 0f;
        public float      DetectionRange => _data != null ? _data.DetectionRange : 0f;

        // ── Protected state (readable by child classes) ───────────────────────

        protected MonsterState _state = MonsterState.Idle;
        protected ICombatant   _currentTarget;
        protected float        _currentHP;

        // ── Private runtime ──────────────────────────────────────────────────

        private float   _suspendTimer;
        private Vector3 _spawnPoint;
        private Vector3 _patrolTarget;
        private float   _idleTimer;
        private float   _attackTimer;
        private float   _sqrAttackRange;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _spawnPoint   = transform.position;
            _patrolTarget = _spawnPoint;

            if (_data == null)
            {
                Debug.LogWarning($"[{name}] MonsterData is not assigned.");
                _currentHP      = 1f;
                _sqrAttackRange = 0f;
                return;
            }

            _currentHP      = _data.MaxHP;
            _sqrAttackRange = _data.AttackRange * _data.AttackRange;
        }

        protected virtual void Start()
        {
            if (CombatSystem.Instance != null)
                CombatSystem.Instance.RegisterCombatant(this);
            else
                Debug.LogWarning($"[{name}] CombatSystem not found in scene.");

            EnterIdle();
        }

        protected virtual void OnDisable()
        {
            CombatSystem.Instance?.UnregisterCombatant(this);
        }

        protected virtual void Update()
        {
            if (_suspendTimer > 0f)
            {
                _suspendTimer -= Time.deltaTime;
                return;
            }

            switch (_state)
            {
                case MonsterState.Idle:   UpdateIdle();   break;
                case MonsterState.Patrol: UpdatePatrol(); break;
                case MonsterState.Chase:  UpdateChase();  break;
                case MonsterState.Attack: UpdateAttack(); break;
                case MonsterState.Dead:   UpdateDead();   break;
            }
        }

        // ── State Updates (virtual — override in child classes) ──────────────

        protected virtual void UpdateIdle()
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
                EnterPatrol();
        }

        protected virtual void UpdatePatrol()
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

        protected virtual void UpdateChase()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                EnterIdle();
                return;
            }

            float sqrDist = (_currentTarget.Transform.position - transform.position).sqrMagnitude;

            if (sqrDist <= _sqrAttackRange)
            {
                EnterAttack();
                return;
            }

            MoveToward(_currentTarget.Transform.position, _data.MoveSpeed);
        }

        protected virtual void UpdateAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                EnterIdle();
                return;
            }

            float sqrDist = (_currentTarget.Transform.position - transform.position).sqrMagnitude;

            if (sqrDist > _sqrAttackRange)
            {
                EnterChase();
                return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = _data.AttackCooldown;
            }
        }

        /// <summary>Dead state does nothing by default. Override for death animations.</summary>
        protected virtual void UpdateDead() { }

        // ── State Transitions (virtual — override in child classes) ──────────

        protected virtual void EnterIdle()
        {
            _state     = MonsterState.Idle;
            _idleTimer = _idleDuration;
        }

        protected virtual void EnterPatrol()
        {
            _state = MonsterState.Patrol;
            Vector2 circle = Random.insideUnitCircle * _patrolRadius;
            _patrolTarget  = _spawnPoint + new Vector3(circle.x, 0f, circle.y);
        }

        protected virtual void EnterChase()
        {
            _state = MonsterState.Chase;
        }

        protected virtual void EnterAttack()
        {
            _state       = MonsterState.Attack;
            _attackTimer = 0f; // fire immediately on entering attack range
        }

        protected virtual void EnterDead()
        {
            _state         = MonsterState.Dead;
            _currentHP     = 0f;
            _currentTarget = null;
            OnMonsterDied?.Invoke(this);
            OnDeath();
        }

        /// <summary>
        /// Override to play death animation / VFX before deactivation.
        /// Base implementation immediately deactivates the GameObject,
        /// which triggers OnDisable → CombatSystem.UnregisterCombatant.
        /// </summary>
        protected virtual void OnDeath()
        {
            gameObject.SetActive(false);
        }

        // ── IHitReactive Implementation ──────────────────────────────────────

        public void SuspendMotion(float duration)
        {
            _suspendTimer = Mathf.Max(_suspendTimer, duration);
        }

        // ── ICombatant Implementation ────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            _currentHP -= amount;
            EffectManager.Instance?.TriggerHitEffect(this, transform.position);
            if (_currentHP <= 0f)
                EnterDead();
        }

        public void OnTargetAssigned(ICombatant target)
        {
            _currentTarget = target;

            if (target != null && target.IsAlive)
            {
                if (_state == MonsterState.Idle || _state == MonsterState.Patrol)
                    EnterChase();
            }
            else if (_state == MonsterState.Chase || _state == MonsterState.Attack)
            {
                EnterIdle();
            }
        }

        // ── Protected Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Moves toward <paramref name="targetPosition"/> at <paramref name="speed"/> u/s.
        /// Ignores Y-axis delta so monsters stay grounded. Rotates to face movement direction.
        /// </summary>
        protected void MoveToward(Vector3 targetPosition, float speed)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude < 0.001f) return;

            direction.Normalize();
            transform.position += direction * speed * Time.deltaTime;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                360f * Time.deltaTime
            );
        }

        /// <summary>
        /// Called by UpdateAttack() each time the attack cooldown expires.
        /// Override in subclasses to replace ranged fire with melee or a special attack.
        /// Base implementation fires a pooled projectile at the current target.
        /// </summary>
        protected virtual void PerformAttack()
        {
            FireProjectile();
        }

        /// <summary>Spawns a projectile from the pool aimed at the current target.</summary>
        protected void FireProjectile()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;

            ProjectilePool.Instance?.Get(transform.position, _data.AttackDamage, _currentTarget);
        }
    }
}
