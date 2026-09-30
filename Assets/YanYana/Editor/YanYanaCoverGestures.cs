// Editor authors the three physical protection steps into native scene graphs.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateCoverGestures()
        {
            main.Initial("CoverDragging",false);main.Initial("CoverPointer",-999);
            var restart=main.Event("BeginQuake");var path=main.SetVar(restart.trigger,"CoverStage",0);main.SetVar(path,"Protected",0);
            var ada=cast["Ada"];var animator=ada.GetComponentInChildren<Animator>();
            var lower=new YanYanaGraphAuthor(ada,"Yakındaki korunma alanında gerçekten çök");var move=lower.Add(new OnDrag());lower.Bind(move.target,ada);
            var can=lower.Branch(move.trigger,And(lower,Available(lower),And(lower,Is(lower,lower.Var("Phase",flow),1),Is(lower,lower.Var("CoverStage",flow),1))));
            var delta=lower.Binary<ScalarSubtract>(lower.Get(typeof(Vector2),"y",lower.Get(typeof(PointerEventData),"position",move.data)),lower.Get(typeof(Vector2),"y",lower.Get(typeof(PointerEventData),"pressPosition",move.data)));
            var enough=lower.Branch(can.ifTrue,lower.Binary<Less>(delta,lower.Binary<ScalarMultiply>(lower.Get(typeof(Screen),"height"),-.045f)));lower.Send(enough.ifTrue,flow,"LowerIntoCover");lower.Dirty();
            for(int index=0;index<2;index++)
            {
                string side=index==0?"Right":"Left";int stage=2+index;
                var hand=animator.GetBoneTransform(index==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var token=Group(index==0?"Başını koruyan el":"Masaya tutunan el",interactions.transform);
                Shape("Elin hareket işareti",PrimitiveType.Sphere,token.transform,Vector3.zero,Vector3.one*.075f,mats["YY_mustard"]);
                var hit=token.AddComponent<SphereCollider>();hit.radius=.24f;
                var g=new YanYanaGraphAuthor(token,"Eli doğru korunma noktasına taşı · "+side);
                object target=index==0?(object)g.Get(typeof(Transform),"position",headPalms["Ada"]):anchors["Anchor_HoldAda"].position;
                var tick=g.Add(new Unity.VisualScripting.LateUpdate());var visible=And(g,Is(g,g.Var("Phase",flow),1),Is(g,g.Var("CoverStage",flow),stage));
                // Disable only renderers/collider, not the graph hosting object.
                var p=g.Set(tick.trigger,typeof(Transform),"position",token.transform,g.Get(typeof(Transform),"position",hand));p=g.Set(p,typeof(Collider),"enabled",hit,visible);
                foreach(var r in token.GetComponentsInChildren<Renderer>())p=g.Set(p,typeof(Renderer),"enabled",r,visible);
                var down=g.Add(new OnPointerDown());g.Bind(down.target,token);var allowed=g.Branch(down.trigger,And(g,Available(g),And(g,visible,Is(g,g.Var("CoverDragging",flow),false))));
                p=g.SetVar(allowed.ifTrue,"CoverDragging",true,flow);g.SetVar(p,"CoverPointer",g.Get(typeof(PointerEventData),"pointerId",down.data),flow);
                var drag=g.Add(new OnDrag());g.Bind(drag.target,token);var owner=And(g,And(g,Available(g),visible),And(g,g.Var("CoverDragging",flow),Is(g,g.Var("CoverPointer",flow),g.Get(typeof(PointerEventData),"pointerId",drag.data))));allowed=g.Branch(drag.trigger,owner);
                var pointer=g.Get(typeof(PointerEventData),"position",drag.data);var screenTarget=g.Call(typeof(Camera),"WorldToScreenPoint",camera,new[]{typeof(Vector3)},target).result;
                var screenPoint=V3(g,g.Get(typeof(Vector2),"x",pointer),g.Get(typeof(Vector2),"y",pointer),g.Get(typeof(Vector3),"z",screenTarget));
                g.SetVar(allowed.ifTrue,"Cover"+side+"Aim",g.Call(typeof(Camera),"ScreenToWorldPoint",camera,new[]{typeof(Vector3)},screenPoint).result,flow);
                var drop=g.Add(new OnEndDrag());g.Bind(drop.target,token);allowed=g.Branch(drop.trigger,And(g,And(g,Available(g),visible),And(g,g.Var("CoverDragging",flow),Is(g,g.Var("CoverPointer",flow),g.Get(typeof(PointerEventData),"pointerId",drop.data)))));
                var actual=g.Get(typeof(PointerEventData),"position",drop.data);var expected=V2(g,g.Get(typeof(Vector3),"x",screenTarget),g.Get(typeof(Vector3),"y",screenTarget));
                var fit=g.Branch(allowed.ifTrue,g.Binary<Less>(g.Call(typeof(Vector2),"Distance",null,new[]{typeof(Vector2),typeof(Vector2)},actual,expected).result,g.Binary<ScalarMultiply>(g.Get(typeof(Screen),"width"),.12f)));
                p=g.SetVar(fit.ifTrue,"CoverDragging",false,flow);p=g.SetVar(p,"CoverStage",stage+1,flow);p=g.SetVar(p,"Cover"+side+"Aim",target,flow);
                if(index==0){p=Text(g,p,goalText,"Diğer elinle masaya tutun");p=Text(g,p,gestureText,"İşaretli eli masa ayağına götür");p=Text(g,p,lineText,"Ada: Başımı koruyorum. Diğer elimle masanın ayağına tutunacağım.");g.Send(p,flow,"CommitCheckpoint");}
                else g.Send(p,flow,"FinishCoverGrip");
                p=g.SetVar(fit.ifFalse,"Cover"+side+"Aim",g.Var("Cover"+side+"Rest",flow),flow);p=g.SetVar(p,"CoverDragging",false,flow);Text(g,p,lineText,index==0?"Efe: Başının üzerini koruyalım. Eli biraz daha yaklaştır.":"Efe: Masanın ayağına uzanalım. Buradan tutunabiliriz.");
                var up=g.Add(new OnPointerUp());g.Bind(up.target,token);var releasing=g.Branch(up.trigger,And(g,Is(g,g.Var("CoverPointer",flow),g.Get(typeof(PointerEventData),"pointerId",up.data)),Is(g,g.Get(typeof(PointerEventData),"dragging",up.data),false)));g.SetVar(releasing.ifTrue,"CoverDragging",false,flow);g.Dirty();
            }
            var restore=main.Event("RestorePhysicalObjects");path=main.SetVar(restore.trigger,"CoverDragging",false);path=main.SetVar(path,"CoverPointer",-999);path=main.SetVar(path,"CoverRightAim",main.Var("CoverRightRest"));main.SetVar(path,"CoverLeftAim",main.Var("CoverLeftRest"));
        }
    }
}
