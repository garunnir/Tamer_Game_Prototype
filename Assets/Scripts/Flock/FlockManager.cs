using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton manager for the flock system.
    /// Holds the live ally unit list, drives neighbor detection each frame via direct
    /// distance comparison (no Physics.OverlapSphere), and calls TickWithNeighbors()
    /// on each unit's FlockMoveLogic. Exposes AddUnit() / RemoveUnit() for taming.
    /// </summary>
    [DefaultExecutionOrder(1)]
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

        [Header("Flock Logic")]
        [SerializeField] private FlockMoveLogic _flockMoveTemplate;

        [Header("Formation")]
        [SerializeField] private FormationHelper _formationHelper;

        [Header("Initial Units")]
        [SerializeField] private List<MonsterUnit> _initialUnits = new List<MonsterUnit>();

        // ── Runtime state ────────────────────────────────────────────────────

        private readonly List<MonsterUnit>   _units        = new List<MonsterUnit>();
        private readonly List<FlockMoveLogic> _flockLogics  = new List<FlockMoveLogic>();
        private readonly List<MonsterUnit>   _nearbyBuffer = new List<MonsterUnit>();

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
                if (!_units[i].IsFollowing) continue;

                BuildNearbyBuffer(i);

                Vector3 target = _formationHelper != null
                    ? playerPosition + _formationHelper.GetOffset(i)
                    : playerPosition;

                _flockLogics[i]?.TickWithNeighbors(_units[i], _nearbyBuffer, target);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Adds a tamed unit to the flock at the next formation slot near the player.
        /// Called by MonsterUnit.BecomeFlockUnit() on taming.
        /// </summary>
        public void AddUnit(MonsterUnit unit)
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
            _flockLogics.Add(CreateFlockLogic(unit));

            if (_formationHelper != null)
            {
                _formationHelper.Recalculate(_units.Count);

                if (_playerTransform != null)
                    unit.transform.position = _playerTransform.position
                                            + _formationHelper.GetOffset(newIndex);
            }
        }

        /// <summary>
        /// Removes a unit from the flock.
        /// Called automatically by MonsterUnit.EnterDead().
        /// </summary>
        public void RemoveUnit(MonsterUnit unit)
        {
            if (unit == null) return;

            int index = _units.IndexOf(unit);
            if (index < 0) return;

            _units.RemoveAt(index);
            _flockLogics.RemoveAt(index);
            _formationHelper?.Recalculate(_units.Count);
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Routes each _initialUnit through the canonical taming flow
        /// (SetFaction → BecomeFlockUnit → AddUnit + EnterFollow), then
        /// recalculates the formation and positions all units around the player.
        /// Must run in Start() so all MonsterUnit.Start() calls (order 0) have
        /// already registered with CombatSystem before we mutate their faction.
        /// </summary>
        private void InitializeFormation()
        {
            foreach (MonsterUnit unit in _initialUnits)
            {
                if (unit != null)
                    unit.SetFaction(FactionId.Player);
            }

            if (_units.Count == 0 || _formationHelper == null) return;

            _formationHelper.Recalculate(_units.Count);

            Vector3 center = _playerTransform != null
                ? _playerTransform.position
                : transform.position;

            for (int i = 0; i < _units.Count; i++)
                _units[i].transform.position = center + _formationHelper.GetOffset(i);
        }

        private FlockMoveLogic CreateFlockLogic(MonsterUnit unit)
        {
            if (_flockMoveTemplate == null)
            {
                Debug.LogWarning("[FlockManager] FlockMoveTemplate is not assigned.");
                return null;
            }

            FlockMoveLogic logic = Instantiate(_flockMoveTemplate);
            logic.Initialize(unit);
            return logic;
        }

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
