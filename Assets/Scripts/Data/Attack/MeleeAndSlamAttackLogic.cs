using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Runs two independent attack patterns while in Attack state:
    ///   Melee  — direct damage every _data.AttackCooldown seconds.
    ///   AoeSlam — every _slamInterval seconds, places _slamZoneCount AoeSlamZone
    ///             warning circles sequentially at the target's position.
    ///
    /// Zone pool is pre-allocated in Initialize(). Zones deactivate themselves
    /// after detonation; GetAvailableZone() finds them by !activeInHierarchy.
    /// Used by: BossA.
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeAndSlamAttack", menuName = "WildTamer/Attack/MeleeAndSlam")]
    public class MeleeAndSlamAttackLogic : AttackLogic
    {
        [Header("Melee")]
        // Attack cooldown is read from MonsterData.

        [Header("AoeSlam Pattern")]
        [Tooltip("Seconds between AoeSlam activations while in Attack state.")]
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

        // ── Runtime ──────────────────────────────────────────────────────────

        private float         _meleeTimer;
        private float         _slamTimer;
        private bool          _isSlamming;
        private int           _slamPhase;
        private float         _nextZoneTimer;
        private AoeSlamZone[] _zonePool;

        public override void Initialize(MonsterUnit owner)
        {
            _slamTimer = _slamInterval;

            _zonePool = new AoeSlamZone[_slamZoneCount];
            for (int i = 0; i < _slamZoneCount; i++)
            {
                var go = new GameObject($"{owner.name}_SlamZone_{i}");
                go.transform.SetParent(owner.transform.parent);
                _zonePool[i] = go.AddComponent<AoeSlamZone>();
                // AoeSlamZone.Awake() sets the GO inactive — pool starts ready.
            }
        }

        public override void OnEnterAttackState(MonsterUnit owner)
        {
            _meleeTimer = 0f; // fire melee immediately on engaging
            _isSlamming = false; // reset slam sequence in case of interrupted engagement
        }

        public override AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange)
        {
            if (!inAttackRange)
                return AttackTickResult.EnterChase;

            // ── Melee timer ──────────────────────────────────────────────────
            _meleeTimer -= Time.deltaTime;
            if (_meleeTimer <= 0f)
            {
                owner.DealMeleeDamage(target, owner.Data.AttackDamage);
                _meleeTimer = owner.Data.AttackCooldown;
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
                UpdateSlamSequence(owner, target);

            return AttackTickResult.Continue;
        }

        // ── Slam Sequence ────────────────────────────────────────────────────

        private void UpdateSlamSequence(MonsterUnit owner, ICombatant target)
        {
            _nextZoneTimer -= Time.deltaTime;
            if (_nextZoneTimer > 0f) return;

            if (_slamPhase < _slamZoneCount)
            {
                SpawnSlamZone(target);
                _slamPhase++;
            }

            if (_slamPhase >= _slamZoneCount)
            {
                _isSlamming = false;
            }
            else
            {
                _nextZoneTimer = _zoneSpawnInterval;
            }
        }

        private void SpawnSlamZone(ICombatant target)
        {
            if (target == null || !target.IsAlive) return;

            AoeSlamZone zone = GetAvailableZone();
            if (zone == null)
            {
                Debug.LogWarning("[MeleeAndSlamAttackLogic] No available AoeSlamZone — skipping.");
                return;
            }

            zone.Init(
                target.Transform.position,
                _slamRadius,
                _slamDamage,
                _warningDuration
            );
        }

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
