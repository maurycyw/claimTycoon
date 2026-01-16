# Gold Mining Sim - Phase 3 Implementation Plan

## Goal: Spending Money & Building
Enable the player to spend their earned Money to purchase and place objects (Basic Machines/Structures) in the world.

## 1. Shop System
*   **Data**: Simple definitions for buyable items (Name, Cost, Prefab).
*   **UI**:
    *   "Shop" Button in HUD (opens/closes panel).
    *   Buttons for items (e.g., "Auto-Miner" - $100).
    *   Clicking an item checks `Money >= Cost`.
    *   If affordable -> deducting money -> Switch to **Placement Mode**.

## 2. Placement System
*   **Grid Placement**:
    *   Reuse `TerrainManager` grid logic.
    *   Raycast from mouse to find tile.
    *   Show "Ghost" prefab (transparent/green).
    *   Click to Place (Instantiate real prefab).
*   **Validation**:
    *   Can only place on "Dirt" (or cleared land?).
    *   **New Rule**: Sluice Box must be placed adjacent to a Water tile.
    *   Cannot place on top of other objects.

## 3. The Object (River Run Sluice Box)
*   **Logic**:
    *   Passive income generator (simulates washing dirt).
    *   Every X seconds: `ResourceManager.AddGold(0.2f)`.
*   **Terrain Update**:
    *   Need to generate a "River" in the `TerrainManager`.

## 4. Tasks
*   [ ] **Terrain**:
    *   [ ] Update `TerrainManager.cs` to generate a Water river.
*   [ ] **Core Logic**:
    *   [ ] Create `BuildingManager.cs` (Handles placement & validation).
    *   [ ] Create `SluiceBox.cs` (Passive income logic).
*   [ ] **UI**:
    *   [ ] Create `ShopController.cs`.
    *   [ ] Add Shop Panel & Sluice Box Button.
*   [ ] **Assets**:
    *   [ ] Create "Water" Prefab (Blue).
    *   [ ] Create "SluiceBox" Prefab.
