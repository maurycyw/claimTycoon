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

        [Header("Vegetation & Rocks")]
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] rockPrefabs;
        [SerializeField] private GameObject bushPrefab;
        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private GameObject mushroomPrefab;
        [SerializeField] private GameObject branchPrefab;

        // Adjustable generation settings
        [SerializeField] private float treeDensity = 0.05f;
        [SerializeField] private float rockDensity = 0.02f;
        [SerializeField] private float bushDensity = 0.1f;
        [SerializeField] private float grassDensity = 0.3f;
        [SerializeField] private float mushroomDensity = 0.05f;
        [SerializeField] private float branchDensity = 0.05f;
        
        [Header("Vegetation Scale")]
        [SerializeField] private Vector2 treeScaleRange = new Vector2(0.8f, 1.4f); // Resized trees
        [SerializeField] private Vector2 rockScaleRange = new Vector2(0.3f, 0.6f);
        [SerializeField] private Vector2 bushScaleRange = new Vector2(0.5f, 0.9f);
        [SerializeField] private Vector2 grassScaleRange = new Vector2(0.4f, 0.6f);
        [SerializeField] private Vector2 mushroomScaleRange = new Vector2(0.5f, 1.0f);
        [SerializeField] private Vector2 branchScaleRange = new Vector2(0.5f, 1.0f);



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
        private Dictionary<Vector3Int, GameObject> natureTiles = new Dictionary<Vector3Int, GameObject>();
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

        private void Update()
        {
             // FORCE Shadow Casting OFF every frame to debug persistence
             if (GetComponent<MeshRenderer>().shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
             {
                 GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
             }
        }
        
        private void GenerateInitialTerrain()
        {
            heightMap = new float[gridSize.x, gridSize.y];
            bedrockMap = new float[gridSize.x, gridSize.y];
            payLayerLimitMap = new float[gridSize.x, gridSize.y];
            vegetationLimitMap = new float[gridSize.x, gridSize.y];
            natureTiles.Clear();


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

                    // River & Mountain Logic
                    float curveOffset = Mathf.Sin(z * 0.1f) * 3f; 
                    float centerX = 25f + curveOffset; 
                    float distFromCenter = Mathf.Abs(x - centerX);
                    
                    // Mountain Generation (Valley Effect)
                    // Rise up as we get further from river banks (approx dist 5)
                    float mountainStartDist = 5.5f;
                    if (distFromCenter > mountainStartDist)
                    {
                        float dist = distFromCenter - mountainStartDist;
                        // Exponential rise for mountainous feel
                        float zGrowthMod = Mathf.Lerp(0.4f, 1.0f, (float)z / gridSize.y);
                        surfaceHeight += Mathf.Pow(dist, 1.5f) * 0.0078125f * zGrowthMod;
                    }

                    float riverBedHalfWidth = 2.0f; 
                    float innerBankWidth = 1.0f;
                    float middleBankWidth = 1.0f;
                    float outerBankWidth = 1.0f;
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

                        // Determine the theoretical river bank height
                        // Blend from 0.0f (River Bottom) to surfaceHeight (Original Noise)
                        
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

            GenerateVegetation(); // Add this call
            UpdateMesh();
        }

        private void GenerateVegetation()
        {
            // Simple random/noise based generation
            float seed = Random.Range(0f, 1000f);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    // Add random thinning to prevent solid blocks of trees
                    // Even if noise says "Tree Here", we act like 50% chance to skip
                    if (Random.value < 0.6f) continue; 

                    // Basic Rules:

                    // 1. Not in Water (Check River Distance or Height vs RiverBed)
                    // 2. Not where Buildings are (though none yet)
                    
                    // Simple check using logic from GenerateInitialTerrain river calculation?
                    // Better: Check if surfaceHeight > some value relative to expected water level or bank logic.
                    // For now, let's use the limit maps or just distance from river center if we can recall it.
                    // We don't have river center stored easily. Let's re-calculate or check "IsAdjacentToWater"?
                    // Actually, let's just use the fact that water is usually set? No water is separate.
                    // We can re-calc distance from center:
                    float curveOffset = Mathf.Sin(z * 0.1f) * 3f;
                    float centerX = 25f + curveOffset;
                    float distFromCenter = Mathf.Abs(x - centerX);
                    
                    // River Bed ~3 width + Banks. Let's say safe zone > 6.
                    if (distFromCenter < 6.0f) continue; // Too close to river

                    // Noise for placement
                    float vNoise = Mathf.PerlinNoise(x * 0.1f + seed, z * 0.1f + seed);
                    float rNoise = Mathf.PerlinNoise(x * 0.15f + seed + 50f, z * 0.15f + seed + 50f);
                    float gNoise = Mathf.PerlinNoise(x * 0.2f + seed + 100f, z * 0.2f + seed + 100f); // Grass
                    float mNoise = Mathf.PerlinNoise(x * 0.25f + seed + 150f, z * 0.25f + seed + 150f); // Mushroom/Branch mix

                    Vector3Int coord = new Vector3Int(x, 0, z);
                    Vector3 pos = new Vector3(x * cellSize, heightMap[x, z], z * cellSize);
                    
                    GameObject prefabToSpawn = null;
                    NatureType nType = NatureType.Bush;

                    // Trees
                    if (vNoise > (1f - treeDensity)) 
                    {
                        if (treePrefabs != null && treePrefabs.Length > 0)
                            prefabToSpawn = treePrefabs[Random.Range(0, treePrefabs.Length)];
                        nType = NatureType.Tree;
                    }
                    // Rocks (Independent chance)
                    else if (rNoise > (1f - rockDensity))
                    {
                        if (rockPrefabs != null && rockPrefabs.Length > 0)
                            prefabToSpawn = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                        nType = NatureType.Rock;
                    }
                    // Bushes
                    else if (vNoise > (1f - bushDensity))
                    {
                         prefabToSpawn = bushPrefab;
                         nType = NatureType.Bush;
                    }
                    // Mushrooms / Branches (Small Details)
                    else if (mNoise > (1f - mushroomDensity)) 
                    {
                        prefabToSpawn = mushroomPrefab;
                        nType = NatureType.Mushroom;
                    }
                    else if (mNoise < branchDensity) // Use lower end of noise for branches? Or separate?
                    {
                        prefabToSpawn = branchPrefab;
                        nType = NatureType.Branch;
                    }
                    // Grass (High density, fills gaps)
                    // Only if nothing else spawned
                    else if (gNoise > (1f - grassDensity)) 
                    {
                        if (grassPrefabs != null && grassPrefabs.Length > 0)
                            prefabToSpawn = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
                        nType = NatureType.Grass;
                    }

                    if (prefabToSpawn == null && (nType == NatureType.Tree || nType == NatureType.Rock)) 
                    {
                         // Generate Primitive Fallback if strictly needed?
                         // For now only if "Should spawn" was true. 
                         // Check fields. If null, create primitive.
                         if (nType == NatureType.Tree && (treePrefabs == null || treePrefabs.Length == 0)) SpawnPrimitive(pos, coord, nType);
                         else if (nType == NatureType.Rock && (rockPrefabs == null || rockPrefabs.Length == 0)) SpawnPrimitive(pos, coord, nType);
                         else if (nType == NatureType.Grass && (grassPrefabs == null || grassPrefabs.Length == 0)) { /* Do nothing for missing grass */ }
                         else if (prefabToSpawn != null) SpawnObject(prefabToSpawn, pos, coord, nType);
                    }
                    else if (prefabToSpawn != null)
                    {
                        SpawnObject(prefabToSpawn, pos, coord, nType);
                    }
                }
            }
        }

        private void SpawnObject(GameObject prefab, Vector3 pos, Vector3Int coord, NatureType type)
        {
            if (natureTiles.ContainsKey(coord)) return; 
            
            // Random Y Rotation
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            GameObject obj = Instantiate(prefab, pos, rot);
            obj.transform.SetParent(this.transform);
            
            // Apply Random Scale
            float scaleVal = 1f;
            switch(type)
            {
                case NatureType.Tree: scaleVal = Random.Range(treeScaleRange.x, treeScaleRange.y); break;
                case NatureType.Rock: scaleVal = Random.Range(rockScaleRange.x, rockScaleRange.y); break;
                case NatureType.Bush: scaleVal = Random.Range(bushScaleRange.x, bushScaleRange.y); break;
                case NatureType.Grass: scaleVal = Random.Range(grassScaleRange.x, grassScaleRange.y); break;
                case NatureType.Mushroom: scaleVal = Random.Range(mushroomScaleRange.x, mushroomScaleRange.y); break;
                case NatureType.Branch: scaleVal = Random.Range(branchScaleRange.x, branchScaleRange.y); break;
            }
            obj.transform.localScale = Vector3.one * scaleVal;

            // Add NatureItem component if missing
            NatureItem item = obj.GetComponent<NatureItem>();
            if (item == null) item = obj.AddComponent<NatureItem>();
            item.SetType(type);
            
            natureTiles[coord] = obj;
        }


        private void SpawnPrimitive(Vector3 pos, Vector3Int coord, NatureType type)
        {
            if (natureTiles.ContainsKey(coord)) return;

            GameObject obj = null;
            if (type == NatureType.Tree)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
                obj.name = "Tree_Primitive";
                // Add leaves?
                GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves.transform.SetParent(obj.transform);
                leaves.transform.localPosition = new Vector3(0, 1f, 0);
                leaves.transform.localScale = new Vector3(2f, 1f, 2f);
                leaves.GetComponent<Renderer>().material.color = Color.green;
            }
            else if (type == NatureType.Rock)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                obj.name = "Rock_Primitive";
                obj.GetComponent<Renderer>().material.color = Color.gray;
            }
            
            if (obj != null)
            {
                // Fix Pivot offset for primitives (they center at 0)
                // We want base at 'pos'.
                // Cylinder height 2, center 0 -> bottom is at -1. So move up by 1 * localScale.y?
                // Actually typical primitives, center is center.
                // Tree: Height 2 scaled. extent 1. so move up 1.
                // Rock: Radius 0.5. move up 0.5.
                
                float pivotOffset = (type == NatureType.Tree) ? 1.0f : 0.5f; 
                obj.transform.position = pos + Vector3.up * pivotOffset; 
                
                obj.transform.SetParent(this.transform);
                 
                NatureItem item = obj.AddComponent<NatureItem>();
                item.SetType(type);
                
                natureTiles[coord] = obj;
            }
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


        private Texture2D terrainTexture;

        public void UpdateMesh()
        {
            // Update Texture (High Resolution for Smooth Curves)
            if (terrainTexture != null) Destroy(terrainTexture);
            
            // DEBUG: Solid Color Texture REMOVED. Restoring normal generation.
            terrainTexture = MeshGenerator.GenerateTerrainTexture(heightMap, payLayerLimitMap, vegetationLimitMap, cellSize);
            GetComponent<MeshRenderer>().material.mainTexture = terrainTexture;
            
            // Restore Receive Shadows
            GetComponent<MeshRenderer>().receiveShadows = true; 
            
            // Fix "Ring" Artifacts (Shadow Acne):
            // The terrain is casting shadows on itself, causing concentric rings.
            // Since the terrain is mostly flat/rolling, we don't strictly need it to cast shadows on itself.
            // We disable Casting, but keep Receiving (for trees/units).
            GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            // Ensure Vertex Colors don't tint the texture weirdly (MeshGen sets them to White)
            GetComponent<MeshRenderer>().material.color = Color.white;
            
            // Matte finish to prevent specular ringing
            GetComponent<MeshRenderer>().material.SetFloat("_Glossiness", 0.0f);
            GetComponent<MeshRenderer>().material.SetFloat("_Metallic", 0.0f);

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
                    
                    // Separate Material for Bedrock (Gray, No Texture)
                    MeshRenderer bedrockRen = bedrockObj.AddComponent<MeshRenderer>();
                    // Fix Ring Artifacts: Ensure Bedrock underneath doesn't cast shadows upwards
                    bedrockRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    
                    Material baseMat = GetComponent<MeshRenderer>().sharedMaterial;
                    Material bedrockMat = new Material(baseMat);
                    bedrockMat.mainTexture = null;
                    bedrockMat.color = Color.gray;
                    bedrockRen.material = bedrockMat;
                } else {
                    bedrockObj = bedrockTrans.gameObject;
                    // Ensure existing bedrock also has shadows off
                    MeshRenderer existingRen = bedrockObj.GetComponent<MeshRenderer>();
                    if (existingRen != null) existingRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

                // Update Nature Item Position
                Vector3Int coord = new Vector3Int(x, 0, z);
                if (natureTiles.ContainsKey(coord))
                {
                    GameObject natureObj = natureTiles[coord];
                    if (natureObj != null)
                    {
                         // Lower it
                         Vector3 newPos = natureObj.transform.position;
                         // Be careful with primitives vs prefabs and pivots.
                         // Primitives were shifted up. Prefabs likely pivot at bottom.
                         // Re-calculate based on known offset or delta?
                         // Simplest: Just apply delta 'amount' to current y.
                         // BUT: ModifyHeight adds 'amount'. If amount is negative (dig), we add negative.
                         newPos.y += amount; 
                         natureObj.transform.position = newPos;
                    }
                }
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
