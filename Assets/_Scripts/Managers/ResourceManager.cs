using UnityEngine;
using System;
using System.Collections.Generic;
using ClaimTycoon.Systems.Inventories; // Add Namespace

namespace ClaimTycoon.Managers
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public float GoldAmount { get; private set; }
        public float MoneyAmount { get; private set; }

        public Inventory PlayerInventory { get; private set; } // The Inventory Object

        // Events
        public event Action<float> OnGoldChanged;
        public event Action<float> OnMoneyChanged;

        [Header("Settings")]
        [SerializeField] private float goldPricePerUnit = 50f; 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                PlayerInventory = new Inventory(); // Create the Object
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        // ... (Start, AddGold, AddMoney, SellGold, SetResources unchanged) ...
        
        private void Start()
        {
            OnGoldChanged?.Invoke(GoldAmount);
            OnMoneyChanged?.Invoke(MoneyAmount);
            
            // Give initial items for testing
            if (PlayerInventory.GetItemCount("SluiceBox") == 0)
            {
                PlayerInventory.AddItem("SluiceBox", 1);
            }
        }

        // ... (Rest of existing methods)
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
            }
        }
        
        public void SetResources(float money, float gold)
        {
             MoneyAmount = money;
            GoldAmount = gold;
            OnMoneyChanged?.Invoke(MoneyAmount);
            OnGoldChanged?.Invoke(GoldAmount);
        }
    }
}
