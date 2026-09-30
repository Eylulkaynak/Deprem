using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateFlashlightAssembly()
        {
            const string workspace="flashlight";
            InitialPhysical("FCoverOpen"); InitialPhysical("FSlotAOwner"); InitialPhysical("FSlotBOwner");
            InitialPhysical("FSlotAFacing"); InitialPhysical("FSlotBFacing"); InitialPhysical("FSwitch");
            var center=anchors["Anchor_FlashlightWork"].position+Vector3.up*.04f;
            physicalFlashlight.transform.rotation=Quaternion.Euler(90,180,0);
            foreach(var renderer in physicalFlashlight.GetComponentsInChildren<Renderer>()) renderer.enabled=false;
            var mechanism=Group("Fenerin pil yuvası ve gerçek anahtarı",physicalFlashlight.transform);
            mechanism.transform.SetPositionAndRotation(center,Quaternion.identity); mechanism.transform.localScale=Vector3.one;
            Shape("Gövde tabanı",PrimitiveType.Cube,mechanism.transform,new Vector3(0,0,0),new Vector3(.14f,.023f,.28f),mats["YY_tealDark"]);
            foreach(float x in new[]{-.065f,.065f})Shape("Gövde kenarı",PrimitiveType.Cube,mechanism.transform,new Vector3(x,.025f,0),new Vector3(.015f,.05f,.28f),mats["YY_teal"]);
            Shape("Lamba başlığı",PrimitiveType.Cylinder,mechanism.transform,new Vector3(0,.025f,.178f),new Vector3(.15f,.055f,.15f),mats["YY_mustard"]).transform.localRotation=Quaternion.Euler(90,0,0);
            Shape("Mercek",PrimitiveType.Cylinder,mechanism.transform,new Vector3(0,.025f,.235f),new Vector3(.128f,.006f,.128f),mats["YY_white"]).transform.localRotation=Quaternion.Euler(90,0,0);
            var a=center+new Vector3(-.031f,.035f,-.028f); var b=center+new Vector3(.031f,.035f,-.028f);
            foreach(var slot in new[]{a,b})
            {
                Shape("Pil yatağı",PrimitiveType.Cube,mechanism.transform,mechanism.transform.InverseTransformPoint(slot)-Vector3.up*.017f,new Vector3(.036f,.008f,.126f),mats["YY_black"]);
                for(int end=-1;end<=1;end+=2)Shape("Metal temas",PrimitiveType.Cube,mechanism.transform,mechanism.transform.InverseTransformPoint(slot)+new Vector3(0,0,end*.063f),new Vector3(.027f,.014f,.006f),mats["YY_metal"]);
            }
            var positiveA=WorldText("+",mechanism.transform,new Vector3(-.046f,.055f,.053f),.075f,Hex("#FFF5DA")); positiveA.transform.rotation=Quaternion.Euler(90,0,0);
            var positiveB=WorldText("+",mechanism.transform,new Vector3(.046f,.055f,-.109f),.075f,Hex("#FFF5DA")); positiveB.transform.rotation=Quaternion.Euler(90,0,0);
            var closed=center+new Vector3(0,.068f,-.018f); var coverRest=center+new Vector3(.21f,.014f,-.04f);
            var cover=Shape("Kaydırılabilir pil kapağı",PrimitiveType.Cube,world.transform,closed,new Vector3(.133f,.018f,.25f),mats["YY_teal"]);
            cover.transform.SetParent(physicalFlashlight.transform,true);
            var capCollider=cover.AddComponent<BoxCollider>(); capCollider.size=Vector3.one;
            var capGraph=new YanYanaGraphAuthor(cover,"Pil kapağını aç veya yerine kaydır");capGraph.Initial("Dragging",false);
            var capDown=capGraph.Add(new OnPointerDown());capGraph.Bind(capDown.target,cover);var allow=capGraph.Branch(capDown.trigger,AtWork(capGraph,workspace));capGraph.SetVar(allow.ifTrue,"Dragging",true);
            var capDrag=capGraph.Add(new OnDrag());capGraph.Bind(capDrag.target,cover);var dragAllowed=capGraph.Branch(capDrag.trigger,And(capGraph,AtWork(capGraph,workspace),capGraph.Var("Dragging")));
            var projected=PointerOnPlane(capGraph,dragAllowed.ifTrue,capDrag.data,closed.y);
            capGraph.Set(projected.path,typeof(Transform),"position",cover.transform,projected.point);
            var capEnd=capGraph.Add(new OnEndDrag());capGraph.Bind(capEnd.target,cover);var endAllowed=capGraph.Branch(capEnd.trigger,AtWork(capGraph,workspace));
            var p=capGraph.SetVar(endAllowed.ifTrue,"Dragging",false);
            var dist=capGraph.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},capGraph.Get(typeof(Transform),"position",cover.transform),closed).result;
            var atBody=capGraph.Branch(p,capGraph.Binary<Less>(dist,.13f));
            p=capGraph.Set(atBody.ifTrue,typeof(Transform),"position",cover.transform,closed);p=capGraph.SetVar(p,"FCoverOpen",0,flow);p=capGraph.Send(p,flow,"RefreshFlashlight");capGraph.Send(p,flow,"CommitCheckpoint");
            p=capGraph.Set(atBody.ifFalse,typeof(Transform),"position",cover.transform,coverRest);p=capGraph.SetVar(p,"FCoverOpen",1,flow);p=capGraph.Send(p,flow,"RefreshFlashlight");capGraph.Send(p,flow,"CommitCheckpoint");
            var capRestore=capGraph.Event("Restore");var opened=capGraph.Branch(capGraph.SetVar(capRestore.trigger,"Dragging",false),Is(capGraph,capGraph.Var("FCoverOpen",flow),1));
            capGraph.Set(opened.ifTrue,typeof(Transform),"position",cover.transform,coverRest);capGraph.Set(opened.ifFalse,typeof(Transform),"position",cover.transform,closed);capGraph.Dirty();
            CreateAssemblyBattery(1,center+new Vector3(-.17f,.028f,-.08f),a,b,workspace);
            CreateAssemblyBattery(2,center+new Vector3(-.17f,.028f,.065f),a,b,workspace);
            var switchObject=Shape("Gerçek açma anahtarı",PrimitiveType.Cube,world.transform,center+new Vector3(.096f,.026f,.098f),new Vector3(.040f,.023f,.07f),mats["YY_coral"]);
            var switchHit=switchObject.AddComponent<BoxCollider>();switchHit.size=new Vector3(2.8f,2.0f,1.6f);
            var beamObject=Group("Fener ışığı",world.transform,center+new Vector3(0,.025f,.246f));var beam=beamObject.AddComponent<Light>();beam.type=LightType.Spot;beam.range=3.5f;beam.spotAngle=36;beam.intensity=7;beam.color=Hex("#FFE4A8");beamObject.transform.rotation=Quaternion.Euler(5,0,0);beam.enabled=false;
            var testCard=Shape("Işığı görme kartı",PrimitiveType.Cube,world.transform,center+new Vector3(0,.012f,.43f),new Vector3(.30f,.006f,.15f),mats["Paper"]);
            var glow=Shape("Merceğin yanan yüzü",PrimitiveType.Cylinder,mechanism.transform,new Vector3(0,.025f,.243f),new Vector3(.12f,.004f,.12f),mats["Glow"]);glow.transform.localRotation=Quaternion.Euler(90,0,0);glow.SetActive(false);
            var switchGraph=new YanYanaGraphAuthor(switchObject,"Elektrik devresinin anahtarı");var click=switchGraph.Add(new OnPointerClick());switchGraph.Bind(click.target,switchObject);var allowed=switchGraph.Branch(click.trigger,AtWork(switchGraph,workspace));
            var toggle=switchGraph.Branch(allowed.ifTrue,Is(switchGraph,switchGraph.Var("FSwitch",flow),0));
            p=switchGraph.SetVar(toggle.ifTrue,"FSwitch",1,flow);p=switchGraph.Send(p,flow,"RefreshFlashlight");switchGraph.Send(p,flow,"CommitCheckpoint");
            p=switchGraph.SetVar(toggle.ifFalse,"FSwitch",0,flow);p=switchGraph.Send(p,flow,"RefreshFlashlight");switchGraph.Send(p,flow,"CommitCheckpoint");switchGraph.Dirty();
            var refresh=main.Event("RefreshFlashlight");
            var power=And(main,And(main,Is(main,main.Var("FCoverOpen"),0),Is(main,main.Var("FSwitch"),1)),And(main,And(main,main.Binary<Greater>(main.Var("FSlotAOwner"),0),main.Binary<Greater>(main.Var("FSlotBOwner"),0)),And(main,Is(main,main.Var("FSlotAFacing"),0),Is(main,main.Var("FSlotBFacing"),1))));
            p=main.Set(refresh.trigger,typeof(Behaviour),"enabled",beam,power);p=main.Active(p,glow,power);
            var works=main.Branch(p,power);p=main.SetVar(works.ifTrue,"FlashlightReady",1);var testedHere=main.Branch(p,AtWork(main,workspace));Text(main,testedHere.ifTrue,lineText,"Efe: Işık yandı! Pillerin yönü önemliymiş.");
            var circuit=And(main,Is(main,main.Var("FCoverOpen"),0),And(main,And(main,main.Binary<Greater>(main.Var("FSlotAOwner"),0),main.Binary<Greater>(main.Var("FSlotBOwner"),0)),And(main,Is(main,main.Var("FSlotAFacing"),0),Is(main,main.Var("FSlotBFacing"),1))));
            var invalid=main.Branch(works.ifFalse,Is(main,circuit,false));main.SetVar(invalid.ifTrue,"FlashlightReady",0);
            var failed=main.Branch(invalid.ifFalse,Is(main,main.Var("FSwitch"),1));Text(main,failed.ifTrue,lineText,"Efe: Işık yanmadı. Pil yönlerine bakalım.");
            var restore=main.Event("RestorePhysicalObjects");p=main.Send(restore.trigger,cover,"Restore");main.Send(p,flow,"RefreshFlashlight");
            WorkCamera(workspace,center+new Vector3(0,0,.03f),.46f);
            ApproachWorkspace(physicalFlashlight,workspace,anchors["Approach_Flashlight"].position,"Feneri çalışır hâle getir","Efe: Kapağı aç. Pillerin yönünü incele.");
        }

        static void CreateAssemblyBattery(int id,Vector3 tray,Vector3 slotA,Vector3 slotB,string workspace)
        {
            string face="FBattery"+id+"Facing";InitialPhysical(face,id==1?0:1);
            var battery=Group("AA pil "+id,world.transform,tray);
            Shape("Pil gövdesi",PrimitiveType.Cylinder,battery.transform,Vector3.zero,new Vector3(.028f,.052f,.028f),mats["YY_mustard"]).transform.localRotation=Quaternion.Euler(90,0,0);
            Shape("Artı uç",PrimitiveType.Cylinder,battery.transform,new Vector3(0,0,.055f),new Vector3(.019f,.007f,.019f),mats["YY_metal"]).transform.localRotation=Quaternion.Euler(90,0,0);
            var glyph=WorldText("+",battery.transform,new Vector3(0,.019f,.019f),.064f,Hex("#263B50"));glyph.transform.rotation=Quaternion.Euler(90,0,0);
            var collider=battery.AddComponent<BoxCollider>();collider.size=new Vector3(.075f,.055f,.13f);
            var g=new YanYanaGraphAuthor(battery,"Pilin yeri, kutupları ve metal teması");g.Initial("Dragging",false);
            var down=g.Add(new OnPointerDown());g.Bind(down.target,battery);var canPick=g.Branch(down.trigger,And(g,AtWork(g,workspace),Is(g,g.Var("FCoverOpen",flow),1)));
            var p=g.SetVar(canPick.ifTrue,"SelectedItem",battery,flow);p=g.Active(p,rotateControl,true);g.SetVar(p,"Dragging",true);
            var clear=g.Event("ClearSlots");var emptyA=g.Branch(clear.trigger,Is(g,g.Var("FSlotAOwner",flow),id));g.SetVar(emptyA.ifTrue,"FSlotAOwner",0,flow);
            var clearB=g.Event("ClearSlots");var emptyB=g.Branch(clearB.trigger,Is(g,g.Var("FSlotBOwner",flow),id));g.SetVar(emptyB.ifTrue,"FSlotBOwner",0,flow);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,battery);var moving=g.Branch(drag.trigger,And(g,AtWork(g,workspace),g.Var("Dragging")));
            var projected=PointerOnPlane(g,moving.ifTrue,drag.data,slotA.y);g.Set(projected.path,typeof(Transform),"position",battery.transform,projected.point);
            var end=g.Add(new OnEndDrag());g.Bind(end.target,battery);var ending=g.Branch(end.trigger,And(g,AtWork(g,workspace),g.Var("Dragging")));p=g.SetVar(ending.ifTrue,"Dragging",false);p=g.Send(p,battery,"ClearSlots");
            var distanceA=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",battery.transform),slotA).result;
            var distanceB=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",battery.transform),slotB).result;
            var a=g.Branch(p,And(g,g.Binary<Less>(distanceA,.06f),Is(g,g.Var("FSlotAOwner",flow),0)));
            p=g.Set(a.ifTrue,typeof(Transform),"position",battery.transform,slotA);p=g.SetVar(p,"FSlotAOwner",id,flow);p=g.SetVar(p,"FSlotAFacing",g.Var(face,flow),flow);p=g.Send(p,flow,"RefreshFlashlight");g.Send(p,flow,"CommitCheckpoint");
            var b=g.Branch(a.ifFalse,And(g,g.Binary<Less>(distanceB,.06f),Is(g,g.Var("FSlotBOwner",flow),0)));
            p=g.Set(b.ifTrue,typeof(Transform),"position",battery.transform,slotB);p=g.SetVar(p,"FSlotBOwner",id,flow);p=g.SetVar(p,"FSlotBFacing",g.Var(face,flow),flow);p=g.Send(p,flow,"RefreshFlashlight");g.Send(p,flow,"CommitCheckpoint");
            p=g.Set(b.ifFalse,typeof(Transform),"position",battery.transform,tray);p=Text(g,p,lineText,"Efe: Pil, boş yatağın içine oturmalı.");p=g.Send(p,flow,"RefreshFlashlight");g.Send(p,flow,"CommitCheckpoint");
            var rotate=g.Event("Rotate");var rotateAllowed=g.Branch(rotate.trigger,And(g,AtWork(g,workspace),Is(g,g.Var("FCoverOpen",flow),1)));
            var toggle=g.Branch(rotateAllowed.ifTrue,Is(g,g.Var(face,flow),0));p=g.SetVar(toggle.ifTrue,face,1,flow);p=g.Set(p,typeof(Transform),"eulerAngles",battery.transform,new Vector3(0,180,0));g.Send(p,battery,"SyncFacing");
            p=g.SetVar(toggle.ifFalse,face,0,flow);p=g.Set(p,typeof(Transform),"eulerAngles",battery.transform,Vector3.zero);g.Send(p,battery,"SyncFacing");
            var syncA=g.Event("SyncFacing");var inA=g.Branch(syncA.trigger,Is(g,g.Var("FSlotAOwner",flow),id));p=g.SetVar(inA.ifTrue,"FSlotAFacing",g.Var(face,flow),flow);g.Send(p,flow,"RefreshFlashlight");
            var syncB=g.Event("SyncFacing");var inB=g.Branch(syncB.trigger,Is(g,g.Var("FSlotBOwner",flow),id));p=g.SetVar(inB.ifTrue,"FSlotBFacing",g.Var(face,flow),flow);g.Send(p,flow,"RefreshFlashlight");
            var restore=g.Event("Restore");p=g.SetVar(restore.trigger,"Dragging",false);var facing=g.Branch(p,Is(g,g.Var(face,flow),1));g.Set(facing.ifTrue,typeof(Transform),"eulerAngles",battery.transform,new Vector3(0,180,0));g.Set(facing.ifFalse,typeof(Transform),"eulerAngles",battery.transform,Vector3.zero);
            var place=g.Event("Restore");var placedA=g.Branch(place.trigger,Is(g,g.Var("FSlotAOwner",flow),id));g.Set(placedA.ifTrue,typeof(Transform),"position",battery.transform,slotA);var placedB=g.Branch(placedA.ifFalse,Is(g,g.Var("FSlotBOwner",flow),id));g.Set(placedB.ifTrue,typeof(Transform),"position",battery.transform,slotB);g.Set(placedB.ifFalse,typeof(Transform),"position",battery.transform,tray);
            var global=main.Event("RestorePhysicalObjects");main.Send(global.trigger,battery,"Restore");g.Dirty();
        }
    }
}
