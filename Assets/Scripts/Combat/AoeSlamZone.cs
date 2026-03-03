using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Pooled ground-slam warning zone. Spawned by BossA.
    ///
    /// Lifecycle (managed entirely by this component):
    ///   1. BossA calls Init() → zone activates, flat red cylinder appears at position.
    ///   2. Update counts down _warningDuration seconds.
    ///   3. Detonate() fires CombatSystem.DealAoeDamage on the Ally team.
    ///   4. GameObject is deactivated — BossA's pool picks it up next time.
    ///
    /// No reference back to BossA is needed; deactivation returns it to the pool
    /// (BossA's GetAvailableZone searches for !activeInHierarchy objects).
    ///
    /// Visual: a flat cylinder primitive (WarningCylinderHeight thick) with an
    /// opaque red material, scaled to diameter = radius × 2.
    ///
    /// Inspector setup: none — fully configured at runtime via Init().
    /// </summary>
    public class AoeSlamZone : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────────

        private const float WarningCylinderHeight = 0.02f;

        /// <summary>Lifts zone just above Y=0 ground to prevent Z-fighting.</summary>
        private const float GroundOffset = 0.02f;

        // ── Private runtime ──────────────────────────────────────────────────

        private Renderer _warningRenderer;
        private float    _timer;
        private float    _radius;
        private float    _damage;
        private float    _warningDuration;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            // Create a flat cylinder as the warning indicator.
            // Cylinder is a child so it moves with this transform.
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localRotation = Quaternion.identity;

            // Remove the auto-added collider — collision is handled by DealAoeDamage.
            Destroy(cylinder.GetComponent<CapsuleCollider>());

            // Set up a simple opaque red material (Built-in RP Standard shader).
            _warningRenderer = cylinder.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            _warningRenderer.material = mat;

            // Start inactive; BossA activates via Init().
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                Detonate();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Configures and activates this zone. Called by BossA.SpawnSlamZone().
        /// </summary>
        /// <param name="position">World position (player's feet at spawn time).</param>
        /// <param name="radius">Explosion radius in world units.</param>
        /// <param name="damage">Damage dealt to every Ally within radius on detonation.</param>
        /// <param name="warningDuration">Seconds the red circle is visible before exploding.</param>
        public void Init(Vector3 position, float radius, float damage, float warningDuration)
        {
            transform.position = new Vector3(position.x, position.y + GroundOffset, position.z);

            _radius          = radius;
            _damage          = damage;
            _warningDuration = warningDuration;
            _timer           = warningDuration;

            // Scale the flat cylinder: diameter = radius × 2, height = constant thin value.
            float diameter = radius * 2f;
            _warningRenderer.transform.localScale =
                new Vector3(diameter, WarningCylinderHeight, diameter);

            gameObject.SetActive(true);
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Deals AoE damage to all living Ally combatants within <see cref="_radius"/>
        /// centred on this zone's position, then returns this zone to the pool by
        /// deactivating it.
        /// </summary>
        private void Detonate()
        {
            CombatSystem.Instance?.DealAoeDamage(
                transform.position,
                _radius,
                _damage,
                CombatTeam.Ally
            );

            // Deactivating here returns this object to BossA's pool automatically;
            // GetAvailableZone() searches for !activeInHierarchy entries.
            gameObject.SetActive(false);
        }
    }
}
