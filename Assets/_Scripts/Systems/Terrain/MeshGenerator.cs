using UnityEngine;

namespace ClaimTycoon.Systems.Terrain
{
    public static class MeshGenerator
    {
        public static Mesh GenerateTerrainMesh(float[,] heightMap, float[,] payLimitMap, float[,] vegLimitMap, float cellSize, bool generateSkirt = true)
        {
            Debug.Log($"[MeshGenerator] Generating Terrain Mesh. Skirt: {generateSkirt}");
            int width = heightMap.GetLength(0);
            int depth = heightMap.GetLength(1);
            float skirtDepth = -0.5f;

            Mesh mesh = new Mesh();
            mesh.name = "ProceduralTerrain";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 

            int mainVertCount = width * depth;
            int perimeterEdges = 2 * (width - 1 + depth - 1);
            int skirtVertCount = generateSkirt ? perimeterEdges * 4 : 0;

            Vector3[] vertices = new Vector3[mainVertCount + skirtVertCount];
            Vector2[] uvs = new Vector2[mainVertCount + skirtVertCount];
            Color[] colors = new Color[mainVertCount + skirtVertCount]; // Vertex Colors
            int[] triangles = new int[(mainVertCount * 6) + (skirtVertCount * 6)];

            int vertIndex = 0;
            int triIndex = 0;

            // Colors
            Color vegetationColor = new Color(0.1f, 0.8f, 0.1f); // BRIGHT GREEN (Grass)
            Color topSoilColor = new Color(0.6f, 0.45f, 0.25f); // LIGHT BROWN (Regular Dirt)
            Color payLayerColor = new Color(0.5f, 0.25f, 0.1f); // DARK RED-BROWN (Pay Dirt)
            Color bedrockColor = Color.gray;

            // 1. Generate Main Terrain
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float y = heightMap[x, z];
                    float pLimit = (payLimitMap != null) ? payLimitMap[x, z] : -100f;
                    float vLimit = (vegLimitMap != null) ? vegLimitMap[x, z] : -100f;

                    vertices[vertIndex] = new Vector3(x * cellSize, y, z * cellSize);
                    uvs[vertIndex] = new Vector2((float)x / width, (float)z / depth);
                    
                    // Color Logic
                    if (y >= vLimit) 
                        colors[vertIndex] = vegetationColor;
                    else if (y >= pLimit)
                        colors[vertIndex] = topSoilColor;
                    else 
                        colors[vertIndex] = payLayerColor;

                    if (x < width - 1 && z < depth - 1)
                    {
                        // Main Grid Triangles (Clockwise)
                        int bl = vertIndex;
                        int br = vertIndex + 1;
                        int tl = vertIndex + width;
                        int tr = vertIndex + width + 1;
                        
                        triangles[triIndex] = bl; triangles[triIndex + 1] = tl; triangles[triIndex + 2] = br;
                        triangles[triIndex + 3] = br; triangles[triIndex + 4] = tl; triangles[triIndex + 5] = tr;
                        triIndex += 6;
                    }
                    vertIndex++;
                }
            }

            // 2. Generate Skirt
            if (generateSkirt)
            {
                void AddSkirtQuad(int x1, int z1, int x2, int z2)
                {
                    float y1 = heightMap[x1, z1];
                    float y2 = heightMap[x2, z2];
                    
                    Vector3 v1 = new Vector3(x1 * cellSize, y1, z1 * cellSize);        
                    Vector3 v2 = new Vector3(x2 * cellSize, y2, z2 * cellSize);        
                    Vector3 v3 = new Vector3(x1 * cellSize, skirtDepth, z1 * cellSize); 
                    Vector3 v4 = new Vector3(x2 * cellSize, skirtDepth, z2 * cellSize); 
                    
                    int currentVert = vertIndex;
                    float pLim1 = (payLimitMap != null) ? payLimitMap[x1, z1] : -100f;
                    float vLim1 = (vegLimitMap != null) ? vegLimitMap[x1, z1] : -100f;
                    Color c1 = (y1 >= vLim1) ? topSoilColor : ((y1 >= pLim1) ? topSoilColor : payLayerColor); 
                    // Note: Skirt top is usually the same as surface, but let's make it TopSoil color if it's vegetation, to imply grass on top only?
                    // User request: "Vegetation layer... very thin. then under it we have our 2 main types of soil".
                    // So side walls (skirt) immediately below surface should probably start as Top Soil or Vegetation?
                    // Let's keep it simple: If surface is Veg, skirt top is Veg too (grass hangs over edge) OR Top Soil.
                    // I will replicate surface color logic for continuity.
                    c1 = (y1 >= vLim1) ? vegetationColor : ((y1 >= pLim1) ? topSoilColor : payLayerColor);

                    vertices[vertIndex] = v1; uvs[vertIndex] = new Vector2(0, 1); colors[vertIndex] = c1; vertIndex++;
                    
                    float pLim2 = (payLimitMap != null) ? payLimitMap[x2, z2] : -100f;
                    float vLim2 = (vegLimitMap != null) ? vegLimitMap[x2, z2] : -100f;
                    Color c2 = (y2 >= vLim2) ? vegetationColor : ((y2 >= pLim2) ? topSoilColor : payLayerColor);

                    vertices[vertIndex] = v2; uvs[vertIndex] = new Vector2(1, 1); colors[vertIndex] = c2; vertIndex++;
                    
                    vertices[vertIndex] = v3; uvs[vertIndex] = new Vector2(0, 0); colors[vertIndex] = bedrockColor; vertIndex++;
                    vertices[vertIndex] = v4; uvs[vertIndex] = new Vector2(1, 0); colors[vertIndex] = bedrockColor; vertIndex++;
                    
                    triangles[triIndex] = currentVert + 2; triangles[triIndex + 1] = currentVert; triangles[triIndex + 2] = currentVert + 1;
                    triangles[triIndex + 3] = currentVert + 2; triangles[triIndex + 4] = currentVert + 1; triangles[triIndex + 5] = currentVert + 3;
                    triIndex += 6;
                }

                for (int x = 0; x < width - 1; x++) AddSkirtQuad(x, 0, x + 1, 0);
                for (int z = 0; z < depth - 1; z++) AddSkirtQuad(width - 1, z, width - 1, z + 1);
                for (int x = width - 1; x > 0; x--) AddSkirtQuad(x, depth - 1, x - 1, depth - 1);
                for (int z = depth - 1; z > 0; z--) AddSkirtQuad(0, z, 0, z - 1);
            }

