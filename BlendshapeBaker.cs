#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BlendshapeBaker : EditorWindow
{
    private SkinnedMeshRenderer targetRenderer;
    private Vector2 scrollPosition;
    private Dictionary<int, bool> selectedBlendshapes = new Dictionary<int, bool>();
    private Dictionary<int, float> blendshapeValues = new Dictionary<int, float>();
    private bool removeAfterBaking = true;
    private string savePath = "";
    private bool overwriteExisting = false;
        
    [MenuItem("Tools/Blendshape Baker")]
    public static void ShowWindow()
    {
        GetWindow<BlendshapeBaker>("Blendshape Baker");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Blendshape Baker Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUI.BeginChangeCheck();
        targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Target Mesh", targetRenderer, typeof(SkinnedMeshRenderer), true);
        if (EditorGUI.EndChangeCheck())
        {
            // Reset selections when changing the target
            selectedBlendshapes.Clear();
            blendshapeValues.Clear();
            
            // Initialize with default values if we have a valid renderer
            if (targetRenderer != null && targetRenderer.sharedMesh != null)
            {
                for (var i = 0; i < targetRenderer.sharedMesh.blendShapeCount; i++)
                {
                    selectedBlendshapes[i] = false;
                    blendshapeValues[i] = 0f;
                }
            }
        }
        
        EditorGUILayout.Space();
        
        if (targetRenderer == null)
        {
            EditorGUILayout.HelpBox("Please assign a SkinnedMeshRenderer to begin.", MessageType.Info);
            return;
        }
        
        if (targetRenderer.sharedMesh == null || targetRenderer.sharedMesh.blendShapeCount == 0)
        {
            EditorGUILayout.HelpBox("The selected mesh has no blendshapes.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField("Blendshapes", EditorStyles.boldLabel);
        
        // Allow toggling all blendshapes
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            for (var i = 0; i < targetRenderer.sharedMesh.blendShapeCount; i++)
            {
                selectedBlendshapes[i] = true;
            }
        }
        
        if (GUILayout.Button("Deselect All"))
        {
            for (var i = 0; i < targetRenderer.sharedMesh.blendShapeCount; i++)
            {
                selectedBlendshapes[i] = false;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Scrollable area for blendshapes
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (var i = 0; i < targetRenderer.sharedMesh.blendShapeCount; i++)
        {
            var blendshapeName = targetRenderer.sharedMesh.GetBlendShapeName(i);
            
            EditorGUILayout.BeginHorizontal();
            
            // Ensure dictionaries contain this index
            if (!selectedBlendshapes.ContainsKey(i))
                selectedBlendshapes[i] = false;
            if (!blendshapeValues.ContainsKey(i))
                blendshapeValues[i] = 0f;
            
            // Checkbox for selection
            selectedBlendshapes[i] = EditorGUILayout.Toggle(selectedBlendshapes[i], GUILayout.Width(20));
            
            // Display blendshape name
            EditorGUILayout.LabelField(blendshapeName, GUILayout.Width(150));
            
            // Value slider
            EditorGUI.BeginDisabledGroup(!selectedBlendshapes[i]);
            blendshapeValues[i] = EditorGUILayout.Slider(blendshapeValues[i], 0f, 100f);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // Option to remove blendshape after baking
        removeAfterBaking = EditorGUILayout.Toggle("Remove After Baking", removeAfterBaking);
            
        EditorGUILayout.HelpBox("If checked, blendshapes will be removed after baking. Unchecking allows you to reapply the effect multiple times.", MessageType.Info);
            
        EditorGUILayout.Space();
            
        EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Save Path");
        var defaultPath = string.IsNullOrEmpty(savePath) && targetRenderer != null && targetRenderer.sharedMesh != null
            ? $"Assets/{targetRenderer.sharedMesh.name}_Baked.asset"
            : savePath;
        savePath = EditorGUILayout.TextField(defaultPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            var initialPath = string.IsNullOrEmpty(savePath) ? "Assets" : savePath;
            var directory = System.IO.Path.GetDirectoryName(initialPath);
            if (string.IsNullOrEmpty(directory)) directory = "Assets";
            
            var selectedPath = EditorUtility.SaveFilePanelInProject(
                "Save Mesh Asset",
                targetRenderer != null && targetRenderer.sharedMesh != null ? $"{targetRenderer.sharedMesh.name}_Baked.asset" : "BakedMesh.asset",
                "asset",
                "Save the baked mesh as an asset",
                directory
            );
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                savePath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        EditorGUILayout.HelpBox("Specify where to save the baked mesh asset. If left empty, will use default location.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        GUI.enabled = HasSelectedBlendshapes();
        if (GUILayout.Button("Bake Selected Blendshapes"))
        {
            BakeBlendshapes();
        }
        GUI.enabled = true;
    }
    
    private bool HasSelectedBlendshapes()
    {
        foreach (var selected in selectedBlendshapes.Values)
        {
            if (selected)
                return true;
        }
        return false;
    }
    
    private void BakeBlendshapes()
    {
        if (targetRenderer == null || targetRenderer.sharedMesh == null)
            return;
        
        // Record for undo and apply changes
        Undo.RecordObject(targetRenderer, "Bake Blendshapes");
        
        // Get a reference to the original mesh
        var originalMesh = targetRenderer.sharedMesh;
        
        // Create a new mesh to avoid modifying the asset directly
        var newMesh = Instantiate(originalMesh);
        
        // Store current blendshape values by name to restore later
        var originalBlendshapeValues = new Dictionary<string, float>();
        
        // Store all current blendshape values
        for (var i = 0; i < originalMesh.blendShapeCount; i++)
        {
            var blendshapeName = originalMesh.GetBlendShapeName(i);
            originalBlendshapeValues[blendshapeName] = targetRenderer.GetBlendShapeWeight(i);
        }
        
        // List to keep track of blendshape names to remove
        var blendshapeNamesToRemove = new List<string>();
        
        // Apply the selected blendshapes directly to the vertices
        var originalVertices = originalMesh.vertices;
        var newVertices = new Vector3[originalVertices.Length];
        var originalNormals = originalMesh.normals;
        var newNormals = new Vector3[originalNormals.Length];
        var originalTangents = originalMesh.tangents;
        var newTangents = new Vector4[originalTangents.Length];

        // Copy original vertices, normals, and tangents as the base
        Array.Copy(originalVertices, newVertices, originalVertices.Length);
        Array.Copy(originalNormals, newNormals, originalNormals.Length);
        Array.Copy(originalTangents, newTangents, originalTangents.Length);
        
        // Apply selected blendshapes directly to vertices
        for (var i = 0; i < originalMesh.blendShapeCount; i++)
        {
            if (selectedBlendshapes.ContainsKey(i) && selectedBlendshapes[i])
            {
                var weight = blendshapeValues[i] / 100f; // Convert from 0-100 range to 0-1
                var blendshapeName = originalMesh.GetBlendShapeName(i);
                
                if (removeAfterBaking)
                {
                    blendshapeNamesToRemove.Add(blendshapeName);
                }
                
                // Apply each frame of the blendshape (usually just one at 100%)
                var frameCount = originalMesh.GetBlendShapeFrameCount(i);
                
                // We're only applying the last frame (usually at weight 100) and scaling by user's weight
                var lastFrameIdx = frameCount - 1;
                
                // Get delta arrays for this blendshape
                var deltaVertices = new Vector3[originalVertices.Length];
                var deltaNormals = new Vector3[originalNormals.Length];
                var deltaTangents = new Vector3[originalTangents.Length];
        
                originalMesh.GetBlendShapeFrameVertices(i, lastFrameIdx, deltaVertices, deltaNormals, deltaTangents);
        
                // Apply weighted deltas to the vertices, normals, and tangents
                for (var v = 0; v < newVertices.Length; v++)
                {
                    // Apply delta with appropriate scaling to prevent mesh shrinkage
                    newVertices[v] += deltaVertices[v] * weight;
                    
                    // Update normals
                    var normal = newNormals[v] + deltaNormals[v] * weight;
                    newNormals[v] = normal.normalized; // Keep normals normalized
                    
                    // For tangents, we need to handle the w component separately
                    var tangent = newTangents[v];
                    tangent.x += deltaTangents[v].x * weight;
                    tangent.y += deltaTangents[v].y * weight;
                    tangent.z += deltaTangents[v].z * weight;
                    
                    // Normalize the tangent xyz components while preserving w
                    var tangentW = tangent.w;
                    var tangentXYZ = new Vector3(tangent.x, tangent.y, tangent.z).normalized;
                    newTangents[v] = new Vector4(tangentXYZ.x, tangentXYZ.y, tangentXYZ.z, tangentW);
                }
            }
        }
        
        // Update mesh with modified vertices
        newMesh.vertices = newVertices;
        newMesh.normals = newNormals;
        newMesh.tangents = newTangents;
        
        // Remove selected blendshapes if option is checked
        if (removeAfterBaking)
        {
            // Clear all blendshapes
            newMesh.ClearBlendShapes();
            
            // Add back only the non-selected blendshapes
            for (var i = 0; i < originalMesh.blendShapeCount; i++)
            {
                var blendShapeName = originalMesh.GetBlendShapeName(i);
                
                // Skip blendshapes that should be removed
                if (blendshapeNamesToRemove.Contains(blendShapeName))
                    continue;
                
                // Get frame count for this blendshape
                var frameCount = originalMesh.GetBlendShapeFrameCount(i);
                
                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    // Get the frame weight
                    var frameWeight = originalMesh.GetBlendShapeFrameWeight(i, frameIndex);
                    
                    // Get the delta vertices, normals, and tangents for this frame
                    var deltaVertices = new Vector3[originalMesh.vertexCount];
                    var deltaNormals = new Vector3[originalMesh.vertexCount];
                    var deltaTangents = new Vector3[originalMesh.vertexCount];
                    
                    originalMesh.GetBlendShapeFrameVertices(i, frameIndex, deltaVertices, deltaNormals, deltaTangents);
                    
                    // Add the blendshape frame to the new mesh
                    newMesh.AddBlendShapeFrame(blendShapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                }
            }
        }
            
        // Save the mesh as an asset
        var meshPath = savePath;
        if (string.IsNullOrEmpty(meshPath))
        {
            // Use default path if none specified
            meshPath = $"Assets/{originalMesh.name}_Baked.asset";
        }
            
        // Ensure the directory exists
        var directory = System.IO.Path.GetDirectoryName(meshPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
            
        // Check if the asset already exists
        var assetExists = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null;
        if (assetExists && !overwriteExisting)
        {
            // Find a unique name by appending a number
            var basePath = meshPath.Substring(0, meshPath.LastIndexOf(".", StringComparison.Ordinal));
            var extension = meshPath.Substring(meshPath.LastIndexOf(".", StringComparison.Ordinal));
            var counter = 1;
            
            while (AssetDatabase.LoadAssetAtPath<Mesh>($"{basePath}_{counter}{extension}") != null)
            {
                counter++;
            }
            
            meshPath = $"{basePath}_{counter}{extension}";
        }
            
        // Create or update the asset
        if (assetExists && overwriteExisting)
        {
            // Replace the existing asset
            var existingAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            EditorUtility.CopySerialized(newMesh, existingAsset);
            AssetDatabase.SaveAssets();
            // Use the existing asset reference
            newMesh = existingAsset;
        }
        else
        {
            // Create a new asset
            AssetDatabase.CreateAsset(newMesh, meshPath);
        }
            
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
            
        // Apply the new mesh - reload it from disk to ensure proper asset usage
        var finalMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        targetRenderer.sharedMesh = finalMesh;
            
        // Restore original blendshape values for remaining blendshapes
        for (var i = 0; i < newMesh.blendShapeCount; i++)
        {
            var blendshapeName = newMesh.GetBlendShapeName(i);
            
            // Restore the value if we have it stored
            if (originalBlendshapeValues.TryGetValue(blendshapeName, out var value))
            {
                targetRenderer.SetBlendShapeWeight(i, value);
            }
            else
            {
                targetRenderer.SetBlendShapeWeight(i, 0);
            }
        }
        
        // Reset selections if any were removed
        if (removeAfterBaking && blendshapeNamesToRemove.Count > 0)
        {
            selectedBlendshapes.Clear();
            blendshapeValues.Clear();
            
            for (var i = 0; i < newMesh.blendShapeCount; i++)
            {
                selectedBlendshapes[i] = false;
                blendshapeValues[i] = 0f;
            }
        }
        
        EditorUtility.DisplayDialog("Blendshape Baker", $"Blendshapes baked successfully!\nMesh saved to: {meshPath}", "OK");
    }
}
#endif