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

        private void UpdateUI(UnitController unit)
        {
            if (contentPanel != null) contentPanel.SetActive(true);
            
            if (nameText != null) nameText.text = unit.name;

            if (statsText != null)
            {
                CharacterStats stats = unit.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    // Logic from old HUDController, potentially expanded
                    string content = "";
                    Stat mining = stats.GetStat(StatType.Mining);
                    if (mining != null) content += $"Mining: Lvl {mining.level}\n";
                    
                    Stat speed = stats.GetStat(StatType.MoveSpeed);
                    if (speed != null) content += $"Speed: Lvl {speed.level}\n";
                    
                    // Add more stats as needed
                    statsText.text = content;
                }
                else
                {
                    statsText.text = "No Stats Available";
                }
            }
        }

        private void HideUI()
        {
            if (contentPanel != null) contentPanel.SetActive(false);
        }
    }
}
