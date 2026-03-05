using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Detonates a ground explosion centred on the target's position,
    /// dealing damage to all Ally combatants within the radius.
    /// Exits to Chase if the target moves out of attack range.
    /// Used by: MonsterC.
    /// </summary>
    [CreateAssetMenu(fileName = "AoeExplosionAttack", menuName = "WildTamer/Attack/AoeExplosion")]
    public class AoeExplosionAttackLogic : AttackLogic
    {
        [Header("AoE Explosion")]
        [Tooltip("Radius of the ground explosion centred on the target's position.")]
        [SerializeField] private float _explosionRadius = 3f;

        [Tooltip("Seconds the warning ring is visible before the explosion fires.")]
        [SerializeField] private float _warningDuration = 1f;

        [Header("VFX")]
        [Tooltip("Explosion effect played at the target's position on detonation.")]
        [SerializeField] private ParticleSystem _explosionVfxPrefab;

        // ── Runtime ──────────────────────────────────────────────────────────

        private const int   RingSegments = 40;
        private const float RingWidth    = 0.16f;
        private const float GroundOffset = 0.02f;

        private float          _attackTimer;
        private bool           _isWarning;
        private float          _warnTimer;
        private LineRenderer   _warnRing;
        private TimedDeactivate _warnRingTimer;

        public override void Initialize(MonsterUnit owner)
        {
            var go = new GameObject($"{owner.name}_AoeWarning");
            go.transform.SetParent(owner.transform.parent);

            _warnRing                   = go.AddComponent<LineRenderer>();
            _warnRing.useWorldSpace     = true;
            _warnRing.loop              = true;
            _warnRing.widthMultiplier   = RingWidth;
            _warnRing.positionCount     = RingSegments;
            _warnRing.material          = new Material(Shader.Find("Sprites/Default"));
            _warnRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _warnRing.receiveShadows    = false;

            _warnRingTimer = go.AddComponent<TimedDeactivate>();
            go.SetActive(false);
        }

        public override void OnEnterAttackState(MonsterUnit owner)
        {
            _attackTimer = 0f;
            _isWarning   = false;
            if (_warnRing != null)
                _warnRing.gameObject.SetActive(false);
        }

        public override AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange)
        {
            // During the warning phase: keep moving toward the target and let the attack complete.
            // Damage is position-based (OverlapSphere at the fixed ring), so targets that moved
            // outside the explosion radius are naturally unaffected.
            if (_isWarning)
            {
                owner.MoveToward(target.Transform.position, owner.Data.MoveSpeed);

                _warnTimer -= Time.deltaTime;
                AnimateRing(owner, _warnTimer / _warningDuration);

                if (_warnTimer <= 0f)
                {
                    Vector3 center = _warnRing != null ? _warnRing.transform.position : target.Transform.position;
                    CancelWarning();
                    owner.DetonateAoe(center, _explosionRadius, owner.Data.AttackDamage, FactionId.Player);
                    EffectManager.Instance?.PlayVfxAt(_explosionVfxPrefab, center);
                    _attackTimer = owner.Data.AttackCooldown;
                }

                return AttackTickResult.Continue;
            }

            if (!inAttackRange)
                return AttackTickResult.EnterChase;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
                StartWarning(owner, target);

            return AttackTickResult.Continue;
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        private void StartWarning(MonsterUnit owner, ICombatant target)
        {
            if (_warnRing == null) return;

            Vector3 pos = target.Transform.position;
            pos.y += GroundOffset;
            _warnRing.transform.position = pos;

            Color ringColor = owner.Faction == FactionId.Player ? Color.green : Color.red;
            BuildWorldRingPoints(pos, _explosionRadius);

            _warnRing.startColor = ringColor;
            _warnRing.endColor   = ringColor;
            if (_warnRingTimer != null)
                _warnRingTimer.Delay = _warningDuration;

            _warnRing.gameObject.SetActive(true); // OnEnable starts the timer

            _isWarning = true;
            _warnTimer = _warningDuration;
        }

        private void AnimateRing(MonsterUnit owner, float t)
        {
            if (_warnRing == null) return;

            float blinkSpeed = Mathf.Lerp(10f, 2f, Mathf.Clamp01(t));
            float alpha      = Mathf.Lerp(0.3f, 1f, Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed)));

            Color ringColor  = owner.Faction == FactionId.Player ? Color.green : Color.red;
            ringColor.a      = alpha;
            _warnRing.startColor = ringColor;
            _warnRing.endColor   = ringColor;
        }

        private void CancelWarning()
        {
            _isWarning = false;
            if (_warnRing != null)
                _warnRing.gameObject.SetActive(false);
        }

        private void BuildWorldRingPoints(Vector3 center, float radius)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                _warnRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }
}
