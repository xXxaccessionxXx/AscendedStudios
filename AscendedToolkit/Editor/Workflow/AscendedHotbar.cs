using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class AscendedHotbar : EditorWindow
{
    // STORAGE KEYS (Unique to your project to avoid conflicts)
    private const string PREFS_SCENE_COUNT = "Ascended_Hotbar_SceneCount";
    private const string PREFS_SCENE_PREFIX = "Ascended_Hotbar_Scene_";

    private const string PREFS_ASSET_COUNT = "Ascended_Hotbar_AssetCount";
    private const string PREFS_ASSET_PREFIX = "Ascended_Hotbar_Asset_";

    // RUNTIME LISTS
    private List<string> _favoriteScenePaths = new List<string>();
    private List<Object> _favoriteAssets = new List<Object>();

    private Vector2 _scrollPos;
    private bool _editMode = false; // Toggle to show "Remove" buttons

    [MenuItem("Tools/Ascended Toolkit/Hotbar")]
    public static void ShowWindow()
    {
        GetWindow<AscendedHotbar>("Hotbar");
    }

    private void OnEnable()
    {
        LoadFavorites();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("ASCENDED HOTBAR", EditorStyles.boldLabel);

        // Edit Mode Toggle (Top Right)
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        _editMode = GUILayout.Toggle(_editMode, "Edit Mode", EditorStyles.miniButton);
        GUILayout.EndHorizontal();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // --- SECTION 1: SCENES ---
        GUILayout.Label("Favorite Scenes", EditorStyles.boldLabel);
        DrawSceneSection();

        GUILayout.Space(15);

        // --- SECTION 2: ASSETS ---
        GUILayout.Label("Asset Shelf", EditorStyles.boldLabel);
        DrawAssetSection();

        EditorGUILayout.EndScrollView();

        // Save whenever we change things (GUI changes)
        if (GUI.changed)
        {
            SaveFavorites();
        }
    }

    // ---------------------- SCENE LOGIC ----------------------

    private void DrawSceneSection()
    {
        EditorGUILayout.BeginVertical("box");

        // List existing scenes
        for (int i = 0; i < _favoriteScenePaths.Count; i++)
        {
            string path = _favoriteScenePaths[i];
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            EditorGUILayout.BeginHorizontal();

            // 1. The "GO" Button
            if (GUILayout.Button($"Open {sceneName}", GUILayout.Height(30)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
            }

            // 2. The Remove Button (Only in Edit Mode)
            if (_editMode)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Red
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(30)))
                {
                    _favoriteScenePaths.RemoveAt(i);
                    SaveFavorites();
                    return; // Exit early to avoid index errors
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        // Add Current Scene Button
        GUILayout.Space(5);
        if (GUILayout.Button("+ Add Current Scene", EditorStyles.miniButton))
        {
            string currentPath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(currentPath))
            {
                EditorUtility.DisplayDialog("Error", "You must save the scene before adding it.", "OK");
            }
            else if (!_favoriteScenePaths.Contains(currentPath))
            {
                _favoriteScenePaths.Add(currentPath);
                SaveFavorites();
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ---------------------- ASSET LOGIC ----------------------

    private void DrawAssetSection()
    {
        EditorGUILayout.BeginVertical("box");

        // 1. Drag & Drop Area
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag Assets Here to Bookmark", EditorStyles.centeredGreyMiniLabel);
        HandleDragDrop(dropArea);

        GUILayout.Space(10);

        // 2. The Asset Grid
        // We use a "Wrap" layout to make icons flow nicely
        float windowWidth = EditorGUIUtility.currentViewWidth;
        int columns = Mathf.FloorToInt(windowWidth / 70); // Calc how many icons fit
        if (columns < 1) columns = 1;

        int rowCount = Mathf.CeilToInt((float)_favoriteAssets.Count / columns);

        for (int row = 0; row < rowCount; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                if (index >= _favoriteAssets.Count) break;

                DrawAssetIcon(index);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAssetIcon(int index)
    {
        Object asset = _favoriteAssets[index];
        if (asset == null)
        {
            _favoriteAssets.RemoveAt(index);
            return;
        }

        EditorGUILayout.BeginVertical(GUILayout.Width(60));

        // Draw the Object Field (this allows drag-and-drop OUT of the window)
        // We disable "AllowSceneObjects" (false) to ensure only Project assets are used
        Object newObj = EditorGUILayout.ObjectField(asset, typeof(Object), false, GUILayout.Width(60), GUILayout.Height(60));

        // Remove button (Tiny X in corner)
        if (_editMode)
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                _favoriteAssets.RemoveAt(index);
                SaveFavorites();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndVertical();
    }

    private void HandleDragDrop(Rect dropArea)
    {
        Event currentEvent = Event.current;

        // Check if mouse is hovering over box with dragged items
        if (dropArea.Contains(currentEvent.mousePosition))
        {
            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    // Only add if not already in list
                    if (!_favoriteAssets.Contains(draggedObject))
                    {
                        _favoriteAssets.Add(draggedObject);
                    }
                }
                SaveFavorites();
                currentEvent.Use();
            }
        }
    }

    // ---------------------- PERSISTENCE ----------------------

    private void SaveFavorites()
    {
        // Save Scenes (String Paths)
        EditorPrefs.SetInt(PREFS_SCENE_COUNT, _favoriteScenePaths.Count);
        for (int i = 0; i < _favoriteScenePaths.Count; i++)
        {
            EditorPrefs.SetString(PREFS_SCENE_PREFIX + i, _favoriteScenePaths[i]);
        }

        // Save Assets (GUIDs - This is the robust way!)
        EditorPrefs.SetInt(PREFS_ASSET_COUNT, _favoriteAssets.Count);
        for (int i = 0; i < _favoriteAssets.Count; i++)
        {
            if (_favoriteAssets[i] != null)
            {
                string path = AssetDatabase.GetAssetPath(_favoriteAssets[i]);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(PREFS_ASSET_PREFIX + i, guid);
            }
        }
    }

    private void LoadFavorites()
    {
        _favoriteScenePaths.Clear();
        _favoriteAssets.Clear();

        // Load Scenes
        int sceneCount = EditorPrefs.GetInt(PREFS_SCENE_COUNT, 0);
        for (int i = 0; i < sceneCount; i++)
        {
            string path = EditorPrefs.GetString(PREFS_SCENE_PREFIX + i, "");
            if (!string.IsNullOrEmpty(path)) _favoriteScenePaths.Add(path);
        }

        // Load Assets
        int assetCount = EditorPrefs.GetInt(PREFS_ASSET_COUNT, 0);
        for (int i = 0; i < assetCount; i++)
        {
            string guid = EditorPrefs.GetString(PREFS_ASSET_PREFIX + i, "");
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null) _favoriteAssets.Add(asset);
            }
        }
    }
}