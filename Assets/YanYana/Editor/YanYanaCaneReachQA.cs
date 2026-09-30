// Editor-only pose-space measurement, separate from real-input route validation.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static class YanYanaCaneReachQA
    {
        class Candidate {public float x,z,max,total;public int samples;}
        [MenuItem("Tools/Yan Yana/QA/Measure Cane Reach")]
        public static void Measure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before measuring cloned poses.");
            var source=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Yusuf").gameObject;
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var animator=actor.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
                var bones=animator.GetComponentsInChildren<Transform>();var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                var grip=JsonUtility.FromJson<YanYanaGripAuthor.Grip>(File.ReadAllText(YanYanaGripAuthor.GripPath));
                var solve=typeof(FirefighterExtinguishManager).GetMethod("SolveArm",BindingFlags.NonPublic|BindingFlags.Static);
                var candidates=new List<Candidate>();for(int x=30;x<=46;x+=2)for(int z=8;z<=28;z+=2)candidates.Add(new Candidate{x=x*.01f,z=z*.01f});
                foreach(float speed in new[]{0f,.6f,1.2f})
                {
                    animator.Rebind();animator.SetFloat("Speed",speed);animator.Update(.4f);
                    for(int frame=0;frame<24;frame++)
                    {
                        animator.Update(1f/24);var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
                        foreach(var c in candidates)foreach(float dz in new[]{-.035f,0,.035f})foreach(float lift in new[]{0f,.04f,.075f})
                        {
                            for(int b=0;b<bones.Length;b++){bones[b].localPosition=positions[b];bones[b].localRotation=rotations[b];}
                            var contact=new Vector3(c.x-.045f,.856f+lift,c.z+.005f+dz);
                            for(int pass=0;pass<4;pass++)
                            {
                                solve.Invoke(null,new object[]{upper,lower,hand,contact-hand.TransformVector(grip.handleCenter),new Vector3(.45f,-1f,-.1f)});
                                var axis=hand.position-lower.position;var across=hand.TransformDirection(Vector3.Cross(grip.forward,grip.normal).normalized);
                                float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(across,axis),Vector3.ProjectOnPlane(Vector3.right,axis),axis);
                                lower.rotation=Quaternion.AngleAxis(angle,axis)*lower.rotation;
                            }
                            float error=Vector3.Distance(hand.TransformPoint(grip.handleCenter),contact);c.max=Mathf.Max(c.max,error);c.total+=error;c.samples++;
                        }
                        for(int b=0;b<bones.Length;b++){bones[b].localPosition=positions[b];bones[b].localRotation=rotations[b];}
                    }
                }
                var rows=new List<string>{"Editor pose-space probe; not a gameplay or child/device test.","72 animation samples, 3 stride offsets (+/-35 mm), 3 ground/lift offsets (0/40/75 mm).","x,z,maxGripErrorMetres,meanGripErrorMetres,samples"};
                foreach(var c in candidates.OrderBy(c=>c.max))rows.Add(c.x.ToString("F3")+","+c.z.ToString("F3")+","+c.max.ToString("F6")+","+(c.total/c.samples).ToString("F6")+","+c.samples);
                File.WriteAllLines("ClientExports/YanYana/Reports/cane-reach-probe.csv",rows);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
