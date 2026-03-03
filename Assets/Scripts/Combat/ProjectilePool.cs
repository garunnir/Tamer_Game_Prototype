using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton object pool for Projectile instances.
    /// Assign a prefab in the Inspector for custom visuals; if none is assigned,
    /// a 0.2-unit sphere is created automatically as a fallback.
    /// All pooled objects are parented to this transform to keep the hierarchy tidy.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [Header("Pool")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private int        _poolSize = 20;

        private readonly Queue<Projectile> _pool = new Queue<Projectile>();

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_projectilePrefab == null)
                _projectilePrefab = CreateFallbackPrefab();

            Prewarm();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves a pooled projectile, positions and initialises it, then activates it.
        /// Spawns a new instance if the pool is exhausted.
        /// </summary>
        public Projectile Get(Vector3 origin, float damage, ICombatant target)
        {
            Projectile p = _pool.Count > 0 ? _pool.Dequeue() : SpawnNew();

            p.transform.position = origin;
            p.transform.rotation = Quaternion.identity;
            p.gameObject.SetActive(true);
            p.Init(damage, target);

            return p;
        }

        /// <summary>Deactivates a projectile and returns it to the pool.</summary>
        public void Return(Projectile p)
        {
            if (p == null) return;

            p.gameObject.SetActive(false);
            _pool.Enqueue(p);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void Prewarm()
        {
            for (int i = 0; i < _poolSize; i++)
                _pool.Enqueue(SpawnNew());
        }

        private Projectile SpawnNew()
        {
            GameObject go = Instantiate(_projectilePrefab, transform);
            go.SetActive(false);

            Projectile p = go.GetComponent<Projectile>();
            if (p == null)
                p = go.AddComponent<Projectile>();

            return p;
        }

        /// <summary>
        /// Creates a minimal sphere primitive used when no prefab is assigned.
        /// The Projectile component is added here so SpawnNew() finds it via GetComponent.
        /// </summary>
        private static GameObject CreateFallbackPrefab()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ProjectilePrefab_Fallback";
            go.transform.localScale = Vector3.one * 0.2f;
            go.AddComponent<Projectile>();
            go.SetActive(false);
            return go;
        }
    }
}
