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
        [SerializeField] private int meshSubdivisions = 8; // Increased for smoothness
        
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
        private List<Vector3> verts = new List<Vector3>(160000);
        private List<int> tris = new List<int>(240000);
        private List<Vector2> uvs = new List<Vector2>(160000);
        private List<Color> colors = new List<Color>(160000);

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
                
                if (waterMaterial != null) 
                {
                    // Create a clone to avoid modifying the asset
                    waterMaterial = new Material(waterMaterial);
                    
                    // Fix Shader: Standard URP Lit does not support Vertex Alpha transparency.
                    // We swap to Particles/Lit for better edge fading if we detect specific "hard" shaders.
                    if (waterMaterial.shader.name == "Universal Render Pipeline/Lit" || 
                        waterMaterial.shader.name == "Universal Render Pipeline/Simple Lit")
                    {
                        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
                        if (particleShader != null)
                        {
                            waterMaterial.shader = particleShader;
                            waterMaterial.SetFloat("_Surface", 1.0f); // Transparent
                            waterMaterial.SetFloat("_Blend", 0.0f);   // Alpha
                            // Re-apply base color to the new shader property if needed
                             if (waterMaterial.HasProperty("_BaseColor")) 
                                waterMaterial.SetColor("_BaseColor", waterMaterial.GetColor("_BaseColor"));
                        }
                    }
                    waterMeshRenderer.material = waterMaterial;
                }
                else 
                {
                     // Create a default material if none exists
                     // Prefer Particle shaders as they support Vertex Colors for alpha fading
                     Shader shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
                     if (!shader) shader = Shader.Find("Particles/Standard Surface");
                     if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");
                     if (!shader) shader = Shader.Find("Standard");
                     
                     waterMaterial = new Material(shader);
                     waterMaterial.color = new Color(0.2f, 0.5f, 0.9f, 0.7f);
                     
                     // Ensure Surface type is set to Transparent for URP Particles
                     if (shader.name.Contains("Universal"))
                     {
                         waterMaterial.SetFloat("_Surface", 1.0f); // Transparent
                         waterMaterial.SetFloat("_Blend", 0.0f);   // Alpha
                         waterMaterial.SetColor("_BaseColor", waterMaterial.color);
                     }
                     
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
               
                // Fill river bed based on terrain height
               // Match the terrain river width (approx 5.0f radius)
               int scanRadius = 6;
               int startX = Mathf.FloorToInt(centerX - scanRadius);
               int endX = Mathf.CeilToInt(centerX + scanRadius);
               
               float targetWaterSurface = 1.5f; // Initial river level

               for (int x = startX; x <= endX; x++)
               {
                   if (x >= 0 && x < gridSize.x)
                   {
                        float terrainHeight = terrainManagerCache.GetHeight(x, z);
                        float depth = targetWaterSurface - terrainHeight;
                        
                        if (depth > 0)
                        {
                            waterMapRead[x, z] = depth;
                        }
                        else
                        {
                            waterMapRead[x, z] = 0f;
                        }
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
            colors.Clear();

            // Local ref for speed
            float[,] map = waterMapRead;
            
            // Pre-calculate Effective Water Surface Height
            // This prevents the water mesh from "climbing" the banks.
            // Wet nodes = Terrain + Depth
            // Dry Coast nodes = Average breadth of neighbors.
            float[,] surfaceLevels = new float[size.x, size.y];
            bool[,] isWet = new bool[size.x, size.y];

            // Pass 1: Identify Wet/Dry and Basic Heights
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    float d = map[x, z];
                    if (d > 0.001f)
                    {
                        isWet[x, z] = true;
                        surfaceLevels[x, z] = terrainManagerCache.GetHeight(x, z) + d;
                    }
                    else
                    {
                        isWet[x, z] = false;
                        surfaceLevels[x, z] = -999f; // Mark as undefined
                    }
                }
            }

            // Pass 2: Extrapolate to Coast
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    if (!isWet[x, z])
                    {
                        // Check neighbors
                        float sum = 0;
                        int count = 0;

                        void Check(int nx, int nz)
                        {
                            if (nx >= 0 && nx < size.x && nz >= 0 && nz < size.y && isWet[nx, nz])
                            {
                                sum += surfaceLevels[nx, nz];
                                count++;
                            }
                        }

                        Check(x + 1, z);
                        Check(x - 1, z);
                        Check(x, z + 1);
                        Check(x, z - 1);

                        if (count > 0)
                        {
                            surfaceLevels[x, z] = sum / count;
                        }
                        else
                        {
                            // Far from water, just clip it way down or keep at terrain (irrelevant as alpha will be 0)
                            // Keeping it at terrain height - bias might help Z-cull?
                            // Let's just put it at terrain height but slightly below to avoid z-fighting
                            surfaceLevels[x, z] = terrainManagerCache.GetHeight(x, z) - 1.0f;
                        }
                    }
                }
            }

            // Cache for vertex indices to support welding
            int subWidth = size.x * meshSubdivisions + 1;
            int subHeight = size.y * meshSubdivisions + 1;
            int[] vertexIndices = new int[subWidth * subHeight];
            System.Array.Fill(vertexIndices, -1);

            // Helper for interpolation
            // We now interpolate the PRE-CALCULATED Surface Levels
            float GetInterpHeight(float x, float z)
            {
                int x0 = Mathf.FloorToInt(x);
                int z0 = Mathf.FloorToInt(z);
                int x1 = Mathf.Min(x0 + 1, size.x - 1);
                int z1 = Mathf.Min(z0 + 1, size.y - 1);

                float tx = x - x0;
                float tz = z - z0;

                // Linear Interpolation for Flat Surface Extension
                float v00 = surfaceLevels[x0, z0];
                float v10 = surfaceLevels[x1, z0];
                float v01 = surfaceLevels[x0, z1];
                float v11 = surfaceLevels[x1, z1];

                return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), tz);
            }

            float GetInterpDepth(float x, float z)
            {
                int x0 = Mathf.FloorToInt(x);
                int z0 = Mathf.FloorToInt(z);
                int x1 = Mathf.Min(x0 + 1, size.x - 1);
                int z1 = Mathf.Min(z0 + 1, size.y - 1);

                float tx = x - x0;
                float tz = z - z0;
                
                // Keep smoothstep for depth/alpha to look nice? 
                // Matching linearity with height might be safer to prevent alpha popping.
                // Let's use Linear.
                
                float v00 = map[x0, z0];
                float v10 = map[x1, z0];
                float v01 = map[x0, z1];
                float v11 = map[x1, z1];

                return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), tz);
            }

            // Local helper to Get or Create Vertex
            int GetOrCreateVertex(int gx, int gz, float fx, float fz)
            {
                int flatIndex = gz * subWidth + gx;
                if (vertexIndices[flatIndex] != -1) return vertexIndices[flatIndex];

                // Data
                float h = GetInterpHeight(fx, fz);
                float w = GetInterpDepth(fx, fz);
                
                // Vertex Colors (Alpha Fade based on depth)
                float fadeDepth = 0.5f;
                float alpha = Mathf.Clamp01(w / fadeDepth);

                int newIndex = verts.Count;
                verts.Add(new Vector3(fx * cellSize, h, fz * cellSize));
                colors.Add(new Color(1, 1, 1, alpha));
                uvs.Add(new Vector2(fx * uvScale, fz * uvScale));
                
                vertexIndices[flatIndex] = newIndex;
                return newIndex;
            }

            for (int x = 0; x < size.x - 1; x++)
            {
                for (int z = 0; z < size.y - 1; z++)
                {
                    // Optimization: Check if whole cell is dry at main corners first
                    if (map[x, z] <= minWaterHeight && map[x + 1, z] <= minWaterHeight && 
                        map[x, z + 1] <= minWaterHeight && map[x + 1, z + 1] <= minWaterHeight)
                        continue;

                    // Subdivide
                    for (int sx = 0; sx < meshSubdivisions; sx++)
                    {
                        for (int sz = 0; sz < meshSubdivisions; sz++)
                        {
                            // Integer coordinates in the subdivided grid
                            int gx0 = x * meshSubdivisions + sx;
                            int gz0 = z * meshSubdivisions + sz;
                            int gx1 = gx0 + 1;
                            int gz1 = gz0 + 1;

                            // Float coordinates in world grid space
                            float fx0 = (float)gx0 / meshSubdivisions;
                            float fz0 = (float)gz0 / meshSubdivisions;
                            float fx1 = (float)gx1 / meshSubdivisions;
                            float fz1 = (float)gz1 / meshSubdivisions;

                            int v00 = GetOrCreateVertex(gx0, gz0, fx0, fz0);
                            int v10 = GetOrCreateVertex(gx1, gz0, fx1, fz0);
                            int v01 = GetOrCreateVertex(gx0, gz1, fx0, fz1);
                            int v11 = GetOrCreateVertex(gx1, gz1, fx1, fz1);

                            tris.Add(v00);
                            tris.Add(v01);
                            tris.Add(v10);
                            
                            tris.Add(v10);
                            tris.Add(v01);
                            tris.Add(v11);
                        }
                    }
                }
            }

            // Generate Skirts 
            // Simplified Skirts: Just drop down from current edge vertices
            // Note: Since we manipulate edge heights now, standard skirts are safer.
            float skirtDepth = -2.0f;
            
            void AddWaterSkirt(int x1, int z1, int x2, int z2)
            {
                 if (map[x1, z1] <= minWaterHeight && map[x2, z2] <= minWaterHeight) return;
                 
                 // Get absolute height from SurfaceMap
                 float h1 = surfaceLevels[x1, z1];
                 float h2 = surfaceLevels[x2, z2];
                 
                 // If we successfully hid the dry nodes, looking up surfaceLevels for skirts is correct.

                 int idx = verts.Count;
                 verts.Add(new Vector3(x1 * cellSize, h1, z1 * cellSize));        
                 verts.Add(new Vector3(x2 * cellSize, h2, z2 * cellSize));        
                 verts.Add(new Vector3(x1 * cellSize, skirtDepth, z1 * cellSize)); 
                 verts.Add(new Vector3(x2 * cellSize, skirtDepth, z2 * cellSize)); 

                 colors.Add(new Color(1, 1, 1, 1));
                 colors.Add(new Color(1, 1, 1, 1));
                 colors.Add(new Color(1, 1, 1, 1));
                 colors.Add(new Color(1, 1, 1, 1));

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
                waterMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
                if (waterMeshFilter != null) waterMeshFilter.mesh = waterMesh;
            }

            waterMesh.Clear();
            waterMesh.SetVertices(verts);
            waterMesh.SetTriangles(tris, 0);
            waterMesh.SetUVs(0, uvs);
            waterMesh.SetColors(colors); 
            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();
        }
    }
}