            if (triIndex < triangles.Length)
            {
                 int[] finalTris = new int[triIndex];
                 System.Array.Copy(triangles, finalTris, triIndex);
                 triangles = finalTris;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void UpdateMeshVertices(Mesh mesh, float[,] heightMap, float[,] payLimitMap, float[,] vegLimitMap, float cellSize, bool updateSkirt = true)
        {
             if (mesh == null) return;
             
             int width = heightMap.GetLength(0);
             int depth = heightMap.GetLength(1);
             Vector3[] vertices = mesh.vertices;
             Color[] colors = mesh.colors;
             
             if (colors.Length != vertices.Length) colors = new Color[vertices.Length];

            // Colors
            Color vegetationColor = new Color(0.1f, 0.8f, 0.1f); 
            Color topSoilColor = new Color(0.6f, 0.45f, 0.25f); 
            Color payLayerColor = new Color(0.5f, 0.25f, 0.1f);
            Color bedrockColor = Color.gray;

             // 1. Update Main Verts
             int i = 0;
             for (int z = 0; z < depth; z++)
             {
                 for (int x = 0; x < width; x++)
                 {
                     float y = heightMap[x, z];
                     float pLimit = (payLimitMap != null) ? payLimitMap[x, z] : -100f;
                     float vLimit = (vegLimitMap != null) ? vegLimitMap[x, z] : -100f;
                     
                     vertices[i].y = y;
                     
                     if (y >= vLimit) colors[i] = vegetationColor;
                     else if (y >= pLimit) colors[i] = topSoilColor;
                     else colors[i] = payLayerColor;
                     
                     i++;
                 }
             }
             
             // 2. Update Skirt Verts
             if (updateSkirt)
             {
                 void UpdateSkirtEdge(int x1, int z1, int x2, int z2)
                 {
                     float y1 = heightMap[x1, z1];
                     float y2 = heightMap[x2, z2];
                     float pLim1 = (payLimitMap != null) ? payLimitMap[x1, z1] : -100f;
                     float vLim1 = (vegLimitMap != null) ? vegLimitMap[x1, z1] : -100f;
                     float pLim2 = (payLimitMap != null) ? payLimitMap[x2, z2] : -100f;
                     float vLim2 = (vegLimitMap != null) ? vegLimitMap[x2, z2] : -100f;

                     // v1 (TL) -> i
                     vertices[i].y = y1;
                     colors[i] = (y1 >= vLim1) ? vegetationColor : ((y1 >= pLim1) ? topSoilColor : payLayerColor);
                     
                     // v2 (TR) -> i+1
                     vertices[i+1].y = y2;
                     colors[i+1] = (y2 >= vLim2) ? vegetationColor : ((y2 >= pLim2) ? topSoilColor : payLayerColor);
                     
                     // v3, v4 unused updates (bedrock)
                     i += 4;
                 }

                 for (int x = 0; x < width - 1; x++) UpdateSkirtEdge(x, 0, x + 1, 0);
                 for (int z = 0; z < depth - 1; z++) UpdateSkirtEdge(width - 1, z, width - 1, z + 1);
                 for (int x = width - 1; x > 0; x--) UpdateSkirtEdge(x, depth - 1, x - 1, depth - 1);
                 for (int z = depth - 1; z > 0; z--) UpdateSkirtEdge(0, z, 0, z - 1);
             }

             mesh.vertices = vertices;
             mesh.colors = colors;
             
             mesh.RecalculateNormals();
             mesh.RecalculateBounds();
        }
    }
}
