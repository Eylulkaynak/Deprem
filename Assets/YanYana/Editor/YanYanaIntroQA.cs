// Editor-only checks using production movement and EventSystem gestures, without decision assignment.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static readonly string IntroReport="ClientExports/YanYana/Reports/intro-geometry.txt";
        [MenuItem("Tools/Yan Yana/QA/Inspect Intro")]
        static void InspectIntro()
        {
            var lines=new List<string>{"PLAY="+EditorApplication.isPlaying};
            foreach(var value in Variables.Object(Flow))if(value.name.StartsWith("Intro")||value.name=="Workspace"||value.name=="Busy"||value.name=="Paused")lines.Add(value.name+"="+value.value);
            foreach(string name in new[]{"Ada","Efe","Efe’nin taşınabilir oyun kutusu","Oyun kutusunun alçak sehpası","Oyun kutusunun masadaki yeri"})
            {
                var obj=Find(name);lines.Add(name+" pos="+obj.transform.position+" yaw="+obj.transform.eulerAngles.y);
                foreach(var col in obj.GetComponents<Collider>())lines.Add(" collider="+col.bounds);
                var nav=obj.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav&&nav.isOnNavMesh)lines.Add(" nav: path="+nav.hasPath+" pending="+nav.pathPending+" remaining="+nav.remainingDistance+" velocity="+nav.velocity+" destination="+nav.destination);
            }
            File.WriteAllLines(IntroReport,lines);Capture();
        }
        static IEnumerable<object> ScreenDrag(GameObject obj,Vector2 offset,float seconds=.65f)
        {
            Vector2 a=Camera.main.WorldToScreenPoint(obj.transform.position),b=a+offset;
            var data=new PointerEventData(EventSystem.current){position=a,pressPosition=a,pointerId=-1,button=PointerEventData.InputButton.Left,pointerDrag=obj,pointerPress=obj,pointerCurrentRaycast=new RaycastResult{gameObject=obj,worldPosition=obj.transform.position}};
            ExecuteEvents.Execute(obj,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(obj,data,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(obj,data,ExecuteEvents.beginDragHandler);data.dragging=true;
            double start=EditorApplication.timeSinceStartup;while(EditorApplication.timeSinceStartup-start<seconds){var next=Vector2.Lerp(a,b,Mathf.Clamp01((float)(EditorApplication.timeSinceStartup-start)/seconds));data.delta=next-data.position;data.position=next;ExecuteEvents.Execute(obj,data,ExecuteEvents.dragHandler);yield return null;}
            data.delta=b-data.position;data.position=b;ExecuteEvents.Execute(obj,data,ExecuteEvents.dragHandler);ExecuteEvents.Execute(obj,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(obj,data,ExecuteEvents.endDragHandler);data.dragging=false;
        }
        static void IntroWalk(Vector3 point)=>Find("Ada").GetComponent<StoryPlayerMovement>().TrySetDestination(point);
        [MenuItem("Tools/Yan Yana/QA/Physical Intro Pickup")]
        static void IntroPickupQA()=>StartPhysicalCheck(IntroPickupSequence(true),"physical-intro-pickup");
        static IEnumerable<object> IntroPickupSequence(bool fresh)
        {
            if(fresh){var old=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Fresh intro"))yield return f;}
            IntroWalk(Find("Efe").transform.position+Vector3.back*.5f);foreach(var f in WaitPhysical(()=>State("IntroStage")==1,"Walk to and greet Efe"))yield return f;
            Click(Find("Efe’nin taşınabilir oyun kutusu"));foreach(var f in WaitPhysical(()=>ReadyIn("intro"),"Walk to low stand"))yield return f;
            foreach(var f in FramesFor(.3f))yield return f;Capture();foreach(var f in FramesFor(.25f))yield return f;
            foreach(var f in TimedDrag(Find("Efe’nin taşınabilir oyun kutusu"),Find("Kutuyu kavrama noktası").transform.position))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("IntroStage")==2,"Grip toy kit"))yield return f;InspectIntro();
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Intro Narrow Route")]
        static void IntroNarrowQA()=>StartPhysicalCheck(IntroCarrySequence(false,true),"physical-intro-narrow");
        [MenuItem("Tools/Yan Yana/QA/Physical Intro Wide Route")]
        static void IntroWideQA()=>StartPhysicalCheck(IntroCarrySequence(true,true),"physical-intro-wide");
        [MenuItem("Tools/Yan Yana/QA/Physical Intro Save Resume")]
        static void IntroSaveQA()=>StartPhysicalCheck(IntroSaveSequence(),"physical-intro-save");
        [MenuItem("Tools/Yan Yana/QA/Physical Intro Framing")]
        static void IntroFramingQA()=>StartPhysicalCheck(IntroFramingSequence(),"physical-intro-framing");
        static IEnumerable<object> IntroFramingSequence()
        {
            var old=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Fresh framing journey"))yield return f;
            IntroWalk(Find("Efe").transform.position+Vector3.back*.5f);foreach(var f in WaitPhysical(()=>State("IntroStage")==1,"Walk to Efe for framing"))yield return f;
            Click(Find("Efe’nin taşınabilir oyun kutusu"));foreach(var f in WaitPhysical(()=>ReadyIn("intro"),"Source close-up"))yield return f;
            var problems=new List<string>();foreach(var f in AuditIntroFraming("source",problems))yield return f;
            foreach(var f in TimedDrag(Find("Efe’nin taşınabilir oyun kutusu"),Find("Kutuyu kavrama noktası").transform.position))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("IntroStage")==2,"Carry view"))yield return f;
            foreach(var f in AuditIntroFraming("carry",problems))yield return f;
            YanYanaQA.SetGameView(540,960);foreach(var f in FramesFor(.8f))yield return f;
            foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
            {
                // Camera geometry probe using the existing facing API; no campaign decisions are assigned.
                var ada=Find("Ada");ada.GetComponent<StoryPlayerMovement>().FaceTowards(ada.transform.position+direction);foreach(var f in FramesFor(.8f))yield return f;
                var heldKit=Find("Efe’nin taşınabilir oyun kutusu");var results=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Camera.main.WorldToScreenPoint(heldKit.transform.position)},results);
                string firstHit=results.Count>0?results[0].gameObject.name:"NONE";File.AppendAllText(physicalCheckReport,"carry facing="+direction+" firstRaycast="+firstHit+"\n");
                if(results.Count==0||ExecuteEvents.GetEventHandler<IPointerDownHandler>(results[0].gameObject)!=heldKit)problems.Add("Carry facing "+direction+" occluded by "+firstHit);
            }
            IntroWalk(new Vector3(-.8f,0,-1.3f));foreach(var f in WaitPhysical(()=>Vector3.Distance(Find("Ada").transform.position,new Vector3(-.8f,0,-1.3f))<.23f,"Wide-route framing waypoint"))yield return f;
            IntroWalk(new Vector3(.8f,0,-1.3f));foreach(var f in WaitPhysical(()=>State("IntroRoute")==2,"Pass around cushions"))yield return f;
            IntroWalk(new Vector3(1.4f,0,-3.2f));foreach(var f in WaitPhysical(()=>ReadyIn("introDrop"),"Drop close-up"))yield return f;
            foreach(var f in AuditIntroFraming("drop",problems))yield return f;
            YanYanaQA.SetGameView(540,960);foreach(var f in FramesFor(1f))yield return f;
            File.AppendAllText(physicalCheckReport,string.Join("\n",problems)+"\n");
            if(problems.Count>0)throw new InvalidOperationException("Intro framing/production raycast has "+problems.Count+" issue(s).");
            foreach(var f in ScreenDrag(Find("Efe’nin taşınabilir oyun kutusu"),new Vector2(0,-Screen.width*.17f)))yield return f;
            foreach(var f in WaitPhysical(()=>State("IntroDone")==1&&ReadyIn(""),"Drop after portrait-ratio checks"))yield return f;
        }
        static IEnumerable<object> AuditIntroFraming(string view,List<string> problems)
        {
            foreach(int height in new[]{960,1170,1200})
            {
                YanYanaQA.SetGameView(540,height);foreach(var f in FramesFor(1.1f))yield return f;
                var kit=Find("Efe’nin taşınabilir oyun kutusu");Vector2 screen=Camera.main.WorldToScreenPoint(kit.transform.position);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
                string hit=hits.Count==0?"NONE":hits[0].gameObject.name;File.AppendAllText(physicalCheckReport,view+" "+Screen.width+"x"+Screen.height+" kit="+screen+" firstRaycast="+hit+"\n");
                if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject)!=kit)problems.Add(view+" "+height+" kit center occluded by "+hit);
                if(view=="drop")foreach(string actor in new[]{"Ada","Efe"})
                {
                    var head=Find(actor).GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                    var crown=Camera.main.WorldToViewportPoint(head.position+Vector3.up*.15f);
                    File.AppendAllText(physicalCheckReport,actor+" upper head viewport="+crown.ToString("F3")+"\n");
                    if(crown.x<.04f||crown.x>.96f||crown.y<.28f||crown.y>.84f)problems.Add(actor+" head meets UI/screen edge at "+height);
                }
                foreach(var label in UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if(!label.isActiveAndEnabled)continue;label.ForceMeshUpdate();var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);foreach(var corner in corners)if(corner.x<-.5f||corner.x>Screen.width+.5f||corner.y<-.5f||corner.y>Screen.height+.5f){problems.Add(view+" "+height+" label outside viewport: "+label.name);break;}
                    if(label.preferredHeight>label.rectTransform.rect.height+3)problems.Add(view+" "+height+" text height: "+label.name);
                }
                ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/intro-"+view+"-"+height+".png");foreach(var f in FramesFor(.2f))yield return f;
            }
        }
        static IEnumerable<object> IntroSaveSequence()
        {
            foreach(var f in IntroPickupSequence(true))yield return f;
            var kit=Find("Efe’nin taşınabilir oyun kutusu");Vector2 screen=Camera.main.WorldToScreenPoint(kit.transform.position);
            var first=new PointerEventData(EventSystem.current){position=screen,pressPosition=screen,pointerId=81,dragging=true,pointerDrag=kit,pointerPress=kit};ExecuteEvents.Execute(kit,first,ExecuteEvents.pointerDownHandler);
            var other=new PointerEventData(EventSystem.current){position=screen+Vector2.right*Screen.width*.3f,pressPosition=screen,pointerId=82,dragging=true};ExecuteEvents.Execute(kit,other,ExecuteEvents.dragHandler);ExecuteEvents.Execute(kit,other,ExecuteEvents.endDragHandler);
            if(State("IntroOrientation")!=0||Convert.ToInt32(Variables.Object(Flow).Get("IntroPointer"))!=81)throw new InvalidOperationException("Second finger took the carried kit.");
            first.position+=Vector2.right*Screen.width*.09f;ExecuteEvents.Execute(kit,first,ExecuteEvents.dragHandler);PausePhysical();PausePhysical();yield return null;
            if((bool)Variables.Object(Flow).Get("IntroDragging")||Convert.ToSingle(Variables.Object(Flow).Get("IntroTurn"))!=0f)throw new InvalidOperationException("Pause retained partial turn.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>ReadyIn(""),"Resume partial turn"))yield return f;
            var pos=Find("Ada").transform.position;var old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("")&&State("IntroStage")==2,"Resume held kit after reload"))yield return f;
            if(Vector3.Distance(pos,Find("Ada").transform.position)>.08f||State("IntroDone")!=0)throw new InvalidOperationException("Reload lost carrying checkpoint.");
            File.AppendAllText(physicalCheckReport,"PASS second pointer rejected; partial turn cancelled; repeated pause and carried-item reload preserved position and decisions.\n");
            IntroWalk(new Vector3(-.8f,0,-1.3f));foreach(var f in WaitPhysical(()=>Vector3.Distance(Find("Ada").transform.position,new Vector3(-.8f,0,-1.3f))<.23f,"Walk wider route after reload"))yield return f;
            IntroWalk(new Vector3(.8f,0,-1.3f));foreach(var f in WaitPhysical(()=>State("IntroRoute")==2,"Record wider route"))yield return f;
            IntroWalk(new Vector3(1.4f,0,-3.2f));foreach(var f in WaitPhysical(()=>ReadyIn("introDrop"),"Reach drop after reload"))yield return f;
            kit=Find("Efe’nin taşınabilir oyun kutusu");foreach(var f in ScreenDrag(kit,new Vector2(0,-Screen.width*.17f)))yield return f;
            foreach(var f in FramesFor(.18f))yield return f;PausePhysical();PausePhysical();yield return null;
            if(State("IntroStage")!=2||State("IntroDone")!=0)throw new InvalidOperationException("Partial placement was incorrectly committed.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>ReadyIn("introDrop"),"Resume interrupted placement"))yield return f;old=Flow;ReloadPhysical();yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("introDrop")&&State("IntroStage")==2,"Reconstruct tabletop action after reload"))yield return f;
            kit=Find("Efe’nin taşınabilir oyun kutusu");foreach(var f in ScreenDrag(kit,new Vector2(0,-Screen.width*.17f)))yield return f;
            foreach(var f in WaitPhysical(()=>State("IntroDone")==1&&ReadyIn(""),"Retry placement successfully"))yield return f;
            old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("")&&State("IntroDone")==1,"Completed opening remains completed"))yield return f;
            if(State("IntroRoute")!=2||State("FamilyPlan")!=0||State("FlashlightReady")!=0)throw new InvalidOperationException("Checkpoint changed independent preparation decisions.");
            File.AppendAllText(physicalCheckReport,"PASS interrupted placement reconstructed; correct retry, route and completed-opening reload preserved; family plan and flashlight remain unprepared.\n");InspectIntro();
        }
        static IEnumerable<object> IntroCarrySequence(bool wide,bool fresh)
        {
            if(fresh||State("IntroStage")==0)foreach(var f in IntroPickupSequence(fresh))yield return f;
            var kit=Find("Efe’nin taşınabilir oyun kutusu");
            if(wide)
            {
                IntroWalk(new Vector3(-.8f,0,-1.3f));foreach(var f in WaitPhysical(()=>Vector3.Distance(Find("Ada").transform.position,new Vector3(-.8f,0,-1.3f))<.23f,"Walk around cushion gate"))yield return f;
                IntroWalk(new Vector3(.8f,0,-1.3f));foreach(var f in WaitPhysical(()=>State("IntroRoute")==2,"Wide route remains usable"))yield return f;
            }
            else
            {
                IntroWalk(new Vector3(-1.2f,0,-2.72f));foreach(var f in WaitPhysical(()=>Vector3.Distance(Find("Ada").transform.position,new Vector3(-1.2f,0,-2.72f))<.23f,"Line up at narrow passage"))yield return f;
                IntroWalk(new Vector3(.8f,0,-2.72f));foreach(var f in WaitPhysical(()=>(bool)Variables.Object(Flow).Get("IntroBlocked"),"Wide kit stops before cushions"))yield return f;
                if(Find("Ada").transform.position.x>-.4f)throw new InvalidOperationException("The wide kit entered the narrow gate before stopping.");InspectIntro();
                foreach(var f in ScreenDrag(kit,new Vector2(Screen.width*.25f,0)))yield return f;
                foreach(var f in WaitPhysical(()=>State("IntroOrientation")==1,"Turn kit lengthwise"))yield return f;
                IntroWalk(new Vector3(.8f,0,-2.72f));foreach(var f in WaitPhysical(()=>State("IntroRoute")==1,"Narrow route passes with turned kit"))yield return f;
            }
            IntroWalk(new Vector3(1.4f,0,-3.2f));foreach(var f in WaitPhysical(()=>ReadyIn("introDrop"),"Approach actual tabletop"))yield return f;
            if(!wide)
            {
                foreach(var f in ScreenDrag(kit,new Vector2(0,-Screen.width*.17f)))yield return f;
                if(State("IntroDone")!=0||State("IntroStage")!=2)throw new InvalidOperationException("Wrongly oriented kit was accepted.");
                foreach(var f in ScreenDrag(kit,new Vector2(-Screen.width*.25f,0)))yield return f;
                foreach(var f in WaitPhysical(()=>State("IntroOrientation")==0,"Align kit with tabletop mark"))yield return f;
            }
            InspectIntro();foreach(var f in FramesFor(.2f))yield return f;
            foreach(var f in ScreenDrag(kit,new Vector2(0,-Screen.width*.17f)))yield return f;
            foreach(var f in WaitPhysical(()=>State("IntroDone")==1&&ReadyIn(""),"Place kit and enter preparation"))yield return f;
            if(State("FamilyPlan")!=0||State("FlashlightReady")!=0)throw new InvalidOperationException("Intro completed unrelated preparation decisions.");InspectIntro();
        }
    }
}
