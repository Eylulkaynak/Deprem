// Editor-only regression checks exercising production interactions, cancellation and reload.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static IEnumerator<object> physicalCheck;
        static bool physicalCheckRunning;
        static string physicalCheckReport;
        static void StartPhysicalCheck(IEnumerable<object> sequence,string report)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter the adventure in Play Mode first.");
            if(physicalCheckRunning||recordedRouteRunning)throw new InvalidOperationException("Finish or stop the active integration run before starting another.");physicalCheckRunning=true;
            physicalCheckReport="ClientExports/YanYana/Reports/"+report+".txt";File.WriteAllText(physicalCheckReport,"Editor integration; production EventSystem handlers. Not physical-phone or child usability evidence.\n");
            physicalCheck=sequence.GetEnumerator();EditorApplication.update-=PhysicalCheckTick;EditorApplication.update+=PhysicalCheckTick;
        }
        static void PhysicalCheckTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=PhysicalCheckTick;physicalCheckRunning=false;return;}
            try{if(!physicalCheck.MoveNext()){EditorApplication.update-=PhysicalCheckTick;physicalCheckRunning=false;File.AppendAllText(physicalCheckReport,"PASS complete\n");Inspect();}}
            catch(Exception e){EditorApplication.update-=PhysicalCheckTick;physicalCheckRunning=false;File.AppendAllText(physicalCheckReport,"FAIL "+e+"\n");Inspect();Debug.LogException(e);}
        }
        static IEnumerable<object> WaitPhysical(Func<bool> condition,string name,double seconds=25)
        {
            double start=EditorApplication.timeSinceStartup;while(!condition()){if(EditorApplication.timeSinceStartup-start>seconds)throw new InvalidOperationException(name+" timeout; phase="+State("Phase")+" workspace="+Variables.Object(Flow).Get("Workspace")+" busy="+Variables.Object(Flow).Get("Busy"));yield return null;}
        }
        static IEnumerable<object> PhysicalCoverSequence()
        {
            foreach (var frame in InteractiveCoverSequence()) yield return frame;
            File.AppendAllText("ClientExports/YanYana/Reports/physical-cover-gestures.txt", "PASS continuous crouch, sustained hand-to-head and hand-to-table drags, fresh held grip and protected completion. No outcome assignments.\n");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Cover Three Steps")]
        static void CoverSteps()=>StartPhysicalCheck(PhysicalCoverSequence(),"physical-cover-check");
        [MenuItem("Tools/Yan Yana/QA/Physical Family Cancellation Reload")]
        static void FamilyResilience()=>StartPhysicalCheck(FamilyResilienceSequence(),"physical-family-cancellation");
        static IEnumerable<object> FamilyResilienceSequence()
        {
            foreach(var f in WaitPhysical(()=>ReadyIn("family"),"Family workspace ready"))yield return f;
            Click(Find("Aile bilgisine başvur"));var at=Find("Görevlinin aile doğrulama masası").transform.position;
            var people=Find("Bilgi parçası 0 0");var slot=at+new Vector3(-.27f,.06f,.03f);
            foreach(var f in TimedDrag(people,slot))yield return f;
            if(State("IdentityPeople")!=1)throw new InvalidOperationException("First clue did not match.");
            foreach(var f in TimedDrag(people,at+Vector3.right*.65f))yield return f;
            if(Vector3.Distance(people.transform.position,slot)>.01f)throw new InvalidOperationException("A completed identity clue could be displaced.");
            File.AppendAllText(physicalCheckReport,"PASS completed clue remains locked\n");
            var marker=Find("Bilgi parçası 1 0");var rest=marker.transform.position;var screen=(Vector2)Camera.main.WorldToScreenPoint(rest);
            var data=new PointerEventData(EventSystem.current){position=screen,pressPosition=screen,pointerId=-1,pointerDrag=marker,pointerPress=marker,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(marker,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(marker,data,ExecuteEvents.beginDragHandler);data.position+=new Vector2(55,45);ExecuteEvents.Execute(marker,data,ExecuteEvents.dragHandler);
            if(Vector3.Distance(marker.transform.position,rest)<.01f)throw new InvalidOperationException("Partial drag was not exercised.");
            PausePhysical();PausePhysical();yield return null;
            if(Vector3.Distance(marker.transform.position,rest)>.01f||Vector3.Distance(people.transform.position,slot)>.01f||Time.timeScale!=0)throw new InvalidOperationException("Pause failed to restore incomplete drag while retaining solved clue.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>ReadyIn("family"),"Resume"))yield return f;
            var previousFlow=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previousFlow&&ReadyIn("family"),"Reload family"))yield return f;
            if(State("IdentityPeople")!=1||State("IdentityMarker")!=0)throw new InvalidOperationException("Reload did not preserve exactly the completed clue.");
            File.AppendAllText(physicalCheckReport,"PASS double pause, incomplete drag cancellation and scene reload retain completed clue only\n");
            at=Find("Görevlinin aile doğrulama masası").transform.position;foreach(var f in TimedDrag(Find("Bilgi parçası 1 0"),at+new Vector3(.27f,.06f,.03f)))yield return f;
            foreach(var f in WaitPhysical(()=>State("InfoVerified")==1,"Finish after reload"))yield return f;
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Aid Walk Pause")]
        static void AidWalkPause()=>StartPhysicalCheck(AidWalkPauseSequence(),"physical-aid-walk-pause");
        static IEnumerable<object> AidWalkPauseSequence()
        {
            foreach(var f in WaitPhysical(()=>ReadyIn("aid"),"Aid workspace"))yield return f;
            var aid=Find("Street_Aid").transform.position;var destinations=new[]{aid+new Vector3(-3,1.75f,.05f),aid+new Vector3(0,1.75f,-1.4f),aid+new Vector3(3,1.75f,.05f)};
            foreach(var f in TimedDrag(Find("Taşınan ihtiyaç işareti 0"),destinations[0]))yield return f;
            var visitor=Find("Yardım bekleyen komşu 0");var before=visitor.transform.position;PausePhysical();PausePhysical();yield return null;
            if(Vector3.Distance(before,visitor.transform.position)>.03f)throw new InvalidOperationException("Pause teleported a walking visitor.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>Vector3.Distance(before,visitor.transform.position)>.25f,"Visitor resumes walking"))yield return f;
            for(int i=1;i<3;i++)foreach(var f in TimedDrag(Find("Taşınan ihtiyaç işareti "+i),destinations[i]))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("relief"),"All visitors arrive before camera changes"))yield return f;
            File.AppendAllText(physicalCheckReport,"PASS walking visitor stays in place during double pause and resumes path; all arrive before next activity\n");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Fire Pointer Save Reload")]
        static void FireSaveReload()=>StartPhysicalCheck(FireSaveReloadSequence(),"physical-fire-save-reload");
        static IEnumerable<object> FireSaveReloadSequence()
        {
            foreach(var f in WaitPhysical(()=>ReadyIn("fire1")&&Find("Hortumu yönlendir 1").activeInHierarchy,"First fire workspace"))yield return f;
            double settled=EditorApplication.timeSinceStartup;while(EditorApplication.timeSinceStartup-settled<.25)yield return null;
            var surface=Find("Hortumu yönlendir 1");var target=Find("Alev odağı 1 0").GetComponent<Collider>();var position=(Vector2)Camera.main.WorldToScreenPoint(target.bounds.center);
            var primary=new PointerEventData(EventSystem.current){position=position,pressPosition=position,pointerId=-1,pointerPress=surface,pointerDrag=surface,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(surface,primary,ExecuteEvents.pointerDownHandler);var aim=(Vector3)Variables.Object(surface).Get("Aim");int hitIndex=(int)Variables.Object(surface).Get("Target");if(hitIndex<0)throw new InvalidOperationException("The initial ray did not hit a fire target.");
            var second=new PointerEventData(EventSystem.current){position=position+new Vector2(120,30),pressPosition=position,pointerId=42,pointerPress=surface,pointerDrag=surface,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(surface,second,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(surface,second,ExecuteEvents.dragHandler);ExecuteEvents.Execute(surface,second,ExecuteEvents.pointerUpHandler);
            if((int)Variables.Object(surface).Get("Finger")!=-1||Vector3.Distance(aim,(Vector3)Variables.Object(surface).Get("Aim"))>.01f||!(bool)Variables.Object(surface).Get("Holding"))throw new InvalidOperationException("Secondary touch stole the hose.");
            double show=EditorApplication.timeSinceStartup;while(EditorApplication.timeSinceStartup-show<1.4)yield return null;Capture();
            foreach(var f in WaitPhysical(()=>State("FireDone1_"+hitIndex)==1,"One target actually extinguished"))yield return f;ExecuteEvents.Execute(surface,primary,ExecuteEvents.pointerUpHandler);
            var previous=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn("fire1"),"Reload partial fire group"))yield return f;
            var manager=Find("Gerçek müdahale alanı 1").GetComponent<Deprem.Minigames.FirefighterExtinguishManager>();
            if(Find("Alev odağı 1 "+hitIndex).GetComponent<Collider>().enabled||manager.RemainingFireCount!=2||State("FireDone1_"+hitIndex)!=1)throw new InvalidOperationException("A completed fire target returned after reload.");
            File.AppendAllText(physicalCheckReport,"PASS secondary touch cannot steal primary hose; actual extinguished target survives reload, two targets remain\n");
            PointerSpray();foreach(var f in WaitPhysical(()=>State("FireStage")==2,"Finish remaining targets after reload",40))yield return f;
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Temporal Replay")]
        static void TemporalReplay()=>StartPhysicalCheck(TemporalReplaySequence(),"physical-temporal-replay");
        static IEnumerable<object> TemporalReplaySequence()
        {
            var previous=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn(""),"New replay test"))yield return f;
            foreach(var f in IntroCarrySequence(true,false))yield return f;
            Map();foreach(var f in WaitPhysical(()=>ReadyIn("map"),"Map"))yield return f;MapTest();Back();foreach(var f in WaitPhysical(()=>ReadyIn(""),"Leave map"))yield return f;
            Fener();foreach(var f in WaitPhysical(()=>ReadyIn("flashlight"),"Flashlight"))yield return f;BatteryTest();Back();foreach(var f in WaitPhysical(()=>ReadyIn(""),"Leave flashlight"))yield return f;
            Radio();foreach(var f in WaitPhysical(()=>ReadyIn("radio"),"Radio"))yield return f;RadioTest();Back();foreach(var f in WaitPhysical(()=>ReadyIn(""),"Leave radio"))yield return f;
            string prefix="Deprem.YanYana.v1.snapshot.";
            if(State("FamilyPlan")!=1||State("FlashlightReady")!=1||State("RadioReady")!=1)throw new InvalidOperationException("The three decisions were not actually completed.");
            previous=Flow;CustomEvent.Trigger(Flow,"ReplayPhysical","flashlight");yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn("flashlight"),"Replay middle decision"))yield return f;
            if(State("FamilyPlan")!=1||State("FlashlightReady")!=0||State("RadioReady")!=0||PlayerPrefs.GetInt(prefix+"map.exists")!=1||PlayerPrefs.GetInt(prefix+"flashlight.exists")!=1||PlayerPrefs.GetInt(prefix+"radio.exists")!=0)throw new InvalidOperationException("Replay did not retain earlier decision and invalidate only later snapshots.");
            File.AppendAllText(physicalCheckReport,"PASS real order map > flashlight > radio; replay flashlight retains family plan, resets flashlight and later radio, invalidates only later snapshot\n");
        }
    }
}
