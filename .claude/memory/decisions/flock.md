# Decisions — Flock System

- FlockManager uses single shared _nearbyBuffer (cleared per unit) — zero per-frame heap allocation
- _sqrDetectionRadius cached in Awake — skip sqrt in hot detection loop
- FlockUnit.VelocityDirection retains last valid direction when speed drops to zero — prevents zero-vector LookRotation crash
- FlockUnit._maxSpeed = 7f (> PlayerController._moveSpeed 5f) — units can catch up during sprint
- FlockUnit.CalculateTargetSeek uses distance-proportional strength (Clamp(dist/_maxSpeed, 0, 1.5)) — rubber-band effect
- AddUnit() captures newIndex before list.Add(), calls Recalculate after — array size always matches index
- FormationHelper is pure C# class (not MonoBehaviour) — FlockManager holds instance; no scene component needed
