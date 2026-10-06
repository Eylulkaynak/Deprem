// Editor-only screen input and rendered-pose integration checks.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Immersion Fire And Grip")]
        static void ImmersionFireQA()=>StartPhysicalCheck(ImmersionFireSequence(),"immersion-fire-grip-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        static (Vector2,RaycastResult) ImmersionHit(GameObject handler,Vector3 point)
        {
            var screen=Camera.main.WorldToScreenPoint(point);var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
            var first=hits.Count>0?ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject):null;
            var drag=hits.Count>0?ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject):null;
            if(screen.z<=0||screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height||(first!=handler&&drag!=handler))
                throw new InvalidOperationException("Unreachable "+handler.name+" screen="+screen+" hit="+(hits.Count>0?hits[0].gameObject.name:"NONE"));
            File.AppendAllText(physicalCheckReport,"PASS screen "+Screen.width+"x"+Screen.height+" "+handler.name+" at "+screen+"\n");
            return(screen,hits[0]);
        }
        static IEnumerable<object> ImmersionFireSequence()
        {
            string folder="ClientExports/YanYana/Screenshots/immersion-fire-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);
            var old=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Fresh fire test"))yield return f;
            FireFixture();foreach(var f in WaitPhysical(()=>ReadyIn("hose"),"Hose ready"))yield return f;
            foreach(int height in new[]{960,1170,1200})
            {
                YanYanaQA.SetGameView(540,height);foreach(var f in FramesFor(.7f))yield return f;
                var plug=Find("Hortumun kavrama ucu");ImmersionHit(plug,plug.transform.position);
                ScreenCapture.CaptureScreenshot(folder+"/coupling-"+height+".png");foreach(var f in FramesFor(.2f))yield return f;
            }
            YanYanaQA.SetGameView(540,960);foreach(var f in FramesFor(.7f))yield return f;
            var root=Find("İdil’in hortum bağlantısı");foreach(var f in TimedDrag(Find("Hortumun kavrama ucu"),root.transform.position+new Vector3(0,.02f,.026f)))yield return f;
            if(State("HoseConnected")!=1)throw new InvalidOperationException("Coupling failed");
            var valve=Find("Su vanası");ImmersionHit(valve,valve.transform.position);foreach(var f in TimedDrag(valve,valve.transform.position+Vector3.forward*.25f))yield return f;
            var left=new List<float>();var right=new List<float>();float maxWaterGap=0,maxFootError=0,maxPlantedDrift=0,maxPlantedRotation=0,maxSteps=0;int footSamples=0;
            var animator=Find("Idil").GetComponentInChildren<Animator>();
            for(int stage=1;stage<=3;stage++)
            {
                foreach(var f in WaitPhysical(()=>ReadyIn("fire"+stage)&&Find("Hortumu yönlendir "+stage).activeInHierarchy,"Fire "+stage))yield return f;
                var surface=Find("Hortumu yönlendir "+stage);
                var previousFeet=new Vector3[2];var previousRotation=new Quaternion[2];var previousPlanted=new bool[2];int previousFootFrame=-1;
                if(stage==1){YanYanaFirePresentation.ReviewConnectedSleeves();YanYanaFirePresentation.ReviewPose();}
                foreach(int height in new[]{960,1170,1200})
                {
                    YanYanaQA.SetGameView(540,height);foreach(var f in FramesFor(.7f))yield return f;
                    for(int target=0;target<3;target++)ImmersionHit(surface,Find("Alev odağı "+stage+" "+target).GetComponent<Collider>().bounds.center);
                    ScreenCapture.CaptureScreenshot(folder+"/fire"+stage+"-"+height+".png");foreach(var f in FramesFor(.15f))yield return f;
                }
                YanYanaQA.SetGameView(540,960);foreach(var f in FramesFor(.7f))yield return f;
                if(stage==1)
                {
                    PausePhysical();foreach(var f in FramesFor(.15f))yield return f;float frozen=Shader.GetGlobalFloat("_DepremFxTime");
                    foreach(var f in FramesFor(.65f))yield return f;
                    if(Shader.GetGlobalFloat("_DepremFxTime")!=frozen)throw new InvalidOperationException("Fire shader continued while paused");
                    ResumePhysical();foreach(var f in FramesFor(.4f))yield return f;
                    if(Shader.GetGlobalFloat("_DepremFxTime")<=frozen)throw new InvalidOperationException("Fire shader did not resume");
                    File.AppendAllText(physicalCheckReport,"PASS fire shader freezes and resumes\n");
                }
                if(stage==1)YanYanaDeliveryTools.StartRecording();
                for(int target=0;target<3;target++)
                {
                    var collider=Find("Alev odağı "+stage+" "+target).GetComponent<Collider>();var hit=ImmersionHit(surface,collider.bounds.center);
                    var pointer=ReunionPointer(hit.Item1,hit.Item2,surface);pointer.pointerId=17;ExecuteEvents.Execute(surface,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(surface,pointer,ExecuteEvents.beginDragHandler);
                    double start=EditorApplication.timeSinceStartup;float playStart=Time.time;bool captured=false,turnChecked=false;
                    while(collider.enabled&&State("FireStage")==stage)
                    {
                        if(EditorApplication.timeSinceStartup-start>14)throw new InvalidOperationException("Fire did not respond to screen input");
                        pointer.position=Camera.main.WorldToScreenPoint(collider.bounds.center);ExecuteEvents.Execute(surface,pointer,ExecuteEvents.dragHandler);
                        if(Time.frameCount!=previousFootFrame)
                        {
                            var vars=Variables.Object(Flow);maxSteps=Mathf.Max(maxSteps,Convert.ToSingle(vars.Get("IdilFootSteps")));
                            for(int index=0;index<2;index++)
                            {
                                string side=index==0?"Left":"Right";var foot=animator.GetBoneTransform(index==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                                bool planted=Convert.ToBoolean(vars.Get("Idil"+side+"FootPlanted"));
                                maxFootError=Mathf.Max(maxFootError,Convert.ToSingle(vars.Get("Idil"+side+"FootError")));
                                if(Time.frameCount==previousFootFrame+1&&planted&&previousPlanted[index])
                                {maxPlantedDrift=Mathf.Max(maxPlantedDrift,Vector3.Distance(foot.position,previousFeet[index]));maxPlantedRotation=Mathf.Max(maxPlantedRotation,Quaternion.Angle(foot.rotation,previousRotation[index]));footSamples++;}
                                previousFeet[index]=foot.position;previousRotation[index]=foot.rotation;previousPlanted[index]=planted;
                            }
                            previousFootFrame=Time.frameCount;
                        }
                        if(!turnChecked&&Time.time-playStart>.18f)
                        {
                            var turning=YanYanaFirePresentation.CheckHoseBodyClearance();turnChecked=true;
                            File.AppendAllText(physicalCheckReport,"TURN BODY AND ARM CLEARANCE "+stage+"/"+target+" nozzle="+turning.nozzle+" hose="+turning.hose+" armNozzle="+turning.armNozzle+" armHose="+turning.armHose+"\n");
                            if(turning.nozzle<.005f||turning.hose<.005f||turning.armNozzle<.005f||turning.armHose<.005f)
                            {YanYanaFirePresentation.ReviewPose();throw new InvalidOperationException("Hose or nozzle intersects body or arm while turning.");}
                        }
                        if(Time.time-playStart>.35f)
                        {
                            left.Add(Convert.ToSingle(Variables.Object(Flow).Get("IdilLeftGripError")));right.Add(Convert.ToSingle(Variables.Object(Flow).Get("IdilRightGripError")));
                            var stream=UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).First(l=>l.name=="Kesintisiz su akışı"&&l.enabled);maxWaterGap=Mathf.Max(maxWaterGap,Vector3.Distance(stream.GetPosition(0),Find("Hortum su çıkışı").transform.position));
                        }
                        if(!captured&&Time.time-playStart>1)
                        {
                            var presentation=collider.transform.Find("Yangın sunumu");
                            var tongues=presentation.Find("Alev dilleri");
                            var smoke=presentation.Find("Duman yukarıda dağılır");
                            if(tongues.localScale.y>=.9f||smoke.localScale!=Vector3.one||!presentation.Find("Hedefte kalan ıslak iz").gameObject.activeSelf)
                                throw new InvalidOperationException("Partial suppression did not reduce flame independently or leave a wet contact mark.");
                            var impact=(ParticleSystem)new SerializedObject(Find("Gerçek müdahale alanı "+stage).GetComponent<Deprem.Minigames.FirefighterExtinguishManager>()).FindProperty("waterImpact").objectReferenceValue;
                            var jet=impact.transform.Find("Basınçlı su damlaları").GetComponent<ParticleSystem>();
                            if(!jet.isEmitting||jet.particleCount==0||impact.particleCount==0)
                                throw new InvalidOperationException("Pressure jet or impact splash did not emit during held screen input.");
                            if(jet.main.simulationSpace!=ParticleSystemSimulationSpace.World||jet.main.startSpeed.constant<18||jet.shape.angle>2)
                                throw new InvalidOperationException("Pressure droplets must travel through world space as a narrow high-speed jet.");
                            var clearance=YanYanaFirePresentation.CheckHoseBodyClearance();
                            File.AppendAllText(physicalCheckReport,"BODY AND ARM CLEARANCE "+stage+"/"+target+" nozzle="+clearance.nozzle+" hose="+clearance.hose+" armNozzle="+clearance.armNozzle+" armHose="+clearance.armHose+"\n");
                            YanYanaFirePresentation.ReviewConnectedSleeves();YanYanaFirePresentation.ReviewPose();
                            foreach(var file in Directory.GetFiles("ClientExports/YanYana/Screenshots/fire-pose-review","grip-*.png"))
                                File.Copy(file,folder+"/"+stage+"-"+target+"-"+Path.GetFileName(file),true);
                            if(clearance.nozzle<.005f||clearance.hose<.005f||clearance.armNozzle<.005f||clearance.armHose<.005f)
                                throw new InvalidOperationException("Nozzle or supply hose intersects the visible body or arm cloth surface.");
                            ScreenCapture.CaptureScreenshot(folder+"/spray"+stage+"-"+target+".png");captured=true;
                        }
                        yield return null;
                    }
                    ExecuteEvents.Execute(surface,pointer,ExecuteEvents.pointerUpHandler);
                    int releasedFrame=Time.frameCount;
                    foreach(var f in WaitPhysical(()=>Time.frameCount>=releasedFrame+2,"Pointer release reaches player frames",5))yield return f;
                    foreach(var f in FramesFor(.12f))yield return f;
                    if(target<2)
                    {
                        var presentation=collider.transform.Find("Yangın sunumu");
                        if(!presentation.gameObject.activeInHierarchy||!presentation.Find("Hedefte kalan ıslak iz").gameObject.activeSelf||presentation.GetComponentsInChildren<ParticleSystem>().Any(ps=>ps.isEmitting))
                            throw new InvalidOperationException("Extinguished target lost its scenery or continued emitting fire/smoke.");
                        var manager=Find("Gerçek müdahale alanı "+stage).GetComponent<Deprem.Minigames.FirefighterExtinguishManager>();
                        var stream=(LineRenderer)new SerializedObject(manager).FindProperty("waterStream").objectReferenceValue;
                        var impact=(ParticleSystem)new SerializedObject(manager).FindProperty("waterImpact").objectReferenceValue;
                        if(stream.enabled||impact.GetComponentsInChildren<ParticleSystem>().Any(ps=>ps.isEmitting))
                            throw new InvalidOperationException("Water did not stop: Holding="+Variables.Object(surface).Get("Holding")+" Finger="+Variables.Object(surface).Get("Finger")+" stream="+stream.enabled+" emitting="+string.Join(",",impact.GetComponentsInChildren<ParticleSystem>().Where(ps=>ps.isEmitting).Select(ps=>ps.name)));
                        ScreenCapture.CaptureScreenshot(folder+"/cooled"+stage+"-"+target+".png");
                    }
                    File.AppendAllText(physicalCheckReport,"PASS extinguished "+stage+"/"+target+" through validated screen input; flame shrinks separately, wet fuel remains, emission stops\n");
                }
                if(stage==1)YanYanaDeliveryTools.StopRecording();
            }
            foreach(var f in WaitPhysical(()=>State("FireCompleted")==1,"All three groups complete"))yield return f;
            left.Sort();right.Sort();File.AppendAllText(physicalCheckReport,"samples="+left.Count+" leftMax="+left.Max()+" leftP95="+left[(int)(left.Count*.95f)]+" rightMax="+right.Max()+" rightP95="+right[(int)(right.Count*.95f)]+" waterStartGap="+maxWaterGap+"\nScreenshots="+folder+"\n");
            File.AppendAllText(physicalCheckReport,"FOOT IK plantedSamples="+footSamples+" ankleError="+maxFootError+" plantedDrift="+maxPlantedDrift+" plantedRotation="+maxPlantedRotation+" steps="+maxSteps+"\n");
            if(footSamples<30||maxFootError>.008f||maxPlantedDrift>.003f||maxPlantedRotation>.5f||maxSteps<2)
                throw new InvalidOperationException("Foot IK lost planted contact or failed to replant while turning.");
            if(left.Max()>.012f||right.Max()>.012f||maxWaterGap>.001f)throw new InvalidOperationException("Visible hand or nozzle-stream separation exceeded tolerance");
        }
    }
}
