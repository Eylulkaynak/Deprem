// Scene authoring only: all shipped interactions below are native Visual Scripting graphs.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static readonly List<GameObject> aidVisitors=new List<GameObject>();
        static readonly Dictionary<int,GameObject> finalWorlds=new Dictionary<int,GameObject>();
        static readonly Dictionary<int,Vector3> finalDestinations=new Dictionary<int,Vector3>();
        static GameObject aidRequests, aidRelief, familyDesk, familyEvidence;
        static Vector3 aidCenter;

        static ControlOutput WalkAndWait(YanYanaGraphAuthor g,ControlOutput before,StoryPlayerMovement mover,object destination,float tolerance=.45f)
        {
            var p=g.Do(before,typeof(StoryPlayerMovement),"SetStoryInputLocked",mover,OneBool,false);
            p=g.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",mover,new[]{typeof(Vector3)},destination);
            var wait=g.Add(new WaitUntilUnit());g.Bind(wait.condition,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",mover.transform),destination).result,tolerance));g.Link(p,wait.enter);
            return g.Do(wait.exit,typeof(StoryPlayerMovement),"Stop",mover,NoArgs);
        }
        static GameObject WorldPortrait(string name,string who,Transform parent,Vector3 at,float size)
        {
            var go=Group(name,parent,at);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=portraits[who];sr.color=Color.white;
            float width=sr.sprite.bounds.size.x;go.transform.localScale=Vector3.one*(size/width);go.transform.rotation=Quaternion.Euler(90,0,0);return go;
        }
        static GameObject NeedSymbol(int need,Transform parent,Vector3 at,float scale=1)
        {
            var root=Group("İhtiyaç simgesi "+need,parent,at);root.transform.localScale=Vector3.one*scale;
            Shape("Simge zemini",PrimitiveType.Cylinder,root.transform,Vector3.zero,new Vector3(.64f,.018f,.64f),mats["YY_cream"]);
            if(need==0)Model("Water",root.transform,new Vector3(0,.05f,0),1.5f,0);
            else if(need==1)Model("FirstAid",root.transform,new Vector3(0,.05f,0),1.7f,0);
            else {WorldPortrait("Birlikte aile", "Ada",root.transform,new Vector3(-.1f,.05f,0),.28f);WorldPortrait("Aileyi bul", "Efe",root.transform,new Vector3(.12f,.051f,0),.25f);}
            return root;
        }
        static GameObject BackgroundPerson(string name,string source,Vector3 at,Color tint,bool moving=true)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterVariants.Root+"/"+source+".prefab");
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,castRoot.transform);actor.name=name;YanYanaCharacterStyle.NormalizeRoot(actor);actor.transform.position=at;actor.transform.rotation=Quaternion.Euler(0,180,0);
            var anim=actor.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=actorController;anim.Rebind();anim.Update(0);
            GroundAnimatedVisual(actor);
            if(!moving)return actor;
            var agent=actor.AddComponent<NavMeshAgent>();agent.radius=.21f;agent.height=1.7f;agent.speed=1.15f;agent.acceleration=5;
            var mover=actor.AddComponent<StoryPlayerMovement>();Serialized(mover,"animator",anim);return actor;
        }

        static void CreatePhysicalAid()
        {
            aidVisitors.Clear();aidCenter=street["Aid"].position;
            InitialPhysical("AidStage");InitialPhysical("AidSelected",-1);InitialPhysical("ReliefGiven");InitialPhysical("EvidenceOpen");InitialPhysical("IdentityPeople");InitialPhysical("IdentityMarker");
            for(int i=0;i<3;i++)InitialPhysical("VisitorHelped"+i);
            var destinations=new[]{aidCenter+new Vector3(-3.0f,0,-1.15f),aidCenter+new Vector3(0,0,-2.6f),aidCenter+new Vector3(3,0,-1.15f)};
            for(int i=0;i<3;i++)
            {
                Model("AidStation",world.transform,destinations[i],.9f,0);
                NeedSymbol(i,world.transform,destinations[i]+new Vector3(0,1.75f,1.2f),1.7f);
                var title=WorldText(new[]{"SU VE DİNLENME","SAĞLIK EKİBİ","AİLE BULUŞMA"}[i],world.transform,destinations[i]+new Vector3(0,1.48f,-.50f),.23f,Ink);title.transform.rotation=Quaternion.Euler(0,0,0);
                var signFacing=new YanYanaGraphAuthor(title.gameObject,"Yardım bölümünde masa başlığını okunur tut");var signFrame=signFacing.Add(new Unity.VisualScripting.LateUpdate());var signVisible=signFacing.Set(signFrame.trigger,typeof(Behaviour),"enabled",title,Is(signFacing,signFacing.Var("Phase",flow),6));signFacing.Set(signVisible,typeof(Transform),"rotation",title.transform,signFacing.Get(typeof(Transform),"rotation",camera.transform));signFacing.Dirty();
            }
            var aidView=WorkCamera("aid",aidCenter+new Vector3(0,.45f,-.6f),8.3f,false);aidView.transform.position=aidCenter+new Vector3(0,9.3f,5.8f);aidView.transform.LookAt(aidCenter+new Vector3(0,0,-.65f));
            var bora=cast["Bora"];var hit=bora.AddComponent<CapsuleCollider>();hit.radius=.30f;hit.height=1.80f;hit.center=Vector3.up*.90f;
            ApproachEvent(bora,aidCenter+Vector3.forward*.9f,"EnterAid",6);
            aidRequests=Group("İnsanların ihtiyacını dinle ve yönlendir",interactions.transform);
            for(int i=0;i<3;i++)
            {
                int index=i;var start=aidCenter+new Vector3(-2.2f+i*2.2f,0,2.15f);
                var visitor=BackgroundPerson("Yardım bekleyen komşu "+i,YanYanaCharacterVariants.Names[i],start,Paper);aidVisitors.Add(visitor);
                var token=NeedSymbol(i,aidRequests.transform,start+Vector3.up*1.75f,1.65f);token.name="Taşınan ihtiyaç işareti "+i;
                var tokenHit=token.AddComponent<BoxCollider>();tokenHit.center=Vector3.up*.14f;tokenHit.size=new Vector3(.8f,.5f,.8f);
                var g=new YanYanaGraphAuthor(token,"İhtiyacı dinleyip doğru yardım masasına yönlendir");
                var click=g.Add(new OnPointerDown());g.Bind(click.target,token);var can=g.Branch(click.trigger,And(g,AtWork(g,"aid"),Is(g,g.Var("VisitorHelped"+i,flow),0)));
                var p=g.SetVar(can.ifTrue,"AidSelected",i,flow);p=Text(g,p,lineText,new[]{"Komşu: Susadım. Biraz dinlenebileceğim bir yer arıyorum.","Komşu: Sağlık ekibine danışmak istiyorum. Hangi masa?","Komşu: Ailemden haber alamadım. Kiminle görüşebilirim?"}[i]);
                var drag=g.Add(new OnDrag());g.Bind(drag.target,token);can=g.Branch(drag.trigger,And(g,AtWork(g,"aid"),Is(g,g.Var("VisitorHelped"+i,flow),0)));
                var projected=PointerOnPlane(g,can.ifTrue,drag.data,start.y+1.75f);g.Set(projected.path,typeof(Transform),"position",token.transform,projected.point);
                var end=g.Add(new OnEndDrag());g.Bind(end.target,token);can=g.Branch(end.trigger,And(g,AtWork(g,"aid"),Is(g,g.Var("VisitorHelped"+i,flow),0)));
                var fits=g.Branch(can.ifTrue,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",token.transform),destinations[i]+new Vector3(0,1.75f,1.2f)).result,1.0f));
                p=g.SetVar(fits.ifTrue,"VisitorHelped"+i,1,flow);p=g.Active(p,token,false);p=g.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",visitor.GetComponent<StoryPlayerMovement>(),new[]{typeof(Vector3)},destinations[i]+Vector3.forward*.95f);
                p=Text(g,p,lineText,"Bora: Doğru masa burada. Görevli sizi karşılayacak.");p=g.Send(p,flow,"CommitCheckpoint");g.Send(p,flow,"CheckAidRequests");
                p=g.Set(fits.ifFalse,typeof(Transform),"position",token.transform,start+Vector3.up*1.75f);Text(g,p,lineText,"Bora: İhtiyacı yeniden dinleyelim. Masanın simgesini karşılaştıralım.");g.Dirty();
                var restore=main.Event("RestoreAid");p=main.Set(restore.trigger,typeof(Transform),"position",token.transform,start+Vector3.up*1.75f);main.Active(p,token,Is(main,main.Var("VisitorHelped"+index),0));
                // Cancelling an unfinished drag must not teleport a walking visitor.
                var restoreActor=main.Event("RestoreAidActors");var done=main.Branch(restoreActor.trigger,Is(main,main.Var("VisitorHelped"+index),1));main.Do(done.ifTrue,typeof(StoryPlayerMovement),"Warp",visitor.GetComponent<StoryPlayerMovement>(),new[]{typeof(Vector3)},destinations[i]+Vector3.forward*.95f);main.Do(done.ifFalse,typeof(StoryPlayerMovement),"Warp",visitor.GetComponent<StoryPlayerMovement>(),new[]{typeof(Vector3)},start);
            }
            aidRequests.SetActive(false);
            var objectRestore=main.Event("RestorePhysicalObjects");main.Send(objectRestore.trigger,flow,"RestoreAid");
            var check=main.Event("CheckAidRequests",true);var all=main.Branch(check.trigger,And(main,And(main,Is(main,main.Var("VisitorHelped0"),1),Is(main,main.Var("VisitorHelped1"),1)),Is(main,main.Var("VisitorHelped2"),1)));
            var path=main.SetVar(all.ifTrue,"Busy",true);
            object arrivals=true;for(int i=0;i<3;i++)arrivals=And(main,arrivals,main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",aidVisitors[i].transform),destinations[i]+Vector3.forward*.95f).result,.45f));
            var arrived=main.Add(new WaitUntilUnit());main.Bind(arrived.condition,arrivals);main.Link(path,arrived.enter);path=main.SetVar(arrived.exit,"AidStage",1);main.Send(path,flow,"AdvanceAid");
            var enter=main.Event("EnterAid",true);path=main.SetVar(enter.trigger,"Busy",true);path=main.Send(path,flow,"EnterRole","Bora");path=main.Wait(path,1.1f);path=Released(main,path);main.Send(path,flow,"AdvanceAid");
            CreateReliefAndIdentity(destinations[2]);
            CreateAidBroadcast();
            var advance=main.Event("AdvanceAid",true);path=main.SetVar(advance.trigger,"Busy",true);path=Text(main,path,chapterText,"BORA’NIN YARDIM NOKTASI");path=main.Active(path,activityBack,false);path=main.Active(path,rotateControl,false);foreach(var view in workCameras.Values)path=main.Active(path,view.gameObject,false);
            path=main.Active(path,aidRequests,false);path=main.Active(path,aidRelief,false);path=main.Active(path,familyDesk,false);path=main.Active(path,aidBroadcast,false);path=main.Send(path,flow,"RestoreAid");
            var stage=main.Add(new SwitchOnInteger{options=new List<int>{0,1,2,4}});main.Bind(stage.selector,main.Var("AidStage"));main.Link(path,stage.enter);
            path=main.Active(stage.branches[0].Value,aidRequests,true);path=main.SetVar(path,"Workspace","aid");path=main.Active(path,aidView.gameObject,true);path=Text(main,path,goalText,"Komşuları uygun masalara yönlendir");path=Text(main,path,chapterText,"BORA’NIN YARDIM NOKTASI");path=Text(main,path,lineText,"Bora: Önce dinleyelim. İhtiyaç işaretini uygun masaya götürelim.");path=Text(main,path,gestureText,"İhtiyaca dokun · masasına sürükle");main.Send(path,flow,"FinishAidCameraBlend");
            path=main.Active(stage.branches[1].Value,aidRelief,true);path=main.SetVar(path,"Workspace","relief");path=main.Active(path,workCameras["relief"].gameObject,true);path=Text(main,path,goalText,"Su ve yiyeceği Efe’ye ulaştır");path=Text(main,path,gestureText,"Şişeyi mindere · paketi tabağa sürükle");
            var packed=main.Branch(path,And(main,PackedForTravel(main,"Water"),Is(main,main.Var("WaterReady"),1)));var a=Text(main,packed.ifTrue,lineText,"Ada: Çantadaki su kapalı. Yiyeceği de kontrol edelim.");main.Send(a,flow,"FinishAidCameraBlend");a=Text(main,packed.ifFalse,lineText,"Bora: Kapalı su ve uygun yiyeceği masadan alabiliriz.");main.Send(a,flow,"FinishAidCameraBlend");
            path=main.Active(stage.branches[2].Value,familyDesk,true);path=main.SetVar(path,"Workspace","family");path=main.Active(path,workCameras["family"].gameObject,true);path=Text(main,path,goalText,"Aile bilgilerini iki işaretle doğrula");path=Text(main,path,gestureText,"Bilgiyi aç · eşleşen iki parçayı yerleştir");path=Text(main,path,lineText,"Bora: Aileyi ve buluşma işaretini birlikte doğrulayalım.");main.Send(path,flow,"FinishAidCameraBlend");
            main.Send(stage.branches[3].Value,flow,"OpenAidBroadcast");main.Send(stage.@default,flow,"BeginReunion");
            var settled=main.Event("FinishAidCameraBlend",true);path=main.Wait(settled.trigger,.82f);path=Released(main,path);var familyInput=main.Branch(path,Is(main,main.Var("Workspace"),"family"));main.Send(familyInput.ifTrue,flow,"PrepareFamilyInput");main.SetVar(familyInput.ifFalse,"Busy",false);
        }

        static void CreateReliefAndIdentity(Vector3 table)
        {
            InitialPhysical("ReliefWaterGiven");InitialPhysical("ReliefFoodGiven");
            var at=aidCenter+new Vector3(0,.02f,2.3f);aidRelief=Group("Çantanın veya yardım masasının suyu",interactions.transform);aidRelief.SetActive(false);
            Shape("Dinlenme için temiz örtü",PrimitiveType.Cube,aidRelief.transform,at+new Vector3(0,-.012f,.30f),new Vector3(1.7f,.018f,1.55f),mats["YY_sand"]);
            var cushion=Model("Cushion",aidRelief.transform,at+Vector3.right*.5f,1.8f,0);
            cushion.name="Dinlenme su hedefi";
            var bottle=Model("Water",aidRelief.transform,at+Vector3.left*.5f,1.6f,0);bottle.name="Efe’ye verilecek su";var hit=bottle.AddComponent<BoxCollider>();hit.size=new Vector3(.3f,.7f,.3f);hit.center=Vector3.up*.2f;
            WorldPortrait("Efe’nin dinlenme yeri","Efe",aidRelief.transform,cushion.transform.position+Vector3.up*.16f,.38f);
            var g=new YanYanaGraphAuthor(bottle,"Hazırlık varsa çantadan, yoksa yardım masasından su al");var drag=g.Add(new OnDrag());g.Bind(drag.target,bottle);var can=g.Branch(drag.trigger,AtWork(g,"relief"));var point=PointerOnPlane(g,can.ifTrue,drag.data,at.y);g.Set(point.path,typeof(Transform),"position",bottle.transform,point.point);
            var end=g.Add(new OnEndDrag());g.Bind(end.target,bottle);can=g.Branch(end.trigger,AtWork(g,"relief"));var fit=g.Branch(can.ifTrue,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",bottle.transform),cushion.transform.position).result,.42f));
            var p=g.SetVar(fit.ifTrue,"ReliefWaterGiven",1,flow);p=g.Active(p,bottle,false);p=Text(g,p,lineText,"Efe: Suyum geldi. Yiyeceği de kontrol edelim.");p=g.Send(p,flow,"CommitCheckpoint");g.Send(p,flow,"CheckReliefSupplies");g.Set(fit.ifFalse,typeof(Transform),"position",bottle.transform,at+Vector3.left*.5f);g.Dirty();
            var restoreWater=main.Event("RestoreAid");p=main.Set(restoreWater.trigger,typeof(Transform),"position",bottle.transform,at+Vector3.left*.5f);main.Active(p,bottle,Is(main,main.Var("ReliefWaterGiven"),0));
            var foodRest=at+new Vector3(-.5f,0,.70f);var foodPlace=at+new Vector3(.50f,0,.70f);
            var food=SupplyModel("Food",aidRelief.transform,foodRest,.27f);food.name="Efe’ye verilecek yiyecek";var foodHit=food.AddComponent<BoxCollider>();foodHit.center=Vector3.up*.15f;foodHit.size=new Vector3(.35f,.45f,.4f);
            Shape("Efe’nin yiyecek tabağı",PrimitiveType.Cylinder,aidRelief.transform,foodPlace,new Vector3(.40f,.018f,.38f),mats["YY_cream"]);
            var fg=new YanYanaGraphAuthor(food,"Uygun paketi çantadan veya yardım masasından getir");var foodDrag=fg.Add(new OnDrag());fg.Bind(foodDrag.target,food);var foodCan=fg.Branch(foodDrag.trigger,AtWork(fg,"relief"));var foodPoint=PointerOnPlane(fg,foodCan.ifTrue,foodDrag.data,foodRest.y);fg.Set(foodPoint.path,typeof(Transform),"position",food.transform,foodPoint.point);
            var foodEnd=fg.Add(new OnEndDrag());fg.Bind(foodEnd.target,food);foodCan=fg.Branch(foodEnd.trigger,AtWork(fg,"relief"));var foodFits=fg.Branch(foodCan.ifTrue,fg.Binary<Less>(fg.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},fg.Get(typeof(Transform),"position",food.transform),foodPlace).result,.25f));
            p=fg.SetVar(foodFits.ifTrue,"ReliefFoodGiven",1,flow);p=fg.Active(p,food,false);p=fg.Send(p,flow,"CommitCheckpoint");fg.Send(p,flow,"CheckReliefSupplies");fg.Set(foodFits.ifFalse,typeof(Transform),"position",food.transform,foodRest);fg.Dirty();
            var restoreFood=main.Event("RestoreAid");p=main.Set(restoreFood.trigger,typeof(Transform),"position",food.transform,foodRest);main.Active(p,food,Is(main,main.Var("ReliefFoodGiven"),0));
            foreach(var supply in new[]{("Water",at+new Vector3(-.5f,.04f,-.23f)),("Food",foodRest+new Vector3(0,.04f,.33f))})
            {
                var label=WorldText("",aidRelief.transform,supply.Item2,.071f,Ink);label.name="Malzeme kaynağı · "+supply.Item1;label.transform.rotation=Quaternion.Euler(90,0,0);
                var updateSource=main.Event("RestoreAid");var ownSupply=main.Branch(updateSource.trigger,And(main,PackedForTravel(main,supply.Item1),Is(main,main.Var(supply.Item1+"Ready"),1)));
                Text(main,ownSupply.ifTrue,label,"ÇANTADAN");Text(main,ownSupply.ifFalse,label,"MASADAN");
            }
            var checkedSupplies=main.Event("CheckReliefSupplies");var both=main.Branch(checkedSupplies.trigger,And(main,Is(main,main.Var("ReliefWaterGiven"),1),Is(main,main.Var("ReliefFoodGiven"),1)));
            p=main.SetVar(both.ifTrue,"ReliefGiven",1);p=main.SetVar(p,"AidStage",4);p=Text(main,p,lineText,"Efe: Teşekkür ederim. Şimdi ailemizi birlikte bulalım.");p=main.Send(p,flow,"CommitCheckpoint");main.Send(p,flow,"AdvanceAid");
            var reliefView=WorkCamera("relief",at+new Vector3(0,.10f,.30f),1.55f,false);reliefView.transform.position=at+new Vector3(0,2.7f,-1.8f);reliefView.transform.LookAt(at+new Vector3(0,.10f,.30f));
            var reliefCutaway=new YanYanaGraphAuthor(flow,"Malzeme yakın planında ön plandaki görevli kadrajı kapatmaz");var reliefFrame=reliefCutaway.Add(new Unity.VisualScripting.LateUpdate());var reliefPath=reliefFrame.trigger;
            foreach(var renderer in cast["Bora"].GetComponentsInChildren<Renderer>())reliefPath=reliefCutaway.Set(reliefPath,typeof(Renderer),"enabled",renderer,Is(reliefCutaway,Is(reliefCutaway,reliefCutaway.Var("Workspace",flow),"relief"),false));reliefCutaway.Dirty();

            // A child-height surface rests on the separately authored folding table.
            // Reshape only this new station's table parts; the canopy retains its scale.
            foreach(var piece in world.GetComponentsInChildren<Renderer>())if(piece.name.StartsWith("Masa")&&Vector3.Distance(piece.bounds.center,table)<1.8f)
            {
                var tablePartPosition=piece.transform.position;tablePartPosition.y=table.y+(tablePartPosition.y-table.y)*.81f;piece.transform.position=tablePartPosition;
                var tablePartScale=piece.transform.localScale;var localUp=piece.transform.InverseTransformDirection(Vector3.up);int upAxis=Mathf.Abs(localUp.x)>Mathf.Abs(localUp.y)?0:1;if(Mathf.Abs(localUp.z)>Mathf.Abs(localUp[upAxis]))upAxis=2;tablePartScale[upAxis]*=.81f;piece.transform.localScale=tablePartScale;
            }
            familyDesk=Group("Görevlinin aile doğrulama masası",interactions.transform,table+Vector3.up*.61f);familyDesk.SetActive(false);
            var baseAt=familyDesk.transform.position;Shape("Açık kayıt yüzeyi",PrimitiveType.Cube,familyDesk.transform,Vector3.zero,new Vector3(1.45f,.035f,1.2f),mats["YY_cream"]);
            familyEvidence=Group("Doğrulanacak iki aile bilgisi",familyDesk.transform,new Vector3(0,.04f,.32f));familyEvidence.SetActive(false);
            WorldPortrait("Kayıttaki anne","Derya",familyEvidence.transform,new Vector3(-.25f,0,0),.25f);WorldPortrait("Kayıttaki baba","Emre",familyEvidence.transform,new Vector3(-.05f,0,0),.25f);FamilyTreeSymbol(familyEvidence.transform,new Vector3(.32f,0,0),.7f);
            var document=Model("FamilyCard",familyDesk.transform,new Vector3(-.48f,.03f,.35f),1.7f,0);document.name="Aile bilgisine başvur";var docHit=document.AddComponent<BoxCollider>();docHit.size=new Vector3(.35f,.35f,.3f);
            document.transform.localRotation=Quaternion.Euler(90,0,0);var bookLabel=WorldText("BİLGİ",familyDesk.transform,new Vector3(-.47f,.10f,.32f),.045f,Ink);bookLabel.transform.rotation=Quaternion.Euler(90,0,0);
            var dg=new YanYanaGraphAuthor(document,"Çantadaki aile kartını veya görevlinin kaydını aç");var click=dg.Add(new OnPointerClick());dg.Bind(click.target,document);can=dg.Branch(click.trigger,AtWork(dg,"family"));p=dg.SetVar(can.ifTrue,"EvidenceOpen",1,flow);p=dg.Active(p,familyEvidence,true);
            var own=dg.Branch(p,PackedForTravel(dg,"FamilyCard"));p=Text(dg,own.ifTrue,lineText,"Ada: Aile kartımız yanımızda. İki bilgiyi karşılaştıralım.");dg.Send(p,flow,"PrepareFamilyInput");p=Text(dg,own.ifFalse,lineText,"Bora: Görevli kaydını açtım. Bilgileri birlikte doğrulayabiliriz.");dg.Send(p,flow,"PrepareFamilyInput");dg.Dirty();
            for(int kind=0;kind<2;kind++)
            {
                var slot=new Vector3(-.27f+kind*.54f,.06f,.03f);var slotShape=Shape("Doğrulama yuvası "+kind,PrimitiveType.Cube,familyDesk.transform,slot-Vector3.up*.018f,new Vector3(.42f,.015f,.3f),mats["YY_tealDark"]);if(kind==1)physicalItems["FamilyMarkerSlot"]=slotShape;
                for(int choice=0;choice<2;choice++)
                {
                    var rest=new Vector3(-.50f+kind*.68f+choice*.30f,.07f,-.38f);var token=Group("Bilgi parçası "+kind+" "+choice,familyDesk.transform,rest);if(kind==1)physicalItems["FamilyMarkerToken"+choice]=token;Shape("Bilgi kartı",PrimitiveType.Cube,token.transform,Vector3.zero,new Vector3(.27f,.025f,.27f),mats["YY_white"]);
                    if(kind==0){WorldPortrait("Aile portresi",choice==0?"Derya":"Yusuf",token.transform,new Vector3(-.045f,.025f,0),.16f);WorldPortrait("İkinci aile portresi",choice==0?"Emre":"Bora",token.transform,new Vector3(.07f,.026f,0),.16f);}
                    else if(choice==0)FamilyTreeSymbol(token.transform,new Vector3(0,.03f,0),.50f);else {Shape("Farklı güneş işareti",PrimitiveType.Sphere,token.transform,new Vector3(0,.035f,0),new Vector3(.16f,.025f,.16f),mats["YY_mustard"]);for(int ray=0;ray<8;ray++){float angle=ray*Mathf.PI/4;var r=Shape("Güneş çizgisi",PrimitiveType.Cube,token.transform,new Vector3(Mathf.Cos(angle)*.11f,.035f,Mathf.Sin(angle)*.11f),new Vector3(.04f,.016f,.012f),mats["YY_mustard"]);r.transform.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);}}
                    var ch=token.AddComponent<BoxCollider>();ch.size=new Vector3(.31f,.18f,.31f);var tg=new YanYanaGraphAuthor(token,"Görsel kayıttaki iki bilgiyi ayrı ayrı karşılaştır");
                    var move=tg.Add(new OnDrag());tg.Bind(move.target,token);var allowed=tg.Branch(move.trigger,And(tg,And(tg,AtWork(tg,"family"),Is(tg,tg.Var("EvidenceOpen",flow),1)),Is(tg,tg.Var(kind==0?"IdentityPeople":"IdentityMarker",flow),0)));var projected=PointerOnPlane(tg,allowed.ifTrue,move.data,baseAt.y+.07f);tg.Set(projected.path,typeof(Transform),"position",token.transform,projected.point);
                    var drop=tg.Add(new OnEndDrag());tg.Bind(drop.target,token);allowed=tg.Branch(drop.trigger,And(tg,And(tg,AtWork(tg,"family"),Is(tg,tg.Var("EvidenceOpen",flow),1)),Is(tg,tg.Var(kind==0?"IdentityPeople":"IdentityMarker",flow),0)));var matched=tg.Branch(allowed.ifTrue,And(tg,choice==0,tg.Binary<Less>(tg.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},tg.Get(typeof(Transform),"position",token.transform),baseAt+slot).result,.20f)));
                    p=tg.Set(matched.ifTrue,typeof(Transform),"position",token.transform,baseAt+slot);p=tg.SetVar(p,kind==0?"IdentityPeople":"IdentityMarker",1,flow);p=tg.Send(p,flow,"CommitCheckpoint");tg.Send(p,flow,"CheckFamilyIdentity");p=tg.Set(matched.ifFalse,typeof(Transform),"localPosition",token.transform,rest);Text(tg,p,lineText,"Bora: Resimleri yan yana karşılaştıralım. İkisi de aynı olmalı.");tg.Dirty();
                    var restoreToken=main.Event("RestoreAid");var already=main.Branch(restoreToken.trigger,And(main,choice==0,Is(main,main.Var(kind==0?"IdentityPeople":"IdentityMarker"),1)));main.Set(already.ifTrue,typeof(Transform),"localPosition",token.transform,slot);main.Set(already.ifFalse,typeof(Transform),"localPosition",token.transform,rest);
                }
            }
            var familyView=WorkCamera("family",baseAt+new Vector3(0,0,-.02f),1.47f);var familyLens=familyView.Lens;familyLens.NearClipPlane=1.65f;familyView.Lens=familyLens;
            // The close document view cuts the canopy away; keep the foreground
            // visitor out of this close view too, rather than slicing through their body.
            var visitorCutaway=new YanYanaGraphAuthor(flow,"Aile belgesinin yakın kadrajında önü açık tut");var visitorFrame=visitorCutaway.Add(new Unity.VisualScripting.Update());var visitorPath=visitorFrame.trigger;
            foreach(var renderer in aidVisitors[2].GetComponentsInChildren<Renderer>())visitorPath=visitorCutaway.Set(visitorPath,typeof(Renderer),"enabled",renderer,Is(visitorCutaway,Is(visitorCutaway,visitorCutaway.Var("Workspace",flow),"family"),false));visitorCutaway.Dirty();
            var roofs=world.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("YumusakTente")||r.name.StartsWith("TenteDikisi")).ToArray();
            var cutaway=new YanYanaGraphAuthor(flow,"Masayı incelerken yalnız çadır çatısını saydamlaştır");var update=cutaway.Add(new Unity.VisualScripting.Update());p=update.trigger;
            foreach(var roof in roofs)
            {
                var nearRoof=cutaway.Binary<Less>(cutaway.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},cutaway.Get(typeof(Transform),"position",cast["Ada"].transform),roof.bounds.center).result,3.5f);
                p=cutaway.Set(p,typeof(Renderer),"enabled",roof,Is(cutaway,Or(cutaway,Is(cutaway,cutaway.Var("Workspace",flow),"family"),nearRoof),false));
            }
            cutaway.Dirty();
            var check=main.Event("CheckFamilyIdentity",true);var verified=main.Branch(check.trigger,And(main,Is(main,main.Var("IdentityPeople"),1),Is(main,main.Var("IdentityMarker"),1)));p=main.SetVar(verified.ifTrue,"Busy",true);p=main.SetVar(p,"InfoVerified",1);p=Text(main,p,lineText,"Bora: İki bilgi de eşleşti. Ailenizden haber aldık!");p=main.Wait(p,1.1f);p=main.SetVar(p,"AidStage",3);p=main.Send(p,flow,"CommitCheckpoint");main.Send(p,flow,"AdvanceAid");
            var restore=main.Event("RestoreAid");main.Active(restore.trigger,familyEvidence,Is(main,main.Var("EvidenceOpen"),1));CreateEfeFamilyContribution();
        }
        static void FamilyTreeSymbol(Transform parent,Vector3 at,float scale)
        {
            var root=Group("Ailenin ağaç işareti",parent,at);root.transform.localScale=Vector3.one*scale;
            Shape("Ağaç gövdesi",PrimitiveType.Cube,root.transform,new Vector3(0,.015f,-.08f),new Vector3(.07f,.025f,.18f),mats["YY_wood"]);
            Shape("Ağaç tacı",PrimitiveType.Sphere,root.transform,new Vector3(0,.025f,.04f),new Vector3(.28f,.035f,.23f),mats["YY_teal"]);
        }

        static void CreatePhysicalFinals()
        {
            finalWorlds.Clear();finalDestinations.Clear();InitialPhysical("FinalStep");InitialPhysical("Reunited");
            var begin=main.Event("BeginReunion",true);var p=main.SetVar(begin.trigger,"Busy",true);p=main.SetVar(p,"Phase",7);p=main.SetVar(p,"Workspace","");p=main.Active(p,familyDesk,false);p=main.Active(p,aidRelief,false);p=main.Active(p,aidRequests,false);
            foreach(var view in workCameras.Values)p=main.Active(p,view.gameObject,false);
            p=main.Send(p,flow,"EnterRole","Ada");p=main.Wait(p,1.1f);
            var alternate=main.Branch(p,Or(main,Is(main,main.Var("FacadeReported"),1),Is(main,main.Var("FireAssisted"),1)));
            p=main.SetVar(alternate.ifTrue,"Ending",1);main.Send(p,flow,"StageReunion");var planned=main.Branch(alternate.ifFalse,Is(main,main.Var("FamilyPlan"),1));p=main.SetVar(planned.ifTrue,"Ending",2);main.Send(p,flow,"StageReunion");var neighbour=main.Branch(planned.ifFalse,Is(main,main.Var("NeighborTogether"),1));p=main.SetVar(neighbour.ifTrue,"Ending",3);main.Send(p,flow,"StageReunion");p=main.SetVar(neighbour.ifFalse,"Ending",4);main.Send(p,flow,"StageReunion");
            for(int ending=1;ending<=4;ending++)
            {
                var destination=ending==1?street["AlternateMeeting"].position:ending==4?aidCenter+new Vector3(-2.5f,0,-5.8f):street["PrimaryMeeting"].position+Vector3.right*(ending==3?1.3f:0);
                finalDestinations[ending]=destination;var root=Group("Buluşma yolu "+ending,world.transform);root.SetActive(false);finalWorlds[ending]=root;
                var first=ending==1?new Vector3(8,-.48f,-22.4f):ending==2?new Vector3(-3,-.48f,-23.5f):ending==3?new Vector3(-2,-.48f,-24.2f):new Vector3(-1,-.48f,-25.7f);
                CreateReunionViews(ending,first,destination);
                var sign=NeedSymbol(2,root.transform,first+Vector3.up*.055f,1.2f);sign.name="Final yol işareti "+ending;FamilyTreeSymbol(root.transform,first+Vector3.up*.10f,2f);var sh=sign.AddComponent<BoxCollider>();sh.size=new Vector3(1.0f,.55f,1.0f);
                var sg=new YanYanaGraphAuthor(sign,"Ailenin bulunduğu yere doğru ilk adımı oyna");var click=sg.Add(new OnPointerClick());sg.Bind(click.target,sign);var can=sg.Branch(click.trigger,And(sg,And(sg,CanExplore(sg),Is(sg,sg.Var("Ending",flow),ending)),Is(sg,sg.Var("FinalStep",flow),0)));sg.Send(can.ifTrue,flow,"WalkFinalFirst"+ending);sg.Dirty();
                var move=main.Event("WalkFinalFirst"+ending,true);p=main.SetVar(move.trigger,"Busy",true);p=WalkAndWait(main,p,movers["Ada"],first+Vector3.forward*.6f);p=main.SetVar(p,"FinalStep",1);p=Text(main,p,lineText,new[]{"Bora: İşaretleri izledin. Ailen yeni toplanma alanında.","Efe: İşte büyük ağaç! Birlikte denediğimiz yeri bulduk.","Yusuf: Ailenizi görüyorum. Şu ağacın yanında bekliyorlar.","Bora: Doğruladığımız bilgilerle anons yaptık. Aileniz geliyor."}[ending-1]);p=Text(main,p,goalText,"Ailene yaklaş ve işaret ver");p=Text(main,p,gestureText,"Ailenin bulunduğu yere dokun");p=SetReunionView(main,p,ending,1);p=main.Wait(p,.8f);p=Released(main,p);p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
                var reunion=Group("Aileye kavuşma hedefi "+ending,root.transform,destination+Vector3.forward*.5f);var rh=reunion.AddComponent<BoxCollider>();rh.center=Vector3.up*.8f;rh.size=new Vector3(2.2f,1.7f,1.1f);
                var rg=new YanYanaGraphAuthor(reunion,"Buluşmayı yürüyerek ve aile işaretiyle tamamla");click=rg.Add(new OnPointerClick());rg.Bind(click.target,reunion);can=rg.Branch(click.trigger,And(rg,And(rg,CanExplore(rg),Is(rg,rg.Var("Ending",flow),ending)),Is(rg,rg.Var("FinalStep",flow),1)));rg.Send(can.ifTrue,flow,"ReachFamily"+ending);rg.Dirty();
                var siblingPlace=Group("Efe’nin aile buluşma yeri "+ending,root.transform,destination+new Vector3(-.65f,0,1.1f));
                var reach=main.Event("ReachFamily"+ending,true);p=main.SetVar(reach.trigger,"Busy",true);p=WalkAndWait(main,p,movers["Ada"],destination+new Vector3(.35f,0,1.1f));p=main.Set(p,typeof(Transform),"rotation",cast["Ada"].transform,Quaternion.Euler(0,180,0));p=GatherSiblingForReunion(main,p,siblingPlace.transform);p=main.SetVar(p,"FinalStep",2);p=Text(main,p,goalText,"Aile işaretini birlikte yap");p=Text(main,p,gestureText,"Ada’ya dokunup yukarı doğru işaret ver");p=SetReunionView(main,p,ending,2);p=main.Wait(p,.8f);p=Released(main,p);p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
                var stage=main.Event("StageReunion",true);var chosen=main.Branch(stage.trigger,Is(main,main.Var("Ending"),ending));p=main.SetVar(chosen.ifTrue,"Busy",true);p=main.Active(p,root,true);p=main.Do(p,typeof(StoryPlayerMovement),"Warp",movers["Derya"],new[]{typeof(Vector3)},destination+new Vector3(-.42f,0,0));p=main.Set(p,typeof(Transform),"position",cast["Emre"].transform,destination+new Vector3(.46f,0,0));p=main.Set(p,typeof(Transform),"rotation",cast["Derya"].transform,Quaternion.identity);p=main.Set(p,typeof(Transform),"rotation",cast["Emre"].transform,Quaternion.identity);
                p=Text(main,p,chapterText,new[]{"YENİ YOLDA BİRLİKTE","SÖZ VERDİĞİMİZ YERDE","KOMŞU ELİ","SESİMİZİ DUYDULAR"}[ending-1]);p=Text(main,p,goalText,ending==1?"Yeni yolun işaretini takip et":ending==2?"Tanıdığın aile ağacını bul":ending==3?"Yusuf’un gösterdiği yere yürü":"Görevlinin buluşma işaretine ulaş");p=Text(main,p,gestureText,"Yerdeki aile işaretine dokun");p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);p=SetReunionView(main,p,ending,main.Var("FinalStep"));p=main.Wait(p,.8f);p=Released(main,p);p=main.SetVar(p,"Busy",false);p=main.Send(p,flow,"PresentReunionStep");main.Send(p,flow,"CommitCheckpoint");
            }
            // A restored checkpoint must describe its saved interaction. A saved
            // greeting restarts that short animation and completes normally.
            var present=main.Event("PresentReunionStep");var savedStep=main.Add(new SwitchOnInteger{options=new List<int>{0,1,2,3}});main.Bind(savedStep.selector,main.Var("FinalStep"));main.Link(present.trigger,savedStep.enter);
            p=Text(main,savedStep.branches[1].Value,goalText,"Ailene yaklaş ve işaret ver");Text(main,p,gestureText,"Ailenin bulunduğu yere dokun");
            p=main.Do(savedStep.branches[2].Value,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=Text(main,p,goalText,"Aile işaretini birlikte yap");Text(main,p,gestureText,"Ada’ya dokunup yukarı doğru işaret ver");
            p=main.Do(savedStep.branches[3].Value,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);main.Send(p,flow,"FinishPhysicalAdventure");
            var adaGraph=new YanYanaGraphAuthor(cast["Ada"],"Aile işaretini kendin yap");var drag=adaGraph.Add(new OnDrag());adaGraph.Bind(drag.target,cast["Ada"]);var gestureDistance=adaGraph.Binary<ScalarSubtract>(adaGraph.Get(typeof(Vector2),"y",adaGraph.Get(typeof(PointerEventData),"position",drag.data)),adaGraph.Get(typeof(Vector2),"y",adaGraph.Get(typeof(PointerEventData),"pressPosition",drag.data)));var allowed=adaGraph.Branch(drag.trigger,And(adaGraph,And(adaGraph,Available(adaGraph),Is(adaGraph,adaGraph.Var("FinalStep",flow),2)),adaGraph.Binary<Greater>(gestureDistance,adaGraph.Binary<ScalarMultiply>(adaGraph.Get(typeof(Screen),"height"),.045f))));adaGraph.Send(allowed.ifTrue,flow,"FinishPhysicalAdventure");adaGraph.Dirty();
            var finish=main.Event("FinishPhysicalAdventure",true);p=main.SetVar(finish.trigger,"Busy",true);p=main.SetVar(p,"FinalStep",3);p=main.SetVar(p,"Reunited",1);p=Text(main,p,goalText,"Yeniden yan yanayız");p=Text(main,p,gestureText,"Ailen işaretine karşılık verdi");p=Text(main,p,lineText,"Derya: Yan yanayız. Hazırlıklarımızı ve yardımlarımızı birlikte hatırlayalım.");p=main.Do(p,typeof(StoryPlayerMovement),"Stop",movers["Ada"],NoArgs);
            var select=main.Add(new SwitchOnInteger{options=new List<int>{1,2,3,4}});main.Bind(select.selector,main.Var("Ending"));main.Link(p,select.enter);
            foreach(var branch in select.branches){var q=main.Active(branch.Value,workCameras["reunion"+branch.Key].gameObject,true);q=main.Wait(q,2.3f);main.Send(q,flow,"ShowPhysicalOutcome");}
            var outcome=main.Event("ShowPhysicalOutcome");p=main.SetVar(outcome.trigger,"Phase",8);p=main.Active(p,endingPanel,true);p=main.Send(p,flow,"CommitCheckpoint");var titles=main.Add(new SwitchOnInteger{options=new List<int>{1,2,3,4}});main.Bind(titles.selector,main.Var("Ending"));main.Link(p,titles.enter);
            foreach(var branch in titles.branches)Text(main,branch.Value,endingText,new[]{"Yeni Yolda Birlikte","Söz Verdiğimiz Yerde","Komşu Eli","Sesimizi Duydular"}[branch.Key-1]);
            var cards=main.Event("ShowPhysicalOutcome");var light=main.Branch(cards.trigger,And(main,Is(main,main.Var("FlashlightReady"),1),PackedForTravel(main,"Flashlight")));
            Text(main,light.ifTrue,causeCards[0],"Feneri denedin → Karanlıkta kullandın.");Text(main,light.ifFalse,causeCards[0],"Fener eksikti → Acil ışığı buldun.");
            var helped=main.Branch(main.Event("ShowPhysicalOutcome").trigger,Is(main,main.Var("NeighborTogether"),1));Text(main,helped.ifTrue,causeCards[1],"Geçişi açtın → Yusuf birlikte yürüdü.");Text(main,helped.ifFalse,causeCards[1],"Durumu ilettin → Görevliler destek oldu.");
            var detour=main.Branch(main.Event("ShowPhysicalOutcome").trigger,Is(main,main.Var("Ending"),1));Text(main,detour.ifTrue,causeCards[2],"Yol değişti → Yeni alanda buluştunuz.");Text(main,detour.ifFalse,causeCards[2],"Bilgiyi doğruladın → Ailene ulaştın.");
        }
        static void CreateReunionViews(int ending,Vector3 first,Vector3 destination)
        {
            var entry=aidCenter+Vector3.forward*1.1f;
            var routeFocus=(entry+first)*.5f+Vector3.up*.3f;
            var route=WorkCamera("reunionRoute"+ending,routeFocus,ending==2||ending==3?6.4f:5.4f,false);route.transform.position=routeFocus+new Vector3(0,7,5);route.transform.LookAt(routeFocus);
            var approachFocus=(first+destination)*.5f+Vector3.up*.55f;
            var approach=WorkCamera("reunionApproach"+ending,approachFocus,ending==2||ending==3?4.9f:4.2f,false);
            // Look along the diagonal walk so both generations fit the portrait
            // frame; a side view clipped Derya while Ada sat on the other edge.
            approach.transform.position=approachFocus+(ending==2||ending==3?new Vector3(5,7,4.8f):new Vector3(ending==4?-1.5f:0,5,3.5f));approach.transform.LookAt(approachFocus);
            var closeFocus=destination+new Vector3(ending==3?.35f:0,.65f,.7f);
            // Yusuf stands alongside the children in his ending. Include his
            // face and cane instead of cropping him at the left frame edge.
            var close=WorkCamera("reunion"+ending,closeFocus,ending==3?3.05f:2.75f,false);close.transform.position=destination+(ending==4?new Vector3(-1.6f,2.3f,3.5f):new Vector3(ending==3?-.20f:.55f,2.1f,4.2f));close.transform.LookAt(closeFocus);
        }
        static ControlOutput SetReunionView(YanYanaGraphAuthor g,ControlOutput before,int ending,object step)
        {
            var p=g.Active(before,workCameras["reunionRoute"+ending].gameObject,Is(g,step,0));p=g.Active(p,workCameras["reunionApproach"+ending].gameObject,Is(g,step,1));return g.Active(p,workCameras["reunion"+ending].gameObject,g.Binary<GreaterOrEqual>(step,2));
        }
        static ControlOutput GatherSiblingForReunion(YanYanaGraphAuthor g,ControlOutput before,Transform place)
        {
            var agent=cast["Efe"].GetComponent<NavMeshAgent>();
            var p=Text(g,before,goalText,"Efe ile yan yana gel");p=Text(g,p,gestureText,"Birlikte aile işaretinizi yapacaksınız");
            p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=g.Set(p,typeof(StorySiblingFollower),"target",follower,place);p=g.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,null);
            p=g.Set(p,typeof(StorySiblingFollower),"followDistance",follower,.10f);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.10f);p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
            var arrived=g.Add(new WaitUntilUnit());g.Bind(arrived.condition,And(g,Is(g,g.Var("Paused"),false),g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Efe"].transform),place.position).result,.23f)));g.Link(p,arrived.enter);
            p=g.Do(arrived.exit,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=g.Set(p,typeof(Transform),"rotation",cast["Efe"].transform,Quaternion.Euler(0,180,0));
            p=g.Set(p,typeof(StorySiblingFollower),"target",follower,cast["Ada"].transform);p=g.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,movers["Ada"]);p=g.Set(p,typeof(StorySiblingFollower),"followDistance",follower,1.05f);return g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,1.05f);
        }
    }
}
