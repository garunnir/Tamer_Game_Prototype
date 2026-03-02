# Day 1 — Claude Code Prompts
# Project Setup & Flock System

> Run prompts in order. Finish one before moving to the next.
> Always start with "Read CLAUDE.md first"

---

## STEP 1. Quarter-view Camera

```
Read CLAUDE.md first.

Set up a quarter-view camera for Unity 6 Built-in pipeline.
File: QuarterViewCamera.cs

Requirements:
- Fixed Y-axis 45deg rotation, X-axis 60deg pitch
- Follows player Transform with fixed offset
- Smooth follow (lerp)
- Include CameraShake() method — will be used for hit feedback later
- Offset and smoothing values exposed via SerializeField
```

---

## STEP 2. Player Movement

```
Read CLAUDE.md first.

Implement player movement for PC.
File: PlayerController.cs
Location: Assets/Scripts/Player/

Requirements:
- WASD or arrow key input
- Movement direction corrected relative to quarter-view camera
- Rigidbody-based
- Animator parameter connection (Speed float)
- Movement speed via SerializeField
- Namespace: WildTamer
```

---

## STEP 3. FlockUnit (Individual Unit)

```
Read CLAUDE.md first.

Implement a single flock unit in pure C# without NavMesh.
File: FlockUnit.cs
Location: Assets/Scripts/Flock/
Namespace: WildTamer

Requirements:
- Separation: move away from nearby units if too close
- Alignment: match average movement direction of nearby units
- Cohesion: move toward center of flock
- Move toward a Target point (player position)
- Combine all 4 forces with weights to determine final movement direction
- Move via direct Transform.position manipulation (no Rigidbody)
- All weights, detection radius, max speed exposed via SerializeField
- Receives nearby unit list from FlockManager
```

---

## STEP 4. FlockManager (Flock Manager)

```
Read CLAUDE.md first.

Implement FlockManager to manage all FlockUnits.
File: FlockManager.cs
Location: Assets/Scripts/Flock/
Namespace: WildTamer

Requirements:
- Holds and manages list of FlockUnits
- Passes player position as Target to all units
- On unit spawn, arrange in circular or grid formation
- Each frame, pass filtered nearby unit list to each unit (distance-based)
- Include AddUnit() and RemoveUnit() methods (called by TamingSystem later)
- Max unit count via SerializeField
- Performance: use direct distance calculation instead of Physics.OverlapSphere
```

---

## STEP 5. FormationHelper (Formation Maintenance)

```
Read CLAUDE.md first.

Implement FormationHelper to maintain formation around player.
File: FormationHelper.cs
Location: Assets/Scripts/Flock/
Namespace: WildTamer

Requirements:
- Calculate circular offset positions around player based on unit count
- Assign each FlockUnit its target offset position
- Recalculate offsets when units are added or removed
- Called from FlockManager
```

---

## STEP 6. Integration Test

```
Read CLAUDE.md first.

Help me test all systems built so far.

Check the following:
1. QuarterViewCamera follows player correctly
2. 5 FlockUnits maintain formation and follow player
3. Units do not overlap each other
4. Flock follows naturally when player moves quickly

If any issues, analyze the cause and fix.
After fixing, update CLAUDE.md Memory section.
```

---

## Day 1 Completion Checklist

- [ ] QuarterViewCamera.cs (with CameraShake)
- [ ] PlayerController.cs
- [ ] FlockUnit.cs
- [ ] FlockManager.cs
- [ ] FormationHelper.cs
- [ ] Integration test passed
- [ ] git commit
- [ ] CLAUDE.md implementation checklist updated
