using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Combat companion for FlockUnit. Implements ICombatant as an ally.
    /// CombatSystem assigns the nearest enemy every 0.2 s via OnTargetAssigned().
    /// Handles HP, attack cooldown, projectile firing, and death independently
    /// of FlockUnit (Single Responsibility — movement stays in FlockUnit).
    /// </summary>
    [RequireComponent(typeof(FlockUnit))]
    public class FlockUnitCombat : MonoBehaviour, ICombatant
    {
        [Header("Combat Stats")]
        [SerializeField] private float _maxHP          = 50f;
        [SerializeField] private float _attackDamage   = 10f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private float _detectionRange = 8f;

        // ── ICombatant ───────────────────────────────────────────────────────

        public CombatTeam Team           => CombatTeam.Ally;
        public Transform  Transform      => transform;
        public bool       IsAlive        => _currentHP > 0f;
        public float      DetectionRange => _detectionRange;

        // ── Runtime state ────────────────────────────────────────────────────

        private float      _currentHP;
        private float      _attackTimer;
        private ICombatant _currentTarget;
        private FlockUnit  _flockUnit;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _flockUnit = GetComponent<FlockUnit>();
            _currentHP = _maxHP;
        }

        private void Start()
        {
            if (CombatSystem.Instance != null)
                CombatSystem.Instance.RegisterCombatant(this);
            else
                Debug.LogWarning("[FlockUnitCombat] CombatSystem not found in scene.");
        }

        private void OnDisable()
        {
            CombatSystem.Instance?.UnregisterCombatant(this);
        }

        private void Update()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _currentTarget = null;
                return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;

            ProjectilePool.Instance?.Get(transform.position, _attackDamage, _currentTarget);
            _attackTimer = _attackCooldown;
        }

        // ── ICombatant Implementation ────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            _currentHP -= amount;
            EffectManager.Instance?.TriggerHitEffect(this, transform.position);
            if (_currentHP <= 0f)
                Die();
        }

        public void OnTargetAssigned(ICombatant target)
        {
            _currentTarget = target;
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void Die()
        {
            _currentHP = 0f;
            FlockManager.Instance?.RemoveUnit(_flockUnit);
            gameObject.SetActive(false);
        }
    }
}
