# Architecture Decisions

## Camera
- `_basePosition` tracks unshaken pos → no cumulative drift from Lerp
- Update-loop timer, linear decay — no coroutine (per CLAUDE.md)
- Shake accumulates via `Mathf.Max` — consecutive hits don't reset
- Offset uses `transform.right/up` (camera-local, not world XY) — correct at 60° pitch
- `LateUpdate` — moves after player Transform updates

## Player
- `Rigidbody.linearVelocity` (Unity 6 API); preserves Y for gravity
- `FreezeRotation` in `Awake`; rotation via `RotateTowards`
- `Animator.StringToHash` cached as `static readonly` — no per-frame string alloc

## Effects (EffectManager)
- Singleton, scene-local; `TriggerHitEffect(victim, hitPos, attacker?)` called from `TakeDamage`
- `TriggerHitstop(attacker)` called from melee `PerformAttack` (ranged skipped — no attacker ref)
- `IHitReactive` in `Scripts/Combat/`; implemented by `MonsterUnit` and `FlockUnit`
- Spark pool: `ParticleSystem.stopAction = Disable` — auto-recycles, no Update loop
- Color flash: `MaterialPropertyBlock` (never modifies shared material); coroutine on EffectManager GO — survives victim death

## Strategy Pattern
- **FactionSystem**: static class — always use `AreHostile(a, b)`, never compare `teamID` directly
- **SO cloning**: `Instantiate(source)` in `Awake()` for `MovementLogic`/`AttackLogic` — each unit needs independent runtime state
- **Follow state**: `FlockManager` drives via `FlockMoveLogic.TickWithNeighbors()`; `MonsterUnit.IsFollowing` exposes state; `MonsterUnit` has no knowledge of `FlockMoveLogic`
- `OnTargetAssigned`: Follow → Chase when target assigned; Chase/Attack → `EnterIdleOrFollow` when target lost
- `AttackLogic.Tick(owner, target, inRange)`: `inRange` precomputed in `UpdateAttack()` — no redundant distance checks
- `OnEnterAttackState` must reset sequence state (`_isSlamming`, `_chargePhase`, windup pos) — target can die mid-sequence

### Taming Flow (`MonsterUnit.SetFaction`)
1. `UnregisterCombatant(this)` → 2. `_factionId = Player` → 3. `RegisterCombatant(this)` → 4. `FlockManager.AddUnit(this)` → 5. `EnterFollow()`

### Strategy Map
| MovementLogic | Used By |
|---|---|
| DirectChaseMoveLogic | MonsterA, BossA, BossB |
| ZigzagRetreatMoveLogic | MonsterB |
| EnrageChaseMoveLogic | MonsterC |
| FlockMoveLogic | Tamed units |

| AttackLogic | Used By | Notes |
|---|---|---|
| MeleeAttackLogic | MonsterA | |
| ProjectileAttackLogic | MonsterB, Tamed | `_retreatAfterFiring=true` for B; `_ignoreRangeCheck=true` for tamed |
| AoeExplosionAttackLogic | MonsterC | |
| MeleeAndSlamAttackLogic | BossA | Zone pool allocated in `Initialize()` |
| MeleeAndChargeAttackLogic | BossB | Charge moves owner via `ApplyDirectVelocity()` |

## Flock Initial Units
- `_initialUnits` = monsters that start the scene as full Player flock members (behavior-equivalent to pre-tamed)
- `FlockManager` has `[DefaultExecutionOrder(1)]`; `InitializeFormation()` runs in `Start()` (not `Awake()`)
- `Start()` calls `SetFaction(Player)` per unit → full taming flow; no manual list-push
- **Constraint**: units in `_initialUnits` must have `factionId = Enemy` in inspector — `SetFaction` early-returns if already Player
