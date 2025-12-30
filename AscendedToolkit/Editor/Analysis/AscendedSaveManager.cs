using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AscendedSaveManager : EditorWindow
{
    // ---------------- TYPES ----------------
    private enum PrefType { String, Int, Float }

    [System.Serializable]
    private class TrackedKey
    {
        public string keyName;
        public PrefType type;

        public TrackedKey(string key, PrefType type)
        {
            this.keyName = key;
            this.type = type;
        }
    }

    // ---------------- DATA ----------------
    private static List<TrackedKey> _trackedKeys = new List<TrackedKey>();
    private string _inputKey = "";
    private PrefType _inputType = PrefType.Int;
    private Vector2 _scrollPos;

    // Persists the watchlist itself so you don't lose it when closing the window
    private const string PREFS_WATCHLIST = "Ascended_SaveManager_Watchlist";

    [MenuItem("Tools/Ascended Toolkit/Save State Manager")]
    public static void ShowWindow()
    {
        GetWindow<AscendedSaveManager>("Save Manager");
    }

    private void OnEnable()
    {
        LoadWatchlist();
    }

    private void OnDisable()
    {
        SaveWatchlist();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("SAVE STATE MANAGER", EditorStyles.boldLabel);

        // --- DANGER ZONE ---
        DrawDangerZone();

        GUILayout.Space(15);

        // --- ADD NEW KEY ---
        DrawAddSection();

        GUILayout.Space(15);

        // --- WATCHLIST ---
        GUILayout.Label("Monitored Keys", EditorStyles.boldLabel);
        DrawWatchlist();
    }

    private void DrawDangerZone()
    {
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Red

        if (GUILayout.Button("NUKE ALL SAVE DATA (DeleteAll)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Delete All Data?",
                "Are you sure you want to delete ALL PlayerPrefs? This cannot be undone.", "Yes, Nuke It", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.LogWarning("All PlayerPrefs have been deleted.");
                Repaint();
            }
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    private void DrawAddSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Add Key to Monitor", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        _inputKey = EditorGUILayout.TextField(_inputKey);
        _inputType = (PrefType)EditorGUILayout.EnumPopup(_inputType, GUILayout.Width(70));

        if (GUILayout.Button("Add", GUILayout.Width(50)))
        {
            if (!string.IsNullOrEmpty(_inputKey))
            {
                // Avoid duplicates
                if (!_trackedKeys.Exists(x => x.keyName == _inputKey))
                {
                    _trackedKeys.Add(new TrackedKey(_inputKey, _inputType));
                    SaveWatchlist();
                    _inputKey = ""; // Clear input
                    GUI.FocusControl(null); // Unfocus
                }
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawWatchlist()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box");

        if (_trackedKeys.Count == 0)
        {
            GUILayout.Label("No keys monitored. Add one above!", EditorStyles.centeredGreyMiniLabel);
        }

        int indexToRemove = -1; // 1. Create a variable to track what to delete

        for (int i = 0; i < _trackedKeys.Count; i++)
        {
            TrackedKey item = _trackedKeys[i];

            EditorGUILayout.BeginHorizontal();

            // 1. Status Icon
            bool exists = PlayerPrefs.HasKey(item.keyName);
            Color statusColor = exists ? Color.green : Color.gray;
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(exists ? "?" : "?", GUILayout.Width(15));
            GUI.color = originalColor;

            // 2. Key Name
            GUILayout.Label(item.keyName, GUILayout.Width(120));

            // 3. Value Editor
            DrawValueEditor(item);

            // 4. Delete Button
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                indexToRemove = i; // 2. Just mark the index, don't return yet!
            }

            EditorGUILayout.EndHorizontal();
        }

        // 3. Perform the deletion safely OUTSIDE the loop and layout groups
        if (indexToRemove != -1)
        {
            _trackedKeys.RemoveAt(indexToRemove);
            SaveWatchlist();
            Repaint();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawValueEditor(TrackedKey item)
    {
        switch (item.type)
        {
            case PrefType.String:
                string currentStr = PlayerPrefs.GetString(item.keyName, "");
                string newStr = EditorGUILayout.DelayedTextField(currentStr);
                if (newStr != currentStr)
                {
                    PlayerPrefs.SetString(item.keyName, newStr);
                    PlayerPrefs.Save();
                }
                break;

            case PrefType.Int:
                int currentInt = PlayerPrefs.GetInt(item.keyName, 0);
                int newInt = EditorGUILayout.DelayedIntField(currentInt);
                if (newInt != currentInt)
                {
                    PlayerPrefs.SetInt(item.keyName, newInt);
                    PlayerPrefs.Save();
                }
                break;

            case PrefType.Float:
                float currentFloat = PlayerPrefs.GetFloat(item.keyName, 0f);
                float newFloat = EditorGUILayout.DelayedFloatField(currentFloat);
                if (Mathf.Abs(newFloat - currentFloat) > 0.001f)
                {
                    PlayerPrefs.SetFloat(item.keyName, newFloat);
                    PlayerPrefs.Save();
                }
                break;
        }
    }

    // ---------------- PERSISTENCE ----------------
    // We save the LIST of keys to EditorPrefs so the tool remembers what you are tracking

    private void SaveWatchlist()
    {
        string data = "";
        foreach (var item in _trackedKeys)
        {
            data += $"{item.keyName}:{((int)item.type)}|";
        }
        EditorPrefs.SetString(PREFS_WATCHLIST, data);
    }

    private void LoadWatchlist()
    {
        _trackedKeys.Clear();
        string data = EditorPrefs.GetString(PREFS_WATCHLIST, "");
        if (string.IsNullOrEmpty(data)) return;

        string[] entries = data.Split('|');
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry)) continue;
            string[] parts = entry.Split(':');
            if (parts.Length == 2)
            {
                string key = parts[0];
                int typeInt = int.Parse(parts[1]);
                _trackedKeys.Add(new TrackedKey(key, (PrefType)typeInt));
            }
        }
    }
}