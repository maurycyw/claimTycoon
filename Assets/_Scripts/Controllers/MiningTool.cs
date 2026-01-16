using UnityEngine;
using UnityEngine.InputSystem;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Controllers
{
    public class MiningTool : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private float range = 100f;
        [SerializeField] private float miningChance = 0.2f; // Base chance to find gold

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // New Input System
            {
                TryMine();
            }
        }

        private void TryMine()
        {
            if (Mouse.current == null) return;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, range, terrainLayer))
            {
                // Simple logic: if we hit a collider that is a child of TerrainManager or just a tile
                // We could put a script on the tile, or just user coordinate math.
                // For this prototype, let's assume tiles are positioned exactly at integer coordinates.
                
                Vector3 hitPos = hit.transform.position;
                Vector3Int coord = new Vector3Int(Mathf.RoundToInt(hitPos.x), Mathf.RoundToInt(hitPos.y), Mathf.RoundToInt(hitPos.z));

                if (TerrainManager.Instance != null)
                {
                    // Check if it's the correct tile
                    if (TerrainManager.Instance.TryGetTile(coord, out TileType type))
                    {
                        if (type == TileType.Dirt)
                        {
                            MineTile(coord);
                        }
                    }
                }
            }
        }

        private void MineTile(Vector3Int coord)
        {
            TerrainManager.Instance.RemoveTile(coord);
            
            // Chance to find gold
            if (Random.value < miningChance)
            {
                Debug.Log("Found Gold Nugget!");
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddGold(0.5f);
                }
            }
        }
    }
}
