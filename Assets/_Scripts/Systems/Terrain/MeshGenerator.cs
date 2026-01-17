using UnityEngine;

namespace ClaimTycoon.Systems.Terrain
{
    public static class MeshGenerator
    {
        public static Mesh GenerateTerrainMesh(float[,] heightMap, float cellSize, bool generateSkirt = true)
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

            Debug.Log($"[MeshGenerator] Counts: Width={width}, Depth={depth}, MainVerts={mainVertCount}, SkirtVerts={skirtVertCount}");

            Vector3[] vertices = new Vector3[mainVertCount + skirtVertCount];
            Vector2[] uvs = new Vector2[mainVertCount + skirtVertCount];
            int[] triangles = new int[(mainVertCount * 6) + (skirtVertCount * 6)];


            int vertIndex = 0;
            int triIndex = 0;

            // 1. Generate Main Terrain
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float y = heightMap[x, z];
                    vertices[vertIndex] = new Vector3(x * cellSize, y, z * cellSize);
                    uvs[vertIndex] = new Vector2((float)x / width, (float)z / depth);

                    if (x < width - 1 && z < depth - 1)
                    {
                        // Main Grid Triangles (Clockwise)
                        int bl = vertIndex;
                        int br = vertIndex + 1;
                        int tl = vertIndex + width;
                        int tr = vertIndex + width + 1;
                        
                        // Tri 1: BL -> TL -> BR
                        triangles[triIndex] = bl;
                        triangles[triIndex + 1] = tl;
                        triangles[triIndex + 2] = br;
                        
                        // Tri 2: BR -> TL -> TR
                        triangles[triIndex + 3] = br;
                        triangles[triIndex + 4] = tl;
                        triangles[triIndex + 5] = tr;
                        
                        triIndex += 6;
                    }
                    vertIndex++;
                }
            }

            // 2. Generate Skirt
            if (generateSkirt)
            {
                // We use a Local Helper that captures Scope
                void AddSkirtQuad(int x1, int z1, int x2, int z2)
                {
                    // Vertices
                    float y1 = heightMap[x1, z1];
                    float y2 = heightMap[x2, z2];
                    Vector3 v1 = new Vector3(x1 * cellSize, y1, z1 * cellSize);        // Top Left (Start)
                    Vector3 v2 = new Vector3(x2 * cellSize, y2, z2 * cellSize);        // Top Right (End)
                    Vector3 v3 = new Vector3(x1 * cellSize, skirtDepth, z1 * cellSize); // Bottom Left
                    Vector3 v4 = new Vector3(x2 * cellSize, skirtDepth, z2 * cellSize); // Bottom Right
                    
                    int currentVert = vertIndex;
                    vertices[vertIndex] = v1; uvs[vertIndex] = new Vector2(0, 1); vertIndex++;
                    vertices[vertIndex] = v2; uvs[vertIndex] = new Vector2(1, 1); vertIndex++;
                    vertices[vertIndex] = v3; uvs[vertIndex] = new Vector2(0, 0); vertIndex++;
                    vertices[vertIndex] = v4; uvs[vertIndex] = new Vector2(1, 0); vertIndex++;
                    
                    // Triangles (Clockwise Winding for visibility from Outside)
                    // v1=TL, v2=TR, v3=BL, v4=BR relative to the wall face
                    // Tri 1: BL -> TL -> TR (v3 -> v1 -> v2)
                    triangles[triIndex] = currentVert + 2; // v3
                    triangles[triIndex + 1] = currentVert;     // v1
                    triangles[triIndex + 2] = currentVert + 1; // v2
                    
                    // Tri 2: BL -> TR -> BR (v3 -> v2 -> v4)
                    triangles[triIndex + 3] = currentVert + 2; // v3
                    triangles[triIndex + 4] = currentVert + 1; // v2
                    triangles[triIndex + 5] = currentVert + 3; // v4
                    
                    triIndex += 6;
                }

                // Walk Perimeter (Keep Outside on Right -> CCW walk)
                // Bottom (0 to W-1)
                for (int x = 0; x < width - 1; x++) AddSkirtQuad(x, 0, x + 1, 0);
                
                // Right (0 to D-1)
                for (int z = 0; z < depth - 1; z++) AddSkirtQuad(width - 1, z, width - 1, z + 1);
                
                // Top (W-1 to 0)
                for (int x = width - 1; x > 0; x--) AddSkirtQuad(x, depth - 1, x - 1, depth - 1);
                
                // Left (D-1 to 0)
                for (int z = depth - 1; z > 0; z--) AddSkirtQuad(0, z, 0, z - 1);
            }

            // Resize Triangles Array to exact size if needed (optional but good)
            if (triIndex < triangles.Length)
            {
                 int[] finalTris = new int[triIndex];
                 System.Array.Copy(triangles, finalTris, triIndex);
                 triangles = finalTris;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        public static void UpdateMeshVertices(Mesh mesh, float[,] heightMap, float cellSize, bool updateSkirt = true)
        {
             if (mesh == null) return;
             
             int width = heightMap.GetLength(0);
             int depth = heightMap.GetLength(1);
             Vector3[] vertices = mesh.vertices;
             
             // 1. Update Main Verts
             int i = 0;
             for (int z = 0; z < depth; z++)
             {
                 for (int x = 0; x < width; x++)
                 {
                     vertices[i].y = heightMap[x, z];
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
                     // v1 is at i, v2 is at i+1. (v3, v4 are i+2, i+3)
                     vertices[i].y = y1;
                     vertices[i+1].y = y2;
                     i += 4;
                 }

                 for (int x = 0; x < width - 1; x++) UpdateSkirtEdge(x, 0, x + 1, 0);
                 for (int z = 0; z < depth - 1; z++) UpdateSkirtEdge(width - 1, z, width - 1, z + 1);
                 for (int x = width - 1; x > 0; x--) UpdateSkirtEdge(x, depth - 1, x - 1, depth - 1);
                 for (int z = depth - 1; z > 0; z--) UpdateSkirtEdge(0, z, 0, z - 1);
             }

             mesh.vertices = vertices;
             mesh.RecalculateNormals();
             mesh.RecalculateBounds();
        }
    }
}
