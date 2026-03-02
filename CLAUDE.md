# Wild Tamer Prototype — CLAUDE.md

## ⚠️ Rules for Claude Code — MUST FOLLOW

### Work Order (strictly in this order)
1. Read this entire file before doing anything
2. Do NOT write code immediately — **plan first**
   - Which files will be created
   - In what order
   - What problems are expected
   - Output the plan and wait for approval before proceeding
3. Write code
4. **Self-verify** — confirm the code works correctly
   - No compile errors
   - No logical bugs
   - No violations of CLAUDE.md rules (no NavMesh, no public fields, etc.)
   - If issues found, fix and re-verify before finishing
5. After completion, update the 🧠 Memory section
6. If changing existing code structure, always record the reason in Memory
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
- Classes: `PascalCase` (e.g. `FlockManager`)
- Private fields: `_camelCase` (e.g. `_moveSpeed`) — applies to ALL private fields including SerializeField
- Public properties: `PascalCase` (e.g. `MoveSpeed`)
- Constants: `PascalCase` (e.g. `MaxHealth`, `SeparationRadius`)
- Methods: `PascalCase` (e.g. `CalculateSeparation()`)

## Folder Structure
```
Assets/
├── Scripts/
│   ├── Player/
│   ├── Flock/          # Flocking system
│   ├── Monster/        # 3 normal + 2 boss monsters
│   ├── Combat/         # Combat, hit feedback
│   ├── Taming/         # Taming system
│   ├── Map/            # Fog of War, minimap
│   ├── UI/             # Bestiary, HUD
│   └── Data/           # ScriptableObject data
├── Prefabs/
├── ScriptableObjects/
├── Scenes/
└── Resources/
```

## Core System Architecture

### Flock System
- FlockManager.cs: manages all flock units, holds unit list
- FlockUnit.cs: individual unit movement (Separation / Alignment / Cohesion)
- FormationHelper.cs: formation offset calculation
- Player position used as target point
- Includes overlap prevention between units

### Monsters
- MonsterBase.cs: shared base class (HP, attack, state machine)
- 3 normal monsters: MonsterA / MonsterB / MonsterC (different movement patterns)
- 2 bosses: BossA (AoE ground slam) / BossB (charge)
- States: Idle → Patrol → Chase → Attack → Dead

### Combat
- CombatSystem.cs: range detection, auto-combat
- HitEffect.cs: hitstop, hitlag, camera shake, hit flash

### Taming
- TamingSystem.cs: probability check on kill, adds unit to flock
- Visual feedback on success (particles + UI)

### Map
- FogOfWar.cs: texture masking, vision range update
- MinimapController.cs: separate camera + RenderTexture

## Implementation Checklist
- [ ] Project setup / quarter-view camera
- [ ] Player movement
- [ ] FlockUnit.cs
- [ ] FlockManager.cs
- [ ] FormationHelper.cs
- [ ] Auto-combat system
- [ ] Normal monster A
- [ ] Normal monster B
- [ ] Normal monster C
- [ ] Boss A (AoE slam)
- [ ] Boss B (charge)
- [ ] Hit feedback (hitlag, hitstop, camera shake)
- [ ] Taming system
- [ ] Fog of War
- [ ] Minimap
- [ ] Bestiary UI (bonus)
- [ ] Data persistence save/load (bonus)

## Architecture
- Unity component-based
- Data-Driven Design using ScriptableObjects for all tunable values
- Manager singletons for global systems
- Do NOT use external packages (UniTask, DOTween, etc.) unless already installed
- Prefer Unity built-in solutions only

## Warnings
- Check Unity 6 Physics API changes before using Physics calls
- If unsure about Unity 6 API, prefer Unity 2022 LTS compatible syntax
- Never call OverlapSphere or Raycast every frame — throttle with InvokeRepeating or coroutine
- For large numbers of monsters, consider distance-based LOD

---

## Memory (Claude Code updates this directly)

### Last Session
- (Claude Code records after each session)

### Completed Work Summary
- (Add one-line summary per completed task)

### Implementation Decisions
- (Why things were implemented a certain way)
- Example: "FlockUnit uses no Rigidbody — direct Transform manipulation performs better with many units"

### Known Issues / Incomplete
- (Bugs, temporary workarounds, things to fix later)

### Next Session Notes
- (What Claude Code needs to know immediately at the start of a new session)
