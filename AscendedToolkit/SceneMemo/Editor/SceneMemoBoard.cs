using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// --- PART 1: GLOBAL LISTENER (Runs Always) ---
[InitializeOnLoad]
public static class SceneMemoGlobal
{
    private const string CONTAINER_NAME = "___SCENE_MEMOS___";

    static SceneMemoGlobal()
    {
        SceneView.duringSceneGui += OnGlobalSceneGUI;
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyItem;
    }

    private static void OnGlobalSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Editor Scene View Shortcut
        if (e.type == EventType.MouseDown && e.button == 1 && e.control)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            Vector3 spawnPos = Physics.Raycast(ray, out hit) ? hit.point : (ray.origin + ray.direction * 10f);

            CreateNoteGlobal(spawnPos);
            e.Use();
        }
    }

    // Handles Hierarchy Coloring
    private static void HandleHierarchyItem(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        SceneNote note = obj.GetComponent<SceneNote>();
        if (note != null)
        {
            Color baseColor = note.isResolved ? Color.gray : note.color;

            Rect bgRect = new Rect(selectionRect);
            bgRect.x += 18; bgRect.width -= 18;
            Color bgColor = baseColor; bgColor.a = 0.15f;
            EditorGUI.DrawRect(bgRect, bgColor);

            Rect markerRect = new Rect(selectionRect);
            markerRect.x = selectionRect.xMax - 4; markerRect.width = 4;
            Color markerColor = baseColor; markerColor.a = 0.8f;
            EditorGUI.DrawRect(markerRect, markerColor);
        }
    }

    public static void CreateNoteGlobal(Vector3 position)
    {
        // 1. Find Container
        GameObject container = GameObject.Find(CONTAINER_NAME);
        if (container == null)
        {
            container = new GameObject(CONTAINER_NAME);
            container.transform.SetSiblingIndex(0);
            Undo.RegisterCreatedObjectUndo(container, "Create Memo Container");
        }

        // 2. NEW: Ensure Runtime Input script is attached
        if (container.GetComponent<SceneMemoRuntime>() == null)
        {
            container.AddComponent<SceneMemoRuntime>();
        }

        // 3. Create Note (Using the shared logic in SceneNote)
        SceneNote.CreateNote(position, container.transform);

        // 4. Refresh Dashboard if open
        if (EditorWindow.HasOpenInstances<SceneMemoBoard>())
        {
            SceneMemoBoard window = EditorWindow.GetWindow<SceneMemoBoard>();
            if (window != null) window.RefreshNoteList();
        }
    }
}

// --- PART 2: DASHBOARD WINDOW ---
public class SceneMemoBoard : EditorWindow
{
    private Vector2 _scrollPos;
    private bool _placementMode = false;
    private SceneNote[] _allNotes;

    [MenuItem("Tools/Ascended Toolkit/Scene Memo")]
    public static void ShowWindow()
    {
        GetWindow<SceneMemoBoard>("Memo Board");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        RefreshNoteList();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("SCENE MEMO BOARD", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal("box");

        string btnText = _placementMode ? "Creating Note... (Click in Scene)" : "+ Create Note (Manual)";
        GUI.backgroundColor = _placementMode ? new Color(0.5f, 1f, 0.5f) : Color.white;
        if (GUILayout.Button(btnText, GUILayout.Height(30)))
        {
            _placementMode = !_placementMode;
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Refresh List", GUILayout.Height(30), GUILayout.Width(100)))
        {
            RefreshNoteList();
        }
        EditorGUILayout.EndHorizontal();

        if (!_placementMode)
        {
            GUILayout.Label("Tip: Ctrl + Right Click in scene to add note instantly.", EditorStyles.centeredGreyMiniLabel);
        }

        GUILayout.Space(10);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        if (_allNotes == null || _allNotes.Length == 0)
        {
            GUILayout.Label("No notes in scene. Go make some!", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            foreach (var note in _allNotes)
            {
                if (note == null) continue;
                DrawNoteCard(note);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawNoteCard(SceneNote note)
    {
        EditorGUILayout.BeginHorizontal("box");

        GUI.backgroundColor = note.isResolved ? Color.gray : note.color;
        GUILayout.Box("", GUILayout.Width(10), GUILayout.Height(40));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginVertical();
        string title = string.IsNullOrEmpty(note.message) ? "Empty Note" : note.message;
        title = title.Split('\n')[0];
        if (title.Length > 30) title = title.Substring(0, 30) + "...";

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
        statusStyle.normal.textColor = note.isResolved ? Color.green : Color.yellow;
        GUILayout.Label(note.isResolved ? "RESOLVED" : "ACTIVE TASK", statusStyle);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Find", GUILayout.Height(40), GUILayout.Width(50)))
        {
            Selection.activeGameObject = note.gameObject;
            SceneView.lastActiveSceneView.FrameSelected();
            EditorGUIUtility.PingObject(note.gameObject);
        }

        string toggleIcon = note.isResolved ? "R" : "?";
        if (GUILayout.Button(toggleIcon, GUILayout.Height(40), GUILayout.Width(30)))
        {
            Undo.RecordObject(note, "Toggle Note Status");
            note.isResolved = !note.isResolved;
            RefreshNoteList();
            EditorApplication.RepaintProjectWindow();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_placementMode)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;
                Vector3 spawnPos = Physics.Raycast(ray, out hit) ? hit.point : (ray.origin + ray.direction * 10f);

                SceneMemoGlobal.CreateNoteGlobal(spawnPos);

                e.Use();
                _placementMode = false;
                Repaint();
            }
        }
    }

    public void RefreshNoteList()
    {
        _allNotes = FindObjectsByType<SceneNote>(FindObjectsSortMode.None);
        System.Array.Sort(_allNotes, (a, b) => a.isResolved.CompareTo(b.isResolved));
        Repaint();
    }

    private void OnHierarchyChange()
    {
        RefreshNoteList();
    }
}