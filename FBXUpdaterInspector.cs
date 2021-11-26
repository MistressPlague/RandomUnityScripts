#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FBXUpdater))]
public class FBXUpdaterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Label("");
        ((FBXUpdater)target).UpdatingTo = EditorGUILayout.ObjectField("Parent Object Of New Ver:", ((FBXUpdater)target).UpdatingTo, typeof(GameObject), true) as GameObject;

        GUILayout.Label("Note: This Is To Be Added To The OLD PARENT GameObject Of Your Model.");

        if (GUILayout.Button("Update To New FBX!"))
        {
            ((FBXUpdater) target).UpdateFBX();
        }
    }
}
#endif