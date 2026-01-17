using UnityEngine;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Controllers
{
    public class ShopController : MonoBehaviour
    {
        [Header("Shop Settings")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject sluiceBoxPrefab;
        [SerializeField] private int sluiceBoxCost = 0; // Free for now



        private void Start()
        {
            // Ensure shop is closed by default
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }
        
        public void ToggleShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(!shopPanel.activeSelf);
            }
        }

        public void BuySluiceBox()
        {
            if (BuildingManager.Instance != null && sluiceBoxPrefab != null)
            {
                // Start placement for Sluice Box (isSluice = true)
                BuildingManager.Instance.StartPlacement(sluiceBoxPrefab, sluiceBoxCost, true);
                
                // Keep shop open? or Close it? 
                // Let's close it so they can see where they move the mouse
                if (shopPanel != null) shopPanel.SetActive(false);
            }
            else
            {
                Debug.LogError("ShopController: Missing BuildingManager or SluiceBoxPrefab!");
            }
        }
    }
}
