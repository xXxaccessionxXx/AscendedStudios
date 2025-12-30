using UnityEngine;
using UnityEditor;
using System.IO;

public class SnapshotStudio : EditorWindow
{
    // Settings
    private GameObject _targetObject;
    private int _resolution = 512;
    private string _folderPath = "Assets/UI/Icons";

    // Preview
    private Texture2D _previewTexture;
    private Vector3 _previewDirection = new Vector3(1, -0.5f, 1); // Angle of the shot
    private float _distanceMultiplier = 1.2f; // Zoom level

    [MenuItem("Tools/Ascended Toolkit/Snapshot Studio")]
    public static void ShowWindow()
    {
        GetWindow<SnapshotStudio>("Studio");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("SNAPSHOT STUDIO", EditorStyles.boldLabel);
        GUILayout.Label("Turn 3D Models into UI Sprites", EditorStyles.centeredGreyMiniLabel);

        GUILayout.Space(15);

        // --- INPUTS ---
        EditorGUILayout.BeginVertical("box");

        // 1. START LISTENING FOR CHANGES
        EditorGUI.BeginChangeCheck();

        _targetObject = (GameObject)EditorGUILayout.ObjectField("Subject", _targetObject, typeof(GameObject), true);

        GUILayout.BeginHorizontal();
        _resolution = EditorGUILayout.IntField("Resolution", _resolution);
        GUILayout.Label("px", GUILayout.Width(20));
        GUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _folderPath = EditorGUILayout.TextField("Save Path", _folderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Choose Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    _folderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        _previewDirection = EditorGUILayout.Vector3Field("Camera Angle", _previewDirection);
        _distanceMultiplier = EditorGUILayout.Slider("Zoom (Distance)", _distanceMultiplier, 0.5f, 3.0f);

        // 2. CHECK IF ANYTHING CHANGED
        if (EditorGUI.EndChangeCheck())
        {
            // Only auto-update if we actually have an object (prevents error spam)
            if (_targetObject != null)
            {
                CaptureSnapshot(true);
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // --- ACTION BUTTONS ---
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview Shot", GUILayout.Height(30)))
        {
            CaptureSnapshot(true);
        }

        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.5f);
        if (GUILayout.Button("SAVE PNG", GUILayout.Height(30)))
        {
            CaptureSnapshot(false);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // --- PREVIEW AREA ---
        if (_previewTexture != null)
        {
            GUILayout.Space(15);
            GUILayout.Label("Preview:", EditorStyles.boldLabel);

            float aspect = (float)_previewTexture.width / _previewTexture.height;
            float displayWidth = Mathf.Min(position.width - 40, 256);
            float displayHeight = displayWidth / aspect;

            Rect displayRect = GUILayoutUtility.GetRect(displayWidth, displayHeight);

            EditorGUI.DrawTextureTransparent(displayRect, _previewTexture, ScaleMode.ScaleToFit);
        }
    }

    private void CaptureSnapshot(bool isPreview)
    {
        if (_targetObject == null)
        {
            // Only show the intrusive dialog if the user CLICKED the button manually
            // (We skip this error during auto-live-update to be smoother)
            if (!isPreview)
                EditorUtility.DisplayDialog("Error", "Please assign a Subject GameObject.", "OK");
            return;
        }

        // 1. SETUP
        GameObject studioGo = new GameObject("Temp_Studio_Rig");
        Camera cam = studioGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = true;

        Light light = studioGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;

        // 2. POSITIONING
        Bounds bounds = new Bounds(_targetObject.transform.position, Vector3.zero);
        foreach (Renderer r in _targetObject.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(r.bounds);
        }

        Vector3 direction = _previewDirection.normalized;
        float magnitude = bounds.extents.magnitude * _distanceMultiplier;

        cam.transform.position = bounds.center - (direction * magnitude * 5f);
        cam.transform.LookAt(bounds.center);
        cam.orthographicSize = magnitude;

        // 3. RENDER
        RenderTexture rt = RenderTexture.GetTemporary(_resolution, _resolution, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        // 4. READ PIXELS
        RenderTexture.active = rt;
        Texture2D resultTex = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false);
        resultTex.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
        resultTex.Apply();

        // 5. CLEANUP
        cam.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        DestroyImmediate(studioGo);

        // 6. OUTPUT
        if (isPreview)
        {
            _previewTexture = resultTex;
        }
        else
        {
            SaveTextureToFile(resultTex, _targetObject.name);
            _previewTexture = resultTex;
        }
    }

    private void SaveTextureToFile(Texture2D tex, string objectName)
    {
        byte[] bytes = tex.EncodeToPNG();

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        string fileName = $"{objectName}_Icon.png";
        string fullPath = Path.Combine(_folderPath, fileName);

        File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.Refresh();

        Object newAsset = AssetDatabase.LoadAssetAtPath<Object>(Path.Combine(_folderPath, fileName));
        EditorGUIUtility.PingObject(newAsset);

        Debug.Log($"Snapshot saved to: {fullPath}");
    }
}