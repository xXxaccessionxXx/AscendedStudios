using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class QuickRenamer : EditorWindow
{
    // State variables
    private string _searchPattern = "";
    private string _replacePattern = "";
    private bool _useRegex = false;
    private Vector2 _scrollPosition;

    // Styling
    private GUIStyle _previewStyleOld;
    private GUIStyle _previewStyleNew;
    private GUIStyle _headerStyle;

    [MenuItem("Tools/Ascended Toolkit/Quick Renamer")]
    public static void ShowWindow()
    {
        QuickRenamer window = GetWindow<QuickRenamer>("Renamer");
        window.minSize = new Vector2(350, 400);
        window.Show();
    }

    private void OnEnable()
    {
        // Setup styles for the list preview
        _headerStyle = new GUIStyle();
        _headerStyle.fontSize = 18;
        _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.normal.textColor = new Color(0.2f, 0.8f, 0.9f); // Ascended Cyan
        _headerStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void OnGUI()
    {
        // Lazy init styles if they are null (keeps them robust across reloads)
        if (_previewStyleOld == null)
        {
            _previewStyleOld = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            _previewStyleNew = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
            _previewStyleNew.normal.textColor = new Color(0.2f, 0.8f, 0.9f); // Cyan highlight
        }

        GUILayout.Space(10);
        GUILayout.Label("QUICK RENAMER", _headerStyle);
        GUILayout.Space(10);

        DrawInputSection();
        DrawPreviewSection();
        DrawFooter();
    }

    private void DrawInputSection()
    {
        EditorGUILayout.BeginVertical("box");

        // Mode Selection
        _useRegex = EditorGUILayout.ToggleLeft("Use Regular Expressions (Regex)", _useRegex);

        GUILayout.Space(5);

        // Input Fields
        string searchLabel = _useRegex ? "Regex Pattern" : "Find Text";
        _searchPattern = EditorGUILayout.TextField(searchLabel, _searchPattern);
        _replacePattern = EditorGUILayout.TextField("Replace With", _replacePattern);

        EditorGUILayout.HelpBox($"Selected Objects: {Selection.gameObjects.Length}", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewSection()
    {
        GUILayout.Space(10);
        GUILayout.Label("Preview", EditorStyles.boldLabel);

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            GUILayout.Label("Select GameObjects in the scene to rename.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        // Scrollable list
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");

        foreach (GameObject obj in selectedObjects)
        {
            string oldName = obj.name;
            string newName = GenerateNewName(oldName);

            EditorGUILayout.BeginHorizontal();

            // Old Name
            GUILayout.Label(oldName, _previewStyleOld, GUILayout.Width(position.width / 2 - 30));

            // Arrow icon
            GUILayout.Label("?", EditorStyles.miniLabel, GUILayout.Width(20));

            // New Name (Highlight if changed)
            if (oldName != newName)
            {
                GUILayout.Label(newName, _previewStyleNew);
            }
            else
            {
                GUILayout.Label("(no change)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFooter()
    {
        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.9f);
        if (GUILayout.Button("APPLY RENAME", GUILayout.Height(40)))
        {
            ApplyRename();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
    }

    private string GenerateNewName(string original)
    {
        if (string.IsNullOrEmpty(_searchPattern)) return original;

        try
        {
            if (_useRegex)
            {
                // Regex Replace
                return Regex.Replace(original, _searchPattern, _replacePattern);
            }
            else
            {
                // Simple String Replace
                return original.Replace(_searchPattern, _replacePattern);
            }
        }
        catch
        {
            return "Invalid Regex"; // Safety catch for bad regex patterns
        }
    }

    private void ApplyRename()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        // "Undo" Registration - CRITICAL for editor tools
        // This groups all operations into a single Undo step named "Batch Rename"
        Undo.RecordObjects(selectedObjects, "Batch Rename");

        foreach (GameObject obj in selectedObjects)
        {
            obj.name = GenerateNewName(obj.name);
        }

        // Force the editor to repaint so the hierarchy updates immediately
        EditorApplication.RepaintProjectWindow();
    }
}