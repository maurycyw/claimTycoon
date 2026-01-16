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
    *   Cannot place on top of other objects.

## 3. The Object (Auto-Miner)
*   **Simplest Logic**:
    *   Just a visual block for now?
    *   Or better: Every X seconds, it calls `ResourceManager.AddGold(0.1f)`.
    *   Let's go with the passive income generator for immediate feedback.

## 4. Tasks
*   [ ] **Core Logic**:
    *   [ ] Create `BuildingManager.cs` (Handles placement state).
    *   [ ] Create `AutoMiner.cs` (Passive income logic).
*   [ ] **UI**:
    *   [ ] Create `ShopController.cs`.
    *   [ ] Add Shop Panel & Item Button.
*   [ ] **Assets**:
    *   [ ] Create "AutoMiner" Prefab (Just a block of a different color, e.g., Blue).
