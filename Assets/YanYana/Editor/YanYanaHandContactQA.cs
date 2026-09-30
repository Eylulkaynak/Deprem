// Editor-only diagnostics of the actual scene poses and original animation samples.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static class YanYanaHandContactQA
    {
        static readonly MethodInfo solve=typeof(FirefighterExtinguishManager).GetMethod("SolveArm",BindingFlags.NonPublic|BindingFlags.Static);
        [MenuItem("Tools/Yan Yana/QA/Measure Carried Props")]
        public static void MeasureCarriedProps()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before measuring cloned poses.");var scene=EditorSceneManager.NewPreviewScene();var rows=new List<string>{"Editor pose-space probe: upright flashlight / comfort toy; 24 idle and walk samples per character."};
            try
            {
                foreach(string who in new[]{"Ada","Efe"})foreach(float sign in new[]{1f})
                {
                    var source=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==who).gameObject;var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var animator=actor.GetComponentInChildren<Animator>();var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var definition=YanYanaInteractionHands.Read(who).hands.Single(h=>h.right);var grip=definition.poses.Single(p=>p.name.EndsWith(who=="Ada"?"_Loop":"_Soft"));var goal=new Vector3(.20f,who=="Ada"?.72f:.55f,who=="Ada"?.18f:.14f);var handleAxis=who=="Ada"?Vector3.right:Vector3.up;var bones=animator.GetComponentsInChildren<Transform>();float maximum=0;
                    foreach(float speed in new[]{0f,1.2f}){animator.Rebind();animator.SetFloat("Speed",speed);animator.Update(.4f);for(int sample=0;sample<12;sample++){animator.Update(.1f);var rotations=bones.Select(t=>t.localRotation).ToArray();for(int pass=0;pass<8;pass++){solve.Invoke(null,new object[]{upper,lower,hand,goal-hand.TransformVector(grip.center),new Vector3(.4f,-1,-.1f)});var axis=hand.position-lower.position;var across=hand.TransformDirection(Vector3.Cross(definition.forward,definition.normal).normalized);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(across,axis),Vector3.ProjectOnPlane(handleAxis*sign,axis),axis);lower.rotation=Quaternion.AngleAxis(angle,axis)*lower.rotation;}maximum=Mathf.Max(maximum,Vector3.Distance(hand.TransformPoint(grip.center),goal));for(int b=0;b<bones.Length;b++)bones[b].localRotation=rotations[b];}}
                    rows.Add(who+" axis="+sign+" target="+goal+" maximumGripErrorMetres="+maximum);UnityEngine.Object.DestroyImmediate(actor);
                }
                File.WriteAllLines("ClientExports/YanYana/Reports/carried-prop-reach.txt",rows);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [MenuItem("Tools/Yan Yana/QA/Measure Hose Reach")]
        public static void MeasureHose()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before measuring cloned poses.");var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var source=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Idil").gameObject;var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var animator=actor.GetComponentInChildren<Animator>();var data=YanYanaInteractionHands.Read("Idil");var candidates=new List<(float y,float z,float rs,float ls,float pole,float max,float mean)>();
                foreach(float y in new[]{-.08f,-.12f,-.16f,-.20f})foreach(float z in new[]{.12f,.16f,.20f})foreach(float rs in new[]{1f,-1f})foreach(float ls in new[]{1f,-1f})foreach(float pole in new[]{-1f,.1f})
                {
                    float maximum=0,total=0;int count=0;
                    foreach(float yaw in new[]{-25f,0f,25f})foreach(float pitch in new[]{5f,15f})
                    {
                        animator.Rebind();animator.Update(.2f);var origin=(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position+animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position)*.5f+new Vector3(0,y,z);var rotation=Quaternion.Euler(pitch,yaw,0);var handleAxis=rotation*Vector3.right;
                        foreach(bool right in new[]{true,false})
                        {
                            var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var definition=data.hands.Single(h=>h.right==right);var grip=definition.poses.Single(p=>p.name.EndsWith("_Loop"));var contact=origin+rotation*new Vector3(right?.12f:-.12f,.04f,.12f);
                            for(int pass=0;pass<12;pass++){solve.Invoke(null,new object[]{upper,lower,hand,Vector3.Lerp(hand.position,contact-hand.TransformVector(grip.center),.55f),new Vector3(right?.4f:-.4f,pole,-.1f)});var axis=hand.position-lower.position;var across=hand.TransformDirection(Vector3.Cross(definition.forward,definition.normal).normalized);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(across,axis),Vector3.ProjectOnPlane(handleAxis*(right?rs:ls),axis),axis);lower.rotation=Quaternion.AngleAxis(angle*.55f,axis)*lower.rotation;}
                            float error=Vector3.Distance(hand.TransformPoint(grip.center),contact);maximum=Mathf.Max(maximum,error);total+=error;count++;
                        }
                    }
                    candidates.Add((y,z,rs,ls,pole,maximum,total/count));
                }
                File.WriteAllLines("ClientExports/YanYana/Reports/hose-reach.csv",new[]{"Editor pose-space probe: shoulder-relative nozzle, 3 yaw / 2 pitch directions / both hands.","y,z,rightAxis,leftAxis,poleY,maxErrorMetres,meanErrorMetres"}.Concat(candidates.OrderBy(c=>c.max).Select(c=>c.y+","+c.z+","+c.rs+","+c.ls+","+c.pole+","+c.max+","+c.mean)));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [MenuItem("Tools/Yan Yana/QA/Capture Interaction Pose")]
        public static void Capture()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Capture requires the running scene.");
            var actors=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);var flow=actors.First(t=>t.name.StartsWith("01 Akış")).gameObject;var vars=Variables.Object(flow);string workspace=(string)vars.Get("Workspace");var rows=new List<string>{"Workspace="+workspace,"Phase="+vars.Get("Phase")};
            foreach(string who in new[]{"Ada","Efe","Idil"})
            {
                var actor=actors.First(t=>t.name==who).gameObject;var animator=actor.GetComponentInChildren<Animator>();rows.Add(who+" root="+actor.transform.position+" rotation="+actor.transform.eulerAngles+" Pose="+animator.GetInteger("Pose"));
                foreach(bool right in new[]{false,true})
                {
                    var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var definition=YanYanaInteractionHands.Read(who).hands.Single(h=>h.right==right);
                    rows.Add((right?"Right":"Left")+" shoulder="+upper.position+" elbow="+lower.position+" wrist="+hand.position+" reach="+(Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position)));
                    foreach(var pose in definition.poses)rows.Add(pose.name+" contact="+hand.TransformPoint(pose.center));
                }
                var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)if(skin.GetBlendShapeWeight(i)>.01f)rows.Add("active "+skin.sharedMesh.GetBlendShapeName(i)+"="+skin.GetBlendShapeWeight(i));
                // Offscreen GPU skin baking is not a reliable runtime mesh sample here.
                // Report the graph's contact marker separately from the visual capture.
                foreach(bool right in new[]{false,true})rows.Add((right?"Right":"Left")+" contactMarkerError="+vars.Get(who+(right?"Right":"Left")+"GripError"));
                if((workspace.StartsWith("fire")&&who!="Idil")||(!workspace.StartsWith("fire")&&who=="Idil"))continue;
                var go=new GameObject("Temporary hand review camera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.75f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.84f,.87f,.83f);go.transform.position=actor.transform.TransformPoint(new Vector3(1.8f,1.35f,2.4f));go.transform.LookAt(actor.transform.position+Vector3.up*.80f);
                var target=RenderTexture.GetTemporary(850,1000,24);camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(850,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,850,1000),0,0);image.Apply();File.WriteAllBytes("ClientExports/YanYana/Screenshots/interaction-"+workspace+"-"+who+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllLines("ClientExports/YanYana/Reports/interaction-pose-"+workspace+".txt",rows);
        }
        [MenuItem("Tools/Yan Yana/QA/Measure Toy Kit Reach")]
        public static void Measure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before measuring cloned poses.");var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var source=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Ada").gameObject;var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);var bones=animator.GetComponentsInChildren<Transform>();var data=YanYanaInteractionHands.Read("Ada");var rows=new List<string>{"Editor-only reach probe; grounded scene character, existing idle/walk clips.","right,y,z,x,passes,maxErrorMetres"};
                foreach(bool right in new[]{false,true})foreach(float y in new[]{.69f,.76f,.81f})foreach(float z in new[]{.04f,.10f,.18f,.28f})foreach(float x in new[]{.08f,.14f})foreach(int passes in new[]{4,8})
                {
                    var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var definition=data.hands.Single(h=>h.right==right);var grip=definition.poses.Single(p=>p.name.EndsWith("_Loop"));float maximum=0;
                    foreach(float speed in new[]{0f,1.2f})
                    {
                        animator.Rebind();animator.SetFloat("Speed",speed);animator.Update(.4f);
                        for(int sample=0;sample<6;sample++)
                        {
                            animator.Update(.16f);var rotations=bones.Select(t=>t.localRotation).ToArray();var contact=new Vector3(right?x:-x,y,z);
                            for(int pass=0;pass<passes;pass++){solve.Invoke(null,new object[]{upper,lower,hand,contact-hand.TransformVector(grip.center),new Vector3(right?.4f:-.4f,-1,-.1f)});var axis=hand.position-lower.position;var across=hand.TransformDirection(Vector3.Cross(definition.forward,definition.normal).normalized);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(across,axis),Vector3.ProjectOnPlane(Vector3.right,axis),axis);lower.rotation=Quaternion.AngleAxis(angle,axis)*lower.rotation;}
                            maximum=Mathf.Max(maximum,Vector3.Distance(hand.TransformPoint(grip.center),contact));for(int b=0;b<bones.Length;b++)bones[b].localRotation=rotations[b];
                        }
                    }
                    rows.Add(right+","+y+","+z+","+x+","+passes+","+maximum);
                }
                File.WriteAllLines("ClientExports/YanYana/Reports/toy-kit-reach.csv",rows);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
