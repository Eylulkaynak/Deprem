// Editor authoring only: native scene graphs reuse the existing two-bone solver.
using System;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static Transform FireFootControl(string name)
        {
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==name);
        }
        static void EnsureFireFootControls()
        {
            var transforms=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var root=transforms.FirstOrDefault(t=>t.name=="İdil’in zeminde sabit ayak hedefleri");
            if(!root)
            {
                var parent=transforms.First(t=>t.name.StartsWith("02 Dünya"));
                root=Group("İdil’in zeminde sabit ayak hedefleri",parent).transform;
                var animator=cast["Idil"].GetComponentInChildren<Animator>();
                foreach(bool right in new[]{false,true})
                {
                    var foot=animator.GetBoneTransform(right?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                    var target=Group(right?"Sağ sabit ayak hedefi":"Sol sabit ayak hedefi",root).transform;
                    target.SetPositionAndRotation(foot.position,foot.rotation);
                }
                Group("Adım mesafesi, yüksekliği ve süresi",root,new Vector3(.045f,.045f,.20f));
            }
        }
        static ControlOutput PresentFireFeet(YanYanaGraphAuthor g,ControlOutput before,FirefighterExtinguishManager manager)
        {
            EnsureFireFootControls();
            var actor=cast["Idil"].transform;var animator=cast["Idil"].GetComponentInChildren<Animator>();
            var settings=FireFootControl("Adım mesafesi, yüksekliği ve süresi");
            var config=g.Get(typeof(Transform),"localPosition",settings);
            g.Initial("FeetReady",false);g.Initial("FootStep",0);g.Initial("NextFoot",1);g.Initial("StepProgress",0f);g.Initial("StepCount",0f);
            foreach(string side in new[]{"Left","Right"})
            {g.Initial(side+"FootOffset",Vector3.zero);g.Initial(side+"FootRestRotation",Quaternion.identity);g.Initial(side+"StepStart",Vector3.zero);g.Initial(side+"StepEnd",Vector3.zero);g.Initial(side+"StepStartRotation",Quaternion.identity);g.Initial(side+"StepEndRotation",Quaternion.identity);}
            var sequence=g.Add(new Unity.VisualScripting.Sequence{outputCount=5});g.Link(before,sequence.enter);
            var initialize=g.Branch(sequence.multiOutputs[0],Is(g,g.Var("FeetReady"),false));var p=initialize.ifTrue;
            foreach(bool right in new[]{false,true})
            {
                string side=right?"Right":"Left";var foot=animator.GetBoneTransform(right?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var target=FireFootControl(right?"Sağ sabit ayak hedefi":"Sol sabit ayak hedefi");
                var position=g.Get(typeof(Transform),"position",foot);var rotation=g.Get(typeof(Transform),"rotation",foot);
                p=g.SetVar(p,side+"FootOffset",g.Call(typeof(Transform),"InverseTransformPoint",actor,new[]{typeof(Vector3)},position).result);
                p=g.SetVar(p,side+"FootRestRotation",g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Quaternion)},g.Call(typeof(Quaternion),"Inverse",null,new[]{typeof(Quaternion)},g.Get(typeof(Transform),"rotation",actor)).result,rotation).result);
                p=g.Set(p,typeof(Transform),"position",target,position);p=g.Set(p,typeof(Transform),"rotation",target,rotation);
            }
            p=g.SetVar(p,"FootStep",0);g.SetVar(p,"FeetReady",true);
            object[] needs=new object[2];
            for(int index=0;index<2;index++)
            {
                bool right=index==1;string side=right?"Right":"Left";
                var target=FireFootControl(right?"Sağ sabit ayak hedefi":"Sol sabit ayak hedefi");
                var desired=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},g.Var(side+"FootOffset")).result;
                var desiredRotation=g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Quaternion)},g.Get(typeof(Transform),"rotation",actor),g.Var(side+"FootRestRotation")).result;
                needs[index]=Or(g,g.Binary<Greater>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},desired,g.Get(typeof(Transform),"position",target)).result,g.Get(typeof(Vector3),"x",config)),g.Binary<Greater>(g.Call(typeof(Quaternion),"Angle",null,new[]{typeof(Quaternion),typeof(Quaternion)},desiredRotation,g.Get(typeof(Transform),"rotation",target)).result,18f));
            }
            var idle=g.Branch(sequence.multiOutputs[1],Is(g,g.Var("FootStep"),0));
            var left=g.Branch(idle.ifTrue,And(g,needs[0],Or(g,Is(g,g.Var("NextFoot"),1),Is(g,needs[1],false))));
            StartFireFootStep(g,left.ifTrue,actor,false);
            var rightNeeded=g.Branch(left.ifFalse,needs[1]);StartFireFootStep(g,rightNeeded.ifTrue,actor,true);
            foreach(bool right in new[]{false,true})
            {
                string side=right?"Right":"Left";int step=right?2:1;
                var target=FireFootControl(right?"Sağ sabit ayak hedefi":"Sol sabit ayak hedefi");
                var during=g.Branch(sequence.multiOutputs[right?3:2],Is(g,g.Var("FootStep"),step));
                // Only one foot moves; the other retains its world position and rotation.
                var delta=g.Call(typeof(Mathf),"Min",null,new[]{typeof(float),typeof(float)},g.Get(typeof(Time),"deltaTime"),.1f).result;
                var fraction=g.Call(typeof(Mathf),"Clamp01",null,new[]{typeof(float)},Sum(g,g.Var("StepProgress"),g.Binary<ScalarDivide>(delta,g.Get(typeof(Vector3),"z",config)))).result;
                p=g.SetVar(during.ifTrue,"StepProgress",fraction);
                var eased=g.Call(typeof(Mathf),"SmoothStep",null,new[]{typeof(float),typeof(float),typeof(float)},0f,1f,g.Var("StepProgress")).result;
                var lift=g.Binary<ScalarMultiply>(g.Call(typeof(Mathf),"Sin",null,new[]{typeof(float)},g.Binary<ScalarMultiply>(g.Var("StepProgress"),Mathf.PI)).result,g.Get(typeof(Vector3),"y",config));
                var position=Add(g,g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Var(side+"StepStart"),g.Var(side+"StepEnd"),eased).result,V3(g,0f,lift,0f));
                p=g.Set(p,typeof(Transform),"position",target,position);
                p=g.Set(p,typeof(Transform),"rotation",target,g.Call(typeof(Quaternion),"Slerp",null,new[]{typeof(Quaternion),typeof(Quaternion),typeof(float)},g.Var(side+"StepStartRotation"),g.Var(side+"StepEndRotation"),eased).result);
                var finished=g.Branch(p,g.Binary<GreaterOrEqual>(g.Var("StepProgress"),1f));
                p=g.SetVar(finished.ifTrue,"FootStep",0);g.SetVar(p,"NextFoot",right?1:2);
            }
            p=sequence.multiOutputs[4];
            foreach(bool right in new[]{false,true})
            {
                string side=right?"Right":"Left";
                var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperLeg:HumanBodyBones.LeftUpperLeg);
                var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerLeg:HumanBodyBones.LeftLowerLeg);
                var foot=animator.GetBoneTransform(right?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var target=FireFootControl(right?"Sağ sabit ayak hedefi":"Sol sabit ayak hedefi");
                var hint=g.Call(typeof(Transform),"TransformDirection",actor,new[]{typeof(Vector3)},new Vector3(right?.12f:-.12f,0,1)).result;
                p=g.Do(p,typeof(FirefighterExtinguishManager),"SolveArm",null,new[]{typeof(Transform),typeof(Transform),typeof(Transform),typeof(Vector3),typeof(Vector3)},upper,lower,foot,g.Get(typeof(Transform),"position",target),hint);
                p=g.Set(p,typeof(Transform),"rotation",foot,g.Get(typeof(Transform),"rotation",target));
                p=g.SetVar(p,"Idil"+side+"FootError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",foot),g.Get(typeof(Transform),"position",target)).result,flow);
                p=g.SetVar(p,"Idil"+side+"FootPlanted",Is(g,Is(g,g.Var("FootStep"),right?2:1),false),flow);
            }
            p=g.SetVar(p,"IdilFootSteps",g.Var("StepCount"),flow);
            return g.SetVar(p,"IdilFootStep",g.Var("FootStep"),flow);
        }
        static void StartFireFootStep(YanYanaGraphAuthor g,ControlOutput before,Transform actor,bool right)
        {
            string side=right?"Right":"Left";var target=FireFootControl(right?"Sağ sabit ayak hedefi":"Sol sabit ayak hedefi");
            var p=g.SetVar(before,side+"StepStart",g.Get(typeof(Transform),"position",target));
            var desired=g.Call(typeof(Transform),"TransformPoint",actor,new[]{typeof(Vector3)},g.Var(side+"FootOffset")).result;
            p=g.SetVar(p,side+"StepEnd",desired);
            p=g.SetVar(p,side+"StepStartRotation",g.Get(typeof(Transform),"rotation",target));
            p=g.SetVar(p,side+"StepEndRotation",g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Quaternion)},g.Get(typeof(Transform),"rotation",actor),g.Var(side+"FootRestRotation")).result);
            p=g.SetVar(p,"StepProgress",0f);p=g.SetVar(p,"StepCount",Sum(g,g.Var("StepCount"),1f));g.SetVar(p,"FootStep",right?2:1);
        }
    }
}
