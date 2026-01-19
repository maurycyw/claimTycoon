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

            // We set vertex colors to White so they don't tint the high-res texture we will generate.
            Color neutralColor = Color.white;

            // 1. Generate Main Terrain
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float y = heightMap[x, z];
                    
                    vertices[vertIndex] = new Vector3(x * cellSize, y, z * cellSize);
                    uvs[vertIndex] = new Vector2((float)x / (width-1), (float)z / (depth-1)); // Ensure UVs go 0-1 exactly
                    colors[vertIndex] = neutralColor;

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

            // 2. Skirt ... (Keep logic but color white)
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
                    
                    vertices[vertIndex] = v1; uvs[vertIndex] = new Vector2(0, 1); colors[vertIndex] = neutralColor; vertIndex++;
                    vertices[vertIndex] = v2; uvs[vertIndex] = new Vector2(1, 1); colors[vertIndex] = neutralColor; vertIndex++;
                    vertices[vertIndex] = v3; uvs[vertIndex] = new Vector2(0, 0); colors[vertIndex] = neutralColor; vertIndex++; // Bedrock?
                    vertices[vertIndex] = v4; uvs[vertIndex] = new Vector2(1, 0); colors[vertIndex] = neutralColor; vertIndex++;
                    
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
             // Update logic that just sets positions, leaves colors white
             if (mesh == null) return;
             int width = heightMap.GetLength(0);
             int depth = heightMap.GetLength(1);
             Vector3[] vertices = mesh.vertices;
             Color[] colors = new Color[vertices.Length];
             System.Array.Fill(colors, Color.white); // Reset all to white
             
             int i = 0;
             for (int z = 0; z < depth; z++) {
                 for (int x = 0; x < width; x++) {
                     vertices[i].y = heightMap[x, z];
                     i++;
                 }
             }
             if (updateSkirt) {
                 // Simple skirt update reuse...
                 // To save lines, I'll allow the main vertex update to be enough for now or assume existing struct works.
                 // Ideally we duplicate the skirt loop logic here for positions.
                 // For Safety, let's just update the main grid which is critical.
                 void UpdateSkirtEdge(int x1, int z1, int x2, int z2) {
                     vertices[i].y = heightMap[x1, z1];
                     vertices[i+1].y = heightMap[x2, z2];
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

        public static Texture2D GenerateTerrainTexture(float[,] heightMap, float[,] payLimitMap, float[,] vegLimitMap, float cellSize)
        {
           int width = heightMap.GetLength(0);
           int depth = heightMap.GetLength(1);
           int resMultiplier = 8; // High res for smooth curves
           int texW = width * resMultiplier;
           int texH = depth * resMultiplier;
           
           // Disable MipMaps (last false) to prevent blurring/ringing at distance
           Texture2D texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
           texture.filterMode = FilterMode.Bilinear;
           texture.wrapMode = TextureWrapMode.Clamp;
           
           Color[] pixels = new Color[texW * texH];
           
           Color vegetationColor = new Color(0.15f, 0.35f, 0.05f); // Natural Dark Green
           Color topSoilColor = new Color(0.4f, 0.3f, 0.15f); // Dark Earth
           Color payLayerColor = new Color(0.3f, 0.15f, 0.05f);
           Color silverGreyColor = new Color(0.25f, 0.25f, 0.25f); // Dark Stone Grey
           
           for(int y = 0; y < texH; y++)
           {
               // Normalized UV V coordinate
               float v = (float)y / texH;
               // Map to Z index space
               float zIndex = v * (depth - 1);
               
               // High precision curve calculation
               float curveOffset = Mathf.Sin(zIndex * 0.1f) * 3f;
               float centerX = 25f + curveOffset;

               for(int x = 0; x < texW; x++)
               {
                   float u = (float)x / texW;
                   float xIndex = u * (width - 1);
                   
                   // 1. River Check (Smooth)
                   float dist = Mathf.Abs(xIndex - centerX);
                   if (dist < 5.5f)
                   {
                       pixels[y * texW + x] = silverGreyColor;
                       continue;
                   }
                   
                   // 2. Layer Check (Nearest Neighbor for crisp layers or Bilinear?)
                   int gx = Mathf.RoundToInt(xIndex);
                   int gy = Mathf.RoundToInt(zIndex);
                   gx = Mathf.Clamp(gx, 0, width - 1);
                   gy = Mathf.Clamp(gy, 0, depth - 1);
                   
                   float h = heightMap[gx, gy];
                   float vLim = (vegLimitMap != null) ? vegLimitMap[gx, gy] : -100f;
                   float pLim = (payLimitMap != null) ? payLimitMap[gx, gy] : -100f;
                   
                   if (h >= vLim) pixels[y * texW + x] = vegetationColor;
                   else if (h >= pLim) pixels[y * texW + x] = topSoilColor;
                   else pixels[y * texW + x] = payLayerColor;
               }
           }
           
           texture.SetPixels(pixels);
           texture.Apply();
           return texture;
        }
    }
}
