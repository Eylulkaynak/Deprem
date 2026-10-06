// Editor-only navigation, pointer-sequence and serialization checks for the park.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        const string NeighborhoodReport="ClientExports/YanYana/Reports/neighborhood-integration.txt";
        static IEnumerator<object> neighborhoodCheck;
        static void NeighborhoodAssert(bool condition,string label)
        {File.AppendAllText(NeighborhoodReport,(condition?"PASS ":"FAIL ")+label+"\n");if(!condition)throw new InvalidOperationException(label);}

        [MenuItem("Tools/Yan Yana/Art/Render Living Neighborhood")]
        public static void RenderNeighborhood()
        {
            Directory.CreateDirectory("ClientExports/YanYana/Screenshots");
            NeighborhoodRender("park-overview",new Vector3(1,19,-12),new Vector3(0,-.4f,-27),1600,1000,48);
            NeighborhoodRender("park-families",new Vector3(-13,6,-17),new Vector3(-7.5f,.25f,-25),1200,1000,48);
            NeighborhoodRender("street-decisions",new Vector3(-3,7,-6),new Vector3(-2.8f,.1f,-12),1200,1000,52);
        }

        static void NeighborhoodRender(string name,Vector3 eye,Vector3 target,int width,int height,float fov)
        {
            var go=new GameObject("Temporary park review camera");var cam=go.AddComponent<Camera>();cam.transform.position=eye;cam.transform.LookAt(target);cam.fieldOfView=fov;cam.nearClipPlane=.1f;cam.farClipPlane=120;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.70f,.81f,.78f);
            var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;Texture2D capture=null;
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;capture=new Texture2D(width,height,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,width,height),0,0);capture.Apply();File.WriteAllBytes("ClientExports/YanYana/Screenshots/"+name+".png",capture.EncodeToPNG());}
            finally{RenderTexture.active=previous;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);if(capture)UnityEngine.Object.DestroyImmediate(capture);UnityEngine.Object.DestroyImmediate(go);}
        }

        [MenuItem("Tools/Yan Yana/QA/Inspect Living Neighborhood")]
        public static void InspectNeighborhood()
        {
            Directory.CreateDirectory("ClientExports/YanYana/Reports");File.WriteAllText(NeighborhoodReport,"Scene checks; rendered review is separate from a child play session.\n");
            var residents=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Where(x=>x.name.StartsWith("Park sakini · ")).ToArray();
            NeighborhoodAssert(residents.Length==22,"22 park/street residents, including 7 children and 2 elders");
            foreach(var person in residents)NeighborhoodAssert(person.GetComponentInChildren<Animator>().avatar.isHuman,"approved humanoid: "+person.name);
            var start=new Vector3(3,-.48f,-21);
            foreach(var target in new[]{new Vector3(-7,-.48f,-24.9f),new Vector3(8,-.48f,-23.9f),new Vector3(-8.2f,-.48f,-23.4f),new Vector3(-9.4f,-.48f,-21.5f),new Vector3(-9.62f,-.48f,-24.3f),new Vector3(-3.4f,-.48f,-13),new Vector3(-4.9f,-.48f,-12.4f),new Vector3(0,-.48f,-29)})
            {
                bool from=NavMesh.SamplePosition(start,out var a,.7f,NavMesh.AllAreas),to=NavMesh.SamplePosition(target,out var b,.7f,NavMesh.AllAreas);var path=new NavMeshPath();
                NeighborhoodAssert(from&&to&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"walkable park/story destination "+target);
            }
            var flags=Variables.Object(Flow);foreach(string key in new[]{"GasNoticed","GasRetreated","GasReported","WallRouteChosen","ParkChildNoticed","ParkChildAsked","ParkChildHelped"})NeighborhoodAssert(flags.IsDefined(key),"serialized story state "+key);
            NeighborhoodAssert(!UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(x=>x&&x.GetType().Namespace=="YanYana.Editor"),"no editor-only component shipped in the scene");
        }

        [MenuItem("Tools/Yan Yana/QA/Play Living Neighborhood")]
        public static void PlayNeighborhood()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            InspectNeighborhood();neighborhoodCheck=NeighborhoodSequence().GetEnumerator();EditorApplication.update-=NeighborhoodTick;EditorApplication.update+=NeighborhoodTick;
        }

        static void NeighborhoodTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=NeighborhoodTick;return;}
            try{if(!neighborhoodCheck.MoveNext()){EditorApplication.update-=NeighborhoodTick;File.AppendAllText(NeighborhoodReport,"PASS pointer sequence complete\n");RenderNeighborhood();}}
            catch(Exception e){EditorApplication.update-=NeighborhoodTick;File.AppendAllText(NeighborhoodReport,"FAIL "+e+"\n");Debug.LogException(e);}
        }

        static IEnumerable<object> NeighborhoodWait(Func<bool> predicate,string label,float timeout=25)
        {
            double start=EditorApplication.timeSinceStartup;
            while(!predicate()){if(EditorApplication.timeSinceStartup-start>timeout)throw new TimeoutException(label);yield return null;}
            NeighborhoodAssert(true,label);
        }

        static IEnumerable<object> NeighborhoodSequence()
        {
            var vars=Variables.Object(Flow);Fixture(4,new Vector3(-3.7f,-.48f,-11.2f));vars.Set("IntroDone",1);vars.Set("NavigationReady",true);
            foreach(string key in new[]{"GasNoticed","GasRetreated","GasReported","WallRouteChosen","ParkChildNoticed","ParkChildAsked","ParkChildHelped"})vars.Set(key,0);
            yield return null;
            Click(Find("Park sakini · Eren · sokak güvenlik görevlisi"));NeighborhoodAssert((int)vars.Get("GasReported")==0,"report cannot skip moving away from smell");
            foreach(var f in NeighborhoodWait(()=>(int)vars.Get("GasNoticed")==1,"smell noticed on the normal street route"))yield return f;
            Click(Find("Gazdan uzak açık bekleme noktası"));
            foreach(var f in NeighborhoodWait(()=>(int)vars.Get("GasRetreated")==1&&!(bool)vars.Get("Busy"),"Ada physically walks to the open waiting point"))yield return f;
            Click(Find("Park sakini · Eren · sokak güvenlik görevlisi"));NeighborhoodAssert((int)vars.Get("GasReported")==1&&Find("Gaz için görevlinin kapattığı şerit").activeSelf,"staff receives report and closes the boundary");
            Click(Find("Duvarın açığındaki güvenli adım"));foreach(var f in NeighborhoodWait(()=>(int)vars.Get("WallRouteChosen")==1&&!(bool)vars.Get("Busy"),"open route around the broken wall is played"))yield return f;
            NeighborhoodAssert((int)vars.Get("FacadeReported")==1,"wall route feeds the existing alternate reunion consequence");
            Fixture(6,new Vector3(-8.2f,-.48f,-22.8f));yield return null;
            Click(Find("Park sakini · Ece · ailesini bekleyen çocuk"));foreach(var f in NeighborhoodWait(()=>(int)vars.Get("ParkChildAsked")==1,"Ada approaches and listens to Ece"))yield return f;
            Click(Find("Park sakini · Zeynep · aile danışma görevlisi"));foreach(var f in NeighborhoodWait(()=>(int)vars.Get("ParkChildHelped")==1&&!(bool)vars.Get("Busy"),"mother walks to the staffed family meeting point"))yield return f;
            var child=Find("Park sakini · Ece · ailesini bekleyen çocuk");var mother=Find("Park sakini · Aylin · Ece’nin annesi");NeighborhoodAssert(Vector3.Distance(child.transform.position,mother.transform.position)<1.2f,"Ece waits safely while her mother reaches her");
            CustomEvent.Trigger(Flow,"CommitCheckpoint");
            foreach(string key in new[]{"GasReported","WallRouteChosen","ParkChildHelped"})NeighborhoodAssert(PlayerPrefs.GetInt("Deprem.YanYana.v1.physical."+key,-1)==1,"checkpoint stores "+key);
            mother.GetComponent<StoryPlayerMovement>().Warp(new Vector3(-11,-.48f,-27.4f));CustomEvent.Trigger(Flow,"RestorePhysicalObjects");yield return null;
            NeighborhoodAssert(Vector3.Distance(child.transform.position,mother.transform.position)<1.2f,"restore keeps the reunited park family together");
        }
    }
}
