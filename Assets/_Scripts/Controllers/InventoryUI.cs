using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Controllers
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemButtonPrefab;
        [SerializeField] private Button toggleButton;

        private void Start()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleInventory);
            }

            // Listen for Inventory Changes
            if (ResourceManager.Instance != null && ResourceManager.Instance.PlayerInventory != null)
            {
                ResourceManager.Instance.PlayerInventory.OnInventoryChanged += RefreshInventory;
            }
        }

        private void OnDestroy()
        {
             if (ResourceManager.Instance != null && ResourceManager.Instance.PlayerInventory != null)
            {
                ResourceManager.Instance.PlayerInventory.OnInventoryChanged -= RefreshInventory;
            }
        }

        public void ToggleInventory()
        {
            if (inventoryPanel == null) return;

            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);

            if (isActive)
            {
                RefreshInventory();
            }
        }

        private void RefreshInventory()
        {
            if (itemsContainer == null || itemButtonPrefab == null) return;
            if (ResourceManager.Instance == null) return;

            // Clear existing items
            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }

            // Get Items
            var items = ResourceManager.Instance.PlayerInventory.GetAllItems();

            foreach (var item in items)
            {
                string itemId = item.Key;
                int count = item.Value;

                if (count <= 0) continue; // Don't show empty items

                GameObject btnObj = Instantiate(itemButtonPrefab, itemsContainer);
                
                // Setup Button Text
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = $"{itemId} ({count})";
                }

                // Setup Click Event
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnItemClicked(itemId));
                }
            }
        }

        private void OnItemClicked(string itemId)
        {
            // Logic to Place Item
            Debug.Log($"Clicked {itemId} from Inventory");
            
            // Close Inventory
            if (inventoryPanel != null) inventoryPanel.SetActive(false);

            // Trigger Placement in BuildingManager
            // For now, hardcode ID mapping or assume ItemID matches something specific
            if (itemId == "SluiceBox")
            {
                // We need reference to Prefab. BuildingManager has it?
                // BuildingManager needs a way to "StartPlacement by ID".
                // Since BuildingManager has sluiceBoxPrefab as public/serialized, we can access it via a new method or modification.
                // Current BuildingManager takes a GameObject prefab.
                // WE SHOULD MODIFY BUILDINGMANAGER TO ACCEPT ID LOOKUP
                
                // For this immediate task, we'll access BuildingManager to start the placement.
                // We need to modify building manager to expose the SluicePrefab or a method to place it.
                // Let's rely on BuildingManager being updated later or assume a lookup.
                // Actually, let's modify BuildingManager to have a simpler entry point: StartPlacement(string buildingId)
                
                BuildingManager.Instance.StartPlacementById(itemId); 
            }
        }
    }
}
