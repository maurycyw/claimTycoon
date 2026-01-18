using UnityEngine;
using TMPro;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Units;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.UI
{
    public class UnitStatsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject contentPanel; // The panel showing the stats
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI activityText;

        private void Start()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected += UpdateUI;
                SelectionManager.Instance.OnUnitDeselected += HideUI;
            }

            HideUI();
        }

        private void OnDestroy()
        {
             if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnUnitSelected -= UpdateUI;
                SelectionManager.Instance.OnUnitDeselected -= HideUI;
            }
        }

        private void Update()
        {
            if (SelectionManager.Instance != null && SelectionManager.Instance.SelectedUnit != null)
            {
                 // If the panel is active and we have a selected unit, strictly referring to it might be safer
                 // But since UpdateUI is event based, we track nothing.
                 // Better: If panel is active, we assume we should show info for the SelectedUnit.
                 if (contentPanel != null && contentPanel.activeSelf)
                 {
                     // Force update text for dynamic status
                     UpdateUI(SelectionManager.Instance.SelectedUnit);
                 }
            }
        }

        private void UpdateUI(UnitController unit)
        {
            if (contentPanel != null) contentPanel.SetActive(true);
            
            if (nameText != null) nameText.text = unit.name;

                if (statsText != null)
                {
                    string status = unit.GetActivityStatus();
                    
                    // Optional: If user wants stats AND status, we could append. 
                    // But user specifically said "it shows stats... instead of Idle", implying they want Idle.
                    // We will prioritize the status string.
                    statsText.text = status;
                }

                if (activityText != null)
                {
                    // If activityText is ALSO assigned, we can replicate the text or leave it blank/redundant.
                    // But to avoid confusion, let's keep it consistent.
                    activityText.text = unit.GetActivityStatus();
                }
        }

        private void HideUI()
        {
            if (contentPanel != null) contentPanel.SetActive(false);
        }
    }
}
