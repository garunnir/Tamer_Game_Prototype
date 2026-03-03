# Day 2 — Claude Code Prompts
# Combat & Monster Systems

> Windows: Terminal 1(Combat), Terminal 2(Monsters), Terminal 3(Review), Terminal 4(Data), Terminal 5(Support)
> Always start with "Read CLAUDE.md first"

---

## [Terminal 4] STEP 0. MonsterData ScriptableObject

```
Read CLAUDE.md first.

Create a ScriptableObject to hold monster data.
File: MonsterData.cs
Location: Assets/Scripts/Data/
Namespace: WildTamer

Fields:
- Monster name
- HP
- Attack damage
- Move speed
- Attack range
- Detection range
- Taming chance (0~1)
- No type Enum — MonsterData asset itself is the type identifier, use asset name as ID

Also create 5 MonsterData asset files, one per type.
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 1] STEP 1. CombatSystem

```
Read CLAUDE.md first.

Implement the auto-combat system.
File: CombatSystem.cs
Location: Assets/Scripts/Combat/
Namespace: WildTamer

Requirements:
- Flock units auto-target the nearest enemy within range
- Auto-attack toward target (with cooldown)
- Enemies detect flock units and auto-attack back
- On HP <= 0, trigger death (Dead state)
- Range detection checked every 0.2s, not every frame
- Structured so both FlockUnit and MonsterBase can reference it
- Projectile management via object pooling
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 2] STEP 2. MonsterBase

```
Read CLAUDE.md first.

Create the base class for all monsters.
File: MonsterBase.cs
Location: Assets/Scripts/Monster/
Namespace: WildTamer

Requirements:
- References MonsterData ScriptableObject
- State machine: Idle → Patrol → Chase → Attack → Dead
- Each state as virtual method — overridable by child classes
- HP management, damage handling, death handling
- Player/flock detection logic (based on detection range)
- On death, send event to TamingSystem
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 2] STEP 3. Normal Monsters x3

```
Read CLAUDE.md first.

Implement MonsterA.

Monster info:
- Name: MonsterA
- Movement: patrol in fixed radius, straight-line chase on detection
- Attack range: melee 1.5f
- Attack type: melee hit
- Speed: normal 4f
- Special: none
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

```
Read CLAUDE.md first.

Implement MonsterB.

Monster info:
- Name: MonsterB
- Movement: zigzag approach
- Attack range: mid-range 4f (projectile)
- Attack type: projectile
- Speed: fast 7f
- Special: retreats after attacking
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

```
Read CLAUDE.md first.

Implement MonsterC.

Monster info:
- Name: MonsterC
- Movement: slow approach, stops at fixed distance
- Attack range: long-range 8f (AoE explosion)
- Attack type: ground explosion under player
- Speed: slow 2f
- Special: move speed doubles below 30% HP
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 5] STEP 4. Boss Monsters x2

```
Read CLAUDE.md first.

Implement BossA.

Boss info:
- Name: BossA
- Pattern: AoeSlam (ground slam)
- Trigger: every 8 seconds
- Description: place 3 ground slam zones sequentially at player position, explode after 1.5s
- Warning: red circle shown for 1.5s before explosion
- Damage: 25
- Also has normal melee attack (cooldown 2s)
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

```
Read CLAUDE.md first.

Implement BossB.

Boss info:
- Name: BossB
- Pattern: Charge
- Trigger: below 70% HP, every 6s cooldown
- Description: high-speed charge toward player, stops after max 2s or 10f distance
- Warning: vibration effect for 0.8s before charging
- Damage: 30
- Also has normal melee attack (cooldown 2s)
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 1] STEP 5. Hit Feedback

```
Read CLAUDE.md first.

Implement hit feedback effects.

Effect info:
- Name: HitEffect
- Trigger: immediately after damage is dealt in CombatSystem
- Effects:
  - Hitlag: stop hit object motion for 0.08s
  - Hitstop: freeze both attacker and target for 0.05s (NO Time.timeScale — affect objects only)
  - Camera shake: call QuarterViewCamera.Shake()
  - Color flash: hit object flashes red for 0.1s
  - Particle: spark effect at hit position (object pooling)
- Manage centrally via EffectManager.cs singleton
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 3] STEP 6. Day 2 Full Review

```
Read CLAUDE.md first.
You are a reviewer. Do NOT modify code — only find bugs.

Review these files in order:
- CombatSystem.cs
- MonsterBase.cs
- MonsterA.cs / MonsterB.cs / MonsterC.cs
- BossA.cs / BossB.cs
- HitEffect.cs / EffectManager.cs

For each file check:
1. Possible compile errors
2. Possible NullReferenceException
3. NavMesh usage (BANNED)
4. Public fields (BANNED)
5. Magic numbers (BANNED)
6. Physics calls every frame in Update
7. Missing state machine transitions
8. Logical bugs

Report format: filename / line / issue / fix suggestion
If no issues: output "OK"
```

---

## Day 2 Completion Checklist

- [ ] MonsterData.cs + 5 assets
- [ ] CombatSystem.cs
- [ ] MonsterBase.cs
- [ ] MonsterA.cs
- [ ] MonsterB.cs
- [ ] MonsterC.cs
- [ ] BossA.cs (AoE slam)
- [ ] BossB.cs (charge)
- [ ] EffectManager.cs
- [ ] HitEffect (hitlag, hitstop, camera shake, color flash, particle)
- [ ] Full review passed
- [ ] git commit
- [ ] CLAUDE.md checklist updated