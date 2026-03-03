# 📑 Implementation Checklist (Refactoring)

- [x] **Step 1: Analysis & Design** - Analyzed existing Flock/Monster logic. Finalized Strategy Pattern architecture. FactionSystem merged as static utility (not Singleton MB).
- [x] **Step 2: Foundation** - MovementLogic.cs, AttackLogic.cs (abstract SOs), FactionSystem.cs (static), MonsterData updated.
- [x] **Step 3: Strategy Extraction** - 4 MovementLogics + 5 AttackLogics ported from MonsterA/B/C, BossA/B, FlockUnit.
- [x] **Step 4: Unification** - MonsterUnit.cs created. Legacy scripts (MonsterBase, MonsterA/B/C, BossA/B, FlockUnit, FlockUnitCombat) deleted.
- [x] **Step 5: System Integration** - FactionSystem integrated into MonsterUnit.SetFaction(). FlockManager updated to MonsterUnit type.

## Next Steps
- [ ] Create ScriptableObject assets in Inspector (DirectChaseMove.asset, etc.)
- [ ] Reassign MonsterUnit prefabs in scene (remove old components, add MonsterUnit)
- [ ] Assign MonsterData DefaultMovementLogic/DefaultAttackLogic fields in Inspector
- [ ] Assign _flockMovementLogic (FlockMove.asset) on each MonsterUnit prefab for taming
- [ ] Test in Play mode: enemy combat, taming, flock following
