using UnityEngine;
// 1. Add namespace for New Input System if enabled
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SceneMemoRuntime : MonoBehaviour
{
    void Update()
    {
        bool rightClick = false;
        bool ctrlPressed = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // --- NEW INPUT SYSTEM LOGIC ---
        // Check if devices exist to prevent errors
        if (Mouse.current != null && Keyboard.current != null)
        {
            rightClick = Mouse.current.rightButton.wasPressedThisFrame;
            ctrlPressed = Keyboard.current.ctrlKey.isPressed;
            mousePos = Mouse.current.position.ReadValue();
        }
#else
        // --- LEGACY INPUT LOGIC ---
        rightClick = Input.GetMouseButtonDown(1);
        ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        mousePos = Input.mousePosition;
#endif

        // --- EXECUTE ---
        if (rightClick && ctrlPressed)
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;
            Vector3 spawnPos;

            if (Physics.Raycast(ray, out hit))
                spawnPos = hit.point;
            else
                spawnPos = ray.origin + ray.direction * 5f;

            SceneNote.CreateNote(spawnPos, this.transform);

            Debug.Log($"[SceneMemo] Note created at {spawnPos}. WARNING: Objects created in Play Mode will disappear when you stop!");
        }
    }
}