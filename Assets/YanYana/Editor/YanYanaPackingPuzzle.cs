using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        const int BagColumns=5, BagRows=6;
        const float BagCell=.095f;
        static Vector3 bagGridOrigin;
        static readonly Dictionary<string,GameObject> packedViews=new Dictionary<string,GameObject>();
        static readonly Dictionary<string,Vector3> packingTrays=new Dictionary<string,Vector3>();

        static void CreatePackingPuzzle()
        {
            packedViews.Clear();packingTrays.Clear();
            for(int cell=0;cell<BagColumns*BagRows;cell++)InitialPhysical("BagCell"+cell);
            InitialPhysical("BagClosed");
            var center=anchors["Anchor_BagWork"].position;
            bagGridOrigin=center+new Vector3(-BagColumns*BagCell*.5f,.055f,-.13f);
            foreach(var renderer in physicalBag.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            var interior=Group("Çantanın gerçek yerleşim alanı",world.transform);
            Model("PackingBagShell",interior.transform,bagGridOrigin+new Vector3(BagColumns*BagCell*.5f,-.01f,BagRows*BagCell*.5f),1,0);
            Shape("Çanta içi",PrimitiveType.Cube,interior.transform,bagGridOrigin+new Vector3(BagColumns*BagCell*.5f,-.022f,BagRows*BagCell*.5f),new Vector3(.51f,.04f,.61f),mats["YY_tealDark"]);
            for(int y=0;y<BagRows;y++)for(int x=0;x<BagColumns;x++)
            {
                Shape("Kumaş bölme "+x+" "+y,PrimitiveType.Cube,interior.transform,bagGridOrigin+new Vector3((x+.5f)*BagCell,-.001f,(y+.5f)*BagCell),new Vector3(BagCell-.003f,.009f,BagCell-.003f),mats["YY_tealDark"]);
            }
            var lid=Model("BackpackClosed",world.transform,center+new Vector3(0,.022f,.12f),1.3f,180);lid.name="Çantanın kapanan dış yüzü";lid.transform.rotation=Quaternion.Euler(90,180,0);lid.SetActive(false);
            var trayRoot=Group("Bulduğun eşyalar için düzenleme bezi",world.transform,center+new Vector3(0,.025f,-.68f));
            Shape("Düzenleme bezi",PrimitiveType.Cube,trayRoot.transform,Vector3.zero,new Vector3(.86f,.016f,.83f),mats["YY_cream"]);
            var entries=new[]{("Water",1,3), ("Radio",3,2), ("Flashlight",1,2), ("FirstAid",2,2), ("Blanket",2,2), ("Food",2,1), ("FamilyCard",1,1), ("Whistle",1,1), ("ComfortFox",1,2)};
            for(int i=0;i<entries.Length;i++)
            {
                var entry=entries[i];
                InitialPhysical("Found."+entry.Item1); InitialPhysical("Pack."+entry.Item1+".X",-1);InitialPhysical("Pack."+entry.Item1+".Y",-1);InitialPhysical("Pack."+entry.Item1+".Rot");
                Vector3 tray=center+new Vector3((i%3-1)*.275f,.06f,-.43f-(i/3)*.245f);
                CreatePackingItem(entry.Item1,i+1,entry.Item2,entry.Item3,tray);
                if(physicalItems.TryGetValue(entry.Item1,out var roomItem))CreateCollectible(roomItem,entry.Item1);
            }
            var strap=Shape("Çantayı kapatma tokası",PrimitiveType.Cube,world.transform,center+new Vector3(.32f,.075f,.05f),new Vector3(.07f,.028f,.10f),mats["YY_coral"]);
            var hit=strap.AddComponent<BoxCollider>();hit.size=new Vector3(1.7f,2,1.6f);
            var g=new YanYanaGraphAuthor(strap,"Yerleştirdiğin içerikle çantayı kapat veya aç");var click=g.Add(new OnPointerClick());g.Bind(click.target,strap);var can=g.Branch(click.trigger,AtWork(g,"bag"));
            var closed=g.Branch(can.ifTrue,Is(g,g.Var("BagClosed",flow),0));g.Send(closed.ifTrue,flow,"OpenBagFit");
            var p=g.SetVar(closed.ifFalse,"BagClosed",0,flow);foreach(string key in new[]{"BagReady","BagZipStep","BagStrapLeft","BagStrapRight","BagCarryActive","BagCarryStep","BagCarried"})p=g.SetVar(p,key,0,flow);p=g.Send(p,flow,"RestorePacking");g.Send(p,flow,"CommitCheckpoint");g.Dirty();
            var restore=main.Event("RestorePacking");var showBag=Is(main,Is(main,main.Var("Workspace"),"bagfit"),false);p=main.Active(restore.trigger,lid,And(main,showBag,Is(main,main.Var("BagClosed"),1)));p=main.Active(p,interior,And(main,showBag,Is(main,main.Var("BagClosed"),0)));p=main.Active(p,trayRoot,showBag);main.Active(p,strap,showBag);
            var global=main.Event("RestorePhysicalObjects");main.Send(global.trigger,flow,"RestorePacking");
            var open=main.Event("OpenWork",arguments:3);var entering=main.Branch(open.trigger,Is(main,open.argumentPorts[0],"bag"));main.Send(entering.ifTrue,flow,"RestorePacking");
            WorkCamera("bag",center+new Vector3(0,.05f,-.55f),1.48f);
            ApproachWorkspace(physicalBag,"bag",anchors["Approach_Bag"].position,"Eşyalarını çantaya sığdır","Efe: Çevirerek deneyelim. Üst üste gelmesinler.");
            // Leaving a device bench transfers that inspected device to the packing cloth.
            var back=main.Event("LeaveWorkspace",arguments:1);var fromFlashlight=main.Branch(back.trigger,Is(main,back.argumentPorts[0],"flashlight"));
            p=main.SetVar(fromFlashlight.ifTrue,"Found.Flashlight",1);main.Send(p,flow,"RestorePacking");
            var back2=main.Event("LeaveWorkspace",arguments:1);var fromRadio=main.Branch(back2.trigger,Is(main,back2.argumentPorts[0],"radio"));
            p=main.SetVar(fromRadio.ifTrue,"Found.Radio",1);main.Send(p,flow,"RestorePacking");
        }

        static void CreateCollectible(GameObject item,string key)
        {
            var renderers=item.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            var hit=item.AddComponent<BoxCollider>();hit.center=item.transform.InverseTransformPoint(b.center);hit.size=new Vector3(Mathf.Max(.32f,b.size.x),Mathf.Max(.30f,b.size.y),Mathf.Max(.32f,b.size.z));
            Vector3 near=key=="Food"||key=="Water"?anchors["Approach_Radio"].position:key=="FirstAid"?anchors["Approach_Shelf"].position:key=="Blanket"?new Vector3(1.0f,0,-.1f):key=="FamilyCard"||key=="Whistle"?anchors["Approach_Bag"].position:anchors["Anchor_CoverAda"].position+Vector3.back*.9f;
            Group("Approach_Collect_"+key,interactions.transform,near);
            var g=new YanYanaGraphAuthor(item,"Eşyayı odada bul ve yanına giderek al");g.Initial("Approaching",false);
            var click=g.Add(new OnPointerClick());g.Bind(click.target,item);var allowed=g.Branch(click.trigger,CanExplore(g));
            var p=g.SetVar(allowed.ifTrue,"ApproachTarget",item,flow);p=g.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",movers["Ada"],new[]{typeof(Vector3)},near);g.SetVar(p,"Approaching",true);
            var update=g.Add(new Unity.VisualScripting.Update());var pending=g.Branch(update.trigger,And(g,And(g,Is(g,g.Var("ApproachTarget",flow),item),Is(g,g.Var("Phase",flow),0)),And(g,g.Var("Approaching"),CanExplore(g))));
            var dist=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Ada"].transform),near).result;
            var arrived=g.Branch(pending.ifTrue,g.Binary<Less>(dist,.4f));p=g.SetVar(arrived.ifTrue,"Approaching",false);
            if(key=="Water"||key=="Food")g.Send(p,flow,"OpenSupply",key);
            else {p=g.SetVar(p,"Found."+key,1,flow);p=g.Send(p,flow,"RestorePacking");p=g.Send(p,flow,"CommitCheckpoint");g.Active(p,item,false);}
            g.Dirty();
            var restore=main.Event("RestorePhysicalObjects");main.Active(restore.trigger,item,Is(main,main.Var("Found."+key),0));
        }

        static Vector3 PackedPosition(int x,int y,int w,int h)=>bagGridOrigin+new Vector3((x+w*.5f)*BagCell,.028f,(y+h*.5f)*BagCell);
        static ControlOutput ClearCells(YanYanaGraphAuthor g,ControlOutput before,int id) => g.Send(before,g.Owner,"ClearOwnCells");

        static ControlOutput AuthorCellClear(YanYanaGraphAuthor g,ControlOutput before,int id)
        {
            var p=before;
            for(int cell=0;cell<BagColumns*BagRows;cell++)
            {
                var own=g.Branch(p,Is(g,g.Var("BagCell"+cell,flow),id));var cleared=g.SetVar(own.ifTrue,"BagCell"+cell,0,flow);
                var join=g.Add(new Unity.VisualScripting.Sequence{outputCount=1});g.Link(cleared,join.enter);g.Link(own.ifFalse,join.enter);p=join.multiOutputs[0];
            }
            return p;
        }

        static void CreatePackingItem(string name,int id,int width,int depth,Vector3 tray)
        {
            var item=Group("Yerleşim · "+name,world.transform,tray);packedViews[name]=item;packingTrays[name]=tray;
            var model=Model(name,item.transform,Vector3.zero,1,180);model.transform.localRotation=Quaternion.Euler(90,180,0);
            var renderers=model.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            // Uniform scale preserves each object's proportions; only compact real-world sizes are used.
            float fit=Mathf.Min((width*BagCell-.012f)/b.size.x,(depth*BagCell-.012f)/b.size.z,.12f/b.size.y);model.transform.localScale*=fit;
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);model.transform.position+=item.transform.position-b.center;
            var footprint=Shape("Yumuşak eşya altlığı",PrimitiveType.Cube,item.transform,new Vector3(0,-.02f,0),new Vector3(width*BagCell-.006f,.008f,depth*BagCell-.006f),mats["YY_sand"]);
            var hit=item.AddComponent<BoxCollider>();hit.size=new Vector3(Mathf.Max(.15f,width*BagCell),.15f,Mathf.Max(.15f,depth*BagCell));
            string px="Pack."+name+".X",py="Pack."+name+".Y",rot="Pack."+name+".Rot";
            var g=new YanYanaGraphAuthor(item,"Çantada alan, yön ve çakışma · "+name);g.Initial("Dragging",false);g.Initial("DraftRot",0);g.Initial("CandidateX",0);g.Initial("CandidateY",0);
            var clearOwn=g.Event("ClearOwnCells");AuthorCellClear(g,clearOwn.trigger,id);
            var down=g.Add(new OnPointerDown());g.Bind(down.target,item);var allowed=g.Branch(down.trigger,And(g,AtWork(g,"bag"),Is(g,g.Var("BagClosed",flow),0)));
            var p=g.SetVar(allowed.ifTrue,"SelectedItem",item,flow);p=g.Active(p,rotateControl,true);g.SetVar(p,"Dragging",true);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,item);var moving=g.Branch(drag.trigger,And(g,AtWork(g,"bag"),g.Var("Dragging")));
            var projected=PointerOnPlane(g,moving.ifTrue,drag.data,bagGridOrigin.y+.028f);g.Set(projected.path,typeof(Transform),"position",item.transform,projected.point);
            var end=g.Add(new OnEndDrag());g.Bind(end.target,item);var ending=g.Branch(end.trigger,And(g,AtWork(g,"bag"),g.Var("Dragging")));p=g.SetVar(ending.ifTrue,"Dragging",false);g.Send(p,item,"Place");
            var rotate=g.Event("Rotate");var rotateAllowed=g.Branch(rotate.trigger,And(g,AtWork(g,"bag"),Is(g,g.Var("BagClosed",flow),0)));var turn=g.Branch(rotateAllowed.ifTrue,Is(g,g.Var("DraftRot"),0));
            p=g.SetVar(turn.ifTrue,"DraftRot",1);p=g.Set(p,typeof(Transform),"eulerAngles",item.transform,new Vector3(0,90,0));g.Send(p,item,"CheckTurn");
            p=g.SetVar(turn.ifFalse,"DraftRot",0);p=g.Set(p,typeof(Transform),"eulerAngles",item.transform,Vector3.zero);g.Send(p,item,"CheckTurn");
            var turnCheck=g.Event("CheckTurn");var packed=g.Branch(turnCheck.trigger,g.Binary<GreaterOrEqual>(g.Var(px,flow),0));g.Send(packed.ifTrue,item,"Place");
            p=g.SetVar(packed.ifFalse,rot,g.Var("DraftRot"),flow);g.Send(p,flow,"CommitCheckpoint");
            var place=g.Event("Place");var orientation=g.Branch(place.trigger,Is(g,g.Var("DraftRot"),0));
            BuildPlacement(g,orientation.ifTrue,item,id,name,width,depth,0,px,py,rot,tray);
            BuildPlacement(g,orientation.ifFalse,item,id,name,depth,width,1,px,py,rot,tray);
            var restore=g.Event("Restore");p=g.SetVar(restore.trigger,"Dragging",false);p=g.SetVar(p,"DraftRot",g.Var(rot,flow));var orientationRestore=g.Branch(p,Is(g,g.Var(rot,flow),0));
            p=g.Set(orientationRestore.ifTrue,typeof(Transform),"eulerAngles",item.transform,Vector3.zero);RestorePackedPosition(g,p,item,px,py,width,depth,tray);
            p=g.Set(orientationRestore.ifFalse,typeof(Transform),"eulerAngles",item.transform,new Vector3(0,90,0));RestorePackedPosition(g,p,item,px,py,depth,width,tray);
            var all=main.Event("RestorePacking");p=main.Active(all.trigger,item,And(main,Is(main,Is(main,main.Var("Workspace"),"bagfit"),false),And(main,Is(main,main.Var("Found."+name),1),Or(main,Is(main,main.Var("BagClosed"),0),main.Binary<Less>(main.Var(px),0)))));main.Send(p,item,"Restore");
            item.SetActive(false);g.Dirty();
        }

        static void RestorePackedPosition(YanYanaGraphAuthor g,ControlOutput path,GameObject item,string px,string py,int width,int height,Vector3 tray)
        {
            var packed=g.Branch(path,g.Binary<GreaterOrEqual>(g.Var(px,flow),0));
            var position=Add(g,bagGridOrigin,V3(g,g.Binary<ScalarMultiply>(Sum(g,g.Var(px,flow),width*.5f),BagCell),.028f,g.Binary<ScalarMultiply>(Sum(g,g.Var(py,flow),height*.5f),BagCell)));
            g.Set(packed.ifTrue,typeof(Transform),"position",item.transform,position);g.Set(packed.ifFalse,typeof(Transform),"position",item.transform,tray);
        }

        static void BuildPlacement(YanYanaGraphAuthor g,ControlOutput before,GameObject item,int id,string name,int width,int height,int rotation,string px,string py,string rot,Vector3 tray)
        {
            var position=g.Get(typeof(Transform),"position",item.transform);
            var cx=g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Binary<ScalarSubtract>(g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"x",position),bagGridOrigin.x),BagCell),width*.5f)).result;
            var cy=g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Binary<ScalarSubtract>(g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"z",position),bagGridOrigin.z),BagCell),height*.5f)).result;
            var p=g.SetVar(before,"CandidateX",cx);p=g.SetVar(p,"CandidateY",cy);
            var overBag=And(g,And(g,g.Binary<GreaterOrEqual>(cx,0),g.Binary<Less>(cx,BagColumns)),And(g,g.Binary<GreaterOrEqual>(cy,0),g.Binary<Less>(cy,BagRows)));
            var inside=g.Branch(p,overBag);
            // Deliberately placing an item back on the cloth removes it from the bag.
            p=ClearCells(g,inside.ifFalse,id);p=g.SetVar(p,px,-1,flow);p=g.SetVar(p,py,-1,flow);p=g.SetVar(p,rot,rotation,flow);p=g.Set(p,typeof(Transform),"position",item.transform,tray);g.Send(p,flow,"CommitCheckpoint");
            var ids=new List<int>();for(int y=0;y<=BagRows-height;y++)for(int x=0;x<=BagColumns-width;x++)ids.Add(y*BagColumns+x);
            var select=g.Add(new SwitchOnInteger{options=ids});g.Bind(select.selector,g.Call(typeof(Convert),"ToInt32",null,OneFloat,Sum(g,cx,g.Binary<ScalarMultiply>(cy,BagColumns))).result);g.Link(inside.ifTrue,select.enter);g.Send(select.@default,item,"Restore");
            foreach(var branch in select.branches)
            {
                int x=branch.Key%BagColumns,y=branch.Key/BagColumns;object clear=true;
                for(int yy=0;yy<height;yy++)for(int xx=0;xx<width;xx++)
                {
                    var occupant=g.Var("BagCell"+((y+yy)*BagColumns+x+xx),flow);clear=And(g,clear,Or(g,Is(g,occupant,0),Is(g,occupant,id)));
                }
                var fits=g.Branch(branch.Value,clear);p=ClearCells(g,fits.ifTrue,id);
                for(int yy=0;yy<height;yy++)for(int xx=0;xx<width;xx++)p=g.SetVar(p,"BagCell"+((y+yy)*BagColumns+x+xx),id,flow);
                p=g.SetVar(p,px,x,flow);p=g.SetVar(p,py,y,flow);p=g.SetVar(p,rot,rotation,flow);p=g.Set(p,typeof(Transform),"position",item.transform,PackedPosition(x,y,width,height));g.Send(p,flow,"CommitCheckpoint");
                p=Text(g,fits.ifFalse,lineText,"Efe: Burada başka eşya var. Çevirip deneyelim.");g.Send(p,item,"Restore");
            }
        }
    }
}
