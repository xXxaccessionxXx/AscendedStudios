using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad] // This keyword makes the script run as soon as Unity loads!
public class SelectionHistory : EditorWindow
{
    // Static list ensures history persists even if you close the window
    private static List<GameObject> _history = new List<GameObject>();
    private const int MAX_HISTORY_SIZE = 20;

    private Vector2 _scrollPosition;
    private GUIStyle _buttonStyle;

    // 1. The Constructor: Hooks into the editor update loop immediately
    static SelectionHistory()
    {
        Selection.selectionChanged += RecordSelection;
    }

    [MenuItem("Tools/Ascended Toolkit/Selection History")]
    public static void ShowWindow()
    {
        SelectionHistory window = GetWindow<SelectionHistory>("History");
        window.minSize = new Vector2(250, 300);
        window.Show();
    }

    // 2. The Logic: Runs every time you click something in the Editor
    private static void RecordSelection()
    {
        GameObject currentSelection = Selection.activeGameObject;

        // Ignore if nothing is selected or if we are just clicking the same object
        if (currentSelection == null) return;
        if (_history.Count > 0 && _history[0] == currentSelection) return;

        // Remove duplicates (move them to top)
        if (_history.Contains(currentSelection))
        {
            _history.Remove(currentSelection);
        }

        // Add to top of stack
        _history.Insert(0, currentSelection);

        // Cap the size
        if (_history.Count > MAX_HISTORY_SIZE)
        {
            _history.RemoveAt(_history.Count - 1);
        }

        // Force the window to redraw so the list updates instantly if open
        if (HasOpenInstances<SelectionHistory>())
        {
            GetWindow<SelectionHistory>().Repaint();
        }
    }

    private void OnEnable()
    {
        // Setup a nice button style with left alignment
        _buttonStyle = new GUIStyle(EditorStyles.miniButton);
        _buttonStyle.alignment = TextAnchor.MiddleLeft;
        _buttonStyle.fixedHeight = 24;
        _buttonStyle.margin = new RectOffset(0, 0, 2, 2);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("SELECTION HISTORY", EditorStyles.boldLabel);
        GUILayout.Label("Click to re-select", EditorStyles.centeredGreyMiniLabel);

        GUILayout.Space(10);

        if (_history.Count == 0)
        {
            EditorGUILayout.HelpBox("Select GameObjects to build history.", MessageType.Info);
            return;
        }

        DrawHistoryList();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Clear History"))
        {
            _history.Clear();
        }
        GUILayout.Space(10);
    }

    private void DrawHistoryList()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // Loop backwards safely to handle deletion while iterating
        for (int i = 0; i < _history.Count; i++)
        {
            GameObject obj = _history[i];

            // Handle case where object was deleted from scene
            if (obj == null)
            {
                _history.RemoveAt(i);
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            // 1. Icon + Name Button
            // We get the icon Unity uses for this object (cube, light, camera icon, etc)
            GUIContent content = EditorGUIUtility.ObjectContent(obj, typeof(GameObject));

            // Highlight button if it is currently selected
            GUI.backgroundColor = Selection.activeGameObject == obj ? new Color(0.2f, 0.8f, 0.9f) : Color.white;

            if (GUILayout.Button(content, _buttonStyle))
            {
                Selection.activeGameObject = obj;
                EditorGUIUtility.PingObject(obj); // "Pings" it in the hierarchy (yellow flash)
            }

            GUI.backgroundColor = Color.white;

            // 2. Lock Button (Optional: Keep it in history?) - kept simple for now
            // You could add a 'Favorite' star button here later

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}