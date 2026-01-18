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

            // Subscribe to HUD State
            if (HUDController.Instance != null)
            {
                HUDController.Instance.OnPanelStateChanged += UpdateButtonVisuals;
            }

            // Setup Buttons
            if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
            if (inventoryButton != null) inventoryButton.onClick.AddListener(ToggleInventory);
            if (staffButton != null) staffButton.onClick.AddListener(ToggleStaff);
            
            if (restButton != null) restButton.onClick.AddListener(OnRestClicked);
            if (statsButton != null) statsButton.onClick.AddListener(OnStatsClicked);

            // Hide Selection Panel initially
            if (selectionPanel != null) selectionPanel.SetActive(false);
            
            // Initial Visual Sync
            UpdateButtonVisuals();
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected -= OnUnitSelected;
                SelectionManager.Instance.OnUnitDeselected -= OnUnitDeselected;
            }
             if (HUDController.Instance != null)
            {
                HUDController.Instance.OnPanelStateChanged -= UpdateButtonVisuals;
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
            if (HUDController.Instance != null)
            {
                HUDController.Instance.ToggleShop();
            }
        }

        private void ToggleInventory()
        {
            if (HUDController.Instance != null)
            {
                HUDController.Instance.ToggleInventory();
            }
        }

        private void UpdateButtonVisuals()
        {
            if (HUDController.Instance == null) return;

            // Update Shop Button
            if (shopButton != null)
            {
                if (HUDController.Instance.shopController != null && HUDController.Instance.shopController.IsShopOpen)
                {
                    shopButton.Select();
                }
                else
                {
                    // If we currently have it selected, deselect it? 
                    // Or actually, we just rely on standard EventSystem behavior?
                    // Issue: If it was selected, and now we close it, it stays "Highlighted" or "Selected" visually unless we clear it.
                    if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == shopButton.gameObject)
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }
                }
            }

            // Update Inventory Button
            if (inventoryButton != null)
            {
                 if (HUDController.Instance.IsInventoryOpen)
                {
                    inventoryButton.Select();
                }
                else
                {
                     if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == inventoryButton.gameObject)
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }
                }
            }
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
