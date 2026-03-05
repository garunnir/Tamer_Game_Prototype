using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton manager for the flock system.
    /// Holds the live ally unit list, drives neighbor detection each frame via direct
    /// distance comparison (no Physics.OverlapSphere), and calls TickWithNeighbors()
    /// on each unit's FlockMoveLogic. Exposes AddUnit() / RemoveUnit() for taming.
    /// FormationDataSO 배열을 직접 소유하여 포메이션 교체를 관리한다.
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
        [SerializeField] private FormationDataSO[] _formations;
        [SerializeField] private int               _defaultFormationIndex   = 0;
        [SerializeField] private float             _spacing                 = 1.5f;
        [SerializeField] private Vector3           _pivotOffset             = Vector3.zero;
        [SerializeField] private float             _formationRotationOffset = 0f;

        [Header("Initial Units")]
        [SerializeField] private List<MonsterUnit> _initialUnits = new List<MonsterUnit>();

        // ── Runtime state ────────────────────────────────────────────────────

        private readonly List<MonsterUnit>    _units       = new List<MonsterUnit>();
        public IReadOnlyList<MonsterUnit> Units => _units;
        private readonly List<FlockMoveLogic> _flockLogics = new List<FlockMoveLogic>();
        private readonly List<MonsterUnit>    _nearbyBuffer = new List<MonsterUnit>();

        // 슬롯 월드 좌표 캐시 (앵커 기준 오프셋 합산 결과)
        private Vector3[] _slots;
        // 유닛 인덱스 → 슬롯 인덱스 매핑 (_assignments[unitIdx] = slotIdx)
        private int[]     _assignments;
        // 슬롯 중복 선택 방지용 플래그 배열 (RecalculateAssignments 내 GC 방지)
        private bool[]    _slotTaken;
        // 재계산이 필요한지 여부 플래그
        private bool      _dirty;

        private float _sqrDetectionRadius;

        // ── Formation cache (FormationHelper 흡수) ────────────────────────────

        private FormationDataSO _currentFormation;
        private int             _currentFormationIndex = -1;
        // 더티 플래그 캐시 — 동일 입력 시 재계산 생략
        private int             _cachedUnitCount  = -1;
        private float           _cachedSpacing    = -1f;
        private FormationDataSO _cachedFormation  = null;
        // 슬롯 로컬 오프셋 배열 (centroid 정렬 완료)
        private Vector3[]       _offsets          = System.Array.Empty<Vector3>();

        // ── Computed properties ───────────────────────────────────────────────

        private int SlotCount => _offsets.Length;

        // 앵커 = 플레이어 위치 + 피벗 오프셋 + 포메이션 위치 오프셋 (모두 플레이어 회전 적용)
        private Vector3 Anchor => _playerTransform.position
                                + (_playerTransform.rotation * _pivotOffset)
                                + (_playerTransform.rotation * (_currentFormation != null ? _currentFormation.PositionOffset : Vector3.zero));

        // Y축 회전만 추출 + FlockManager 회전 오프셋 + 포메이션 회전 오프셋 합산
        private Quaternion PlayerYaw => Quaternion.Euler(
            0f,
            _playerTransform.eulerAngles.y
                + _formationRotationOffset
                + (_currentFormation != null ? _currentFormation.RotationOffset : 0f),
            0f);

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

            // 기본 포메이션 초기화
            if (_formations != null && _formations.Length > 0)
            {
                int clampedIndex = Mathf.Clamp(_defaultFormationIndex, 0, _formations.Length - 1);
                _currentFormation      = _formations[clampedIndex];
                _currentFormationIndex = clampedIndex;
            }
            else
            {
                Debug.LogWarning("[FlockManager] Formations array is empty or not assigned.");
            }
        }

        private void Start()
        {
            InitializeFormation();
        }

        private void Update()
        {
            if (_playerTransform == null || _units.Count == 0) return;

            Vector3    anchor = Anchor;
            Quaternion yaw    = PlayerYaw;

            // dirty 상태면 슬롯 재할당 수행
            if (_dirty)
                RecalculateAssignments(anchor, yaw);

            for (int i = 0; i < _units.Count; i++)
            {
                if (!_units[i].IsFollowing) continue;

                BuildNearbyBuffer(i);

                // 플레이어 Y축 회전을 로컬 오프셋에 적용해 월드 슬롯 위치 계산
                // Quaternion * Vector3: XZ 평면 회전만 수행 (오프셋 Y=0 보장됨)
                Vector3 target = _assignments != null
                    ? anchor + (yaw * GetOffset(_assignments[i]))
                    : anchor;

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
                ReleaseOldest();

            _units.Add(unit);
            _flockLogics.Add(CreateFlockLogic(unit));
            RecalculateOffsets(_units.Count);
            _dirty = true; // 유닛 수 변화로 인해 슬롯 재할당 필요
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
            RecalculateOffsets(_units.Count);
            _dirty = true; // 유닛 사망으로 인해 슬롯 재할당 필요
        }

        /// <summary>인덱스로 포메이션을 교체한다.</summary>
        public void SetFormationIndex(int index)
        {
            if (_formations == null || _formations.Length == 0) return;

            index = Mathf.Clamp(index, 0, _formations.Length - 1);
            _currentFormation      = _formations[index];
            _currentFormationIndex = index;
            RecalculateOffsets(_units.Count);
            _dirty = true;
        }

        /// <summary>다음 포메이션으로 순환한다.</summary>
        public void NextFormation()
        {
            if (_formations == null || _formations.Length == 0) return;
            SetFormationIndex((_currentFormationIndex + 1) % _formations.Length);
        }

        /// <summary>이전 포메이션으로 순환한다.</summary>
        public void PrevFormation()
        {
            if (_formations == null || _formations.Length == 0) return;
            SetFormationIndex((_currentFormationIndex - 1 + _formations.Length) % _formations.Length);
        }

        /// <summary>SO를 직접 지정해 포메이션을 교체한다.</summary>
        public void SetFormationData(FormationDataSO data)
        {
            _currentFormation      = data;
            _currentFormationIndex = -1; // 배열 외부 SO이므로 인덱스 무효화
            RecalculateOffsets(_units.Count);
            _dirty = true;
        }

        /// <summary>런타임에 슬롯 간격을 변경한다.</summary>
        public void SetFormationSpacing(float spacing)
        {
            _spacing = spacing;
            RecalculateOffsets(_units.Count);
            _dirty = true;
        }

        /// <summary>런타임에 포메이션 회전 오프셋을 변경한다 (도 단위, 플레이어 yaw에 합산).</summary>
        public void SetFormationRotationOffset(float degrees)
        {
            _formationRotationOffset = degrees;
        }

        // ── Formation math (FormationHelper 흡수) ────────────────────────────

        /// <summary>
        /// 포메이션 슬롯 오프셋 배열을 재계산한다.
        /// unitCount, _spacing, _currentFormation 중 하나라도 변경되었을 때만 실제로 연산한다.
        /// </summary>
        private void RecalculateOffsets(int unitCount)
        {
            // 더티 플래그: 입력이 동일하면 재계산 생략
            if (unitCount == _cachedUnitCount &&
                Mathf.Approximately(_spacing, _cachedSpacing) &&
                _currentFormation == _cachedFormation)
            {
                return;
            }

            _cachedUnitCount = unitCount;
            _cachedSpacing   = _spacing;
            _cachedFormation = _currentFormation;

            if (unitCount <= 0 || _currentFormation == null)
            {
                _offsets = System.Array.Empty<Vector3>();
                return;
            }

            _offsets = new Vector3[unitCount];

            if (unitCount == 1)
            {
                _offsets[0] = Vector3.zero;
                return;
            }

            // 포메이션 SO에 슬롯 계산 위임
            _currentFormation.Compute(unitCount, _spacing, _offsets);

            // 무게중심(centroid)을 원점(0,0,0)으로 정렬
            AlignToCentroid(unitCount);
        }

        /// <summary>index번째 슬롯의 로컬 오프셋을 반환한다. 범위 밖이면 Vector3.zero.</summary>
        private Vector3 GetOffset(int index)
        {
            if (index < 0 || index >= _offsets.Length)
                return Vector3.zero;

            return _offsets[index];
        }

        /// <summary>
        /// 모든 슬롯 위치의 산술 평균(무게중심)을 계산하고,
        /// 각 위치에서 빼 포메이션 전체를 (0,0,0) 중심으로 정렬한다.
        ///
        /// centroid = (1/n) × Σ positions[i]
        /// positions[i] -= centroid
        /// </summary>
        private void AlignToCentroid(int n)
        {
            // 1단계: 무게중심 계산
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < n; i++)
                centroid += _offsets[i];
            centroid /= n;

            // 2단계: 각 슬롯에서 무게중심을 빼 원점 정렬
            for (int i = 0; i < n; i++)
                _offsets[i] -= centroid;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Evicts the oldest flock member (index 0) to make room for a new tame.
        /// The released unit switches to Neutral, patrols briefly, then despawns.
        /// </summary>
        private void ReleaseOldest()
        {
            MonsterUnit evicted = _units[0];
            _units.RemoveAt(0);
            _flockLogics.RemoveAt(0);
            RecalculateOffsets(_units.Count);
            _dirty = true;
            evicted.ReleaseFromFlock();
        }

        /// <summary>
        /// 탐욕적(greedy) 방식으로 각 유닛에 가장 가까운 슬롯을 할당한다.
        /// 이미 선택된 슬롯은 다른 유닛이 선택하지 못하도록 slotTaken 배열로 관리한다.
        /// </summary>
        private void RecalculateAssignments(Vector3 anchor, Quaternion yaw)
        {
            int unitCount = _units.Count;
            int slotCount = SlotCount;

            // 슬롯이 없거나 유닛이 없으면 빈 배열로 초기화
            if (slotCount == 0 || unitCount == 0)
            {
                _assignments = new int[unitCount];
                _dirty = false;
                return;
            }

            // 슬롯 월드 좌표 캐시 갱신 (Y축 회전 적용)
            if (_slots == null || _slots.Length != slotCount)
                _slots = new Vector3[slotCount];

            for (int j = 0; j < slotCount; j++)
                _slots[j] = anchor + (yaw * GetOffset(j));

            // 슬롯 중복 선택 방지용 플래그 배열 — 크기가 달라질 때만 재할당 (GC 방지)
            if (_slotTaken == null || _slotTaken.Length != slotCount)
                _slotTaken = new bool[slotCount];
            else
                System.Array.Clear(_slotTaken, 0, slotCount);

            if (_assignments == null || _assignments.Length != unitCount)
                _assignments = new int[unitCount];

            for (int i = 0; i < unitCount; i++)
            {
                float bestSqr  = float.MaxValue;
                int   bestSlot = 0;

                // 각 유닛에서 가장 가까운 미할당 슬롯 탐색
                for (int j = 0; j < slotCount; j++)
                {
                    if (_slotTaken[j]) continue;

                    float sqr = (_units[i].transform.position - _slots[j]).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr  = sqr;
                        bestSlot = j;
                    }
                }

                // 최적 슬롯 할당 및 중복 방지 플래그 설정
                _assignments[i]      = bestSlot;
                _slotTaken[bestSlot] = true;
            }

            _dirty = false;
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

            if (_units.Count == 0) return;

            RecalculateOffsets(_units.Count);

            Vector3    center = _playerTransform != null ? Anchor : transform.position;
            Quaternion yaw    = _playerTransform != null ? PlayerYaw : Quaternion.identity;

            for (int i = 0; i < _units.Count; i++)
                _units[i].transform.position = center + (yaw * GetOffset(i));

            _dirty = true; // 초기 배치 후 슬롯 재할당 필요
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

        // ── Gizmos ───────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_currentFormation == null || _playerTransform == null) return;

            Vector3    anchor    = Anchor;
            Quaternion yaw       = PlayerYaw;
            int        slotCount = SlotCount;

            // 할당 여부 확인을 위한 배열 구성
            bool[] assigned = new bool[slotCount];
            if (_assignments != null)
            {
                for (int i = 0; i < _assignments.Length && i < _units.Count; i++)
                {
                    int slotIdx = _assignments[i];
                    if (slotIdx >= 0 && slotIdx < slotCount)
                        assigned[slotIdx] = true;
                }
            }

            for (int j = 0; j < slotCount; j++)
            {
                Vector3 worldSlot = anchor + (yaw * GetOffset(j));

                // 할당된 슬롯은 청록색, 비어있는 슬롯은 빨간색으로 표시
                Gizmos.color = assigned[j] ? Color.cyan : Color.red;
                Gizmos.DrawWireSphere(worldSlot, 0.3f);
            }

            // 각 유닛에서 할당된 슬롯까지 선 그리기
            if (_assignments != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < _units.Count && i < _assignments.Length; i++)
                {
                    if (_units[i] == null) continue;

                    int slotIdx = _assignments[i];
                    if (slotIdx < 0 || slotIdx >= slotCount) continue;

                    Vector3 worldSlot = anchor + (yaw * GetOffset(slotIdx));
                    Gizmos.DrawLine(_units[i].transform.position, worldSlot);
                }
            }
        }
#endif
    }
}
