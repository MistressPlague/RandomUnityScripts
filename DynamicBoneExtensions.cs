#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DynamicBoneExtensions : UnityEditor.EditorWindow
{
    //Created By Plague <3
    //Copyright Reserved
    [MenuItem("GameObject/DynamicBone Extensions/Set DynamicBones To Have All Colliders", false, -10)]
    private static void Set()
    {
        //Check If The User Has Even Got A GameObject Selected - Prevents NullReferenceExceptions Due To Such.
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogError("You Have No GameObjects Selected!");

            return;
        }

        //Define Object Reference
        GameObject RootArmature = null;

        //Define Variables
        int DynBonesFound = 0;

        //Retrieve The Armature Of The Avatar
        //Enumerate The First Child GameObject Of Each GameObject
        for (int i = 0; i < Selection.activeGameObject.transform.root.childCount; i++)
        {
            //Object Reference
            GameObject obj2 = Selection.activeGameObject.transform.root.GetChild(i).gameObject;

            //Check If Found GameObject Is The Armature
            if (obj2.name == "Armature")
            {
                //If So, Add It To The Object Reference For Later Use
                RootArmature = obj2;
                break;
            }
        }

        //If No Armature Was Found, Don't Continue
        if (RootArmature == null)
        {
            Debug.LogError("Armature == null!");

            return;
        }

        //Recursively Check Every GameObject In The Armature For DynamicBoneColliders
        Helpers.CheckTransform(RootArmature.transform);

        //Check If No DynamicBoneColliders Were Found - Prevents NullReferenceExceptions Due To Such.
        if (Helpers.DynamicBoneColliders == null)
        {
            Debug.LogError("Helpers.DynamicBoneColliders == null!");

            return;
        }

        Undo.RecordObject(RootArmature, "Before Collider List Setting");

        //Enumerate Selected GameObjects
        foreach (var CurrentGameObject in Selection.gameObjects)
        {
            //Ignore Empty GameObjects
            if (CurrentGameObject == null)
            {
                continue;
            }

            //Enumerate DynamicBones On GameObject
            foreach (DynamicBone DynBone in CurrentGameObject.GetComponentsInChildren<DynamicBone>(true))
            {
                //Ignore Invalid DynamicBone Components In GameObject
                if (DynBone == null)
                {
                    continue;
                }

                //Raise DynBonesFound By 1
                DynBonesFound++;

                //Set Colliders On DynamicBone
                DynBone.m_Colliders = Helpers.DynamicBoneColliders;
            }
        }

        //If No DynamicBones Were Found And Therefore Effected, Print To Console
        if (DynBonesFound == 0)
        {
            Debug.LogError("No Dynamic Bones Were Found So None Were Effected!");
        }
    }

    [MenuItem("GameObject/DynamicBone Extensions/Trim Colliders List", false, -10)]
    private static void Trim()
    {
        //Check If The User Has Even Got A GameObject Selected - Prevents NullReferenceExceptions Due To Such.
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogError("You Have No GameObjects Selected!");

            return;
        }

        //Define Variables
        int DynBonesFound = 0;

        //Enumerate Selected GameObjects
        foreach (var CurrentGameObject in Selection.gameObjects)
        {
            //Ignore Empty GameObjects
            if (CurrentGameObject == null)
            {
                continue;
            }

            //Enumerate DynamicBones On GameObject
            foreach (DynamicBone DynBone in CurrentGameObject.GetComponentsInChildren<DynamicBone>(true))
            {
                //Ignore Invalid DynamicBone Components In GameObject
                if (DynBone == null)
                {
                    continue;
                }

                //Raise DynBonesFound By 1
                DynBonesFound++;

                //Set Colliders On DynamicBone
                DynBone.m_Colliders = DynBone.m_Colliders.FindAll(o => o != null);
            }
        }

        //If No DynamicBones Were Found And Therefore Effected, Print To Console
        if (DynBonesFound == 0)
        {
            Debug.LogError("No Dynamic Bones Were Found So None Were Effected!");
        }
    }

    private static List<DynamicBoneColliderBase> SavedColliders;

    [MenuItem("GameObject/DynamicBone Extensions/Copy Colliders List", false, -10)]
    private static void Copy()
    {
        //Check If The User Has Even Got A GameObject Selected - Prevents NullReferenceExceptions Due To Such.
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogError("You Have No GameObjects Selected!");

            return;
        }

        //Define Variables
        int DynBonesFound = 0;

        //Ignore Empty GameObjects
        if (Selection.activeGameObject == null)
        {
            return;
        }

        //Enumerate DynamicBones On GameObject
        foreach (DynamicBone DynBone in Selection.activeGameObject.GetComponentsInChildren<DynamicBone>(true))
        {
            //Ignore Invalid DynamicBone Components In GameObject
            if (DynBone == null)
            {
                continue;
            }

            //Raise DynBonesFound By 1
            DynBonesFound++;

            //Set Colliders On DynamicBone
            SavedColliders = DynBone.m_Colliders;
        }

        //If No DynamicBones Were Found And Therefore Effected, Print To Console
        if (DynBonesFound == 0)
        {
            Debug.LogError("No Dynamic Bones Were Found So None Were Effected!");
        }
    }

    [MenuItem("GameObject/DynamicBone Extensions/Paste Colliders List", false, -10)]
    private static void Paste()
    {
        //Check If The User Has Even Got A GameObject Selected - Prevents NullReferenceExceptions Due To Such.
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogError("You Have No GameObjects Selected!");

            return;
        }

        //Define Variables
        int DynBonesFound = 0;

        //Enumerate Selected GameObjects
        foreach (var CurrentGameObject in Selection.gameObjects)
        {
            //Ignore Empty GameObjects
            if (CurrentGameObject == null)
            {
                continue;
            }

            //Enumerate DynamicBones On GameObject
            foreach (DynamicBone DynBone in CurrentGameObject.GetComponentsInChildren<DynamicBone>(true))
            {
                //Ignore Invalid DynamicBone Components In GameObject
                if (DynBone == null)
                {
                    continue;
                }

                //Raise DynBonesFound By 1
                DynBonesFound++;

                //Set Colliders On DynamicBone
                DynBone.m_Colliders = SavedColliders;
            }
        }

        //If No DynamicBones Were Found And Therefore Effected, Print To Console
        if (DynBonesFound == 0)
        {
            Debug.LogError("No Dynamic Bones Were Found So None Were Effected!");
        }
    }

    public class Helpers
    {
        //Define Object Reference
        public static List<DynamicBoneColliderBase> DynamicBoneColliders;

        //Init
        public static void CheckTransform(Transform transform)
        {
            //Reset Object Reference
            DynamicBoneColliders = new List<DynamicBoneColliderBase>();

            //Don't Act On A Invalid Transform
            if (transform == null)
            {
                return;
            }

            //Call Recursive Method
            GetChildren(transform);
        }

        //Recursive Method
        public static void GetChildren(Transform transform)
        {
            //If There Is A DynamicBoneCollider On This Transform
            if (transform.GetComponent<DynamicBoneCollider>())
            {
                //Add It To List For Use Later
                DynamicBoneColliders.Add(transform.GetComponent<DynamicBoneCollider>());
            }

            //Recursive Re-Call
            for (int i = 0; i < transform.childCount; i++)
            {
                GetChildren(transform.GetChild(i));
            }
        }
    }
}
#endif