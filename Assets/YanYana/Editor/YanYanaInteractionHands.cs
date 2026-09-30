// Editor-only mesh authoring. Native blend-shape and arm nodes present these poses in play.
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
    public static class YanYanaInteractionHands
    {
        public const string Root="Assets/YanYana/Characters/Interaction";
        public const string DataRoot="ArtDirection/YanYana/Characters/Interaction";
        [Serializable] public class Pose {public string name;public Vector3 center;public float radius;}
        [Serializable] public class Hand {public bool right;public Vector3 forward,normal,palm;public int vertices;public int[] indices;public Pose[] poses;}
        [Serializable] public class Character {public string name;public Hand[] hands;}
        static readonly string[] Names={"Ada","Efe","Idil"};
        static readonly int[] Counts={30432,30765,4071};
        static readonly int[][] RightSeeds={new[]{0,677,826,473,66,166,330,940},new[]{259,69,593,1295,1344},new[]{15,3030,4042,30,72,279,1200,2992,66,3994,87,2159,3200,3389,639,2868}};
        static readonly int[][] LeftSeeds={new[]{28914,63,28712,28259,29191,29114,28997,28520,28607},new[]{29480,66,29302,29237,29560,29392,29683,29163,28460,26527},new[]{3342,3978,449,1455,1756,147,2392,3953,2676,2716,4058,2726,384,1739}};
        public static Character Read(string name)=>JsonUtility.FromJson<Character>(File.ReadAllText(DataRoot+"/"+name+"_Hands.json"));
        [MenuItem("Tools/Yan Yana/Art/Author Interaction Hands")]
        public static void Author()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before authoring mesh poses.");
            Directory.CreateDirectory(Root);Directory.CreateDirectory(DataRoot);AssetDatabase.Refresh();var scene=EditorSceneManager.NewPreviewScene();var report=new List<string>();
            try
            {
                for(int characterIndex=0;characterIndex<Names.Length;characterIndex++)
                {
                    string name=Names[characterIndex];var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root+"/"+name+".prefab"));SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(YanYanaCharacterStyle.Root+"/"+name+"_Surface.asset");if(original.vertexCount!=Counts[characterIndex])throw new InvalidOperationException("Review hand regions after a source mesh change: "+name);
                    var mesh=UnityEngine.Object.Instantiate(original);mesh.name=name+"_InteractionSurface";mesh.ClearBlendShapes();
                    var evaluated=YanYanaEditableCharacterExport.EvaluateSurface(skin,original);var vertices=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;var weights=mesh.boneWeights;if(tangents.Length!=vertices.Length)tangents=Enumerable.Repeat(new Vector4(1,0,0,1),vertices.Length).ToArray();
                    var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*original.bindposes[i]).ToArray();
                    var adjacency=new List<int>[vertices.Length];for(int i=0;i<vertices.Length;i++)adjacency[i]=new List<int>();var triangles=original.triangles;
                    for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];adjacency[a].Add(b);adjacency[a].Add(c);adjacency[b].Add(a);adjacency[b].Add(c);adjacency[c].Add(a);adjacency[c].Add(b);}
                    var hands=new List<Hand>();var shapes=new List<(string name,Vector3[] vertices,Vector3[] normals,Vector3[] tangents)>();
                    foreach(bool right in new[]{false,true})
                    {
                        var bone=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);int index=Array.IndexOf(skin.bones,bone),lowerIndex=Array.IndexOf(skin.bones,lower);var bind=mesh.bindposes[index];
                        var selected=new HashSet<int>();var pending=new Stack<int>(right?RightSeeds[characterIndex]:LeftSeeds[characterIndex]);while(pending.Count>0){int i=pending.Pop();if(!selected.Add(i))continue;foreach(int j in adjacency[i])if(!selected.Contains(j))pending.Push(j);}
                        // Efe's palm shares a UV island with his sleeve. Keep the sleeve's
                        // surface and skinning intact; include only the distal skin region.
                        if(name=="Efe"){var distal=(bone.position-lower.position).normalized;selected.RemoveWhere(i=>Vector3.Dot(evaluated[i]-bone.position,distal)<-.055f);}
                        var local=selected.Select(i=>bone.InverseTransformPoint(evaluated[i])).ToArray();var mean=local.Aggregate(Vector3.zero,(a,b)=>a+b)/local.Length;
                        var covariance=Matrix4x4.zero;covariance[3,3]=1;foreach(var point in local){var d=point-mean;for(int r=0;r<3;r++)for(int c=0;c<3;c++)covariance[r,c]+=d[r]*d[c]/local.Length;}for(int i=0;i<3;i++)covariance[i,i]+=1e-7f;
                        var away=mean-bone.InverseTransformPoint(lower.position);
                        var inverse=covariance.inverse;var normal=new Vector3(.27f,.43f,.71f);for(int i=0;i<24;i++)normal=inverse.MultiplyVector(normal).normalized;if(Vector3.Dot(bone.TransformDirection(normal),right?Vector3.left:Vector3.right)<0)normal=-normal;
                        // Finger direction follows the anatomical wrist-to-palm direction.
                        // The largest PCA axis can run across a spread child's fingers.
                        var forward=Vector3.ProjectOnPlane(away,normal).normalized;
                        var values=local.Select(v=>Vector3.Dot(v-mean,forward)).OrderBy(v=>v).ToArray();var origin=mean+forward*Mathf.Lerp(values.First(),values.Last(),.48f);float scale=Mathf.Abs(bone.lossyScale.x);
                        var handNormals=new Dictionary<int,Vector3>();var handTangents=new Dictionary<int,Vector3>();
                        foreach(int i in selected)
                        {
                            var w=weights[i];var n=normals[i];var world=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;
                            var t=tangents[i];var tv=new Vector3(t.x,t.y,t.z);var wt=(matrices[w.boneIndex0].MultiplyVector(tv)*w.weight0+matrices[w.boneIndex1].MultiplyVector(tv)*w.weight1+matrices[w.boneIndex2].MultiplyVector(tv)*w.weight2+matrices[w.boneIndex3].MultiplyVector(tv)*w.weight3).normalized;
                            var hn=bone.InverseTransformDirection(world);var ht=bone.InverseTransformDirection(wt);handNormals[i]=hn;handTangents[i]=ht;vertices[i]=bind.inverse.MultiplyPoint3x4(bone.InverseTransformPoint(evaluated[i]));normals[i]=bind.inverse.MultiplyVector(hn).normalized;var mt=bind.inverse.MultiplyVector(ht).normalized;tangents[i]=new Vector4(mt.x,mt.y,mt.z,t.w);weights[i]=new BoneWeight{boneIndex0=index,weight0=1};
                        }
                        var poses=new List<Pose>();
                        foreach(var shape in new[]{("Loop",.013f,2.65f),("Cylinder",name=="Idil"?.052f:.026f,2.4f),("Soft",.041f,1.45f),("Pinch",.009f,1.05f)})
                        {
                            float radius=shape.Item2/scale;var dv=new Vector3[vertices.Length];var dn=new Vector3[vertices.Length];var dt=new Vector3[vertices.Length];var side=Vector3.Cross(forward,normal).normalized;
                            foreach(int i in selected)
                            {
                                var point=bone.InverseTransformPoint(evaluated[i]);float d=Vector3.Dot(point-origin,forward);if(d<=0)continue;float angle=Mathf.Min(d/radius,shape.Item3),thickness=Vector3.Dot(point-origin,normal),remainder=Mathf.Max(0,d-radius*shape.Item3);
                                float f=(radius-thickness)*Mathf.Sin(angle)+remainder*Mathf.Cos(angle),inward=radius-(radius-thickness)*Mathf.Cos(angle)+remainder*Mathf.Sin(angle);
                                var posed=point+forward*(f-d)+normal*(inward-thickness);var curl=Quaternion.AngleAxis(angle*Mathf.Rad2Deg,side);dv[i]=bind.inverse.MultiplyPoint3x4(posed)-vertices[i];dn[i]=bind.inverse.MultiplyVector(curl*handNormals[i]).normalized-normals[i];var tangent=bind.inverse.MultiplyVector(curl*handTangents[i]).normalized;dt[i]=tangent-new Vector3(tangents[i].x,tangents[i].y,tangents[i].z);
                            }
                            string poseName=(right?"Right_":"Left_")+shape.Item1;shapes.Add((poseName,dv,dn,dt));poses.Add(new Pose{name=poseName,center=origin+normal*radius,radius=shape.Item2});
                        }
                        hands.Add(new Hand{right=right,forward=forward,normal=normal,palm=mean,vertices=selected.Count,indices=selected.ToArray(),poses=poses.ToArray()});report.Add(name+" "+(right?"right":"left")+" coherent hand vertices="+selected.Count);
                    }
                    // The source sleeves heavily follow the upper arm even below the elbow.
                    // Rebind that cloth in the same evaluated rest pose, with a smooth elbow
                    // transition, so the sleeve follows the forearm when the hand reaches.
                    var handSet=new HashSet<int>(hands.SelectMany(h=>h.indices));int repaired=0;
                    foreach(bool right in new[]{false,true})
                    {
                        if(name=="Efe")continue;
                        var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);int ui=Array.IndexOf(skin.bones,upper),li=Array.IndexOf(skin.bones,lower),si=Array.IndexOf(skin.bones,animator.GetBoneTransform(right?HumanBodyBones.RightShoulder:HumanBodyBones.LeftShoulder));var axis=(hand.position-lower.position).normalized;
                        for(int i=0;i<vertices.Length;i++)
                        {
                            if(handSet.Contains(i))continue;var w=weights[i];var entries=new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)};float arm=entries.Where(a=>a.Item1==ui||a.Item1==li).Sum(a=>a.Item2);
                            float lateral=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Abs(upper.position.x)*.6f,Mathf.Abs(upper.position.x)*.85f,Mathf.Abs(evaluated[i].x)));float distal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.02f,.06f,Vector3.Dot(evaluated[i]-upper.position,(lower.position-upper.position).normalized)));float shoulderTransfer=entries.Where(a=>a.Item1==si).Sum(a=>a.Item2)*lateral*distal;arm+=shoulderTransfer;if(arm<.40f)continue;
                            float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.035f,.045f,Vector3.Dot(evaluated[i]-lower.position,axis)));var influences=entries.Where(a=>a.Item1!=ui&&a.Item1!=li&&a.Item2>0).Select(a=>(a.Item1,a.Item2-(a.Item1==si?shoulderTransfer:0))).Concat(new[]{(ui,arm*(1-blend)),(li,arm*blend)}).Where(a=>a.Item2>0).OrderByDescending(a=>a.Item2).Take(4).ToArray();float total=influences.Sum(a=>a.Item2);var ids=new int[4];var values=new float[4];for(int j=0;j<influences.Length;j++){ids[j]=influences[j].Item1;values[j]=influences[j].Item2/total;}
                            var next=new BoneWeight{boneIndex0=ids[0],boneIndex1=ids[1],boneIndex2=ids[2],boneIndex3=ids[3],weight0=values[0],weight1=values[1],weight2=values[2],weight3=values[3]};var before=BlendMatrix(matrices,w);var after=BlendMatrix(matrices,next);var inverse=after.inverse;var worldNormal=before.MultiplyVector(normals[i]).normalized;var t=tangents[i];var worldTangent=before.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;vertices[i]=inverse.MultiplyPoint3x4(evaluated[i]);normals[i]=inverse.MultiplyVector(worldNormal).normalized;var tangent=inverse.MultiplyVector(worldTangent).normalized;tangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,t.w);weights[i]=next;repaired++;
                        }
                    }
                    report.Add(name+" sleeve vertices rebound around the elbow="+repaired);
                    mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.boneWeights=weights;foreach(var shape in shapes)mesh.AddBlendShapeFrame(shape.name,100,shape.vertices,shape.normals,shape.tangents);mesh.RecalculateBounds();
                    if(name=="Idil")
                    {
                        var handIndices=new HashSet<int>(hands.SelectMany(h=>h.indices));var bodyTriangles=new List<int>();var gloveTriangles=new List<int>();
                        for(int i=0;i<triangles.Length;i+=3){var list=handIndices.Contains(triangles[i])&&handIndices.Contains(triangles[i+1])&&handIndices.Contains(triangles[i+2])?gloveTriangles:bodyTriangles;list.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
                        mesh.subMeshCount=2;mesh.SetTriangles(bodyTriangles,0);mesh.SetTriangles(gloveTriangles,1);string glovePath=Root+"/Idil_ProtectiveGloves.mat";var glove=AssetDatabase.LoadAssetAtPath<Material>(glovePath);
                        if(!glove){glove=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(glove,glovePath);}glove.SetColor("_BaseColor",new Color(.16f,.20f,.20f));glove.SetFloat("_Smoothness",.16f);EditorUtility.SetDirty(glove);skin.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(YanYanaCharacterStyle.Root+"/Idil_ClothesAndFace_0.mat"),glove};
                    }
                    string path=Root+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
                    skin.sharedMesh=saved;for(int i=0;i<saved.blendShapeCount;i++)skin.SetBlendShapeWeight(i,0);var neutral=YanYanaEditableCharacterExport.EvaluateSurface(skin);report.Add(name+" neutral maximum displacement metres="+neutral.Select((v,i)=>Vector3.Distance(v,evaluated[i])).Max());PrefabUtility.SaveAsPrefabAsset(actor,YanYanaCharacterStyle.Root+"/"+name+".prefab");File.WriteAllText(DataRoot+"/"+name+"_Hands.json",JsonUtility.ToJson(new Character{name=name,hands=hands.ToArray()},true));UnityEngine.Object.DestroyImmediate(actor);
                }
                AssetDatabase.SaveAssets();File.WriteAllLines("ClientExports/YanYana/Reports/interaction-hand-authoring.txt",report);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        static Matrix4x4 BlendMatrix(Matrix4x4[] matrices,BoneWeight w)
        {
            var result=Matrix4x4.zero;for(int i=0;i<16;i++)result[i]=matrices[w.boneIndex0][i]*w.weight0+matrices[w.boneIndex1][i]*w.weight1+matrices[w.boneIndex2][i]*w.weight2+matrices[w.boneIndex3][i]*w.weight3;return result;
        }
    }
}
