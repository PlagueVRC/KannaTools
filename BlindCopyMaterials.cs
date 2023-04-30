#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

public class BlindCopyMaterials : Editor
{
    private static Material[] Materials;

    [MenuItem("GameObject/Kanna's Tools/Materials/Copy Materials", false, 10)]
    private static void Copy()
    {
        if (Selection.activeGameObject?.GetComponent<Renderer>() is var renderer && renderer != null)
        {
            Materials = renderer.sharedMaterials;
        }
    }

    [MenuItem("GameObject/Kanna's Tools/Materials/Paste Materials", false, 10)]
    private static void Paste()
    {
        if (Selection.activeGameObject?.GetComponent<Renderer>() is var renderer && renderer != null)
        {
            var TempArr = renderer.sharedMaterials;

            for (var i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if ((Materials.Length - 1) >= i)
                {
                    TempArr[i] = Materials[i];
                }
            }

            renderer.sharedMaterials = TempArr;
        }
    }
}

#endif
