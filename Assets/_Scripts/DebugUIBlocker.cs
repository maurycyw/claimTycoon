using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DebugUIBlocker : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current == null) return;

        // Check if mouse is over UI
        if (EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                 // The top-most object is index 0
                 Debug.Log($"[UI Debug] Mouse is over: {results[0].gameObject.name} (Parent: {results[0].gameObject.transform.parent.name})");
            }
        }
    }
}
