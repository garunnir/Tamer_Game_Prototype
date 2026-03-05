using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Pooled ground-slam warning zone. Spawned by MeleeAndSlamAttackLogic.
    ///
    /// Lifecycle (managed entirely by this component):
    ///   1. Init() → zone activates, a pulsing ring appears at the position.
    ///   2. Update counts down _warningDuration seconds; ring blinks faster as time runs out.
    ///   3. Detonate() fires CombatSystem.DealAoeDamage on the target team.
    ///   4. GameObject is deactivated — the pool picks it up next time.
    ///
    /// Visual: LineRenderer circle ring (faction-coloured: green = ally, red = enemy).
    /// Inspector setup: none — fully configured at runtime via Init().
    /// </summary>
    public class AoeSlamZone : MonoBehaviour
    {
        private const float GroundOffset  = 0.02f;
        private const int   RingSegments  = 40;
        private const float RingWidth     = 0.16f;

        private LineRenderer _ring;
        private float        _timer;
        private float        _radius;
        private float        _damage;
        private float        _warningDuration;
        private Color        _baseColor;

        private void Awake()
        {
            _ring                   = gameObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace     = false;
            _ring.loop              = true;
            _ring.widthMultiplier   = RingWidth;
            _ring.positionCount     = RingSegments;
            _ring.material          = new Material(Shader.Find("Sprites/Default"));
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows    = false;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;

            // Blink faster as the explosion approaches.
            float t          = Mathf.Clamp01(_timer / _warningDuration);
            float blinkSpeed = Mathf.Lerp(10f, 2f, t);
            float alpha      = Mathf.Lerp(0.3f, 1f, Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed)));
            var   c          = _baseColor;
            c.a              = alpha;
            _ring.startColor = c;
            _ring.endColor   = c;

            if (_timer <= 0f)
                Detonate();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Configures and activates this zone.
        /// </summary>
        /// <param name="position">World position of the zone centre.</param>
        /// <param name="radius">Explosion radius in world units.</param>
        /// <param name="damage">Damage dealt on detonation.</param>
        /// <param name="warningDuration">Seconds the ring is visible before exploding.</param>
        /// <param name="ringColor">Faction colour — green for ally, red for enemy.</param>
        public void Init(Vector3 position, float radius, float damage, float warningDuration, Color ringColor)
        {
            transform.position = new Vector3(position.x, position.y + GroundOffset, position.z);

            _radius          = radius;
            _damage          = damage;
            _warningDuration = warningDuration;
            _timer           = warningDuration;
            _baseColor       = ringColor;

            BuildRingPoints(radius);
            gameObject.SetActive(true);
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        private void BuildRingPoints(float radius)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void Detonate()
        {
            CombatSystem.Instance?.DealAoeDamage(
                transform.position,
                _radius,
                _damage,
                FactionId.Player
            );
            gameObject.SetActive(false);
        }
    }
}
