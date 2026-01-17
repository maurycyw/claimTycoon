using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Persistence;
using ClaimTycoon.Controllers;
using ClaimTycoon.Systems.Buildings;

namespace ClaimTycoon.Managers
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Material errorMaterial;

        // Prefab references for loading
        [SerializeField] private GameObject sluiceBoxPrefab; 
        // If we have more buildings, use a List<GameObject> and ID lookup

        private GameObject buildingPrefab;
        private GameObject ghostObject;
        private int currentCost;
        private bool isPlacing = false;
        public bool IsPlacing => isPlacing;
        private Camera mainCamera;
        private bool isPlacingSluiceBox = false;

        // Track placed buildings
        private List<BuildingData> placedBuildings = new List<BuildingData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void StartPlacement(GameObject prefab, int cost, bool isSluice = false)
        {
            if (ResourceManager.Instance.MoneyAmount < cost)
            {
                Debug.Log("Not enough money!");
                return;
            }

            StartPlacementInternal(prefab, cost, isSluice);
        }

        public void StartPlacementById(string buildingId)
        {
            // Simple lookup for now
            if (buildingId == "SluiceBox")
            {
                // Sluice Box costs 0 if from inventory? 
                // Logic says if isSluice=true, we check inventory instead of money.
                StartPlacementInternal(sluiceBoxPrefab, 0, true);
            }
            else
            {
                Debug.LogWarning($"Unknown building ID for placement: {buildingId}");
            }
        }

        private void StartPlacementInternal(GameObject prefab, int cost, bool isSluice)
        {
            buildingPrefab = prefab;
            currentCost = cost;
            isPlacingSluiceBox = isSluice;
            isPlacing = true;
            Debug.Log("Entered Placement Mode");
            
            if (ghostObject != null) Destroy(ghostObject);
            ghostObject = Instantiate(buildingPrefab);
            
            foreach (var c in ghostObject.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var m in ghostObject.GetComponentsInChildren<MonoBehaviour>()) Destroy(m);
        }

        private void Update()
        {
            if (!isPlacing) return;

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                StopPlacement();
                return;
            }
            
            TryPlaceBuilding();
        }

        private void TryPlaceBuilding()
        {
             if (Mouse.current == null) return;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, terrainLayer))
            {
                Vector3 hitPos = hit.point;
                float cellSize = TerrainManager.Instance.CellSize;
                
                int gridX = Mathf.RoundToInt(hitPos.x / cellSize);
                int gridZ = Mathf.RoundToInt(hitPos.z / cellSize);

                Vector3Int coord = new Vector3Int(gridX, 0, gridZ);

                bool isValid = true;

                if (!TerrainManager.Instance.TryGetTile(coord, out TileType type))
                {
                    isValid = false; 
                }

                if (type == TileType.Water) isValid = false;

                if (isPlacingSluiceBox)
                {
                    // Strict Water Check
                    float terrainHeight = TerrainManager.Instance.GetHeight(coord.x, coord.z);
                    float waterLevel = 1.8f; 
                    float minDepth = 0.1f;

                    if ((waterLevel - terrainHeight) < minDepth)
                    {
                        isValid = false;
                        // Optional: Debug log why invalid
                        // Debug.Log($"Invalid Sluice Placement: Terrain {terrainHeight} is too high for water {waterLevel}");
                    }
                }

                if (ghostObject != null)
                {
                    // Snap to grid but get Height from Mesh
                    float yHeight = TerrainManager.Instance.GetHeight(coord.x, coord.z);
                    Vector3 ghostPos = new Vector3(coord.x * cellSize, yHeight, coord.z * cellSize); 
                    ghostObject.transform.position = ghostPos;

                    Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
                    Material matToUse = isValid ? ghostMaterial : errorMaterial;
                    
                    if (matToUse != null)
                    {
                        foreach (var r in renderers) r.material = matToUse;
                    }
                }

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isValid)
                {
                    Place(coord);
                }
            }
        }

        private void Place(Vector3Int coord)
        {
            // Inventory Check
            if (isPlacingSluiceBox)
            {
                if (!ResourceManager.Instance.PlayerInventory.RemoveItem("SluiceBox", 1))
                {
                    Debug.LogWarning("Cannot place Sluice Box: No Inventory!");
                    StopPlacement();
                    return;
                }
            }
            else
            {
                ResourceManager.Instance.AddMoney(-currentCost);
            }

            float cellSize = TerrainManager.Instance.CellSize;
            float yHeight = TerrainManager.Instance.GetHeight(coord.x, coord.z);
            Vector3 spawnPos = new Vector3(coord.x * cellSize, yHeight, coord.z * cellSize);
            
            // SPAWN CONSTRUCTION SITE INSTEAD OF PREFAB
            // We need a visual for the site. For now, use the Ghost Prefab (Sluice) but with ConstructionSite component?
            // Or instantiate a "Site" prefab. 
            // Quickest Prototype: Instantiate the SluicePrefab, but disable SluiceBox script, add ConstructionSite.
            
            GameObject siteObj = new GameObject("ConstructionSite_" + buildingPrefab.name);
            siteObj.transform.position = spawnPos;
            
            // Add Visual (Ghost of the building)
            GameObject visual = Instantiate(buildingPrefab, siteObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            
            // Strip logic and Apply Ghost Material
            foreach (var c in visual.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var m in visual.GetComponentsInChildren<MonoBehaviour>()) Destroy(m);
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                if (ghostMaterial != null) r.material = ghostMaterial;
            }
            
            ConstructionSite siteComp = siteObj.AddComponent<ConstructionSite>();
            siteComp.SetResultPrefab(buildingPrefab);

            // Register SITE as building? 
            TerrainManager.Instance.RegisterBuilding(coord, siteObj);

            // TRACK DATA
            BuildingData data = new BuildingData();
            data.buildingID = isPlacingSluiceBox ? "SluiceBox" : "Unknown"; 
            data.position = spawnPos;
            data.gridCoord = coord;
            placedBuildings.Add(data);

            Debug.Log($"Placed Construction Site for {buildingPrefab.name} at {coord}");
            
            // Auto-Assign Job if Unit Selected OR Find Main Player
            UnitController worker = SelectionManager.Instance.SelectedUnit;
            if (worker == null)
            {
                // Try to find any unit (assuming there's one main player for now)
                worker = FindObjectOfType<UnitController>();
            }

            if (worker != null)
            {
                // Calculate Stand Position (Neighboring Tile)
                Vector3 standPos = spawnPos; // Default fall back
                Vector3Int[] neighbors = new Vector3Int[] 
                { 
                    coord + new Vector3Int(1, 0, 0), 
                    coord + new Vector3Int(-1, 0, 0), 
                    coord + new Vector3Int(0, 0, 1), 
                    coord + new Vector3Int(0, 0, -1) 
                };

                foreach(var n in neighbors)
                {
                    // Check if walkable (Simple check: Is Dirt? Not Water?)
                    // Assuming TerrainManager.Instance.IsTileOccupied(n) check might be good too
                    if (TerrainManager.Instance.TryGetTile(n, out TileType type))
                    {
                        if (type != TileType.Water && !TerrainManager.Instance.IsTileOccupied(n))
                        {
                            float h = TerrainManager.Instance.GetHeight(n.x, n.z);
                            standPos = new Vector3(n.x * cellSize, h, n.z * cellSize);
                            break; // Found one
                        }
                    }
                }

                worker.StartJob(JobType.Build, coord, standPos, null, siteComp);
            }

            StopPlacement();
        }

        private void StopPlacement()
        {
            isPlacing = false;
            buildingPrefab = null;
            if (ghostObject != null) Destroy(ghostObject);
        }

        // SAVE/LOAD METHODS
        public List<BuildingData> GetPlacedBuildings() => placedBuildings;

        public void RestoreBuildings(List<BuildingData> loadedBuildings)
        {
            // Clear existing logic if needed? For now we assume fresh load or just adding to it.
            // Ideally we Destroy all current buildings first.
            // But we don't have a list of GameObject references easily unless we stored them.
            // TerrainManager has occupiedTiles, might need to clear that too.
            
            // For prototype: Just spawn the loaded ones.
            foreach (var b in loadedBuildings)
            {
                GameObject prefab = null;
                if (b.buildingID == "SluiceBox") prefab = sluiceBoxPrefab;

                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab, b.position, Quaternion.identity);
                    TerrainManager.Instance.RegisterBuilding(b.gridCoord, obj);
                    placedBuildings.Add(b);
                }
            }
        }
    }
}
