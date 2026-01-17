using System.Collections.Generic;
using UnityEngine;
using ClaimTycoon.Systems.Units; // For Stat class

namespace ClaimTycoon.Systems.Persistence
{
    [System.Serializable]
    public class GameSaveData
    {
        public PlayerData playerData = new PlayerData();
        public TerrainData terrainData = new TerrainData();
        public List<BuildingData> buildings = new List<BuildingData>();
    }

    [System.Serializable]
    public class PlayerData
    {
        public float money;
        public float gold;
        public Vector3 position;
        public List<Stat> stats = new List<Stat>();
    }

    [System.Serializable]
    public class TerrainData
    {
        // We only save removed tiles to reconstruct the terrain
        public List<Vector3Int> removedTiles = new List<Vector3Int>();
    }

    [System.Serializable]
    public class BuildingData
    {
        // For now we only have SluiceBox, but let's store a type or ID
        public string buildingID; // e.g. "SluiceBox"
        public Vector3 position;
        public Vector3Int gridCoord;
    }
}
