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
                    // Create a layer of dirt at height 0
                    CreateTile(new Vector3Int(x, 0, z), TileType.Dirt);
                    
                    // Create bedrock below
                    CreateTile(new Vector3Int(x, -1, z), TileType.Bedrock); 
                }
            }
        }

        public void CreateTile(Vector3Int coord, TileType type)
        {
            if (gridData.ContainsKey(coord)) return; // Already exists

            gridData[coord] = type;

            GameObject prefabToSpawn = type == TileType.Dirt ? dirtPrefab : bedrockPrefab;
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
    }
}
