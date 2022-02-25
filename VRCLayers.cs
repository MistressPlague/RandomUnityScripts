#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

public class VRCLayers : Editor
{
    internal static Dictionary<int, string> VRChatLayerToLayerName = new Dictionary<int, string>()
        {
            { 0, "Default" },
            { 1, "TransparentFX" },
            { 2, "Ignore Raycast" },
            { 3, "Empty1" },
            { 4, "Water" },
            { 5, "UI" },
            { 6, "Empty2" },
            { 7, "Empty3" },
            { 8, "Interactive" },
            { 9, "Player" },
            { 10, "PlayerLocal" },
            { 11, "Enviroment" },
            { 12, "UiMenu" },
            { 13, "Pickup" },
            { 14, "PickupNoEnviroment" },
            { 15, "StereoLeft" },
            { 16, "StereoRight" },
            { 17, "Walkthrough" },
            { 18, "MirrorReflection" },
            { 19, "reserved2" },
            { 20, "reserved3" },
            { 21, "reserved4" },
            { 22, "PostProcessing" },
            { 23, "Empty4" },
            { 24, "Empty5" },
            { 25, "Empty6" },
            { 26, "Empty7" },
            { 27, "Empty8" },
            { 28, "Empty9" },
            { 29, "Empty10" },
            { 30, "Empty11" },
            { 31, "Empty12" },
        };

    [MenuItem("VRCLayers/Generate")]
    public static void Generate()
    {
        UpdateLayers.SetLayers(VRChatLayerToLayerName);

        Debug.Log("Done!");
    }
}
#endif