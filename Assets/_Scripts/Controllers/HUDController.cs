using UnityEngine;
using TMPro;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Units;
using UnityEngine.EventSystems;

namespace ClaimTycoon.Controllers
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] public ShopController shopController; // Made public for BottomBarUI visual sync

        [Header("Stats Panel")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI statsContentText; // Simple text for list for now, or use container

        public event System.Action OnPanelStateChanged;
        public bool IsInventoryOpen => inventoryUI != null && inventoryUI.isActiveAndEnabled;
        public bool IsAnyPanelOpen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find references if missing (Fallbacks) - Include Inactive!
            if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>(true);
            if (shopController == null) shopController = FindObjectOfType<ShopController>(true);

            // Subscribe to events
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged += UpdateGoldUI;
                ResourceManager.Instance.OnMoneyChanged += UpdateMoneyUI;

                // Initial update
                UpdateGoldUI(ResourceManager.Instance.GoldAmount);
                UpdateMoneyUI(ResourceManager.Instance.MoneyAmount);
            }

            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected += OnUnitSelected;
                SelectionManager.Instance.OnUnitDeselected += OnUnitDeselected;
            }

            if (statsPanel != null) statsPanel.SetActive(false);
            
            // Ensure correct initial state
            NotifyPanelStateChange();
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged -= UpdateGoldUI;
                ResourceManager.Instance.OnMoneyChanged -= UpdateMoneyUI;
            }
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected -= OnUnitSelected;
                SelectionManager.Instance.OnUnitDeselected -= OnUnitDeselected;
            }
        }

        #region Panel Management

        public void ToggleInventory()
        {
             if (inventoryUI == null)
            {
                Debug.LogError("[HUDController] Cannot Toggle Inventory: InventoryUI reference is missing!");
                return;
            }

            // If opening Inventory, close Shop
            bool isOpening = !inventoryUI.isActiveAndEnabled; // Simplified check
            
            if (isOpening)
            {
                CloseAllPanels();
                inventoryUI.gameObject.SetActive(true);
            }
            else
            {
                inventoryUI.gameObject.SetActive(false);
            }
            
            NotifyPanelStateChange();
        }

        public void ToggleShop()
        {
            if (shopController == null) return;

            bool isOpening = !shopController.IsShopOpen; // Check via property we will add
            
            if (isOpening)
            {
                CloseAllPanels();
                shopController.SetShopActive(true);
            }
            else
            {
                shopController.SetShopActive(false);
            }

            NotifyPanelStateChange();
        }

        public void CloseAllPanels()
        {
            if (inventoryUI != null) inventoryUI.gameObject.SetActive(false);
            if (shopController != null) shopController.SetShopActive(false);
            // Add other panels here (Staff, etc.)
        }

        private void NotifyPanelStateChange()
        {
            bool inventoryOpen = inventoryUI != null && inventoryUI.isActiveAndEnabled;
            bool shopOpen = shopController != null && shopController.IsShopOpen;

            IsAnyPanelOpen = inventoryOpen || shopOpen;

            // Handle EventSystem Navigation
            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = IsAnyPanelOpen;
            }

            Debug.Log($"[HUDController] Panel State Updated. AnyOpen: {IsAnyPanelOpen}");
            OnPanelStateChanged?.Invoke();
        }

        #endregion

        #region Stats & Resource UI

        private void OnUnitSelected(UnitController unit)
        {
            if (statsPanel != null) statsPanel.SetActive(true);
            if (unitNameText != null) unitNameText.text = unit.name;

            UpdateStatsDisplay(unit);
            
            // Subscribe to dynamic stat changes if possible
            CharacterStats stats = unit.GetComponent<CharacterStats>();
            if (stats != null) stats.OnStatChanged += (s) => UpdateStatsDisplay(unit);
        }

        private void OnUnitDeselected()
        {
            if (statsPanel != null) statsPanel.SetActive(false);
        }

        private void Update()
        {
             // Continuously update stats/status if a unit is selected and panel is open
             if (SelectionManager.Instance != null && SelectionManager.Instance.SelectedUnit != null && statsPanel != null && statsPanel.activeSelf)
             {
                 UpdateStatsDisplay(SelectionManager.Instance.SelectedUnit);
             }
        }

        private void UpdateStatsDisplay(UnitController unit)
        {
            if (statsContentText == null) return;
            
            // Replaced stats logic with Activity Status as requested ("Idle", "Digging" etc)
            statsContentText.text = unit.GetActivityStatus();
        }

        private void UpdateGoldUI(float amount)
        {
            if (goldText != null)
                goldText.text = $"Gold: {amount:F1} oz";
        }

        private void UpdateMoneyUI(float amount)
        {
            if (moneyText != null)
                moneyText.text = $"$: {amount:F0}";
        }
        
        #endregion

        #region Button Events

        public void OnSellButtonClicked()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SellGold();
            }
        }

        public void OnInventoryButtonClicked()
        {
            ToggleInventory();
        }

        public void OnShopButtonClicked()
        {
            ToggleShop();
        }
        
        #endregion
    }
}
