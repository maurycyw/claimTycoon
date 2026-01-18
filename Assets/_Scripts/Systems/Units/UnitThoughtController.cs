using UnityEngine;
using ClaimTycoon.UI;

namespace ClaimTycoon.Systems.Units
{
    public class UnitThoughtController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ThoughtBubble _thoughtBubblePrefab;
        [SerializeField] private Vector3 _offset = new Vector3(0, 2.5f, 0);

        private ThoughtBubble _currentBubble;

        private void Awake()
        {
            // If we have a prefab, instantiate it. 
            // If users prefer, they can manually place a ThoughtBubble on the unit in the prefab editor
            // and assign it here via some other means, but instantiating a common prefab is easier.
            if (_thoughtBubblePrefab != null)
            {
                CreateBubble();
            }
        }

        private void CreateBubble()
        {
            if (_currentBubble == null && _thoughtBubblePrefab != null)
            {
                // Instantiate as child so it moves with unit
                _currentBubble = Instantiate(_thoughtBubblePrefab, transform);
                _currentBubble.transform.localPosition = _offset;
            }
        }

        private void Update()
        {
            // Force the bubble to stay at the offset position in world space relative to the unit, 
            // ignoring unit rotation potentially affecting local offset if axes are weird.
            if (_currentBubble != null && _currentBubble.isActiveAndEnabled)
            {
                // We keep it as a child for hierarchy cleanliness, but we override transform.position
                // Parent rotation * localPosition = worldOffset relative to parent.
                // If parent rotation is rotating the offset, we see it move.
                // We want: Position = Unit.position + _offset (if _offset is intended as World Up/Right/Forward relative to Identity unit)
                // Assuming _offset.y is "Up", we just want Unit.position + _offset.
                
                _currentBubble.transform.position = transform.position + _offset;
            }
        }

        public void ShowThought(string text, float duration = 3f)
        {
            if (_currentBubble == null)
            {
                // Try to find one if not instantiated or assigned
                _currentBubble = GetComponentInChildren<ThoughtBubble>();
                
                // If still null and we have a prefab, try creating it again (maybe it was destroyed)
                if (_currentBubble == null && _thoughtBubblePrefab != null)
                {
                    CreateBubble();
                }
            }

            if (_currentBubble != null)
            {
                _currentBubble.SetText(text);
                _currentBubble.Show(duration);
                // Force update position immediately
                 _currentBubble.transform.position = transform.position + _offset;
            }
            else
            {
                Debug.LogWarning($"[UnitThoughtController] No ThoughtBubble found or prefab assigned on {name}");
            }
        }
    }
}
