# Gold Mining Sim - Design Document

**Status**: Draft
**Date**: 2026-01-16
**Platform**: PC / Mac (Steam)
**Genre**: Isometric Management / Tycoon Strategy / RPG

## 1. Executive Summary
An isometric management simulation where players inherit a dilapidated plot of land in gold country. Unlike first-person mining sims (*Gold Rush: The Game*, *Hydroneer*) that focus on direct physics manipulation, this game focuses on **strategic management** and **automation**. Players start by mining manually but eventually build a mining empire run by autonomous AI employees with unique traits and RPG-like skill progression.

**The "Hook"**: *The Sims* meets *Factorio* (lite) in a Gold Rush setting. You don't just upgrade machines; you upgrade *people*.

## 2. Market & Competitor Analysis
### The Landscape
*   **Genre Growth**: The Tycoon/Sim genre is growing (17.2% CAGR). Steam indie revenue is hitting record highs ($4.5B in 2025).
*   **Proven Demand**:
    *   *Gold Rush: The Game*: ~400k-800k owners, ~$13M revenue. (Realistic, Heavy Machinery focus).
    *   *Hydroneer*: ~1M owners, ~$18M revenue. (Physics building, Voxel mining).
*   **The Gap**: Most successes are First-Person. There is a lack of high-quality **Isometric Management** games in this specific niche. Players who enjoy *Two Point Hospital* or *RimWorld* mechanics but want a gold mining theme are underserved.

### Target Audience
*   Fans of management/automation games (*Factorio*, *Satisfactory*).
*   Players who like "number go up" progression but want visual feedback (seeing the stockpiles grow).
*   PC Gamers looking for a "chill" but deep experience (1-2 hour sessions).

## 3. Gameplay Mechanics
### Core Loop
1.  **Extract**: Break ground (Pickaxe -> Excavator -> Automated Drills).
2.  **Process**: Wash dirt (Pan -> Sluice Box -> Wash Plant).
3.  **Refine & Sell**: Smelt gold dust into bars, sell at the market (dynamic prices).
4.  **Invest**: Buy better machines, expand land, or **Hire Staff**.

### Key Systems
*   **AI Staff (The Differentiator)**:
    *   **Traits**: "Early Bird" (works faster in morning), "Klutz" (breaks machines often), "Geologist" (finds gems).
    *   **Skills**: Mining, Driving, Repair, Negotiation.
    *   **Logic**: Uses Behavior Trees. Employees have Needs (Energy, Morale). If Morale drops, they strike or steal.
*   **Terrain System**: Voxel-based or Heightmap modification. Digging actually changes the terrain.
*   **Seasons/Weather**: Winter freezes water (halts easy washing), Rain fills pits.

## 4. Visual Style: 3D Low Poly
*   **Why**:
    *   **Cost**: "Synty Studios" or "Kenney" assets are cheap ($30-$50 packs) and high quality.
    *   **Performance**: Handles hundreds of AI agents and moving parts easily.
    *   **Aesthetic**: Timeless, clean, and reads well in Isometric view.
*   **Perspective**: Orthographic Camera, rotatable 45 degrees.

## 5. Technical Architecture
*   **Engine**: Unity 6 (LTS).
*   **Language**: C#.
*   **Data**: `ScriptableObjects` for Item definitions, Employee Archetypes, Tech Tree.
*   **AI**:
    *   **Navigation**: Unity NavMesh (Agents move between Task nodes).
    *   **Logic**: State Machine or Behavior Tree (NodeCanvas or custom).
*   **Save System**: JSON serialization of terrain state and employee stats.

## 6. Cost & Development Estimates (Indie Scale)
**Goal**: A polished Vertical Slice (Demo) for Steam Next Fest.

### Estimated Costs (Solo/Small Team)
*   **Engine**: Unity (Free until $200k revenue).
*   **Assets**:
    *   3D Models (Environment/Characters): $200 - $500 (Asset Store packs).
    *   UI/Icons: $50 - $100.
    *   Sound Effects: $50.
*   **Steam Fee**: $100 (Direct to Valve).
*   **Legal**: LLC Formation (Optional but recommended): $50-200.
*   **Marketing**:
    *   Assets (Trailer/Capsule Art): $0 (DIY) to $1,000 (Freelancer).
    *   **Total MVP Cost**: **~$500 - $2,000** (assuming solo dev time = $0).

### Time Budget
*   **Month 1**: Prototype. Terrain digging, clicking to mine, basic UI.
*   **Month 2**: Core Loop. Buying/Selling, Equipment placement.
*   **Month 3**: AI System. Employee hiring/tasking.
*   **Month 4**: Polish & Save System.
*   **Month 5**: Content Expansion & Demo Release.

## 7. Risks
*   **Scope Creep**: Simulation games can get infinitely complex. We must strictly define the MVP (Minimum Viable Product).
*   **Performance**: Deformable terrain + Pathfinding is tricky.
    *   *Mitigation*: Use grid-based terrain interactions (Minecraft style logic) rather than smooth physics.
