using UnityEngine;
using UnityEditor;

public class AscendedDashboard : EditorWindow
{
    private GUIStyle _headerStyle;
    private Texture2D _brandingLogo;

    // Tab State
    private int _selectedTab = 0;
    private readonly string[] _tabNames = { "Scene", "Workflow", "Analysis" };

    [MenuItem("Ascended/Dashboard &a", false, 0)]
    public static void ShowWindow()
    {
        AscendedDashboard window = GetWindow<AscendedDashboard>("Ascended Hub");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnEnable()
    {
        // Setup styles
        _headerStyle = new GUIStyle();
        _headerStyle.fontSize = 24;
        _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.normal.textColor = new Color(0.2f, 0.8f, 0.9f); // Cyan Ascended color
        _headerStyle.alignment = TextAnchor.MiddleCenter;

        // --- LOAD LOGO ---
        string[] guids = AssetDatabase.FindAssets("AscendedStudios t:Texture");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _brandingLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            // Set the small icon on the Window Tab itself
            if (_brandingLogo != null)
            {
                titleContent.image = _brandingLogo;
            }
        }
    }

    private void OnGUI()
    {
        // --- BRANDING HEADER (FULL WIDTH BANNER) ---
        if (_brandingLogo != null)
        {
            float maxBannerHeight = 80f; // Maximum height for the sleek look

            // Calculate scale
            float aspectRatio = (float)_brandingLogo.width / _brandingLogo.height;
            float headerHeight = position.width / aspectRatio;

            // Cap the height
            headerHeight = Mathf.Min(headerHeight, maxBannerHeight);

            // FIX: Draw manually at (0,0) to ignore window margins and fill the full width
            Rect logoRect = new Rect(0, 0, position.width, headerHeight);

            // FIX: Use ScaleAndCrop to ensure it always fills the width, even if cropped vertically
            GUI.DrawTexture(logoRect, _brandingLogo, ScaleMode.ScaleAndCrop);

            // Push the rest of the UI down by the banner height
            GUILayout.Space(headerHeight + 10);
        }
        else
        {
            GUILayout.Space(15);
            GUILayout.Label("ASCENDED TOOLKIT", _headerStyle);
        }

        GUILayout.Label("Elevate your workflow", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(15);

        // --- TABS ---
        _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(30));

        GUILayout.Space(15);

        // --- CONTENT AREA ---
        EditorGUILayout.BeginVertical();

        switch (_selectedTab)
        {
            case 0: // SCENE TAB
                DrawSceneTab();
                break;
            case 1: // WORKFLOW TAB
                DrawWorkflowTab();
                break;
            case 2: // ANALYSIS TAB
                DrawAnalysisTab();
                break;
        }

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        DrawHorizontalLine(Color.gray);
        GUILayout.Label("Ascended Toolkit v1.1.0", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(10);
    }

    // ---------------- TAB CONTENTS ----------------

    private void DrawSceneTab()
    {
        GUILayout.Label("Hierarchy & Object Management", EditorStyles.boldLabel);

        DrawToolCard("Smart Grouper", "GameObject Icon", "Organize hierarchy, auto-parent, and color code groups.", () =>
        {
            SmartGrouper.ShowWindow();
        });

        DrawToolCard("Raycast Aligner", "TerrainCollider Icon", "Snap objects to terrain/surface and randomize rotation.", () =>
        {
            RaycastAligner.ShowWindow();
        });

        DrawToolCard("Quick Renamer", "InputField Icon", "Batch rename objects using Regex or search/replace.", () =>
        {
            QuickRenamer.ShowWindow();
        });

        DrawToolCard("Pivot Doctor", "RectTransform Icon", "Move object pivot points by wrapping them in a parent.", () =>
        {
            PivotDoctor.ShowWindow();
        });
    }

    private void DrawWorkflowTab()
    {
        GUILayout.Label("Navigation & Efficiency", EditorStyles.boldLabel);

        DrawToolCard("Scene & Asset Hotbar", "Favorite Icon", "Favorite scenes and assets for instant access.", () =>
        {
            AscendedHotbar.ShowWindow();
        });

        DrawToolCard("Selection History", "VerticalLayoutGroup Icon", "Quickly re-select objects you were working on previously.", () =>
        {
            SelectionHistory.ShowWindow();
        });

        DrawToolCard("Snapshot Studio", "Camera Icon", "Generate transparent UI icons from 3D models.", () =>
        {
            SnapshotStudio.ShowWindow();
        }, true);

        DrawToolCard("Scene Memo", "TextAsset Icon", "Place sticky notes in 3D space. (Ctrl + Right Click)", () =>
        {
            SceneMemoBoard.ShowWindow();
        }, true);
    }

    private void DrawAnalysisTab()
    {
        GUILayout.Label("Debugging & Repair", EditorStyles.boldLabel);

        DrawToolCard("Ascended Debugger", "AssemblyDefinitionAsset Icon", "Analyze Console errors, explain warnings, and preview code.", () =>
        {
            AscendedDebugger.ShowWindow();
        });

        DrawToolCard("Save State Manager", "ScriptableObject Icon", "View and edit PlayerPrefs (Coins, Score) in real-time.", () =>
        {
            AscendedSaveManager.ShowWindow();
        });

        DrawToolCard("Missing Script Fixer", "cs Script Icon", "Find and remove broken script references instantly.", () =>
        {
            // MissingScriptFixer.ShowWindow(); 
            Debug.Log("Missing Script Fixer module not found.");
        });
    }

    // ---------------- HELPERS ----------------

    private void DrawToolCard(string title, string iconName, string description, System.Action onClick, bool isEnabled = true)
    {
        EditorGUILayout.BeginHorizontal("box");

        // --- ICON COLUMN ---
        Color iconColor = Color.white;
        if (_selectedTab == 0) iconColor = new Color(0.2f, 0.9f, 0.5f); // Green for Scene
        if (_selectedTab == 1) iconColor = new Color(0.2f, 0.8f, 0.9f); // Cyan for Workflow
        if (_selectedTab == 2) iconColor = new Color(1f, 0.5f, 0.4f);   // Orange for Analysis

        if (!isEnabled) iconColor = Color.gray;

        GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
        Texture image = iconContent != null ? iconContent.image : null;

        GUI.color = iconColor;
        if (image != null)
        {
            if (GUILayout.Button(image, GUIStyle.none, GUILayout.Width(40), GUILayout.Height(40)))
            {
                if (isEnabled) onClick?.Invoke();
            }
        }
        else
        {
            GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
        }
        GUI.color = Color.white;

        // --- TEXT COLUMN ---
        EditorGUILayout.BeginVertical();
        GUILayout.Space(2);
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Label(description, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        // --- BUTTON COLUMN ---
        GUILayout.Space(5);
        if (isEnabled)
        {
            if (GUILayout.Button("Open", GUILayout.Height(40), GUILayout.Width(60)))
            {
                onClick?.Invoke();
            }
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("Soon", GUILayout.Height(40), GUILayout.Width(60));
            GUI.enabled = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawHorizontalLine(Color color)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
    }
}