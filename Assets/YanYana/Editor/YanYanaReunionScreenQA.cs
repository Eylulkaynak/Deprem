// Editor integration. Targets must be reachable through the production raycasters.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical Reunion Screen Targets")]
        static void ReunionScreenQA()=>StartPhysicalCheck(ReunionScreenSequence(),"physical-reunion-screen-targets");
        static (Vector2 screen,RaycastResult hit) VisibleReunionTarget(GameObject target,int step)
        {
            var points=new List<Vector3>();
            if(step==0){points.Add(target.transform.position+Vector3.up*.08f);points.Add(target.GetComponent<Collider>().bounds.center);}
            else foreach(string who in step==1?new[]{"Derya","Emre"}:new[]{"Ada"})
            {
                var animator=Find(who).GetComponentInChildren<Animator>();points.Add(animator.GetBoneTransform(HumanBodyBones.Head).position+Vector3.up*.10f);
                var torso=animator.GetBoneTransform(HumanBodyBones.Chest)??animator.GetBoneTransform(HumanBodyBones.Spine);if(torso)points.Add(torso.position+Vector3.up*.06f);
            }
            var failures=new List<string>();
            foreach(var world in points)
            {
                Vector3 viewport=Camera.main.WorldToViewportPoint(world);Vector2 screen=Camera.main.WorldToScreenPoint(world);
                if(viewport.z<=0||viewport.x<.04f||viewport.x>.96f||viewport.y<.27f||viewport.y>.84f){failures.Add("outside readable viewport "+viewport);continue;}
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
                GameObject handler=hits.Count==0?null:step==2?ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject):ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                if(handler==target){File.AppendAllText(physicalCheckReport,"PASS raycast ending="+State("Ending")+" step="+step+" size="+Screen.width+"x"+Screen.height+" viewport="+viewport+" hit="+hits[0].gameObject.name+"\n");return(screen,hits[0]);}
                failures.Add("hit="+(hits.Count>0?hits[0].gameObject.name:"NONE")+" handler="+(handler?handler.name:"NONE")+" viewport="+viewport);
            }
            throw new InvalidOperationException("No visible interactive point for "+target.name+": "+string.Join("; ",failures));
        }
        static PointerEventData ReunionPointer(Vector2 at,RaycastResult hit,GameObject target)=>new PointerEventData(EventSystem.current){position=at,pressPosition=at,pointerId=-1,button=PointerEventData.InputButton.Left,pointerCurrentRaycast=hit,pointerPressRaycast=hit,pointerPress=target,pointerDrag=target,eligibleForClick=true};
        static IEnumerable<object> ReunionScreenSequence()
        {
            var folder="ClientExports/YanYana/Screenshots/reunion-screen-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);
            File.AppendAllText(physicalCheckReport,"Isolated decision fixtures. Inputs below begin at validated screen raycasts; screenshots require visual review. Not a child or physical-phone session.\n");
            var previous=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn(""),"Fresh screen-target session"))yield return f;
            for(int ending=1;ending<=4;ending++)
            {
                finalRoute=ending;StartReunionFixture();foreach(var f in WaitPhysical(()=>State("Ending")==ending&&ReadyIn(""),"Stage screen-target reunion"))yield return f;
                for(int step=0;step<3;step++)
                {
                    foreach(var f in WaitPhysical(()=>State("FinalStep")==step&&ReadyIn(""),"Reunion step "+step))yield return f;
                    var target=Find(step==0?"Final yol işareti "+ending:step==1?"Aileye kavuşma hedefi "+ending:"Ada");
                    foreach(int height in new[]{960,1170,1200})
                    {
                        YanYanaQA.SetGameView(540,height);foreach(var f in FramesFor(.65f))yield return f;VisibleReunionTarget(target,step);
                        if(step==1||step==2)foreach(string who in new[]{"Ada","Derya","Emre"})
                        {
                            var actor=Find(who).GetComponentInChildren<Animator>();var head=Camera.main.WorldToViewportPoint(actor.GetBoneTransform(HumanBodyBones.Head).position+Vector3.up*.1f);
                            if(head.z<=0||head.x<.08f||head.x>.92f||head.y<.27f||head.y>.84f)throw new InvalidOperationException("Family face clipped: "+who+" "+head);
                            File.AppendAllText(physicalCheckReport,"PASS framed "+who+" ending="+ending+" step="+step+" height="+height+" viewport="+head+"\n");
                        }
                        if(step==1&&height==960)
                        {
                            var portrait=Find("Aktif karakter portresi").GetComponent<UnityEngine.UI.Image>();var corners=new Vector3[4];portrait.rectTransform.GetWorldCorners(corners);
                            File.AppendAllText(physicalCheckReport,"HUD portrait active="+portrait.gameObject.activeInHierarchy+" enabled="+portrait.enabled+" sprite="+(portrait.sprite?portrait.sprite.name:"NONE")+" color="+portrait.color+" alpha="+portrait.canvasRenderer.GetAlpha()+" culled="+portrait.canvasRenderer.cull+" rect="+corners[0]+".."+corners[2]+"\n");
                        }
                        ScreenCapture.CaptureScreenshot(folder+"/ending-"+ending+"-step"+step+"-"+height+".png");foreach(var f in FramesFor(.12f))yield return f;
                    }
                    YanYanaQA.SetGameView(540,960);foreach(var f in FramesFor(.65f))yield return f;
                    var pointer=VisibleReunionTarget(target,step);var data=ReunionPointer(pointer.screen,pointer.hit,target);
                    ExecuteEvents.ExecuteHierarchy(pointer.hit.gameObject,data,ExecuteEvents.pointerDownHandler);
                    if(step<2)
                    {
                        ExecuteEvents.Execute(target,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(target,data,ExecuteEvents.pointerClickHandler);
                    }
                    else
                    {
                        data.dragging=true;ExecuteEvents.Execute(target,data,ExecuteEvents.beginDragHandler);double started=EditorApplication.timeSinceStartup;
                        while(EditorApplication.timeSinceStartup-started<.38)
                        {
                            data.position=pointer.screen+Vector2.up*(Screen.height*.10f*Mathf.Clamp01((float)(EditorApplication.timeSinceStartup-started)/.35f));ExecuteEvents.Execute(target,data,ExecuteEvents.dragHandler);yield return null;
                        }
                        ExecuteEvents.Execute(target,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(target,data,ExecuteEvents.endDragHandler);
                    }
                }
                foreach(var f in WaitPhysical(()=>State("Phase")==8&&State("Reunited")==1,"Screen-driven ending completes"))yield return f;
                if(State("Ending")!=ending)throw new InvalidOperationException("Screen inputs changed the ending.");File.AppendAllText(physicalCheckReport,"PASS screen-driven reunion "+ending+" completed.\n");
            }
            File.AppendAllText(physicalCheckReport,"Screenshots="+folder+"\n");
        }
    }
}
