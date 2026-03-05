using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton manager for all hit feedback effects.
    ///
    /// Effects triggered per hit:
    ///   Hitlag   — victim motion frozen for _hitlagDuration (default 0.08 s)
    ///   Hitstop  — attacker motion frozen for _hitstopDuration (default 0.05 s)
    ///   Shake    — QuarterViewCamera.CameraShake()
    ///   Flash    — red color flash via MaterialPropertyBlock for _flashDuration (default 0.1 s)
    ///   Spark    — pooled ParticleSystem burst at hit position
    ///
    /// Trigger points:
    ///   TriggerHitEffect() — called from MonsterBase.TakeDamage() and
    ///                        FlockUnitCombat.TakeDamage() (victim side).
    ///   TriggerHitstop()   — called from melee PerformAttack() methods on the
    ///                        attacker side (MonsterA, BossA, BossB).
    ///
    /// Scene setup: add this component to any scene GameObject.
    ///   _camera     — assign QuarterViewCamera in Inspector, or leave null (auto-found).
    ///   _sparkPrefab — assign a ParticleSystem prefab, or leave null (procedural default).
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("Camera Reference")]
        [SerializeField] private QuarterViewCamera _camera;

        [Header("Hitlag / Hitstop")]
        [SerializeField] private float _hitlagDuration  = 0.08f;
        [SerializeField] private float _hitstopDuration = 0.05f;

        [Header("Camera Shake")]
        [SerializeField] private float _shakeMagnitude = 0.15f;
        [SerializeField] private float _shakeDuration  = 0.1f;

        [Header("Color Flash")]
        [SerializeField] private Color _flashColor    = Color.white;
        [SerializeField] private float _flashDuration = 0.1f;

        [Header("Spark Pool")]
        [SerializeField] private ParticleSystem _sparkPrefab;
        [SerializeField] private int _sparkPoolSize = 10;

        private ParticleSystem[] _sparkPool;
        private readonly Dictionary<ParticleSystem, List<ParticleSystem>> _vfxPool = new();

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_camera == null)
                _camera = FindObjectOfType<QuarterViewCamera>();

            BuildSparkPool();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Fires all five hit feedback effects for a single damage event.
        /// Call immediately after reducing the victim's HP in TakeDamage().
        /// </summary>
        /// <param name="victim">Combatant that received the damage.</param>
        /// <param name="hitPosition">World-space hit position (typically victim.Transform.position).</param>
        /// <param name="attacker">Optional attacker; if provided, hitstop is also applied to it.</param>
        public void TriggerHitEffect(ICombatant victim, Vector3 hitPosition, ICombatant attacker = null)
        {
            ApplyHitlag(victim);
            if (attacker != null) ApplyHitstop(attacker);
            ShakeCamera();
            StartCoroutine(FlashColor(victim));
            PlaySpark(hitPosition);
        }

        /// <summary>
        /// Freezes the attacker for the hitstop duration.
        /// Call from melee PerformAttack() after dealing damage, when <c>this</c> is available.
        /// </summary>
        public void TriggerHitstop(ICombatant attacker)
        {
            ApplyHitstop(attacker);
        }

        /// <summary>
        /// Plays a one-shot particle effect at the given world position.
        /// Instances are pooled per prefab — idle instances are reused, new ones created on demand.
        /// Prefab's stopAction is overridden to Disable so instances return to the pool automatically.
        /// </summary>
        /// <param name="prefab">Source ParticleSystem prefab (assigned in each AttackLogic SO).</param>
        /// <param name="position">World-space position where the effect plays.</param>
        public void PlayVfxAt(ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null) return;

            if (!_vfxPool.TryGetValue(prefab, out var pool))
            {
                pool = new List<ParticleSystem>();
                _vfxPool[prefab] = pool;
            }

            ParticleSystem ps = null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeInHierarchy)
                {
                    ps = pool[i];
                    break;
                }
            }

            if (ps == null)
            {
                ps = Instantiate(prefab);
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Disable;
                pool.Add(ps);
            }

            ps.transform.position = position;
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        // ── Effect Implementations ───────────────────────────────────────────

        private void ApplyHitlag(ICombatant victim)
        {
            if (victim?.Transform == null) return;
            victim.Transform.GetComponent<IHitReactive>()?.SuspendMotion(_hitlagDuration);
        }

        private void ApplyHitstop(ICombatant attacker)
        {
            if (attacker?.Transform == null) return;
            attacker.Transform.GetComponent<IHitReactive>()?.SuspendMotion(_hitstopDuration);
        }

        private void ShakeCamera()
        {
            _camera?.CameraShake(_shakeMagnitude, _shakeDuration);
        }

        /// <summary>
        /// Flashes the victim's first child Renderer to _flashColor for _flashDuration seconds.
        /// Uses MaterialPropertyBlock so the shared material is never modified.
        /// </summary>
        private IEnumerator FlashColor(ICombatant victim)
        {
            if (victim?.Transform == null) yield break;

            Renderer rend = victim.Transform.GetComponentInChildren<Renderer>();
            if (rend == null) yield break;

            // Cache existing overrides so we can restore them after the flash.
            var originalBlock = new MaterialPropertyBlock();
            rend.GetPropertyBlock(originalBlock);

            var flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_BaseColor",     _flashColor);
            flashBlock.SetColor("_EmissionColor", _flashColor);
            rend.SetPropertyBlock(flashBlock);

            yield return new WaitForSeconds(_flashDuration);

            // Renderer may be null if the GO was destroyed while the coroutine ran.
            if (rend != null)
                rend.SetPropertyBlock(originalBlock);
        }

        private void PlaySpark(Vector3 position)
        {
            if (_sparkPool == null) return;

            foreach (ParticleSystem ps in _sparkPool)
            {
                if (!ps.gameObject.activeInHierarchy)
                {
                    ps.transform.position = position;
                    ps.gameObject.SetActive(true);
                    ps.Play();
                    return;
                }
            }
            // All slots active — silently skip (acceptable at this pool size).
        }

        // ── Spark Pool ───────────────────────────────────────────────────────

        private void BuildSparkPool()
        {
            _sparkPool = new ParticleSystem[_sparkPoolSize];
            for (int i = 0; i < _sparkPoolSize; i++)
            {
                ParticleSystem ps = _sparkPrefab != null
                    ? Instantiate(_sparkPrefab)
                    : CreateDefaultSpark();
                ps.gameObject.SetActive(false);
                _sparkPool[i] = ps;
            }
        }

        /// <summary>
        /// Builds a yellow-orange spark burst ParticleSystem entirely in code.
        /// Used when no prefab is assigned.
        /// stopAction = Disable automatically returns the GO to the pool when playback ends.
        /// </summary>
        private ParticleSystem CreateDefaultSpark()
        {
            var go = new GameObject("EffectManager_Spark");
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration                   = 0.2f;
            main.loop                       = false;
            main.startLifetime              = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSpeed                 = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize                  = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor                 = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0f),   // yellow
                new Color(1f, 0.4f,  0f)    // orange
            );
            main.maxParticles               = 15;
            main.gravityModifierMultiplier  = -0.5f;  // slight float upward
            main.stopAction                 = ParticleSystemStopAction.Disable;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;

            return ps;
        }
    }
}
