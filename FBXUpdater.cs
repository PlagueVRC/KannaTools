#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

using static UnityEngine.EventSystems.EventTrigger;

[CustomEditor(typeof(FBXUpdater))]
public class FBXUpdaterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Label("");
        ((FBXUpdater)target).UpdatingTo = EditorGUILayout.ObjectField("Parent Object Of New Ver:", ((FBXUpdater)target).UpdatingTo, typeof(GameObject), true) as GameObject;

        GUILayout.Label("Note: This Is To Be Added To The OLD PARENT GameObject Of Your Model.");

        if (GUILayout.Button("Update To New FBX!"))
        {
            ((FBXUpdater)target).UpdateFBX();
        }
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(VRCAvatarDescriptor))]
public class FBXUpdater : MonoBehaviour
{
    public GameObject UpdatingTo;

    private static string GetPath(Transform obj, bool MakeRelative = false)
    {
        var text = "/" + obj.name;
        while (obj.parent != null)
        {
            obj = obj.parent.transform;
            text = "/" + obj.name + text;
        }

        text = text.Substring(1); // Remove / At Start?

        if (MakeRelative)
        {
            text = text.Substring(text.IndexOf("/") + 1);
        }

        return text;
    }

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

        var skinnedMeshRenderersOld = ToComponentList<SkinnedMeshRenderer>(AllComponentsOld).Select(Old => (Old, ToComponentList<SkinnedMeshRenderer>(AllComponentsNew).FirstOrDefault(p => (p.sharedMesh.name == Old.sharedMesh.name && !AssetDatabase.GetAssetPath(p).Contains("unity default resources")) || p.name == Old.name))).Where(o => o.Old != null && o.Item2 != null).ToList();

        Debug.Log($"Got {skinnedMeshRenderersOld.Count} SkinnedMeshRenderers With Matching Mesh Names & SubMesh Counts!");
        
        for (var index = 0; index < skinnedMeshRenderersOld.Count; index++)
        {
            var entry = skinnedMeshRenderersOld[index];

            // Copy Shape Key Weights
            for (var i = 0; i < entry.Old.sharedMesh.blendShapeCount; i++)
            {
                var NameForID = entry.Old.sharedMesh.GetBlendShapeName(i);

                var IDOfNew = -1;

                for (var i2 = 0; i2 < entry.Item2.sharedMesh.blendShapeCount; i2++)
                {
                    var NewNameForID = entry.Item2.sharedMesh.GetBlendShapeName(i2);

                    if (NewNameForID == NameForID)
                    {
                        IDOfNew = i2;
                        break;
                    }
                }

                if (IDOfNew != -1)
                {
                    entry.Item2.SetBlendShapeWeight(IDOfNew, entry.Old.GetBlendShapeWeight(i));
                }
            }

            var AnchorPath = GetPath(entry.Old.probeAnchor, true);

            entry.Item2.probeAnchor = UpdatingTo.transform.Find(AnchorPath);
            
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
                        PathToCreate = CurrentObject.name + "/" + PathToCreate;
                    }

