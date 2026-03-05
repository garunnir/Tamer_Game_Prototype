using System.Collections.Generic;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton combat manager. Maintains separate ally/enemy registries and
    /// runs a target scan every 0.2 s via InvokeRepeating (never per-frame).
    /// Combatants register in their Start() and receive OnTargetAssigned() each scan.
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _scanInterval = 0.2f;

        private readonly List<ICombatant> _allies  = new List<ICombatant>();
        private readonly List<ICombatant> _enemies = new List<ICombatant>();

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InvokeRepeating(nameof(ScanTargets), 0f, _scanInterval);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Adds a combatant to the appropriate registry (ally or enemy).
        /// Called by each combatant in its Start().
        /// </summary>
        public void RegisterCombatant(ICombatant combatant)
        {
            if (combatant == null) return;

            if (combatant.Faction == FactionId.Player)
                _allies.Add(combatant);
            else
                _enemies.Add(combatant);
        }

        /// <summary>
        /// Removes a combatant from the registry.
        /// Called by each combatant in OnDisable() or on death.
        /// </summary>
        public void UnregisterCombatant(ICombatant combatant)
        {
            if (combatant == null) return;

            if (combatant.Faction == FactionId.Player)
                _allies.Remove(combatant);
            else
                _enemies.Remove(combatant);
        }

        /// <summary>
        /// Deals <paramref name="damage"/> to every living combatant on
        /// <paramref name="targetTeam"/> within <paramref name="radius"/> of
        /// <paramref name="center"/>. Used by MonsterC's ground explosion.
        ///
        /// Iterates a snapshot of the registry so that TakeDamage-induced deaths
        /// (OnDisable → UnregisterCombatant → List.Remove) do not corrupt the loop.
        /// </summary>
        public void DealAoeDamage(Vector3 center, float radius, float damage, FactionId targetFaction)
        {
            float sqrRadius = radius * radius;

            List<ICombatant> registry = targetFaction == FactionId.Player ? _allies : _enemies;

            // Snapshot: deaths during TakeDamage mutate the registry; iterating
            // the original list mid-removal throws InvalidOperationException.
            var snapshot = new List<ICombatant>(registry);

            foreach (ICombatant c in snapshot)
            {
                if (!c.IsAlive) continue;

                float sqrDist = (c.Transform.position - center).sqrMagnitude;
                if (sqrDist <= sqrRadius)
                    c.TakeDamage(damage);
            }
        }

        // ── Target Scan ──────────────────────────────────────────────────────

        private void ScanTargets()
        {
            foreach (ICombatant ally in _allies)
            {
                if (!ally.IsAlive) continue;
                ally.OnTargetAssigned(GetNearestInRange(ally, _enemies));
            }

            foreach (ICombatant enemy in _enemies)
            {
                if (!enemy.IsAlive) continue;
                enemy.OnTargetAssigned(GetNearestInRange(enemy, _allies));
            }
        }

        /// <summary>
        /// Returns the nearest living candidate within <paramref name="source"/>'s
        /// detection range, or null if none qualifies.
        /// Uses sqrMagnitude to avoid sqrt in the inner loop.
        /// </summary>
        private static ICombatant GetNearestInRange(ICombatant source, List<ICombatant> candidates)
        {
            float maxSqrRange    = source.DetectionRange * source.DetectionRange;
            ICombatant nearest   = null;
            float nearestSqrDist = maxSqrRange; // only accept targets closer than this

            foreach (ICombatant candidate in candidates)
            {
                if (!candidate.IsAlive) continue;

                float sqrDist = (candidate.Transform.position - source.Transform.position).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest        = candidate;
                }
            }

            return nearest;
        }
    }
}
