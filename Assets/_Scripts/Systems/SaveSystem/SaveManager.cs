using UnityEngine;
using System.IO;
using System.Collections.Generic;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Units;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.Systems.Persistence
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private string saveFileName = "savegame.json";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SaveGame()
        {
            GameSaveData data = new GameSaveData();

            // 1. Save Resources
            if (ResourceManager.Instance != null)
            {
                data.playerData.money = ResourceManager.Instance.MoneyAmount;
                data.playerData.gold = ResourceManager.Instance.GoldAmount;
            }

            // 2. Save Player Stats
            UnitController player = FindFirstObjectByType<UnitController>();
            if (player != null)
            {
                data.playerData.position = player.transform.position;
                
                CharacterStats stats = player.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    data.playerData.stats = stats.AllStats;
                }
            }

            // 3. Save Terrain (Removed Tiles)
            if (TerrainManager.Instance != null)
            {
                data.terrainData.removedTiles = TerrainManager.Instance.GetRemovedTiles(); 
            }

            // 4. Save Buildings
            if (BuildingManager.Instance != null)
            {
                data.buildings = BuildingManager.Instance.GetPlacedBuildings();
            }

            // Serialize
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            File.WriteAllText(path, json);

            Debug.Log($"Game Saved to {path}");
        }

        public void LoadGame()
        {
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("No save file found.");
                return;
            }

            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            // 1. Load Resources
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetResources(data.playerData.money, data.playerData.gold);
            }

            // 2. Load Stats & Position
            UnitController player = FindFirstObjectByType<UnitController>();
            if (player != null)
            {
                // Restore Position (Use Warp for NavMeshAgent)
                UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.Warp(data.playerData.position);
                else player.transform.position = data.playerData.position;

                if (data.playerData.stats != null)
                {
                    CharacterStats stats = player.GetComponent<CharacterStats>();
                    if (stats != null)
                    {
                        stats.LoadStats(data.playerData.stats);
                    }
                }
            }

            // 3. Load Terrain
            if (TerrainManager.Instance != null)
            {
                TerrainManager.Instance.RestoreRemovedTiles(data.terrainData.removedTiles);
            }

            // 4. Load Buildings
            if (BuildingManager.Instance != null)
            {
               BuildingManager.Instance.RestoreBuildings(data.buildings);
            }

            Debug.Log("Game Loaded!");
        }

        // GUI for testing
        private void OnGUI()
        {
             // Simple debug buttons
             if (GUI.Button(new Rect(10, 10, 100, 30), "Save")) SaveGame();
             if (GUI.Button(new Rect(10, 50, 100, 30), "Load")) LoadGame();
        }
    }
}
