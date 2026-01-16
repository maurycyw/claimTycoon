using UnityEngine;
using TMPro;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Controllers
{
    public class HUDController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI moneyText;

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
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged -= UpdateGoldUI;
                ResourceManager.Instance.OnMoneyChanged -= UpdateMoneyUI;
            }
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
    }
}
