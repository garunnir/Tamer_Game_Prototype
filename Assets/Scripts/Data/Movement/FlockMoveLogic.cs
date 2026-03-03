using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Boids flocking movement: Separation + Alignment + Cohesion + Target-seek.
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
        [SerializeField] private float _alignmentWeight  = 1.0f;
        [SerializeField] private float _cohesionWeight   = 1.0f;
        [SerializeField] private float _targetWeight     = 3.0f;

        [Header("Separation")]
        [SerializeField] private float _separationRadius = 1.5f;

        [Header("Movement")]
        [SerializeField] private float _maxSpeed      = 7f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _acceleration  = 8f;

        // ── Runtime (safe after Instantiate per unit) ────────────────────────

        private Vector3 _currentVelocity;

        public override void Initialize(MonsterUnit owner)
        {
            _currentVelocity = Vector3.zero;
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

            Vector3 separation = CalculateSeparation(owner, nearbyUnits);
            Vector3 targetSeek = CalculateTargetSeek(owner, targetPosition);

            // FormationHelper가 정의한 슬롯(targetPosition)을 강하게 따르되,
            // 유닛 간 과도한 겹침만 separation으로 완화한다.
            Vector3 desired = separation * _separationWeight
                            + targetSeek * _targetWeight;

            if (desired.magnitude > _maxSpeed)
                desired = desired.normalized * _maxSpeed;

            _currentVelocity = Vector3.MoveTowards(
                _currentVelocity,
                desired,
                _acceleration * Time.deltaTime
            );

            if (_currentVelocity.magnitude > 0.001f)
                owner.ApplyDirectVelocity(_currentVelocity);
        }

        // ── Force Calculations ───────────────────────────────────────────────

        private Vector3 CalculateSeparation(MonsterUnit owner, List<MonsterUnit> nearbyUnits)
        {
            Vector3 force = Vector3.zero;

            foreach (MonsterUnit neighbor in nearbyUnits)
            {
                Vector3 diff     = owner.transform.position - neighbor.transform.position;
                float   distance = diff.magnitude;

                if (distance < _separationRadius && distance > 0.0001f)
                    force += diff.normalized / distance;
            }

            return force.magnitude > 0f ? force.normalized : Vector3.zero;
        }

        private static Vector3 CalculateAlignment(List<MonsterUnit> nearbyUnits)
        {
            if (nearbyUnits.Count == 0) return Vector3.zero;

            Vector3 sumDirection = Vector3.zero;

            foreach (MonsterUnit neighbor in nearbyUnits)
                sumDirection += neighbor.VelocityDirection;

            return sumDirection.magnitude > 0f ? sumDirection.normalized : Vector3.zero;
        }

        private static Vector3 CalculateCohesion(MonsterUnit owner, List<MonsterUnit> nearbyUnits)
        {
            if (nearbyUnits.Count == 0) return Vector3.zero;

            Vector3 sumPosition = Vector3.zero;

            foreach (MonsterUnit neighbor in nearbyUnits)
                sumPosition += neighbor.transform.position;

            Vector3 averagePosition = sumPosition / nearbyUnits.Count;
            Vector3 toCenter        = averagePosition - owner.transform.position;

            return toCenter.magnitude > 0f ? toCenter.normalized : Vector3.zero;
        }

        private Vector3 CalculateTargetSeek(MonsterUnit owner, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - owner.transform.position;
            float   distance = toTarget.magnitude;
            if (distance < 0.001f) return Vector3.zero;

            // Ramps 0→1 as distance goes 0→_maxSpeed, then caps at 1.5.
            float strength = Mathf.Clamp(distance / _maxSpeed, 0f, 1.5f);
            return toTarget.normalized * strength;
        }
    }
}
