# Gold Mining Sim - Phase 2 Implementation Plan

## Goal: Economy & Resource Loop
Transition from "destroying blocks" to "collecting and selling resources."

## 1. Resource Management
*   **ResourceManager (Singleton)**:
    *   Track `Gold` (amount) and `Money` (amount).
    *   UI events for when values change.
*   **Inventory (Simple)**:
    *   Player has a "Backpack" capacity.
    *   Mining adds raw dirt/gold to backpack.

## 2. Basic UI
*   **HUD**:
    *   Top-left: Money Counter ($0).
    *   Top-left: Gold Counter (0 oz).
*   **World UI**:
    *   Floating text when gold is found ("+0.5g").

## 3. Game Loop Logic
*   **Selling**:
    *   A simple "Sell All" button for the prototype (later: Drive to market).
    *   Conversion: 1g Gold = $50 (tunable).

## 4. Tasks
*   [ ] Create `ResourceManager.cs`
*   [ ] Implement `HUDController.cs` (Canvas management)
*   [ ] Update `MiningTool.cs` to add resources instead of just logging debug messages.
*   [ ] Create UI Canvas (User Action).
