#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;

using UnityEngine;

using static UnityEngine.EventSystems.EventTrigger;

public class BlindCopyTransforms : Editor
{
    private static List<Transform> AllTransforms = new List<Transform>();

    [MenuItem("GameObject/Kanna's Tools/Transforms/Copy All", false, 10)]
    private static void Copy()
    {
        if (Selection.activeGameObject != null)
        {
            AllTransforms = Selection.activeGameObject.GetComponentsInChildren<Transform>(true).Where(o => o != Selection.activeGameObject).OrderBy(GetParentCount).ToList();
        }
    }

    [MenuItem("GameObject/Kanna's Tools/Transforms/Paste Un-Found", false, 10)]
    private static void Paste()
    {
        if (Selection.activeGameObject != null)
        {
            foreach (var transform in AllTransforms)
            {
                var FullPath = GetPath(transform);

                if (!Selection.activeGameObject.transform.Find(FullPath))
                {
                    transform.SetParent(transform.parent != null ? Selection.activeGameObject.transform.Find(GetPath(transform.parent)) : Selection.activeGameObject.transform);
                }
            }
        }
    }

    private static string GetPath(Transform transform)
    {
        var PathToCreate = "";
        var CurrentObject = transform;

        while (CurrentObject != transform.root) // Create Path String - Loop
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
                PathToCreate = CurrentObject.name + "/" + PathToCreate;
            }

            CurrentObject = CurrentObject.parent;
        }

        return PathToCreate;
    }

    private static int GetParentCount(Transform transform)
    {
        var count = 0;

        var CurrentObject = transform;

        while (CurrentObject != transform.root)
        {
            count++;
            CurrentObject = CurrentObject.parent;
        }

        return count;
    }
}

#endif
