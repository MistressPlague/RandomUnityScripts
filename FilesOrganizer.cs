#if UNITY_EDITOR
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

        if (!AssetDatabase.IsValidFolder(FullDestPath))
        {
            AssetDatabase.CreateFolder("Assets", DestFolderName);
        }

        void WriteAssetsToPath(IEnumerable<string> items, string SubFolder)
        {
            if (!AssetDatabase.IsValidFolder($"{FullDestPath}/{SubFolder}"))
            {
                var dirs = SubFolder.Split('/');

                var BuiltDir = $"{FullDestPath}";
                foreach (var dir in dirs)
                {
                    if (string.IsNullOrEmpty(dir))
                    {
                        continue;
                    }

                    Debug.Log($"BuiltDir: {BuiltDir}");
                    Debug.Log($"dir: {dir}");

                    if (!AssetDatabase.IsValidFolder($"{BuiltDir}/{dir}"))
                    {
                        AssetDatabase.CreateFolder(BuiltDir, dir);
                        Debug.Log($"Created Dir: {BuiltDir}/{dir}");
                    }

                    BuiltDir += $"/{dir}";
                }
            }

            foreach (var path in items)
            {
                if (!path.Contains($"{FullDestPath}/{SubFolder}") && !path.Contains("unity_built"))
                {
                    var Message = AssetDatabase.MoveAsset(path, $"{FullDestPath}/{SubFolder}/{Path.GetFileName(path)}");

                    if (!string.IsNullOrEmpty(Message))
                    {
                        EditorUtility.DisplayDialog("Error Moving Asset", $"{path} -> {FullDestPath}/{SubFolder}\r\n\r\n{Message}", "Ok");
                    }
                }
            }
        }

        #region Scene Organizing
        var ScenePath = SceneManager.GetActiveScene().path;

        WriteAssetsToPath(new [] { ScenePath }, "");
        #endregion

        #region Mesh Organizing
        var SkinnedMeshes = Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(o => AssetDatabase.GetAssetPath(o.sharedMesh)).Distinct();

        var PlainMeshMeshes = Selection.activeGameObject.GetComponentsInChildren<MeshFilter>(true).Select(o => AssetDatabase.GetAssetPath(o.sharedMesh)).Distinct();

        var ParticleMeshes = Selection.activeGameObject.GetComponentsInChildren<ParticleSystemRenderer>(true).Select(o => AssetDatabase.GetAssetPath(o.mesh)).Distinct();

        var CombinedMeshes = SkinnedMeshes.Concat(PlainMeshMeshes).Concat(ParticleMeshes).Distinct().Where(o => !string.IsNullOrEmpty(o) && !o.ToLower().Contains("default resources"));

        var AllFBXs = CombinedMeshes.Where(o => o.ToLower().Contains(".fbx"));
        var AllGenerics = CombinedMeshes.Where(o => !o.ToLower().Contains(".fbx"));

        WriteAssetsToPath(AllFBXs, "FBX");
        WriteAssetsToPath(AllGenerics, "Mesh");
        #endregion

        #region Material/Textures Organizing

        var SkinnedMaterials = Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(a => a.sharedMaterials);

        var PlainMeshMaterials = Selection.activeGameObject.GetComponentsInChildren<MeshRenderer>(true).SelectMany(a => a.sharedMaterials);

        var ParticleMaterials = Selection.activeGameObject.GetComponentsInChildren<ParticleSystemRenderer>(true).SelectMany(a => a.sharedMaterials);

        var AllMaterials = SkinnedMaterials.Concat(PlainMeshMaterials).Concat(ParticleMaterials).Select(AssetDatabase.GetAssetPath).Distinct().Where(o => !string.IsNullOrEmpty(o));

        var AllTextures = Selection.activeGameObject.GetComponentsInChildren<Renderer>(true).SelectMany(GetTextures).Select(AssetDatabase.GetAssetPath).Distinct().Where(o => !string.IsNullOrEmpty(o));

        WriteAssetsToPath(AllMaterials, "Materials");
        WriteAssetsToPath(AllTextures, "Textures");
        #endregion

        #region VRChat Related Files Organizing
        var DescriptorObject = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();

        var MenuPaths = new List<string>();

        void AddAllSubMenus(VRCExpressionsMenu Menu)
        {
            MenuPaths.Add(AssetDatabase.GetAssetPath(Menu));

            foreach (var control in Menu.controls)
            {
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                {
                    // Recursion
                    AddAllSubMenus(control.subMenu);
                }
            }
        }

        AddAllSubMenus(DescriptorObject.expressionsMenu);

        MenuPaths = MenuPaths.Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();

        WriteAssetsToPath(MenuPaths, "VRChat/Menus");

        var ParamsPath = AssetDatabase.GetAssetPath(DescriptorObject.expressionParameters);

        var AllAnimatorControllers = DescriptorObject.baseAnimationLayers.Select(a => a.animatorController).Concat(DescriptorObject.specialAnimationLayers.Select(a => a.animatorController)).Where(o => o != null);

        var AllAnimClips = AllAnimatorControllers.SelectMany(o => o.animationClips);

        WriteAssetsToPath(AllAnimClips.Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p)), "VRChat/Animations");

        WriteAssetsToPath(AllAnimatorControllers.Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p)), "VRChat/Controllers");

        WriteAssetsToPath(new [] { ParamsPath }, "VRChat");
        #endregion

        #region Component Related Files Organizing

        var AllAudioClips = Selection.activeGameObject.GetComponentsInChildren<AudioSource>(true).Select(o => o.clip);

        WriteAssetsToPath(AllAudioClips.Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p)), "AudioClips");

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
}
#endif
