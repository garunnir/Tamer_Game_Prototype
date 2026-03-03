# Wild Tamer Prototype — CLAUDE.md

## ⚠️ Rules for Claude Code — MUST FOLLOW

### Work Order (strictly in this order)
1. Read this entire file before doing anything.
2. Do NOT write code immediately — **plan first**.
   - Output the plan and wait for approval before proceeding.
3. Write code.
4. **Self-verify** — No compile errors, no logical bugs, no violations of these rules.
5. After completion, update `.claude/memory/` files.

---

## Architecture

- **MonsterUnit** is the only monster class. No subclasses. (`Scripts/Monster/MonsterUnit.cs`)
- **Passive Executor:** `MonsterUnit` owns the state machine skeleton and helper methods only. No hardcoded behavior logic inside it.
- **Strategy Pattern:** All behavior is injected via `MovementLogic` and `AttackLogic` ScriptableObjects. Always clone SOs in `Awake()` — `Instantiate(source)` — so each unit has independent runtime state.
- **Flock initial units:** Any unit listed in `FlockManager._initialUnits` must start as a full Player flock member (Follow state, flock movement). See `.claude/memory/decisions/flock-initial-units.md` for details.
- **FactionSystem** is a `static` utility class (no scene object). Use `FactionSystem.AreHostile(a, b)` — never compare `teamID` directly.
- **Taming:** Call `MonsterUnit.SetFaction(FactionId.Player)`. Details → `.claude/memory/decisions/strategy-pattern.md`

## Folder Structure
```
Assets/Scripts/
├── Player/
├── Monster/       ← MonsterUnit.cs only
├── Combat/        ← CombatSystem, FactionSystem, ICombatant, AoeSlamZone, Projectile(Pool)
├── Data/
│   ├── MovementLogic.cs, AttackLogic.cs  (abstract SO bases)
│   ├── MonsterData.cs
│   ├── Movement/  ← MovementLogic implementations
│   └── Attack/    ← AttackLogic implementations
├── Flock/         ← FlockManager, FormationHelper
└── Effects/
```

## Coding Conventions
- Namespace: `WildTamer`
- Single Responsibility — one MonoBehaviour, one role
- `[SerializeField]` only — NO public fields, NO NavMesh
- Private fields: `_camelCase` | Public properties/methods: `PascalCase` | Constants: `PascalCase`
- Prefer Update loop over coroutines (performance)
- Object pooling for monsters, effects, projectiles

## Key Warnings
- Never `OverlapSphere` / `Raycast` every frame — throttle with `InvokeRepeating`
- `CombatSystem` + `ProjectilePool` must each be on a scene GameObject
- Combatants register in `Start()`, not `Awake()` — `CombatSystem` singleton must exist first
- `AttackLogic.OnEnterAttackState()` must reset any sequence state (charge phase, slam flag)
