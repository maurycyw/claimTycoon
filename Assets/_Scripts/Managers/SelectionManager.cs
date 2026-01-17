using UnityEngine;
using System;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.Managers
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        public UnitController SelectedUnit { get; private set; }

        public event Action<UnitController> OnUnitSelected;
        public event Action OnUnitDeselected;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SelectUnit(UnitController unit)
        {
            if (SelectedUnit == unit) return; // Already selected

            // Deselect previous if any
            if (SelectedUnit != null)
            {
                // Optional: visual feedback for deselection on the unit itself
                OnUnitDeselected?.Invoke();
            }

            SelectedUnit = unit;

            if (SelectedUnit != null)
            {
                Debug.Log($"Selected Unit: {unit.name}");
                OnUnitSelected?.Invoke(SelectedUnit);
            }
            else
            {
                Deselect();
            }
        }

        public void Deselect()
        {
            SelectedUnit = null;
            OnUnitDeselected?.Invoke();
            Debug.Log("Deselected Unit");
        }
    }
}
