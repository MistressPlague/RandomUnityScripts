#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

public class FilesOrganizer : Editor
{
    [MenuItem("GameObject/FileOrganizer/Organize Files")]
    // ReSharper disable once UnusedMember.Local
    private static void Organize()
    {
        var DestFolderName = Selection.activeTransform.name + "_Organized";

        var FullDestPath = "Assets/" + DestFolderName;

        CreateFolder("Assets", DestFolderName);

        CreateFolder(FullDestPath, "FBX");
        CreateFolder(FullDestPath, "Mesh");
        CreateFolder(FullDestPath, "Materials");
        CreateFolder(FullDestPath, "Textures");
        CreateFolder(FullDestPath, "VRChat"); CreateFolder(FullDestPath + "/VRChat", "Controllers"); CreateFolder(FullDestPath + "/VRChat", "Menus");

        #region Scene Organizing
        var ScenePath = SceneManager.GetActiveScene().path;

        if (!ScenePath.Contains($"{FullDestPath}/"))
        {
            var Message = AssetDatabase.MoveAsset(ScenePath, $"{FullDestPath}/{Path.GetFileName(ScenePath)}");

            if (!string.IsNullOrEmpty(Message))
            {
                EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
            }
        }
        #endregion

        #region Mesh Organizing
        var SkinnedMeshes = Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(o => AssetDatabase.GetAssetPath(o.sharedMesh)).Distinct();

        var PlainMeshMeshes = Selection.activeGameObject.GetComponentsInChildren<MeshFilter>(true).Select(o => AssetDatabase.GetAssetPath(o.sharedMesh)).Distinct();

        var ParticleMeshes = Selection.activeGameObject.GetComponentsInChildren<ParticleSystemRenderer>(true).Select(o => AssetDatabase.GetAssetPath(o.mesh)).Distinct();

        var CombinedMeshes = SkinnedMeshes.Concat(PlainMeshMeshes).Concat(ParticleMeshes).Distinct().Where(o => !string.IsNullOrEmpty(o) && !o.ToLower().Contains("default resources"));

        var AllFBXs = CombinedMeshes.Where(o => o.ToLower().Contains(".fbx"));
        var AllGenerics = CombinedMeshes.Where(o => !o.ToLower().Contains(".fbx"));

        foreach (var path in AllFBXs)
        {
            if (!path.Contains($"{FullDestPath}/FBX/"))
            {
                var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/FBX/{Path.GetFileName(path)}");

                if (!string.IsNullOrEmpty(Message))
                {
                    EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
                }
            }
        }

        foreach (var path in AllGenerics)
        {
            if (!path.Contains($"{FullDestPath}/Mesh/"))
            {
                var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/Mesh/{Path.GetFileName(path)}");

                if (!string.IsNullOrEmpty(Message))
                {
                    EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
                }
            }
        }
        #endregion

        #region Material/Textures Organizing

        var SkinnedMaterials = Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(a => a.sharedMaterials);

        var PlainMeshMaterials = Selection.activeGameObject.GetComponentsInChildren<MeshRenderer>(true).SelectMany(a => a.sharedMaterials);

        var ParticleMaterials = Selection.activeGameObject.GetComponentsInChildren<ParticleSystemRenderer>(true).SelectMany(a => a.sharedMaterials);

        var AllMaterials = SkinnedMaterials.Concat(PlainMeshMaterials).Concat(ParticleMaterials).Select(AssetDatabase.GetAssetPath).Distinct().Where(o => !string.IsNullOrEmpty(o));

        var AllTextures = Selection.activeGameObject.GetComponentsInChildren<Renderer>(true).SelectMany(GetTextures).Select(AssetDatabase.GetAssetPath).Distinct().Where(o => !string.IsNullOrEmpty(o));

        foreach (var path in AllMaterials)
        {
            if (!path.Contains($"{FullDestPath}/Materials/"))
            {
                var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/Materials/{Path.GetFileName(path)}");

                if (!string.IsNullOrEmpty(Message))
                {
                    EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
                }
            }
        }

        foreach (var path in AllTextures)
        {
            if (!path.Contains($"{FullDestPath}/Textures/"))
            {
                var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/Textures/{Path.GetFileName(path)}");

                if (!string.IsNullOrEmpty(Message))
                {
                    EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
                }
            }
        }
        #endregion

        #region VRChat Related Files Organizing
        var DescriptorObject = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();

        var MenuPaths = new List<string>();

        MenuPaths.AddRange(Flatten(DescriptorObject?.expressionsMenu?.controls?
                    .Where(o => o != null && o.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    .Select(p => p.subMenu),

                a => a?.controls?
                    .Where(o => o != null && o.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    .Select(p => p.subMenu))?

            .Where(o => o != null).Select(AssetDatabase.GetAssetPath)
            .Where(o => o != null) ?? Array.Empty<string>());

        //I don't know how this dumb flatten trash works
        var ToAdd = DescriptorObject?.expressionsMenu?.controls?.Where(o => o != null && o.type == VRCExpressionsMenu.Control.ControlType.SubMenu).Select(p => p.subMenu).Select(AssetDatabase.GetAssetPath);

        if (ToAdd != null)
        {
            MenuPaths.AddRange(ToAdd);
        }
        MenuPaths.Add(AssetDatabase.GetAssetPath(DescriptorObject?.expressionsMenu));

        MenuPaths = MenuPaths.Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();

        foreach (var path in MenuPaths)
        {
            if (!path.Contains($"{FullDestPath}/VRChat/Menus/"))
            {
                var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/VRChat/Menus/{Path.GetFileName(path)}");

                if (!string.IsNullOrEmpty(Message))
                {
                    EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
                }
            }
        }

        var ParamsPath = AssetDatabase.GetAssetPath(DescriptorObject?.expressionParameters);

        var AllAnimatorControllers = DescriptorObject?.baseAnimationLayers.Select(a => a.animatorController).Concat(DescriptorObject.specialAnimationLayers.Select(a => a.animatorController)).Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p));

        if (AllAnimatorControllers != null)
        {
            foreach (var path in AllAnimatorControllers)
            {
                if (!path.Contains($"{FullDestPath}/VRChat/Menus/"))
                {
                    var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/VRChat/Controllers/{Path.GetFileName(path)}");

                    if (!string.IsNullOrEmpty(Message))
                    {
                        EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(ParamsPath) && !ParamsPath.Contains($"{FullDestPath}/VRChat/"))
        {
            var Message = AssetDatabase.MoveAsset(ParamsPath, $"{FullDestPath}/VRChat/{Path.GetFileName(ParamsPath)}");

            if (!string.IsNullOrEmpty(Message))
            {
                EditorUtility.DisplayDialog("Error Moving Asset", Message, "Ok");
            }
        }
        #endregion

        AssetDatabase.SaveAssets();
    }

    public static IEnumerable<Texture> GetTextures(Renderer renderer)
    {
        foreach (var obj in EditorUtility.CollectDependencies(new Object[] { renderer }))
        {
            if (obj is Texture texture)
            {
                yield return texture;
            }
        }
    }

    public static IEnumerable<T> Flatten<T, R>(IEnumerable<T> source, Func<T, R> recursion) where R : IEnumerable<T>
    {
        try
        {
            var result = source?.SelectMany(x => (x != null && recursion(x) != null && recursion(x).Any()) ? Flatten(recursion(x).Where(z => z != null), recursion) : null).Where(x => x != null);

            return result;
        }
        catch
        {

        }

        return null;
    }

    private static void CreateFolder(string ParentFolder, string NewFolder)
    {
        if (!AssetDatabase.IsValidFolder($"{ParentFolder}/{NewFolder}"))
            AssetDatabase.CreateFolder(ParentFolder, NewFolder);
    }
}
#endif