using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation; // Requires "AI Navigation" package

namespace ClaimTycoon.Systems.Terrain
{
    public enum TileType
    {
        Dirt,
        Bedrock,
        Water
    }

    [RequireComponent(typeof(NavMeshSurface))]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TerrainManager : MonoBehaviour
    {
        public static TerrainManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(50, 50);
        public Vector2Int GridSize => gridSize;
        [SerializeField] private float cellSize = 1.0f;
        public float CellSize => cellSize;

        [SerializeField] private float initialHeight = 2.0f;
        [SerializeField] private float bedrockBaseHeight = -0.5f; // Renamed for clarity, acts as base
        [SerializeField] private float bedrockNoiseScale = 0.1f;
        [SerializeField] private float bedrockNoiseAmplitude = 0.5f;

        private float[,] heightMap;
        private float[,] bedrockMap;
        private Mesh terrainMesh;
        private Mesh bedrockMesh; // Added bedrock mesh
        private MeshCollider meshCollider;
        private NavMeshSurface navMeshSurface;

        // Keep track of buildings for persistence (unchanged logic)
        private Dictionary<Vector3Int, GameObject> occupiedTiles = new Dictionary<Vector3Int, GameObject>();
        private Dictionary<Vector3Int, GameObject> activeTiles = new Dictionary<Vector3Int, GameObject>(); // Deprecated for mesh, but kept for building compatibility

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            navMeshSurface = GetComponent<NavMeshSurface>();
            meshCollider = GetComponent<MeshCollider>();
            
            GenerateInitialTerrain();
        }

        private void Start()
        {
            // Optional: delayed updates if needed
        }