                    CurrentObject = CurrentObject.parent;
                }

                Debug.Log($"Path To Create: {PathToCreate} - This Always Should Show Above A Non Object ForEach Set Inside");

                if (FindOrNull(UpdatingTo.transform, PathToCreate) == null)
                {
                    GameObject CurrentDupedObj = null;
                    foreach (var ObjName in PathToCreate.Split('/')) // Left to right, begins at rootmost
                    {
                        if (string.IsNullOrWhiteSpace(ObjName))
                        {
                            break;
                        }

                        var NewDupe = UpdatingTo.transform.Find(ObjName)?.gameObject ?? new GameObject(ObjName);

                        Debug.Log($"[ForEach] Created Object {NewDupe.name}! - [D]: Path: {PathToCreate}");

                        if (CurrentDupedObj != null)
                        {
                            NewDupe.transform.SetParent(CurrentDupedObj.transform);
                            Debug.Log($"[ForEach] Set Object {NewDupe.name} Inside {CurrentDupedObj.name}! - [D]: Path: {PathToCreate}");
                        }

                        CurrentDupedObj = NewDupe;
                    }

                    if (CurrentDupedObj != null)
                    {
                        entry.Item2.transform.SetParent(CurrentDupedObj.transform);
                        Debug.Log($"[ForEach] Set {entry.Item2.transform.name} Inside {CurrentDupedObj.name}! - [D]: Path: {PathToCreate}");

                        CurrentDupedObj.transform.root.SetParent(UpdatingTo.transform);
                        Debug.Log($"Set Path Inside Root!");
                    }

                    Debug.LogWarning($"Created Path As It Never Existed!");
                }
                else
                {
                    Debug.Log($"Path To Create Found!");
                    entry.Item2.transform.SetParent(FindOrNull(UpdatingTo.transform, PathToCreate));
                }
            }

            entry.Item2.sharedMaterials = CopyMaterials(entry.Old.sharedMaterials, entry.Item2.sharedMaterials);
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

            NewRenderer.sharedMaterials = CopyMaterials(OldRenderer.sharedMaterials, NewRenderer.sharedMaterials);
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

        //var AllNonMatchingObjects = AllOldTransforms.Where(u => u?.parent != null && u?.parent?.name != "Armature" && u?.name != "Armature" && u.parent != u.root && AllNewTransforms.Where(o => o?.parent != null && o?.parent?.name != "Armature" && o?.name != "Armature" && o.parent != o.root).All(o => (o.parent.name.Replace(".", "_") + "/" + o.name.Replace(".", "_")) != (u.parent.name.Replace(".", "_") + "/" + u.name.Replace(".", "_")))).OrderBy(i => i.GetParentCount());

        //var PrevCopied = new List<GameObject>();

        //foreach (var entry in AllNonMatchingObjects)
        //{
        //    if (PrevCopied.Any(o => o.transform.IsChildOf(o.transform)))
        //    {
        //        continue;
        //    }

        //    var path = GetPath(entry.parent, true);

        //    var ToPath = path.Replace(".", "_");

        //    Debug.Log($"ToPath: {ToPath}");

        //    if (string.IsNullOrEmpty(ToPath))
        //    {
        //        Debug.LogError("Empty Path!");
        //        continue;
        //    }

        //    var ToPathObj = UpdatingTo.transform.Find(ToPath);

        //    if (ToPathObj == null)
        //    {
        //        Debug.LogError("ToPathObj == null!");
        //        continue;
        //    }

        //    var copy = Object.Instantiate(entry.gameObject);

        //    copy.transform.SetParent(ToPathObj);

        //    PrevCopied.Add(entry.gameObject);
        //}

        #region Organize Transforms And Match Scales

        var AllMatchingObjects = AllOldTransforms.Where(i => i != null && i.parent != null).Select(Old => (Old, AllNewTransforms.Where(u => u.parent != null).FirstOrDefault(o => (Old.parent.name + "/" + Old.name) == (o.parent.name + "/" + o.name)))).Where(p => p.Item2 != null);

        foreach (var entry in AllMatchingObjects)
        {
            entry.Item2.SetSiblingIndex(entry.Old.GetSiblingIndex());

            entry.Item2.localScale = entry.Old.localScale;
        }

        UpdatingTo.transform.localScale = transform.localScale;

        #endregion
    }

    private Material[] CopyMaterials(Material[] From, Material[] To)
    {
        if (To == null && From != null)
        {
            To = From;
            return To;
        }

        if (From == null)
        {
            return To;
        }

        if (From.Length == To.Length)
        {
            To = From;
        }
        else
        {
            var MatchingMaterialsOnEntry = From.Where(z => z != null).Select(Old => (Old, To.FirstOrDefault(p =>  p?.name != null && (p.name.Replace(" (Instance)", "").Contains(Old.name.Replace(" (Instance)", "")) || (p.mainTexture != null && Old.mainTexture != null && p.mainTexture.name.Replace(" (Instance)", "").Contains(Old.mainTexture.name.Replace(" (Instance)", ""))))))).Where(o => o.Old != null && o.Item2 != null).ToList();

            for (var i = 0; i < MatchingMaterialsOnEntry.Count; i++)
            {
                var matching = MatchingMaterialsOnEntry[i];

                //Debug.Log($"Updating Material: {matching.Item2.name.Replace(" (Instance)", "")} On Mesh Object: {entry.Item2.name} To: {matching.Old.name.Replace(" (Instance)", "")}!");

                if (matching.Item2 != null)
                {
                    var IndexToUpdate = To.ToList().FindIndex(o => o.name == matching.Item2.name);

                    To[IndexToUpdate] = matching.Old;
                }
            }
        }

        return To;
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

    public static List<T> ToComponentList<T>(IEnumerable<Component> list) where T : Component
    {
        return OfILCastedType<T>(list);
    }

    public static List<T> OfILCastedType<T>(IEnumerable<Component> source) where T : Component
    {
        return OfTypeIterator<T>(source).ToList();
    }

    private static IEnumerable<T> OfTypeIterator<T>(IEnumerable<Component> source) where T : Component
    {
        foreach (var obj in source)
        {
            if (obj != null && obj is T result)
            {
                yield return result;
            }
        }
    }
}
#endif
