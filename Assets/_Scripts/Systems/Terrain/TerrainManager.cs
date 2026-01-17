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
        [SerializeField] private float bedrockHeight = -0.5f;

        private float[,] heightMap;
        private Mesh terrainMesh;
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

            // Initialize Heightmap 
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    // River Channel: Wider and flatter
                    // Center roughly at x=6 to x=8
                    
                    if (x >= 3 && x <= 11) // Overall Valley
                    {
                        float h = 0f;
                        
                        if (x >= 6 && x <= 8) 
                        {
                            // River Bed (Flat Bottom)
                            h = 0.0f; 
                        }
                        else if (x == 5 || x == 9)
                        {
                            // Inner Banks
                            h = 0.5f;
                        }
                        else if (x == 4 || x == 10)
                        {
                            // Middle Banks
                            h = 1.0f;
                        }
                        else if (x == 3 || x == 11)
                        {
                            // Outer Banks (Start of slope)
                            h = 1.5f;
                        }
                        
                        heightMap[x, z] = h;
                    }
                    else
                    {
                        heightMap[x, z] = initialHeight; // 2.0f
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

        private void CreateBedrockVisual()
        {
            GameObject bedrock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedrock.name = "Bedrock";
            bedrock.transform.SetParent(this.transform);
            
            // Scale: X = Width * CellSize, Y = Thickness, Z = Depth * CellSize
            // Position: Center of grid, at bedrock Height - Thickness/2
            float width = gridSize.x * cellSize;
            float depth = gridSize.y * cellSize;
            float thickness = 2.0f; 

            bedrock.transform.localScale = new Vector3(width, thickness, depth);
            bedrock.transform.position = new Vector3(width / 2f, bedrockHeight - (thickness / 2f), depth / 2f);

            // Optional: Assign material if available, or just leave gray
             if (GetComponent<MeshRenderer>() != null)
             {
                 Material mat = GetComponent<MeshRenderer>().sharedMaterial;
                 bedrock.GetComponent<Renderer>().material = mat;
                 // Darken it
                 bedrock.GetComponent<Renderer>().material.color = Color.gray; 
             }
        }

        private void UpdateMesh()
        {
            if (terrainMesh == null)
            {
                terrainMesh = MeshGenerator.GenerateTerrainMesh(heightMap, cellSize);
                GetComponent<MeshFilter>().mesh = terrainMesh;
                meshCollider.sharedMesh = terrainMesh; // Important for Raycast
            }
            else
            {
                MeshGenerator.UpdateMeshVertices(terrainMesh, heightMap, cellSize);
                meshCollider.sharedMesh = terrainMesh;
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
                if (heightMap[x, z] + amount < bedrockHeight)
                {
                    amount = bedrockHeight - heightMap[x, z]; // Clamp
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
                // Clamp Main HeightMap check handles it? No, we modify directly here.
                if (newHeight < bedrockHeight) newHeight = bedrockHeight;
                
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
                if (heightMap[coord.x, coord.z] <= bedrockHeight + 0.1f)
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
