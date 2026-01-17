using UnityEngine;
using TMPro;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Units;

namespace ClaimTycoon.Controllers
{
    public class HUDController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private InventoryUI inventoryUI;

        [Header("Stats Panel")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI statsContentText; // Simple text for list for now, or use container

        private void Start()
        {
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

        private void UpdateStatsDisplay(UnitController unit)
        {
            if (statsContentText == null) return;
            
            CharacterStats stats = unit.GetComponent<CharacterStats>();
            if (stats == null) return;

            // Simple string builder for prototype
            string content = "";
            
            // Allow access to stats list? Or expose it via method?
            // CharacterStats doesn't expose list directly yet. 
            // We can add a specialized method or just iterate common types.
            
            Stat mining = stats.GetStat(StatType.Mining);
            if (mining != null) content += $"Mining Lvl {mining.level}\n";
            
            Stat moving = stats.GetStat(StatType.MoveSpeed);
            if (moving != null) content += $"Speed Lvl {moving.level}\n";

            statsContentText.text = content;
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

        // Called by Button event in Inspector
        public void OnSellButtonClicked()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SellGold();
            }
        }

        public void OnInventoryButtonClicked()
        {
            if (inventoryUI != null)
            {
                inventoryUI.ToggleInventory();
            }
        }
    }
}
