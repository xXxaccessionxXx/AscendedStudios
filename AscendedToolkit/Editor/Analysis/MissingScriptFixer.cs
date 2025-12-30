using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MissingScriptFixer : EditorWindow
{
    private List<GameObject> _affectedObjects = new List<GameObject>();
    private Vector2 _scrollPos;
    private bool _hasScanned = false;

    [MenuItem("Tools/Ascended Toolkit/Missing Script Fixer")]
    public static void ShowWindow()
    {
        MissingScriptFixer window = GetWindow<MissingScriptFixer>("Script Fixer");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("MISSING SCRIPT CLEANER", EditorStyles.boldLabel);
        GUILayout.Label("Scan scene for broken components", EditorStyles.miniLabel);
        GUILayout.Space(10);

        // --- SCAN BUTTON ---
        if (GUILayout.Button("Scan Active Scene", GUILayout.Height(40)))
        {
            ScanScene();
        }

        GUILayout.Space(10);

        // --- RESULTS AREA ---
        if (_hasScanned)
        {
            if (_affectedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No missing scripts found! Your scene is clean.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Found {_affectedObjects.Count} objects with missing scripts.", MessageType.Warning);

                // Action Buttons
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Red tint
                if (GUILayout.Button($"Clean All ({_affectedObjects.Count})", GUILayout.Height(30)))
                {
                    CleanAll();
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(10);
                GUILayout.Label("Affected Objects:", EditorStyles.boldLabel);

                // Scrollable List
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box");
                foreach (GameObject go in _affectedObjects)
                {
                    if (go == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    // Click to ping object in hierarchy
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(go);
                        Selection.activeGameObject = go;
                    }

                    GUILayout.Label(go.name);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void ScanScene()
    {
        _affectedObjects.Clear();
        // Find all GameObjects in scene (including inactive ones)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => !EditorUtility.IsPersistent(go) &&    // Exclude prefabs on disk
                         go.hideFlags == HideFlags.None)       // Exclude internal Unity objects
            .ToArray();

        foreach (GameObject go in allObjects)
        {
            // Unity has a built-in utility for this!
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                _affectedObjects.Add(go);
            }
        }
        _hasScanned = true;
    }

    private void CleanAll()
    {
        int fixedCount = 0;
        int objectCount = 0;

        // Register a massive undo so we can revert if something goes wrong
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clean Missing Scripts");
        var undoGroupIndex = Undo.GetCurrentGroup();

        foreach (GameObject go in _affectedObjects)
        {
            if (go != null)
            {
                // This removes the specific null components
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed > 0)
                {
                    fixedCount += removed;
                    objectCount++;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroupIndex);

        // Refresh scan
        ScanScene();

        EditorUtility.DisplayDialog("Cleanup Complete",
            $"Removed {fixedCount} broken components from {objectCount} objects.", "Ok");
    }
}