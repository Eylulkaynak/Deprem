using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        public static void RefreshImmersiveFireInput()
        {
            var transforms=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            flow=transforms.First(t=>t.name.StartsWith("01 Akış")).gameObject;camera=Camera.main;
            cast["Idil"]=transforms.First(t=>t.name=="Idil").gameObject;
            hintPanel=transforms.First(t=>t.name=="İsteğe bağlı destek").gameObject;
            for(int stage=1;stage<=3;stage++)
            {
                var manager=transforms.First(t=>t.name=="Gerçek müdahale alanı "+stage).GetComponent<FirefighterExtinguishManager>();
                var surface=transforms.First(t=>t.name=="Hortumu yönlendir "+stage).gameObject;
                foreach(var machine in surface.GetComponents<ScriptMachine>())UnityEngine.Object.DestroyImmediate(machine);
                var so=new SerializedObject(manager);so.FindProperty("bodyTurnSpeed").floatValue=5;so.FindProperty("upperBodyAimWeight").floatValue=.16f;so.ApplyModifiedPropertiesWithoutUndo();
                BindImmersiveFireInput(surface,manager,stage);
            }
        }
        static void CreateFirePointerSurface(FirefighterExtinguishManager manager,int stage)
        {
            // The pre-existing manager remains the source of water, fire and rig behavior.
            // Native graph events own touch capture, pause and lifecycle in this adventure.
            manager.enabled=false;
            var surface=Panel("Hortumu yönlendir "+stage,safeRect,Vector2.zero,Vector2.one,new Vector2(0,296),new Vector2(0,-148),Color.clear);surface.raycastTarget=true;surface.gameObject.SetActive(false);
            var display=new YanYanaGraphAuthor(flow,"Aktif hortumun dokunma yüzeyi "+stage);var tick=display.Add(new Unity.VisualScripting.Update());display.Active(tick.trigger,surface.gameObject,And(display,Is(display,display.Var("Workspace",flow),"fire"+stage),Available(display)));display.Dirty();
            BindImmersiveFireInput(surface.gameObject,manager,stage);
        }
        static void BindImmersiveFireInput(GameObject surface,FirefighterExtinguishManager manager,int stage)
        {
            var g=new YanYanaGraphAuthor(surface,"Dokunmayı yakala; sürükleyerek suyun yönünü değiştir");g.Initial("Holding",false);g.Initial("Finger",-999);g.Initial("Aim",Vector3.zero);g.Initial("Target",-1);
            var down=g.Add(new OnPointerDown());g.Bind(down.target,surface);var can=g.Branch(down.trigger,And(g,AtWork(g,"fire"+stage),Is(g,g.Var("Holding"),false)));var p=g.SetVar(can.ifTrue,"Holding",true);p=g.SetVar(p,"Finger",g.Get(typeof(PointerEventData),"pointerId",down.data));AimFromPointer(g,p,down.data,manager);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,surface);can=g.Branch(drag.trigger,And(g,AtWork(g,"fire"+stage),Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",drag.data))));AimFromPointer(g,can.ifTrue,drag.data,manager);
            var up=g.Add(new OnPointerUp());g.Bind(up.target,surface);var same=g.Branch(up.trigger,Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",up.data)));p=g.SetVar(same.ifTrue,"Holding",false);g.Do(p,typeof(FirefighterExtinguishManager),"SetSpraying",manager,OneBool,false);
            var disabled=g.Add(new Unity.VisualScripting.OnDisable());p=g.SetVar(disabled.trigger,"Holding",false);g.Do(p,typeof(FirefighterExtinguishManager),"SetSpraying",manager,OneBool,false);
            var update=g.Add(new Unity.VisualScripting.Update());var active=g.Branch(update.trigger,And(g,And(g,AtWork(g,"fire"+stage),g.Var("Holding")),Is(g,g.Get(typeof(FirefighterExtinguishManager),"IsFinished",manager),false)));
            p=g.SetVar(active.ifTrue,"IdleSeconds",0f,flow);p=g.SetVar(p,"HintLevel",0,flow);p=g.Active(p,hintPanel,false);p=g.Set(p,typeof(FirefighterExtinguishManager),"currentAimPoint",manager,g.Var("Aim"));p=g.Do(p,typeof(FirefighterExtinguishManager),"SetSpraying",manager,OneBool,true);
            p=g.Do(p,typeof(FirefighterExtinguishManager),"ApplyWater",manager,new[]{typeof(Vector3),typeof(int),typeof(float)},g.Var("Aim"),g.Var("Target"),g.Call(typeof(Mathf),"Min",null,new[]{typeof(float),typeof(float)},g.Get(typeof(Time),"deltaTime"),.1f).result);
            g.Do(active.ifFalse,typeof(FirefighterExtinguishManager),"SetSpraying",manager,OneBool,false);
            var late=g.Add(new Unity.VisualScripting.LateUpdate());var aiming=g.Branch(late.trigger,AtWork(g,"fire"+stage));p=g.Do(aiming.ifTrue,typeof(FirefighterExtinguishManager),"UpdateFirefighterAimRig",manager,OneFloat,g.Call(typeof(Mathf),"Min",null,new[]{typeof(float),typeof(float)},g.Get(typeof(Time),"deltaTime"),.1f).result);p=PresentHoseGrip(g,p,manager);var spraying=g.Branch(p,g.Var("Holding"));PresentWaterJet(g,spraying.ifTrue,manager,g.Var("Aim"));g.Dirty();
        }
        static ControlOutput PresentHoseGrip(YanYanaGraphAuthor g,ControlOutput p,FirefighterExtinguishManager manager)
        {
            var so=new SerializedObject(manager);var nozzle=(Transform)so.FindProperty("nozzleRig").objectReferenceValue;var chest=(Transform)so.FindProperty("chestBone").objectReferenceValue;var hose=(LineRenderer)so.FindProperty("supplyHose").objectReferenceValue;var actor=cast["Idil"].transform;
            var animator=cast["Idil"].GetComponentInChildren<Animator>();var shoulders=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)),g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.RightUpperArm)),.5f).result;
            var offset=g.Call(typeof(Transform),"TransformDirection",actor,new[]{typeof(Vector3)},new Vector3(.045f,-.17f,-.035f)).result;var origin=Add(g,shoulders,offset);p=g.Set(p,typeof(Transform),"position",nozzle,origin);
            var direction=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(FirefighterExtinguishManager),"currentAimPoint",manager),origin).result;p=g.Set(p,typeof(Transform),"rotation",nozzle,g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},direction,g.Get(typeof(Transform),"up",actor)).result);
            foreach(bool right in new[]{true,false}){var grip=(Transform)so.FindProperty(right?"rightNozzleGrip":"leftNozzleGrip").objectReferenceValue;var point=g.Get(typeof(Transform),"position",grip);p=SolveGripArm(g,p,"Idil",point,right,right?"Loop":"Cylinder",g.Get(typeof(Transform),right?"up":"forward",nozzle));}
            p=g.Do(p,typeof(FirefighterExtinguishManager),"UpdateSupplyHose",manager,Type.EmptyTypes);
            var rear=g.Call(typeof(Transform),"TransformPoint",nozzle,new[]{typeof(Vector3)},new Vector3(0,0,-.078f)).result;
            var hip=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(.34f,.43f,-.16f)).result;
            var floor=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(.72f,.04f,-.50f)).result;
            var end=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(2.2f,.04f,-1.15f)).result;
            p=g.Set(p,typeof(LineRenderer),"positionCount",hose,18);
            for(int i=0;i<18;i++)
            {
                float t=i/17f;object point;
                if(t<.56f){float q=t/.56f;var a=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},rear,hip,q).result;var b=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},hip,floor,q).result;point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},a,b,q).result;}
                else point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},floor,end,(t-.56f)/.44f).result;
                p=g.Do(p,typeof(LineRenderer),"SetPosition",hose,new[]{typeof(int),typeof(Vector3)},i,point);
            }
            return p;
        }
        static ControlOutput PresentWaterJet(YanYanaGraphAuthor g,ControlOutput p,FirefighterExtinguishManager manager,object aim)
        {
            var so=new SerializedObject(manager);var stream=(LineRenderer)so.FindProperty("waterStream").objectReferenceValue;var tip=(Transform)so.FindProperty("nozzleTip").objectReferenceValue;var impact=(ParticleSystem)so.FindProperty("waterImpact").objectReferenceValue;
            var start=g.Get(typeof(Transform),"position",tip);p=g.Set(p,typeof(LineRenderer),"positionCount",stream,18);
            for(int i=0;i<18;i++)
            {
                float t=i/17f;var point=Add(g,g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},start,aim,t).result,Vector3.up*(.045f*4*t*(1-t)));
                p=g.Do(p,typeof(LineRenderer),"SetPosition",stream,new[]{typeof(int),typeof(Vector3)},i,point);
            }
            return g.Set(p,typeof(Transform),"position",impact.transform,aim);
        }
        static void AimFromPointer(YanYanaGraphAuthor g,ControlOutput before,ValueOutput data,FirefighterExtinguishManager manager)
        {
            var point=g.Get(typeof(PointerEventData),"position",data);var ray=g.Call(typeof(Camera),"ScreenPointToRay",camera,new[]{typeof(Vector3)},V3(g,g.Get(typeof(Vector2),"x",point),g.Get(typeof(Vector2),"y",point),0f)).result;
            var aim=g.Call(typeof(FirefighterExtinguishManager),"ResolveAimPoint",manager,new[]{typeof(Ray),typeof(Vector3).MakeByRefType()},ray);g.Link(before,aim.enter);
            var p=g.SetVar(aim.exit,"Aim",aim.outputParameters[1]);g.SetVar(p,"Target",aim.result);
        }
    }
}
