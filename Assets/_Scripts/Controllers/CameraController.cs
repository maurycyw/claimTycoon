using UnityEngine;
using UnityEngine.InputSystem;

namespace ClaimTycoon.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float panBorderThickness = 10f;
        [SerializeField] private Vector2 panLimit = new Vector2(50f, 50f);

        [Header("Zoom Settings")]
        [SerializeField] private float scrollSpeed = 20f;
        [SerializeField] private float minY = 5f;
        [SerializeField] private float maxY = 40f;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 100f;

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Vector3 pos = transform.position;
            
            // New Input System
            bool w = Keyboard.current != null && Keyboard.current.wKey.isPressed;
            bool s = Keyboard.current != null && Keyboard.current.sKey.isPressed;
            bool d = Keyboard.current != null && Keyboard.current.dKey.isPressed;
            bool a = Keyboard.current != null && Keyboard.current.aKey.isPressed;

            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            // Calculate horizontal forward and right vectors
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            if (w || (mousePos.y >= Screen.height - panBorderThickness && mousePos.y < Screen.height))
            {
                pos += forward * panSpeed * Time.deltaTime;
            }
            if (s || (mousePos.y <= panBorderThickness && mousePos.y > 0))
            {
                pos -= forward * panSpeed * Time.deltaTime;
            }
            if (d || (mousePos.x >= Screen.width - panBorderThickness && mousePos.x < Screen.width))
            {
                pos += right * panSpeed * Time.deltaTime;
            }
            if (a || (mousePos.x <= panBorderThickness && mousePos.x > 0))
            {
                pos -= right * panSpeed * Time.deltaTime;
            }

            // Clamp position (optional, based on map size)
            // pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);
            // pos.z = Mathf.Clamp(pos.z, -panLimit.y, panLimit.y);

            transform.position = pos;
        }

        private void HandleRotation()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.qKey.isPressed)
            {
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
            }
            if (Keyboard.current.eKey.isPressed)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void HandleZoom()
        {
            if (Mouse.current == null) return;
            
            float scroll = Mouse.current.scroll.ReadValue().y;
            // Scroll usually returns large values like 120, normalize it a bit
            // or just adjust zoom speed. The old Input.GetAxis returned smaller values.
            scroll *= 0.01f; 

            Vector3 pos = transform.position;

            pos.y -= scroll * scrollSpeed * 100f * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }
    }
}
