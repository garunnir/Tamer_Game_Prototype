# Decision: Strategy Pattern Refactoring

## Status: COMPLETE

## FactionSystem — Static class, not Singleton MB
- FactionSystem is a static utility. No scene object needed.
- Team re-registration on taming handled directly in MonsterUnit.SetFaction().
- Use `FactionSystem.AreHostile(a, b)` — never compare teamID directly.

## SO Cloning per Unit
- MonsterUnit always calls `Instantiate(source)` on MovementLogic/AttackLogic SOs in Awake().
- Reason: SOs are shared assets; mutable runtime state (zigzag timer, charge phase) must be per-unit.

## Follow State (MonsterState.Follow)
- 6th state for tamed units. FlockManager drives movement via FlockMoveLogic.TickWithNeighbors().
- MonsterUnit.UpdateFollow() only ticks AttackLogic; return values are ignored (unit never leaves Follow via attack result).

## AttackLogic.Tick(owner, target, inAttackRange)
- MonsterUnit.UpdateAttack() precomputes inRange and passes it in — avoids redundant distance checks.
- Exception: MeleeAndChargeAttackLogic ignores inRange while charge phase is active.

## OnEnterAttackState resets sequence state
- MeleeAndSlamAttackLogic: resets _isSlamming.
- MeleeAndChargeAttackLogic: resets _chargePhase, restores windup position.
- Reason: target can die mid-sequence; MonsterUnit calls EnterIdle() directly, bypassing AttackLogic cleanup.

## Implemented Strategy Map
### MovementLogic → Scripts/Data/Movement/
| Class                    | Used By              |
|--------------------------|----------------------|
| DirectChaseMoveLogic     | MonsterA, BossA, BossB |
| ZigzagRetreatMoveLogic   | MonsterB             |
| EnrageChaseMoveLogic     | MonsterC             |
| FlockMoveLogic           | Tamed units          |

### AttackLogic → Scripts/Data/Attack/
| Class                    | Used By              | Notes |
|--------------------------|----------------------|-------|
| MeleeAttackLogic         | MonsterA             | |
| ProjectileAttackLogic    | MonsterB, Tamed      | _retreatAfterFiring=true for B, _ignoreRangeCheck=true for tamed |
| AoeExplosionAttackLogic  | MonsterC             | |
| MeleeAndSlamAttackLogic  | BossA                | Zone pool allocated in Initialize() |
| MeleeAndChargeAttackLogic| BossB                | Charge moves owner via ApplyDirectVelocity() |

## Taming Flow (MonsterUnit.SetFaction)
1. CombatSystem.UnregisterCombatant(this)
2. _factionId = FactionId.Player
3. CombatSystem.RegisterCombatant(this)  ← now registers as Ally
4. _movementLogic = Instantiate(_flockMovementLogic)
5. FlockManager.AddUnit(this)
6. EnterFollow()

## Inspector Setup Required (Post-Refactoring)
- Create SO assets under ScriptableObjects/Movement/ and ScriptableObjects/Attack/
- Assign DefaultMovementLogic + DefaultAttackLogic in each MonsterData asset
- Assign _flockMovementLogic (FlockMove.asset) on each MonsterUnit prefab
- Update FlockManager._initialUnits list (type changed from FlockUnit to MonsterUnit)
