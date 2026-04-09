using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FullWorkflowTest))]
public class FullWorkflowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        FullWorkflowTest test = (FullWorkflowTest)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("RUN FULL WORKFLOW TEST", GUILayout.Height(40)))
        {
            if (!Application.isPlaying) 
            {
                Debug.LogError("[FullWorkflowTest] Must be in Play Mode to run workflow.");
            }
            else
            {
                test.RunWorkflowTest();
            }
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Fetch Artist ID"))
        {
            _ = test.FetchArtistIdByName();
        }
    }
}
