// Editor-only wardrobe authoring. All derived meshes/materials/prefabs belong to Yan Yana.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YanYana.Editor
{
    public static class YanYanaCharacterVariants
    {
        [Serializable] public class MaskSource { public Vector2[] uv; public Color[] colors; public int[] triangles; }
        public const string Root="Assets/YanYana/Characters/Neighbors";
        public static readonly string[] Names={"Selma","Mert","Gul","Deniz","Asli","Ozan"};
        static readonly string[] Bases={"Derya","Emre","Derya","Emre","Derya","Emre"};
        static readonly string[] Colors={"#4E8280","#B78147","#738F68","#607F93","#CB7865","#6C7166"};

        [MenuItem("Tools/Yan Yana/Art/Prepare Yusuf And Six Neighbors")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before authoring.");
            AssetDatabase.Refresh();Directory.CreateDirectory(Root);
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                for(int i=-1;i<Names.Length;i++)
                {
                    bool elder=i<0;string name=elder?"Yusuf":Names[i],source=elder?"Yusuf":Bases[i];
                    var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root+"/"+source+".prefab"));
                    SceneManager.MoveGameObjectToScene(actor,scene);actor.name=name;actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    // Idempotent: regenerate only our own named wardrobe children.
                    foreach(var t in actor.GetComponentsInChildren<Transform>().Where(x=>x.name.StartsWith("YYWardrobe_")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                    var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
                    var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
                    var baseMesh=AssetDatabase.LoadAssetAtPath<Mesh>(YanYanaCharacterStyle.Root+"/"+source+"_Surface.asset");
                    var points=YanYanaEditableCharacterExport.EvaluateSurface(skin,baseMesh);float sole=points.Min(v=>v.y);
                    float height=points.Max(v=>v.y)-sole;float center=animator.GetBoneTransform(HumanBodyBones.Head).position.x;
                    var skinWeights=baseMesh.boneWeights;
                    var regions=points.Select((v,vertexIndex)=>
                    {
                        float y=(v.y-sole)/height;
                        float body=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.39f,y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.73f,.76f,y)));
                        var weight=skinWeights[vertexIndex];float headWeight=0;
                        foreach(var entry in new[]{(weight.boneIndex0,weight.weight0),(weight.boneIndex1,weight.weight1),(weight.boneIndex2,weight.weight2),(weight.boneIndex3,weight.weight3)})
                            if(skin.bones[entry.Item1].name=="Head")headWeight+=entry.Item2;
                        body*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.5f,headWeight));
                        float head=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.77f,.82f,y))*Mathf.Max(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.83f,.86f,y)),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.135f,Mathf.Abs(v.x-center))));
                        return new Color(body,head,0,1);
                    }).ToArray();
                    Directory.CreateDirectory("ArtDirection/YanYana/Characters/Wardrobe");
                    File.WriteAllText("ArtDirection/YanYana/Characters/Wardrobe/"+source+".mask.json",JsonUtility.ToJson(new MaskSource{uv=baseMesh.uv,colors=regions,triangles=baseMesh.triangles}));
                    // Share the approved surface. New wardrobe detail is in original models
                    // and Blender-baked UV masks; no duplicate mesh/extra GPU skin stream.
                    skin.sharedMesh=elder?(AssetDatabase.LoadAssetAtPath<Mesh>(YanYanaGripAuthor.MeshPath)??baseMesh):baseMesh;
                    var baseMat=AssetDatabase.LoadAssetAtPath<Material>(YanYanaCharacterStyle.Root+"/"+source+"_ClothesAndFace_0.mat");
                    var clothes=new Material(baseMat);clothes.name=name+"_Clothing";clothes.shader=Shader.Find("YanYana/Approved Wardrobe");
                    ColorUtility.TryParseHtmlString(elder?"#7E8771":Colors[i],out var clothColor);clothes.SetColor("_WardrobeColor",clothColor);
                    clothes.SetColor("_GreyColor",new Color(.66f,.66f,.63f));clothes.SetFloat("_WardrobeMode",source=="Derya"?1:0);clothes.SetFloat("_GreyAmount",elder?1:0);
                    string regionPath="Assets/YanYana/Art/Textures/"+source+"_WardrobeRegions.png";
                    var regionImporter=AssetImporter.GetAtPath(regionPath) as TextureImporter;
                    if(regionImporter&&(regionImporter.sRGBTexture||regionImporter.mipmapEnabled||regionImporter.maxTextureSize!=512)){regionImporter.sRGBTexture=false;regionImporter.mipmapEnabled=false;regionImporter.maxTextureSize=512;regionImporter.textureCompression=TextureImporterCompression.Uncompressed;regionImporter.SaveAndReimport();}
                    clothes.SetTexture("_WardrobeRegions",AssetDatabase.LoadAssetAtPath<Texture2D>(regionPath));
                    skin.sharedMaterials=new[]{Save(clothes,Root+"/"+name+"_Clothing.mat")};
                    if(elder)
                    {
                        Attach(actor,"YusufRoundGlasses",HumanBodyBones.Head,new Vector3(0,.196f,.215f));
                        Attach(actor,"YusufMoustache",HumanBodyBones.Head,new Vector3(0,.125f,.235f));
                        Attach(actor,"CardiganButtons",HumanBodyBones.Chest,new Vector3(0,.01f,.17f));
                    }
                    else if(i==0)Attach(actor,"NeighborCoralScarf",HumanBodyBones.Head,new Vector3(0,.005f,.15f));
                    else if(i==1)Attach(actor,"NeighborPocket",HumanBodyBones.Chest,new Vector3(-.10f,.12f,.19f));
                    else if(i==2)Attach(actor,"NeighborSunHat",HumanBodyBones.Head,new Vector3(0,.445f,.015f));
                    else if(i==3)Attach(actor,"NeighborCrossbodyBag",HumanBodyBones.Hips,new Vector3(.19f,.04f,.12f));
                    else if(i==4)Attach(actor,"NeighborHairBand",HumanBodyBones.Head,new Vector3(0,.29f,.16f));
                    else Attach(actor,"YusufRoundGlasses",HumanBodyBones.Head,new Vector3(0,.206f,.226f));
                    PrefabUtility.SaveAsPrefabAsset(actor,(elder?YanYanaCharacterStyle.Root:Root)+"/"+name+".prefab");
                    UnityEngine.Object.DestroyImmediate(actor);
                }
                AssetDatabase.SaveAssets();
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            RenderNeighbors();YanYanaCharacterStyle.RenderReview();
        }
        static T Save<T>(T created,string path) where T:UnityEngine.Object
        {var saved=AssetDatabase.LoadAssetAtPath<T>(path);if(saved){EditorUtility.CopySerialized(created,saved);UnityEngine.Object.DestroyImmediate(created);return saved;}AssetDatabase.CreateAsset(created,path);return created;}
        static Material Solid(string name,string hex)
        {ColorUtility.TryParseHtmlString(hex,out var color);var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=name;mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.22f);return Save(mat,Root+"/"+name+".mat");}
        static void Attach(GameObject actor,string model,HumanBodyBones bone,Vector3 offset)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/"+model+".fbx");
            if(!prefab)throw new FileNotFoundException(model);
            var animator=actor.GetComponentInChildren<Animator>();
            var anchor=animator.GetBoneTransform(bone)??animator.GetComponentsInChildren<Transform>().FirstOrDefault(x=>x.name==bone.ToString());
            if(!anchor)throw new InvalidOperationException("Missing attachment bone "+bone+" on "+actor.name);
            var child=UnityEngine.Object.Instantiate(prefab);child.name="YYWardrobe_"+model;SceneManager.MoveGameObjectToScene(child,actor.scene);
            child.transform.SetPositionAndRotation(anchor.position+offset,Quaternion.identity);
            child.transform.SetParent(anchor,true);
            foreach(var renderer in child.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/YanYana/Art/Materials/"+m.name+".mat")??m).ToArray();
            }
        }
        [MenuItem("Tools/Yan Yana/Art/Render Six Neighbors")]
        public static void RenderNeighbors()=>RenderNeighbors(false);
        [MenuItem("Tools/Yan Yana/Art/Inspect Wardrobe Masks")]
        public static void InspectMasks()=>RenderNeighbors(true);
        static void RenderNeighbors(bool masks)
        {
            var scene=EditorSceneManager.NewPreviewScene();Directory.CreateDirectory("ClientExports/YanYana/ArtReview");
            try
            {
                var go=new GameObject("Wardrobe preview");SceneManager.MoveGameObjectToScene(go,scene);
                var camera=go.AddComponent<Camera>();camera.cameraType=CameraType.Preview;camera.scene=scene;camera.orthographic=true;camera.orthographicSize=1.26f;camera.aspect=2.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.85f,.88f,.83f);
                go.transform.position=new Vector3(0,1.7f,-8);go.transform.LookAt(new Vector3(0,.85f,0));
                foreach(var spec in new[]{new Vector3(35,-25,0),new Vector3(20,150,0)})
                {var lg=new GameObject("Wardrobe light");SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=spec.x==35?1.15f:.65f;lg.transform.rotation=Quaternion.Euler(spec);}
                var report=new List<string>();
                for(int i=0;i<Names.Length;i++)
                {
                    var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+Names[i]+".prefab"));SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(new Vector3((i-2.5f)*1.0f,0,0),Quaternion.Euler(0,180,0));var anim=actor.GetComponentInChildren<Animator>();anim.Rebind();anim.Update(0);
                    if(masks)
                    {
                        var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mat=new Material(skin.sharedMaterial);mat.SetFloat("_MaskDebug",1);skin.sharedMaterial=mat;
                        var uv=new List<Vector4>();skin.sharedMesh.GetUVs(3,uv);report.Add(actor.name+" "+AssetDatabase.GetAssetPath(skin.sharedMesh)+" uv4="+uv.Count+" first="+uv.FirstOrDefault());
                        if(i>=3){var baked=new Mesh();skin.BakeMesh(baked,true);skin.gameObject.AddComponent<MeshFilter>().sharedMesh=baked;skin.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;skin.enabled=false;report.Add("STATIC baked uv4="+baked.uv4.Length);}
                    }
                }
                var target=new RenderTexture(1960,700,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1960,700,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1960,700),0,0);image.Apply();File.WriteAllBytes("ClientExports/YanYana/ArtReview/"+(masks?"WardrobeMasks":"SixNeighbors")+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
                if(masks)File.WriteAllLines("ClientExports/YanYana/Reports/wardrobe-mask-probe.txt",report);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
