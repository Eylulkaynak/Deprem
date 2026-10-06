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
            YanYanaFirePresentation.EnsureHoseControls();
            for(int stage=1;stage<=3;stage++)
            {
                var manager=transforms.First(t=>t.name=="Gerçek müdahale alanı "+stage).GetComponent<FirefighterExtinguishManager>();
                var surface=transforms.First(t=>t.name=="Hortumu yönlendir "+stage).gameObject;
                foreach(var machine in surface.GetComponents<ScriptMachine>())UnityEngine.Object.DestroyImmediate(machine);
                var so=new SerializedObject(manager);so.FindProperty("bodyTurnSpeed").floatValue=6;so.FindProperty("upperBodyAimWeight").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();
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
            var late=g.Add(new Unity.VisualScripting.LateUpdate());var aiming=g.Branch(late.trigger,AtWork(g,"fire"+stage));p=PresentFireBody(g,aiming.ifTrue,manager);p=PresentHoseGrip(g,p,manager);var spraying=g.Branch(p,g.Var("Holding"));PresentWaterJet(g,spraying.ifTrue,manager,g.Var("Aim"));g.Dirty();
        }
        static ControlOutput PresentFireBody(YanYanaGraphAuthor g,ControlOutput p,FirefighterExtinguishManager manager)
        {
            var actor=cast["Idil"].transform;
            var direction=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(FirefighterExtinguishManager),"currentAimPoint",manager),g.Get(typeof(Transform),"position",actor)).result;
            var flat=V3(g,g.Get(typeof(Vector3),"x",direction),0f,g.Get(typeof(Vector3),"z",direction));
            var delta=g.Call(typeof(Mathf),"Min",null,new[]{typeof(float),typeof(float)},g.Get(typeof(Time),"deltaTime"),.1f).result;
            var turn=g.Binary<ScalarSubtract>(1f,g.Call(typeof(Mathf),"Exp",null,new[]{typeof(float)},g.Binary<ScalarMultiply>(delta,-6f)).result);
            var rotation=g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},flat,Vector3.up).result;
            return g.Set(p,typeof(Transform),"rotation",actor,g.Call(typeof(Quaternion),"Slerp",null,new[]{typeof(Quaternion),typeof(Quaternion),typeof(float)},g.Get(typeof(Transform),"rotation",actor),rotation,turn).result);
        }
        static ControlOutput PresentHoseGrip(YanYanaGraphAuthor g,ControlOutput p,FirefighterExtinguishManager manager)
        {
            var so=new SerializedObject(manager);var nozzle=(Transform)so.FindProperty("nozzleRig").objectReferenceValue;var hose=(LineRenderer)so.FindProperty("supplyHose").objectReferenceValue;var actor=cast["Idil"].transform;
            var animator=cast["Idil"].GetComponentInChildren<Animator>();var shoulders=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)),g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.RightUpperArm)),.5f).result;
            var shoulderSpan=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.RightUpperArm)),g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.LeftUpperArm))).result;
            var chestForward=g.Get(typeof(Vector3),"normalized",g.Call(typeof(Vector3),"Cross",null,new[]{typeof(Vector3),typeof(Vector3)},shoulderSpan,g.Get(typeof(Transform),"up",actor)).result);
            var authored=actor.Find("Hortum başlığının gövde önündeki konumu");
            object placement=authored?g.Get(typeof(Transform),"localPosition",authored):new Vector3(-.015f,-.02f,.35f);
            var offset=Add(g,g.Call(typeof(Vector3),"op_Multiply",null,new[]{typeof(Vector3),typeof(float)},chestForward,g.Get(typeof(Vector3),"z",placement)).result,g.Call(typeof(Transform),"TransformDirection",actor,new[]{typeof(Vector3)},V3(g,g.Get(typeof(Vector3),"x",placement),g.Get(typeof(Vector3),"y",placement),0f)).result);
            var origin=Add(g,shoulders,offset);p=g.Set(p,typeof(Transform),"position",nozzle,origin);
            var direction=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(FirefighterExtinguishManager),"currentAimPoint",manager),origin).result;p=g.Set(p,typeof(Transform),"rotation",nozzle,g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},direction,g.Get(typeof(Transform),"up",actor)).result);
            foreach(bool right in new[]{true,false}){var grip=(Transform)so.FindProperty(right?"rightNozzleGrip":"leftNozzleGrip").objectReferenceValue;p=PresentFireGripArm(g,p,nozzle,g.Get(typeof(Transform),"position",grip),right);}
            foreach(bool right in new[]{false,true})p=PresentFireSleeve(g,p,right);
            var rear=g.Call(typeof(Transform),"TransformPoint",nozzle,new[]{typeof(Vector3)},new Vector3(0,0,-.062f)).result;
            var hoseControl=nozzle.Find("Hortumun kol dışındaki kavis kontrolü");
            object bend=hoseControl?g.Get(typeof(Transform),"localPosition",hoseControl):new Vector3(.30f,-.06f,-.12f);
            var outward=g.Call(typeof(Transform),"TransformPoint",nozzle,new[]{typeof(Vector3)},bend).result;
            var hip=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(.44f,.42f,.25f)).result;
            var knee=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(.54f,.10f,.20f)).result;
            var floor=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(.80f,.035f,.12f)).result;
            var groundBend=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(1.28f,.035f,-.04f)).result;
            var end=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},new Vector3(2.2f,.035f,-1.15f)).result;
            p=g.Set(p,typeof(LineRenderer),"positionCount",hose,25);
            for(int i=0;i<25;i++)
            {
                float t=i/24f;object a,b,c;float q;
                if(t<.375f){a=rear;b=outward;c=hip;q=t/.375f;}
                else if(t<.708333f){a=hip;b=knee;c=floor;q=(t-.375f)/.333333f;}
                else{a=floor;b=groundBend;c=end;q=(t-.708333f)/.291667f;}
                var ab=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},a,b,q).result;
                var bc=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},b,c,q).result;
                var point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},ab,bc,q).result;
                p=g.Do(p,typeof(LineRenderer),"SetPosition",hose,new[]{typeof(int),typeof(Vector3)},i,point);
            }
            return p;
        }
        static ControlOutput PresentFireGripArm(YanYanaGraphAuthor g,ControlOutput p,Transform nozzle,object contact,bool right)
        {
            var animator=cast["Idil"].GetComponentInChildren<Animator>(); var hand=InteractionHand("Idil",right);
            var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
            var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
            var definition=HandDefinition("Idil",right); string pose=right?"Loop":"Cylinder";
            var grip=HandPoseDefinition("Idil",right,pose); p=SetHandShape(g,p,"Idil",right,pose);
            // The original hand frame's cross axis is the cylinder axis: vertical
            // for the rear pistol grip and forward for the barrel support.
            object axis=g.Get(typeof(Transform),right?"up":"forward",nozzle);
            var palmControl=nozzle.Find("Sol hortum avuç yönü");
            object palmDirection=palmControl?g.Get(typeof(Transform),"localPosition",palmControl):new Vector3(.819152f,.573576f,0);
            object palm=right?g.Get(typeof(Transform),"forward",nozzle):g.Call(typeof(Transform),"TransformDirection",nozzle,new[]{typeof(Vector3)},palmDirection).result;
            var basis=Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(definition.forward,definition.normal).normalized,definition.normal));
            var rotation=g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Quaternion)},g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},axis,palm).result,basis).result;
            p=g.Set(p,typeof(Transform),"rotation",hand,rotation);
            var goal=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},contact,g.Call(typeof(Transform),"TransformVector",hand,new[]{typeof(Vector3)},grip.center).result).result;
            var elbowControl=cast["Idil"].transform.Find(right?"Sağ hortum dirseğinin yönü":"Sol hortum dirseğinin yönü");
            object elbowDirection=elbowControl?g.Get(typeof(Transform),"localPosition",elbowControl):new Vector3(right?1.6f:-.65f,-1,-.15f);
            var hint=g.Call(typeof(Transform),"TransformDirection",cast["Idil"].transform,new[]{typeof(Vector3)},elbowDirection).result;
            p=g.Do(p,typeof(FirefighterExtinguishManager),"SolveArm",null,new[]{typeof(Transform),typeof(Transform),typeof(Transform),typeof(Vector3),typeof(Vector3)},upper,lower,hand,goal,hint);
            // The existing arm solver rotates parent bones. Restore the authored palm
            // orientation at the wrist so each glove actually wraps its own handle.
            p=g.Set(p,typeof(Transform),"rotation",hand,rotation);
            return g.SetVar(p,"Idil"+(right?"Right":"Left")+"GripError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,"Idil",right,pose),contact).result,flow);
        }
        static ControlOutput PresentWaterJet(YanYanaGraphAuthor g,ControlOutput p,FirefighterExtinguishManager manager,object aim)
        {
            var so=new SerializedObject(manager);var stream=(LineRenderer)so.FindProperty("waterStream").objectReferenceValue;var tip=(Transform)so.FindProperty("nozzleTip").objectReferenceValue;var impact=(ParticleSystem)so.FindProperty("waterImpact").objectReferenceValue;
            var start=g.Get(typeof(Transform),"position",tip);p=g.Set(p,typeof(LineRenderer),"positionCount",stream,18);
            for(int i=0;i<18;i++)
            {
                float t=i/17f;var point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},start,aim,t).result;
                p=g.Do(p,typeof(LineRenderer),"SetPosition",stream,new[]{typeof(int),typeof(Vector3)},i,point);
            }
            p=g.Set(p,typeof(Transform),"position",impact.transform,aim);
            var jet=impact.transform.Find("Basınçlı su damlaları");
            if(jet)
            {
                p=g.Set(p,typeof(Transform),"position",jet,start);
                var direction=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},aim,start).result;
                p=g.Set(p,typeof(Transform),"rotation",jet,g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},direction,Vector3.up).result);
                var distance=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},start,aim).result;
                p=g.Set(p,typeof(ParticleSystem.MainModule),"startLifetimeMultiplier",g.Get(typeof(ParticleSystem),"main",jet.GetComponent<ParticleSystem>()),g.Binary<ScalarDivide>(distance,11f));
            }
            return p;
        }
        static ControlOutput PresentFireSleeve(YanYanaGraphAuthor g,ControlOutput p,bool right)
        {
            var actor=cast["Idil"].transform; var animator=cast["Idil"].GetComponentInChildren<Animator>();
            var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
            var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
            var cuff=hand.Find("Kıyafetin eldiven bağlantısı");
            var sleeve=actor.Find("İskeleti takip eden itfaiye kolları/"+(right?"Sağ":"Sol")+" ön kol kumaşı");
            if(!cuff||!sleeve)return p; // Existing scenes can refresh input before uniform authoring.
            var elbow=g.Get(typeof(Transform),"position",lower); var wrist=g.Get(typeof(Transform),"position",cuff);
            var direction=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},wrist,elbow).result;
            p=g.Set(p,typeof(Transform),"position",sleeve,g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},elbow,wrist,.5f).result);
            p=g.Set(p,typeof(Transform),"rotation",sleeve,g.Call(typeof(Quaternion),"FromToRotation",null,new[]{typeof(Vector3),typeof(Vector3)},Vector3.up,direction).result);
            var scale=g.Get(typeof(Transform),"lossyScale",sleeve.parent);
            var length=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},elbow,wrist).result;
            return g.Set(p,typeof(Transform),"localScale",sleeve,V3(g,g.Binary<ScalarDivide>(.13f,g.Get(typeof(Vector3),"x",scale)),g.Binary<ScalarDivide>(g.Binary<ScalarMultiply>(length,.5f),g.Get(typeof(Vector3),"y",scale)),g.Binary<ScalarDivide>(.13f,g.Get(typeof(Vector3),"z",scale))));
        }
        static void AimFromPointer(YanYanaGraphAuthor g,ControlOutput before,ValueOutput data,FirefighterExtinguishManager manager)
        {
            var point=g.Get(typeof(PointerEventData),"position",data);var ray=g.Call(typeof(Camera),"ScreenPointToRay",camera,new[]{typeof(Vector3)},V3(g,g.Get(typeof(Vector2),"x",point),g.Get(typeof(Vector2),"y",point),0f)).result;
            var aim=g.Call(typeof(FirefighterExtinguishManager),"ResolveAimPoint",manager,new[]{typeof(Ray),typeof(Vector3).MakeByRefType()},ray);g.Link(before,aim.enter);
            // Keep empty-space touches on the intervention area instead of aiming behind the actor.
            var bindings=new SerializedObject(manager).FindProperty("fires");
            var points=Enumerable.Range(0,bindings.arraySize).Select(i=>((Collider)bindings.GetArrayElementAtIndex(i).FindPropertyRelative("hitCollider").objectReferenceValue).transform.position).ToArray();
            var resolved=aim.outputParameters[1];
            var bounded=V3(g,
                g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Get(typeof(Vector3),"x",resolved),points.Min(v=>v.x)-.5f,points.Max(v=>v.x)+.5f).result,
                g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Get(typeof(Vector3),"y",resolved),points.Min(v=>v.y)+.04f,points.Max(v=>v.y)+.75f).result,
                g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Get(typeof(Vector3),"z",resolved),points.Min(v=>v.z)-.5f,points.Max(v=>v.z)+.5f).result);
            var p=g.SetVar(aim.exit,"Aim",bounded);g.SetVar(p,"Target",aim.result);
        }
    }
}
