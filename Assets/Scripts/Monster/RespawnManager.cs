using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Periodically spawns enemy monsters off-screen.
    /// Monsters self-register with CombatSystem in their own Start().
    ///
    /// Scene setup: add this component to any scene GameObject.
    ///   _spawnPrefabs — assign one or more MonsterUnit prefabs.
    ///   _camera       — assign Main Camera, or leave null (auto-found in Start).
    /// </summary>
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [Header("Spawn Prefabs")]
        [SerializeField] private MonsterUnit[] _spawnPrefabs;

        public IReadOnlyList<MonsterUnit> SpawnPrefabs => _spawnPrefabs;

        [Header("Settings")]
        [SerializeField] private float _spawnInterval   = 10f;
        [SerializeField] private int   _maxEnemyCount   = 15;
        [SerializeField] private float _spawnDistance   = 25f;

        [Header("References")]
        [SerializeField] private Camera _camera;

        private int _activeEnemyCount;

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
                _camera = Camera.main;

            GlobalEvents.OnUnitDied += OnUnitDied;

            InvokeRepeating(nameof(TrySpawn), _spawnInterval, _spawnInterval);
        }

        private void OnDestroy()
        {
            GlobalEvents.OnUnitDied -= OnUnitDied;
        }

        // ── Spawn Logic ──────────────────────────────────────────────────────

        private void TrySpawn()
        {
            if (_spawnPrefabs == null || _spawnPrefabs.Length == 0) return;
            if (_activeEnemyCount >= _maxEnemyCount) return;

            Vector3 spawnPos;
            if (!TryGetOffScreenPosition(out spawnPos)) return;

            int index = Random.Range(0, _spawnPrefabs.Length);
            MonsterUnit prefab = _spawnPrefabs[index];
            if (prefab == null) return;

            Instantiate(prefab, spawnPos, Quaternion.identity);
            _activeEnemyCount++;
        }

        /// <summary>
        /// Picks a random XZ direction and steps _spawnDistance units from the camera.
        /// Validates the resulting world point is outside the camera's viewport [0,1] range.
        /// Returns false after MaxAttempts if no valid position is found.
        /// </summary>
        private bool TryGetOffScreenPosition(out Vector3 result)
        {
            const int MaxAttempts = 10;

            Vector3 origin = _camera != null ? _camera.transform.position : Vector3.zero;
            origin.y = 0f;

            for (int i = 0; i < MaxAttempts; i++)
            {
                float   angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 candidate = origin + direction * _spawnDistance;

                if (_camera == null || IsOffScreen(candidate))
                {
                    result = candidate;
                    return true;
                }
            }

            result = Vector3.zero;
            return false;
        }

        private bool IsOffScreen(Vector3 worldPos)
        {
            Vector3 vp = _camera.WorldToViewportPoint(worldPos);
            // vp.z < 0 means behind the camera.
            return vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
        }

        // ── Event Handlers ───────────────────────────────────────────────────

        private void OnUnitDied(ITargetable unit)
        {
            // Only count enemies (non-player faction units).
            if (unit is ITeamable teamable && teamable.Faction != FactionId.Player)
                _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
        }
    }
}
