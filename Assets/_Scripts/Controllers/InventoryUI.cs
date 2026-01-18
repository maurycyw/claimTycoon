using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Inventories; // Added namespace

namespace ClaimTycoon.Controllers
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        // [SerializeField] private GameObject inventoryPanel; // Removed: The script IS the panel
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemButtonPrefab;
        // [SerializeField] private Button toggleButton; // Removed: Handled by BottomBarUI

        private void Awake()
        {
            // Optional: Ensure we are hidden at start if not handled by a parent manager
            // But usually we want to just let the scene setup decide or BottomBarUI close us.
        }

        private void OnEnable()
        {
            // Listen for Inventory Changes
            if (ResourceManager.Instance != null && ResourceManager.Instance.PlayerInventory != null)
            {
                ResourceManager.Instance.PlayerInventory.OnInventoryChanged += RefreshInventory;
            }
            
            // Also Refresh immediately when opened
            RefreshInventory();
        }

        private void OnDisable()
        {
             if (ResourceManager.Instance != null && ResourceManager.Instance.PlayerInventory != null)
            {
                ResourceManager.Instance.PlayerInventory.OnInventoryChanged -= RefreshInventory;
            }
        }

        // Logic for External Toggle (called by BottomBarUI)
        public void ToggleVisibility()
        {
            if (HUDController.Instance != null)
            {
                HUDController.Instance.ToggleInventory();
            }
            else
            {
                // Fallback
                bool isActive = !gameObject.activeSelf;
                gameObject.SetActive(isActive);
            }
        }
        
        // Keep compatibility if BottomBarUI calls ToggleInventory
        public void ToggleInventory() => ToggleVisibility();

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
            Debug.Log($"[InventoryUI] Refreshing. Found {items.Count} items.");

            // Force Container Size to be valid if it's zero
            RectTransform containerRect = itemsContainer.GetComponent<RectTransform>();
            if (containerRect.rect.width < 10f)
            {
                Debug.LogWarning("[InventoryUI] Content Width was 0! Forcing to 500.");
                containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            }

            foreach (var item in items)
            {
                string itemId = item.Key;
                int count = item.Value;

                if (count <= 0) continue; // Don't show empty items

                GameObject btnObj = Instantiate(itemButtonPrefab, itemsContainer);
                btnObj.transform.localScale = Vector3.one;
                btnObj.transform.localRotation = Quaternion.identity;
                
                // Force Layout Element to ensure size
                LayoutElement layoutElem = btnObj.GetComponent<LayoutElement>();
                if (layoutElem == null) layoutElem = btnObj.AddComponent<LayoutElement>();
                layoutElem.minHeight = 100f; // Ensure it has size
                layoutElem.preferredHeight = 100f;
                layoutElem.minWidth = 300f; // Force Width too!
                layoutElem.preferredWidth = 300f;
                layoutElem.flexibleHeight = 0f;

                // Get Components
                TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
                
                if (texts.Length >= 2)
                {
                    texts[0].text = itemId; // Name (Fallback)
                    texts[1].text = $"x{count}"; // Count
                    
                    // Try to get DisplayName from DB
                    if (ItemDatabase.Instance != null)
                    {
                        var def = ItemDatabase.Instance.GetItemDef(itemId);
                        if (!string.IsNullOrEmpty(def.displayName)) texts[0].text = def.displayName;
                    }
                }
                
                // Setup Icon
                if (ItemDatabase.Instance != null)
                {
                    Sprite icon = ItemDatabase.Instance.GetItemIcon(itemId);
                    if (icon != null)
                    {
                        // Find Image component that isn't the button background (optional heuristic)
                        // Or Just look for an Image named "Icon" or assume first image is it?
                        // Let's look for components in children.
                        Image[] images = btnObj.GetComponentsInChildren<Image>();
                         foreach (Image img in images)
                        {
                            // Avoid changing the button's own background if it's the root or typical setup
                            // Usually Main Button has Image. Child has Icon.
                            if (img.gameObject != btnObj && img.gameObject.transform.parent != itemsContainer)
                            {
                                img.sprite = icon;
                                img.color = Color.white; // Ensure visibility
                                break;
                            }
                        }
                    }
                }

                // Setup Button
                Button actionBtn = btnObj.GetComponentInChildren<Button>();
                if (actionBtn != null)
                {
                    Debug.Log($"[InventoryUI] Found Button on {itemId}. Setting up...");
                    TextMeshProUGUI btnLabel = actionBtn.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (itemId == "SluiceBox")
                    {
                        if (btnLabel != null) btnLabel.text = "Place";
                        
                        actionBtn.onClick.RemoveAllListeners();
                        actionBtn.onClick.AddListener(() => OnItemClicked(itemId));
                        actionBtn.interactable = true; // Ensure interactable
                        Debug.Log($"[InventoryUI] Added 'Place' listener for {itemId}");
                    }
                    else
                    {
                        if (btnLabel != null) btnLabel.text = "Use";
                         actionBtn.interactable = false; 
                    }
                }
                else
                {
                    Debug.LogError($"[InventoryUI] No Button component found on {btnObj.name}!");
                }
            }
        }

        private void OnItemClicked(string itemId)
        {
            Debug.Log($"Clicked {itemId} from Inventory");
            
            // Close Inventory
            gameObject.SetActive(false);

            if (itemId == "SluiceBox")
            {
                 BuildingManager.Instance.StartPlacementById(itemId); 
            }
        }
    }
}
