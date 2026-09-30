// Editor-only construction of native Visual Scripting zipper, straps and walking checks.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateBackpackTrial()
        {
            foreach(string key in new[]{"BagZipStep","BagStrapLeft","BagStrapRight","BagCarryActive","BagCarryStep","BagCarried"})InitialPhysical(key);
            InitialPhysical("BagStrapPosLeft",-20);InitialPhysical("BagStrapPosRight",-20);
            var center=anchors["Anchor_BagWork"].position+new Vector3(0,.035f,-.10f);
            var front=Group("Fermuar denemesi",interactions.transform,center);
            var back=Group("Askı ayarı",interactions.transform,center);
            Model("BackpackTrialFront",front.transform,Vector3.zero);
            Model("BackpackTrialBack",back.transform,Vector3.zero);
            front.SetActive(false);back.SetActive(false);
            var stage=main.Event("OpenBagFit");var p=main.Send(stage.trigger,flow,"CapturePhysicalSnapshot","bagfit");
            p=main.Send(p,flow,"OpenWork","bagfit","Çantanı taşımaya hazırla","Derya: Fermuarı kapat. İki askıyı da rahatça ayarla.");p=main.Send(p,flow,"RestorePacking");main.Send(p,flow,"RestoreBagTrial");
            var view=WorkCamera("bagfit",center+Vector3.up*.13f,.74f);
            var restore=main.Event("RestorePhysicalObjects");main.Send(restore.trigger,flow,"RestoreBagTrial");
            var visible=new YanYanaGraphAuthor(flow,"Yakından çanta incelerken bedenler nesneyi kapatmaz");var frame=visible.Add(new Unity.VisualScripting.LateUpdate());
            var inTrial=Is(visible,visible.Var("Workspace",flow),"bagfit");
            p=visible.Active(frame.trigger,front,And(visible,inTrial,visible.Binary<Less>(visible.Var("BagZipStep",flow),6)));
            p=visible.Active(p,back,And(visible,inTrial,visible.Binary<GreaterOrEqual>(visible.Var("BagZipStep",flow),6)));
            var showBody=Is(visible,Or(visible,inTrial,Is(visible,visible.Var("Workspace",flow),"bag")),false);
            foreach(string who in new[]{"Ada","Efe"})foreach(var renderer in cast[who].GetComponentsInChildren<Renderer>())p=visible.Set(p,typeof(Renderer),"enabled",renderer,showBody);
            visible.Dirty();
            CreateBagZipper(front,center);CreateBagStrap(back,center,"Left",-.14f);CreateBagStrap(back,center,"Right",.14f);
            var check=main.Event("CheckBagStraps",true);var ready=main.Branch(check.trigger,And(main,Is(main,main.Var("BagStrapLeft"),1),Is(main,main.Var("BagStrapRight"),1)));
            p=main.SetVar(ready.ifTrue,"BagCarryActive",1);p=main.SetVar(p,"BagCarryStep",0);p=main.SetVar(p,"BagCarried",0);p=main.SetVar(p,"Busy",true);p=main.Send(p,flow,"CommitCheckpoint");p=main.Send(p,flow,"Explore");
            CreateBagWalkingTrial();
        }

        static void CreateBagZipper(GameObject root,Vector3 center)
        {
            var points=new[]{new Vector3(-.205f,.235f,-.16f),new Vector3(-.205f,.235f,.06f),new Vector3(-.18f,.235f,.22f),new Vector3(0,.235f,.28f),new Vector3(.18f,.235f,.22f),new Vector3(.205f,.235f,.06f),new Vector3(.205f,.235f,-.16f)};
            var slider=Shape("Çantanın sürüklenen fermuarı",PrimitiveType.Cube,root.transform,points[0],new Vector3(.063f,.030f,.085f),mats["YY_coral"]);
            physicalItems["BagZipperCue"]=slider;
            var handle=Shape("Fermuar tutacağı",PrimitiveType.Cylinder,slider.transform,new Vector3(0,.60f,-.55f),new Vector3(.82f,.20f,.56f),mats["YY_cream"]);
            var hit=slider.AddComponent<BoxCollider>();hit.size=new Vector3(2.1f,3f,1.7f);
            var next=Shape("Fermuarın sonraki ilmeği",PrimitiveType.Sphere,root.transform,points[1],new Vector3(.047f,.018f,.047f),mats["YY_mustard"]);
            var g=new YanYanaGraphAuthor(slider,"Fermuarı iz boyunca çek; köşeleri atlayamazsın");g.Initial("Held",false);g.Initial("Finger",-999);
            var down=g.Add(new OnPointerDown());g.Bind(down.target,slider);var allowed=g.Branch(down.trigger,And(g,AtWork(g,"bagfit"),Is(g,g.Var("Dragging",flow),false)));
            var p=g.SetVar(allowed.ifTrue,"Held",true);p=g.SetVar(p,"Finger",g.Get(typeof(PointerEventData),"pointerId",down.data));g.SetVar(p,"Dragging",true,flow);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,slider);allowed=g.Branch(drag.trigger,And(g,AtWork(g,"bagfit"),And(g,g.Var("Held"),Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",drag.data)))));
            var projection=PointerOnPlane(g,allowed.ifTrue,drag.data,center.y+.235f);
            var choose=g.Add(new SwitchOnInteger{options=new System.Collections.Generic.List<int>{0,1,2,3,4,5}});g.Bind(choose.selector,g.Var("BagZipStep",flow));g.Link(projection.path,choose.enter);
            foreach(var branch in choose.branches)
            {
                int index=branch.Key+1;var target=center+points[index];
                var close=g.Branch(branch.Value,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},projection.point,target).result,.085f));
                p=g.SetVar(close.ifTrue,"BagZipStep",index,flow);p=g.Set(p,typeof(Transform),"position",slider.transform,target);
                p=g.Set(p,typeof(Transform),"position",next.transform,center+points[Mathf.Min(index+1,6)]);p=g.Send(p,flow,"CommitCheckpoint");
                if(index==6){p=g.SetVar(p,"BagClosed",1,flow);p=g.SetVar(p,"Held",false);p=g.SetVar(p,"Dragging",false,flow);p=Text(g,p,goalText,"İki askıyı rahatça ayarla");p=Text(g,p,gestureText,"Tokaları çizgili bölgeye sürükle");p=Text(g,p,lineText,"Derya: Çanta kapandı. Askılar iki omzunda da rahat dursun.");g.Send(p,flow,"CommitCheckpoint");}
            }
            var up=g.Add(new OnPointerUp());g.Bind(up.target,slider);
            var own=g.Branch(up.trigger,Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",up.data)));p=g.SetVar(own.ifTrue,"Held",false);g.SetVar(p,"Dragging",false,flow);
            // Restore from the always-active flow graph: close-up roots may still be inactive on load.
            var restore=main.Event("RestoreBagTrial");p=main.SetVar(restore.trigger,"Held",false,slider);var pick=main.Add(new SwitchOnInteger{options=new System.Collections.Generic.List<int>{0,1,2,3,4,5,6}});main.Bind(pick.selector,main.Var("BagZipStep"));main.Link(p,pick.enter);
            foreach(var branch in pick.branches){p=main.Set(branch.Value,typeof(Transform),"position",slider.transform,center+points[branch.Key]);main.Set(p,typeof(Transform),"position",next.transform,center+points[Mathf.Min(branch.Key+1,6)]);}
            g.Dirty();
            for(int i=0;i<points.Length;i++)Group("BagZipWaypoint"+i,interactions.transform,center+points[i]);
        }

        static void CreateBagStrap(GameObject root,Vector3 center,string side,float x)
        {
            var slider=Shape("Ayarlanabilir askı · "+side,PrimitiveType.Cube,root.transform,new Vector3(x,.265f,-.20f),new Vector3(.10f,.025f,.072f),mats["YY_coral"]);
            physicalItems["BagStrapCue"+side]=slider;
            Shape("Tokanın orta boşluğu",PrimitiveType.Cube,slider.transform,new Vector3(0,.60f,0),new Vector3(.52f,.18f,.48f),mats["YY_tealDark"]);
            var hit=slider.AddComponent<BoxCollider>();hit.size=new Vector3(1.5f,3f,1.8f);
            var zone=Shape("Rahat askı bölgesi · "+side,PrimitiveType.Cube,root.transform,new Vector3(x,.255f,.045f),new Vector3(.098f,.012f,.075f),mats["YY_mustard"]);
            var g=new YanYanaGraphAuthor(slider,"Askıyı ne gevşek ne fazla sıkı bırak");g.Initial("Held",false);g.Initial("Finger",-999);
            var down=g.Add(new OnPointerDown());g.Bind(down.target,slider);var can=g.Branch(down.trigger,And(g,AtWork(g,"bagfit"),Is(g,g.Var("Dragging",flow),false)));
            var p=g.SetVar(can.ifTrue,"Held",true);p=g.SetVar(p,"Finger",g.Get(typeof(PointerEventData),"pointerId",down.data));g.SetVar(p,"Dragging",true,flow);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,slider);can=g.Branch(drag.trigger,And(g,AtWork(g,"bagfit"),And(g,g.Var("Held"),Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",drag.data)))));
            var point=PointerOnPlane(g,can.ifTrue,drag.data,center.y+.265f);var z=g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"z",point.point),center.z),-.20f,.16f).result;
            g.Set(point.path,typeof(Transform),"position",slider.transform,Add(g,center,V3(g,x,.265f,z)));
            var end=g.Add(new OnEndDrag());g.Bind(end.target,slider);can=g.Branch(end.trigger,And(g,AtWork(g,"bagfit"),And(g,g.Var("Held"),Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",end.data)))));
            p=g.SetVar(can.ifTrue,"Held",false);p=g.SetVar(p,"Dragging",false,flow);
            var value=g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"z",g.Get(typeof(Transform),"position",slider.transform)),center.z);
            p=g.SetVar(p,"BagStrapPos"+side,g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Binary<ScalarMultiply>(value,100f)).result,flow);
            var good=And(g,g.Binary<GreaterOrEqual>(value,.005f),g.Binary<LessOrEqual>(value,.085f));p=g.SetVar(p,"BagStrap"+side,g.Call(typeof(Convert),"ToInt32",null,OneBool,good).result,flow);
            p=g.Send(p,flow,"CommitCheckpoint");var answer=g.Branch(p,good);Text(g,answer.ifFalse,lineText,"Derya: Çizgili bölgeyi dene. Askı rahatça oturmalı.");g.Send(answer.ifTrue,flow,"CheckBagStraps");
            var up=g.Add(new OnPointerUp());g.Bind(up.target,slider);var own=g.Branch(up.trigger,And(g,Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",up.data)),Is(g,g.Get(typeof(PointerEventData),"dragging",up.data),false)));p=g.SetVar(own.ifTrue,"Held",false);g.SetVar(p,"Dragging",false,flow);
            var reset=main.Event("RestoreBagTrial");p=main.SetVar(reset.trigger,"Held",false,slider);main.Set(p,typeof(Transform),"position",slider.transform,Add(main,center,V3(main,x,.265f,main.Binary<ScalarDivide>(main.Var("BagStrapPos"+side),100f))));g.Dirty();
            Group("BagStrapTarget"+side,interactions.transform,center+new Vector3(x,.265f,.045f));
        }

        static void CreateBagWalkingTrial()
        {
            var destinations=new[]{anchors["Anchor_Start"].position,anchors["Anchor_Exit"].position+new Vector3(.8f,0,.30f)};
            for(int step=0;step<destinations.Length;step++)
            {
                var destination=destinations[step];var marker=Group("Çanta taşıma adımı "+step,interactions.transform,destination+Vector3.up*.026f);
                physicalItems["BagWalkCue"+step]=marker;
                Group("Approach_BagCarry"+step,interactions.transform,destination);
                for(int foot=0;foot<2;foot++)Shape("Ayak izi",PrimitiveType.Capsule,marker.transform,new Vector3(foot==0?-.13f:.13f,0,foot==0?-.06f:.06f),new Vector3(.13f,.012f,.25f),mats["YY_teal"]);
                var hit=marker.AddComponent<BoxCollider>();hit.size=new Vector3(1.0f,.08f,1.0f);
                var g=new YanYanaGraphAuthor(flow,"Hazırladığın çantayla gerçekten yürü · "+step);var frame=g.Add(new Unity.VisualScripting.Update());
                var active=And(g,And(g,Is(g,g.Var("Phase",flow),0),Is(g,g.Var("BagCarryActive",flow),1)),Is(g,g.Var("BagCarryStep",flow),step));
                var p=g.Active(frame.trigger,marker,active);var arrived=g.Branch(p,And(g,active,CanExplore(g)));
                var distance=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Ada"].transform),destination).result;
                var close=g.Branch(arrived.ifTrue,g.Binary<Less>(distance,.30f));p=g.SetVar(close.ifTrue,"BagCarryStep",step+1,flow);p=g.Send(p,flow,"CommitCheckpoint");
                if(step==1){p=g.SetVar(p,"BagCarryActive",0,flow);p=g.SetVar(p,"BagCarried",1,flow);p=g.SetVar(p,"BagReady",1,flow);p=g.Send(p,flow,"CommitCheckpoint");p=Text(g,p,goalText,"Hazırlığını odada sürdürebilirsin");Text(g,p,lineText,"Ada: Çanta kapalı. İki askıyla rahat yürüyebiliyorum.");}
                var click=g.Add(new OnPointerClick());g.Bind(click.target,marker);var can=g.Branch(click.trigger,And(g,active,CanExplore(g)));g.Do(can.ifTrue,typeof(StoryPlayerMovement),"TrySetDestination",movers["Ada"],new[]{typeof(Vector3)},destination);g.Dirty();
            }
            var hint=main.Event("RefreshCarryGoal");var activeTrial=main.Branch(hint.trigger,And(main,Is(main,main.Var("BagCarryActive"),1),Is(main,main.Var("Phase"),0)));
            var path=Text(main,activeTrial.ifTrue,goalText,"Çantanla kapıya kadar yürü");path=Text(main,path,gestureText,"Ayak izlerine dokunarak yürü");Text(main,path,lineText,"Derya: Çantayı evde deneyelim. İki askını da kullan.");
        }
    }
}
