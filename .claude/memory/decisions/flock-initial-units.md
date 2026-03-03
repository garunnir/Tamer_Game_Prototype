## Flock initial units behavior

### Context

- `FlockManager` has an inspector field `_initialUnits` (`List<MonsterUnit>`).
- `FlockManager.InitializeFormation()` currently:
  - Adds `_initialUnits` directly into its internal `_units` / `_flockLogics` lists.
  - Recalculates formation and repositions them.
- However, those units:
  - Do not go through the normal taming flow (`SetFaction(FactionId.Player) → BecomeFlockUnit()`).
  - Do not enter `MonsterState.Follow` (`IsFollowing == false`).
  - Therefore are skipped by `FlockManager.Update()` and keep using their default `MovementLogic` from `MonsterData` instead of `FlockMoveLogic`.

Result: `_initialUnits` affects only initial placement and internal lists, but not actual flock movement. From a gameplay perspective this makes the field almost meaningless.

### Decision

`_initialUnits` represents monsters that should start the scene as **full flock members**, not just as pre-placed enemies.

For any `MonsterUnit` referenced in `_initialUnits`:

- It should start the scene as a **Player-aligned ally**.
- It should be **registered with `FlockManager`** as a flock member.
- It should be in **`MonsterState.Follow`**, so that:
  - `MonsterUnit.IsFollowing == true`.
  - `FlockManager.Update()` calls `FlockMoveLogic.TickWithNeighbors()` for that unit every frame.
  - Its movement is driven by flock movement, not by its default `MovementLogic.Tick()`.

In other words: being in `_initialUnits` must be equivalent (from behavior’s point of view) to having been tamed via `SetFaction(FactionId.Player)` before the first frame, only with formation/position initialized around the player.

### Implementation (COMPLETE)

**Changed file:** `Assets/Scripts/Flock/FlockManager.cs` only. `MonsterUnit.cs` unchanged.

**Key changes:**
1. Added `[DefaultExecutionOrder(1)]` to `FlockManager` — guarantees all `MonsterUnit.Start()` (order 0) run before `FlockManager.Start()`.
2. Moved `InitializeFormation()` call from `Awake()` to a new `Start()`.
3. Rewrote `InitializeFormation()`:
   - Calls `unit.SetFaction(FactionId.Player)` for each non-null `_initialUnit` (canonical taming flow: Unregister→faction change→Reregister→`BecomeFlockUnit()`→`AddUnit()`+`EnterFollow()`).
   - After the loop, recalculates formation and repositions all `_units` around the player.

**Why the timing works:**
- `Awake()` order: FlockManager singleton + sqrRadius set; MonsterUnit SOs cloned.
- `MonsterUnit.Start()` (order 0): RegisterCombatant(Enemy) + EnterIdle().
- `FlockManager.Start()` (order 1): SetFaction(Player) → UnregisterEnemy → RegisterAlly → BecomeFlockUnit → AddUnit + EnterFollow. Then final reposition.

**No double-registration**: Mono.Start registers as Enemy; SetFaction unregisters then re-registers as Ally. One active registration at all times.
**No state override**: EnterFollow() is called after (not before) MonsterUnit's EnterIdle().
**No double-adding**: `_units.Add` only happens inside `AddUnit()`. The old manual push in `InitializeFormation()` is gone.
**Constraint**: Units in `_initialUnits` should have `_factionId == Enemy` in the inspector (the default). If already Player, `SetFaction` early-returns and the unit won't be auto-added — that edge case is not supported.

