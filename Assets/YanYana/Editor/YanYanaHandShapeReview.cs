// Native 3D source-pose renders; no play state or production scene is changed.
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
    public static class YanYanaHandShapeReview
    {
        [MenuItem("Tools/Yan Yana/Art/Render Hand Shapes")]
        public static void Render()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before source review.");
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var lightGo=new GameObject("Source review light");SceneManager.MoveGameObjectToScene(lightGo,scene);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-20,0);
                var cameraGo=new GameObject("Source review camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.AddComponent<Camera>();camera.cameraType=CameraType.Preview;camera.scene=scene;camera.aspect=1;camera.nearClipPlane=.01f;camera.farClipPlane=3;camera.orthographic=true;camera.orthographicSize=.13f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.86f,.89f,.85f);
                foreach(string name in new[]{"Ada","Efe","Idil"})
                {
                    var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root+"/"+name+".prefab"));SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
                    var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();skin.enabled=false;var source=skin.sharedMesh;var data=YanYanaInteractionHands.Read(name);var sheet=new Texture2D(1800,720,TextureFormat.RGB24,false);
                    for(int row=0;row<2;row++)for(int column=0;column<5;column++)
                    {
                        var h=data.hands[row];var bone=animator.GetBoneTransform(h.right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);int bi=Array.IndexOf(skin.bones,bone);var matrix=bone.localToWorldMatrix*source.bindposes[bi];var keep=new HashSet<int>(h.indices);var vertices=source.vertices;var normals=source.normals;
                        if(column>0){var dv=new Vector3[vertices.Length];var dn=new Vector3[vertices.Length];source.GetBlendShapeFrameVertices(source.GetBlendShapeIndex(h.poses[column-1].name),0,dv,dn,null);for(int i=0;i<vertices.Length;i++){vertices[i]+=dv[i];normals[i]+=dn[i];}}
                        var mesh=new Mesh();mesh.vertices=vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=normals.Select(n=>matrix.MultiplyVector(n).normalized).ToArray();mesh.uv=source.uv;mesh.tangents=source.tangents.Select(t=>{var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w);}).ToArray();
                        var triangles=new List<int>();var all=source.triangles;for(int i=0;i<all.Length;i+=3)if(keep.Contains(all[i])&&keep.Contains(all[i+1])&&keep.Contains(all[i+2])){triangles.Add(all[i]);triangles.Add(all[i+1]);triangles.Add(all[i+2]);}mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();
                        var go=new GameObject("Reviewed hand surface");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=name=="Idil"?skin.sharedMaterials[1]:skin.sharedMaterial;
                        GameObject handle=null;
                        if(column>0){handle=GameObject.CreatePrimitive(PrimitiveType.Cylinder);SceneManager.MoveGameObjectToScene(handle,scene);handle.transform.position=bone.TransformPoint(h.poses[column-1].center);handle.transform.rotation=Quaternion.FromToRotation(Vector3.up,bone.TransformDirection(Vector3.Cross(h.forward,h.normal).normalized));handle.transform.localScale=new Vector3(h.poses[column-1].radius*2,.075f,h.poses[column-1].radius*2);}
                        var center=bone.TransformPoint(h.palm);var forward=bone.TransformDirection(h.forward);var normal=bone.TransformDirection(h.normal);var across=Vector3.Cross(forward,normal).normalized;
                        cameraGo.transform.position=center+normal*.45f+across*.12f;cameraGo.transform.rotation=Quaternion.LookRotation(center-cameraGo.transform.position,forward);
                        var target=RenderTexture.GetTemporary(360,360,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var tile=new Texture2D(360,360,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,360,360),0,0);tile.Apply();sheet.SetPixels(column*360,(1-row)*360,360,360,tile.GetPixels());RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);if(handle)UnityEngine.Object.DestroyImmediate(handle);
                    }
                    sheet.Apply();File.WriteAllBytes("ClientExports/YanYana/ArtReview/"+name+"_HandShapes.png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(actor);
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
