using System.Collections.Generic;
using UnityEngine;
using System;

namespace ClaimTycoon.Systems.Inventories
{
    [Serializable]
    public class Inventory
    {
        // Dictionary to store items: ItemID -> Count
        private Dictionary<string, int> items = new Dictionary<string, int>();

        public event Action OnInventoryChanged;

        public Inventory()
        {
            // Default Starting Items
            AddItem("SluiceBox", 1);
        }

        public void AddItem(string itemId, int amount)
        {
            if (items.ContainsKey(itemId))
            {
                items[itemId] += amount;
            }
            else
            {
                items[itemId] = amount;
            }
            OnInventoryChanged?.Invoke();
            Debug.Log($"Inventory: Added {amount} {itemId}. Total: {items[itemId]}");
        }

        public bool RemoveItem(string itemId, int amount)
        {
            if (items.ContainsKey(itemId) && items[itemId] >= amount)
            {
                items[itemId] -= amount;
                OnInventoryChanged?.Invoke();
                Debug.Log($"Inventory: Removed {amount} {itemId}. Remaining: {items[itemId]}");
                return true;
            }
            return false;
        }

        public int GetItemCount(string itemId)
        {
            if (items.ContainsKey(itemId)) return items[itemId];
            return 0;
        }
        public Dictionary<string, int> GetAllItems()
        {
            return new Dictionary<string, int>(items); // Return copy to protect internal state
        }
    }
}
