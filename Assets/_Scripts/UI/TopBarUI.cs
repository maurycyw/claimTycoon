using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.TimeSystem;

namespace ClaimTycoon.UI
{
    public class TopBarUI : MonoBehaviour
    {
        [Header("Resource References")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI moneyText;

        [Header("Time References")]
        [SerializeField] private TextMeshProUGUI dateText;

        [Header("Menu References")]
        [SerializeField] private Button menuButton;

        private void Start()
        {
            // Subscribe to Resources
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged += UpdateGold;
                ResourceManager.Instance.OnMoneyChanged += UpdateMoney;
                
                // Initial Update
                UpdateGold(ResourceManager.Instance.GoldAmount);
                UpdateMoney(ResourceManager.Instance.MoneyAmount);
            }

            // Subscribe to Time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged += UpdateTime;
                UpdateTime();
            }

            // Menu Button
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuClicked);
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged -= UpdateGold;
                ResourceManager.Instance.OnMoneyChanged -= UpdateMoney;
            }

            if (TimeManager.Instance != null)
            {
                 TimeManager.Instance.OnTimeChanged -= UpdateTime;
            }
        }

        private void UpdateGold(float amount)
        {
            if (goldText != null) goldText.text = $"{amount:F1} Oz";
        }

        private void UpdateMoney(float amount)
        {
            if (moneyText != null) moneyText.text = $"${amount:F2}";
        }

        private void UpdateTime()
        {
            if (dateText != null && TimeManager.Instance != null)
            {
                dateText.text = $"Day {TimeManager.Instance.Day}, {TimeManager.Instance.GetTimeString()}";
            }
        }

        private void OnMenuClicked()
        {
            Debug.Log("Menu Clicked - Open Pause/Settings Menu (To Be Implemented)");
        }
    }
}
