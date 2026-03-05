using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Flock movement ScriptableObject.
    /// Steering math is now computed in FlockSteeringJob (Burst/Jobs).
    /// This SO holds configuration parameters and applies the computed velocity
    /// and rotation back on the main thread via ApplyVelocityAndRotation().
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

        // ── Parameters exposed for FlockSteeringJob ───────────────────────────

        public float SeparationRadius       => _separationRadius;
        public float PlayerSeparationRadius => _playerSeparationRadius;
        public float SeparationWeight       => _separationWeight;
        public float PlayerSeparationWeight => _playerSeparationWeight;
        public float TargetWeight           => _targetWeight;
        public float MaxSpeed               => _maxSpeed;
        public float SlowingDistance        => _slowingDistance;
        public float Acceleration           => _acceleration;

        // ── Runtime cache (main-thread only) ──────────────────────────────────

        private Rigidbody _rb;
        private Transform _ownerTransform;
        private Transform _playerTransform;

        // ── MovementLogic overrides ────────────────────────────────────────────

        public override void Initialize(MonsterUnit owner)
        {
            _rb              = owner.GetComponent<Rigidbody>();
            _ownerTransform  = owner.transform;
            _playerTransform = Object.FindObjectOfType<PlayerController>()?.transform;
        }

        /// <summary>No-op: steering is driven by FlockManager + FlockSteeringJob.</summary>
        public override MoveTickResult Tick(MonsterUnit owner, ICombatant target)
            => MoveTickResult.Continue;

        // ── Main-thread velocity/rotation application ──────────────────────────

        /// <summary>
        /// Applies a pre-computed velocity (from FlockSteeringJob output) to
        /// the unit's Rigidbody or transform, then updates rotation.
        /// Called by FlockManager on the main thread after job completion.
        /// </summary>
        public void ApplyVelocityAndRotation(MonsterUnit owner, Vector3 velocity)
        {
            float speed = velocity.magnitude;

            if (_rb != null)
                _rb.linearVelocity = velocity;
            else if (speed > 0.001f)
                owner.ApplyDirectVelocity(velocity);

            ApplyAdaptiveRotation(owner, speed, velocity);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplyAdaptiveRotation(MonsterUnit owner, float currentSpeed, Vector3 currentVelocity)
        {
            if (currentSpeed > 0.1f)
            {
                Vector3 flatVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                if (flatVel.sqrMagnitude < 0.0001f) return;

                Quaternion targetRot = Quaternion.LookRotation(flatVel, Vector3.up);
                owner.transform.rotation = Quaternion.RotateTowards(
                    owner.transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }
            else if (_playerTransform != null)
            {
                Quaternion yawOnly = Quaternion.Euler(0f, _playerTransform.eulerAngles.y, 0f);
                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation, yawOnly, 5f * Time.deltaTime);
            }
        }
    }
}
