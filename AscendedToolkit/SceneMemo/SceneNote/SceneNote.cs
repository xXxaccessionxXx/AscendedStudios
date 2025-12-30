using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SceneNote : MonoBehaviour
{
    [TextArea(3, 10)] public string message = "New Task";
    public Color color = new Color(1f, 0.8f, 0.2f); // Default Sticky Note Yellow
    public bool isResolved = false;

    // --- STATIC CREATION (Accessible from Editor and Game) ---
    public static SceneNote CreateNote(Vector3 position, Transform parent = null)
    {
        GameObject go = new GameObject("New Note");
        go.transform.position = position;
        if (parent != null) go.transform.SetParent(parent);

        SceneNote note = go.AddComponent<SceneNote>();
        note.message = "Write task here...";
        note.color = new Color(1f, 0.8f, 0.2f);

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(go, "Create Scene Note");
        Selection.activeGameObject = go;
        // If we are in the editor, focus the inspector
        if (!Application.isPlaying) 
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
#endif
        return note;
    }

    // --- EDITOR VISUALS (Scene View) ---
    private void OnDrawGizmos()
    {
        if (!enabled) return;

        Gizmos.color = isResolved ? Color.gray : color;
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);

#if UNITY_EDITOR
        string display = isResolved ? $"[RESOLVED] {message}" : message;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = isResolved ? Color.gray : Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        Handles.Label(transform.position + Vector3.up * 1.7f, display, style);
#endif
    }

    // --- GAME VISUALS (Game View) ---
    private void OnGUI()
    {
        // Only draw in Game View if enabled and object is visible
        if (!enabled || Camera.main == null) return;

        // Calculate Screen Position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.7f);

        // Check if target is behind the camera
        if (screenPos.z < 0) return;

        // Flip Y coordinate (GUI system has 0 at top, ScreenPoint has 0 at bottom)
        screenPos.y = Screen.height - screenPos.y;

        // Determine Style
        Color textColor = isResolved ? Color.gray : Color.white;
        string display = isResolved ? $"[RESOLVED] {message}" : message;

        // Draw Background Box
        Vector2 size = new GUIStyle(GUI.skin.label).CalcSize(new GUIContent(display));
        Rect rect = new Rect(screenPos.x - (size.x / 2), screenPos.y, size.x + 10, size.y + 5);

        GUI.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black background
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Draw Text
        GUIStyle gameStyle = new GUIStyle(GUI.skin.label);
        gameStyle.normal.textColor = textColor;
        gameStyle.alignment = TextAnchor.MiddleCenter;
        gameStyle.fontStyle = FontStyle.Bold;

        GUI.Label(rect, display, gameStyle);
    }

    // Auto-naming in Hierarchy
    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(message))
        {
            string cleanName = message.Split('\n')[0];
            if (cleanName.Length > 20) cleanName = cleanName.Substring(0, 20) + "...";
            name = $"NOTE: {cleanName}";
        }
    }
}