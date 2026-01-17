# Phase 5: Progression & Persistence

## Goal
Transform the prototype into a persistent game where progress is saved and visible. Players should feel a sense of growth through leveling up and seeing their stats improve.

## Objectives

1.  **Leveling UI**:
    *   Display current XP / XP To Next Level.
    *   Show current Level.
    *   Visualize progress with a slider bar.
2.  **Stat Upgrades**:
    *   Ensure leveling up has tangible benefits (already in `CharacterStats` but needs feedback).
    *   Show "Level Up!" notification.
3.  **Persistence (Save/Load)**:
    *   Save: Money, Gold.
    *   Save: Player Stats (Level, XP, Speed).
    *   Save: Buildings (Type, Position).
    *   Save: Terrain Modifications (Removed tiles).

## Implementation Plan

### 1. Leveling UI (`HUDController.cs` & `CharacterStats.cs`)
*   **Modify `CharacterStats.cs`**:
    *   Expose `MaxXP` (xpToNextLevel) via property or event so UI knows the slider max value.
    *   Ensure events sends `currentXP` and `maxXP`.
*   **Modify `HUDController.cs`**:
    *   Add references to XP Slider and Level Text.
    *   Subscribe to `CharacterStats` events.
    *   Update UI on `Start` and events.

### 2. Save System (`SaveSystem.cs` & `GameManager`?)
*   **Data Structures (`SaveData.cs`)**:
    *   `PlayerData`: Money, Gold, XP, Level.
    *   `TerrainData`: List of "Removed Tile Coordinates". (Since we generate procedurally, we just need to know what to remove).
    *   `BuildingData`: List of `{ Type, X, Y, Z }`.
*   **`SaveManager.cs`**:
    *   `SaveGame()`: Gather data from Managers, serialize to JSON, write to file (`Application.persistentDataPath`).
    *   `LoadGame()`: Read JSON, deserialize, distribute data to Managers.
*   **Integration**:
    *   Add "Save" and "Load" buttons to a Pause Menu or Main Menu.
    *   Or Auto-save on exit.

## Task Breakdown

### UI & Feedback
- [ ] Update `CharacterStats` to expose MaxXP.
- [ ] Update `HUDController` with XP Bar logic.
- [ ] Create XP Bar UI in scene.

### Persistence
- [ ] Create `SaveData` classes.
- [ ] Create `SaveManager`.
- [ ] Implement `TerrainManager` Save/Load (Record removed tiles).
- [ ] Implement `BuildingManager` Save/Load (Record placed buildings).
- [ ] Implement `ResourceManager` Save/Load.
- [ ] Add Save/Load UI buttons.
