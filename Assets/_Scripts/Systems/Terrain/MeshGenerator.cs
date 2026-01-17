using UnityEngine;

namespace ClaimTycoon.Systems.Terrain
{
    public static class MeshGenerator
    {
        public static Mesh GenerateTerrainMesh(float[,] heightMap, float cellSize)
        {
            int width = heightMap.GetLength(0);
            int depth = heightMap.GetLength(1);

            Mesh mesh = new Mesh();
            mesh.name = "ProceduralTerrain";

            Vector3[] vertices = new Vector3[width * depth];
            Vector2[] uvs = new Vector2[width * depth];
            int[] triangles = new int[(width - 1) * (depth - 1) * 6];

            int vertIndex = 0;
            int triIndex = 0;

            // 1. Generate Vertices & UVs
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Position: X * CellSize, Height, Z * CellSize
                    // We might need to center it or start at 0. Let's start at 0.
                    float y = heightMap[x, z];
                    vertices[vertIndex] = new Vector3(x * cellSize, y, z * cellSize);
                    
                    // Simple UV mapping (0-1 across the whole chunk)
                    // Or 0-1 per tile if we use a tiling texture
                    uvs[vertIndex] = new Vector2((float)x / width, (float)z / depth);

                    // 2. Generate Triangles (Grid Squares)
                    if (x < width - 1 && z < depth - 1)
                    {
                        // Vertex Indices
                        // Bottom-Left (Current), Bottom-Right (Next X), Top-Left (Next Z), Top-Right (Both)
                        int bl = vertIndex;
                        int br = vertIndex + 1;
                        int tl = vertIndex + width;
                        int tr = vertIndex + width + 1;

                        // Triangle 1 (BL -> TL -> BR)
                        triangles[triIndex] = bl;
                        triangles[triIndex + 1] = tl;
                        triangles[triIndex + 2] = br;

                        // Triangle 2 (BR -> TL -> TR)
                        triangles[triIndex + 3] = br;
                        triangles[triIndex + 4] = tl;
                        triangles[triIndex + 5] = tr;

                        triIndex += 6;
                    }

                    vertIndex++;
                }
            }

            // 3. Assign to Mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            
            mesh.RecalculateNormals(); // Crucial for lighting
            mesh.RecalculateBounds();

            return mesh;
        }

        // Helper to update just vertices for performance (if topology doesn't change)
        public static void UpdateMeshVertices(Mesh mesh, float[,] heightMap, float cellSize)
        {
             int width = heightMap.GetLength(0);
             int depth = heightMap.GetLength(1);
             Vector3[] vertices = mesh.vertices;
             
             int i = 0;
             for (int z = 0; z < depth; z++)
             {
                 for (int x = 0; x < width; x++)
                 {
                     vertices[i].y = heightMap[x, z];
                     i++;
                 }
             }
             
             mesh.vertices = vertices;
             mesh.RecalculateNormals();
             mesh.RecalculateBounds();
        }
    }
}
