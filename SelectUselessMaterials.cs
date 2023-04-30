#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectUselessMaterials
{
    [MenuItem("Assets/Kanna's Tools/Select Useless Materials", false, 10)]
    private static void SelectUselessMaterialsF(MenuCommand selected)
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);

        var AllUsedMaterials = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(p => p.GetComponentsInChildren<Renderer>(true)).SelectMany(o => o.sharedMaterials).Where(u => u != null);

        var Assets = AssetDatabase.FindAssets("t:Material", new [] { path }).Select(AssetDatabase.GUIDToAssetPath).Where(a => a != null && Path.GetDirectoryName(a).Replace("\\", "/") == path).ToArray();

        if (Assets.Length > 0)
        {
            Debug.Log($"{Path.GetDirectoryName(Assets[0]).Replace("\\", "/")} == {path}: {Path.GetDirectoryName(Assets[0]).Replace("\\", "/") == path}");
        }

        Debug.Log($"{Assets.Length} Materials Found");

        var UnUsed = Assets.Select(AssetDatabase.LoadAssetAtPath<Material>).Where(i => AllUsedMaterials.All(p => p.name != i.name)).ToArray();

        Debug.Log($"{UnUsed.Length} Un-Used Materials");

        Selection.objects = UnUsed;
    }

    // Thx Dread
    public static List<string> GetAssetPathsInFolder(string path, bool deep = true)
    {
        var fileEntries = Directory.GetFiles(path);
        var subDirectories = deep ? Directory.GetDirectories(path) : null;

        var list =
            (from fileName in fileEntries
             where !fileName.EndsWith(".meta")
             select fileName.Replace('\\', '/')).ToList();


        if (deep)
            foreach (var sd in subDirectories)
            {
                list.AddRange(GetAssetPathsInFolder(sd));
            }


        return list;
    }
}
#endif
