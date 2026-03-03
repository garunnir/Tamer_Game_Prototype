using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Moves toward a target ICombatant, applies damage on arrival,
    /// and returns itself to ProjectilePool on hit or lifetime timeout.
    /// Hit detection uses sqrMagnitude — no physics collision.
    /// Speed and lifetime are Inspector-tunable per prefab.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private const float HitRadius    = 0.4f;
        private const float HitRadiusSqr = HitRadius * HitRadius;

        [SerializeField] private float _speed       = 10f;
        [SerializeField] private float _maxLifetime = 5f;

        private float      _damage;
        private ICombatant _target;
        private float      _elapsedTime;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Called by ProjectilePool immediately before the object is activated.</summary>
        public void Init(float damage, ICombatant target)
        {
            _damage      = damage;
            _target      = target;
            _elapsedTime = 0f;
        }

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Update()
        {
            if (_target == null || !_target.IsAlive)
            {
                ReturnToPool();
                return;
            }

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _maxLifetime)
            {
                ReturnToPool();
                return;
            }

            Vector3 toTarget = _target.Transform.position - transform.position;
            float   sqrDist  = toTarget.sqrMagnitude;

            if (sqrDist < HitRadiusSqr)
            {
                _target.TakeDamage(_damage);
                ReturnToPool();
                return;
            }

            transform.position += toTarget.normalized * _speed * Time.deltaTime;

            if (toTarget != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(toTarget);
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void ReturnToPool()
        {
            ProjectilePool.Instance?.Return(this);
        }
    }
}
