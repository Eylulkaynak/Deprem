using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static GameObject neighborChoices, facadeBarrier;
        static StorySiblingFollower neighborFollower;

        static void CreateEvacuation()
        {
            InitialPhysical("AftershockDone");InitialPhysical("NeighborAsked");InitialPhysical("NeighborBoxClear");
            var yusuf=cast["Yusuf"];var agent=yusuf.AddComponent<NavMeshAgent>();agent.radius=.22f;agent.height=1.73f;agent.speed=1.2f;agent.acceleration=6;agent.stoppingDistance=.2f;
            neighborFollower=yusuf.AddComponent<StorySiblingFollower>();Serialized(neighborFollower,"target",cast["Ada"].transform);Serialized(neighborFollower,"animator",yusuf.GetComponentInChildren<Animator>());
            var begin=main.Add(new Unity.VisualScripting.Start());main.Do(begin.trigger,typeof(StorySiblingFollower),"SetFollowing",neighborFollower,OneBool,false);
            var cane=Model("WalkingCane",yusuf.transform,new Vector3(.28f,0,.04f),1,0);
            CreateYusufCanePose(yusuf,cane,agent);
            var collider=yusuf.AddComponent<CapsuleCollider>();collider.radius=.28f;collider.height=1.6f;collider.center=Vector3.up*.8f;
            neighborChoices=Panel("Yusuf’a nasıl destek olalım",safeRect,new Vector2(0,0),new Vector2(1,0),new Vector2(18,213),new Vector2(-18,385),Paper).gameObject;
            Button("NeighborWalk","Birlikte yürüyelim",neighborChoices.transform,Vector2.zero,new Vector2(.5f,1),new Vector2(8,8),new Vector2(-4,-8));
            Button("NeighborTeam","Görevliye haber ver",neighborChoices.transform,new Vector2(.5f,0),Vector2.one,new Vector2(4,8),new Vector2(-8,-8));neighborChoices.SetActive(false);
            ApproachEvent(yusuf,street["Yusuf"].position+Vector3.left*.7f,"TalkNeighbor",3);
            var talk=main.Event("TalkNeighbor");var firstTalk=main.Branch(talk.trigger,And(main,Is(main,main.Var("NeighborBoxClear"),0),main.Binary<Less>(main.Var("NeighborAsked"),3)));var p=main.SetVar(firstTalk.ifTrue,"NeighborAsked",1);p=Text(main,p,lineText,"Ada: Yardım ister misiniz? Yusuf: Önce geçişi açabiliriz.");p=main.Active(p,neighborChoices,true);main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,true);
            var boxPosition=street["Yusuf"].position+new Vector3(-.3f,0,-1);var park=boxPosition+Vector3.right*1.15f;
            var box=Model("Parcel",world.transform,boxPosition,1,0);box.name="Yusuf’un önündeki hafif boş kutu";var hit=box.AddComponent<BoxCollider>();hit.size=new Vector3(.5f,.44f,.42f);hit.center=Vector3.up*.22f;
            var parking=Shape("Kutuyu geçişin dışına koy",PrimitiveType.Cube,world.transform,park+Vector3.up*.012f,new Vector3(.65f,.014f,.60f),mats["YY_cream"]);
            p=ButtonEvent(main,"NeighborWalk","NeighborWalk");p=main.Active(p,neighborChoices,false);p=main.SetVar(p,"NeighborAsked",2);p=Text(main,p,goalText,"Hafif kutuyu geçişten kaldır");p=Text(main,p,lineText,"Yusuf: Teşekkür ederim. Kutuyu yolun dışına koyabiliriz.");main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);
            p=ButtonEvent(main,"NeighborTeam","NeighborTeam");p=main.Active(p,neighborChoices,false);p=main.SetVar(p,"NeighborAsked",3);p=main.SetVar(p,"NeighborTogether",0);p=Text(main,p,lineText,"Ada: Görevliye yerinizi söyleyeceğim. Yusuf: Burada güvenle beklerim.");p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);main.Send(p,flow,"CommitCheckpoint");
            var move=new YanYanaGraphAuthor(box,"Hafif kutuyu yolun dışına taşı; Yusuf kendi yürür");var drag=move.Add(new OnDrag());move.Bind(drag.target,box);var allowed=move.Branch(drag.trigger,And(move,CanExplore(move),Is(move,move.Var("NeighborAsked",flow),2)));
            var projected=PointerOnPlane(move,allowed.ifTrue,drag.data,boxPosition.y);move.Set(projected.path,typeof(Transform),"position",box.transform,projected.point);
            var drop=move.Add(new OnEndDrag());move.Bind(drop.target,box);allowed=move.Branch(drop.trigger,And(move,CanExplore(move),Is(move,move.Var("NeighborAsked",flow),2)));
            var fits=move.Branch(allowed.ifTrue,move.Binary<Less>(move.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},move.Get(typeof(Transform),"position",box.transform),park).result,.45f));
            p=move.Set(fits.ifTrue,typeof(Transform),"position",box.transform,park);p=move.SetVar(p,"NeighborBoxClear",1,flow);p=move.SetVar(p,"NeighborTogether",1,flow);p=move.SetVar(p,"NeighborAsked",4,flow);p=move.Do(p,typeof(StorySiblingFollower),"SetFollowing",neighborFollower,OneBool,true);p=Text(move,p,goalText,"Açık geçişten birlikte yürü");p=Text(move,p,lineText,"Yusuf: Şimdi birlikte, kendi hızımızda ilerleyebiliriz.");move.Send(p,flow,"CommitCheckpoint");move.Set(fits.ifFalse,typeof(Transform),"position",box.transform,boxPosition);move.Dirty();
            var restore=main.Event("RestorePhysicalObjects");var cleared=main.Branch(restore.trigger,Is(main,main.Var("NeighborBoxClear"),1));main.Set(cleared.ifTrue,typeof(Transform),"position",box.transform,park);main.Set(cleared.ifFalse,typeof(Transform),"position",box.transform,boxPosition);
            CreateAftershock();
            var update=main.Add(new Unity.VisualScripting.Update());var outside=main.Branch(update.trigger,And(main,And(main,CanExplore(main),Is(main,main.Var("Phase"),3)),main.Binary<Less>(main.Get(typeof(Vector3),"z",main.Get(typeof(Transform),"position",cast["Ada"].transform)),-10f)));
            p=main.SetVar(outside.ifTrue,"Phase",4);p=Text(main,p,chapterText,"MAHALLEDE DAYANIŞMA");p=Text(main,p,goalText,"Güvenli taraftaki İdil’e ulaş");p=Text(main,p,lineText,"Efe: İtfaiye ekibi burada. Güvenli taraftan yaklaşalım.");p=main.Set(p,typeof(Light),"intensity",sun,1.05f);main.Send(p,flow,"CommitCheckpoint");
            CreateFacadeReport();
        }

        static void ApproachEvent(GameObject target,Vector3 point,string finishedEvent,int phase)
        {
            var g=new YanYanaGraphAuthor(target,"Yakından etkileşim · "+finishedEvent);g.Initial("Pending",false);
            var click=g.Add(new OnPointerClick());g.Bind(click.target,target);var allowed=g.Branch(click.trigger,And(g,CanExplore(g),Is(g,g.Var("Phase",flow),phase)));
            var p=g.SetVar(allowed.ifTrue,"ApproachTarget",target,flow);p=g.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",movers["Ada"],new[]{typeof(Vector3)},point);g.SetVar(p,"Pending",true);
            var update=g.Add(new Unity.VisualScripting.Update());var arrived=g.Branch(update.trigger,And(g,And(g,Is(g,g.Var("ApproachTarget",flow),target),Is(g,g.Var("Phase",flow),phase)),And(g,And(g,g.Var("Pending"),CanExplore(g)),g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Ada"].transform),point).result,.45f))));
            p=g.SetVar(arrived.ifTrue,"Pending",false);g.Send(p,flow,finishedEvent);g.Dirty();
        }

        static void CreateAftershock()
        {
            var afterView=WorkCamera("aftershock",street["StairsStart"].position+Vector3.up*.58f,1.4f,false);
            var offset=new Vector3(-.7f,2.4f,-2.2f);afterView.transform.rotation=Quaternion.LookRotation(-offset,Vector3.up);
            var framing=new YanYanaGraphAuthor(flow,"Artçıda iki çocuğun korunmasını birlikte göster");var frame=framing.Add(new Unity.VisualScripting.Update());
            var visible=Is(framing,framing.Var("Phase",flow),31);var shown=framing.Active(frame.trigger,afterView.gameObject,visible);
            var moving=framing.Branch(shown,And(framing,visible,Is(framing,framing.Var("Paused",flow),false)));
            var midpoint=framing.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},framing.Get(typeof(Transform),"position",cast["Ada"].GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head)),framing.Get(typeof(Transform),"position",cast["Efe"].GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head)),.5f).result;
            framing.Set(moving.ifTrue,typeof(Transform),"position",afterView.transform,Add(framing,midpoint,offset+Vector3.up*.15f));framing.Dirty();
            main.Initial("AftershockPositionAda",Vector3.zero);main.Initial("AftershockPositionEfe",Vector3.zero);
            main.Initial("AftershockGathering",false);var meeting=Group("Merdiven öncesi birlikte bekleme yeri",interactions.transform).transform;
            var tick=main.Add(new Unity.VisualScripting.Update());var atLanding=main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",cast["Ada"].transform),street["StairsStart"].position).result,.55f);
            var begins=main.Branch(tick.trigger,And(main,And(main,Is(main,main.Var("Phase"),3),Is(main,main.Var("AftershockDone"),0)),And(main,atLanding,Is(main,main.Var("AftershockGathering"),false))));var p=main.SetVar(begins.ifTrue,"AftershockGathering",true);main.Send(p,flow,"GatherBeforeStairs");
            var gather=main.Event("GatherBeforeStairs",true);p=main.SetVar(gather.trigger,"Busy",true);p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,true);
            p=main.Set(p,typeof(Transform),"position",meeting,main.Get(typeof(Transform),"position",cast["Ada"].transform));p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            p=main.Set(p,typeof(StorySiblingFollower),"target",follower,meeting);p=main.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,null);p=main.Set(p,typeof(StorySiblingFollower),"followDistance",follower,.45f);p=main.Set(p,typeof(NavMeshAgent),"stoppingDistance",cast["Efe"].GetComponent<NavMeshAgent>(),.45f);p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
            p=Text(main,p,goalText,"Efe ile yan yana gel");p=Text(main,p,lineText,"Ada: Merdivene birlikte geçelim. Seni bekliyorum.");
            var arrived=main.Add(new WaitUntilUnit());main.Bind(arrived.condition,And(main,Is(main,main.Var("Paused"),false),main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",cast["Ada"].transform),main.Get(typeof(Transform),"position",cast["Efe"].transform)).result,.95f)));main.Link(p,arrived.enter);p=arrived.exit;
            p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=main.Set(p,typeof(StorySiblingFollower),"target",follower,cast["Ada"].transform);p=main.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,movers["Ada"]);p=main.Set(p,typeof(StorySiblingFollower),"followDistance",follower,1.05f);p=main.Set(p,typeof(NavMeshAgent),"stoppingDistance",cast["Efe"].GetComponent<NavMeshAgent>(),1.05f);
            p=main.SetVar(p,"Phase",31);p=main.SetVar(p,"Busy",false);
            p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,true);p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=Text(main,p,goalText,"Dur ve yeniden korun");p=Text(main,p,gestureText,"Ada’yı aşağı doğru sürükle");var heard=main.Branch(p,Is(main,main.Var("SiblingSupported"),1));Text(main,heard.ifTrue,lineText,"Efe: Yeniden sallanıyor! Merdivene devam etmeyelim.");Text(main,heard.ifFalse,lineText,"Ada: Yeniden sallanıyor! Burada durup korunalım.");
            var ada=cast["Ada"];var collider=ada.AddComponent<CapsuleCollider>();collider.radius=.27f;collider.height=1.4f;collider.center=Vector3.up*.7f;
            var g=new YanYanaGraphAuthor(ada,"Artçıda yürümeyi bırakıp yeniden korun");var drag=g.Add(new OnDrag());g.Bind(drag.target,ada);var allowed=g.Branch(drag.trigger,And(g,Available(g),Is(g,g.Var("Phase",flow),31)));
            var distance=g.Binary<ScalarSubtract>(g.Get(typeof(Vector2),"y",g.Get(typeof(PointerEventData),"position",drag.data)),g.Get(typeof(Vector2),"y",g.Get(typeof(PointerEventData),"pressPosition",drag.data)));var down=g.Branch(allowed.ifTrue,g.Binary<Less>(distance,g.Binary<ScalarMultiply>(g.Get(typeof(Screen),"height"),-.045f)));g.Send(down.ifTrue,flow,"ProtectAftershock");g.Dirty();
            var protect=main.Event("ProtectAftershock",true);p=main.SetVar(protect.trigger,"Busy",true);
            foreach(string who in new[]{"Ada","Efe"})
            {
                p=main.SetVar(p,"AftershockPosition"+who,main.Get(typeof(Transform),"position",cast[who].transform));
                p=main.Do(p,typeof(Animator),"SetInteger",cast[who].GetComponentInChildren<Animator>(),new[]{typeof(string),typeof(int)},"Pose",1);
                p=main.Set(p,typeof(Transform),"localPosition",cast[who].GetComponentInChildren<Animator>().transform,coverOffsets[who]);
            }
            p=Text(main,p,goalText,"Korunmayı sürdür");p=Text(main,p,gestureText,"Sarsıntı geçene kadar korun");p=Text(main,p,lineText,"Ada: Başımızı koruyalım. Sarsıntının geçmesini bekleyelim.");p=main.Wait(p,2.5f);
            foreach(string who in new[]{"Ada","Efe"})
            {
                p=main.Do(p,typeof(Animator),"SetInteger",cast[who].GetComponentInChildren<Animator>(),new[]{typeof(string),typeof(int)},"Pose",0);
                p=main.Set(p,typeof(Transform),"localPosition",cast[who].GetComponentInChildren<Animator>().transform,idleOffsets[who]);
            }
            p=main.SetVar(p,"AftershockDone",1);p=main.SetVar(p,"Phase",3);p=main.SetVar(p,"Busy",false);p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);p=Text(main,p,goalText,"Merdivenden birlikte dışarı ilerle");p=Text(main,p,gestureText,"Açık geçişleri izleyerek yürü");main.Send(p,flow,"CommitCheckpoint");
        }

        static void CreateFacadeReport()
        {
            var point=street["FacadeReport"].position;
            var debris=Group("Cephedeki tehlikeyi güvenli taraftan gör",world.transform,point+Vector3.left*1.5f);
            DressDamagedWall(debris.transform);
            var collider=debris.AddComponent<BoxCollider>();collider.center=new Vector3(.1f,.7f,.3f);collider.size=new Vector3(1.2f,1.6f,1.4f);
            facadeBarrier=Group("Görevlinin kapattığı cephe şeridi",world.transform,point+new Vector3(-.7f,0,.1f));
            for(int i=0;i<3;i++)Model("Cone",facadeBarrier.transform,new Vector3(-.8f+i*.8f,0,0),1,0);facadeBarrier.SetActive(false);
            ApproachEvent(debris,point,"ReportFacade",4);
            var report=main.Event("ReportFacade");var p=main.SetVar(report.trigger,"FacadeReported",1);p=main.Active(p,facadeBarrier,true);p=Text(main,p,lineText,"Ada: Cepheden parçalar düşmüş. Görevli: Alternatif yolu işaretliyorum.");main.Send(p,flow,"CommitCheckpoint");
            CreateWallRouteChoice(point);
            var restore=main.Event("RestorePhysicalObjects");main.Active(restore.trigger,facadeBarrier,Is(main,main.Var("FacadeReported"),1));
        }
    }
}
