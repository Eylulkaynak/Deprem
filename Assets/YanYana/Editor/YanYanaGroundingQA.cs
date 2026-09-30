using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical Feet Probe")]
        static void FeetProbe()
        {
            var report=new List<string>();var mesh=new Mesh();
            foreach(string who in new[]{"Ada","Efe","Idil","Bora","Yusuf"})
            {
                var actor=Find(who);var animator=actor.GetComponentInChildren<Animator>();var agent=actor.GetComponent<NavMeshAgent>();
                report.Add(who+" root="+actor.transform.position.ToString("F4")+" visual="+animator.transform.localPosition.ToString("F4")+" nav="+agent.isOnNavMesh+" base="+agent.baseOffset);
                foreach(var foot in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes})
                {var bone=animator.GetBoneTransform(foot);if(bone)report.Add(foot+"="+bone.position.ToString("F4"));}
                foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    foreach(bool scale in new[]{false,true})
                    {
                        renderer.BakeMesh(mesh,scale);var transformed=new Bounds();var unscaled=new Bounds();bool first=true;
                        foreach(var vertex in mesh.vertices)
                        {var a=renderer.transform.TransformPoint(vertex);var b=renderer.transform.position+renderer.transform.rotation*vertex;if(first){transformed=new Bounds(a,Vector3.zero);unscaled=new Bounds(b,Vector3.zero);first=false;}else{transformed.Encapsulate(a);unscaled.Encapsulate(b);}}
                        report.Add(renderer.name+" bakeScale="+scale+" lossy="+renderer.transform.lossyScale.ToString("F6")+" raw="+mesh.bounds+" transformed="+transformed+" unscaled="+unscaled+" renderer="+renderer.bounds);
                    }
                }
            }
            Object.DestroyImmediate(mesh);File.WriteAllLines("ClientExports/YanYana/Reports/physical-feet-probe.txt",report);
        }
    }
}
