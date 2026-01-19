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
        [SerializeField] private LayerMask groundLayer;

        private void Start()
        {
            if (GetComponent<Camera>() != null)
            {
                // Fix Z-Fighting / Ring Artifacts by increasing Near Clip
                GetComponent<Camera>().nearClipPlane = 0.5f;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        private void HandleMovement()
        {
            // Block Movement if Panel is Open
            if (HUDController.Instance != null && HUDController.Instance.IsAnyPanelOpen) return;

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

            float rotateDir = 0f;
            if (Keyboard.current.qKey.isPressed) rotateDir = -1f;
            if (Keyboard.current.eKey.isPressed) rotateDir = 1f;

            if (rotateDir != 0f)
            {
                // Orbit around pivot if hitting ground, else rotate self
                Ray ray = new Ray(transform.position, transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 300f, groundLayer))
                {
                     transform.RotateAround(hit.point, Vector3.up, rotateDir * rotationSpeed * Time.deltaTime);
                }
                else
                {
                     transform.Rotate(Vector3.up, rotateDir * rotationSpeed * Time.deltaTime, Space.World);
                }
            }
        }

        private void HandleZoom()
        {
            if (Mouse.current == null) return;
            
            float scroll = Mouse.current.scroll.ReadValue().y;
            scroll *= 0.01f; 

            if (Mathf.Abs(scroll) > 0.001f)
            {
                Vector3 moveDir = transform.forward * scroll * scrollSpeed * 100f * Time.deltaTime;
                Vector3 newPos = transform.position + moveDir;

                // Clamp based on Height (Y)
                if (newPos.y < minY)
                {
                    // Clamp to MinY
                    // We need to back off along the vector until Y is at least MinY
                    // Or just stop moving.
                    // Simple clamp: just don't apply if it goes too low.
                    // Better: Project to boundary.
                    newPos = transform.position; // Cancel move
                }
                else if (newPos.y > maxY)
                {
                    newPos = transform.position; // Cancel move
                }

                transform.position = newPos;
            }
        }

        public void FocusOn(Vector3 targetData)
        {
            // We want to position the camera such that 'targetData' is in the center.
            // Current rotation is preserved.
            // We back up from the target by some distance along the inverse forward vector.
            
            // Calculate current distance from ground/pivot (or just pick a default zoom)
            // Let's assume we want to maintain the current height Y relative to the target?
            // Or just set a fixed height?
            
            float targetHeight = 20f; // Default nice viewing height
            if (transform.position.y > minY && transform.position.y < maxY)
            {
                targetHeight = transform.position.y;
            }

            // Raycast strategy:
            // Forward vector points at ground.
            // We want (CameraPos + Forward * d) = TargetPos
            // So CameraPos = TargetPos - Forward * d.
            
            // We need to find 'd' such that CameraPos.y = targetHeight.
            // CameraPos.y = TargetPos.y - Forward.y * d
            // d = (TargetPos.y - CameraPos.y) / Forward.y  <-- Wait, we want to SET CameraPos.y
            // Let's use specific height.
            
            // Simply:
            // 1. Get Forward vector.
            Vector3 forward = transform.forward;
            
            // 2. We want Camera.y = targetHeight.
            // 3. We want the ray from Camera along Forward to hit Target (at y=0 usually, or Target.y).
            // Let's assume we look at Target.y.
            
            if (Mathf.Abs(forward.y) < 0.001f) return; // Looking parallel to horizon, can't focus down.

            float heightDiff = targetHeight - targetData.y;
            // forward.y is typically negative (looking down).
            // We want to go BACKWARDS (Negative Forward) from Target.
            // T = C + F * dist
            // C = T - F * dist
            // C.y = T.y - F.y * dist
            // targetHeight = T.y - F.y * dist
            // F.y * dist = T.y - targetHeight
            // dist = (T.y - targetHeight) / F.y
            
            float dist = (targetData.y - targetHeight) / forward.y;
            
            Vector3 newPos = targetData - forward * dist;
            transform.position = newPos;
            
            Debug.Log($"[CameraController] Focused on {targetData}. New Pos: {newPos}");
        }
    }
}
