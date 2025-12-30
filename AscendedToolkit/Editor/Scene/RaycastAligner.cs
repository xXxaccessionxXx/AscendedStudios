using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RaycastAligner : EditorWindow
{
    // Settings
    private float _yOffset = 0.0f;
    private bool _alignNormal = false; // Match the slope of the ground?

    // Randomization Settings
    private bool _randomizeRotation = true;
    private float _minRot = 0f;
    private float _maxRot = 360f;

    private bool _randomizeScale = false;
    private float _minScale = 0.8f;
    private float _maxScale = 1.2f;

    [MenuItem("Tools/Ascended Toolkit/Raycast Aligner")]
    public static void ShowWindow()
    {
        GetWindow<RaycastAligner>("Aligner");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("RAYCAST ALIGNER", EditorStyles.boldLabel);
        GUILayout.Label("Snap objects to terrain/floors", EditorStyles.centeredGreyMiniLabel);

        GUILayout.Space(15);

        // --- SECTION 1: THE DROP ---
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Drop Settings", EditorStyles.boldLabel);

        _yOffset = EditorGUILayout.FloatField("Height Offset", _yOffset);
        _alignNormal = EditorGUILayout.Toggle("Align to Slope", _alignNormal);

        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.2f); // Green button
        if (GUILayout.Button("DROP TO SURFACE", GUILayout.Height(40)))
        {
            DropSelectedObjects();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // --- SECTION 2: NATURALIZE ---
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Naturalize (Randomize)", EditorStyles.boldLabel);

        // Rotation
        _randomizeRotation = EditorGUILayout.ToggleLeft("Randomize Y Rotation", _randomizeRotation);
        if (_randomizeRotation)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Range:", GUILayout.Width(50));
            _minRot = EditorGUILayout.FloatField(_minRot, GUILayout.Width(50));
            GUILayout.Label("to", GUILayout.Width(20));
            _maxRot = EditorGUILayout.FloatField(_maxRot, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        // Scale
        _randomizeScale = EditorGUILayout.ToggleLeft("Randomize Uniform Scale", _randomizeScale);
        if (_randomizeScale)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Range:", GUILayout.Width(50));
            _minScale = EditorGUILayout.FloatField(_minScale, GUILayout.Width(50));
            GUILayout.Label("to", GUILayout.Width(20));
            _maxScale = EditorGUILayout.FloatField(_maxScale, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Apply Randomization", GUILayout.Height(30)))
        {
            ApplyRandomization();
        }

        EditorGUILayout.EndVertical();
    }

    private void DropSelectedObjects()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0) return;

        Undo.RecordObjects(Selection.transforms, "Drop to Surface");

        int layerMask = Physics.AllLayers;

        foreach (GameObject obj in selection)
        {
            // --- STEP 0: CALCULATE PIVOT OFFSET ---
            // We need to know how far the "bottom" is from the "pivot"
            float distToBottom = 0f;
            Collider[] myColliders = obj.GetComponentsInChildren<Collider>();

            if (myColliders.Length > 0)
            {
                // Create a bounds that encapsulates all child colliders (for complex groups)
                Bounds combinedBounds = myColliders[0].bounds;
                for (int i = 1; i < myColliders.Length; i++)
                {
                    combinedBounds.Encapsulate(myColliders[i].bounds);
                }

                // The distance from the pivot (transform.position.y) to the lowest point (bounds.min.y)
                distToBottom = obj.transform.position.y - combinedBounds.min.y;
            }

            // --- STEP 1: DISABLE COLLIDERS ---
            foreach (var c in myColliders) c.enabled = false;

            Physics.SyncTransforms();

            Vector3 startPos = obj.transform.position + Vector3.up * 500.0f;

            bool foundGround = false;
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.up;

            // --- ATTEMPT 1: PHYSICS RAYCAST ---
            RaycastHit hit;
            if (Physics.Raycast(startPos, Vector3.down, out hit, 2000f, layerMask, QueryTriggerInteraction.Ignore))
            {
                foundGround = true;
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }
            // --- ATTEMPT 2: TERRAIN SAMPLING ---
            else if (Terrain.activeTerrain != null)
            {
                Terrain t = Terrain.activeTerrain;
                float terrainHeight = t.SampleHeight(obj.transform.position) + t.transform.position.y;

                foundGround = true;
                hitPoint = new Vector3(obj.transform.position.x, terrainHeight, obj.transform.position.z);

                if (_alignNormal)
                {
                    float normX = Mathf.InverseLerp(t.transform.position.x, t.transform.position.x + t.terrainData.size.x, hitPoint.x);
                    float normZ = Mathf.InverseLerp(t.transform.position.z, t.transform.position.z + t.terrainData.size.z, hitPoint.z);
                    hitNormal = t.terrainData.GetInterpolatedNormal(normX, normZ);
                }
            }

            // --- APPLY THE MOVE ---
            if (foundGround)
            {
                // CRITICAL CHANGE: Add distToBottom to the target position
                Vector3 targetPos = hitPoint + (Vector3.up * (_yOffset + distToBottom));
                obj.transform.position = targetPos;

                if (_alignNormal)
                {
                    obj.transform.up = hitNormal;
                }
            }
            else
            {
                Debug.LogWarning($"Could not find ground for {obj.name}.");
            }

            // --- RE-ENABLE COLLIDERS ---
            foreach (var c in myColliders) c.enabled = true;
        }
    }


    private void ApplyRandomization()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0) return;

        Undo.RecordObjects(Selection.transforms, "Randomize Transforms");

        foreach (GameObject obj in selection)
        {
            if (_randomizeRotation)
            {
                Vector3 currentRot = obj.transform.localEulerAngles;
                float randomY = Random.Range(_minRot, _maxRot);
                obj.transform.localEulerAngles = new Vector3(currentRot.x, randomY, currentRot.z);
            }

            if (_randomizeScale)
            {
                float randomScale = Random.Range(_minScale, _maxScale);
                obj.transform.localScale = Vector3.one * randomScale;
            }
        }
    }
}