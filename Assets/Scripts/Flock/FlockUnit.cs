using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Individual flock unit. Computes separation, alignment, cohesion, and
    /// target-seeking forces and moves via direct Transform manipulation.
    /// FlockManager is responsible for neighbor detection and calls UpdateUnit()
    /// each frame with the pre-filtered nearby list.
    /// </summary>
    public class FlockUnit : MonoBehaviour, IHitReactive
    {
        [Header("Flocking Weights")]
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _alignmentWeight  = 1.0f;
        [SerializeField] private float _cohesionWeight   = 1.0f;
        [SerializeField] private float _targetWeight     = 2.0f;

        [Header("Separation")]
        [SerializeField] private float _separationRadius = 1.5f;

        [Header("Movement")]
        [SerializeField] private float _maxSpeed       = 7f;
        [SerializeField] private float _rotationSpeed  = 360f;
        [SerializeField] private float _acceleration   = 8f;

        /// <summary>Normalized heading exposed for neighbor alignment calculations.</summary>
        public Vector3 VelocityDirection { get; private set; }

        private Vector3 _currentVelocity;
        private float   _suspendTimer;

        // ── IHitReactive Implementation ──────────────────────────────────────

        public void SuspendMotion(float duration)
        {
            _suspendTimer = Mathf.Max(_suspendTimer, duration);
        }

        // ── Flock Update ─────────────────────────────────────────────────────

        /// <summary>
        /// Called every frame by FlockManager.
        /// nearbyUnits must NOT include this unit itself.
        /// </summary>
        public void UpdateUnit(List<FlockUnit> nearbyUnits, Vector3 targetPosition)
        {
            if (_suspendTimer > 0f)
            {
                _suspendTimer -= Time.deltaTime;
                return;
            }

            Vector3 separation = CalculateSeparation(nearbyUnits);
            Vector3 alignment  = CalculateAlignment(nearbyUnits);
            Vector3 cohesion   = CalculateCohesion(nearbyUnits);
            Vector3 targetSeek = CalculateTargetSeek(targetPosition);

            Vector3 desired = separation * _separationWeight
                            + alignment  * _alignmentWeight
                            + cohesion   * _cohesionWeight
                            + targetSeek * _targetWeight;

            // Clamp desired to max speed before smoothing
            if (desired.magnitude > _maxSpeed)
                desired = desired.normalized * _maxSpeed;

            // Smooth acceleration toward desired velocity
            _currentVelocity = Vector3.MoveTowards(
                _currentVelocity,
                desired,
                _acceleration * Time.deltaTime
            );

            // Cache normalized heading for neighbors to read
            VelocityDirection = _currentVelocity.magnitude > 0.001f
                ? _currentVelocity.normalized
                : VelocityDirection;

            ApplyMovement();
        }

        // ── Force Calculations ───────────────────────────────────────────────

        private Vector3 CalculateSeparation(List<FlockUnit> nearbyUnits)
        {
            Vector3 force = Vector3.zero;

            foreach (FlockUnit neighbor in nearbyUnits)
            {
                Vector3 diff = transform.position - neighbor.transform.position;
                float distance = diff.magnitude;

                if (distance < _separationRadius && distance > 0.0001f)
                    force += diff.normalized / distance;
            }

            return force.magnitude > 0f ? force.normalized : Vector3.zero;
        }

        private Vector3 CalculateAlignment(List<FlockUnit> nearbyUnits)
        {
            if (nearbyUnits.Count == 0) return Vector3.zero;

            Vector3 sumDirection = Vector3.zero;

            foreach (FlockUnit neighbor in nearbyUnits)
                sumDirection += neighbor.VelocityDirection;

            return sumDirection.magnitude > 0f ? sumDirection.normalized : Vector3.zero;
        }

        private Vector3 CalculateCohesion(List<FlockUnit> nearbyUnits)
        {
            if (nearbyUnits.Count == 0) return Vector3.zero;

            Vector3 sumPosition = Vector3.zero;

            foreach (FlockUnit neighbor in nearbyUnits)
                sumPosition += neighbor.transform.position;

            Vector3 averagePosition = sumPosition / nearbyUnits.Count;
            Vector3 toCenter = averagePosition - transform.position;

            return toCenter.magnitude > 0f ? toCenter.normalized : Vector3.zero;
        }

        private Vector3 CalculateTargetSeek(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - transform.position;
            float distance = toTarget.magnitude;
            if (distance < 0.001f) return Vector3.zero;

            // Scale force proportionally with distance for rubber-band catch-up.
            // Ramps 0→1 as distance goes 0→_maxSpeed, then caps at 1.5 so far
            // units rush without completely overriding separation.
            float strength = Mathf.Clamp(distance / _maxSpeed, 0f, 1.5f);
            return toTarget.normalized * strength;
        }

        // ── Movement Application ─────────────────────────────────────────────

        private void ApplyMovement()
        {
            if (_currentVelocity.magnitude < 0.001f) return;

            transform.position += _currentVelocity * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(_currentVelocity, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }
    }
}
