using UnityEngine;
using UnityEngine.UI;

namespace ClaimTycoon.UI
{
    public class ShopPanelUI : MonoBehaviour
    {
        [Header("Main Tabs")]
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button ordersTabButton;

        [Header("Content")]
        [SerializeField] private Transform contentContainer; // The shell container
        [SerializeField] private GameObject shopViewPrefab;
        [SerializeField] private GameObject ordersViewPrefab;

        private GameObject currentViewObject;

        private void Start()
        {
            if (shopTabButton != null) shopTabButton.onClick.AddListener(() => SwitchMainTab(true));
            if (ordersTabButton != null) ordersTabButton.onClick.AddListener(() => SwitchMainTab(false));

            // Initial State
            SwitchMainTab(true);
        }

        private void SwitchMainTab(bool isShop)
        {
            // Destroy previous view
            if (currentViewObject != null)
            {
                Destroy(currentViewObject);
            }

            // Instantiate new view
            GameObject prefabToSpawn = isShop ? shopViewPrefab : ordersViewPrefab;
            
            if (prefabToSpawn != null && contentContainer != null)
            {
                currentViewObject = Instantiate(prefabToSpawn, contentContainer);
                
                // Reset transform to ensure it fits the container
                RectTransform rt = currentViewObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.localScale = Vector3.one;
                }
            }
            else
            {
                Debug.LogError($"ShopPanelUI: Cannot switch tab! Prefab: {prefabToSpawn}, Container: {contentContainer}");
            }
        }
    }
}
