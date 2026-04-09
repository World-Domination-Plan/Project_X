using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InviteTest))]
public class InviteTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        InviteTest test = (InviteTest)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("RUN INVITE TEST", GUILayout.Height(30)))
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[InviteTest] Must be in Play Mode to send invites.");
            }
            else
            {
                test.RunInviteTest();
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
