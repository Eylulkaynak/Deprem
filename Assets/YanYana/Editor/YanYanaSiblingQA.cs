// Editor-only interaction checks. Family entry fixtures are explicitly identified in reports.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Inspect Sibling Arm Geometry")]
        static void InspectSiblingArmGeometry()
        {
            var report=new List<string>();var solve=typeof(Deprem.Minigames.FirefighterExtinguishManager).GetMethod("SolveArm",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            foreach(string who in new[]{"Ada","Efe"})
            {
                var actor=Find(who);var animator=actor.GetComponentInChildren<Animator>();var upper=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
                report.Add(who+" root="+actor.transform.position+" upper="+upper.position+" lower="+lower.position+" hand="+hand.position+" lengths="+Vector3.Distance(upper.position,lower.position)+","+Vector3.Distance(lower.position,hand.position));
                for(var t=hand;t!=null;t=t.parent)report.Add("  "+t.name+" local="+t.localScale+" lossy="+t.lossyScale);
                var u=upper.localRotation;var l=lower.localRotation;
                foreach(var offset in new[]{new Vector3(.20f,-.08f,0),new Vector3(0,.18f,0),new Vector3(.10f,.10f,.30f)})
                {
                    upper.localRotation=u;lower.localRotation=l;var target=upper.position+offset;
                    for(int i=0;i<4;i++){solve.Invoke(null,new object[]{upper,lower,hand,target,new Vector3(-.8f,.25f,-.3f)});report.Add(" target="+target+" pass="+i+" distance="+Vector3.Distance(hand.position,target));}
                }
                upper.localRotation=u;lower.localRotation=l;
            }
            var table=Find("Görevlinin aile doğrulama masası");foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(Vector3.Distance(r.bounds.center,table.transform.position)<3.0f)report.Add("near table: "+r.name+" "+r.bounds);
            File.WriteAllLines("ClientExports/YanYana/Reports/sibling-arm-geometry.txt",report);
        }
        static IEnumerable<object> FramesFor(float seconds){double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
        [MenuItem("Tools/Yan Yana/QA/Physical Sibling Support Reload")]
        static void SiblingSupportQA()=>StartPhysicalCheck(SiblingSupportSequence(),"physical-sibling-support");
        static IEnumerable<object> SiblingSupportSequence()
        {
            var old=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Fresh adventure"))yield return f;
            foreach(var f in IntroCarrySequence(true,false))yield return f;
            BeginBridge();foreach(var f in WaitPhysical(()=>ReadyIn("bridge"),"Toy bridge"))yield return f;BridgeTest();foreach(var f in WaitPhysical(()=>State("Phase")==1&&ReadyIn(""),"Quake begins"))yield return f;
            foreach(var f in PhysicalCoverSequence())yield return f;
            Click(Find("Efe"));foreach(var f in WaitPhysical(()=>ReadyIn("sibling"),"Listen to Efe"))yield return f;Capture();
            foreach(var f in FramesFor(.6f))yield return f;
            var cue=Find("Efe’ye uzanan el");var target=(Vector3)Variables.Object(Flow).Get("SiblingTarget");
            foreach(var f in TimedDrag(cue,target+Vector3.up*1.1f))yield return f;
            if(State("SiblingChecked")!=0||State("SiblingSupported")!=0)throw new InvalidOperationException("An outside drop incorrectly completed sibling support.");
            var screen=(Vector2)Camera.main.WorldToScreenPoint(cue.transform.position);var first=new PointerEventData(EventSystem.current){position=screen,pressPosition=screen,pointerId=81,dragging=true,pointerDrag=cue,pointerPress=cue};
            ExecuteEvents.Execute(cue,first,ExecuteEvents.pointerDownHandler);var second=new PointerEventData(EventSystem.current){position=Camera.main.WorldToScreenPoint(target),pointerId=82,dragging=true};
            ExecuteEvents.Execute(cue,second,ExecuteEvents.dragHandler);ExecuteEvents.Execute(cue,second,ExecuteEvents.endDragHandler);
            if(State("SiblingChecked")!=0)throw new InvalidOperationException("Another pointer stole the sibling gesture.");
            first.position+=new Vector2(40,20);ExecuteEvents.Execute(cue,first,ExecuteEvents.dragHandler);PausePhysical();PausePhysical();yield return null;
            if((bool)Variables.Object(Flow).Get("SiblingDragging")||State("SiblingChecked")!=0)throw new InvalidOperationException("Pause retained the unfinished hand gesture.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>ReadyIn("sibling"),"Resume sibling gesture"))yield return f;old=Flow;ReloadPhysical();yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("sibling"),"Reload unfinished sibling gesture"))yield return f;
            cue=Find("Efe’ye uzanan el");target=(Vector3)Variables.Object(Flow).Get("SiblingTarget");foreach(var f in TimedDrag(cue,target,.50f))yield return f;
            foreach(var f in FramesFor(.15f))yield return f;Capture();YanYanaHandContactQA.Capture();var vars=Variables.Object(cue);float a=Convert.ToSingle(vars.Get("AdaContactError")),e=Convert.ToSingle(vars.Get("EfeContactError"));
            File.AppendAllText(physicalCheckReport,$"Measured hand contact Ada={a:F4}m Efe={e:F4}m target={target}\n");
            foreach(var who in new[]{"Ada","Efe"}){var actor=Find(who);var animator=actor.GetComponentInChildren<Animator>();bool right=who=="Ada";var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);File.AppendAllText(physicalCheckReport,$"{who} root={actor.transform.position} rotation={actor.transform.eulerAngles} upper={upper.position} lower={lower.position} hand={hand.position} targetDistance={Vector3.Distance(upper.position,target)} reach={Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position)}\n");}
            if(a>.025f||e>.025f)throw new InvalidOperationException("Hands do not reach the shared contact point.");
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("SiblingSupported")==1,"Support completed"))yield return f;
            old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Restore completed support"))yield return f;
            if(State("SiblingChecked")!=1||State("SiblingSupported")!=1)throw new InvalidOperationException("Support was not persisted.");
            File.AppendAllText(physicalCheckReport,"PASS actual toy/quake/three protection gestures; sibling outside-drop rejection, second-pointer rejection, double-pause, partial reload, physical hand contact, completed support reload. No sibling outcome assignments.\n");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Efe Family Contribution")]
        static void EfeFamilyQA()=>StartPhysicalCheck(EfeFamilySequence(),"physical-efe-family-contribution");
        static IEnumerable<object> EfeFamilySequence()
        {
            File.AppendAllText(physicalCheckReport,"ISOLATED ENTRY FIXTURE: family plan/support prerequisites and aid phase/location are supplied. Efe walking, gesture, marker completion and cancellation remain production graph actions.\n");
            Fixture(6,Find("Street_Aid").transform.position+Vector3.forward*.9f);var v=Variables.Object(Flow);v.Set("FamilyPlan",1);v.Set("SiblingSupported",1);v.Set("IdentityMarker",0);v.Set("IdentityPeople",0);v.Set("InfoVerified",0);v.Set("EvidenceOpen",0);v.Set("EfeContributed",0);v.Set("AidStage",2);CustomEvent.Trigger(Flow,"EnterAid");
            foreach(var f in WaitPhysical(()=>ReadyIn("family"),"Family contribution entry"))yield return f;Click(Find("Aile bilgisine başvur"));
            foreach(var f in WaitPhysical(()=>Convert.ToInt32(Variables.Object(Flow).Get("EfeHelpStage"))==1,"Efe starts walking"))yield return f;
            var before=Find("Efe").transform.position;foreach(var f in WaitPhysical(()=>Vector3.Distance(Find("Efe").transform.position,before)>.25f,"Efe really moves to desk"))yield return f;
            PausePhysical();yield return null;var held=Find("Efe").transform.position;foreach(var f in FramesFor(.4f))yield return f;
            if(Vector3.Distance(Find("Efe").transform.position,held)>.025f||State("IdentityMarker")!=0)throw new InvalidOperationException("Paused Efe moved or completed the marker.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>Convert.ToInt32(Variables.Object(Flow).Get("EfeHelpStage"))==2&&Convert.ToSingle(Variables.Object(Flow).Get("EfeHelpProgress"))>.8f,"Efe physically places marker"))yield return f;
            ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/physical_efe_places_marker.png");foreach(var f in FramesFor(.12f))yield return f;float error=Convert.ToSingle(Variables.Object(Flow).Get("EfeCardContactError"));File.AppendAllText(physicalCheckReport,$"Measured card contact {error:F4}m\n");if(error>.025f)throw new InvalidOperationException("Efe's hand does not reach the card.");
            PausePhysical();yield return null;if(State("IdentityMarker")!=0)throw new InvalidOperationException("Incomplete placement was committed on pause.");
            ResumePhysical();var old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&Convert.ToInt32(Variables.Object(Flow).Get("EfeHelpStage"))==5,"Reconstruct small action and return camera after reload"))yield return f;
            var earlyTable=Find("Görevlinin aile doğrulama masası").transform.position;foreach(var f in TimedDrag(Find("Bilgi parçası 0 0"),earlyTable+new Vector3(-.27f,.06f,.03f),.12f))yield return f;
            if(State("IdentityPeople")!=0)throw new InvalidOperationException("Card accepted during camera return.");PausePhysical();PausePhysical();yield return null;ResumePhysical();
            foreach(var f in WaitPhysical(()=>State("EfeContributed")==1&&ReadyIn("family"),"Camera return resumes before card input"))yield return f;
            File.AppendAllText(physicalCheckReport,"PASS camera return rejects early card input and resumes its input gate after double pause.\n");
            if(State("IdentityMarker")!=1||State("IdentityPeople")!=0||Find("Bilgi parçası 1 0").activeSelf)throw new InvalidOperationException("Efe did not complete only the learned marker.");
            foreach(var f in FramesFor(1.1f))yield return f;Capture();foreach(var f in FramesFor(.6f))yield return f;old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("family"),"Reload completed marker"))yield return f;
            if(State("EfeContributed")!=1||State("IdentityMarker")!=1||!Find("Efe’nin hatırladığı aile kartı").activeSelf)throw new InvalidOperationException("Completed contribution failed to restore.");
            File.AppendAllText(physicalCheckReport,"Before remaining card: stage="+Variables.Object(Flow).Get("EfeHelpStage")+" busy="+Variables.Object(Flow).Get("Busy")+" blend="+Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>().IsBlending+"\n");
            var table=Find("Görevlinin aile doğrulama masası").transform.position;foreach(var f in TimedDrag(Find("Bilgi parçası 0 0"),table+new Vector3(-.27f,.06f,.03f)))yield return f;
            if(State("IdentityPeople")!=1){Capture();throw new InvalidOperationException("Remaining card did not match after completed-marker reload; stage="+Variables.Object(Flow).Get("EfeHelpStage")+" blend="+Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>().IsBlending);}
            foreach(var f in WaitPhysical(()=>State("InfoVerified")==1,"Player matches remaining people evidence"))yield return f;
            File.AppendAllText(physicalCheckReport,"PASS real Efe walk, pause while walking, physical card placement, pause while placing, reload partial/completed, player retains independent family portrait action.\n");
        }
    }
}
