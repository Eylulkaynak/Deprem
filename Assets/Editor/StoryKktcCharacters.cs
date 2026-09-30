using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Offline mesh refinement, adapted to the original skeleton and face bindings.</summary>
public static class StoryKktcCharacters
{
    const string Root = StoryKktcArtLibrary.Root + "/Characters";
    static readonly string[] Roles = { "Deniz", "Can", "Anne", "Baba", "Komsu", "Police", "Firefighter", "RescueWorker" };

    [MenuItem("Tools/Deprem Story/KKTC/4 Prepare Refined Family")]
    public static void Prepare()
    {
        var report = new List<string>();
        foreach (string role in Roles)
        {
            string originalRoot=Array.IndexOf(Roles,role)<5?MeshyFamilyCharacterImporter.Root:"Assets/Story/Characters/MeshyResponders";
            string path = Root + "/" + role + "_Rigged.fbx";
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if(importer == null) throw new InvalidOperationException("Missing Blender export " + path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true;
            importer.SaveAndReimport();
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if(avatar == null || !avatar.isHuman || !avatar.isValid)
                throw new InvalidOperationException(role + ": refined Humanoid import is invalid");
            GameObject old = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(originalRoot+"/Prefabs/"+role+".prefab"));
            GameObject refined = Object.Instantiate(source);
            try
            {
                // Match the existing corrected Visual's world frame. All scene skeletons,
                // controller references, facial overlays and attachments stay intact.
                Transform originalVisual = old.transform.Find("Visual");
                refined.transform.SetPositionAndRotation(originalVisual.position, originalVisual.rotation);
                refined.transform.localScale = originalVisual.lossyScale;
                var targets = old.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var sources = refined.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(targets.Length != sources.Length) throw new InvalidOperationException(role+": mesh count changed");
                Material material = new Material(AssetDatabase.LoadAssetAtPath<Material>(originalRoot+"/"+role+"/"+role+"_URP.mat"));
                material.name = role+"_KKTC";
                material.SetFloat("_BumpScale", .28f);
                material.SetFloat("_Smoothness", .27f);
                string materialPath=Root+"/"+role+".mat";
                Material savedMaterial=Save(material, materialPath);
                for(int i=0;i<targets.Length;i++)
                {
                    var target=targets[i]; var from=sources[i];
                    Mesh mesh=Object.Instantiate(from.sharedMesh);
                    Matrix4x4 convert=target.transform.worldToLocalMatrix * from.transform.localToWorldMatrix;
                    mesh.vertices=mesh.vertices.Select(convert.MultiplyPoint3x4).ToArray();
                    mesh.normals=mesh.normals.Select(n=>convert.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                    var boneMap = from.bones.Select(b=>Array.FindIndex(target.bones,t=>t.name==b.name)).ToArray();
                    if(boneMap.Any(b=>b<0)) throw new InvalidOperationException(role+": bone identity changed");
                    var weights=mesh.boneWeights;
                    for(int w=0;w<weights.Length;w++)
                    {
                        var v=weights[w];
                        v.boneIndex0=boneMap[v.boneIndex0]; v.boneIndex1=boneMap[v.boneIndex1];
                        v.boneIndex2=boneMap[v.boneIndex2]; v.boneIndex3=boneMap[v.boneIndex3];
                        weights[w]=v;
                    }
                    mesh.boneWeights=weights;
                    mesh.bindposes=target.sharedMesh.bindposes;
                    Mesh stitched=MeshyFamilyCharacterImporter.BuildSeamFixedMesh(mesh,out _);
                    if(stitched!=null){Object.DestroyImmediate(mesh);mesh=stitched;}
                    mesh.RecalculateBounds(); mesh.RecalculateTangents();
                    Bounds before=target.sharedMesh.bounds;
                    float tolerance = Mathf.Max(.02f, before.size.magnitude * .004f);
                    if(Vector3.Distance(before.center,mesh.bounds.center)>tolerance || Vector3.Distance(before.size,mesh.bounds.size)>tolerance)
                        throw new InvalidOperationException(role+": refined mesh no longer matches original frame: "+before+" -> "+mesh.bounds);
                    mesh.name=role+"_Refined_"+i;
                    target.sharedMesh=Save(mesh,Root+"/"+mesh.name+".asset");
                    target.sharedMaterials=Enumerable.Repeat(savedMaterial,target.sharedMaterials.Length).ToArray();
                    report.Add(role+" mesh="+i+" vertices="+target.sharedMesh.vertexCount+" bones="+target.bones.Length+" bounds="+target.sharedMesh.bounds+" humanoid=valid; original skeleton retained");
                }
                old.name=role;
                PrefabUtility.SaveAsPrefabAsset(old,Root+"/"+role+".prefab");
            }
            finally { Object.DestroyImmediate(old); Object.DestroyImmediate(refined); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("ClientExports/KKTC/Reports/FamilyRefinement.txt",report);
        Debug.Log("KKTC_FAMILY_READY");
    }

    static T Save<T>(T value,string path) where T:Object
    {
        T existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing==null) { AssetDatabase.CreateAsset(value,path); return value; }
        EditorUtility.CopySerialized(value,existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(value); return existing;
    }

    public static void Apply(Scene scene)
    {
        foreach(Transform person in StoryKktcArtLibrary.Transforms(scene).Where(t=>t.Find("Visual")!=null))
        {
            string marker=person.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith("CharacterSource_"))?.name;
            string role=marker?.Replace("CharacterSource_","");
            if(!Roles.Contains(role)) continue;
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+role+".prefab");
            if(prefab==null) continue;
            var sources=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var targets=person.Find("Visual").GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(sources.Length!=targets.Length) throw new InvalidOperationException(person.name+": skeleton renderers changed");
            for(int i=0;i<targets.Length;i++)
            {
                targets[i].sharedMesh=sources[i].sharedMesh;
                targets[i].sharedMaterials=sources[i].sharedMaterials;
            }
        }
    }
}
