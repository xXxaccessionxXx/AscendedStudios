using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;

[InitializeOnLoad]
public class SmartGrouper : EditorWindow
{
    private string _groupName = "New Group";
    private Color _userColor = new Color(0.2f, 0.8f, 0.4f, 0.15f); // Default soft green
    private bool _recursivePhysics = true;

    // --- INITIALIZATION ---
    static SmartGrouper()
    {
        // Remove listener first to ensure we don't duplicate it on recompile
        EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    [MenuItem("Tools/Smart Group %g")]
    public static void ShowWindow()
    {
        SmartGrouper window = (SmartGrouper)EditorWindow.GetWindow(typeof(SmartGrouper));
        window.titleContent = new GUIContent("Smart Group");
        window.minSize = new Vector2(280, 400);
        window.Show();
    }

    // --- HIERARCHY PAINTING ---
    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // Check for the component gracefully
        GroupColor colorData = obj.GetComponent<GroupColor>();
        if (colorData != null)
        {
            Rect bgRect = new Rect(selectionRect);
            bgRect.x += 18;
            bgRect.width -= 18;
            EditorGUI.DrawRect(bgRect, colorData.displayColor);
        }
    }

    // --- EDITOR GUI ---
    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("1. Configuration", EditorStyles.boldLabel);
        _groupName = EditorGUILayout.TextField("Group Name:", _groupName);
        _userColor = EditorGUILayout.ColorField("Group Label Color:", _userColor);

        GUILayout.Space(10);
        GUILayout.Label("2. Group Type", EditorStyles.boldLabel);

        if (GUILayout.Button("Standard Group\n(Empty Parent)", GUILayout.Height(35)))
        {
            CreateGroup(GroupType.Standard);
            Close();
        }

        if (GUILayout.Button("Layout Group\n(UI Grid)", GUILayout.Height(35)))
        {
            CreateGroup(GroupType.Layout);
            Close();
        }

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Physics Options", EditorStyles.miniLabel);
        _recursivePhysics = EditorGUILayout.Toggle("Recursive Colliders", _recursivePhysics);
        if (GUILayout.Button("Physics Group\n(Rigidbody + Colliders)", GUILayout.Height(35)))
        {
            CreateGroup(GroupType.Physics);
            Close();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(20);
        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Ungroup Selected", GUILayout.Height(30)))
        {
            UngroupSelected();
            Close();
        }
        GUI.backgroundColor = Color.white;
    }

    enum GroupType { Standard, Layout, Physics }

    void CreateGroup(GroupType type)
    {
        // FIX 1: Only get objects that are in the Scene and Editable. 
        // This ignores Prefabs in the Project window which cause crashes.
        Transform[] selectedTransforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable | SelectionMode.ExcludePrefab);

        // defensive check
        if (selectedTransforms == null || selectedTransforms.Length == 0)
        {
            Debug.LogWarning("[SmartGrouper] No valid scene objects selected.");
            return;
        }

        // Center Calculation
        Vector3 centerPos = Vector3.zero;
        foreach (Transform t in selectedTransforms) centerPos += t.position;
        centerPos /= selectedTransforms.Length;

        // Create Group
        string finalName = string.IsNullOrEmpty(_groupName) ? type.ToString() + " Group" : _groupName;
        GameObject newGroup = new GameObject(finalName);

        // Critical for undo functionality
        Undo.RegisterCreatedObjectUndo(newGroup, "Create Smart Group");

        // FIX 2: Safely add GroupColor component
        GroupColor colorComp = newGroup.AddComponent<GroupColor>();
        if (colorComp != null)
        {
            colorComp.displayColor = _userColor;
        }

        // Logic for different Group Types
        switch (type)
        {
            case GroupType.Layout:
                if (newGroup.GetComponent<RectTransform>() == null) newGroup.AddComponent<RectTransform>();
                newGroup.AddComponent<GridLayoutGroup>();
                break;

            case GroupType.Physics:
                Rigidbody rb = newGroup.AddComponent<Rigidbody>();
                rb.mass = 1f;
                break;
        }

        // Positioning
        newGroup.transform.position = centerPos;

        // Parent Handling
        Transform commonParent = selectedTransforms[0].parent;
        if (commonParent != null)
        {
            newGroup.transform.SetParent(commonParent);
        }

        // Move Children
        foreach (Transform t in selectedTransforms)
        {
            Undo.SetTransformParent(t, newGroup.transform, "Group Parent Change");

            if (type == GroupType.Physics)
            {
                if (_recursivePhysics)
                {
                    Collider[] allColliders = t.GetComponentsInChildren<Collider>(true);
                    Renderer[] allRenderers = t.GetComponentsInChildren<Renderer>(true);

                    foreach (Renderer r in allRenderers)
                    {
                        if (r.gameObject.GetComponent<Collider>() == null)
                        {
                            Undo.AddComponent<BoxCollider>(r.gameObject);
                        }
                    }
                }
                else
                {
                    if (t.GetComponent<Collider>() == null)
                    {
                        Undo.AddComponent<BoxCollider>(t.gameObject);
                    }
                }
            }
        }

        // Select the new group
        Selection.activeGameObject = newGroup;

        // Reset local position for UI layout groups
        if (type == GroupType.Layout && commonParent != null)
        {
            newGroup.transform.localPosition = Vector3.zero;
        }
    }

    void UngroupSelected()
    {
        // FIX 3: Ensure we only ungroup valid scene objects
        GameObject[] selectedGroups = Selection.gameObjects;
        if (selectedGroups.Length == 0) return;

        foreach (GameObject group in selectedGroups)
        {
            // FIX APPLIED HERE: Added () to IsValid
            if (!group.scene.IsValid()) continue;

            Transform parentTransform = group.transform.parent;

            // Loop backwards when modifying collections or just use while
            while (group.transform.childCount > 0)
            {
                Transform child = group.transform.GetChild(0);
                Undo.SetTransformParent(child, parentTransform, "Ungroup Layer");
            }
            Undo.DestroyObjectImmediate(group);
        }
    }
}