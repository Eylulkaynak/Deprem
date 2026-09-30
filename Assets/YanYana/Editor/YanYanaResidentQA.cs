// Passive editor diagnostics: never change decisions, movement, clocks or input.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static bool watchingResidents;static int residentSamples,residentWalking;static float residentHandMax,residentGripMax,residentFootMin,residentFootMax;
        static readonly HashSet<string> residentCaptures=new HashSet<string>();
        static string residentWorst;static readonly List<float> residentGripSamples=new List<float>();
        [MenuItem("Tools/Yan Yana/QA/Watch Residents During Route")]
        static void WatchResidents()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play first.");
            watchingResidents=true;residentSamples=residentWalking=0;residentHandMax=residentGripMax=0;residentFootMin=float.PositiveInfinity;residentFootMax=float.NegativeInfinity;residentCaptures.Clear();
            residentGripSamples.Clear();residentWorst="";
            EditorApplication.update-=ResidentsTick;EditorApplication.update+=ResidentsTick;
            File.WriteAllText("ClientExports/YanYana/Reports/resident-route-observation.txt","Watching an actual route. No game state or input is injected by this observer.\n");
        }
        static void ResidentsTick()
        {
            if(!watchingResidents)return;
            if(!EditorApplication.isPlaying){watchingResidents=false;EditorApplication.update-=ResidentsTick;return;}
            var state=Variables.Object(Flow);if((bool)state.Get("Paused"))return;
            int phase=Convert.ToInt32(state.Get("Phase"));var actor=Find("Yusuf");var cane=actor.transform.Find("WalkingCane");var agent=actor.GetComponent<NavMeshAgent>();
            if(phase>=3&&phase<=7)
            {
                residentSamples++;if(agent.velocity.magnitude>.10f)residentWalking++;
                residentHandMax=Mathf.Max(residentHandMax,Convert.ToSingle(Variables.Object(cane.gameObject).Get("HandError")));
                float gripError=Convert.ToSingle(Variables.Object(cane.gameObject).Get("GripCenterError"));residentGripSamples.Add(gripError);
                if(gripError>residentGripMax)
                {residentGripMax=gripError;var a=actor.GetComponentInChildren<Animator>();residentWorst="Worst phase="+phase+" speed="+agent.velocity.magnitude+" actor="+actor.transform.position+" cane="+cane.position+" upper="+a.GetBoneTransform(HumanBodyBones.RightUpperArm).position+" elbow="+a.GetBoneTransform(HumanBodyBones.RightLowerArm).position+" hand="+a.GetBoneTransform(HumanBodyBones.RightHand).position;}
                foreach(var hit in Physics.RaycastAll(cane.position+Vector3.up*.20f,Vector3.down,.50f).Where(h=>h.collider.name.StartsWith("COLLIDER_")))
                {float gap=cane.position.y-hit.point.y;residentFootMin=Mathf.Min(residentFootMin,gap);residentFootMax=Mathf.Max(residentFootMax,gap);break;}
                string key=phase==3&&Convert.ToInt32(state.Get("NeighborTogether"))==1?"neighbor":phase==6&&Convert.ToInt32(state.Get("AidStage"))>0?"aid":"";
                if(key!=""&&residentCaptures.Add(key)){ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/residents-"+key+".png");if(key=="neighbor")CaptureResidentPose();}
            }
            if(phase==8)
            {
                var residents=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Mahalledeki komşu ·")||t.name.StartsWith("Yardım bekleyen komşu ")).ToArray();
                var report=new List<string>{"Passive observation of the EventSystem route; separate from child/device validation.","Samples="+residentSamples+" walking samples="+residentWalking,"Max wrist target error metres="+residentHandMax,"Max sculpted grip center error metres="+residentGripMax,"Cane ground gap range metres="+residentFootMin+" .. "+residentFootMax,"Six resident instances="+residents.Length,"Ending="+state.Get("Ending")+" Reunited="+state.Get("Reunited"),"PASS="+(residentSamples>60&&residentWalking>10&&residentHandMax<.025f&&residentGripMax<.025f&&residentFootMin>=-.02f&&residentFootMax<.10f&&residents.Length==6),"Finger closure and wrist continuity also require the accompanying visual review; distances alone are not a grip test."};
                foreach(var r in residents)report.Add(r.name+" at "+r.position);
                residentGripSamples.Sort();if(residentGripSamples.Count>0)report.Add("Grip p95 metres="+residentGripSamples[Mathf.Min(residentGripSamples.Count-1,(int)(residentGripSamples.Count*.95f))]);report.Add(residentWorst);
                File.WriteAllLines("ClientExports/YanYana/Reports/resident-route-observation.txt",report);watchingResidents=false;EditorApplication.update-=ResidentsTick;
            }
        }
        [MenuItem("Tools/Yan Yana/QA/Capture Yusuf Pose")]
        static void CaptureResidentPose()
        {
            var actor=Find("Yusuf");var go=new GameObject("Temporary resident diagnostic camera");
            var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.05f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.84f,.87f,.83f);
            go.transform.position=actor.transform.TransformPoint(new Vector3(1.7f,1.5f,3.5f));go.transform.LookAt(actor.transform.position+Vector3.up*.85f);
            var target=RenderTexture.GetTemporary(760,1000,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(760,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,760,1000),0,0);image.Apply();File.WriteAllBytes("ClientExports/YanYana/Screenshots/yusuf-pose-diagnostic.png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
