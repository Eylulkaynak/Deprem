using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static int sprayStage,sprayTarget;
        static double sprayStarted,targetStarted;
        static PointerEventData sprayPointer;
        static GameObject spraySurface;
        static bool sprayBegan;
        [MenuItem("Tools/Yan Yana/QA/Physical Pointer Spray")]
        static void PointerSpray()
        {
            sprayStage=(int)Variables.Object(Flow).Get("FireStage");sprayTarget=0;sprayStarted=targetStarted=EditorApplication.timeSinceStartup;
            spraySurface=Find("Hortumu yönlendir "+sprayStage);sprayBegan=false;
            EditorApplication.update-=PointerSprayTick;EditorApplication.update+=PointerSprayTick;
        }
        static void AimSprayPointer(bool begin)
        {
            var target=Find("Alev odağı "+sprayStage+" "+sprayTarget).GetComponent<Collider>();var point=(Vector2)Camera.main.WorldToScreenPoint(target.bounds.center);
            if(begin)
            {
                sprayPointer=new PointerEventData(EventSystem.current){position=point,pressPosition=point,button=PointerEventData.InputButton.Left,pointerId=-1,pointerPress=spraySurface,pointerDrag=spraySurface};
                ExecuteEvents.Execute(spraySurface,sprayPointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(spraySurface,sprayPointer,ExecuteEvents.beginDragHandler);
            }
            else{sprayPointer.delta=point-sprayPointer.position;sprayPointer.position=point;ExecuteEvents.Execute(spraySurface,sprayPointer,ExecuteEvents.dragHandler);}
        }
        static void PointerSprayTick()
        {
            if(!EditorApplication.isPlaying||!spraySurface){EditorApplication.update-=PointerSprayTick;return;}
            int phase=(int)Variables.Object(Flow).Get("Phase"),stage=(int)Variables.Object(Flow).Get("FireStage");
            if(!sprayBegan)
            {
                if(EditorApplication.timeSinceStartup-sprayStarted>25){File.AppendAllText("ClientExports/YanYana/Reports/physical-pointer-spray.txt","FAIL waiting for active input stage="+stage+" busy="+Variables.Object(Flow).Get("Busy")+"\n");EditorApplication.update-=PointerSprayTick;return;}
                if(!spraySurface.activeInHierarchy||(bool)Variables.Object(Flow).Get("Busy"))return;
                sprayBegan=true;sprayStarted=EditorApplication.timeSinceStartup;AimSprayPointer(true);return;
            }
            if(stage!=sprayStage||phase!=5)
            {
                ExecuteEvents.Execute(spraySurface,sprayPointer,ExecuteEvents.pointerUpHandler);EditorApplication.update-=PointerSprayTick;
                File.AppendAllText("ClientExports/YanYana/Reports/physical-pointer-spray.txt","PASS stage="+sprayStage+" -> "+stage+" seconds="+(EditorApplication.timeSinceStartup-sprayStarted).ToString("F2")+". Production graph pointer handlers, water and arm manager; no test-side health changes.\n");return;
            }
            if(EditorApplication.timeSinceStartup-sprayStarted>90)
            {
                var vars=Variables.Object(spraySurface);string diagnostic=" holding="+vars.Get("Holding")+" finger="+vars.Get("Finger")+" resolvedTarget="+vars.Get("Target")+" aim="+vars.Get("Aim")+" busy="+Variables.Object(Flow).Get("Busy")+" workspace="+Variables.Object(Flow).Get("Workspace");
                ExecuteEvents.Execute(spraySurface,sprayPointer,ExecuteEvents.pointerUpHandler);EditorApplication.update-=PointerSprayTick;File.AppendAllText("ClientExports/YanYana/Reports/physical-pointer-spray.txt","FAIL stage="+sprayStage+" timeout target="+sprayTarget+diagnostic+"\n");return;
            }
            var target=Find("Alev odağı "+sprayStage+" "+sprayTarget).GetComponent<Collider>();
            if(!target.enabled&&sprayTarget<2){sprayTarget++;targetStarted=EditorApplication.timeSinceStartup;}
            AimSprayPointer(false);EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
