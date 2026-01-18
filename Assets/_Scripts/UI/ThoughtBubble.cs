using UnityEngine;
using TMPro;

namespace ClaimTycoon.UI
{
    public class ThoughtBubble : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textComponent;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private float _defaultDuration = 3f;

        private Transform _mainCameraTransform;
        private float _hideTime;
        private bool _isVisible;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInChildren<Canvas>();
            }
            if (_textComponent == null)
            {
                _textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }

            _mainCameraTransform = Camera.main?.transform;
            
            // Should be world space
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
            }

            Hide(); // Start hidden
        }

        private void Update()
        {
            if (_isVisible)
            {
                if (Time.time >= _hideTime)
                {
                    Hide();
                }
                else
                {
                    Billboard();
                }
            }
        }

        public void SetText(string text)
        {
            if (_textComponent != null)
            {
                _textComponent.text = text;
                _textComponent.alignment = TextAlignmentOptions.Center; // Force center
            }
        }

        public void Show(float duration = -1)
        {
            if (duration < 0) duration = _defaultDuration;
            
            _hideTime = Time.time + duration;
            _isVisible = true;
            gameObject.SetActive(true);
            
            Billboard(); // Update immediately
        }

        public void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
        }

        private void Billboard()
        {
            if (_mainCameraTransform != null)
            {
                // Simple LookAt for world space UI to face camera
                // We want it to look at the camera, but maybe we want to just match rotation?
                //transform.LookAt(transform.position + _mainCameraTransform.rotation * Vector3.forward,
                //    _mainCameraTransform.rotation * Vector3.up);
                
                // Usually just copying the camera rotation works well for 2D sprites/UI in 3D
                transform.forward = _mainCameraTransform.forward;
            }
            else
            {
                // Try to find camera if lost
                if (Camera.main != null)
                    _mainCameraTransform = Camera.main.transform;
            }
        }
    }
}
