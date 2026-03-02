# SKILL: Bug Debugging

> Use this template when an error or unexpected behavior occurs

---

## Prompt Template

```
Read CLAUDE.md first.

There is a bug. Analyze and fix it.

## Bug Info
- When it happens: [what action triggers it]
- Symptom: [what goes wrong]
- Reproducibility: [always / sometimes / only under specific conditions]

## Error Message (paste full message if available)
```
[full error message here]
```

## Related Files
- [suspected filename.cs]

## Already Tried
- [things already attempted]

## Requirements
- Explain the root cause first
- After fixing, check if the same bug could appear elsewhere
- Update CLAUDE.md Memory known issues section after completion
```

---

## Example — NullReferenceException

```
Read CLAUDE.md first.

There is a bug. Analyze and fix it.

## Bug Info
- When it happens: when the player first moves after game start
- Symptom: units stop following and error appears in console
- Reproducibility: always

## Error Message
NullReferenceException: Object reference not set to an instance of an object
WildTamer.FlockUnit.UpdateMovement () (at Assets/Scripts/Flock/FlockUnit.cs:47)
WildTamer.FlockUnit.Update () (at Assets/Scripts/Flock/FlockUnit.cs:22)

## Related Files
- FlockUnit.cs
- FlockManager.cs

## Already Tried
- Confirmed FlockManager exists in scene — it does
```

---

## Example — Unexpected Behavior (no error)

```
Read CLAUDE.md first.

There is a bug. Analyze and fix it.

## Bug Info
- When it happens: when a monster is killed
- Symptom: taming always succeeds — probability seems ignored
- Reproducibility: always

## Error Message
None

## Related Files
- TamingSystem.cs

## Already Tried
- Confirmed SerializeField value is 0.3f (30%)
- But taming succeeds 100% of the time
```

---

## Performance Bug Template

```
Read CLAUDE.md first.

Performance has dropped suddenly. Find the cause and optimize.

## Situation
- When it started: [e.g. when 10+ monsters are on screen / after adding X feature]
- Symptom: FPS dropped from [N] to [N]
- Profiler shows: [what is heavy on Main Thread]

## Suspected Files
- [filename.cs]
```
