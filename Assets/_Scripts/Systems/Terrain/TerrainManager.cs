using System.Collections.Generic;
using UnityEngine;

namespace ClaimTycoon.Systems.Terrain
{
    public enum TileType
    {
        Dirt,
        Bedrock,
        Water
    }

    public class TerrainManager : MonoBehaviour
    {
        public static TerrainManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(10, 10);
        [SerializeField] private float tileSize = 1f;

        [Header("Prefabs")]
        [SerializeField] private GameObject dirtPrefab;
        [SerializeField] private GameObject bedrockPrefab;
        [SerializeField] private GameObject waterPrefab;

        private Dictionary<Vector3Int, TileType> gridData = new Dictionary<Vector3Int, TileType>();
        private Dictionary<Vector3Int, GameObject> activeTiles = new Dictionary<Vector3Int, GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            GenerateInitialGrid();
        }

        private void GenerateInitialGrid()
        {
            // Simple flat terrain for prototype
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    // Create River at x = 0 to 2 for example, or a strip across Z
                    // Let's make a river at x = 4
                    if (x == 4)
                    {
                        CreateTile(new Vector3Int(x, -1, z), TileType.Water);
                        // Do not create dirt on top of water
                    }
                    else
                    {
                        // Create a layer of dirt at height 0
                        CreateTile(new Vector3Int(x, 0, z), TileType.Dirt);
                        
                        // Create bedrock below
                        CreateTile(new Vector3Int(x, -1, z), TileType.Bedrock);
                    }
                }
            }
        }

        public void CreateTile(Vector3Int coord, TileType type)
        {
            if (gridData.ContainsKey(coord)) return; // Already exists

            gridData[coord] = type;

            gridData[coord] = type;

            GameObject prefabToSpawn = null;
            switch(type) {
                case TileType.Dirt: prefabToSpawn = dirtPrefab; break;
                case TileType.Bedrock: prefabToSpawn = bedrockPrefab; break;
                case TileType.Water: prefabToSpawn = waterPrefab; break;
            }

            if (prefabToSpawn != null)
            {
                GameObject tileObj = Instantiate(prefabToSpawn, transform);
                tileObj.transform.position = new Vector3(coord.x * tileSize, coord.y * tileSize, coord.z * tileSize);
                tileObj.name = $"Tile_{coord.x}_{coord.y}_{coord.z}";
                
                // Ensure it has a collider for raycasting
                if (tileObj.GetComponent<Collider>() == null)
                    tileObj.AddComponent<BoxCollider>();

                activeTiles[coord] = tileObj;
            }
        }

        public void RemoveTile(Vector3Int coord)
        {
            if (gridData.ContainsKey(coord) && activeTiles.ContainsKey(coord))
            {
                // Remove visual
                Destroy(activeTiles[coord]);
                activeTiles.Remove(coord);
                gridData.Remove(coord);

                Debug.Log($"Removed tile at {coord}");
            }
        }

        public bool TryGetTile(Vector3Int coord, out TileType type)
        {
            return gridData.TryGetValue(coord, out type);
        }
        public bool IsAdjacentToWater(Vector3Int coord)
        {
            Vector3Int[] neighbors = new Vector3Int[]
            {
                coord + Vector3Int.left,
                coord + Vector3Int.right,
                coord + Vector3Int.forward,
                coord + Vector3Int.back
            };

            foreach (var n in neighbors)
            {
                // Check immediate neighbors at current level and below (where water is)
                // Water is at y = -1
                Vector3Int waterCheck = new Vector3Int(n.x, -1, n.z);
                
                if (gridData.TryGetValue(waterCheck, out TileType type))
                {
                    if (type == TileType.Water) return true;
                }
            }
            return false;
        }
    }
}
