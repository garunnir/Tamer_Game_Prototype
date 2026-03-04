# 🚀 Next Session Summary

## Current Status
**Strategy Pattern refactoring is COMPLETE (code only).**

Scripts written:
- `Scripts/Combat/FactionSystem.cs` — static utility (AreHostile, ToCombatTeam)
- `Scripts/Data/MovementLogic.cs` + `AttackLogic.cs` — abstract SO bases
- `Scripts/Data/MonsterData.cs` — added DefaultMovementLogic/DefaultAttackLogic fields
- `Scripts/Monster/MonsterUnit.cs` — unified class (replaces all old Monster/FlockUnit scripts)
- `Scripts/Data/Movement/` — DirectChase, ZigzagRetreat, EnrageChase, FlockMove
- `Scripts/Data/Attack/` — Melee, Projectile, AoeExplosion, MeleeAndSlam, MeleeAndCharge
- `Scripts/Flock/FlockManager.cs` — updated FlockUnit→MonsterUnit

Deleted: MonsterBase, MonsterA/B/C, BossA/B, FlockUnit, FlockUnitCombat

## Key Architecture Facts
- SO clones: MonsterUnit Instantiates SO copies in Awake() — each unit has independent runtime state
- MonsterState.Follow: tamed units stay here; FlockManager drives movement, Update() only ticks attack
- SetFaction(FactionId.Player): re-registers with CombatSystem as Ally + BecomeFlockUnit()
- OnEnterAttackState() resets charge/slam state to handle interrupted engagements

## Up Next (Inspector Setup Required)
1. Create SO assets under `ScriptableObjects/Movement/` and `ScriptableObjects/Attack/`
2. Assign DefaultMovementLogic/DefaultAttackLogic in each MonsterData asset
3. Add MonsterUnit component to enemy prefabs; assign _data and _flockMovementLogic
4. Update FlockManager Inspector: _initialUnits list type changed to MonsterUnit
5. Verify in Play mode

## Step 5 Complete — Minimap UI Shader
- `Assets/Shaders/MinimapFog.shader` (WildTamer/MinimapFog): URP Unlit UI shader
  - `_BackgroundTex`: terrain/world map image
  - `_FogMaskTex`: FoW RenderTexture (R=1 hidden, R=0 revealed)
  - smoothstep soft edges via `_EdgeLow` / `_EdgeHigh` (default 0.45 / 0.55)
  - Supports Unity UI stencil masking and vertex colour tinting
- `manual_setting.md`: full UI hierarchy setup guide (5-A through 5-G)
- Optional binder script in guide: `MinimapFogUIBinder.cs` → `Assets/Scripts/Minimap/`
- Next: Step 7 — Procedural Feedback (MaterialPropertyBlock squash/stretch, hit flash, taming absorption coroutine)

## Step 6 Complete — Minimap Unit Tracking
- `Assets/Scripts/Minimap/MinimapUnitTracker.cs` — singleton, InvokeRepeating at 0.1 s
  - Blue icons: Player/Ally — always visible
  - Red icons: Enemy — visible only when FoW cache R > _revealThreshold (0.05)
  - AsyncGPUReadback polls FowController.FowRT every 0.2 s → Color32[] _fowCache
  - Enemy visibility: `_fowCache[py * width + px].r / 255f > _revealThreshold`
  - Icon pool: Queue<RectTransform> per type; SpawnIcon sets anchorMin/Max/pivot to (0.5,0.5)
  - PlaceIcon: `anchoredPosition = (uv - 0.5) * panelSize`
  - Faction swap detected per-tick in UpdateIconPositions (no extra event needed)
- `MonsterUnit.cs` changes:
  - `Start()`: `MinimapUnitTracker.Instance?.Register(this);`
  - `OnDisable()`: `MinimapUnitTracker.Instance?.Unregister(this);`
- `manual_setting.md` Step 6: prefab setup (8×8 Image, circle sprite, blue/red), scene wiring, layer order, threshold tuning, smoke-test
