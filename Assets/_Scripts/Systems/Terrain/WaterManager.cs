using UnityEngine;
using System.Collections.Generic;

namespace ClaimTycoon.Systems.Terrain
{
    [RequireComponent(typeof(MeshRenderer))] // Optional: Can just hold material reference
    public class WaterManager : MonoBehaviour
    {
        public static WaterManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float flowRate = 0.05f; // Reduced from 0.1f
        [SerializeField] private float minWaterHeight = 0.01f;
        [SerializeField] private Material waterMaterial; // explicitly assign here
        [SerializeField] private int preWarmIterations = 500;
        
        [SerializeField] private float uvScale = 0.1f;
        [SerializeField] private Vector2 waterScrollVelocity = new Vector2(0, -0.5f);
        [SerializeField] private bool generateNoiseTexture = true;
        [SerializeField] private float particleEmissionRate = 20f;

        // Double Buffering
        private float[,] waterMapRead;
        private float[,] waterMapWrite;
        
        private Mesh waterMesh;
        private float tickTimer;
        
        private GameObject waterMeshObject;
        private MeshFilter waterMeshFilter;
        private MeshRenderer waterMeshRenderer;
        
        private ParticleSystem foamParticleSystem;

        // Reusable Lists to avoid GC
        private List<Vector3> verts = new List<Vector3>(4000);
        private List<int> tris = new List<int>(6000);
        private List<Vector2> uvs = new List<Vector2>(4000);

        private TerrainManager terrainManagerCache;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            InitializeWater();
        }

        private void InitializeWater()
        {
            // Cache reference
            terrainManagerCache = TerrainManager.Instance;

            // Create Child Object for rendering
            if (waterMeshObject == null)
            {
                waterMeshObject = new GameObject("WaterVisuals");
                waterMeshObject.transform.SetParent(this.transform);
                waterMeshObject.transform.localPosition = Vector3.zero;
                
                waterMeshFilter = waterMeshObject.AddComponent<MeshFilter>();
                waterMeshRenderer = waterMeshObject.AddComponent<MeshRenderer>();
                
                if (waterMaterial != null) waterMeshRenderer.material = waterMaterial;
                else if (GetComponent<MeshRenderer>() != null) waterMeshRenderer.material = GetComponent<MeshRenderer>().sharedMaterial;
                else 
                {
                     // Create a default material if none exists
                     Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                     if (!shader) shader = Shader.Find("Standard");
                     waterMaterial = new Material(shader);
                     waterMaterial.color = new Color(0.2f, 0.5f, 0.9f, 0.7f);
                     waterMeshRenderer.material = waterMaterial;
                }
                
                ConfigureTransparency();
                if (generateNoiseTexture) GenerateWaterTexture();
                SetupParticles();
            }

            if (terrainManagerCache == null) return;
            Vector2Int gridSize = terrainManagerCache.GridSize;
            
            // Initialize Arrays
            waterMapRead = new float[gridSize.x, gridSize.y];
            waterMapWrite = new float[gridSize.x, gridSize.y];

            // Initial River (Center at 6-8)
            // Initial River Water
            for (int z = 0; z < gridSize.y; z++)
            {
               float curveOffset = Mathf.Sin(z * 0.1f) * 3f;
               float centerX = 25f + curveOffset;
               
               // Fill roughly the river bed width (Radius 2 around center)
               int startX = Mathf.FloorToInt(centerX - 2f);
               int endX = Mathf.CeilToInt(centerX + 2f);
               
               for (int x = startX; x <= endX; x++)
               {
                   if (x >= 0 && x < gridSize.x)
                   {
                        waterMapRead[x, z] = 1.8f;
                        // Write buffer will be synced in SimulateFlow (or initial copy)
                   }
               }
            }
            
            // Initial Sync
            System.Array.Copy(waterMapRead, waterMapWrite, waterMapRead.Length);

            // Pre-warm the water simulation so it starts full
            for(int i = 0; i < preWarmIterations; i++)
            {
                SimulateFlow();
            }

            UpdateWaterMesh();
        }

