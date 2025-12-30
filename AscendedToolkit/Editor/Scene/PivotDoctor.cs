using UnityEngine;
using UnityEditor;

public class PivotDoctor : EditorWindow
{
    private GameObject _targetObject;
    private bool _isEditMode = false;

    // REMOVED: private Vector3 _currentSnapPoint; (Unused)
    // REMOVED: private bool _hasPoint = false; (Unused)

    [MenuItem("Tools/Ascended Toolkit/Pivot Doctor")]
    public static void ShowWindow()
    {
        GetWindow<PivotDoctor>("Pivot Dr.");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("PIVOT DOCTOR", EditorStyles.boldLabel);
        GUILayout.Label("Move pivots without Blender", EditorStyles.centeredGreyMiniLabel);

        GUILayout.Space(15);

        EditorGUILayout.BeginVertical("box");

        if (_targetObject == null && Selection.activeGameObject != null)
        {
            _targetObject = Selection.activeGameObject;
        }

        _targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", _targetObject, typeof(GameObject), true);

        GUILayout.Space(10);

        if (_targetObject == null)
        {
            EditorGUILayout.HelpBox("Select an object to fix.", MessageType.Info);
        }
        else
        {
            string btnText = _isEditMode ? "CANCEL PIVOT EDIT" : "PICK NEW PIVOT POINT";
            GUI.backgroundColor = _isEditMode ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 0.9f, 0.5f);

            if (GUILayout.Button(btnText, GUILayout.Height(40)))
            {
                _isEditMode = !_isEditMode;
                // REMOVED: _hasPoint = false; (This was the line causing the warning)
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if (_isEditMode)
            {
                EditorGUILayout.HelpBox("Hover over the object in Scene View.\nGreen Dot = Vertex Found.\nClick to Apply.", MessageType.Info);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_isEditMode || _targetObject == null) return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.IsChildOf(_targetObject.transform))
            {
                Vector3 snapPos = GetClosestVertex(hit);

                Handles.color = Color.green;
                Handles.DrawWireDisc(snapPos, hit.normal, 0.2f);
                Handles.DrawSolidDisc(snapPos, hit.normal, 0.05f);

                sceneView.Repaint();

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    ApplyNewPivot(snapPos);
                    e.Use();
                }
            }
        }
    }

    private Vector3 GetClosestVertex(RaycastHit hit)
    {
        MeshFilter meshFilter = hit.collider.GetComponent<MeshFilter>();
        if (meshFilter == null) return hit.point;

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Transform hitTransform = hit.collider.transform;

        Vector3 closestPoint = Vector3.zero;
        float minDst = float.MaxValue;

        Vector3 localHit = hitTransform.InverseTransformPoint(hit.point);

        for (int i = 0; i < vertices.Length; i++)
        {
            float dst = Vector3.SqrMagnitude(vertices[i] - localHit);
            if (dst < minDst)
            {
                minDst = dst;
                closestPoint = vertices[i];
            }
        }

        return hitTransform.TransformPoint(closestPoint);
    }

    private void ApplyNewPivot(Vector3 newPivotWorldPos)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Fix Pivot");
        var undoGroup = Undo.GetCurrentGroup();

        GameObject newParent = new GameObject(_targetObject.name + "_PivotRoot");
        Undo.RegisterCreatedObjectUndo(newParent, "Create Pivot Root");

        newParent.transform.position = newPivotWorldPos;
        newParent.transform.rotation = _targetObject.transform.rotation;

        if (_targetObject.transform.parent != null)
        {
            newParent.transform.SetParent(_targetObject.transform.parent);
        }

        Undo.RecordObject(_targetObject.transform, "Re-Parent Mesh");
        _targetObject.transform.SetParent(newParent.transform);

        Selection.activeGameObject = newParent;
        _isEditMode = false;
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"Pivot Moved! New root created: {newParent.name}");
    }
}