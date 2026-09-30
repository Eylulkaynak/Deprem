using System;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static ValueOutput PackedForTravel(YanYanaGraphAuthor g,string item)=>And(g,Is(g,g.Var("BagClosed",flow),1),g.Binary<GreaterOrEqual>(g.Var("Pack."+item+".X",flow),0));
        static GameObject CarriedModel(string model,string label,float height)
        {
            var root=Group(label,castRoot.transform);var visual=Model(model,root.transform,Vector3.zero,1,0);var rr=visual.GetComponentsInChildren<Renderer>();var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);visual.transform.localScale*=height/bounds.size.y;return root;
        }
        static void CreatePhysicalCarry()
        {
            var bag=CarriedModel("BackpackClosed","Ada’nın hazırlayıp taşıdığı çanta",.44f);
            // FBX front pockets face +Z; turn the shoulder straps toward Ada.
            bag.transform.GetChild(0).localRotation=Quaternion.Euler(0,180,0);
            var g=new YanYanaGraphAuthor(flow,"Hazırlığın görünür sonucu · çanta sırtta, ışık elde");var tick=g.Add(new Unity.VisualScripting.LateUpdate());
            var carried=And(g,Is(g,g.Var("BagClosed",flow),1),Or(g,g.Binary<GreaterOrEqual>(g.Var("Phase",flow),1),Or(g,Is(g,g.Var("BagCarryActive",flow),1),Is(g,g.Var("BagCarried",flow),1))));var p=g.Active(tick.trigger,bag,And(g,carried,Is(g,g.Var("Workspace",flow),"")));
            BindBackpackToClothing(g,p,bag);g.Dirty();
            CreateHeldProp("Ada","FlashlightTravel","Yolu gösteren eldeki ışık",.22f,"LightFound",false);
            CreateHeldProp("Efe","ComfortFox","Efe’nin yanında getirdiği küçük tilki",.24f,"Pack.ComfortFox.X",true);
        }
        static void BindBackpackToClothing(YanYanaGraphAuthor g,ControlOutput p,GameObject bag)
        {
            // Author the same weighted bone transforms as one real jacket-back
            // vertex. The player graph follows that surface through every pose.
            var actor=cast["Ada"].transform;var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;var weights=mesh.boneWeights;var bind=mesh.bindposes;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*bind[i]).ToArray();
            var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var neck=animator.GetBoneTransform(HumanBodyBones.Neck);var desired=Vector3.Lerp(hips.position,neck.position,.6f)-actor.forward*.12f;int selected=-1;float score=float.PositiveInfinity;Vector3 measured=Vector3.zero;
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var point=Vector3.zero;var normal=Vector3.zero;
                foreach(var part in new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)}){point+=matrices[part.Item1].MultiplyPoint3x4(vertices[i])*part.Item2;normal+=matrices[part.Item1].MultiplyVector(normals[i])*part.Item2;}
                if(Vector3.Dot(normal.normalized,-actor.forward)<.6f||point.y<hips.position.y+.08f||point.y>neck.position.y-.10f)continue;
                float distance=(point-desired).sqrMagnitude;if(distance<score){score=distance;selected=i;measured=point;}
            }
            if(selected<0)throw new InvalidOperationException("No jacket-back surface for the carried bag.");
            var weight=weights[selected];object contact=Vector3.zero,normalSum=Vector3.zero;
            foreach(var part in new[]{(weight.boneIndex0,weight.weight0),(weight.boneIndex1,weight.weight1),(weight.boneIndex2,weight.weight2),(weight.boneIndex3,weight.weight3)})
            {
                if(part.Item2<=0)continue;var bone=skin.bones[part.Item1];var point=g.Call(typeof(Transform),"TransformPoint",bone,new[]{typeof(Vector3)},bind[part.Item1].MultiplyPoint3x4(vertices[selected])).result;
                var direction=g.Call(typeof(Transform),"TransformVector",bone,new[]{typeof(Vector3)},bind[part.Item1].MultiplyVector(normals[selected])).result;
                contact=Add(g,contact,Mul(g,point,part.Item2));normalSum=Add(g,normalSum,Mul(g,direction,part.Item2));
            }
            var normalUnit=g.Get(typeof(Vector3),"normalized",normalSum);var torsoUp=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",neck),g.Get(typeof(Transform),"position",hips)).result;
            var rotation=g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},Mul(g,normalUnit,-1f),torsoUp).result;
            float scale=bag.transform.GetChild(0).localScale.x;var padCentre=new Vector3(0,.30f*scale,.105f*scale);
            var padOffset=g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Vector3)},rotation,padCentre).result;
            p=g.Set(p,typeof(Transform),"position",bag.transform,g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},Add(g,contact,Mul(g,normalUnit,.005f)),padOffset).result);g.Set(p,typeof(Transform),"rotation",bag.transform,rotation);
            System.IO.File.WriteAllText("ClientExports/YanYana/Reports/backpack-surface-binding.txt","Actual jacket mesh vertex="+selected+" idle world="+measured.ToString("F5")+" source pad centre="+padCentre.ToString("F5")+" normal clearance=0.005m. Weighted bone transforms retain contact during crouching; no runtime C# adapter.\n");
        }
        static void CreateHeldProp(string who,string model,string label,float size,string key,bool packed)
        {
            var prop=CarriedModel(model,label,size);var actor=cast[who];var animator=actor.GetComponentInChildren<Animator>();var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            var g=new YanYanaGraphAuthor(flow,label+" · yürüyüş ve elde temas");var frame=g.Add(new Unity.VisualScripting.LateUpdate());
            var valid=And(g,And(g,packed?(object)g.Binary<GreaterOrEqual>(g.Var(key,flow),0):Is(g,g.Var(key,flow),1),g.Binary<GreaterOrEqual>(g.Var("Phase",flow),2)),And(g,Is(g,Is(g,g.Var("Phase",flow),31),false),g.Binary<Less>(g.Var("Phase",flow),7)));
            if(packed)valid=And(g,valid,Is(g,g.Var("BagClosed",flow),1));
            if(who=="Ada")valid=And(g,valid,And(g,Is(g,Is(g,g.Var("Workspace",flow),"sibling"),false),And(g,Is(g,g.Var("FlashlightReady",flow),1),PackedForTravel(g,"Flashlight"))));
            var p=g.Active(frame.trigger,prop,valid);var active=g.Branch(p,And(g,valid,Is(g,g.Var("Paused",flow),false)));var rotation=g.Get(typeof(Transform),"rotation",actor.transform);
            var offset=g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Vector3)},rotation,new Vector3(.20f,who=="Ada"?.72f:.55f,who=="Ada"?.18f:.14f)).result;var goal=Add(g,g.Get(typeof(Transform),"position",actor.transform),offset);
            string pose=who=="Ada"?"Loop":"Soft";p=SolveGripArm(g,active.ifTrue,who,goal,true,pose,g.Get(typeof(Transform),who=="Ada"?"right":"up",actor.transform));
            object propRotation=rotation;
            float scale=prop.transform.GetChild(0).localScale.x;var gripLocal=who=="Ada"?new Vector3(0,.34f*scale,-.015f*scale):new Vector3(.064f*scale,.10f*scale,0);
            var gripOffset=g.Call(typeof(Quaternion),"op_Multiply",null,new[]{typeof(Quaternion),typeof(Vector3)},propRotation,gripLocal).result;
            p=g.Set(p,typeof(Transform),"position",prop.transform,g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,who,true,pose),gripOffset).result);p=g.Set(p,typeof(Transform),"rotation",prop.transform,propRotation);
            if(who=="Ada"){p=g.Set(p,typeof(Transform),"position",torch.transform,g.Call(typeof(Transform),"TransformPoint",prop.transform,new[]{typeof(Vector3)},new Vector3(0,.225f*scale,.07f*scale)).result);p=g.Set(p,typeof(Transform),"rotation",torch.transform,rotation);}g.Dirty();
        }
    }
}
