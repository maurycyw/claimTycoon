# Gold Mining Sim - Phase 4 Implementation Plan

## Goal: Player Character & RPG Elements
Transition from "God Interaction" to "Character Interaction". The player controls a specific character who walks to locations to mine or work.

## 1. Character Navigation (NavMesh)
*   **System**: Unity NavMesh.
*   **Setup**: Grid Tiles (Terrain) must be marked `Navigation Static`.
*   **Movement**: "Tap-to-Move" style.
    *   Click Ground -> Character walks there.

## 2. Character Logic & Stats
*   **UnitController**: Handles movement and current "Command" (Idle, Moving, Working).
*   **Attributes (RPG)**:
    *   `MiningLevel`: Speeds up mining.
    *   `Strength`: Carry capacity?
    *   `Speed`: Move speed.
    *   **XP System**: Doing actions increases stats.

## 3. Interaction Refactor
*   **Old**: Click Dirt -> `Destroy()`.
*   **New**: Click Dirt -> Issue "Mine Command" -> Character walks to Dirt -> Plays animation (or waits time) -> `Destroy()`.

## 4. Tasks
*   [ ] **Navigation Setup**:
    *   [ ] Add `NavMeshSurface` (or bake setup) to Terrain logic.
*   [ ] **Character Core**:
    *   [ ] Create `CharacterStats.cs` (Data container).
    *   [ ] Create `UnitController.cs` (NavMeshAgent wrapper).
    *   [ ] Create `PlayerInteraction.cs` (Input handler to issue commands).
*   [ ] **Actions**:
    *   [ ] Create `Action_Mine.cs` (Logic: Walk -> Wait -> Break).
*   [ ] **Assets**:
    *   [ ] Create "Player" Prefab (Capsule for now with a distinct color).
