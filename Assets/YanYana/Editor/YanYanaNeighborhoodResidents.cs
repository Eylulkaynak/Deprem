// Scene authoring only. The existing follower and native graph nodes drive the cane.
using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateNeighborhoodResidents()
        {
            var points=new[]{new Vector3(-8.9f,-.44f,-25.0f),new Vector3(-8.8f,-.44f,-26.7f),new Vector3(9.4f,-.44f,-24.6f)};
            for(int i=0;i<3;i++)BackgroundPerson("Mahalledeki komşu · "+YanYanaCharacterVariants.Names[i+3],YanYanaCharacterVariants.Names[i+3],points[i],Paper,false);
        }
        static void CreateYusufCanePose(GameObject actor,GameObject cane,NavMeshAgent agent)
        {
            var animator=actor.GetComponentInChildren<Animator>();var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            var grip=JsonUtility.FromJson<YanYanaGripAuthor.Grip>(System.IO.File.ReadAllText(YanYanaGripAuthor.GripPath));var g=new YanYanaGraphAuthor(cane,"Yusuf’un bastonu · yere basış ve elde taşıma");
            g.Initial("StepPhase",0f);g.Initial("HandError",0f);g.Initial("GripCenterError",0f);g.Initial("FootLift",0f);
            var frame=g.Add(new Unity.VisualScripting.Update());var running=g.Branch(frame.trigger,Is(g,g.Var("Paused",flow),false));
            var speed=g.Get(typeof(Vector3),"magnitude",g.Get(typeof(NavMeshAgent),"velocity",agent));
            var moving=g.Branch(running.ifTrue,g.Binary<Greater>(speed,.055f));
            var p=g.SetVar(moving.ifTrue,"StepPhase",Sum(g,g.Var("StepPhase"),g.Binary<ScalarMultiply>(g.Binary<ScalarMultiply>(speed,g.Get(typeof(Time),"deltaTime")),8f)));
            p=g.SetVar(p,"FootLift",g.Binary<ScalarMultiply>(g.Call(typeof(Mathf),"Max",null,new[]{typeof(float),typeof(float)},0f,g.Call(typeof(Mathf),"Sin",null,OneFloat,g.Var("StepPhase")).result).result,.045f));
            // Measured against 72 poses and nine stride/ground combinations per pose:
            // this stance stays between the rig's folded and fully extended reaches.
            p=g.Set(p,typeof(Transform),"localPosition",cane.transform,V3(g,.40f,g.Var("FootLift"),Sum(g,.16f,g.Binary<ScalarMultiply>(g.Call(typeof(Mathf),"Cos",null,OneFloat,g.Var("StepPhase")).result,.035f))));g.Send(p,cane,"GroundCane");
            p=g.SetVar(moving.ifFalse,"FootLift",0f);p=g.Set(p,typeof(Transform),"localPosition",cane.transform,new Vector3(.40f,0,.16f));g.Send(p,cane,"GroundCane");
            var ground=g.Event("GroundCane");var point=g.Get(typeof(Transform),"position",cane.transform);
            var ray=g.Call(typeof(Physics),"Raycast",null,new[]{typeof(Vector3),typeof(Vector3),typeof(RaycastHit).MakeByRefType(),typeof(float),typeof(int),typeof(QueryTriggerInteraction)},Add(g,point,Vector3.up*.20f),Vector3.down,null,.65f,~0,QueryTriggerInteraction.Ignore);g.Link(ground.trigger,ray.enter);
            var hit=g.Branch(ray.exit,ray.result);g.Set(hit.ifTrue,typeof(Transform),"position",cane.transform,V3(g,g.Get(typeof(Vector3),"x",point),Sum(g,g.Get(typeof(Vector3),"y",g.Get(typeof(RaycastHit),"point",ray.outputParameters[2])),g.Var("FootLift")),g.Get(typeof(Vector3),"z",point)));
            var late=g.Add(new Unity.VisualScripting.LateUpdate());var active=g.Branch(late.trigger,Is(g,g.Var("Paused",flow),false));
            var contact=g.Call(typeof(Transform),"TransformPoint",cane.transform,new[]{typeof(Vector3)},new Vector3(-.045f,.856f,.005f)).result;
            var hint=g.Call(typeof(Transform),"TransformDirection",actor.transform,new[]{typeof(Vector3)},new Vector3(.45f,-1f,-.1f)).result;
            // Keep the authored wrist/forearm relation. Align the actual sculpted grip
            // through a few arm solves, instead of twisting the wrist off its sleeve.
            var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            p=active.ifTrue;ValueOutput target=null;
            for(int iteration=0;iteration<4;iteration++)
            {
                var offset=g.Call(typeof(Transform),"TransformVector",hand,new[]{typeof(Vector3)},grip.handleCenter).result;
                target=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},contact,offset).result;
                p=g.Do(p,typeof(FirefighterExtinguishManager),"SolveArm",null,new[]{typeof(Transform),typeof(Transform),typeof(Transform),typeof(Vector3),typeof(Vector3)},animator.GetBoneTransform(HumanBodyBones.RightUpperArm),animator.GetBoneTransform(HumanBodyBones.RightLowerArm),hand,target,hint);
                // Roll the whole forearm around its elbow-to-wrist axis. The sleeve
                // and hand stay joined while the sculpted grip follows the handle.
                var axis=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",hand),g.Get(typeof(Transform),"position",lower)).result;
                var across=g.Call(typeof(Transform),"TransformDirection",hand,new[]{typeof(Vector3)},Vector3.Cross(grip.forward,grip.normal).normalized).result;
                var desired=g.Get(typeof(Transform),"right",actor.transform);
                var from=g.Call(typeof(Vector3),"ProjectOnPlane",null,new[]{typeof(Vector3),typeof(Vector3)},across,axis).result;
                var to=g.Call(typeof(Vector3),"ProjectOnPlane",null,new[]{typeof(Vector3),typeof(Vector3)},desired,axis).result;
                var angle=g.Call(typeof(Vector3),"SignedAngle",null,new[]{typeof(Vector3),typeof(Vector3),typeof(Vector3)},from,to,axis).result;
                var rotation=g.Call(typeof(Quaternion),"AngleAxis",null,new[]{typeof(float),typeof(Vector3)},angle,axis).result;
                p=g.Set(p,typeof(Transform),"rotation",lower,g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Quaternion)},rotation,g.Get(typeof(Transform),"rotation",lower)).result);
            }
            p=g.SetVar(p,"HandError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",hand),target).result);
            g.SetVar(p,"GripCenterError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Call(typeof(Transform),"TransformPoint",hand,new[]{typeof(Vector3)},grip.handleCenter).result,contact).result);g.Dirty();
        }
    }
}