        private void ConfigureTransparency()
        {
            if (waterMeshRenderer == null) return;
            Material mat = waterMeshRenderer.material;
            if (mat != null)
            {
                // Attempt to configure Standard Shader for Transparency
                mat.SetFloat("_Mode", 3); 
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                // Ensure Alpha is translucent
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    if (color.a > 0.8f) 
                    {
                        color.a = 0.6f;
                        mat.color = color;
                    }
                }
                else if (mat.HasProperty("_BaseColor")) // URP often uses _BaseColor
                {
                     Color color = mat.GetColor("_BaseColor");
                     if (color.a > 0.8f) 
                    {
                        color.a = 0.6f;
                        mat.SetColor("_BaseColor", color);
                    }
                    
                    // URP Properties
                     if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
                     if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f); // Alpha
                }
            }
        }

        [Header("Debug")]
        [SerializeField] private bool simulateWater = true;

        private void Update()
        {
            // Update Texture Scrolling
            if (waterMeshRenderer != null && waterMeshRenderer.material != null)
            {
                Vector2 offset = waterMeshRenderer.material.mainTextureOffset;
                offset += waterScrollVelocity * Time.deltaTime;
                waterMeshRenderer.material.mainTextureOffset = offset;
            }

            if (!simulateWater) return;

            tickTimer += Time.deltaTime;
            if (tickTimer > 0.1f) 
            {
                SimulateFlow();
                UpdateWaterMesh();
                tickTimer = 0;
            }
        }

        private void GenerateWaterTexture()
        {
            if (waterMeshRenderer == null || waterMeshRenderer.material == null) return;
            if (waterMeshRenderer.material.mainTexture != null) return; // Don't overwrite if exists

            int width = 256;
            int height = 256;
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            float scale = 10f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xCoord = (float)x / width * scale;
                    float yCoord = (float)y / height * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    // Make it subtle white variations but higher alpha for visibility
                    pixels[y * width + x] = new Color(1, 1, 1, 0.5f + (sample * 0.4f)); 
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            waterMeshRenderer.material.mainTexture = texture;
            if (waterMeshRenderer.material.HasProperty("_BaseMap")) waterMeshRenderer.material.SetTexture("_BaseMap", texture);
        }

        private void SetupParticles()
        {
            // Create Particle System for Foam/White Water
            GameObject particleObj = new GameObject("FoamParticles");
            particleObj.transform.SetParent(this.transform);
            particleObj.transform.localPosition = new Vector3(8f, 2f, 8f); 
            particleObj.transform.localRotation = Quaternion.identity; 

            foamParticleSystem = particleObj.AddComponent<ParticleSystem>();
            var main = foamParticleSystem.main;
            main.startColor = new Color(1, 1, 1, 0.6f);
            main.startSize = 0.3f;
            main.startLifetime = 2f;
            main.startSpeed = 0f; 
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = foamParticleSystem.emission;
            emission.rateOverTime = particleEmissionRate;

            var shape = foamParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.1f, 50f); 
            particleObj.transform.localPosition = new Vector3(25f, 1.85f, 25f);

            var velocityOverLifetime = foamParticleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            // Flow along negative Z
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-2f, -1f); 
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); 
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0f);

            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit")); 
            renderer.material.SetColor("_Color", new Color(1,1,1,0.5f));
            renderer.sortMode = ParticleSystemSortMode.Distance;
            
             Texture2D particleTex = new Texture2D(32, 32);
             for(int x=0; x<32; x++) {
                 for(int y=0; y<32; y++) {
                     float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                     float alpha = Mathf.Clamp01(1.0f - dist/16f);
                     particleTex.SetPixel(x,y, new Color(1,1,1, alpha));
                 }
             }
             particleTex.Apply();
             renderer.material.mainTexture = particleTex;
        }

        private void SimulateFlow()
        {
             if (waterMapRead == null)
             {
                 InitializeWater();
                 if (waterMapRead == null) return;
             }
             
             if (terrainManagerCache == null)
             {
                 terrainManagerCache = TerrainManager.Instance;
                 if (terrainManagerCache == null) return;
             }
             
            Vector2Int size = terrainManagerCache.GridSize;
            
            // Double Buffering: Copy Read to Write (Fast Block Copy)
            System.Array.Copy(waterMapRead, waterMapWrite, waterMapRead.Length);

            // Using local references for speed
            float[,] readMap = waterMapRead;
            float[,] writeMap = waterMapWrite;

            void AttemptFlow(int x, int z, int nx, int nz)
            {
                if (nx < 0 || nx >= size.x || nz < 0 || nz >= size.y) return;
                
                // Read from ReadMap + Terrain
                float currentHeight = terrainManagerCache.GetHeight(x, z) + readMap[x, z];
                float neighborHeight = terrainManagerCache.GetHeight(nx, nz) + readMap[nx, nz];

                if (currentHeight > neighborHeight)
                {
                    float transfer = (currentHeight - neighborHeight) * 0.5f * flowRate;
                    transfer = Mathf.Min(transfer, readMap[x, z]);
                    
                    // Write delta to WriteMap
                    writeMap[x, z] -= transfer;
                    writeMap[nx, nz] += transfer;
                }
            }
            
            // Replenish Logic
             for (int z = 0; z < size.y; z++) 
            {
               float curveOffset = Mathf.Sin(z * 0.1f) * 3f;
               float centerX = 25f + curveOffset;
               int cX = Mathf.RoundToInt(centerX);
               
               if (cX >= 0 && cX < size.x && writeMap[cX, z] < 1.8f) writeMap[cX, z] += flowRate * 2;
            }

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    if (readMap[x, z] <= minWaterHeight) continue;
                    AttemptFlow(x, z, x + 1, z);
                    AttemptFlow(x, z, x - 1, z);
                    AttemptFlow(x, z, x, z + 1);
                    AttemptFlow(x, z, x, z - 1);
                }
            }
            
            // Swap Buffers
            waterMapRead = writeMap;
            waterMapWrite = readMap;
        }

        public float GetWaterDepth(int x, int z)
        {
            if (waterMapRead == null) return 0f;
            if (x >= 0 && x < waterMapRead.GetLength(0) && z >= 0 && z < waterMapRead.GetLength(1))
            {
                return waterMapRead[x, z];
            }
            return 0f;
        }

        private void UpdateWaterMesh()
        {
            if (terrainManagerCache == null || waterMapRead == null) return;

            Vector2Int size = terrainManagerCache.GridSize;
            float cellSize = terrainManagerCache.CellSize;
            
            // Clear reusable lists
            verts.Clear();
            tris.Clear();
            uvs.Clear();

            // Local ref for speed
            float[,] map = waterMapRead;

            for (int x = 0; x < size.x - 1; x++)
            {
                for (int z = 0; z < size.y - 1; z++)
                {
                    float wBL = map[x, z];
                    float wBR = map[x + 1, z];
                    float wTL = map[x, z + 1];
                    float wTR = map[x + 1, z + 1];

                    if (wBL <= minWaterHeight && wBR <= minWaterHeight && wTL <= minWaterHeight && wTR <= minWaterHeight)
                        continue;

                    float hBL = terrainManagerCache.GetHeight(x, z) + wBL;
                    float hBR = terrainManagerCache.GetHeight(x + 1, z) + wBR;
                    float hTL = terrainManagerCache.GetHeight(x, z + 1) + wTL;
                    float hTR = terrainManagerCache.GetHeight(x + 1, z + 1) + wTR;

                    int startIndex = verts.Count;
                    verts.Add(new Vector3(x * cellSize, hBL, z * cellSize));         
                    verts.Add(new Vector3((x + 1) * cellSize, hBR, z * cellSize));   
                    verts.Add(new Vector3(x * cellSize, hTL, (z + 1) * cellSize));   
                    verts.Add(new Vector3((x + 1) * cellSize, hTR, (z + 1) * cellSize)); 

                    uvs.Add(new Vector2(x * uvScale, z * uvScale));
                    uvs.Add(new Vector2((x + 1) * uvScale, z * uvScale));
                    uvs.Add(new Vector2(x * uvScale, (z + 1) * uvScale));
                    uvs.Add(new Vector2((x + 1) * uvScale, (z + 1) * uvScale));

                    tris.Add(startIndex);
                    tris.Add(startIndex + 2);
                    tris.Add(startIndex + 1);
                    tris.Add(startIndex + 1);
                    tris.Add(startIndex + 2);
                    tris.Add(startIndex + 3);
                }
            }

            // Generate Skirts
            float skirtDepth = -0.5f;
            // int skirtQuads = 0; // Unused

            void AddWaterSkirt(int x1, int z1, int x2, int z2)
            {
                 if (map[x1, z1] <= minWaterHeight && map[x2, z2] <= minWaterHeight) return;
                 
                 // skirtQuads++; 
                 float h1 = terrainManagerCache.GetHeight(x1, z1) + map[x1, z1];
                 float h2 = terrainManagerCache.GetHeight(x2, z2) + map[x2, z2];

                 int idx = verts.Count;
                 verts.Add(new Vector3(x1 * cellSize, h1, z1 * cellSize));        
                 verts.Add(new Vector3(x2 * cellSize, h2, z2 * cellSize));        
                 verts.Add(new Vector3(x1 * cellSize, skirtDepth, z1 * cellSize)); 
                 verts.Add(new Vector3(x2 * cellSize, skirtDepth, z2 * cellSize)); 

                 uvs.Add(new Vector2(0, 1));
                 uvs.Add(new Vector2(1, 1));
                 uvs.Add(new Vector2(0, 0));
                 uvs.Add(new Vector2(1, 0));

                 tris.Add(idx + 2); tris.Add(idx); tris.Add(idx + 1);
                 tris.Add(idx + 2); tris.Add(idx + 1); tris.Add(idx + 3);
            }

            for (int x = 0; x < size.x - 1; x++) AddWaterSkirt(x + 1, 0, x, 0); 
            for (int z = 0; z < size.y - 1; z++) AddWaterSkirt(size.x - 1, z, size.x - 1, z + 1);
            for (int x = size.x - 1; x > 0; x--) AddWaterSkirt(x, size.y - 1, x - 1, size.y - 1);
            for (int z = size.y - 1; z > 0; z--) AddWaterSkirt(0, z, 0, z - 1);
            
            if (waterMesh == null)
            {
                waterMesh = new Mesh();
                waterMesh.name = "Water Mesh";
                if (waterMeshFilter != null) waterMeshFilter.mesh = waterMesh;
            }

            waterMesh.Clear();
            waterMesh.SetVertices(verts);
            waterMesh.SetTriangles(tris, 0);
            waterMesh.SetUVs(0, uvs);
            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();
        }
    }
}
