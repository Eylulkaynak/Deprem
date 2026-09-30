// Editor integration checks dispatch normal production pointer handlers.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical Backpack Trial")]
        static void BackpackTrial()=>StartPhysicalCheck(BackpackTrialSequence(),"physical-backpack-trial");
        static IEnumerable<object> BackpackTrialSequence()
        {
            var previous=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn(""),"Fresh backpack test"))yield return f;
            foreach(var f in IntroCarrySequence(true,false))yield return f;
            Bag();foreach(var f in WaitPhysical(()=>ReadyIn("bag"),"Walk to packing bench"))yield return f;
            Click(Find("Çantayı kapatma tokası"));foreach(var f in WaitPhysical(()=>ReadyIn("bagfit"),"Open zipper inspection"))yield return f;Capture();
            foreach(var renderer in Find("Ada").GetComponentsInChildren<Renderer>())if(renderer.enabled)throw new InvalidOperationException("Ada occludes the packing close-up.");
            var slider=Find("Çantanın sürüklenen fermuarı");
            foreach(var f in TimedDrag(slider,Find("BagZipWaypoint6").transform.position))yield return f;
            if(State("BagZipStep")!=0||State("BagClosed")!=0)throw new InvalidOperationException("Zipper shortcut skipped the track.");
            for(int i=1;i<=2;i++)foreach(var f in TimedDrag(slider,Find("BagZipWaypoint"+i).transform.position))yield return f;
            if(State("BagZipStep")!=2)throw new InvalidOperationException("Actual zipper track did not advance.");
            var rest=slider.transform.position;var screen=(Vector2)Camera.main.WorldToScreenPoint(rest);
            var data=new PointerEventData(EventSystem.current){position=screen,pressPosition=screen,pointerId=-1,pointerDrag=slider,pointerPress=slider,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(slider,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(slider,data,ExecuteEvents.beginDragHandler);
            PausePhysical();PausePhysical();yield return null;ResumePhysical();foreach(var f in WaitPhysical(()=>ReadyIn("bagfit"),"Release interrupted zipper"))yield return f;
            if((bool)Variables.Object(Flow).Get("Dragging")||State("BagZipStep")!=2)throw new InvalidOperationException("Pause committed or held the zipper.");
            previous=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn("bagfit"),"Restore zipper segments"))yield return f;
            if(State("BagZipStep")!=2||Vector3.Distance(Find("Çantanın sürüklenen fermuarı").transform.position,Find("BagZipWaypoint2").transform.position)>.01f)throw new InvalidOperationException("Zipper restore lost completed segments.");
            for(int i=3;i<=6;i++)foreach(var f in TimedDrag(Find("Çantanın sürüklenen fermuarı"),Find("BagZipWaypoint"+i).transform.position))yield return f;
            foreach(var f in WaitPhysical(()=>State("BagClosed")==1&&Find("Askı ayarı").activeInHierarchy,"Closed bag reveals straps"))yield return f;Capture();
            var left=Find("Ayarlanabilir askı · Left");var target=Find("BagStrapTargetLeft").transform.position;
            foreach(var f in TimedDrag(left,target+Vector3.back*.18f))yield return f;
            if(State("BagStrapLeft")!=0||State("BagReady")!=0)throw new InvalidOperationException("Loose strap passed carrying readiness.");
            foreach(var f in TimedDrag(left,target))yield return f;
            previous=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn("bagfit"),"Restore one adjusted strap"))yield return f;
            if(State("BagStrapLeft")!=1||State("BagStrapRight")!=0)throw new InvalidOperationException("Independent strap state was lost.");
            foreach(var f in TimedDrag(Find("Ayarlanabilir askı · Right"),Find("BagStrapTargetRight").transform.position))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("BagCarryActive")==1,"Start actual carrying"))yield return f;Capture();
            if(State("BagCarried")!=0||State("BagReady")!=0)throw new InvalidOperationException("Readiness skipped walking.");
            Click(Find("Çanta taşıma adımı 1"));yield return null;if(State("BagCarried")!=0)throw new InvalidOperationException("Final footprint skipped first waypoint.");
            foreach(var f in WalkBackpackRoute())yield return f;
            previous=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn(""),"Restore actual carried bag"))yield return f;
            if(State("BagCarried")!=1||State("BagReady")!=1||State("BagCarryActive")!=0)throw new InvalidOperationException("Completed carrying was lost.");Capture();
            File.AppendAllText(physicalCheckReport,"PASS physical zipper track and shortcut rejection; interrupted pointer/double pause; completed segment reload; incorrect and correct straps; separate strap reload; ordered actual walking; persistent carried readiness. No runtime C# or QA state assignments.\n");
        }
        static IEnumerable<object> CompleteBackpackFitting()
        {
            foreach(var f in WaitPhysical(()=>ReadyIn("bagfit"),"Bag fitting opens"))yield return f;
            for(int i=State("BagZipStep")+1;i<=6;i++)foreach(var f in TimedDrag(Find("Çantanın sürüklenen fermuarı"),Find("BagZipWaypoint"+i).transform.position,.40f))yield return f;
            yield return null;
            foreach(string side in new[]{"Left","Right"})foreach(var f in TimedDrag(Find("Ayarlanabilir askı · "+side),Find("BagStrapTarget"+side).transform.position))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("BagCarryActive")==1,"Wear fitted bag"))yield return f;
            foreach(var f in WalkBackpackRoute())yield return f;
        }
        static IEnumerable<object> WalkBackpackRoute()
        {
            foreach(var f in WaitPhysical(()=>Find("Çanta taşıma adımı 0").activeInHierarchy,"First footprint is available"))yield return f;
            Click(Find("Çanta taşıma adımı 0"));foreach(var f in WaitPhysical(()=>State("BagCarryStep")==1&&Find("Çanta taşıma adımı 1").activeInHierarchy,"First carrying waypoint"))yield return f;
            Click(Find("Çanta taşıma adımı 1"));foreach(var f in WaitPhysical(()=>State("BagCarried")==1,"Carry bag to the doorway"))yield return f;
        }
    }
}