        private void GenerateInitialTerrain()
        {
            heightMap = new float[gridSize.x, gridSize.y];
            bedrockMap = new float[gridSize.x, gridSize.y];

            float noiseOffset = Random.Range(0f, 100f);

            // Initialize Heightmap and Bedrock Map
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    // Generate Bedrock Height using Perlin Noise
                    float noise = Mathf.PerlinNoise((x * bedrockNoiseScale) + noiseOffset, (z * bedrockNoiseScale) + noiseOffset);
                    bedrockMap[x, z] = bedrockBaseHeight + (noise * bedrockNoiseAmplitude);
                    // River Channel: Wider and flatter
                    // Center roughly at x=6 to x=8
                    
                    // River Channel with Curvature
                    float curveOffset = Mathf.Sin(z * 0.1f) * 3f; // Amplitude 3, Freq 0.1
                    float centerX = 25f + curveOffset; // Center of map (50/2 = 25)
                    
                    // Define River Widths
                    float riverBedHalfWidth = 2.0f; // Flat bottom radius
                    float innerBankWidth = 1.0f;
                    float middleBankWidth = 1.0f;
                    float outerBankWidth = 1.0f;
                    
                    float distFromCenter = Mathf.Abs(x - centerX);
                    
                    if (distFromCenter <= riverBedHalfWidth + innerBankWidth + middleBankWidth + outerBankWidth) // Total Valley Width
                    {
                        float h = 0f;
                        
                        if (distFromCenter <= riverBedHalfWidth) 
                        {
                            // River Bed (Flat Bottom)
                            h = 0.0f; 
                        }
                        else if (distFromCenter <= riverBedHalfWidth + innerBankWidth)
                        {
                            // Inner Banks
                            h = 0.5f;
                        }
                        else if (distFromCenter <= riverBedHalfWidth + innerBankWidth + middleBankWidth)
                        {
                            // Middle Banks
                            h = 1.0f;
                        }
                        else 
                        {
                            // Outer Banks
                            h = 1.5f;
                        }
                        
                        heightMap[x, z] = h;
                    }
                    else
                    {
                         heightMap[x, z] = initialHeight;
                    }

                }
            }

            UpdateMesh();
        }

        public float GetHeight(int x, int z)
        {
            if (x >= 0 && x < gridSize.x && z >= 0 && z < gridSize.y)
            {
                return heightMap[x, z];
            }
            return 0f;
        }

        public float GetBedrockHeight(int x, int z)
        {
             if (x >= 0 && x < gridSize.x && z >= 0 && z < gridSize.y)
            {
                return bedrockMap[x, z];
            }
            return bedrockBaseHeight;
        }


        private void UpdateMesh()
        {
            if (terrainMesh == null)
            {
                terrainMesh = MeshGenerator.GenerateTerrainMesh(heightMap, cellSize);
                GetComponent<MeshFilter>().mesh = terrainMesh;
                meshCollider.sharedMesh = terrainMesh; // Important for Raycast
                
                // Create Bedrock Visual Object if not exists
                Transform bedrockTrans = transform.Find("BedrockMeshDetails");
                GameObject bedrockObj;
                if (bedrockTrans == null) {
                    bedrockObj = new GameObject("BedrockMeshDetails");
                    bedrockObj.transform.SetParent(transform);
                    bedrockObj.transform.localPosition = Vector3.zero;
                    bedrockObj.AddComponent<MeshFilter>();
                    bedrockObj.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().sharedMaterial;
                    bedrockObj.GetComponent<Renderer>().material.color = Color.gray;
                } else {
                    bedrockObj = bedrockTrans.gameObject;
                }
                
                // Generate Bedrock Mesh (Similar to Terrain but for Bedrock Map)
                bedrockMesh = MeshGenerator.GenerateTerrainMesh(bedrockMap, cellSize, false);
                bedrockObj.GetComponent<MeshFilter>().mesh = bedrockMesh;
                // No collider needed for bedrock logic usually, unless we want to click it?
            }
            else
            {
                MeshGenerator.UpdateMeshVertices(terrainMesh, heightMap, cellSize);
                meshCollider.sharedMesh = terrainMesh;
                
                // Update Bedrock Mesh (It shouldn't change often but for safety)
                if (bedrockMesh != null)
                     MeshGenerator.UpdateMeshVertices(bedrockMesh, bedrockMap, cellSize, false);
            }
            
            // Rebuild NavMesh
            if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
        }

        public void ModifyHeight(Vector3 worldPoint, float amount)
        {
            int x = Mathf.RoundToInt(worldPoint.x / cellSize);
            int z = Mathf.RoundToInt(worldPoint.z / cellSize);

            Debug.Log($"[TerrainManager] ModifyHeight called: Input {worldPoint} -> Grid [{x}, {z}]. Amount: {amount}");

            if (x >= 0 && x < gridSize.x && z >= 0 && z < gridSize.y)
            {
                // Prevent digging below bedrock
                float currentBedrock = bedrockMap[x, z];
                if (heightMap[x, z] + amount < currentBedrock)
                {
                    amount = currentBedrock - heightMap[x, z]; // Clamp
                    if (Mathf.Abs(amount) < 0.01f) return; // Hit bottom
                }

                heightMap[x, z] += amount;

                // Smooth Neighbors (Reduced impact to preserve walls)
                float smoothFactor = 0.2f;
                SmoothNeighbor(x + 1, z, amount * smoothFactor);
                SmoothNeighbor(x - 1, z, amount * smoothFactor);
                SmoothNeighbor(x, z + 1, amount * smoothFactor);
                SmoothNeighbor(x, z - 1, amount * smoothFactor);

                UpdateMesh();
            }
        }

        private void SmoothNeighbor(int x, int z, float amount)
        {
             if (x >= 0 && x < gridSize.x && z >= 0 && z < gridSize.y)
            {
                float newHeight = heightMap[x, z] + amount;
                float localBedrock = bedrockMap[x, z];
                if (newHeight < localBedrock) newHeight = localBedrock;
                
                heightMap[x, z] = newHeight;
            }
        }

        // --- Backward Compatibility for Building Manager ---
        
        public void RegisterBuilding(Vector3Int coord, GameObject building)
        {
             if (!occupiedTiles.ContainsKey(coord)) occupiedTiles[coord] = building;
        }

        public bool IsTileOccupied(Vector3Int coord) => occupiedTiles.ContainsKey(coord);

        public bool TryGetTile(Vector3Int coord, out TileType type)
        {
            if (coord.x >= 0 && coord.x < gridSize.x && coord.z >= 0 && coord.z < gridSize.y)
            {
                // If we are at or near bedrock, report Bedrock
                if (heightMap[coord.x, coord.z] <= bedrockMap[coord.x, coord.z] + 0.1f)
                {
                    type = TileType.Bedrock;
                }
                else
                {
                    type = TileType.Dirt; 
                }
                return true;
            }
            type = TileType.Bedrock;
            return false;
        }
        
        // --- Persistence Stubs (Need Refactor for Heightmap) ---
        // For now preventing errors
        public List<Vector3Int> GetRemovedTiles() => new List<Vector3Int>();
        public void RestoreRemovedTiles(List<Vector3Int> list) { }
        public bool IsAdjacentToWater(Vector3Int coord) => false; 
    }
}
