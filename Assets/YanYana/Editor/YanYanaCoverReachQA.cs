// Editor-only pose probe. It measures cloned poses without changing the scene.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static class YanYanaCoverReachQA
    {
        static readonly MethodInfo Solve=typeof(FirefighterExtinguishManager).GetMethod("SolveArm",BindingFlags.NonPublic|BindingFlags.Static);
        [MenuItem("Tools/Yan Yana/QA/Measure Cover Palms")]
        static void Measure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            var scene=EditorSceneManager.NewPreviewScene();var rows=new List<string>{"Cloned crouched source poses. Head contact candidates are actual head-weighted mesh vertices.","actor,x,y,z,localHint,damping,passes,error,normalAlignment,headLocalX,headLocalY,headLocalZ,normalLocalX,normalLocalY,normalLocalZ"};
            try
            {
                foreach(string who in new[]{"Ada","Efe","AdaLeft"})
                {
                    string actorName=who.StartsWith("Ada")?"Ada":"Efe";
                    var source=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==actorName).gameObject;
                    var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,180,0));
                    var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);float ankle=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;animator.Play("Çök, korun, tutun",0,0);animator.Update(.2f);animator.transform.position+=Vector3.up*(ankle-animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y);
                    bool right=who=="Ada";var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var head=animator.GetBoneTransform(HumanBodyBones.Head);var definition=YanYanaInteractionHands.Read(actorName).hands.Single(h=>h.right==right);
                    var bones=animator.GetComponentsInChildren<Transform>();var rotations=bones.Select(t=>t.localRotation).ToArray();var points=new List<(Vector3 point,Vector3 normal)>();
                    foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh=skin.sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;var weights=mesh.boneWeights;int headIndex=Array.IndexOf(skin.bones,head);if(headIndex<0)continue;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                        for(int v=0;v<vertices.Length;v++)
                        {
                            var w=weights[v];float influence=(w.boneIndex0==headIndex?w.weight0:0)+(w.boneIndex1==headIndex?w.weight1:0)+(w.boneIndex2==headIndex?w.weight2:0)+(w.boneIndex3==headIndex?w.weight3:0);if(influence<.7f)continue;
                            Vector3 p=Vector3.zero,n=Vector3.zero;foreach(var b in new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)}){p+=matrices[b.Item1].MultiplyPoint3x4(vertices[v])*b.Item2;n+=matrices[b.Item1].MultiplyVector(normals[v])*b.Item2;}points.Add((p,n.normalized));
                        }
                    }
                    if(points.Count==0)throw new InvalidOperationException("No head surface in "+who);
                    var results=new List<(float score,string row)>();
                    foreach(float x in new[]{0f,.06f,.12f})foreach(float y in new[]{.10f,.14f,.18f,.21f})foreach(float z in new[]{-.10f,-.05f,0f,.05f})foreach(bool localHint in new[]{false,true})foreach(float damping in new[]{1f,.55f})
                    {
                        for(int b=0;b<bones.Length;b++)bones[b].localRotation=rotations[b];
                        var desired=head.position+Vector3.up*y+actor.transform.TransformDirection(new Vector3(right?x:-x,0,z));var surface=points.OrderBy(p=>(p.point-desired).sqrMagnitude).First();var contact=surface.point+surface.normal*.003f;var hint=new Vector3(right?.8f:-.8f,.3f,-.2f);if(localHint)hint=actor.transform.TransformDirection(hint);int passes=damping==1?8:12;
                        for(int pass=0;pass<passes;pass++)
                        {
                            Solve.Invoke(null,new object[]{upper,lower,hand,Vector3.Lerp(hand.position,contact-hand.TransformVector(definition.palm),damping),hint});var axis=hand.position-lower.position;float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(hand.TransformDirection(definition.normal),axis),Vector3.ProjectOnPlane(-surface.normal,axis),axis);lower.rotation=Quaternion.AngleAxis(angle*damping,axis)*lower.rotation;
                        }
                        float error=Vector3.Distance(hand.TransformPoint(definition.palm),contact),align=Vector3.Dot(hand.TransformDirection(definition.normal),-surface.normal);var hp=head.InverseTransformPoint(contact);var hn=head.InverseTransformDirection(-surface.normal);string row=$"{who},{x},{y},{z},{localHint},{damping},{passes},{error},{align},{hp.x},{hp.y},{hp.z},{hn.x},{hn.y},{hn.z}";
                        results.Add((error+(1-align)*.01f+Mathf.Abs(y-.18f)*.005f,row));
                    }
                    rows.AddRange(results.OrderBy(r=>r.score).Select(r=>r.row));UnityEngine.Object.DestroyImmediate(actor);
                }
                File.WriteAllLines("ClientExports/YanYana/Reports/cover-palm-reach.csv",rows);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
