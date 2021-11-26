#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

[DisallowMultipleComponent]
#if VRC_SDK_VRCSDK3
[RequireComponent(typeof(VRCAvatarDescriptor))]
#elif VRC_SDK_VRCSDK2
    [RequireComponent(typeof(VRC_AvatarDescriptor))]
#endif
public class FBXUpdater : MonoBehaviour
{
    public GameObject UpdatingTo;

    public void UpdateFBX()
    {
        Debug.Log("Init!");

        if (PrefabUtility.IsPartOfPrefabInstance(UpdatingTo))
        {
            Debug.Log("Unpacking UpdatingTo Object!");
            PrefabUtility.UnpackPrefabInstance(UpdatingTo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        var AllComponentsOld = gameObject.GetComponentsInChildren<Component>(true);
        var AllComponentsNew = UpdatingTo.GetComponentsInChildren<Component>(true);

        var AllOldTransforms = ToComponentList<Transform>(AllComponentsOld);
        var AllNewTransforms = ToComponentList<Transform>(AllComponentsNew);

        Debug.Log("Got All Components!");

        #region Update SkinnedMeshRenderers' Materials

        var skinnedMeshRenderersOld = ToComponentList<SkinnedMeshRenderer>(AllComponentsOld).Select(Old => (Old, ToComponentList<SkinnedMeshRenderer>(AllComponentsNew).FirstOrDefault(p => p.sharedMesh.name == Old.sharedMesh.name && !AssetDatabase.GetAssetPath(p).Contains("unity default resources")))).Where(o => o.Old != null && o.Item2 != null).ToList();

        Debug.Log($"Got {skinnedMeshRenderersOld.Count} SkinnedMeshRenderers With Matching Mesh Names & SubMesh Counts!");

        for (var index = 0; index < skinnedMeshRenderersOld.Count; index++)
        {
            var entry = skinnedMeshRenderersOld[index];

            if ((entry.Item2.sharedMaterials.Length == 1 && entry.Old.sharedMaterials.Length == 1) || (entry.Item2.sharedMaterials.Length == 1 && entry.Old.sharedMaterials.Length == 2 && entry.Old.sharedMaterials.GroupBy(x => x.name).All(g => g.Count() == 1))) // No Changes
            {
                //Debug.Log($"Updating Single Material: {entry.Item2.sharedMaterial.name.Replace(" (Instance)", "")} On Mesh Object: {entry.Item2.name} To: {entry.Old.sharedMaterial.name.Replace(" (Instance)", "")}!");

                entry.Item2.sharedMaterial = entry.Old.sharedMaterial;
            }
            else
            {
                var MatchingMaterialsOnEntry = entry.Old.sharedMaterials.Select(Old => (Old, entry.Item2.sharedMaterials.FirstOrDefault(p => p.name.Replace(" (Instance)", "") == Old.name.Replace(" (Instance)", "")))).Where(o => o.Old != null && o.Item2 != null).ToList();

                var NewMats = new List<Material>(entry.Item2.sharedMaterials);

                for (var i = 0; i < MatchingMaterialsOnEntry.Count; i++)
                {
                    var matching = MatchingMaterialsOnEntry[i];

                    //Debug.Log($"Updating Material: {matching.Item2.name.Replace(" (Instance)", "")} On Mesh Object: {entry.Item2.name} To: {matching.Old.name.Replace(" (Instance)", "")}!");

                    var Index = NewMats.IndexOf(NewMats.First(o => o.name.Replace(" (Instance)", "") == matching.Old.name.Replace(" (Instance)", "")));

                    NewMats[Index] = matching.Old;
                }

                entry.Item2.sharedMaterials = NewMats.ToArray();
            }

            entry.Item2.gameObject.SetActive(entry.Old.gameObject.activeSelf);
            entry.Item2.gameObject.name = entry.Old.gameObject.name;

            //Hierarchy Matching
            if (entry.Old.transform.parent.name != entry.Old.transform.root.name)
            {
                var PathToCreate = "";

                var CurrentObject = entry.Old.transform.parent;
                while (CurrentObject != entry.Old.transform.root) // Create Path String - Loop
                {
                    if (CurrentObject == null || string.IsNullOrWhiteSpace(CurrentObject.name))
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(PathToCreate))
                    {
                        PathToCreate = CurrentObject.name;
                    }
                    else
                    {
                        PathToCreate += CurrentObject.name + "/" + PathToCreate;
                    }

                    CurrentObject = CurrentObject.parent;
                }

                Debug.Log($"Path To Create: {PathToCreate} - This Always Should Show Above A Non Object ForEach Set Inside");

                if (FindOrNull(UpdatingTo.transform, PathToCreate) == null)
                {
                    GameObject CurrentDupedObj = null;
                    foreach (var ObjName in PathToCreate.Split('/'))
                    {
                        if (string.IsNullOrWhiteSpace(ObjName))
                        {
                            break;
                        }

                        var NewDupe = new GameObject(ObjName);

                        Debug.Log($"[ForEach] Created Object {NewDupe.name}! - [D]: Path: {PathToCreate}");

                        if (CurrentDupedObj != null)
                        {
                            CurrentDupedObj.transform.SetParent(NewDupe.transform);
                            Debug.Log($"[ForEach] Set Object {NewDupe.name} Inside {CurrentDupedObj.name}! - [D]: Path: {PathToCreate}");
                        }
                        else
                        {
                            entry.Item2.transform.SetParent(NewDupe.transform);
                            Debug.Log($"[ForEach] Set {entry.Item2.transform.name} Inside {NewDupe.name}! - [D]: Path: {PathToCreate}");
                        }

                        CurrentDupedObj = NewDupe;
                    }

                    if (CurrentDupedObj != null)
                    {
                        CurrentDupedObj.transform.SetParent(UpdatingTo.transform);
                        Debug.Log($"Set Path Inside Root! - New Path: {CurrentDupedObj.transform.parent}/{CurrentDupedObj.transform.name}");
                    }

                    Debug.LogWarning($"Created Path As It Never Existed!");
                }
                else
                {
                    Debug.Log($"Path To Create Found!");
                    entry.Item2.transform.SetParent(FindOrNull(UpdatingTo.transform, PathToCreate));
                }
            }
        }

        Debug.Log($"Finished Updating {skinnedMeshRenderersOld.Count} SkinnedMeshRenderers' Materials!");

        #endregion

        #region Update MeshRenderers' / MeshFilters' Materials

        var meshRenderersOld = ToComponentList<MeshFilter>(AllComponentsOld).Select(Old => (Old, ToComponentList<MeshFilter>(AllComponentsNew).FirstOrDefault(p => p.sharedMesh.name == Old.sharedMesh.name && !AssetDatabase.GetAssetPath(p).Contains("unity default resources")))).Where(o => o.Old != null && o.Item2 != null).ToList();

        Debug.Log($"Got {meshRenderersOld.Count} MeshRenderers With Matching Mesh Names & That Aren't Unity Default Meshes!");

        for (var index = 0; index < meshRenderersOld.Count; index++)
        {
            var entry = meshRenderersOld[index];
            var OldRenderer = entry.Old.GetComponent<MeshRenderer>();
            var NewRenderer = entry.Item2.GetComponent<MeshRenderer>();

            if ((NewRenderer.sharedMaterials.Length == 1 && OldRenderer.sharedMaterials.Length == 1) || (NewRenderer.sharedMaterials.Length == 1 && OldRenderer.sharedMaterials.Length == 2 && OldRenderer.sharedMaterials.GroupBy(x => x.name).All(g => g.Count() == 1))) // No Changes
            {
                //Debug.Log($"Updating Single Material: {NewRenderer.sharedMaterial.name.Replace(" (Instance)", "")} On Mesh Object: {entry.Item2.name} To: {OldRenderer.sharedMaterial.name.Replace(" (Instance)", "")}!");

                NewRenderer.sharedMaterial = OldRenderer.sharedMaterial;
            }
            else
            {
                var MatchingMaterialsOnEntry = OldRenderer.sharedMaterials.Select(Old => (Old, NewRenderer.sharedMaterials.FirstOrDefault(p => p.name.Replace(" (Instance)", "") == Old.name.Replace(" (Instance)", "")))).Where(o => o.Old != null && o.Item2 != null).ToList();

                var NewMats = new List<Material>(NewRenderer.sharedMaterials);

                for (var i = 0; i < MatchingMaterialsOnEntry.Count; i++)
                {
                    var matching = MatchingMaterialsOnEntry[i];

                    //Debug.Log($"Updating Material: {matching.Item2.name.Replace(" (Instance)", "")} On Mesh Object: {entry.Item2.name} To: {matching.Old.name.Replace(" (Instance)", "")}!");

                    var Index = NewMats.IndexOf(NewMats.First(o => o.name.Replace(" (Instance)", "") == matching.Old.name.Replace(" (Instance)", "")));

                    NewMats[Index] = matching.Old;
                }

                NewRenderer.sharedMaterials = NewMats.ToArray();
            }
        }

        Debug.Log($"Finished Updating {meshRenderersOld.Count} MeshRenderers' Materials!");

        #endregion

        #region Move VRC Components To New

        var Descriptor = ToComponentList<VRCAvatarDescriptor>(AllComponentsOld).FirstOrDefault();
        var NewDescriptor = UpdatingTo.AddComponent<VRCAvatarDescriptor>();
        EditorUtility.CopySerialized(Descriptor, NewDescriptor);

        Debug.Log("Copied Old VRCAvatarDescriptor To New!");

        var PipelineMan = ToComponentList<PipelineManager>(AllComponentsOld).FirstOrDefault();
        var NewPipelineMan = UpdatingTo.AddComponent<PipelineManager>();
        EditorUtility.CopySerialized(PipelineMan, NewPipelineMan);

        Debug.Log("Copied Old PipelineManager To New!");

        #endregion

        #region Move DynamicBones To New

        var OldDynBones = ToComponentList<DynamicBone>(AllComponentsOld).Where(o => o?.m_Root != null).Select(DynBone => (DynBone, DynBone.transform.parent?.name + "/" + DynBone.gameObject.name, DynBone.m_Root.parent?.name + "/" + DynBone.m_Root.name));

        foreach (var dynBone in OldDynBones)
        {
            var BoneToAddTo = AllNewTransforms.FirstOrDefault(o => (o.parent?.name + "/" + o.name) == dynBone.Item2);
            var RootBone = AllNewTransforms.FirstOrDefault(o => (o.parent?.name + "/" + o.name) == dynBone.Item3);

            if (BoneToAddTo == null || RootBone == null)
            {
                continue;
            }

            var Bone = BoneToAddTo.gameObject.AddComponent<DynamicBone>();
            EditorUtility.CopySerialized(dynBone.DynBone, Bone);
            Bone.m_Root = RootBone;
        }

        #endregion

        #region Move DynamicBoneColliders To New

        var OldDynBoneColliders = ToComponentList<DynamicBoneCollider>(AllComponentsOld).Where(o => o != null).Select(DynBoneCol => (DynBoneCol, DynBoneCol.transform.parent?.name + "/" + DynBoneCol.gameObject.name));

        foreach (var dynBonecol in OldDynBoneColliders)
        {
            var BoneToAddTo = AllNewTransforms.FirstOrDefault(o => (o.parent?.name + "/" + o.name) == dynBonecol.Item2);

            if (BoneToAddTo == null)
            {
                continue;
            }

            var Col = BoneToAddTo.gameObject.AddComponent<DynamicBoneCollider>();
            EditorUtility.CopySerialized(dynBonecol.DynBoneCol, Col);
        }

        #endregion

        #region Organize Transforms

        var AllMatchingObjects = AllOldTransforms.Where(i => i != null && i.parent != null).Select(Old => (Old, AllNewTransforms.Where(u => u.parent != null).FirstOrDefault(o => (Old.parent.name + "/" + Old.name) == (o.parent.name + "/" + o.name)))).Where(p => p.Item2 != null);

        foreach (var entry in AllMatchingObjects)
        {
            entry.Item2.SetSiblingIndex(entry.Old.GetSiblingIndex());
        }

        #endregion
    }

    public static Transform FindOrNull(Transform transform, string path)
    {
        try
        {
            path = path.Replace("\\", "/");

            if (path[0] == '/')
            {
                path = path.Substring(1, path.Length);
            }

            if (path[path.Length - 1] == '/')
            {
                path = path.Substring(0, path.Length - 1);
            }

            var EndObject = transform;

            foreach (var child in path.Split('/'))
            {
                if (!string.IsNullOrWhiteSpace(child) && child.Length > 1)
                {
                    EndObject = EndObject.Find(child.Replace("/", ""));
                }
            }

            if (EndObject == null || EndObject == transform)
            {
                Debug.LogError($"Failed To Find Child Object At Path: {path}");
            }

            return EndObject;
        }
        catch
        {

        }

        Debug.LogError($"Failed To Find Child Object At Path: {path}");

        return null;
    }

    public static string GetPathToObject(Transform obj)
    {
        var Path = obj.name;

        if (obj.transform.parent != null)
        {
            Path = GetPathToObject(obj.transform.parent) + "/" + Path;
        }

        return Path;
    }

    public static List<T> ToComponentList<T>(IEnumerable<Component> list) where T : Component
    {
        return OfILCastedType<T>(list);
    }

    public static List<T> OfILCastedType<T>(IEnumerable<Component> source) where T : Component
    {
        return source == null ? null : OfTypeIterator<T>(source).ToList();
    }

    private static IEnumerable<T> OfTypeIterator<T>(IEnumerable<Component> source) where T : Component
    {
        foreach (var obj in source)
        {
            if (obj != null && obj is T && obj is var result)
            {
                yield return result as T;
            }
        }
    }
}
#endif