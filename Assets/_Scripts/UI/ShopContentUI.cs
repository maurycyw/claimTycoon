using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Inventories;

namespace ClaimTycoon.UI
{
    public class ShopContentUI : MonoBehaviour
    {
        [Header("Shop Categories")]
        [SerializeField] private Button equipmentButton;
        [SerializeField] private Button suppliesButton;
        [SerializeField] private Button landButton;
        [SerializeField] private Button buildingsButton;
        [SerializeField] private Button sellButton;

        [Header("Containers")]
        [SerializeField] private GameObject categoriesContainer;
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private Button backButton;

        [Header("Item List")]
        [SerializeField] private GameObject shopItemPrefab;

        // [Header("Item Icons")] - Removed
        // Fields removed: sluiceBoxIcon, fuelIcon, etc.

        private enum ShopCategory { Equipment, Supplies, Land, Buildings, Sell }
        private ShopCategory currentCategory = ShopCategory.Equipment;
        // ... Start and other methods unchanged ...

        private void RefreshItems()
        {
            if (itemsContainer != null)
            {
                foreach (Transform child in itemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            Debug.Log($"[ShopContentUI] Refreshing. Category: {currentCategory}");

            if (currentCategory == ShopCategory.Sell)
            {
                Debug.Log("[ShopContentUI] Creating Sell Gold item...");
                // Sell Gold is special, might not be in DB or is special case. Let's keep icon lookup safe.
                Sprite goldIcon = null;
                if (ItemDatabase.Instance != null) goldIcon = ItemDatabase.Instance.GetItemIcon("Gold");
                
                CreateItem("Sell Gold", "Sell all gold for cash", () => 
                { 
                    ResourceManager.Instance.SellGold(); 
                    RefreshItems(); 
                }, 0, goldIcon); 
            }
            else
            {
                 switch (currentCategory)
                {
                    case ShopCategory.Equipment:
                        CreateShopItemFromDB("SluiceBox");
                        break;
                    case ShopCategory.Supplies:
                        CreateShopItemFromDB("Fuel");
                        break;
                    case ShopCategory.Land:
                         CreateShopItemFromDB("LandClaim");
                         break;
                    case ShopCategory.Buildings:
                         CreateShopItemFromDB("Shack");
                         break;
                }
            }

            // FORCE LAYOUT REBUILD & RESET SCROLL
            if (itemsContainer != null)
            {
                RectTransform rt = itemsContainer.GetComponent<RectTransform>();
                if (rt != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }
        
        private void CreateShopItemFromDB(string id)
        {
            if (ItemDatabase.Instance == null) return;
            var def = ItemDatabase.Instance.GetItemDef(id);
            CreateItem(def.displayName, def.description, () => BuyItem(def.id, def.basePrice), def.basePrice, def.icon);
        }



        private void CreateItem(string name, string desc, UnityEngine.Events.UnityAction onClick, int price, Sprite icon = null)
        {
            if (shopItemPrefab == null)
            {
                Debug.LogError("ShopContentUI: shopItemPrefab is missing!");
                return;
            }
            if (itemsContainer == null)
            {
                Debug.LogError("ShopContentUI: itemsContainer is missing!");
                return;
            }

            GameObject item = Instantiate(shopItemPrefab, itemsContainer);
            
            // Debug Layout info
            item.transform.localScale = Vector3.one; 
            item.SetActive(true); 
            
            // FORCE SIZE
            LayoutElement le = item.GetComponent<LayoutElement>();
            if (le == null) le = item.AddComponent<LayoutElement>();
            
            le.minHeight = 60f; 
            le.flexibleWidth = 1f; 
            
            // Icon Setup
            if (icon != null)
            {
                Image[] images = item.GetComponentsInChildren<Image>();
                foreach (Image img in images)
                {
                    if (img.gameObject != item && img.gameObject.GetComponent<Button>() == null)
                    {
                        img.sprite = icon;
                        break; 
                    }
                }
            }

            // Find Button (InChildren now!)
            Button btn = item.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(onClick);

                // Set Interactable based on Affordability
                if (currentCategory == ShopCategory.Sell)
                {
                    // For Sell, enable only if we have gold
                    btn.interactable = ResourceManager.Instance.GoldAmount > 0;
                }
                else
                {
                    // For Buy, enable only if we have enough money
                    if (ResourceManager.Instance != null)
                    {
                        btn.interactable = ResourceManager.Instance.MoneyAmount >= price;
                    }
                }
                
                // Set Button Text (Buy/Sell)
                TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = (currentCategory == ShopCategory.Sell) ? "Sell" : "Buy";
                }
            }

            // Text Setup (Name/Desc/Price)
            TextMeshProUGUI[] allTexts = item.GetComponentsInChildren<TextMeshProUGUI>();
            int textIndex = 0;
            
            foreach (var t in allTexts)
            {
                // Skip if this is the button's text
                if (btn != null && t.transform.IsChildOf(btn.transform)) continue;

                if (textIndex == 0)
                {
                    // Main Text: Name + Price
                    if (price > 0)
                        t.text = $"{name} - ${price}";
                    else
                        t.text = name;
                }
                else if (textIndex == 1)
                {
                    // Description
                    t.text = desc;
                }
                textIndex++;
            }
            
            Debug.Log($"[ShopContentUI] Created '{name}'. Parent: {item.transform.parent.name}, Scale: {item.transform.localScale}");
        }

        private void Start()
        {
            // Debug References
            if (categoriesContainer == null) Debug.LogError("ShopContentUI: 'Categories Container' is not assigned!");
            if (itemsContainer == null) Debug.LogError("ShopContentUI: 'Items Container' is not assigned!");
            if (backButton == null) Debug.LogError("ShopContentUI: 'Back Button' is not assigned!");

            // Categories
            if (equipmentButton != null) equipmentButton.onClick.AddListener(() => SwitchCategory(ShopCategory.Equipment));
            if (suppliesButton != null) suppliesButton.onClick.AddListener(() => SwitchCategory(ShopCategory.Supplies));
            if (landButton != null) landButton.onClick.AddListener(() => SwitchCategory(ShopCategory.Land));
            if (buildingsButton != null) buildingsButton.onClick.AddListener(() => SwitchCategory(ShopCategory.Buildings));
            if (sellButton != null) sellButton.onClick.AddListener(() => SwitchCategory(ShopCategory.Sell));

            // Back Button
            if (backButton != null) backButton.onClick.AddListener(ShowCategories);

            // Initial State: Show Categories
            ShowCategories();
        }

        private void ShowCategories()
        {
            if (categoriesContainer != null) categoriesContainer.SetActive(true);
            if (itemsContainer != null) itemsContainer.gameObject.SetActive(false); // Hide the ScrollView/Container
            if (backButton != null) backButton.gameObject.SetActive(false);
        }

        private void SwitchCategory(ShopCategory category)
        {
            currentCategory = category;
            
            // Hide Categories, Show Items
            if (categoriesContainer != null) categoriesContainer.SetActive(false);
            if (itemsContainer != null) itemsContainer.gameObject.SetActive(true);
            if (backButton != null) backButton.gameObject.SetActive(true);

            RefreshItems();
        }
        private void BuyItem(string itemId, int cost)
        {
             if (ResourceManager.Instance.MoneyAmount >= cost)
            {
                ResourceManager.Instance.AddMoney(-cost);
                ResourceManager.Instance.PlayerInventory.AddItem(itemId, 1);
                Debug.Log($"Bought {itemId}");
                
                // Refresh to update button states (disable unaffordable ones)
                RefreshItems();
            }
            else
            {
                if (AlertManager.Instance != null) AlertManager.Instance.ShowAlert("Not enough money!");
            }
        }
    }
}
