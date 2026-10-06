using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.Cinemachine;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static GameObject Flow=>SceneManager.GetActiveScene().GetRootGameObjects().First(x=>x.name.StartsWith("01 Akış"));
        static GameObject Find(string name)=>UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==name).gameObject;
        [MenuItem("Tools/Yan Yana/QA/Physical New")]
        static void New()=>Find("New").GetComponent<Button>().onClick.Invoke();
        [MenuItem("Tools/Yan Yana/QA/Physical Inspect")]
        static void Inspect()
        {
            var report=new List<string>();report.Add("PLAY="+EditorApplication.isPlaying);
            if(EditorApplication.isPlaying)foreach(var variable in Variables.Object(Flow))report.Add(variable.name+"="+variable.value);
            var ada=Find("Ada");
            foreach(var name in new[]{"Ada","Efe","Derya","Emre"})
            {
                var obj=Find(name);var rr=obj.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
                report.Add(name+" position="+obj.transform.position+" bounds="+b+" feet="+b.min.y);
                var animator=obj.GetComponentInChildren<Animator>();var contact=animator.GetComponent<Variables>();
                if(contact)foreach(var v in contact.declarations)report.Add(name+" contact "+v.name+"="+v.value);
                var agent=obj.GetComponent<NavMeshAgent>();if(agent)report.Add(" nav="+agent.isOnNavMesh+" remaining="+(agent.isOnNavMesh?agent.remainingDistance:-1));
            }
            foreach(var anchor in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Approach_")))
            {
                var path=new NavMeshPath();var valid=NavMesh.CalculatePath(ada.transform.position,anchor.position,NavMesh.AllAreas,path);report.Add(anchor.name+"="+anchor.position+" reachable="+valid+" status="+path.status);
            }
            Directory.CreateDirectory("ClientExports/YanYana/Reports");File.WriteAllLines("ClientExports/YanYana/Reports/physical-state.txt",report);Debug.Log("PHYSICAL INSPECT "+report.Count+" rows");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Capture")]
        static void Capture()
        {
            if(EditorApplication.isPlaying)
            {
                string workspace=(string)Variables.Object(Flow).Get("Workspace");if(workspace=="")workspace="phase"+Variables.Object(Flow).Get("Phase");
                if(quakeReviewActive)QuakeFrame("setup-"+workspace+"-"+DateTime.Now.ToString("HHmmssfff"));
                else if(!preparationReviewActive) {Directory.CreateDirectory("ClientExports/YanYana/Screenshots");ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/physical_"+workspace+".png");}
                EditorApplication.QueuePlayerLoopUpdate();EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Repaint();return;
            }
            var cam=Camera.main;var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(540,960,24,RenderTextureFormat.ARGB32);
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var mode=canvas.renderMode;var wc=canvas.worldCamera;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.35f;Canvas.ForceUpdateCanvases();
            var priorTarget=cam.targetTexture;cam.targetTexture=target;cam.Render();RenderTexture.active=target;
            var texture=new Texture2D(540,960,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,540,960),0,0);texture.Apply();
            string view=EditorApplication.isPlaying?(string)Variables.Object(Flow).Get("Workspace"):"edit";if(view=="")view="room";
            Directory.CreateDirectory("ClientExports/YanYana/Screenshots");File.WriteAllBytes("ClientExports/YanYana/Screenshots/physical_"+view+".png",texture.EncodeToPNG());
            canvas.renderMode=mode;canvas.worldCamera=wc;cam.targetTexture=priorTarget;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Fener")]
        static void Fener()=>Click(Find("Pilli fener"));
        [MenuItem("Tools/Yan Yana/QA/Physical Bag")]
        static void Bag()=>Click(Find("Aile çantası"));
        [MenuItem("Tools/Yan Yana/QA/Physical Back")]
        static void Back()=>Find("ActivityBack").GetComponent<Button>().onClick.Invoke();
        [MenuItem("Tools/Yan Yana/QA/Physical Begin Bridge")]
        static void BeginBridge()=>Find("PreparationDone").GetComponent<Button>().onClick.Invoke();
        [MenuItem("Tools/Yan Yana/QA/Physical Bridge Test")]
        static void BridgeTest()
        {
            if((string)Variables.Object(Flow).Get("Workspace")!="bridge")throw new InvalidOperationException("Walk to the toy bridge first.");
            var at=Find("Anchor_CoverAda").transform.position+Vector3.up*(Find("Anchor_Comfort").transform.position.y+.035f);
            for(int i=0;i<3;i++)Drag(Find("Köprü parçası "+i),at+new Vector3(-.20f+i*.20f,0,0));
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Protect")]
        static void Protect()=>Click(Find("COLLIDER_TableTop"));
        [MenuItem("Tools/Yan Yana/QA/Physical Battery Test")]
        static void BatteryTest()
        {
            if((string)Variables.Object(Flow).Get("Workspace")!="flashlight")throw new InvalidOperationException("Approach the flashlight first.");
            var center=Find("Anchor_FlashlightWork").transform.position+Vector3.up*.04f;
            Drag(Find("Kaydırılabilir pil kapağı"),center+new Vector3(.21f,.068f,-.04f));
            Drag(Find("AA pil 1"),center+new Vector3(-.031f,.035f,-.028f));
            Drag(Find("AA pil 2"),center+new Vector3(.031f,.035f,-.028f));
            Drag(Find("Kaydırılabilir pil kapağı"),center+new Vector3(0,.068f,-.018f));
            Click(Find("Gerçek açma anahtarı"));Inspect();Capture();
            bool ready=(int)Variables.Object(Flow).Get("FlashlightReady")==1;
            File.WriteAllText("ClientExports/YanYana/Reports/physical-battery-test.txt","Correct polarity, actual pointer handlers: "+ready);
            if(!ready)throw new InvalidOperationException("Correct batteries failed to power the flashlight.");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Wrong Polarity")]
        static void WrongPolarity()
        {
            var center=Find("Anchor_FlashlightWork").transform.position+Vector3.up*.04f;
            Drag(Find("Kaydırılabilir pil kapağı"),center+new Vector3(.21f,.068f,-.04f));
            Click(Find("AA pil 1"));Find("RotateItem").GetComponent<Button>().onClick.Invoke();
            Drag(Find("Kaydırılabilir pil kapağı"),center+new Vector3(0,.068f,-.018f));
            Inspect();Capture();
            bool off=(int)Variables.Object(Flow).Get("FlashlightReady")==0;
            File.AppendAllText("ClientExports/YanYana/Reports/physical-battery-test.txt","\nReversed battery prevents power: "+off);
            if(!off)throw new InvalidOperationException("Reversed battery incorrectly powered the flashlight.");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Packing Test")]
        static void PackingTest()
        {
            if((string)Variables.Object(Flow).Get("Workspace")!="bag")throw new InvalidOperationException("Approach the bag first.");
            var vars=Variables.Object(Flow);var notes=new List<string>{"Isolated packing fixture; inventory discovery is outside this test."};
            vars.Set("Found.Water",1);vars.Set("Found.Radio",1);vars.Set("BagClosed",0);CustomEvent.Trigger(Flow,"RestorePacking");
            var origin=Find("Anchor_BagWork").transform.position+new Vector3(-.2375f,.083f,-.13f);
            var water=Find("Yerleşim · Water");var radio=Find("Yerleşim · Radio");
            Drag(water,origin+new Vector3(.0475f,0,.1425f));
            bool waterFits=(int)vars.Get("Pack.Water.X")==0&&(int)vars.Get("Pack.Water.Y")==0;notes.Add("Water occupies 1x3 cells="+waterFits);
            Drag(radio,origin+new Vector3(.1425f,0,.095f));bool overlapBlocked=(int)vars.Get("Pack.Radio.X")==-1;notes.Add("Overlap rejected="+overlapBlocked);
            Click(radio);Find("RotateItem").GetComponent<Button>().onClick.Invoke();Drag(radio,origin+new Vector3(.19f,0,.1425f));
            bool rotated=(int)vars.Get("Pack.Radio.Rot")==1&&(int)vars.Get("Pack.Radio.X")==1;notes.Add("Rotated 2x3 radio fits beside water="+rotated);
            Drag(water,origin+new Vector3(-.30f,0,-.30f));bool removed=(int)vars.Get("Pack.Water.X")==-1&&(int)vars.Get("BagCell0")==0;notes.Add("Unpacking releases occupied cells="+removed);
            File.WriteAllLines("ClientExports/YanYana/Reports/physical-packing-test.txt",notes);Inspect();Capture();
            if(!waterFits||!overlapBlocked||!rotated||!removed)throw new InvalidOperationException("Packing interaction regression.");
        }
        public static void Drag(GameObject go,Vector3 destination)
        {
            var screen=(Vector2)Camera.main.WorldToScreenPoint(go.transform.position);
            var end=(Vector2)Camera.main.WorldToScreenPoint(destination);
            var data=new PointerEventData(EventSystem.current){position=screen,pressPosition=screen,button=PointerEventData.InputButton.Left,pointerId=-1,pointerDrag=go,pointerPress=go,pointerCurrentRaycast=new RaycastResult{gameObject=go,worldPosition=go.transform.position},pointerPressRaycast=new RaycastResult{gameObject=go,worldPosition=go.transform.position}};
            ExecuteEvents.Execute(go,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(go,data,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(go,data,ExecuteEvents.beginDragHandler);
            for(int i=1;i<=12;i++){var next=Vector2.Lerp(screen,end,i/12f);data.delta=next-data.position;data.position=next;ExecuteEvents.Execute(go,data,ExecuteEvents.dragHandler);}
            ExecuteEvents.Execute(go,data,ExecuteEvents.endDragHandler);ExecuteEvents.Execute(go,data,ExecuteEvents.pointerUpHandler);
        }
        public static void Click(GameObject go)
        {
            var hit=go.GetComponent<Collider>();Vector3 point=hit?hit.bounds.center:go.transform.position;
            var eventData=new PointerEventData(EventSystem.current){position=Camera.main.WorldToScreenPoint(point),button=PointerEventData.InputButton.Left,pointerId=-1,pointerCurrentRaycast=new RaycastResult{gameObject=go,worldPosition=point},pointerPressRaycast=new RaycastResult{gameObject=go,worldPosition=point}};
            ExecuteEvents.Execute(go,eventData,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(go,eventData,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(go,eventData,ExecuteEvents.pointerClickHandler);
        }
    }
}
