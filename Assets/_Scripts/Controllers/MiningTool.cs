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
            
            // Debug Draw
            Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 1.0f);

            if (Physics.Raycast(ray, out RaycastHit hit, range, terrainLayer))
            {
                Debug.Log($"Raycast Hit: {hit.collider.name} at {hit.point}");

                // Simple logic: if we hit a collider that is a child of TerrainManager or just a tile
                // We could put a script on the tile, or just user coordinate math.
                // For this prototype, let's assume tiles are positioned exactly at integer coordinates.
                
                Vector3 hitPos = hit.point; // Use exact point
                
                // Vector3Int coord = new Vector3Int(Mathf.RoundToInt(hitPos.x), Mathf.RoundToInt(hitPos.y), Mathf.RoundToInt(hitPos.z));

                if (TerrainManager.Instance != null)
                {
                    Debug.Log("Mining Tool: Calling ModifyHeight");
                    // Just Dig at the point
                    TerrainManager.Instance.ModifyHeight(hit.point, -0.5f);
                }
            }
            else
            {
                // Debug.Log("Raycast Missed Terrain");
            }
        }

        private void MineTile(Vector3 hitPoint)
        {
            TerrainManager.Instance.ModifyHeight(hitPoint, -0.5f);
            
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
