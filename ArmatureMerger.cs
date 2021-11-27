#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class ArmatureMerger : MonoBehaviour
{
    public Animator MergeArmatureInto;

    public void Merge()
    {
        if (MergeArmatureInto == null)
        {
            Debug.LogError("Merge To Object Is Null!");
            return;
        }

        var ThisAnim = gameObject.GetComponent<Animator>();

        if (ThisAnim == null)
        {
            Debug.LogError("Merge From Object Is Null!");
            return;
        }

        if (ThisAnim.avatar == null)
        {
            Debug.LogError("Avatar Inside Animator Not Found On Merge From Object!");
            return;
        }

        if (MergeArmatureInto.avatar == null)
        {
            Debug.LogError("Avatar Inside Animator Not Found On Merge To Object!");
            return;
        }

        for (var i = 0; i < 54; i++)
        {
            var Bone = ThisAnim.GetBoneTransform((HumanBodyBones)i);

            if (Bone != null && MergeArmatureInto.GetBoneTransform((HumanBodyBones)i) is var IntoBone && IntoBone != null)
            {
                Bone.SetParent(IntoBone, true);
            }
        }

        var NormalMeshes = ThisAnim.GetComponentsInChildren<MeshRenderer>(true);

        foreach (var mesh in NormalMeshes)
        {
            mesh.transform.SetParent(MergeArmatureInto.transform, true);
        }

        var SkinnedMeshes = ThisAnim.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var mesh in SkinnedMeshes)
        {
            mesh.transform.SetParent(MergeArmatureInto.transform, true);
        }
    }
}

[CustomEditor(typeof(ArmatureMerger))] 
public class ArmatureInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ((ArmatureMerger)target).MergeArmatureInto = EditorGUILayout.ObjectField("Merge Armature Into:", ((ArmatureMerger)target).MergeArmatureInto, typeof(Animator), true) as Animator;

        GUILayout.Label("Note: This Component Is To Be Added To The GameObject Of The Model\nWhere An Animator Is Present To Merge Into Your Model.");

        if (GUILayout.Button("Merge!"))
        {
            ((ArmatureMerger)target).Merge();
        }
    }
}
#endif
