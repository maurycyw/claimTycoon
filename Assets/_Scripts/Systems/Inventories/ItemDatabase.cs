using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ClaimTycoon.Systems.Inventories
{
    [System.Serializable]
    public struct ItemDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public int basePrice;
    }

    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        public List<ItemDefinition> allItems;

        private Dictionary<string, ItemDefinition> itemLookup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeLookup();
        }

        private void InitializeLookup()
        {
            itemLookup = new Dictionary<string, ItemDefinition>();
            foreach (var item in allItems)
            {
                if (!itemLookup.ContainsKey(item.id))
                {
                    itemLookup.Add(item.id, item);
                }
            }
        }

        public ItemDefinition GetItemDef(string id)
        {
            if (itemLookup == null) InitializeLookup();
            
            if (itemLookup.TryGetValue(id, out ItemDefinition def))
            {
                return def;
            }
            
            Debug.LogWarning($"[ItemDatabase] Item ID '{id}' not found!");
            return new ItemDefinition { id = id, displayName = id, description = "Unknown Item", basePrice = 0 };
        }

        public Sprite GetItemIcon(string id)
        {
            return GetItemDef(id).icon;
        }
    }
}
