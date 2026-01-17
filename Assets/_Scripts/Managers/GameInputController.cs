using UnityEngine;
using UnityEngine.InputSystem;
using ClaimTycoon.Controllers;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings;
using ClaimTycoon.Systems.Units.Jobs;

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

        // Auto Mine Selection State
        private bool isSelectingAutoMineLocation = false;
        private SluiceBox pendingAutoMineSluice;
        private GameObject selectionIndicator;

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

            // Create Selection Indicators
            CreateSelectionIndicator();
        }

        private void CreateSelectionIndicator()
        {
            selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selectionIndicator.name = "AutoMineIndicator";
            selectionIndicator.transform.SetParent(transform);
            Destroy(selectionIndicator.GetComponent<Collider>()); // Visual only
            
            // Flatten cylinder to look like a circle/area
            selectionIndicator.transform.localScale = new Vector3(10f, 0.1f, 10f); 
            
            if (selectionIndicator.GetComponent<Renderer>() != null)
            {
                selectionIndicator.GetComponent<Renderer>().material.color = new Color(0, 1, 1, 0.5f); // Cyan transparent
            }
            selectionIndicator.SetActive(false);
        }

        private void Update()
        {
            // Block input if we are in placement mode
            if (BuildingManager.Instance != null && BuildingManager.Instance.IsPlacing) return;

            if (mainCamera == null) mainCamera = Camera.main;

            if (Mouse.current != null)
            {
                // Check if mouse is over UI
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (isSelectingAutoMineLocation)
                {
                    HandleSelectionMode();
                    return; // Consumes all input
                }

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
                         selectedUnit.StartJob(new FeedSluiceJob(Vector3Int.zero, hit.point, sluice));
                         return; // Action consumed click
                    }


                    // 2. Check for Terrain -> Drop Dirt
                    // Raycast SPECIFICALLY for Terrain to avoid Unit blocking
                    if (Physics.Raycast(ray, out RaycastHit terrainHit, 1000f, interactionLayer))
                    {
                        float cellSize = TerrainManager.Instance.CellSize;
                        int x = Mathf.RoundToInt(terrainHit.point.x / cellSize);
                        int z = Mathf.RoundToInt(terrainHit.point.z / cellSize);
                        Vector3Int coord = new Vector3Int(x, 0, z);

                        if (TerrainManager.Instance.TryGetTile(coord, out TileType type))
                        {
                            if (type == TileType.Dirt)
                            {
                                 Debug.Log("Left Click Action: Drop Dirt");
                                 selectedUnit.StartJob(new DropDirtJob(coord, terrainHit.point));
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
                         selectedUnit.StartJob(new FeedSluiceJob(Vector3Int.zero, sluice.GetInteractionPosition(), sluice)); 
                     }
                     else
                     {
                         if (Keyboard.current.shiftKey.isPressed)
                         {
                             Debug.Log("Command: Enter Auto Mining Selection Mode");
                             StartAutoMineSelection(sluice);
                         }
                         else
                         {
                             Debug.Log("Command: Cleanup Sluice");
                             sluice.Interact(); 
                         }
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
                                 selectedUnit.StartJob(new DropDirtJob(coord, hit.point));
                            }
                            else
                            {
                                Debug.Log("Command: Dig Dirt");
                                selectedUnit.StartJob(new MineJob(coord, hit.point));
                            }
                            return;
                        }
                    }
                }
            }
        }
        private void StartAutoMineSelection(SluiceBox sluice)
        {
            isSelectingAutoMineLocation = true;
            pendingAutoMineSluice = sluice;
            if (selectionIndicator != null) selectionIndicator.SetActive(true);
        }

        private void HandleSelectionMode()
        {
            // Raycast to Terrain
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            
            // Only hit Terrain
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactionLayer)) // Assuming interactionLayer includes terrain
            {
                if (selectionIndicator != null)
                {
                    selectionIndicator.transform.position = hit.point;
                }

                // Confirm on Left Click
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector3Int mineCenter = new Vector3Int(Mathf.RoundToInt(hit.point.x / TerrainManager.Instance.CellSize), 0, Mathf.RoundToInt(hit.point.z / TerrainManager.Instance.CellSize));
                    
                    // Visual Pulse
                    StartCoroutine(PulseSelection(hit.point));

                    // Start Mining
                    UnitController selectedUnit = SelectionManager.Instance.SelectedUnit;
                    if (selectedUnit != null && pendingAutoMineSluice != null)
                    {
                        selectedUnit.StartAutoMining(mineCenter, pendingAutoMineSluice);
                    }
                    
                    // Cleanup State
                    isSelectingAutoMineLocation = false;
                    pendingAutoMineSluice = null;
                }
            }
            
            // Cancel on Right Click or Escape
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                 Debug.Log("Auto Mining Selection Cancelled.");
                 isSelectingAutoMineLocation = false;
                 pendingAutoMineSluice = null;
                 if (selectionIndicator != null) selectionIndicator.SetActive(false);
            }
        }

        private System.Collections.IEnumerator PulseSelection(Vector3 position)
        {
            GameObject pulseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(pulseObj.GetComponent<Collider>());
            pulseObj.transform.position = position;
            pulseObj.transform.localScale = selectionIndicator.transform.localScale;
             if (pulseObj.GetComponent<Renderer>() != null)
                pulseObj.GetComponent<Renderer>().material.color = Color.green;

            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = pulseObj.transform.localScale;
            Vector3 targetScale = startScale * 1.5f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                pulseObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                 if (pulseObj.GetComponent<Renderer>() != null)
                 {
                    Color c = pulseObj.GetComponent<Renderer>().material.color;
                    c.a = Mathf.Lerp(0.5f, 0f, t);
                    pulseObj.GetComponent<Renderer>().material.color = c;
                 }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Destroy(pulseObj);
            if (selectionIndicator != null) selectionIndicator.SetActive(false);
        }
    }
}
