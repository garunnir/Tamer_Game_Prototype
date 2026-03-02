using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton manager for the flock system.
    /// Holds the live unit list, drives neighbor detection each frame via direct
    /// distance comparison (no Physics.OverlapSphere), and dispatches UpdateUnit()
    /// to each FlockUnit. Exposes AddUnit() / RemoveUnit() for TamingSystem.
    /// </summary>
    public class FlockManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        public static FlockManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Transform _playerTransform;

        [Header("Flock Settings")]
        [SerializeField] private int   _maxUnits        = 20;
        [SerializeField] private float _detectionRadius = 5f;

        [Header("Formation")]
        [SerializeField] private FormationHelper _formationHelper;

        [Header("Initial Units")]
        [SerializeField] private List<FlockUnit> _initialUnits = new List<FlockUnit>();

        // ── Runtime state ────────────────────────────────────────────────────

        private readonly List<FlockUnit> _units       = new List<FlockUnit>();
        private readonly List<FlockUnit> _nearbyBuffer = new List<FlockUnit>();

        /// Cached to avoid per-frame multiplication inside the detection loop.
        private float _sqrDetectionRadius;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sqrDetectionRadius = _detectionRadius * _detectionRadius;

            if (_playerTransform == null)
                Debug.LogWarning("[FlockManager] Player Transform is not assigned.");

            if (_formationHelper == null)
                Debug.LogWarning("[FlockManager] FormationHelper is not assigned.");
        }

        private void Start()
        {
            InitializeFormation();
        }

        private void Update()
        {
            if (_playerTransform == null || _units.Count == 0) return;

            Vector3 playerPosition = _playerTransform.position;

            for (int i = 0; i < _units.Count; i++)
            {
                BuildNearbyBuffer(i);

                Vector3 target = _formationHelper != null
                    ? playerPosition + _formationHelper.GetOffset(i)
                    : playerPosition;

                _units[i].UpdateUnit(_nearbyBuffer, target);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Adds a unit to the flock at the next circular formation slot near the player.
        /// Called by TamingSystem after a successful taming event.
        /// </summary>
        public void AddUnit(FlockUnit unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[FlockManager] AddUnit called with a null unit.");
                return;
            }

            if (_units.Count >= _maxUnits)
            {
                Debug.LogWarning("[FlockManager] Max unit count reached. Cannot add more units.");
                return;
            }

            int newIndex = _units.Count;
            _units.Add(unit);

            if (_formationHelper != null)
            {
                _formationHelper.Recalculate(_units.Count);

                if (_playerTransform != null)
                    unit.transform.position = _playerTransform.position
                                            + _formationHelper.GetOffset(newIndex);
            }
        }

        /// <summary>
        /// Removes a unit from the flock. The caller is responsible for
        /// destroying or deactivating the GameObject.
        /// </summary>
        public void RemoveUnit(FlockUnit unit)
        {
            if (unit == null) return;

            _units.Remove(unit);

            _formationHelper?.Recalculate(_units.Count);
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Adds all inspector-assigned initial units to the live list and
        /// arranges them in an evenly-spaced circle around the player.
        /// </summary>
        private void InitializeFormation()
        {
            for (int i = 0; i < _initialUnits.Count; i++)
            {
                if (_initialUnits[i] != null)
                    _units.Add(_initialUnits[i]);
            }

            if (_units.Count == 0) return;

            if (_formationHelper != null)
            {
                _formationHelper.Recalculate(_units.Count);

                Vector3 center = _playerTransform != null
                    ? _playerTransform.position
                    : transform.position;

                for (int i = 0; i < _units.Count; i++)
                    _units[i].transform.position = center + _formationHelper.GetOffset(i);
            }
        }

        /// <summary>
        /// Clears and refills the shared neighbor buffer for unit at index <paramref name="unitIndex"/>.
        /// Uses sqrMagnitude to avoid sqrt in the hot path.
        /// </summary>
        private void BuildNearbyBuffer(int unitIndex)
        {
            _nearbyBuffer.Clear();

            Vector3 origin = _units[unitIndex].transform.position;

            for (int j = 0; j < _units.Count; j++)
            {
                if (j == unitIndex) continue;

                Vector3 delta = _units[j].transform.position - origin;

                if (delta.sqrMagnitude < _sqrDetectionRadius)
                    _nearbyBuffer.Add(_units[j]);
            }
        }

    }
}
