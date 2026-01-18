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

    public struct DirtData
    {
        public float TopSoil;
        public float PayLayer;
        public float Total => TopSoil + PayLayer;
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
        [SerializeField] private float bedrockBaseHeight = -0.5f; 
        [SerializeField] private float bedrockNoiseScale = 0.1f;
        [SerializeField] private float bedrockNoiseAmplitude = 0.5f;

        [Header("Rolling Hills")]
        [SerializeField] private float surfaceNoiseScale = 0.05f; // Broader hills
        [SerializeField] private float surfaceNoiseAmplitude = 1.5f; 
        [SerializeField] private float topSoilDepth = 1.5f; // Depth of topsoil before pay layer
        [SerializeField] private float vegetationDepth = 0.2f; // Depth of vegetation layer

        private float[,] heightMap;
        private float[,] bedrockMap;
        private float[,] payLayerLimitMap; // Height at which pay layer starts
        private float[,] vegetationLimitMap; // Height at which vegetation ends (below this is Top Soil)
        
        private Mesh terrainMesh;
        private Mesh bedrockMesh; 
        private MeshCollider meshCollider;
        private NavMeshSurface navMeshSurface;
        
        // Keep track of buildings for persistence
        private Dictionary<Vector3Int, GameObject> occupiedTiles = new Dictionary<Vector3Int, GameObject>();
        private Dictionary<Vector3Int, GameObject> activeTiles = new Dictionary<Vector3Int, GameObject>(); 

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            navMeshSurface = GetComponent<NavMeshSurface>();
            meshCollider = GetComponent<MeshCollider>();

            // Ensure we use the Vertex Color shader
            Shader vcShader = Shader.Find("Custom/VertexColor");
            if (vcShader != null)
            {
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr.material.shader.name != vcShader.name) 
                {
                    mr.material.shader = vcShader;
                }
            }

            GenerateInitialTerrain();
        }

        private void Start()
        {
        }
        
        private void GenerateInitialTerrain()
        {
            heightMap = new float[gridSize.x, gridSize.y];
            bedrockMap = new float[gridSize.x, gridSize.y];
            payLayerLimitMap = new float[gridSize.x, gridSize.y];
            vegetationLimitMap = new float[gridSize.x, gridSize.y];

            float noiseOffset = Random.Range(0f, 100f);
            float surfaceOffset = Random.Range(100f, 200f); 

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    // ... (Bedrock & Surface Noise Logic same) ...
                    float bedNoise = Mathf.PerlinNoise((x * bedrockNoiseScale) + noiseOffset, (z * bedrockNoiseScale) + noiseOffset);
                    bedrockMap[x, z] = bedrockBaseHeight + (bedNoise * bedrockNoiseAmplitude);

                    float surfNoise = Mathf.PerlinNoise((x * surfaceNoiseScale) + surfaceOffset, (z * surfaceNoiseScale) + surfaceOffset);
                    float surfaceHeight = initialHeight + (surfNoise * surfaceNoiseAmplitude);

                    // ... (River Logic same) ...
                    float curveOffset = Mathf.Sin(z * 0.1f) * 3f; 
                    float centerX = 25f + curveOffset; 
                    float riverBedHalfWidth = 2.0f; 
                    float innerBankWidth = 1.0f;
                    float middleBankWidth = 1.0f;
                    float outerBankWidth = 1.0f;
                    float distFromCenter = Mathf.Abs(x - centerX);
                    float totalBankWidth = innerBankWidth + middleBankWidth + outerBankWidth; 
                    if (distFromCenter <= riverBedHalfWidth + totalBankWidth) 
                    {
                        // Calculate interpolation factor 't' from 0 (deepest part/water edge) to 1 (top of bank)
                        // 0 to riverBedHalfWidth is flat bottom (t=0)
                        float t = 0f;
                        if (distFromCenter > riverBedHalfWidth)
                        {
                            t = (distFromCenter - riverBedHalfWidth) / totalBankWidth;
                            // Apply SmoothStep for Natural Curve
                            t = t * t * (3f - 2f * t);
                        }

                        // Determine the theoretical river bank height at this X,Z
                        // We assume the river cuts into the generated surface noise.
                        // We want to blend from 0.0f (River Bottom) to surfaceHeight (Original Noise)
                        
                        float riverCarveHeight = Mathf.Lerp(0.0f, surfaceHeight, t);

                        // Ensure we actually carve downwards
                        if (riverCarveHeight < surfaceHeight) surfaceHeight = riverCarveHeight;
                    }

                    heightMap[x, z] = surfaceHeight;

                    // Calculate Limits
                    // Vegetation Limit: Surface - VegDepth
                    float vegLimit = surfaceHeight - vegetationDepth;
                    if (vegLimit < bedrockMap[x, z] + 0.05f) vegLimit = bedrockMap[x, z] + 0.05f;
                    vegetationLimitMap[x, z] = vegLimit;

                    // Pay Layer Limit: Surface - TopSoilDepth
                    // Note: If topSoilDepth includes VegDepth? Usually "Top Soil" starts BELOW Veg.
                    // Let's assume user structure: [Veg (Green)] -> [Top Soil (Light Brown)] -> [Pay Layer (Dark Brown)]
                    // So pay limit is lower than vegetation limit.
                    float payLimit = surfaceHeight - topSoilDepth;
                    if (payLimit < bedrockMap[x, z] + 0.01f) payLimit = bedrockMap[x, z] + 0.01f;
                    payLayerLimitMap[x, z] = payLimit;
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
                terrainMesh = MeshGenerator.GenerateTerrainMesh(heightMap, payLayerLimitMap, vegetationLimitMap, cellSize);
                GetComponent<MeshFilter>().mesh = terrainMesh;
                meshCollider.sharedMesh = terrainMesh; 
                
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
                
                bedrockMesh = MeshGenerator.GenerateTerrainMesh(bedrockMap, null, null, cellSize, false);
                bedrockObj.GetComponent<MeshFilter>().mesh = bedrockMesh;
            }
            else
            {
                MeshGenerator.UpdateMeshVertices(terrainMesh, heightMap, payLayerLimitMap, vegetationLimitMap, cellSize);
                meshCollider.sharedMesh = terrainMesh;
                
                if (bedrockMesh != null)
                     MeshGenerator.UpdateMeshVertices(bedrockMesh, bedrockMap, null, null, cellSize, false);
            }
            
            if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
        }

        // Returns { TopSoilRemoved, PayLayerRemoved }
        public DirtData ModifyHeight(Vector3 worldPoint, float amount)
        {
            DirtData data = new DirtData();
            int x = Mathf.RoundToInt(worldPoint.x / cellSize);
            int z = Mathf.RoundToInt(worldPoint.z / cellSize);

            Debug.Log($"[TerrainManager] ModifyHeight called: Input {worldPoint} -> Grid [{x}, {z}]. Amount: {amount}");

            if (x >= 0 && x < gridSize.x && z >= 0 && z < gridSize.y)
            {
                float currentHeight = heightMap[x, z];
                float currentBedrock = bedrockMap[x, z];
                float payLimit = payLayerLimitMap[x, z];

                // Prevent digging below bedrock
                if (currentHeight + amount < currentBedrock)
                {
                    amount = currentBedrock - currentHeight; 
                    if (Mathf.Abs(amount) < 0.001f) return data; // Hit bottom, no dirt removed
                }

                // If removing dirt (negative amount implies adding digging, wait. 
                // In MineJob: ModifyHeight(targetPos, -0.5f). So negative amount is digging.
                // But "amount" passed is negative. 
                // So "removed amount" strictly positive is -amount.
                
                if (amount < 0) // Digging
                {
                    float digAmount = -amount;
                    float heightAfterDig = currentHeight - digAmount;

                    // Calculate Layers
                    // Range [heightAfterDig, currentHeight]
                    // Limit is payLimit.
                    
                    // Case 1: All above pay limit (Top Soil)
                    if (heightAfterDig >= payLimit)
                    {
                        data.TopSoil = digAmount;
                        data.PayLayer = 0;
                    }
                    // Case 2: All below pay limit (Pay Layer)
                    else if (currentHeight <= payLimit)
                    {
                        data.TopSoil = 0;
                        data.PayLayer = digAmount;
                    }
                    // Case 3: Crossing the boundary
                    else 
                    {
                        // Some top soil, some pay layer
                        data.TopSoil = currentHeight - payLimit;
                        data.PayLayer = payLimit - heightAfterDig;
                    }
                }
                else // Adding dirt (Dropping)
                {
                    // For now, dropping just adds "Top Soil" effectively, or just generic dirt.
                    // We don't really track richness of placed dirt in the map yet.
                    // Just accept it.
                    // data returned is 0 because we didn't harvest anything.
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
            return data;
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

        public void RegisterBuilding(Vector3Int coord, GameObject building)
        {
             if (!occupiedTiles.ContainsKey(coord)) occupiedTiles[coord] = building;
        }

        public bool IsTileOccupied(Vector3Int coord) => occupiedTiles.ContainsKey(coord);

        public bool TryGetTile(Vector3Int coord, out TileType type)
        {
            if (coord.x >= 0 && coord.x < gridSize.x && coord.z >= 0 && coord.z < gridSize.y)
            {
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
        
        public List<Vector3Int> GetRemovedTiles() => new List<Vector3Int>();
        public void RestoreRemovedTiles(List<Vector3Int> list) { }
        public bool IsAdjacentToWater(Vector3Int coord) => false; 
    }
}
