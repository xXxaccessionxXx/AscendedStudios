using UnityEngine;
// This script just holds the color data for our Editor tool.
// We allow it to execute in Edit Mode so we can see updates instantly.
[ExecuteInEditMode]
public class GroupColor : MonoBehaviour
{
    public Color displayColor = new Color(0.2f, 0.6f, 1f, 0.3f); // Default Light Blue
}