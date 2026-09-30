// Editor authoring of the adventure's isolated save, replay and pause graphs.
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Story;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static readonly string[] replayIds={"map","flashlight","radio","supplyWater","supplyFood","bagfit","home","evacuation","fire","aid"};
        static ValueOutput DynamicPhysical(YanYanaGraphAuthor g,object key)
        {
            var get=g.Add(new GetVariable{kind=VariableKind.Object});g.Bind(get.name,key);g.Bind(get.@object,flow);return get.value;
        }
        static ControlOutput SetDynamicPhysical(YanYanaGraphAuthor g,ControlOutput before,object key,object value)
        {
            var set=g.Add(new SetVariable{kind=VariableKind.Object});g.Bind(set.name,key);g.Bind(set.@object,flow);g.Bind(set.input,value);g.Link(before,set.assign);return set.assigned;
        }
        static ControlOutput ForPhysical(YanYanaGraphAuthor g,ControlOutput before,Action<ControlOutput,ValueOutput> author)
        {
            var loop=g.Add(new ForEach());g.Bind(loop.collection,new List<string>(physicalKeys));g.Link(before,loop.enter);author(loop.body,loop.currentItem);return loop.exit;
        }
        static ControlOutput StorePhysical(YanYanaGraphAuthor g,ControlOutput before,object prefix)
        {
            return ForPhysical(g,before,(body,key)=>PutInt(g,body,Concat(g,prefix,key),DynamicPhysical(g,key)));
        }
        static ControlOutput LoadPhysical(YanYanaGraphAuthor g,ControlOutput before,object prefix)
        {
            return ForPhysical(g,before,(body,key)=>SetDynamicPhysical(g,body,key,g.Call(typeof(PlayerPrefs),"GetInt",null,new[]{typeof(string),typeof(int)},Concat(g,prefix,key),DynamicPhysical(g,key)).result));
        }
        static ControlOutput CapturePositions(YanYanaGraphAuthor g,ControlOutput before)
        {
            var p=before;
            foreach(var name in new[]{"Ada","Efe","Idil","Bora","Yusuf"})foreach(var axis in new[]{"x","y","z"})
                p=g.SetVar(p,"Pos"+name+axis,g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Binary<ScalarMultiply>(g.Get(typeof(Vector3),axis,g.Get(typeof(Transform),"position",cast[name].transform)),1000f)).result);
            return g.SetVar(p,"IntroYaw",g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Get(typeof(Vector3),"y",g.Get(typeof(Transform),"eulerAngles",cast["Ada"].transform))).result);
        }
        static ValueOutput SavedPosition(YanYanaGraphAuthor g,string name)=>V3(g,g.Binary<ScalarDivide>(g.Var("Pos"+name+"x"),1000f),g.Binary<ScalarDivide>(g.Var("Pos"+name+"y"),1000f),g.Binary<ScalarDivide>(g.Var("Pos"+name+"z"),1000f));
        static void CreatePhysicalPersistence()
        {
            foreach(var name in new[]{"Ada","Efe","Idil","Bora","Yusuf"})
            {
                var at=cast[name].transform.position;InitialPhysical("Pos"+name+"x",Mathf.RoundToInt(at.x*1000));InitialPhysical("Pos"+name+"y",Mathf.RoundToInt(at.y*1000));InitialPhysical("Pos"+name+"z",Mathf.RoundToInt(at.z*1000));
            }
            main.Initial("SavedWorkspace","");main.Initial("SavedRole","Ada");
            var boot=main.Add(new Unity.VisualScripting.Start());var p=main.Set(boot.trigger,typeof(Application),"targetFrameRate",null,60);p=main.Set(p,typeof(Screen),"orientation",null,ScreenOrientation.Portrait);p=main.Set(p,typeof(Time),"timeScale",null,1f);
            // The surface registers saved navigation in OnEnable; actor activation belongs in Start.
            object navigationReady=true;
            foreach(var agent in UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                p=main.Set(p,typeof(Behaviour),"enabled",agent,true);
                navigationReady=And(main,navigationReady,main.Get(typeof(NavMeshAgent),"isOnNavMesh",agent));
            }
            foreach(var mover in UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsInactive.Include,FindObjectsSortMode.None))p=main.Set(p,typeof(Behaviour),"enabled",mover,true);
            foreach(var sibling in UnityEngine.Object.FindObjectsByType<StorySiblingFollower>(FindObjectsInactive.Include,FindObjectsSortMode.None))p=main.Set(p,typeof(Behaviour),"enabled",sibling,true);
            p=main.SetVar(p,"NavigationReady",navigationReady);
            p=main.Do(p,typeof(Debug),"Log",null,new[]{typeof(object)},Concat(main,"YAN YANA navigation ready: ",main.Call(typeof(Convert),"ToString",null,new[]{typeof(bool)},main.Var("NavigationReady")).result));
            p=LoadPhysical(main,p,SavePrefix+"physical.");p=main.Send(p,flow,"MigrateIntroSave");p=main.SetVar(p,"PlaySeconds",main.Call(typeof(PlayerPrefs),"GetFloat",null,new[]{typeof(string),typeof(float)},SavePrefix+"physical.playSeconds",0f).result);p=main.SetVar(p,"SavedWorkspace",PrefString(main,SavePrefix+"workspace"));p=main.SetVar(p,"SavedRole",PrefString(main,SavePrefix+"role","Ada"));p=main.Send(p,flow,"RestorePhysicalObjects");
            var automatic=main.Branch(p,Is(main,PrefInt(main,SavePrefix+"autoContinue"),1));p=PutInt(main,automatic.ifTrue,SavePrefix+"autoContinue",0);main.Send(p,flow,"ContinueLoaded");
            var continueButton=ButtonEvent(main,"Continue","ContinuePhysical");main.Send(continueButton,flow,"ContinueLoaded");
            var cont=main.Event("ContinueLoaded",true);p=main.Active(cont.trigger,titlePanel,false);p=main.SetVar(p,"Paused",false);p=main.SetVar(p,"Busy",true);p=main.Wait(p,.15f);
            foreach(var name in new[]{"Ada","Idil","Bora"})p=main.Do(p,typeof(StoryPlayerMovement),"Warp",movers[name],new[]{typeof(Vector3)},SavedPosition(main,name));
            foreach(var name in new[]{"Efe","Yusuf"})p=main.Do(p,typeof(NavMeshAgent),"Warp",cast[name].GetComponent<NavMeshAgent>(),new[]{typeof(Vector3)},SavedPosition(main,name));
            p=main.Wait(p,.2f);p=main.Send(p,flow,"RestoreAidActors");main.Send(p,flow,"ResumePhysicalPhase");
            p=ButtonEvent(main,"New","NewPhysical");foreach(string key in physicalKeys)p=main.SetVar(p,key,(int)Variables.Object(flow).Get(key));
            p=StorePhysical(main,p,SavePrefix+"physical.");foreach(string id in replayIds)p=PutInt(main,p,SavePrefix+"snapshot."+id+".exists",0);
            p=main.Do(p,typeof(PlayerPrefs),"SetFloat",null,new[]{typeof(string),typeof(float)},SavePrefix+"physical.playSeconds",0f);p=PutString(main,p,SavePrefix+"workspace","");p=PutString(main,p,SavePrefix+"role","Ada");p=PutInt(main,p,SavePrefix+"autoContinue",1);p=main.Do(p,typeof(PlayerPrefs),"Save",null,NoArgs);main.Do(p,typeof(SceneManager),"LoadScene",null,OneString,ScenePath);
            var save=main.Event("CommitCheckpoint");p=CapturePositions(main,save.trigger);p=StorePhysical(main,p,SavePrefix+"physical.");p=PutString(main,p,SavePrefix+"workspace",main.Var("Workspace"));p=PutString(main,p,SavePrefix+"role",main.Var("Role"));p=main.Do(p,typeof(PlayerPrefs),"SetFloat",null,new[]{typeof(string),typeof(float)},SavePrefix+"physical.playSeconds",main.Var("PlaySeconds"));main.Do(p,typeof(PlayerPrefs),"Save",null,NoArgs);
            var resume=main.Event("ResumePhysicalPhase",true);p=main.SetVar(resume.trigger,"Busy",false);p=main.SetVar(p,"Workspace","");p=main.Active(p,endingPanel,false);foreach(var view in workCameras.Values)p=main.Active(p,view.gameObject,false);
            var phase=main.Add(new SwitchOnInteger{options=new List<int>{0,1,2,3,31,4,5,6,7,8}});main.Bind(phase.selector,main.Var("Phase"));main.Link(p,phase.enter);
            var intro=main.Branch(phase.branches[0].Value,Is(main,main.Var("IntroDone"),0));main.Send(intro.ifTrue,flow,"ResumeIntro");
            var workspace=main.Add(new SwitchOnString{options=new List<string>{"flashlight","radio","bag","bagfit","map","bridge","supplyWater","supplyFood"}});main.Bind(workspace.selector,main.Var("SavedWorkspace"));main.Link(intro.ifFalse,workspace.enter);
            foreach(var branch in workspace.branches)
            {
                if(branch.Key=="bridge"){main.Send(branch.Value,flow,"ToyBridge");continue;}
                if(branch.Key=="bagfit"){main.Send(branch.Value,flow,"OpenBagFit");continue;}
                if(branch.Key.StartsWith("supply")){main.Send(branch.Value,flow,"OpenSupply",branch.Key.Substring(6));continue;}
                string goal=branch.Key=="flashlight"?"Feneri çalışır hâle getir":branch.Key=="radio"?"Resmî yayını bul":branch.Key=="bag"?"Eşyalarını çantaya sığdır":"Ailece buluşma yolunu dene";
                main.Send(branch.Value,flow,"OpenWork",branch.Key,goal,"Efe: Kaldığımız yerden birlikte devam edebiliriz.");
            }
            main.Send(workspace.@default,flow,"Explore");main.Send(phase.branches[1].Value,flow,"BeginQuake");
            p=main.Set(phase.branches[2].Value,typeof(Light),"intensity",sun,.43f);p=main.Send(p,flow,"SetAfterQuake");var siblingWork=main.Branch(p,And(main,Is(main,main.Var("SavedWorkspace"),"sibling"),Is(main,main.Var("SiblingChecked"),0)));main.Send(siblingWork.ifTrue,flow,"BeginSiblingCheck");main.Send(siblingWork.ifFalse,flow,"Explore");
            main.Send(phase.branches[3].Value,flow,"Explore");p=main.SetVar(phase.branches[4].Value,"Phase",3);p=main.Do(p,typeof(StoryPlayerMovement),"Warp",movers["Ada"],new[]{typeof(Vector3)},street["StairsStart"].position);main.Send(p,flow,"Explore");main.Send(phase.branches[5].Value,flow,"Explore");
            var ready=main.Branch(phase.branches[6].Value,Is(main,main.Var("HoseReady"),1));p=main.Send(ready.ifTrue,flow,"EnterRole","Idil");p=main.Wait(p,1.1f);main.Send(p,flow,"AdvanceFire");main.Send(ready.ifFalse,flow,"EnterFirefighter");
            var working=main.Branch(phase.branches[7].Value,Is(main,main.Var("SavedRole"),"Bora"));main.Send(working.ifTrue,flow,"EnterAid");main.Send(working.ifFalse,flow,"Explore");main.Send(phase.branches[8].Value,flow,"StageReunion");main.Send(phase.branches[9].Value,flow,"ShowPhysicalOutcome");
            CreatePhysicalReplay();CreatePhysicalPause();
        }
        static void CreatePhysicalReplay()
        {
            main.Initial("SnapshotSerial",0);
            var snapshot=main.Event("CapturePhysicalSnapshot",arguments:1);var exists=Concat(main,Concat(main,SavePrefix+"snapshot.",snapshot.argumentPorts[0]),".exists");var missing=main.Branch(snapshot.trigger,Is(main,PrefInt(main,exists),0));
            var p=CapturePositions(main,missing.ifTrue);p=StorePhysical(main,p,Concat(main,Concat(main,SavePrefix+"snapshot.",snapshot.argumentPorts[0]),"."));p=PutInt(main,p,exists,1);
            p=main.SetVar(p,"SnapshotSerial",main.Call(typeof(Convert),"ToInt32",null,OneFloat,Sum(main,PrefInt(main,SavePrefix+"snapshotSerial"),1f)).result);p=PutInt(main,p,SavePrefix+"snapshotSerial",main.Var("SnapshotSerial"));p=PutInt(main,p,Concat(main,Concat(main,SavePrefix+"snapshot.",snapshot.argumentPorts[0]),".order"),main.Var("SnapshotSerial"));main.Do(p,typeof(PlayerPrefs),"Save",null,NoArgs);
            var work=main.Event("OpenWork",arguments:3);var selection=main.Add(new SwitchOnString{options=new List<string>{"map","flashlight","radio"}});main.Bind(selection.selector,work.argumentPorts[0]);main.Link(work.trigger,selection.enter);foreach(var branch in selection.branches)main.Send(branch.Value,flow,"CapturePhysicalSnapshot",branch.Key);
            main.Send(main.Event("SetAfterQuake").trigger,flow,"CapturePhysicalSnapshot","evacuation");main.Send(main.Event("EnterFirefighter").trigger,flow,"CapturePhysicalSnapshot","fire");main.Send(main.Event("EnterAid").trigger,flow,"CapturePhysicalSnapshot","aid");
            var replayPanel=Panel("Dönüp deneyebileceğin kararlar",safeRect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Paper).gameObject;replayPanel.SetActive(false);
            Label("Hangi ana dönelim?",replayPanel.transform,new Vector2(.08f,.82f),new Vector2(.92f,.94f),Vector2.zero,Vector2.zero,34,Ink,TextAlignmentOptions.Center);
            string[] titles={"Aile planı","Feneri deneme","Radyo yayını","Suyu inceleme","Yiyeceği inceleme","Çantayı taşıma","Evde hazırlık","Birlikte tahliye","İdil’in müdahalesi","Bora’nın yardım noktası"};
            for(int i=0;i<replayIds.Length;i++)
            {
                string id=replayIds[i],button="ReplayDecision"+id;float y=.69f-(i/2)*.116f;float x=.06f+(i%2)*.46f;var row=Button(button,titles[i],replayPanel.transform,new Vector2(x,y),new Vector2(x+.42f,y+.10f),Vector2.zero,Vector2.zero);
                var shown=main.Event("OpenReplayChoices");main.Set(shown.trigger,typeof(Selectable),"interactable",row,Is(main,PrefInt(main,SavePrefix+"snapshot."+id+".exists"),1));
                p=ButtonEvent(main,button,"ChooseReplay"+id);main.Send(p,flow,"ReplayPhysical",id);
            }
            Button("CloseReplay","Sonuma dön",replayPanel.transform,new Vector2(.08f,.055f),new Vector2(.92f,.135f),Vector2.zero,Vector2.zero);p=ButtonEvent(main,"CloseReplay","CloseReplay");main.Active(p,replayPanel,false);
            buttons["ReplayPlan"].GetComponentInChildren<TMP_Text>().text="Kararlarını yeniden dene";p=ButtonEvent(main,"ReplayPlan","ReplayPlanPhysical");p=main.Active(p,replayPanel,true);main.Send(p,flow,"OpenReplayChoices");p=ButtonEvent(main,"ReplayBag","ReplayBagPhysical");main.Send(p,flow,"ReplayPhysical","flashlight");
            var replay=main.Event("ReplayPhysical",arguments:1);p=main.SetVar(replay.trigger,"Busy",true);
            var has=main.Branch(p,Is(main,PrefInt(main,Concat(main,Concat(main,SavePrefix+"snapshot.",replay.argumentPorts[0]),".exists")),1));
            p=LoadPhysical(main,has.ifTrue,Concat(main,Concat(main,SavePrefix+"snapshot.",replay.argumentPorts[0]),"."));main.Send(p,flow,"CommitReplay",replay.argumentPorts[0]);
            // An unvisited preparation can be tried without erasing other completed preparation.
            p=LoadPhysical(main,has.ifFalse,SavePrefix+"snapshot.home.");
            foreach(string key in physicalKeys)
            {
                bool preparation=key.StartsWith("Found.")||key.StartsWith("Pack.")||key.StartsWith("BagCell")||key.StartsWith("FSlot")||key.StartsWith("FBattery")||new[]{"FCoverOpen","FSwitch","FlashlightReady","RadioReady","RadioDial","RadioPower","FamilyPlan","MapCell","ExitBoxMoved","ExitCleared","ShelfSecured","WardrobeSecured","BagReady","BagClosed","WaterReady","FoodReady","AidReady","BlanketReady","ContactCard","WhistleReady","Comfort"}.Contains(key);
                if(!preparation)p=main.SetVar(p,key,(int)Variables.Object(flow).Get(key));
            }
            p=main.SetVar(p,"Phase",0);main.Send(p,flow,"CommitReplay",replay.argumentPorts[0]);
            var commit=main.Event("CommitReplay",arguments:1);p=StorePhysical(main,commit.trigger,SavePrefix+"physical.");p=PutString(main,p,SavePrefix+"workspace",commit.argumentPorts[0]);p=PutString(main,p,SavePrefix+"role","Ada");p=PutInt(main,p,SavePrefix+"autoContinue",1);
            foreach(string id in replayIds)
            {
                var later=main.Branch(p,main.Binary<Greater>(PrefInt(main,SavePrefix+"snapshot."+id+".order"),PrefInt(main,Concat(main,Concat(main,SavePrefix+"snapshot.",commit.argumentPorts[0]),".order"))));var cleared=PutInt(main,later.ifTrue,SavePrefix+"snapshot."+id+".exists",0);var join=main.Add(new Unity.VisualScripting.Sequence{outputCount=1});main.Link(cleared,join.enter);main.Link(later.ifFalse,join.enter);p=join.multiOutputs[0];
            }
            // Scene reload cancels old coroutines and restores every authored object atomically.
            p=main.Do(p,typeof(PlayerPrefs),"Save",null,NoArgs);main.Do(p,typeof(SceneManager),"LoadScene",null,OneString,ScenePath);
        }
        static void CreatePhysicalPause()
        {
            CreatePresentationPause();
            var pauseButton=ButtonEvent(main,"Pause","PausePhysical");main.Send(pauseButton,flow,"PauseAllPhysical");
            var pause=main.Event("PauseAllPhysical");var firstPause=main.Branch(pause.trigger,Is(main,main.Var("Paused"),false));var p=main.SetVar(firstPause.ifTrue,"Paused",true);p=main.SetVar(p,"Dragging",false);p=main.SetVar(p,"PointerOwner",-999);p=main.Active(p,pausePanel,true);p=main.Set(p,typeof(Time),"timeScale",null,0f);
            foreach(var mover in UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsSortMode.None))
            {
                var agent=mover.GetComponent<NavMeshAgent>();string key="PauseStopped."+mover.gameObject.name;main.Initial(key,false);
                p=main.SetVar(p,key,main.Get(typeof(NavMeshAgent),"isStopped",agent));p=main.Set(p,typeof(NavMeshAgent),"isStopped",agent,true);p=main.Set(p,typeof(Behaviour),"enabled",mover,false);
            }
            foreach(var manager in fireManagers.Values){p=main.Do(p,typeof(FirefighterExtinguishManager),"SetSpraying",manager,OneBool,false);p=main.Set(p,typeof(Behaviour),"enabled",manager,false);}
            foreach(var sound in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))p=main.Do(p,typeof(AudioSource),"Pause",sound,NoArgs);
            p=main.Send(p,flow,"RestorePhysicalObjects");main.Send(p,flow,"CommitCheckpoint");
            p=ButtonEvent(main,"Resume","ResumePhysical");main.Send(p,flow,"ResumeAfterPointersRelease");
            var resumeReleased=main.Event("ResumeAfterPointersRelease",true);p=Released(main,resumeReleased.trigger);p=main.Set(p,typeof(Time),"timeScale",null,1f);p=main.Active(p,pausePanel,false);p=main.SetVar(p,"Paused",false);p=main.Send(p,flow,"ResumePresentation");
            foreach(var sound in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))p=main.Do(p,typeof(AudioSource),"UnPause",sound,NoArgs);
            foreach(var manager in fireManagers.Values)p=main.Set(p,typeof(Behaviour),"enabled",manager,false);
            foreach(var mover in UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsSortMode.None)){p=main.Set(p,typeof(Behaviour),"enabled",mover,true);p=main.Set(p,typeof(NavMeshAgent),"isStopped",mover.GetComponent<NavMeshAgent>(),main.Var("PauseStopped."+mover.gameObject.name));}
            var app=main.Add(new Unity.VisualScripting.OnApplicationPause());main.Send(app.trigger,flow,"PauseAllPhysical");
            var quit=main.Add(new Unity.VisualScripting.OnApplicationQuit());main.Send(quit.trigger,flow,"CommitCheckpoint");
            foreach(string setting in new[]{"ReducedMotion","Sound","Captions","Vibration"})
            {
                p=ButtonEvent(main,setting,"PhysicalOption"+setting);p=main.SetVar(p,setting,Is(main,main.Var(setting),false));p=main.Do(p,typeof(PlayerPrefs),"SetInt",null,new[]{typeof(string),typeof(int)},SavePrefix+"option."+setting,main.Call(typeof(Convert),"ToInt32",null,OneBool,main.Var(setting)).result);main.Send(p,flow,"ApplyPhysicalOptions");
            }
            var apply=main.Event("ApplyPhysicalOptions");p=main.Set(apply.trigger,typeof(AudioListener),"volume",null,main.Call(typeof(Convert),"ToSingle",null,OneBool,main.Var("Sound")).result);p=main.Active(p,lineText.transform.parent.gameObject,main.Var("Captions"));
            foreach(string setting in new[]{"ReducedMotion","Sound","Captions","Vibration"})
            {
                var eventUnit=main.Event("ApplyPhysicalOptions");var yes=main.Branch(eventUnit.trigger,main.Var(setting));var label=buttons[setting].GetComponentInChildren<TMP_Text>();string title=setting=="ReducedMotion"?"Kamera sarsıntısı":setting=="Sound"?"Ses":setting=="Captions"?"Altyazı":"Titreşim";Text(main,yes.ifTrue,label,title+": "+(setting=="ReducedMotion"?"sakin":"açık"));Text(main,yes.ifFalse,label,title+": "+(setting=="ReducedMotion"?"hareketli":"kapalı"));
            }
            var start=main.Add(new Unity.VisualScripting.Start());p=start.trigger;foreach(string setting in new[]{"ReducedMotion","Sound","Captions","Vibration"})p=main.SetVar(p,setting,Is(main,PrefInt(main,SavePrefix+"option."+setting,(bool)Variables.Object(flow).Get(setting)?1:0),1));main.Send(p,flow,"ApplyPhysicalOptions");
        }
    }
}
