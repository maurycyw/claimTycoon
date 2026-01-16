using UnityEngine;
using UnityEngine.InputSystem;
using ClaimTycoon.Systems.Terrain;

namespace ClaimTycoon.Managers
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Material errorMaterial;

        private GameObject buildingPrefab;
        private GameObject ghostObject; // The visual ghost
        private int currentCost;
        private bool isPlacing = false;
        private Camera mainCamera;

        // Simple check if user is placing SluiceBox specifically
        private bool isPlacingSluiceBox = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void StartPlacement(GameObject prefab, int cost, bool isSluice = false)
        {
            if (ResourceManager.Instance.MoneyAmount < cost)
            {
                Debug.Log("Not enough money!");
                return;
            }

            buildingPrefab = prefab;
            currentCost = cost;
            isPlacingSluiceBox = isSluice;
            isPlacing = true;
            Debug.Log("Entered Placement Mode");
            
            // Create ghost
            if (ghostObject != null) Destroy(ghostObject);
            ghostObject = Instantiate(buildingPrefab);
            
            // Strip scripts and colliders from ghost so it doesn't interfere
            // Or just disable them if complex. For simple cubes, removing BoxCollider is enough.
            foreach (var c in ghostObject.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var m in ghostObject.GetComponentsInChildren<MonoBehaviour>()) Destroy(m);
        }

        private void Update()
        {
            if (!isPlacing) return;

            // Right click to cancel
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                StopPlacement();
                return;
            }

            // Click to confirm - Moved inside TryPlaceBuilding to ensure validity check first
            // if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            // {
            //    TryPlaceBuilding();
            // }
            
            // Always run Raycast to update Ghost position
            TryPlaceBuilding();
        }

        private void TryPlaceBuilding()
        {
            // Raycast to find tile
             if (Mouse.current == null) return;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, terrainLayer))
            {
                Vector3 hitPos = hit.transform.position;
                Vector3Int coord = new Vector3Int(Mathf.RoundToInt(hitPos.x), Mathf.RoundToInt(hitPos.y), Mathf.RoundToInt(hitPos.z));

                // Validation
                bool isValid = true;

                // 1. Must be Dirt or empty space above bedrock? 
                // For now, let's say we PLACE it on top of a tile, so we check if the tile exists
                if (!TerrainManager.Instance.TryGetTile(coord, out TileType type))
                {
                    isValid = false; 
                }

                if (type == TileType.Water) isValid = false; // Cannot place on water

                // 2. Water Adjacency Check
                if (isPlacingSluiceBox)
                {
                    if (!TerrainManager.Instance.IsAdjacentToWater(coord))
                    {
                        // Debug.Log("Must be placed next to water!"); // Too spammy in Update
                        isValid = false;
                    }
                }

                // Update Ghost Position
                if (ghostObject != null)
                {
                    Vector3 ghostPos = new Vector3(coord.x, coord.y + 1, coord.z); // +1 to sit on top
                    ghostObject.transform.position = ghostPos;

                    // Update Ghost Material (Visual Feedback)
                    Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
                    Material matToUse = isValid ? ghostMaterial : errorMaterial;
                    
                    if (matToUse != null)
                    {
                        foreach (var r in renderers) r.material = matToUse;
                    }
                }

                // Click to confirm
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isValid)
                {
                    Place(coord);
                }
            }
        }

        private void Place(Vector3Int coord)
        {
            // Deduct money
            ResourceManager.Instance.AddMoney(-currentCost);

            // Spawn object
            // Adjust y position to sit ON TOP of the tile if the tile is at y=0
            Vector3 spawnPos = new Vector3(coord.x, coord.y + 1, coord.z); // +1 assuming tile is height 1
            
            // Or if we clicked the top face, fit.point might be better. 
            // For blocky grid, coord + 1 up is usually safe for "sitting on top".
            
            Instantiate(buildingPrefab, spawnPos, Quaternion.identity);

            Debug.Log($"Placed {buildingPrefab.name} at {coord}");
            StopPlacement();
        }

        private void StopPlacement()
        {
            isPlacing = false;
            buildingPrefab = null;
            if (ghostObject != null) Destroy(ghostObject);
        }
    }
}
