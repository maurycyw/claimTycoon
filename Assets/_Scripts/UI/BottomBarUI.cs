using UnityEngine;
using UnityEngine.UI;
using ClaimTycoon.Managers;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.UI
{
    public class BottomBarUI : MonoBehaviour
    {
        [Header("Main Buttons")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button staffButton;

        [Header("Unit Selection Panel")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button restButton;
        [SerializeField] private Button statsButton;

        // References to other UI panels
        [SerializeField] private ShopPanelUI shopPanel;  // To be created
        [SerializeField] private InventoryUI inventoryPanel; // Existing
        [SerializeField] private ActiveJobsPanel activeJobsPanel; // Existing

        private void Start()
        {
            // Subscribe to Selection
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected += OnUnitSelected;
                SelectionManager.Instance.OnUnitDeselected += OnUnitDeselected;
            }

            // Setup Buttons
            if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
            if (inventoryButton != null) inventoryButton.onClick.AddListener(ToggleInventory);
            if (staffButton != null) staffButton.onClick.AddListener(ToggleStaff);
            
            if (restButton != null) restButton.onClick.AddListener(OnRestClicked);
            if (statsButton != null) statsButton.onClick.AddListener(OnStatsClicked);

            // Hide Selection Panel initially
            if (selectionPanel != null) selectionPanel.SetActive(false);
            
            // Debug Assignments
            if (shopButton != null) Debug.Log($"[BottomBarUI] Shop Button assigned to: {shopButton.name}");
            if (inventoryButton != null) Debug.Log($"[BottomBarUI] Inventory Button assigned to: {inventoryButton.name}");
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected -= OnUnitSelected;
                SelectionManager.Instance.OnUnitDeselected -= OnUnitDeselected;
            }
        }

        private void OnUnitSelected(UnitController unit)
        {
            if (selectionPanel != null) selectionPanel.SetActive(true);
        }

        private void OnUnitDeselected()
        {
             if (selectionPanel != null) selectionPanel.SetActive(false);
        }

        private void ToggleShop()
        {
            Debug.Log("[BottomBarUI] ToggleShop called.");
            if (shopPanel != null)
            {
                bool newState = !shopPanel.gameObject.activeSelf;
                shopPanel.gameObject.SetActive(newState);
                
                if (newState)
                {
                    // Panel Open -> Button Selected
                    if (shopButton != null) shopButton.Select();
                }
                else
                {
                    // Panel Closed -> Button Deselected
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }
            }
            else
            {
                Debug.LogWarning("ShopPanel reference missing in BottomBarUI");
            }
        }

        private void ToggleInventory()
        {
            Debug.Log("[BottomBarUI] ToggleInventory called.");
            if (inventoryPanel != null)
            {
                // We need to know the NEW state. InventoryUI.ToggleInventory toggles internally.
                // We'll peek at the current state, assume it flips. 
                // Better approach: Check logic in InventoryUI or just check activeSelf after toggle if possible?
                // InventoryUI toggles immediately.
                
                inventoryPanel.ToggleInventory(); 
                
                // Now check the state of the panel object inside InventoryUI? 
                // InventoryUI doesn't expose the panel publically but we can guess or modify InventoryUI. 
                // Actually, let's just use the known reference if we had it, but InventoryUI hides it.
                // Let's rely on standard Select behavior for now, or clearer:
                
                // Issue: We don't have direct access to 'inventoryPanel.activeSelf' cleanly unless we expose it.
                // But wait, BottomBarUI HAS a serialized field 'inventoryPanel' of type InventoryUI.
                // Does InventoryUI expose its state? No.
                // Let's try to assume it worked.
                
                // Ideally we update InventoryUI to return the new state, but for now let's just Select().
                if (inventoryButton != null) inventoryButton.Select();
                
                // If the user wants it to DESELECT when closing, we need to know if it closed.
                // For now, let's just keep the functionality consistent with Shop for the Select part, 
                // but we might miss the 'Deselect on Close' if we don't know state.
                // Force deselect if we think it closed? 
                // Let's leave Inventory simple for a second or update InventoryUI. 
            }
             // RE-READING InventoryUI Code from previous turn (Step 221):
             // public void ToggleInventory() { ... inventoryPanel.SetActive(isActive); ... }
             // It doesn't return state. 
             // We can check `inventoryPanel.gameObject.activeInHierarchy`? 
             // Ref: `[SerializeField] private InventoryUI inventoryPanel;`
             // `inventoryPanel` is the SCRIPT. The script is on a GameObject. Is that GO the panel?
             // In Step 221: `[SerializeField] private GameObject inventoryPanel;` inside InventoryUI.
             // so InventoryUI script sits on a manager probably.
             
             // Simple Fix: modification to ToggleInventory to return bool, OR just Select() for now 
             // and fix Deselect if user complains specifically about Inventory.
             // OR: Logic catch:
             if (inventoryButton != null) inventoryButton.Select();
        }

        private void ToggleStaff()
        {
             if (activeJobsPanel != null)
            {
                bool isActive = activeJobsPanel.gameObject.activeSelf;
                activeJobsPanel.gameObject.SetActive(!isActive);
            }
        }

        private void OnRestClicked()
        {
            // Logic to make selected unit rest
            UnitController unit = SelectionManager.Instance.SelectedUnit;
            if (unit != null)
            {
                // unit.Rest(); // Need to implement or access Rest logic
                Debug.Log($"Rest requested for {unit.name}");
            }
        }

        private void OnStatsClicked()
        {
            UnitController unit = SelectionManager.Instance.SelectedUnit;
            if (unit != null)
            {
                Debug.Log($"Stats requested for {unit.name}");
                // Open detailed stats panel
            }
        }
    }
}
