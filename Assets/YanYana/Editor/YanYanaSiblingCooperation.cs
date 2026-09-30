// Editor authoring only: all interaction, walking and arm contact run in native scene graphs.
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static ValueOutput EfeCanHelp(YanYanaGraphAuthor g)=>And(g,Is(g,g.Var("SiblingSupported",flow),1),Is(g,g.Var("FamilyPlan",flow),1));
        static ControlOutput SolveArmContact(YanYanaGraphAuthor g,ControlOutput p,Animator animator,object goal,bool right=false)
        {
            return SolveGripArm(g,p,right?"Ada":"Efe",goal,right,"Soft",Vector3.up);
        }
        static ControlOutput RestoreEfeFollowing(YanYanaGraphAuthor g,ControlOutput p,float distance)
        {
            p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            p=g.Set(p,typeof(StorySiblingFollower),"target",follower,cast["Ada"].transform);p=g.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,movers["Ada"]);
            p=g.Set(p,typeof(StorySiblingFollower),"followDistance",follower,distance);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",cast["Efe"].GetComponent<NavMeshAgent>(),distance);
            return g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
        }
        static void CreateEfeFamilyContribution()
        {
            main.Initial("EfeHelpStage",0);main.Initial("EfeHelpProgress",0f);main.Initial("EfeCardStart",Vector3.zero);main.Initial("EfeWasSettling",false);
            var efe=cast["Efe"];var animator=efe.GetComponentInChildren<Animator>();var hand=animator.GetBoneTransform(HumanBodyBones.LeftHand);var agent=efe.GetComponent<NavMeshAgent>();
            float followDistance=new SerializedObject(follower).FindProperty("followDistance").floatValue;
            var slot=new Vector3(.27f,.06f,-.46f);var target=familyDesk.transform.position+slot;
            var approach=Group("Efe’nin aile masasına yaklaşma noktası",interactions.transform,familyDesk.transform.position+new Vector3(.43f,-.61f,-.73f));
            var aside=Group("Efe’nin masanın yanında açtığı yer",interactions.transform,familyDesk.transform.position+new Vector3(1.0f,-.61f,-.95f));
            var card=Group("Efe’nin hatırladığı aile kartı",familyDesk.transform,slot);Shape("Efe’nin ağaç kartı",PrimitiveType.Cube,card.transform,Vector3.zero,new Vector3(.27f,.025f,.27f),mats["YY_white"]);FamilyTreeSymbol(card.transform,new Vector3(0,.03f,0),.50f);card.SetActive(false);
            var closeFocus=familyDesk.transform.position+new Vector3(.15f,.03f,-.42f);var view=WorkCamera("familyEfe",closeFocus,1.14f,false);view.transform.position=closeFocus+new Vector3(1.8f,.85f,.50f);view.transform.LookAt(closeFocus);
            var g=new YanYanaGraphAuthor(flow,"Efe öğrendiği işareti yürüyerek ve kendi eliyle eşleştirir");g.Initial("EfeCardContactError",0f);
            var tick=g.Add(new Unity.VisualScripting.Update());var inFamily=And(g,Is(g,g.Var("Workspace",flow),"family"),Is(g,g.Var("Phase",flow),6));
            var alreadyAside=And(g,Is(g,g.Var("IdentityMarker",flow),1),g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",efe.transform),aside.transform.position).result,.25f));
            var shouldStart=And(g,And(g,AtWork(g,"family"),EfeCanHelp(g)),And(g,Is(g,alreadyAside,false),And(g,Is(g,g.Var("EvidenceOpen",flow),1),Is(g,g.Var("EfeHelpStage",flow),0))));
            g.Send(tick.trigger,flow,"EvaluateEfeHelp");var evaluate=g.Event("EvaluateEfeHelp");
            var prepareInput=g.Event("PrepareFamilyInput");var inputReady=g.SetVar(prepareInput.trigger,"Busy",false,flow);g.Send(inputReady,flow,"EvaluateEfeHelp");
            var begin=g.Branch(evaluate.trigger,shouldStart);var already=g.Branch(begin.ifTrue,Is(g,g.Var("IdentityMarker",flow),1));g.Send(already.ifTrue,flow,"EfeMakeRoom");
            var p=g.SetVar(already.ifFalse,"EfeHelpStage",1,flow);p=g.SetVar(p,"Busy",true,flow);p=g.SetVar(p,"EfeHelpProgress",0f,flow);
            p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=g.Set(p,typeof(StorySiblingFollower),"target",follower,approach.transform);p=g.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,null);
            p=g.Set(p,typeof(StorySiblingFollower),"followDistance",follower,.025f);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.025f);p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
            p=Text(g,p,goalText,"Efe aile işaretini hatırlıyor");p=Text(g,p,gestureText,"Efe masaya geliyor");Text(g,p,lineText,"Efe: Ağacı hatırlıyorum! Birlikte o yolu denemiştik.");
            var frame=g.Add(new Unity.VisualScripting.Update());var active=g.Branch(frame.trigger,And(g,inFamily,Is(g,g.Var("Paused",flow),false)));
            var walk=g.Branch(active.ifTrue,Is(g,g.Var("EfeHelpStage",flow),1));var arrived=g.Branch(walk.ifTrue,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",efe.transform),approach.transform.position).result,.047f));
            p=g.Do(arrived.ifTrue,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=g.Do(p,typeof(StorySiblingFollower),"FaceTowards",follower,new[]{typeof(Vector3)},target+Vector3.forward*.4f);
            p=g.SetVar(p,"EfeHelpStage",2,flow);p=g.SetVar(p,"EfeHelpProgress",0f,flow);Text(g,p,gestureText,"Efe işareti masaya yerleştiriyor");
            var place=g.Branch(walk.ifFalse,Is(g,g.Var("EfeHelpStage",flow),2));p=g.SetVar(place.ifTrue,"EfeHelpProgress",Sum(g,g.Var("EfeHelpProgress",flow),g.Get(typeof(Time),"deltaTime")),flow);
            var done=g.Branch(p,g.Binary<GreaterOrEqual>(g.Var("EfeHelpProgress",flow),1.3f));p=g.SetVar(done.ifTrue,"IdentityMarker",1,flow);p=g.SetVar(p,"EfeContributed",1,flow);p=g.Send(p,flow,"CommitCheckpoint");g.Send(p,flow,"EfeMakeRoom");
            var room=g.Event("EfeMakeRoom");p=g.SetVar(room.trigger,"Busy",true,flow);p=g.SetVar(p,"EfeHelpStage",3,flow);p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=g.Set(p,typeof(StorySiblingFollower),"target",follower,aside.transform);p=g.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,null);p=g.Set(p,typeof(StorySiblingFollower),"followDistance",follower,.10f);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.10f);g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
            var moved=g.Branch(place.ifFalse,And(g,Is(g,g.Var("EfeHelpStage",flow),3),g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",efe.transform),aside.transform.position).result,.20f)));
            p=g.SetVar(moved.ifTrue,"EfeHelpStage",5,flow);p=g.SetVar(p,"EfeHelpProgress",0f,flow);RestoreEfeFollowing(g,p,followDistance);
            // Finish the camera blend and release the pointer before another card can be dragged.
            // A state stage avoids a stale delayed coroutine after pause/reload cancels the action.
            var settling=g.Branch(moved.ifFalse,Is(g,g.Var("EfeHelpStage",flow),5));p=g.SetVar(settling.ifTrue,"EfeHelpProgress",Sum(g,g.Var("EfeHelpProgress",flow),g.Get(typeof(Time),"deltaTime")),flow);
            var released=And(g,Is(g,g.Call(typeof(Input),"GetMouseButton",null,new[]{typeof(int)},0).result,false),Is(g,g.Get(typeof(Input),"touchCount"),0));
            var settled=g.Branch(p,And(g,g.Binary<GreaterOrEqual>(g.Var("EfeHelpProgress",flow),.88f),released));p=g.SetVar(settled.ifTrue,"EfeHelpStage",4,flow);p=g.SetVar(p,"Busy",false,flow);p=Text(g,p,goalText,"Aile portrelerini karşılaştır");p=Text(g,p,gestureText,"Eşleşen portreleri soldaki yuvaya sürükle");p=Text(g,p,lineText,"Efe: İşareti yerleştirdim. Sen de ailemizin resimlerini karşılaştır.");p=g.Send(p,flow,"CommitCheckpoint");g.Send(p,flow,"CheckFamilyIdentity");
            var late=g.Add(new Unity.VisualScripting.LateUpdate());var helping=And(g,inFamily,And(g,g.Binary<Greater>(g.Var("EfeHelpStage",flow),0),g.Binary<Less>(g.Var("EfeHelpStage",flow),4)));
            p=g.Active(late.trigger,view.gameObject,helping);
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())if(Vector3.Distance(renderer.bounds.center,familyDesk.transform.position)<2.4f&&(renderer.name.StartsWith("KatlanirAyak")||renderer.name.StartsWith("AlanIsareti")||renderer.name.StartsWith("ResimliTabela")||renderer.name=="AİLE BULUŞMA"))p=g.Set(p,typeof(Renderer),"enabled",renderer,Is(g,inFamily,false));
            var visible=g.Branch(p,inFamily);p=g.Active(visible.ifTrue,workCameras["family"].gameObject,Is(g,helping,false));
            p=g.Active(p,card,And(g,EfeCanHelp(g),Or(g,Is(g,g.Var("IdentityMarker",flow),1),And(g,Is(g,g.Var("EfeHelpStage",flow),2),g.Binary<GreaterOrEqual>(g.Var("EfeHelpProgress",flow),.35f)))));
            var moving=g.Branch(p,And(g,Is(g,g.Var("EfeHelpStage",flow),2),Is(g,g.Var("Paused",flow),false)));
            var edge=new Vector3(.12f,.015f,-.10f);var turning=g.Branch(moving.ifTrue,g.Binary<Less>(g.Var("EfeHelpProgress",flow),.35f));g.SetVar(turning.ifTrue,"EfeCardStart",Add(g,HandContactPoint(g,"Efe",false,"Pinch"),-edge),flow);
            var t=g.Call(typeof(Mathf),"Clamp01",null,OneFloat,g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(g.Var("EfeHelpProgress",flow),.35f),.85f)).result;
            var lift=new Vector3(target.x,target.y+.02f,approach.transform.position.z+.07f);
            var raised=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Var("EfeCardStart",flow),lift,g.Call(typeof(Mathf),"Clamp01",null,OneFloat,g.Binary<ScalarDivide>(t,.45f)).result).result;
            var point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},raised,target,g.Call(typeof(Mathf),"Clamp01",null,OneFloat,g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(t,.45f),.55f)).result).result;
            p=g.Set(turning.ifFalse,typeof(Transform),"position",card.transform,point);var cardContact=Add(g,point,edge);p=SolveGripArm(g,p,"Efe",cardContact,false,"Pinch",Vector3.forward);g.SetVar(p,"EfeCardContactError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,"Efe",false,"Pinch"),cardContact).result);
            var stationary=g.Branch(moving.ifFalse,Is(g,g.Var("IdentityMarker",flow),1));g.Set(stationary.ifTrue,typeof(Transform),"position",card.transform,target);g.Dirty();
            // The short action is a state graph, without coroutines left running after cancellation.
            var restore=main.Event("RestoreAid");p=main.SetVar(restore.trigger,"EfeWasSettling",Is(main,main.Var("EfeHelpStage"),5));p=main.SetVar(p,"EfeHelpStage",0);p=main.SetVar(p,"EfeHelpProgress",0f);p=RestoreEfeFollowing(main,p,followDistance);p=main.Active(p,view.gameObject,false);
            var pausedWork=main.Branch(p,And(main,Is(main,main.Var("Paused"),true),Is(main,main.Var("Workspace"),"family")));var interruptedBlend=main.Branch(pausedWork.ifTrue,main.Var("EfeWasSettling"));p=main.SetVar(interruptedBlend.ifTrue,"EfeHelpStage",5);main.SetVar(p,"Busy",true);main.SetVar(interruptedBlend.ifFalse,"Busy",false);
            var redraw=main.Event("RestoreAid");var eligible=main.Branch(redraw.trigger,EfeCanHelp(main));p=main.Set(eligible.ifTrue,typeof(Transform),"localPosition",physicalItems["FamilyMarkerSlot"].transform,slot-Vector3.up*.018f);main.Set(p,typeof(Transform),"localScale",physicalItems["FamilyMarkerSlot"].transform,new Vector3(.42f,.015f,.27f));main.Set(eligible.ifFalse,typeof(Transform),"localPosition",physicalItems["FamilyMarkerSlot"].transform,new Vector3(.27f,.042f,.03f));
            foreach(string key in new[]{"FamilyMarkerToken0","FamilyMarkerToken1"})main.Active(main.Event("RestoreAid").trigger,physicalItems[key],Is(main,EfeCanHelp(main),false));
        }
        static void CreateSiblingCooperation()
        {
            main.Initial("SiblingDragging",false);main.Initial("SiblingPointer",-999);main.Initial("SiblingAim",Vector3.zero);main.Initial("SiblingRest",Vector3.zero);main.Initial("SiblingTarget",Vector3.zero);
            InitialPhysical("EfeContributed");
            var ada=cast["Ada"];var efe=cast["Efe"];var aa=ada.GetComponentInChildren<Animator>();var ea=efe.GetComponentInChildren<Animator>();
            var ah=aa.GetBoneTransform(HumanBodyBones.RightHand);var eh=ea.GetBoneTransform(HumanBodyBones.LeftHand);
            var center=anchors["Anchor_CoverAda"].position+new Vector3(.32f,.78f,-.97f);
            var view=WorkCamera("sibling",center,1.12f,false);view.transform.position=center+new Vector3(-.1f,.20f,-2.6f);view.transform.LookAt(center);
            var cue=Group("Efe’ye uzanan el",interactions.transform);physicalItems["SiblingHandCue"]=cue;
            Shape("Ada’nın el işareti",PrimitiveType.Sphere,cue.transform,Vector3.zero,Vector3.one*.07f,mats["YY_mustard"]);
            var hit=cue.AddComponent<SphereCollider>();hit.radius=.23f;
            var targetCue=Shape("Efe’nin uzattığı el işareti",PrimitiveType.Sphere,interactions.transform,center,Vector3.one*.08f,mats["YY_teal"]);
            physicalItems["SiblingTargetCue"]=targetCue;
            var skip=Button("SiblingContinue","Çıkışı kontrol et",safeRect,new Vector2(1,0),new Vector2(1,0),new Vector2(-270,214),new Vector2(-20,286)).gameObject;
            var begin=main.Event("BeginSiblingCheck",true);var allowed=main.Branch(begin.trigger,And(main,Is(main,main.Var("SiblingChecked"),0),Is(main,main.Var("Phase"),2)));
            var p=main.SetVar(allowed.ifTrue,"Busy",true);p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            var adaAgent=ada.GetComponent<NavMeshAgent>();float approachPadding=new SerializedObject(movers["Ada"]).FindProperty("arrivalPadding").floatValue;p=main.Set(p,typeof(NavMeshAgent),"stoppingDistance",adaAgent,.025f);p=main.Set(p,typeof(StoryPlayerMovement),"arrivalPadding",movers["Ada"],.005f);
            p=WalkAndWait(main,p,movers["Ada"],Add(main,main.Get(typeof(Transform),"position",efe.transform),new Vector3(-.43f,0,-.04f)),.047f);
            p=main.Set(p,typeof(NavMeshAgent),"stoppingDistance",adaAgent,.10f);p=main.Set(p,typeof(StoryPlayerMovement),"arrivalPadding",movers["Ada"],approachPadding);
            p=main.Do(p,typeof(StoryPlayerMovement),"FaceTowards",movers["Ada"],new[]{typeof(Vector3)},main.Get(typeof(Transform),"position",efe.transform));
            p=main.Do(p,typeof(StorySiblingFollower),"FaceTowards",follower,new[]{typeof(Vector3)},main.Get(typeof(Transform),"position",ada.transform));
            p=main.Wait(p,.35f);p=main.SetVar(p,"SiblingRest",HandContactPoint(main,"Ada",true,"Soft"));p=main.SetVar(p,"SiblingAim",main.Var("SiblingRest"));
            var midpoint=main.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},main.Get(typeof(Transform),"position",aa.GetBoneTransform(HumanBodyBones.RightUpperArm)),main.Get(typeof(Transform),"position",ea.GetBoneTransform(HumanBodyBones.LeftUpperArm)),.53f).result;
            p=main.SetVar(p,"SiblingTarget",Add(main,midpoint,Vector3.down*.05f));
            var focus=Add(main,main.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},main.Get(typeof(Transform),"position",ada.transform),main.Get(typeof(Transform),"position",efe.transform),.5f).result,new Vector3(0,.78f,-.04f));
            p=main.Set(p,typeof(Transform),"position",view.transform,Add(main,focus,new Vector3(-.1f,.20f,-2.6f)));p=main.Do(p,typeof(Transform),"LookAt",view.transform,new[]{typeof(Vector3)},focus);
            p=main.Send(p,flow,"OpenWork","sibling","Efe’ye nasıl eşlik edelim?","Efe: İyiyim. Biraz korktum. Elimi tutar mısın?");main.Send(p,flow,"CommitCheckpoint");
            var hands=new YanYanaGraphAuthor(cue,"Kardeşine kendi elinle karşılık ver");hands.Initial("AdaContactError",0f);hands.Initial("EfeContactError",0f);
            var late=hands.Add(new Unity.VisualScripting.LateUpdate());var workspace=Is(hands,hands.Var("Workspace",flow),"sibling");var visible=And(hands,workspace,Is(hands,hands.Var("SiblingChecked",flow),0));
            p=hands.Set(late.trigger,typeof(Transform),"position",cue.transform,HandContactPoint(hands,"Ada",true,"Soft"));p=hands.Set(p,typeof(Transform),"position",targetCue.transform,hands.Var("SiblingTarget",flow));
            p=hands.Set(p,typeof(Collider),"enabled",hit,visible);foreach(var r in cue.GetComponentsInChildren<Renderer>())p=hands.Set(p,typeof(Renderer),"enabled",r,visible);
            p=hands.Set(p,typeof(Renderer),"enabled",targetCue.GetComponent<Renderer>(),visible);p=hands.Active(p,skip,And(hands,visible,Available(hands)));
            foreach(var adult in new[]{"Derya","Emre"})foreach(var r in cast[adult].GetComponentsInChildren<Renderer>())p=hands.Set(p,typeof(Renderer),"enabled",r,Is(hands,workspace,false));
            var contact=hands.Branch(p,And(hands,workspace,Is(hands,hands.Var("Paused",flow),false)));p=SolveArmContact(hands,contact.ifTrue,ea,hands.Var("SiblingTarget",flow));p=SolveArmContact(hands,p,aa,hands.Var("SiblingAim",flow),true);
            p=hands.SetVar(p,"AdaContactError",hands.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(hands,"Ada",true,"Soft"),hands.Var("SiblingAim",flow)).result);
            hands.SetVar(p,"EfeContactError",hands.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(hands,"Efe",false,"Soft"),hands.Var("SiblingTarget",flow)).result);
            var down=hands.Add(new OnPointerDown());hands.Bind(down.target,cue);var ready=hands.Branch(down.trigger,And(hands,And(hands,Available(hands),visible),Is(hands,hands.Var("SiblingDragging",flow),false)));
            p=hands.SetVar(ready.ifTrue,"SiblingDragging",true,flow);hands.SetVar(p,"SiblingPointer",hands.Get(typeof(PointerEventData),"pointerId",down.data),flow);
            var drag=hands.Add(new OnDrag());hands.Bind(drag.target,cue);var owns=And(hands,And(hands,Available(hands),visible),And(hands,hands.Var("SiblingDragging",flow),Is(hands,hands.Var("SiblingPointer",flow),hands.Get(typeof(PointerEventData),"pointerId",drag.data))));
            ready=hands.Branch(drag.trigger,owns);var screenTarget=hands.Call(typeof(Camera),"WorldToScreenPoint",camera,new[]{typeof(Vector3)},hands.Var("SiblingTarget",flow)).result;var pointer=hands.Get(typeof(PointerEventData),"position",drag.data);
            var screenPoint=V3(hands,hands.Get(typeof(Vector2),"x",pointer),hands.Get(typeof(Vector2),"y",pointer),hands.Get(typeof(Vector3),"z",screenTarget));
            hands.SetVar(ready.ifTrue,"SiblingAim",hands.Call(typeof(Camera),"ScreenToWorldPoint",camera,new[]{typeof(Vector3)},screenPoint).result,flow);
            var end=hands.Add(new OnEndDrag());hands.Bind(end.target,cue);ready=hands.Branch(end.trigger,And(hands,And(hands,Available(hands),visible),And(hands,hands.Var("SiblingDragging",flow),Is(hands,hands.Var("SiblingPointer",flow),hands.Get(typeof(PointerEventData),"pointerId",end.data)))));
            var expected=V2(hands,hands.Get(typeof(Vector3),"x",screenTarget),hands.Get(typeof(Vector3),"y",screenTarget));var fit=hands.Branch(ready.ifTrue,hands.Binary<Less>(hands.Call(typeof(Vector2),"Distance",null,new[]{typeof(Vector2),typeof(Vector2)},hands.Get(typeof(PointerEventData),"position",end.data),expected).result,hands.Binary<ScalarMultiply>(hands.Get(typeof(Screen),"width"),.10f)));
            p=hands.SetVar(fit.ifTrue,"SiblingDragging",false,flow);hands.Send(p,flow,"SupportSibling");p=hands.SetVar(fit.ifFalse,"SiblingDragging",false,flow);p=hands.SetVar(p,"SiblingAim",hands.Var("SiblingRest",flow),flow);Text(hands,p,lineText,"Efe: Elim burada. Biraz daha yaklaştırabilirsin.");
            // OnPointerUp precedes OnEndDrag in Unity's input module. Only a non-drag release cancels here.
            var up=hands.Add(new OnPointerUp());hands.Bind(up.target,cue);var cancel=hands.Branch(up.trigger,And(hands,Is(hands,hands.Var("SiblingPointer",flow),hands.Get(typeof(PointerEventData),"pointerId",up.data)),Is(hands,hands.Get(typeof(PointerEventData),"dragging",up.data),false)));
            p=hands.SetVar(cancel.ifTrue,"SiblingDragging",false,flow);hands.SetVar(p,"SiblingAim",hands.Var("SiblingRest",flow),flow);hands.Dirty();
            var support=main.Event("SupportSibling",true);var notDone=main.Branch(support.trigger,And(main,AtWork(main,"sibling"),Is(main,main.Var("SiblingChecked"),0)));
            p=main.SetVar(notDone.ifTrue,"Busy",true);p=main.SetVar(p,"SiblingAim",main.Var("SiblingTarget"));p=main.SetVar(p,"SiblingChecked",1);p=main.SetVar(p,"SiblingSupported",1);p=Text(main,p,lineText,"Efe: Birlikteyiz. Çıkış yoluna beraber bakalım.");p=main.Send(p,flow,"CommitCheckpoint");p=main.Wait(p,1.1f);main.Send(p,flow,"Explore");
            p=ButtonEvent(main,"SiblingContinue","ContinueSibling");ready=main.Branch(p,And(main,AtWork(main,"sibling"),Is(main,main.Var("SiblingChecked"),0)));p=main.SetVar(ready.ifTrue,"SiblingChecked",1);p=main.SetVar(p,"SiblingSupported",0);main.Send(p,flow,"Explore");
            var restore=main.Event("RestorePhysicalObjects");p=main.SetVar(restore.trigger,"SiblingDragging",false);p=main.SetVar(p,"SiblingPointer",-999);main.SetVar(p,"SiblingAim",main.Var("SiblingRest"));
            var refresh=main.Event("RefreshSiblingGoal");var after=main.Branch(refresh.trigger,And(main,Is(main,main.Var("Phase"),2),Is(main,main.Var("SiblingChecked"),1)));var lit=main.Branch(after.ifTrue,Is(main,main.Var("LightFound"),1));
            p=Text(main,lit.ifTrue,goalText,"Güvenli çıkışını kontrol et");Text(main,p,lineText,"Ada: Birbirimizi kontrol ettik. Şimdi çıkışa bakalım.");p=Text(main,lit.ifFalse,goalText,"Çıkıştaki acil ışığı bul");var listened=main.Branch(p,Is(main,main.Var("SiblingSupported"),1));Text(main,listened.ifTrue,lineText,"Efe: Kapının yanındaki acil ışığı görüyorum.");Text(main,listened.ifFalse,lineText,"Ada: Kapının yanındaki acil ışığı kontrol edeyim.");
        }
    }
}
