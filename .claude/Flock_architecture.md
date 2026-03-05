Prompt for AI Code Generation: Professional Unit Formation & Steering System

Context & Goal

Develop a high-performance, production-ready military formation and steering system in Unity. The architecture must be strictly decoupled to support scalability and future Unity DOTS (Job System/Burst) integration.

Architecture & Constraints

Namespace: WildTamer

Language: C# (Unity)

Design Pattern: Data-Oriented separation. Logic must be decoupled from MonoBehaviour where possible to allow for future conversion to IJobParallelFor.

Performance: Zero GC in Update/FixedUpdate. Use pre-allocated arrays (Vector3[]) instead of List<T>.

1. FormationHelper.cs (Static Math Utility)

Objective: A stateless utility class for geometric calculations.

Formation Types: Implement Circle, Line, Column, Square, and Wedge.

Centroid Alignment: For every formation, calculate the arithmetic mean (centroid) of all points. Subtract the centroid from each position so the formation is always centered at $(0,0,0)$.

Memory Optimization: Use Vector3[] or NativeArray<Vector3> (for DOTS compatibility).

Dirty Flag Logic: Implement a calculation trigger that only runs when unitCount, spacing, or type changes.

Math Logic:

Wedge: $z = -|x|$ distribution.

Square: Grid size = $\lceil \sqrt{\text{unitCount}} \rceil$.

2. FlockManager.cs (Coordination & Greedy Assignment)

Objective: The "Brain" that bridges player movement and unit slots.

Anchor Point: playerTransform.position + (playerTransform.rotation * _pivotOffset).

Greedy Slot Assignment:

Avoid complex Hungarian algorithms for now; implement an Optimized Greedy Assignment.

For each unit, find the nearest available slot using sqrMagnitude (to avoid costly sqrt).

Recalculate assignments only when the formation "Dirties" or a unit dies to minimize CPU spikes.

DOTS Scalability: Structure unit data into flat arrays so they can be easily passed to a Job System in the future.

Enhanced Gizmo Debugging:

Draw wire spheres for all slots.

Color Coding: Cyan for assigned slots, Red for empty/overflow.

Visual Links: Draw Gizmos.DrawLine from each unit's current position to its assigned target slot for real-time debugging.

3. FlockMoveLogic.cs (Steering & Physics)

Objective: Individual unit behavior and movement execution.

Execution Guard: Run logic only when CurrentState == UnitState.Follow.

Steering Forces:

Arrival: Move toward the assigned slot. Implement a _slowingDistance to prevent overshooting.

Separation: Apply repulsive force between neighbors.

Jitter Mitigation: Linearly scale down (dampen) the Separation force as the unit's distance to the target slot center approaches zero.

Physics-Based Movement: Use Rigidbody.AddForce or Rigidbody.velocity. No direct Transform manipulation.

Adaptive Rotation:

If velocity.magnitude > 0.1f, rotate toward movement direction.

If nearly stopped, smoothly Slerp rotation to match the playerTransform.rotation.

Optimization: Cache all component references (Rigidbody, Transform) in Awake.

4. Technical Excellence Requirements

Detailed Comments: All complex math and coordination logic must include detailed Korean comments.

Inspector Exposure: Serialize _spacing, _slowingDistance, _maxSpeed, and _acceleration.

DOTS-Ready Thinking: Keep logic functions pure (input -> output) to ensure easy migration to SystemBase or IJobEntity.