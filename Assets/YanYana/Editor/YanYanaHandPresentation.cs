// Editor authoring only: scene graphs pose the existing rigs and native blend shapes.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static readonly Dictionary<string,YanYanaInteractionHands.Character> handDefinitions=new Dictionary<string,YanYanaInteractionHands.Character>();
        static YanYanaInteractionHands.Hand HandDefinition(string who,bool right)
        {
            if(!handDefinitions.TryGetValue(who,out var value)){value=YanYanaInteractionHands.Read(who);handDefinitions[who]=value;}
            return value.hands.Single(x=>x.right==right);
        }
        static YanYanaInteractionHands.Pose HandPoseDefinition(string who,bool right,string pose)=>pose=="Open"?new YanYanaInteractionHands.Pose{name="Open",center=HandDefinition(who,right).palm}:HandDefinition(who,right).poses.Single(x=>x.name==(right?"Right_":"Left_")+pose);
        static Transform InteractionHand(string who,bool right)=>cast[who].GetComponentInChildren<Animator>().GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
        static ValueOutput HandContactPoint(YanYanaGraphAuthor g,string who,bool right,string pose)=>g.Call(typeof(Transform),"TransformPoint",InteractionHand(who,right),new[]{typeof(Vector3)},HandPoseDefinition(who,right,pose).center).result;
        static ControlOutput SetHandShape(YanYanaGraphAuthor g,ControlOutput p,string who,bool right,string pose)
        {
            if(pose=="Open")return p;
            var skin=cast[who].GetComponentInChildren<SkinnedMeshRenderer>();int index=skin.sharedMesh.GetBlendShapeIndex((right?"Right_":"Left_")+pose);
            if(index<0)throw new InvalidOperationException("Missing authored hand pose: "+who+" "+pose);
            return g.Do(p,typeof(SkinnedMeshRenderer),"SetBlendShapeWeight",skin,new[]{typeof(int),typeof(float)},index,100f);
        }
        static void CreateInteractionHandPresentation()
        {
            handDefinitions.Clear();
            foreach(string who in new[]{"Ada","Efe","Idil"})
            {
                var skin=cast[who].GetComponentInChildren<SkinnedMeshRenderer>();var g=new YanYanaGraphAuthor(cast[who],"Doğal eller · etkin nesne kavraması");var tick=g.Add(new Unity.VisualScripting.Update());var ready=g.Branch(tick.trigger,Is(g,g.Var("Paused",flow),false));var p=ready.ifTrue;
                for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)p=g.Do(p,typeof(SkinnedMeshRenderer),"SetBlendShapeWeight",skin,new[]{typeof(int),typeof(float)},i,0f);
                foreach(bool right in new[]{false,true})main.Initial(who+(right?"Right":"Left")+"GripError",0f);g.Dirty();
            }
        }
        static ControlOutput SolveGripArm(YanYanaGraphAuthor g,ControlOutput p,string who,object contact,bool right,string pose,object handleAxis=null,bool alignNormal=false,object elbowHint=null)
        {
            var actor=cast[who].transform;var animator=cast[who].GetComponentInChildren<Animator>();var hand=InteractionHand(who,right);var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var definition=HandDefinition(who,right);var grip=HandPoseDefinition(who,right,pose);
            p=SetHandShape(g,p,who,right,pose);object hint=elbowHint??g.Call(typeof(Transform),"TransformDirection",actor,new[]{typeof(Vector3)},new Vector3(right?.4f:-.4f,-1f,-.1f)).result;handleAxis=handleAxis??g.Get(typeof(Transform),"right",actor);
            for(int i=0;i<(who=="Idil"?12:8);i++)
            {
                var goal=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},contact,g.Call(typeof(Transform),"TransformVector",hand,new[]{typeof(Vector3)},grip.center).result).result;if(who=="Idil")goal=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Get(typeof(Transform),"position",hand),goal,.55f).result;
                p=g.Do(p,typeof(FirefighterExtinguishManager),"SolveArm",null,new[]{typeof(Transform),typeof(Transform),typeof(Transform),typeof(Vector3),typeof(Vector3)},upper,lower,hand,goal,hint);
                var axis=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",hand),g.Get(typeof(Transform),"position",lower)).result;
                var across=g.Call(typeof(Transform),"TransformDirection",hand,new[]{typeof(Vector3)},alignNormal?definition.normal:Vector3.Cross(definition.forward,definition.normal).normalized).result;
                var a=g.Call(typeof(Vector3),"ProjectOnPlane",null,new[]{typeof(Vector3),typeof(Vector3)},across,axis).result;var b=g.Call(typeof(Vector3),"ProjectOnPlane",null,new[]{typeof(Vector3),typeof(Vector3)},handleAxis,axis).result;
                var angle=g.Call(typeof(Vector3),"SignedAngle",null,new[]{typeof(Vector3),typeof(Vector3),typeof(Vector3)},a,b,axis).result;if(who=="Idil")angle=g.Binary<ScalarMultiply>(angle,.55f);var roll=g.Call(typeof(Quaternion),"AngleAxis",null,new[]{typeof(float),typeof(Vector3)},angle,axis).result;
                p=g.Set(p,typeof(Transform),"rotation",lower,g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Quaternion)},roll,g.Get(typeof(Transform),"rotation",lower)).result);
            }
            return g.SetVar(p,who+(right?"Right":"Left")+"GripError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,who,right,pose),contact).result,flow);
        }
    }
}
