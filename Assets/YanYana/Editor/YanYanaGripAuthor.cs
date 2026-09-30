// Editor-only sculpt and skin-weight correction for Yusuf's permanently carried cane.
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
    public static class YanYanaGripAuthor
    {
        [Serializable] public class Grip {public Vector3 forward,normal,handleCenter,scale;public int vertices;}
        public const string MeshPath=YanYanaCharacterVariants.Root+"/Yusuf_CaneSurface.asset";
        public const string GripPath="ArtDirection/YanYana/Characters/Wardrobe/Yusuf_CaneGrip.json";
        [MenuItem("Tools/Yan Yana/Art/Author Yusuf Cane Grip")]
        public static void Author()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before sculpting.");
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root+"/Yusuf.prefab"));SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
                var original=AssetDatabase.LoadAssetAtPath<Mesh>(YanYanaCharacterStyle.Root+"/Yusuf_Surface.asset");if(original.vertexCount!=29909)throw new InvalidOperationException("Review the hand regions for the changed approved mesh.");
                var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);int handIndex=Array.IndexOf(skin.bones,hand);
                var points=YanYanaEditableCharacterExport.EvaluateSurface(skin,original);
                var adjacency=new List<int>[original.vertexCount];for(int i=0;i<adjacency.Length;i++)adjacency[i]=new List<int>();var tris=original.triangles;
                for(int i=0;i<tris.Length;i+=3){int a=tris[i],b=tris[i+1],c=tris[i+2];adjacency[a].Add(b);adjacency[a].Add(c);adjacency[b].Add(a);adjacency[b].Add(c);adjacency[c].Add(a);adjacency[c].Add(b);}
                // Reviewed UV islands making up this approved right hand, including the thumb.
                var selected=new HashSet<int>();var pending=new Stack<int>(new[]{0,24,141,488,1101,1250,1664,29806});
                while(pending.Count>0){int index=pending.Pop();if(!selected.Add(index))continue;foreach(int next in adjacency[index])if(!selected.Contains(next))pending.Push(next);}
                var local=selected.Select(i=>hand.InverseTransformPoint(points[i])).ToArray();Vector3 mean=local.Aggregate(Vector3.zero,(a,b)=>a+b)/local.Length;
                var covariance=Matrix4x4.zero;covariance[3,3]=1;foreach(var point in local){var d=point-mean;for(int r=0;r<3;r++)for(int c=0;c<3;c++)covariance[r,c]+=d[r]*d[c]/local.Length;}for(int i=0;i<3;i++)covariance[i,i]+=1e-7f;
                var away=mean-hand.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position);var forward=away.normalized;
                for(int i=0;i<24;i++)forward=covariance.MultiplyVector(forward).normalized;if(Vector3.Dot(forward,away)<0)forward=-forward;
                var inverse=covariance.inverse;var normal=new Vector3(.27f,.43f,.71f);for(int i=0;i<24;i++)normal=inverse.MultiplyVector(normal).normalized;
                normal=Vector3.ProjectOnPlane(normal,forward).normalized;if(Vector3.Dot(hand.TransformDirection(normal),Vector3.left)<0)normal=-normal;
                float scale=Mathf.Abs(hand.lossyScale.x),radius=.021f/scale;var values=local.Select(v=>Vector3.Dot(v-mean,forward)).OrderBy(v=>v).ToArray();
                float begin=Mathf.Lerp(values.First(),values.Last(),.50f);var origin=mean+forward*begin;var side=Vector3.Cross(forward,normal).normalized;
                var mesh=UnityEngine.Object.Instantiate(original);mesh.name="Yusuf_CaneSurface";var vertices=mesh.vertices;var normals=mesh.normals;var weights=mesh.boneWeights;var bind=mesh.bindposes[handIndex];
                var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*original.bindposes[i]).ToArray();
                foreach(int i in selected)
                {
                    var point=hand.InverseTransformPoint(points[i]);float d=Vector3.Dot(point-origin,forward),angle=d>0?Mathf.Min(d/radius,2.6f):0;
                    var w=weights[i];var n=normals[i];var worldNormal=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;
                    var handNormal=hand.InverseTransformDirection(worldNormal);
                    if(d>0)
                    {
                        float thickness=Vector3.Dot(point-origin,normal),remainder=Mathf.Max(0,d-radius*2.6f);
                        float f=(radius-thickness)*Mathf.Sin(angle)+remainder*Mathf.Cos(angle);
                        float inward=radius-(radius-thickness)*Mathf.Cos(angle)+remainder*Mathf.Sin(angle);
                        point+=forward*(f-d)+normal*(inward-thickness);handNormal=Quaternion.AngleAxis(angle*Mathf.Rad2Deg,side)*handNormal;
                    }
                    vertices[i]=bind.inverse.MultiplyPoint3x4(point);normals[i]=bind.inverse.MultiplyVector(handNormal).normalized;weights[i]=new BoneWeight{boneIndex0=handIndex,weight0=1};
                }
                mesh.vertices=vertices;mesh.normals=normals;mesh.boneWeights=weights;mesh.RecalculateBounds();
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,MeshPath);
                File.WriteAllText(GripPath,JsonUtility.ToJson(new Grip{forward=forward,normal=normal,handleCenter=origin+normal*radius,scale=hand.lossyScale,vertices=selected.Count},true));AssetDatabase.SaveAssets();
                File.WriteAllText("ClientExports/YanYana/Reports/yusuf-grip-authoring.txt","Corrected "+selected.Count+" right-hand vertices to one coherent hand binding, then sculpted a 21 mm radius cane grip. Source mesh unchanged. Visual and motion checks required.\n");
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
