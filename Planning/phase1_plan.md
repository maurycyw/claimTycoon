# Gold Mining Sim - Phase 1 Implementation Plan

This document outlines the immediate technical steps to build the **Terrain & Mining Prototype**.

## 1. Project Setup (User Actions)
*   [ ] **Create New Unity Project**:
    *   **Version**: Unity 6 (or 2022 LTS).
    *   **Template**: 3D (URP recommended for nice water/lighting).
*   [ ] **Version Control**:
    *   Initialize Git in the project folder.
    *   Add a standard Unity `.gitignore` file.
*   [ ] **Folder Structure**:
    *   `_Scripts/`
        *   `Managers/`
        *   `Controllers/`
        *   `Systems/Terrain/`
    *   `_Prefabs/`
    *   `_Materials/`

## 2. Core Scripts to Create
I can write these scripts for you once the project is created.

### A. CameraController.cs
*   **Goal**: RTS-style movement.
*   **Features**:
    *   WASD to pan.
    *   Scroll wheel to zoom.
    *   `Q` / `E` to rotate 45 degrees.

### B. TerrainManager.cs (The "Grid")
*   **Goal**: Manage data for which tiles are "Dirt", "Bedrock", or "Water".
*   **Approach**:
    *   Use a `Dictionary<Vector3Int, TileType>` or a flattened 3D array for storage.
    *   **Visuals**: For the prototype, just instantiate Cube prefabs for dirt. Later we can do mesh generation.

### C. MiningTool.cs
*   **Goal**: Interaction logic.
*   **Logic**:
    *   Raycast from mouse to world.
    *   If click on "Dirt" -> Call `TerrainManager.RemoveTile(coord)`.
    *   Spawn a "Gold Nugget" chance.

## 3. Immediate "To-Do" for You
1.  **Open Unity Hub** and create the project (Name: `GoldMiningSim`).
2.  **Tell me** when you have created it and where it is located (path), so I can start creating the script files directly into that folder.
