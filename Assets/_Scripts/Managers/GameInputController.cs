using UnityEngine;
using UnityEngine.InputSystem;
using ClaimTycoon.Controllers;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings;

namespace ClaimTycoon.Managers
{
    public class GameInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitController playerUnit;
        [SerializeField] private LayerMask interactionLayer;

        private Camera mainCamera;

        private static GameInputController _instance;
        private int instanceId;

        private void Awake()
        {
            instanceId = GetInstanceID();
            if (_instance != null && _instance != this)
            {
                Debug.LogError($"[GameInputController] DUPLICATE INSTANCE DETECTED! Destroying this one on {gameObject.name}. Active one is on {_instance.gameObject.name}");
                Destroy(this);
                return;
            }
            _instance = this;
            Debug.Log($"[GameInputController] Initialized on {gameObject.name} (ID: {instanceId})");
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (playerUnit == null)
            {
                playerUnit = FindFirstObjectByType<UnitController>();
                if (playerUnit == null) Debug.LogError("GameInputController: No UnitController found!");
            }
        }

        private void Update()
        {
            // Block input if we are in placement mode
            if (BuildingManager.Instance != null && BuildingManager.Instance.IsPlacing) return;

            if (mainCamera == null) mainCamera = Camera.main;

            if (Mouse.current != null)
            {
                 // Left Click: Movement OR Selection
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    HandleLeftClick();
                }
                
                // Right Click: Action (Dig, Drop, Interact)
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    HandleRightClick();
                }
            }
        }

        private void HandleLeftClick()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                // PRIORITY: If Unit Selected AND Carrying Dirt -> Check for Drop/Action
                UnitController selectedUnit = SelectionManager.Instance.SelectedUnit;
                if (selectedUnit != null && selectedUnit.IsCarryingDirt)
                {
                     // 1. Check for SluiceBox (or future Washplant)
                    SluiceBox sluice = hit.collider.GetComponent<SluiceBox>();
                    if (sluice == null) sluice = hit.collider.GetComponentInParent<SluiceBox>();

                    if (sluice != null)
                    {
                         Debug.Log("Left Click Action: Feed Sluice");
                         selectedUnit.StartJob(JobType.FeedSluice, Vector3Int.zero, hit.point, sluice);
                         return; // Action consumed click
                    }

                    // 2. Check for Terrain -> Drop Dirt
                    if (((1 << hit.collider.gameObject.layer) & interactionLayer) != 0)
                    {
                        float cellSize = TerrainManager.Instance.CellSize;
                        int x = Mathf.RoundToInt(hit.point.x / cellSize);
                        int z = Mathf.RoundToInt(hit.point.z / cellSize);
                        Vector3Int coord = new Vector3Int(x, 0, z);

                        if (TerrainManager.Instance.TryGetTile(coord, out TileType type))
                        {
                            if (type == TileType.Dirt)
                            {
                                 Debug.Log("Left Click Action: Drop Dirt");
                                 selectedUnit.StartJob(JobType.DropDirt, coord, hit.point);
                                 return; // Action consumed click
                            }
                        }
                    }
                }

                // 1. Check for Unit Selection/Deselection
                UnitController unit = hit.collider.GetComponent<UnitController>();
                if (unit == null) unit = hit.collider.GetComponentInParent<UnitController>();

                if (unit != null)
                {
                    // Toggle Logic
                    if (SelectionManager.Instance.SelectedUnit == unit)
                    {
                        SelectionManager.Instance.Deselect();
                    }
                    else
                    {
                        SelectionManager.Instance.SelectUnit(unit);
                    }
                    return; 
                }
                
                // 2. If Unit Selected -> Move Command
                if (SelectionManager.Instance.SelectedUnit != null)
                {
                     // Move to click point
                     SelectionManager.Instance.SelectedUnit.MoveTo(hit.point);
                     return;
                }
                
                // If nothing relevant clicked and no unit selected -> Deselect (Safety)
                SelectionManager.Instance.Deselect();
            }
        }

        private void HandleRightClick()
        {
            // Only perform actions if a unit is selected
            UnitController selectedUnit = SelectionManager.Instance.SelectedUnit;
            if (selectedUnit == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Debug.Log($"[GameInput] Right Click Hit: {hit.collider.name}");

                // 1. Check for SluiceBox Interaction
                SluiceBox sluice = hit.collider.GetComponent<SluiceBox>();
                if (sluice == null) sluice = hit.collider.GetComponentInParent<SluiceBox>();
                
                if (sluice != null)
                {
                     if (selectedUnit.IsCarryingDirt)
                     {
                         Debug.Log("Command: Feed Sluice");
                         selectedUnit.StartJob(JobType.FeedSluice, Vector3Int.zero, hit.point, sluice); 
                     }
                     else
                     {
                         Debug.Log("Command: Cleanup Sluice");
                         sluice.Interact(); // Or Unit moves to interact? Current logic is immediate interact for cleanup.
                         // Ideally: Unit moves there then interacts. 
                         // For now keeping 'Interact' from distance or assuming close enough if we want consistent job system.
                         // Let's make it a pseudo-job for consistency? 
                         // Or stick to Sluice.Interact() being "Player action" vs "Unit Action".
                         // UnitController doesn't have "Cleanup" job yet.
                     }
                      return;
                }

                // 2. Terrain Interaction
                if (((1 << hit.collider.gameObject.layer) & interactionLayer) != 0)
                {
                    float cellSize = TerrainManager.Instance.CellSize;
                    int x = Mathf.RoundToInt(hit.point.x / cellSize);
                    int z = Mathf.RoundToInt(hit.point.z / cellSize);
                    Vector3Int coord = new Vector3Int(x, 0, z);

                    if (TerrainManager.Instance.TryGetTile(coord, out TileType type))
                    {
                        if (type == TileType.Dirt)
                        {
                            if (selectedUnit.IsCarryingDirt)
                            {
                                 Debug.Log("Command: Drop Dirt");
                                 selectedUnit.StartJob(JobType.DropDirt, coord, hit.point);
                            }
                            else
                            {
                                Debug.Log("Command: Dig Dirt");
                                selectedUnit.StartJob(JobType.Mine, coord, hit.point);
                            }
                            return;
                        }
                    }
                }
            }
        }
    }
}
