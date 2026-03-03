using UnityEngine;

namespace WildTamer
{
    public enum AttackTickResult { Continue, EnterChase, EnterIdle }

    /// <summary>
    /// Abstract ScriptableObject base for all attack strategies.
    /// Each MonsterUnit Instantiates a clone at runtime so instances share
    /// configuration but maintain independent mutable state.
    ///
    /// Implementors: MeleeAttackLogic, ProjectileAttackLogic,
    ///               AoeExplosionAttackLogic, MeleeAndSlamAttackLogic,
    ///               MeleeAndChargeAttackLogic.
    /// </summary>
    public abstract class AttackLogic : ScriptableObject
    {
        /// <summary>Called once after the SO clone is created. Use to allocate pools.</summary>
        public virtual void Initialize(MonsterUnit owner) { }

        /// <summary>Called each time the unit enters Attack state. Reset timers here.</summary>
        public virtual void OnEnterAttackState(MonsterUnit owner) { }

        /// <summary>
        /// Called every frame during Attack state.
        /// <paramref name="inAttackRange"/> is precomputed by MonsterUnit from _data.AttackRange.
        /// Returns a transition hint; MonsterUnit executes the state change.
        /// </summary>
        public abstract AttackTickResult Tick(MonsterUnit owner, ICombatant target, bool inAttackRange);
    }
}
