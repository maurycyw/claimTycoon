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

        private float[,] waterMap; 
        private Mesh waterMesh;
        private float tickTimer;
        
        private GameObject waterMeshObject;
        private MeshFilter waterMeshFilter;
        private MeshRenderer waterMeshRenderer;

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
            }

            if (TerrainManager.Instance == null) return;
            Vector2Int gridSize = TerrainManager.Instance.GridSize;
            waterMap = new float[gridSize.x, gridSize.y];

            // Initial River (Center at 6-8)
            for (int z = 0; z < gridSize.y; z++)
            {
               waterMap[6, z] = 1.8f;
               waterMap[7, z] = 1.8f;
               waterMap[8, z] = 1.8f;
            }
            
            UpdateWaterMesh();
        }

        [Header("Debug")]
        [SerializeField] private bool simulateWater = true;

        private void Update()
        {
            if (!simulateWater) return;

            tickTimer += Time.deltaTime;
            if (tickTimer > 0.1f) 
            {
                SimulateFlow();
                UpdateWaterMesh();
                tickTimer = 0;
            }
        }

        private void SimulateFlow()
        {
             if (TerrainManager.Instance == null) return;
             
            Vector2Int size = TerrainManager.Instance.GridSize;
            float[,] newWaterMap = (float[,])waterMap.Clone();

            void AttemptFlow(int x, int z, int nx, int nz)
            {
                if (nx < 0 || nx >= size.x || nz < 0 || nz >= size.y) return;
                float currentHeight = TerrainManager.Instance.GetHeight(x, z) + waterMap[x, z];
                float neighborHeight = TerrainManager.Instance.GetHeight(nx, nz) + waterMap[nx, nz];

                if (currentHeight > neighborHeight)
                {
                    float transfer = (currentHeight - neighborHeight) * 0.5f * flowRate;
                    transfer = Mathf.Min(transfer, waterMap[x, z]);
                    newWaterMap[x, z] -= transfer;
                    newWaterMap[nx, nz] += transfer;
                }
            }
            
            // Source
            for (int z = 0; z < size.y; z++) 
            {
                if (newWaterMap[6, z] < 1.8f) newWaterMap[6, z] += flowRate * 2;
                if (newWaterMap[7, z] < 1.8f) newWaterMap[7, z] += flowRate * 2;
                if (newWaterMap[8, z] < 1.8f) newWaterMap[8, z] += flowRate * 2;
            }

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    if (waterMap[x, z] <= minWaterHeight) continue;
                    AttemptFlow(x, z, x + 1, z);
                    AttemptFlow(x, z, x - 1, z);
                    AttemptFlow(x, z, x, z + 1);
                    AttemptFlow(x, z, x, z - 1);
                }
            }
            waterMap = newWaterMap;
        }

        private void UpdateWaterMesh()
        {
            if (TerrainManager.Instance == null) return;

            Vector2Int size = TerrainManager.Instance.GridSize;
            float cellSize = TerrainManager.Instance.CellSize;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            for (int x = 0; x < size.x - 1; x++)
            {
                for (int z = 0; z < size.y - 1; z++)
                {
                   float wBL = waterMap[x, z];
                    float wBR = waterMap[x + 1, z];
                    float wTL = waterMap[x, z + 1];
                    float wTR = waterMap[x + 1, z + 1];

                    if (wBL <= minWaterHeight && wBR <= minWaterHeight && wTL <= minWaterHeight && wTR <= minWaterHeight)
                        continue;

                    float hBL = TerrainManager.Instance.GetHeight(x, z) + wBL;
                    float hBR = TerrainManager.Instance.GetHeight(x + 1, z) + wBR;
                    float hTL = TerrainManager.Instance.GetHeight(x, z + 1) + wTL;
                    float hTR = TerrainManager.Instance.GetHeight(x + 1, z + 1) + wTR;

                    int startIndex = verts.Count;
                    verts.Add(new Vector3(x * cellSize, hBL, z * cellSize));         
                    verts.Add(new Vector3((x + 1) * cellSize, hBR, z * cellSize));   
                    verts.Add(new Vector3(x * cellSize, hTL, (z + 1) * cellSize));   
                    verts.Add(new Vector3((x + 1) * cellSize, hTR, (z + 1) * cellSize)); 

                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));

                    tris.Add(startIndex);
                    tris.Add(startIndex + 2);
                    tris.Add(startIndex + 1);
                    tris.Add(startIndex + 1);
                    tris.Add(startIndex + 2);
                    tris.Add(startIndex + 3);
                }
            }

            if (waterMesh == null)
            {
                waterMesh = new Mesh();
                waterMesh.name = "Water Mesh";
                // Assign to CHILD Filter
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
