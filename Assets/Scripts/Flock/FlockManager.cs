using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton manager for the flock system.
    /// Neighbor detection and Boids steering are computed in FlockSteeringJob
    /// (Burst-compiled, parallel) every frame. Results are applied back on the
    /// main thread via FlockMoveLogic.ApplyVelocityAndRotation().
    /// FormationDataSO 배열을 직접 소유하여 포메이션 교체를 관리한다.
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class FlockManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static FlockManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

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

        // ── Runtime state ─────────────────────────────────────────────────────

        private readonly List<MonsterUnit>    _units       = new List<MonsterUnit>();
        public IReadOnlyList<MonsterUnit> Units => _units;
        private readonly List<FlockMoveLogic> _flockLogics = new List<FlockMoveLogic>();

        // 슬롯 월드 좌표 캐시
        private Vector3[] _slots;
        // 유닛 인덱스 → 슬롯 인덱스 매핑
        private int[]     _assignments;
        private bool[]    _slotTaken;
        private bool      _dirty;

        // ── Formation cache ────────────────────────────────────────────────────

        private FormationDataSO _currentFormation;
        private int             _currentFormationIndex = -1;
        private int             _cachedUnitCount  = -1;
        private float           _cachedSpacing    = -1f;
        private FormationDataSO _cachedFormation  = null;
        private Vector3[]       _offsets          = System.Array.Empty<Vector3>();

        // ── DOTS: NativeArrays + JobHandle ────────────────────────────────────

        private NativeArray<float3> _positionsNA;
        private NativeArray<float3> _slotsNA;
        private NativeArray<float3> _velocitiesNA;      // current velocities (persists frame-to-frame)
        private NativeArray<float3> _outVelocitiesNA;   // job output
        private NativeArray<bool>   _suspendedNA;
        private JobHandle           _steeringHandle;

        // ── Computed properties ───────────────────────────────────────────────

        private int SlotCount => _offsets.Length;

        private Vector3 Anchor => _playerTransform.position
                                + (_playerTransform.rotation * _pivotOffset)
                                + (_playerTransform.rotation * (_currentFormation != null ? _currentFormation.PositionOffset : Vector3.zero));

        private Quaternion PlayerYaw => Quaternion.Euler(
            0f,
            _playerTransform.eulerAngles.y
                + _formationRotationOffset
                + (_currentFormation != null ? _currentFormation.RotationOffset : 0f),
            0f);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_playerTransform == null)
                Debug.LogWarning("[FlockManager] Player Transform is not assigned.");

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

            if (_dirty)
                RecalculateAssignments(anchor, yaw);

            if (!_positionsNA.IsCreated || _positionsNA.Length != _units.Count) return;

            int count = _units.Count;

            // ── Populate job inputs ───────────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                MonsterUnit unit = _units[i];

                // 모든 유닛 위치는 separation 계산에 필요하므로 항상 채움
                _positionsNA[i] = unit.transform.position;

                bool isActive = unit.IsFollowing && !unit.IsMotionSuspended;
                _suspendedNA[i] = !isActive;

                if (isActive)
                {
                    _slotsNA[i] = _assignments != null
                        ? (float3)(anchor + (yaw * GetOffset(_assignments[i])))
                        : (float3)anchor;
                }
            }

            FlockMoveLogic refLogic = GetReferenceLogic();
            if (refLogic == null) return;

            // ── Schedule Burst job ────────────────────────────────────────────
            var job = new FlockSteeringJob
            {
                Positions                = _positionsNA,
                TargetSlots              = _slotsNA,
                Suspended                = _suspendedNA,
                CurrentVelocities        = _velocitiesNA,
                OutVelocities            = _outVelocitiesNA,
                PlayerPosition           = _playerTransform.position,
                SeparationRadiusSqr      = refLogic.SeparationRadius * refLogic.SeparationRadius,
                PlayerSeparationRadiusSqr = refLogic.PlayerSeparationRadius * refLogic.PlayerSeparationRadius,
                SeparationWeight         = refLogic.SeparationWeight,
                PlayerSeparationWeight   = refLogic.PlayerSeparationWeight,
                TargetWeight             = refLogic.TargetWeight,
                MaxSpeed                 = refLogic.MaxSpeed,
                SlowingDistance          = refLogic.SlowingDistance,
                Acceleration             = refLogic.Acceleration,
                DeltaTime                = Time.deltaTime,
                UnitCount                = count,
            };

            // innerloopBatchCount=4: 유닛 4개씩 배치로 워커 스레드 분배
            _steeringHandle = job.Schedule(count, 4);
            _steeringHandle.Complete();

            // out → current 복사 (다음 프레임의 CurrentVelocities 입력값)
            _outVelocitiesNA.CopyTo(_velocitiesNA);

            // ── Apply on main thread ──────────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                if (_suspendedNA[i]) continue;

                float3 vel = _outVelocitiesNA[i];
                _flockLogics[i]?.ApplyVelocityAndRotation(_units[i], new Vector3(vel.x, vel.y, vel.z));
            }
        }

        private void OnDisable()
        {
            _steeringHandle.Complete();
        }

        private void OnDestroy()
        {
            _steeringHandle.Complete();
            DisposeNativeArrays();
        }

        // ── Public API ────────────────────────────────────────────────────────

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
            ReallocNativeArrays(_units.Count);
            _dirty = true;
        }

        public void RemoveUnit(MonsterUnit unit)
        {
            if (unit == null) return;

            int index = _units.IndexOf(unit);
            if (index < 0) return;

            _units.RemoveAt(index);
            _flockLogics.RemoveAt(index);
            RecalculateOffsets(_units.Count);
            ReallocNativeArrays(_units.Count);
            _dirty = true;
        }

        public void SetFormationIndex(int index)
        {
            if (_formations == null || _formations.Length == 0) return;

            index = Mathf.Clamp(index, 0, _formations.Length - 1);
            _currentFormation      = _formations[index];
            _currentFormationIndex = index;
            RecalculateOffsets(_units.Count);
            _dirty = true;
        }

        public void NextFormation()
        {
            if (_formations == null || _formations.Length == 0) return;
            SetFormationIndex((_currentFormationIndex + 1) % _formations.Length);
        }

        public void PrevFormation()
        {
            if (_formations == null || _formations.Length == 0) return;
            SetFormationIndex((_currentFormationIndex - 1 + _formations.Length) % _formations.Length);
        }

        public void SetFormationData(FormationDataSO data)
        {
            _currentFormation      = data;
            _currentFormationIndex = -1;
            RecalculateOffsets(_units.Count);
            _dirty = true;
        }

        public void SetFormationSpacing(float spacing)
        {
            _spacing = spacing;
            RecalculateOffsets(_units.Count);
            _dirty = true;
        }

        public void SetFormationRotationOffset(float degrees)
        {
            _formationRotationOffset = degrees;
        }

        // ── Formation math ────────────────────────────────────────────────────

        private void RecalculateOffsets(int unitCount)
        {
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

            _currentFormation.Compute(unitCount, _spacing, _offsets);
            AlignToCentroid(unitCount);
        }

        private Vector3 GetOffset(int index)
        {
            if (index < 0 || index >= _offsets.Length) return Vector3.zero;
            return _offsets[index];
        }

        private void AlignToCentroid(int n)
        {
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < n; i++)
                centroid += _offsets[i];
            centroid /= n;

            for (int i = 0; i < n; i++)
                _offsets[i] -= centroid;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ReleaseOldest()
        {
            MonsterUnit evicted = _units[0];
            _units.RemoveAt(0);
            _flockLogics.RemoveAt(0);
            RecalculateOffsets(_units.Count);
            // NativeArrays는 AddUnit에서 새 count로 재할당됨
            _dirty = true;
            evicted.ReleaseFromFlock();
        }

        private void RecalculateAssignments(Vector3 anchor, Quaternion yaw)
        {
            int unitCount = _units.Count;
            int slotCount = SlotCount;

            if (slotCount == 0 || unitCount == 0)
            {
                _assignments = new int[unitCount];
                _dirty = false;
                return;
            }

            if (_slots == null || _slots.Length != slotCount)
                _slots = new Vector3[slotCount];

            for (int j = 0; j < slotCount; j++)
                _slots[j] = anchor + (yaw * GetOffset(j));

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

                _assignments[i]      = bestSlot;
                _slotTaken[bestSlot] = true;
            }

            _dirty = false;
        }

        private void InitializeFormation()
        {
            foreach (MonsterUnit unit in _initialUnits)
            {
                if (unit != null)
                    unit.SetFaction(FactionId.Player);
            }

            if (_units.Count == 0) return;

            RecalculateOffsets(_units.Count);
            ReallocNativeArrays(_units.Count);

            Vector3    center = _playerTransform != null ? Anchor : transform.position;
            Quaternion yaw    = _playerTransform != null ? PlayerYaw : Quaternion.identity;

            for (int i = 0; i < _units.Count; i++)
                _units[i].transform.position = center + (yaw * GetOffset(i));

            _dirty = true;
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

        /// <summary>첫 번째 유효한 FlockMoveLogic을 반환한다 (Job 파라미터 참조용).</summary>
        private FlockMoveLogic GetReferenceLogic()
        {
            for (int i = 0; i < _flockLogics.Count; i++)
                if (_flockLogics[i] != null) return _flockLogics[i];
            return null;
        }

        // ── NativeArray lifecycle ─────────────────────────────────────────────

        /// <summary>
        /// 유닛 수가 바뀔 때 NativeArray를 재할당한다.
        /// 기존 속도는 유지되지 않는다(유닛 목록이 바뀌므로 리셋이 더 안전하다).
        /// </summary>
        private void ReallocNativeArrays(int count)
        {
            _steeringHandle.Complete();
            DisposeNativeArrays();

            if (count == 0) return;

            _positionsNA     = new NativeArray<float3>(count, Allocator.Persistent);
            _slotsNA         = new NativeArray<float3>(count, Allocator.Persistent);
            _velocitiesNA    = new NativeArray<float3>(count, Allocator.Persistent);
            _outVelocitiesNA = new NativeArray<float3>(count, Allocator.Persistent);
            _suspendedNA     = new NativeArray<bool>  (count, Allocator.Persistent);
        }

        private void DisposeNativeArrays()
        {
            if (_positionsNA.IsCreated)     _positionsNA.Dispose();
            if (_slotsNA.IsCreated)         _slotsNA.Dispose();
            if (_velocitiesNA.IsCreated)    _velocitiesNA.Dispose();
            if (_outVelocitiesNA.IsCreated) _outVelocitiesNA.Dispose();
            if (_suspendedNA.IsCreated)     _suspendedNA.Dispose();
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_currentFormation == null || _playerTransform == null) return;

            Vector3    anchor    = Anchor;
            Quaternion yaw       = PlayerYaw;
            int        slotCount = SlotCount;

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
                Gizmos.color = assigned[j] ? Color.cyan : Color.red;
                Gizmos.DrawWireSphere(worldSlot, 0.3f);
            }

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
