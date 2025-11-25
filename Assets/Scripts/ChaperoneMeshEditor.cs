#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChaperoneMesh))]
public class ChaperoneMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        ChaperoneMesh chaperoneMesh = (ChaperoneMesh)target;

        if (GUILayout.Button("Refresh Mesh"))
        {
            chaperoneMesh.LoadPlayArea();
        }

        if (GUILayout.Button("Save Changes"))
        {
            chaperoneMesh.SaveToSharedMemory();
        }
    }
}
#endif