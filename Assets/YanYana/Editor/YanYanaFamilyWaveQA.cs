// Editor-only calibration of the existing humanoid avatars on disposable clones.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YanYana.Editor
{
    public static class YanYanaFamilyWaveQA
    {
        [MenuItem("Tools/Yan Yana/QA/Measure Family Wave")]
        static void Measure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            var scene=EditorSceneManager.NewPreviewScene();
            var rows=new List<string>{"actor,kind,armUp,armFront,armTwist,elbowStretch,wristX,wristY,wristZ,headY,error"};
            string F(float x)=>x.ToString("F5",CultureInfo.InvariantCulture);
            try
            {
                foreach(string who in new[]{"Ada","Efe","Derya","Emre"})
                {
                    var source=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==who).gameObject;
                    var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
                    var wrist=animator.GetBoneTransform(HumanBodyBones.RightHand);var head=animator.GetBoneTransform(HumanBodyBones.Head);
                    float height=head.position.y-animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
                    var desired=head.position+actor.transform.right*height*.26f+Vector3.up*height*.035f+actor.transform.forward*height*.04f;
                    animator.Play("Aile işareti",0,.52f);animator.Update(0);
                    rows.Add(who+",previous,,,,,"+F(wrist.position.x)+","+F(wrist.position.y)+","+F(wrist.position.z)+","+F(head.position.y)+","+F(Vector3.Distance(wrist.position,desired)));
                    animator.Play("Duruş ve yürüyüş",0,0);animator.Update(0);
                    var sourceWave=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/YanYana/Animation/FamilyWave.anim");
                    var probe=UnityEngine.Object.Instantiate(sourceWave);var over=new AnimatorOverrideController(animator.runtimeAnimatorController);over[sourceWave]=probe;animator.runtimeAnimatorController=over;
                    try
                    {
                        // Sample through the actual controller/clip path. Direct
                        // HumanPoseHandler application changed the cloned root's
                        // orientation and gave incomparable hand coordinates.
                        void Curve(string muscle,float value)=>probe.SetCurve("",typeof(Animator),muscle,AnimationCurve.Constant(0,2.3f,value));
                        var candidates=new List<(float score,string row)>();
                        foreach(float a in new[]{.2f,.4f,.6f,.8f,1f})foreach(float b in new[]{-.2f,0f,.2f})foreach(float c in new[]{-.8f,-.4f,0f,.4f,.8f})foreach(float d in new[]{-.8f,-.4f,0f,.4f})
                        {
                            Curve("Right Arm Down-Up",a);Curve("Right Arm Front-Back",b);Curve("Right Arm Twist In-Out",c);Curve("Right Forearm Stretch",d);Curve("Right Hand In-Out",0);Curve("Right Hand Down-Up",0);animator.Rebind();animator.Play("Aile işareti",0,.52f);animator.Update(0);
                            float score=Vector3.Distance(wrist.position,desired);candidates.Add((score,who+",candidate,"+F(a)+","+F(b)+","+F(c)+","+F(d)+","+F(wrist.position.x)+","+F(wrist.position.y)+","+F(wrist.position.z)+","+F(head.position.y)+","+F(score)));
                        }
                        rows.AddRange(candidates.OrderBy(x=>x.score).Take(8).Select(x=>x.row));
                    }
                    finally{UnityEngine.Object.DestroyImmediate(over);UnityEngine.Object.DestroyImmediate(probe);}
                    UnityEngine.Object.DestroyImmediate(actor);
                }
                File.WriteAllLines("ClientExports/YanYana/Reports/family-wave-calibration.csv",rows);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
