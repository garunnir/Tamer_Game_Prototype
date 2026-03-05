using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Boids flocking movement: Separation + Target-seek with arrival slowing.
    /// Driven externally by FlockManager via TickWithNeighbors() each frame.
    /// The standard Tick() is a no-op (returns Continue) because FlockManager
    /// owns the update loop for flock units.
    /// Used by: tamed MonsterUnits in Follow state.
    /// </summary>
    [CreateAssetMenu(fileName = "FlockMove", menuName = "WildTamer/Movement/Flock")]
    public class FlockMoveLogic : MovementLogic
    {
        [Header("Flocking Weights")]
        [SerializeField] private float _separationWeight = 1.0f;
        [SerializeField] private float _targetWeight     = 3.0f;

        [Header("Separation")]
        [SerializeField] private float _separationRadius       = 1.5f;
        [SerializeField] private float _playerSeparationRadius = 2.0f;
        [SerializeField] private float _playerSeparationWeight = 2.0f;

        [Header("Movement")]
        [SerializeField] private float _maxSpeed        = 7f;
        [SerializeField] private float _rotationSpeed   = 360f;
        [SerializeField] private float _acceleration    = 8f;
        [SerializeField] private float _slowingDistance = 2f;

        // ── Runtime (캐시: Initialize에서 설정) ──────────────────────────────

        private Vector3   _currentVelocity;
        private Rigidbody _rb;
        private Transform _ownerTransform;
        private Transform _playerTransform;

        public override void Initialize(MonsterUnit owner)
        {
            _currentVelocity = Vector3.zero;
            _rb              = owner.GetComponent<Rigidbody>();   // null 허용
            _ownerTransform  = owner.transform;
            // PlayerController를 찾아 플레이어 방향 회전에 사용 (null 허용)
            _playerTransform = Object.FindObjectOfType<PlayerController>()?.transform;
        }

        /// <summary>No-op: movement is driven by FlockManager via TickWithNeighbors.</summary>
        public override MoveTickResult Tick(MonsterUnit owner, ICombatant target)
            => MoveTickResult.Continue;

        /// <summary>
        /// Called every frame by FlockManager with pre-built neighbor and target data.
        /// Skips if the unit's motion is suspended (hit stun).
        /// </summary>
        public void TickWithNeighbors(
            MonsterUnit owner,
            List<MonsterUnit> nearbyUnits,
            Vector3 targetPosition)
        {
            if (owner.IsMotionSuspended) return;

            // 슬롯까지의 거리 계산
            float distToSlot = Vector3.Distance(_ownerTransform.position, targetPosition);

            // 슬롯 근처에서 separation 힘을 선형적으로 줄여 지터 방지
            // (슬롯에 가까울수록 0으로 감쇠, 멀수록 1에 수렴)
            float jitterDampen = Mathf.Clamp01(distToSlot / _slowingDistance);

            Vector3 separation       = CalculateSeparation(nearbyUnits) * _separationWeight * jitterDampen;
            Vector3 playerSeparation = CalculatePlayerSeparation() * _playerSeparationWeight;
            Vector3 targetSeek       = CalculateTargetSeek(targetPosition);

            // FlockManager가 정의한 슬롯(targetPosition)을 강하게 따르되,
            // 유닛 간 과도한 겹침만 separation으로 완화한다.
            // 플레이어 분리 힘은 jitterDampen 없이 항상 적용한다.
            Vector3 desired    = separation + playerSeparation + targetSeek * _targetWeight;
            float   desiredMag = desired.magnitude;

            if (desiredMag > 0.001f)
            {
                // 슬롯 가까워질수록 속도 감소 (오버슈팅 방지)
                desired = (desired / desiredMag) * (_maxSpeed * jitterDampen);
            }

            _currentVelocity = Vector3.MoveTowards(
                _currentVelocity,
                desired,
                _acceleration * Time.deltaTime
            );

            // 현재 속도 크기 캐싱 (Rigidbody 적용 및 회전 판단에 재사용)
            float currentSpeed = _currentVelocity.magnitude;

            // Rigidbody가 있으면 물리 기반 이동, 없으면 직접 이동으로 fallback
            if (_rb != null)
                _rb.linearVelocity = _currentVelocity;
            else if (currentSpeed > 0.001f)
                owner.ApplyDirectVelocity(_currentVelocity);

            // 이동 방향 또는 플레이어 방향으로 회전
            ApplyAdaptiveRotation(owner, currentSpeed);
        }

        // ── Force Calculations ───────────────────────────────────────────────

        private Vector3 CalculateSeparation(List<MonsterUnit> nearbyUnits)
        {
            Vector3 force = Vector3.zero;

            foreach (MonsterUnit neighbor in nearbyUnits)
            {
                // 이웃 유닛과의 거리 벡터 계산
                Vector3 diff     = _ownerTransform.position - neighbor.transform.position;
                float   distance = diff.magnitude;

                // 분리 반경 내에 있으면 거리에 반비례하는 반발력 적용
                if (distance < _separationRadius && distance > 0.0001f)
                    force += diff.normalized / distance;
            }

            return force.magnitude > 0f ? force.normalized : Vector3.zero;
        }

        private Vector3 CalculatePlayerSeparation()
        {
            if (_playerTransform == null) return Vector3.zero;

            Vector3 diff     = _ownerTransform.position - _playerTransform.position;
            float   distance = diff.magnitude;

            if (distance >= _playerSeparationRadius || distance < 0.0001f)
                return Vector3.zero;

            return diff.normalized / distance;
        }

        private Vector3 CalculateTargetSeek(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - _ownerTransform.position;
            float   distance = toTarget.magnitude;
            if (distance < 0.001f) return Vector3.zero;

            // 슬롯 방향으로 정규화된 벡터 반환 (크기는 TickWithNeighbors에서 스케일)
            return toTarget.normalized;
        }

        // ── Rotation ─────────────────────────────────────────────────────────

        private void ApplyAdaptiveRotation(MonsterUnit owner, float currentSpeed)
        {
            // 이동 중이면 이동 방향으로, 정지 시 플레이어 방향으로 부드럽게 회전
            if (currentSpeed > 0.1f)
            {
                // Y 성분 제거: XZ 평면 방향으로만 LookRotation 계산
                Vector3 flatVel = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
                if (flatVel.sqrMagnitude < 0.0001f) return;
                Quaternion targetRot = Quaternion.LookRotation(flatVel, Vector3.up);
                owner.transform.rotation = Quaternion.RotateTowards(
                    owner.transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }
            else if (_playerTransform != null)
            {
                // Y축(Yaw)만 추출하여 피치/롤이 전이되지 않도록 함
                Quaternion yawOnly = Quaternion.Euler(0f, _playerTransform.eulerAngles.y, 0f);
                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation, yawOnly, 5f * Time.deltaTime);
            }
        }
    }
}
