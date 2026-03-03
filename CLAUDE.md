# Wild Tamer Prototype — CLAUDE.md

## ⚠️ Rules for Claude Code — MUST FOLLOW

### Work Order (strictly in this order)
1. Read this entire file before doing anything.
2. Do NOT write code immediately — **plan first**.
   - Output the plan and wait for approval before proceeding.
   - Specifically, plan how to migrate `FlockUnit` and `MonsterA, B, C` into the new `MonsterUnit` system.
3. Write code.
4. **Self-verify** — No compile errors, no logical bugs, no violations of these rules.
5. After completion, update .claude/memory/ files.

---

## Architecture (Unified & Strategy-Driven)
- **Single Unit Principle:** Individual scripts (`MonsterA, B, C`) and `FlockUnit` are **DEPRECATED**.
- **MonsterUnit:** All monsters must use the unified `MonsterUnit.cs` class.
- **Passive Agent:** `MonsterUnit` is a passive executor. It must **NOT** contain internal decision-making logic or `if/switch` for behavior states.
- **Strategy Pattern:** All behaviors (Movement, Attack) must be injected via ScriptableObject-based `MovementLogic` and `AttackLogic`.
- **Faction & Alliance:** Use `FactionSystem` (or integrated `FlockManager`) to determine hostile relationships. 
- **Relationship Rule:** **Never** use simple `teamID` comparison; always use `FactionSystem.AreHostile(teamA, teamB)` to account for alliances.
- **Taming Flow:** When a unit's team changes to Player, the system must immediately swap its `MovementLogic` to the `FlockMovement` asset.

## Folder Structure
Assets/
├── Scripts/
│   ├── Player/
│   ├── Monster/      <-- Unified MonsterUnit & Entities
│   ├── Combat/       <-- AttackLogic SOs & CombatSystem
│   ├── Data/         <-- MonsterData & MovementLogic SOs
│   ├── Flock/        <-- Integrated with FactionSystem
│   └── ...

*Note: Moving files between Flock/ and Monster/ is permitted to achieve unit unification.*

## Coding Conventions
- Namespace: WildTamer
- Private fields: _camelCase (applies to ALL private fields including SerializeField)
- Public properties/methods: PascalCase
- NO public fields, NO NavMesh.