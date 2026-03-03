using UnityEngine;

namespace WildTamer
{
    public enum MoveTickResult { Continue, EnterAttack }

    /// <summary>
    /// Abstract ScriptableObject base for all movement strategies.
    /// Each MonsterUnit Instantiates a clone at runtime so instances share
    /// configuration but maintain independent mutable state.
    ///
    /// Implementors: DirectChaseMoveLogic, ZigzagRetreatMoveLogic,
    ///               EnrageChaseMoveLogic, FlockMoveLogic.
    /// </summary>
    public abstract class MovementLogic : ScriptableObject
    {
        /// <summary>Called once after the SO clone is created. Cache references here.</summary>
        public virtual void Initialize(MonsterUnit owner) { }

        /// <summary>
        /// Called every frame during Chase state.
        /// Returns EnterAttack when the unit is close enough to engage.
        /// </summary>
        public abstract MoveTickResult Tick(MonsterUnit owner, ICombatant target);

        /// <summary>
        /// Called by MonsterUnit when an attack fires.
        /// Used by ZigzagRetreatMoveLogic to trigger the retreat phase.
        /// </summary>
        public virtual void OnAttackFired(MonsterUnit owner) { }
    }
}
