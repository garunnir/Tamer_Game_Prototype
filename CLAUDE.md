# Wild Tamer Prototype — CLAUDE.md

## ⚠️ Rules for Claude Code — MUST FOLLOW

### Work Order (strictly in this order)
1. Read this entire file before doing anything
2. Do NOT write code immediately — **plan first**
   - Which files will be created
   - In what order
   - What problems are expected
   - Output the plan and wait for approval before proceeding
   - Record predicted problems in .claude/memory/predicted-problems.md
3. Write code
4. **Self-verify** — confirm the code works correctly
   - No compile errors
   - No logical bugs
   - No violations of CLAUDE.md rules (no NavMesh, no public fields, etc.)
   - If issues found, fix and re-verify before finishing
5. After completion, update .claude/memory/ files
6. If changing existing code structure, record reason in .claude/memory/decisions.md
7. When creating new scripts, follow the folder structure rules below

---

## Project Overview
- Genre: Quarter-view RPG prototype (Wild Tamer reference)
- Unity version: 6000.x (Unity 6)
- Render pipeline: Built-in Render Pipeline
- Target: Editor playback (final submission: Windows build)
- NavMesh is BANNED — implement all flocking algorithms in pure code

## Camera
- Fixed quarter-view camera (Y-axis 45deg rotation, X-axis 60deg pitch)
- Follows player with fixed offset

## Coding Conventions
- Namespace: WildTamer
- Single Responsibility Principle — one MonoBehaviour, one role
- Expose via SerializeField, NO public fields
- NO magic numbers — use constants or SerializeField
- Prefer Update loop over coroutines (performance)
- Use object pooling aggressively (monsters, effects, projectiles)

## Naming Conventions
- Classes: PascalCase (e.g. FlockManager)
- Private fields: _camelCase (e.g. _moveSpeed) — applies to ALL private fields including SerializeField
- Public properties: PascalCase (e.g. MoveSpeed)
- Constants: PascalCase (e.g. MaxHealth, SeparationRadius)
- Methods: PascalCase (e.g. CalculateSeparation())

## Folder Structure
```
Assets/
├── Scripts/
│   ├── Player/
│   ├── Flock/
│   ├── Monster/
│   ├── Combat/
│   ├── Taming/
│   ├── Map/
│   ├── UI/
│   └── Data/
├── Prefabs/
├── ScriptableObjects/
├── Scenes/
└── Resources/
```

## Architecture
- Unity component-based
- Data-Driven Design using ScriptableObjects for all tunable values
- No MonsterType enum — MonsterData asset itself is the type identifier (asset name = ID)
- Manager singletons for global systems
- Do NOT use external packages (UniTask, DOTween, etc.) unless already installed
- Prefer Unity built-in solutions only
- If unsure about Unity 6 API, prefer Unity 2022 LTS compatible syntax

## Warnings
- Never call OverlapSphere or Raycast every frame — throttle with InvokeRepeating or coroutine
- For large numbers of monsters, consider distance-based LOD
- CombatSystem + ProjectilePool must each be on a GameObject in the scene
- Combatants register in Start() not Awake() — CombatSystem singleton must exist first

## Memory Files
- Checklist → .claude/memory/checklist.md
- Next session & summary → .claude/memory/next-session.md
- Implementation decisions → .claude/memory/decisions/ (per system)
- Known issues → .claude/memory/issues.md
- Predicted problems → .claude/memory/predicted-problems.md
