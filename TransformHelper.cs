#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TransformHelper : Editor
{
    private static Dictionary<Transform, (Vector3, Quaternion, Vector3)> TransformValues = new Dictionary<Transform, (Vector3, Quaternion, Vector3)>();

    [MenuItem("GameObject/TransformHelper/Copy Transform Values", false, -1)]
    private static void CopyTransformValues()
    {
        var ParentObj = Selection.activeGameObject;

        var AllTransforms = ParentObj.GetComponentsInChildren<Transform>(true);

        foreach (var trans in AllTransforms)
        {
            TransformValues[trans] = (trans.position, trans.rotation, trans.localScale);
        }
    }

    [MenuItem("GameObject/TransformHelper/Paste Transform Values", false, -1)]
    private static void PasteTransformValues()
    {
        foreach (var entry in TransformValues)
        {
            entry.Key.position = entry.Value.Item1;
            entry.Key.rotation = entry.Value.Item2;
            entry.Key.localScale = entry.Value.Item3;
        }
    }

    [MenuItem("GameObject/TransformHelper/Paste Transform Positions", false, -1)]
    private static void PasteTransformPositions()
    {
        foreach (var entry in TransformValues)
        {
            entry.Key.position = entry.Value.Item1;
        }
    }

    [MenuItem("GameObject/TransformHelper/Paste Transform Rotations", false, -1)]
    private static void PasteTransformRotations()
    {
        foreach (var entry in TransformValues)
        {
            entry.Key.rotation = entry.Value.Item2;
        }
    }

    [MenuItem("GameObject/TransformHelper/Paste Transform Scales", false, -1)]
    private static void PasteTransformScales()
    {
        foreach (var entry in TransformValues)
        {
            entry.Key.localScale = entry.Value.Item3;
        }
    }
}
#endif
