using System;
using UnityEngine;

namespace ClaimTycoon.Managers
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public float GoldAmount { get; private set; }
        public float MoneyAmount { get; private set; }

        // Events for UI updates
        public event Action<float> OnGoldChanged;
        public event Action<float> OnMoneyChanged;

        [Header("Settings")]
        [SerializeField] private float goldPricePerUnit = 50f; // $50 per 1.0 gold

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Initialize UI
            OnGoldChanged?.Invoke(GoldAmount);
            OnMoneyChanged?.Invoke(MoneyAmount);
        }

        public void AddGold(float amount)
        {
            GoldAmount += amount;
            OnGoldChanged?.Invoke(GoldAmount);
        }

        public void AddMoney(float amount)
        {
            MoneyAmount += amount;
            OnMoneyChanged?.Invoke(MoneyAmount);
        }

        public void SellGold()
        {
            if (GoldAmount > 0)
            {
                float revenue = GoldAmount * goldPricePerUnit;
                AddMoney(revenue);
                
                GoldAmount = 0;
                OnGoldChanged?.Invoke(GoldAmount);
                
                Debug.Log($"Sold gold for ${revenue}");
            }
        }
    }
}
