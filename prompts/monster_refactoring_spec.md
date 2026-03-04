# 📑 Technical Specification: Strategy-Driven Monster System

## 1. Architectural Goal
- **Consolidation of Units:** Eliminate `FlockUnit.cs` and individual monster scripts (e.g., `MonsterA`, `MonsterB`, `MonsterC`). All entities will now operate under a single, unified `MonsterUnit.cs`.
- **Passive Execution:** `MonsterUnit` acts as a passive agent that does not make internal decisions. It only executes logic injected by external systems.
- **Strategy Pattern:** Behavior is externalized into `ScriptableObject` assets (`MovementLogic`, `AttackLogic`).

## 2. Component Responsibilities

### **A. MonsterUnit (The Unified Agent)**
- **Role:** A single execution body for all monster types.
- **Requirement:** - No internal `if/switch` statements to determine behavior.
    - Uses `ApplyStrategy(MovementLogic, AttackLogic)` for dependency injection.
    - Executes `_currentMovement.Execute(this)` in the `Update` loop.

### **B. FactionSystem / FlockManager (The Brain)**
- **Judgment:** Handles relationship logic (Hostile, Friendly, Alliance) beyond simple `teamID` comparison.
- **Injection:** Decides which `MovementLogic` to inject (e.g., swapping to `FlockMovement` when a unit is Tamed).
- **Consolidation:** If roles overlap, integrate `FactionSystem` and `FlockManager` into a single centralized system.

### **C. MonsterData (The Strategy Provider)**
- **Role:** Holds the reference to the default `MovementLogic` and `AttackLogic` for each monster species.

## 3. Reference Folders
Analyze the following paths for refactoring:
- `Assets\Scripts\Combat`
- `Assets\Scripts\Data`
- `Assets\Scripts\Flock`
- `Assets\Scripts\Monster`

## 4. Key Logic to Extract
- **Movement:** Extract Flock, Patrol, Chase, etc., into `MovementLogic` SO classes.
- **Attack:** Unify attack behaviors into `AttackLogic` SO classes shared by all factions.