# Day 4 — Claude Code Prompts
# Polish + Build + Submission

> Terminals: 1(Bug fix+Balance), 2(Save system-bonus), 3(Final review), 4(Build+Submission), 5(Tech spec)
> Always start with "Read CLAUDE.md first"

---

## [Terminal 1] STEP 1. Integration Test & Bug Fix

```
Read CLAUDE.md first.

Help me run a full integration test of all systems.

Test these scenarios in order and fix any issues:
1. Game start → player moves → 5 flock units follow
2. Auto-combat triggers with 3 normal monster types
3. Hit feedback fires (hitlag, hitstop, camera shake)
4. Taming probability check works on monster kill
5. Taming success → unit joins flock + effect plays
6. Boss special patterns trigger (AoE slam, charge)
7. Fog of War vision updates correctly
8. Minimap reflects exploration in real time

Fix any issues found immediately.
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 1] STEP 2. Sound Timing

```
Read CLAUDE.md first.

Wire up sound connections.

Requirements:
- Create AudioManager.cs singleton (if not exists)
- Attack SFX slot — play in CombatSystem on attack
- Hit SFX slot — play in HitEffect on trigger
- Taming success SFX slot — play in TamingSuccessEffect on trigger
- Boss aggro SFX slot — play in BossA and BossB on first detection
- Audio clips can be empty (slots only)
- Confirm timing matches effect triggers exactly
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 2] STEP 3. Data Persistence (bonus)

```
Read CLAUDE.md first.

Implement save/load system for player progress.
File: SaveSystem.cs
Location: Assets/Scripts/Data/
Namespace: WildTamer

Data to save:
- Current flock (list of monster types)
- Explored areas (FogOfWar exploration state)
- Bestiary registration state (BestiaryManager data)

Requirements:
- JSON serialization to local file (Application.persistentDataPath)
- Auto-load on game start
- Auto-save every 30s or on key events (taming success)
- Exception handling for save/load failures
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 3] STEP 4. Final Full Review

```
Read CLAUDE.md first.
You are a reviewer. Do NOT modify code — only find bugs.

Run final review on all files under Assets/Scripts/.

Review criteria:
1. Possible compile errors
2. Possible NullReferenceException
3. Memory leaks (unremoved event subscriptions, unreturned pool objects)
4. NavMesh usage (BANNED)
5. Public fields (BANNED)
6. Magic numbers (BANNED)
7. Physics called every frame in Update
8. Cross-system dependency issues (circular references etc.)
9. Performance concerns

Classify by severity:
- RED: build failure / crash risk
- YELLOW: possible bug
- GREEN: improvement suggested

If no issues: output "FINAL REVIEW PASSED"
```

---

## [Terminal 4] STEP 5. Build Prep

```
Read CLAUDE.md first.

Check the Windows build checklist and flag any issues.

Check:
1. Build Settings — PC, Windows, x86_64
2. Player Settings — company name, product name, version
3. Scene added to Build Settings
4. Required resources in Resources/ folder
5. No hardcoded local paths
6. Application.persistentDataPath used correctly

If issues found, explain how to fix.
```

---

## [Terminal 5] STEP 6. Technical Specification

```
Read CLAUDE.md first.

Write a draft technical specification document based on the implemented systems.
File: TechSpec.md

Contents:
1. Flocking Algorithm
   - Explain 3 rules: Separation, Alignment, Cohesion
   - Why NavMesh was avoided and how pure code was used instead
   - Formation maintenance approach

2. Optimization
   - Range detection throttling method
   - Object pooling coverage
   - Multi-unit computation handling

3. System Architecture Summary
   - Key classes and their roles
   - Inter-system dependencies

Length: 1~2 pages equivalent
Tone: technical documentation style
```

---

## Day 4 Completion Checklist

- [ ] 8 integration test scenarios passed
- [ ] Sound slots connected
- [ ] SaveSystem.cs (bonus)
- [ ] Final full review passed
- [ ] Windows build generated and tested (.exe)
- [ ] TechSpec.md written
- [ ] Gameplay video recorded (key features + combat)
- [ ] Unity project cleaned (remove Library, Temp, .vs folders)
- [ ] Archive filename: "111percent_assignment_[YourName]"
- [ ] Email subject: "111percent_assignment_[YourName]"
- [ ] Submit to recruit@111percent.net
