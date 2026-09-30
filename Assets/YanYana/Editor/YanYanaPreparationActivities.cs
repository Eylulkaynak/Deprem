using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateRadioTuning()
        {
            var existingParts=new HashSet<Transform>();foreach(Transform part in world.transform)existingParts.Add(part);
            InitialPhysical("RadioDial",20); InitialPhysical("RadioPower");
            var center=anchors["Anchor_RadioWork"].position;
            physicalRadio.transform.rotation=Quaternion.Euler(90,180,0);
            var desk=Group("Radyonun ayar yüzü",world.transform,center+Vector3.up*.06f);
            Shape("Ayar yüzü",PrimitiveType.Cube,desk.transform,Vector3.zero,new Vector3(.38f,.04f,.24f),mats["YY_tealDark"]);
            for(int x=0;x<9;x++)for(int z=0;z<4;z++)Shape("Hoparlör deliği",PrimitiveType.Sphere,desk.transform,new Vector3(-.14f+x*.019f,.023f,-.075f+z*.028f),new Vector3(.009f,.004f,.009f),mats["YY_black"]);
            var dial=Shape("Frekans düğmesi",PrimitiveType.Cylinder,world.transform,center+new Vector3(.094f,.10f,0),new Vector3(.105f,.018f,.105f),mats["YY_cream"]);
            Shape("Düğmenin işareti",PrimitiveType.Cube,dial.transform,new Vector3(0,1.06f,.3f),new Vector3(.095f,.15f,.3f),mats["YY_coral"]);
            var hit=dial.AddComponent<BoxCollider>();hit.size=new Vector3(1.7f,3,1.7f);
            var needle=Shape("Frekans göstergesi",PrimitiveType.Cube,world.transform,center+new Vector3(-.12f,.092f,.093f),new Vector3(.012f,.01f,.042f),mats["YY_coral"]);
            Shape("Frekans çizgisi",PrimitiveType.Cube,world.transform,center+new Vector3(0,.085f,.093f),new Vector3(.30f,.004f,.048f),mats["YY_cream"]);
            for(int i=0;i<11;i++)Shape("Yayın çizgisi",PrimitiveType.Cube,world.transform,center+new Vector3(-.14f+i*.028f,.09f,.102f),new Vector3(.003f,.006f,i%2==0?.029f:.015f),mats["YY_tealDark"]);
            var lamp=Shape("Yayın netleşti",PrimitiveType.Sphere,world.transform,center+new Vector3(.16f,.096f,.09f),new Vector3(.023f,.01f,.023f),mats["Glow"]);lamp.SetActive(false);
            var g=new YanYanaGraphAuthor(dial,"Döndürerek frekansı bul; yayın gerçekten değişir");
            var down=g.Add(new OnPointerDown());g.Bind(down.target,dial);var allowed=g.Branch(down.trigger,AtWork(g,"radio"));g.SetVar(allowed.ifTrue,"RadioPower",1,flow);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,dial);allowed=g.Branch(drag.trigger,AtWork(g,"radio"));
            var projected=PointerOnPlane(g,allowed.ifTrue,drag.data,center.y+.1f);
            var horizontal=g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"x",projected.point),dial.transform.position.x);
            var vertical=g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"z",projected.point),dial.transform.position.z);
            var angle=g.Binary<ScalarMultiply>(g.Call(typeof(Mathf),"Atan2",null,new[]{typeof(float),typeof(float)},horizontal,vertical).result,Mathf.Rad2Deg);
            var degrees=g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},Sum(g,angle,90f),0f,180f).result;
            var p=g.SetVar(projected.path,"RadioDial",g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,degrees).result,flow);g.Send(p,flow,"TuneRadio");
            var end=g.Add(new OnEndDrag());g.Bind(end.target,dial);allowed=g.Branch(end.trigger,AtWork(g,"radio"));g.Send(allowed.ifTrue,flow,"CommitCheckpoint");g.Dirty();
            var tune=main.Event("TuneRadio");
            var good=And(main,main.Binary<GreaterOrEqual>(main.Var("RadioDial"),112),main.Binary<LessOrEqual>(main.Var("RadioDial"),128));
            p=main.Set(tune.trigger,typeof(Transform),"eulerAngles",dial.transform,V3(main,0f,main.Var("RadioDial"),0f));
            p=main.Set(p,typeof(Transform),"position",needle.transform,Add(main,center+new Vector3(-.14f,.092f,.093f),Mul(main,Vector3.right,main.Binary<ScalarMultiply>(main.Var("RadioDial"),.28f/180f))));
            p=main.Active(p,lamp,good);var clear=main.Branch(p,good);
            p=main.SetVar(clear.ifTrue,"RadioReady",1);var hear=main.Branch(p,AtWork(main,"radio"));Text(main,hear.ifTrue,lineText,"Radyo: Resmî duyuruları dinleyin. Toplanma alanına güvenle ilerleyin.");
            p=main.SetVar(clear.ifFalse,"RadioReady",0);var noise=main.Branch(p,AtWork(main,"radio"));Text(main,noise.ifTrue,lineText,"Efe: Cızırtı var. Düğmeyi yavaşça döndür.");
            var restore=main.Event("RestorePhysicalObjects");main.Send(restore.trigger,flow,"TuneRadio");
            WorkCamera("radio",center+new Vector3(0,.08f,0),.45f);
            ApproachWorkspace(physicalRadio,"radio",anchors["Approach_Radio"].position,"Resmî yayını bul","Efe: Düğmeyi döndür. Göstergeyi ve yayını izle.");
            var mechanism=Group("Radyonun bütün çalışma parçaları",world.transform);physicalItems["RadioMechanism"]=mechanism;
            var newParts=new List<Transform>();foreach(Transform part in world.transform)if(part!=mechanism.transform&&!existingParts.Contains(part))newParts.Add(part);
            foreach(var part in newParts)part.SetParent(mechanism.transform,true);physicalRadio.transform.SetParent(mechanism.transform,true);
        }

        static void CreateFamilyMapPuzzle()
        {
            InitialPhysical("MapCell");
            var origin=anchors["Anchor_FamilyMap"].position+new Vector3(-.78f,-.30f,-.40f);
            const float size=.22f;
            var board=Group("Ailecek denenen resimli mahalle planı",world.transform,origin);
            Shape("Harita tahtası",PrimitiveType.Cube,board.transform,new Vector3(.22f,-.035f,.33f),new Vector3(.76f,.065f,.98f),mats["YY_wood"]);
            var blocked=new HashSet<int>{1,4,8};
            for(int i=0;i<12;i++)
            {
                var at=new Vector3(i%3*size,0,i/3*size);
                Shape("Plan karesi "+i,PrimitiveType.Cube,board.transform,at,new Vector3(.208f,.025f,.208f),mats[blocked.Contains(i)?"YY_coral":"YY_cream"]);
                if(blocked.Contains(i))
                {
                    Shape("Yüksek bina",PrimitiveType.Cube,board.transform,at+new Vector3(0,.075f,0),new Vector3(.12f,.13f,.13f),mats["YY_terracotta"]);
                    Shape("Çatı",PrimitiveType.Cube,board.transform,at+new Vector3(0,.15f,0),new Vector3(.15f,.028f,.16f),mats["YY_coral"]);
                }
                if(i==0) { var label=WorldText("EV",board.transform,at+new Vector3(0,.025f,0),.05f,Ink);label.transform.rotation=Quaternion.Euler(90,0,0); }
                if(i==11)
                {
                    Shape("Buluşma ağacı",PrimitiveType.Cylinder,board.transform,at+new Vector3(0,.075f,0),new Vector3(.025f,.07f,.025f),mats["YY_wood"]);
                    Shape("Ağacın tacı",PrimitiveType.Sphere,board.transform,at+new Vector3(0,.16f,0),new Vector3(.17f,.17f,.17f),mats["YY_teal"]);
                }
            }
            var token=Shape("Haritadaki aile taşı",PrimitiveType.Sphere,world.transform,origin+Vector3.up*.065f,new Vector3(.095f,.07f,.095f),mats["YY_mustard"]);
            var hit=token.AddComponent<BoxCollider>();hit.size=new Vector3(1.7f,2,1.7f);
            var g=new YanYanaGraphAuthor(token,"Yüksek binalardan uzak bir buluşma yolu çiz");
            var drag=g.Add(new OnDrag());g.Bind(drag.target,token);var allowed=g.Branch(drag.trigger,AtWork(g,"map"));
            var point=PointerOnPlane(g,allowed.ifTrue,drag.data,origin.y+.065f);
            var cx=g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"x",point.point),origin.x),size)).result;
            var cy=g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"z",point.point),origin.z),size)).result;
            var valid=g.Branch(point.path,And(g,And(g,g.Binary<GreaterOrEqual>(cx,0),g.Binary<Less>(cx,3)),And(g,g.Binary<GreaterOrEqual>(cy,0),g.Binary<Less>(cy,4))));
            var select=g.Add(new SwitchOnInteger{options=new List<int>{0,1,2,3,4,5,6,7,8,9,10,11}});g.Link(valid.ifTrue,select.enter);g.Bind(select.selector,g.Call(typeof(Convert),"ToInt32",null,OneFloat,Sum(g,cx,g.Binary<ScalarMultiply>(cy,3f))).result);
            foreach(var branch in select.branches)
            {
                int cell=branch.Key;
                if(blocked.Contains(cell)){Text(g,branch.Value,lineText,"Emre: Binalardan uzak, açık geçişi deneyelim.");continue;}
                object neighbour=false;
                for(int previous=0;previous<12;previous++)if(Mathf.Abs(previous%3-cell%3)+Mathf.Abs(previous/3-cell/3)==1)neighbour=Or(g,neighbour,Is(g,g.Var("MapCell",flow),previous));
                var connected=g.Branch(branch.Value,neighbour);var p=g.SetVar(connected.ifTrue,"MapCell",cell,flow);
                p=g.Set(p,typeof(Transform),"position",token.transform,origin+new Vector3(cell%3*size,.065f,cell/3*size));
                if(cell==11){p=g.SetVar(p,"FamilyPlan",1,flow);p=Text(g,p,lineText,"Efe: Büyük ağacın yanında buluşacağız. Yolu öğrendim!");p=g.Send(p,flow,"CommitCheckpoint");}
            }
            var restore=main.Event("RestorePhysicalObjects");main.Set(restore.trigger,typeof(Transform),"position",token.transform,Add(main,origin+Vector3.up*.065f,V3(main,main.Binary<ScalarMultiply>(main.Binary<ScalarModulo>(main.Var("MapCell"),3f),size),0f,main.Binary<ScalarMultiply>(main.Call(typeof(Mathf),"Floor",null,OneFloat,main.Binary<ScalarDivide>(main.Var("MapCell"),3f)).result,size))));
            WorkCamera("map",origin+new Vector3(.22f,0,.33f),1.0f);
            ApproachWorkspace(board,"map",anchors["Approach_FamilyMap"].position+Vector3.left*.84f,"Ailece buluşma yolunu dene","Derya: Aile taşını açık yoldan büyük ağaca götür.");g.Dirty();
        }

        static void CreatePhysicalHomeSafety()
        {
            foreach(var type in new[]{"shelf","wardrobe"})
            {
                string flag=type=="shelf"?"ShelfSecured":"WardrobeSecured";
                var item=physicalItems[type];
                var strap=Model("SafetyStrap",item.transform,new Vector3(0,1.5f,0),1,0);strap.SetActive(false);
                var g=new YanYanaGraphAuthor(item,"Yetişkine haber ver; sabitleme sırasında keşfe devam et");g.Initial("Working",false);
                var click=g.Add(new OnPointerClick());g.Bind(click.target,item);
                var can=g.Branch(click.trigger,And(g,CanExplore(g),And(g,Is(g,g.Var(flag,flow),0),Is(g,g.Var("Working"),false))));
                var p=g.SetVar(can.ifTrue,"Working",true);p=Text(g,p,lineText,"Ada: Bunu birlikte sabitleyelim mi? Derya: Ben hallederim.");g.Send(p,item,"AdultSecure");
                var action=g.Event("AdultSecure",true);var adult=cast["Derya"];g.Initial("AtFurniture",false);
                var queue=g.Add(new WaitUntilUnit());g.Bind(queue.condition,Or(g,Is(g,g.Var("AdultWorking",flow),false),Is(g,Is(g,g.Var("Phase",flow),0),false)));g.Link(action.trigger,queue.enter);
                var preparation=g.Branch(queue.exit,Is(g,g.Var("Phase",flow),0));g.SetVar(preparation.ifFalse,"Working",false);
                // Multiple wait predicates can become true before either coroutine resumes.
                // Claim the shared adult synchronously after the wait; retry if another job won.
                var claim=g.Branch(preparation.ifTrue,Is(g,g.Var("AdultWorking",flow),false));g.Send(claim.ifFalse,item,"AdultSecure");p=g.SetVar(claim.ifTrue,"AdultWorking",true,flow);
                p=g.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Derya"],OneBool,false);var approach=anchors[type=="shelf"?"Approach_Shelf":"Approach_Wardrobe"].position;p=g.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",movers["Derya"],new[]{typeof(Vector3)},approach);
                var arrived=g.Add(new WaitUntilUnit());g.Bind(arrived.condition,Or(g,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",adult.transform),approach).result,.42f),Is(g,Is(g,g.Var("Phase",flow),0),false)));g.Link(p,arrived.enter);
                var safeToWork=g.Branch(arrived.exit,Is(g,g.Var("Phase",flow),0));p=g.SetVar(safeToWork.ifFalse,"AdultWorking",false,flow);g.SetVar(p,"Working",false);
                p=g.Do(safeToWork.ifTrue,typeof(StoryPlayerMovement),"Stop",movers["Derya"],NoArgs);p=g.Do(p,typeof(Transform),"LookAt",adult.transform,new[]{typeof(Vector3)},item.transform.position);p=g.SetVar(p,"AtFurniture",true);p=g.Wait(p,2.5f);p=g.SetVar(p,"AtFurniture",false);p=g.SetVar(p,"AdultWorking",false,flow);
                var stillPreparing=g.Branch(p,Is(g,g.Var("Phase",flow),0));p=stillPreparing.ifTrue;
                p=g.Active(p,strap,true);p=g.SetVar(p,flag,1,flow);p=g.SetVar(p,"Working",false);p=Text(g,p,lineText,"Derya: Sabitlendi. Sen hafif eşyaları düzenleyebilirsin.");g.Send(p,flow,"CommitCheckpoint");
                var late=g.Add(new Unity.VisualScripting.LateUpdate());var contact=g.Branch(late.trigger,And(g,g.Var("AtFurniture"),Is(g,g.Var("Phase",flow),0)));p=contact.ifTrue;var animator=adult.GetComponentInChildren<Animator>();
                foreach(bool left in new[]{true,false})p=g.Do(p,typeof(Deprem.Minigames.FirefighterExtinguishManager),"SolveArm",null,new[]{typeof(Transform),typeof(Transform),typeof(Transform),typeof(Vector3),typeof(Vector3)},animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm),animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm),animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand),Add(g,g.Get(typeof(Transform),"position",strap.transform),new Vector3(left?-.10f:.10f,0,0)),new Vector3(left?-.5f:.5f,-1,0));
                var restore=main.Event("RestorePhysicalObjects");main.Active(restore.trigger,strap,Is(main,main.Var(flag),1));g.Dirty();
            }
            InitialPhysical("ExitBoxMoved");
            var start=anchors["Anchor_ExitBox"].position;var park=start+new Vector3(.9f,0,0);
            var box=Model("ToyBox",world.transform,start,1,180);box.name="Çıkış önündeki hafif oyuncak kutusu";
            var obstacle=box.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=new Vector3(.55f,.5f,.45f);obstacle.center=Vector3.up*.25f;obstacle.carving=true;
            var collider=box.AddComponent<BoxCollider>();collider.size=obstacle.size;collider.center=obstacle.center;
            var place=Shape("Kutunun güvenli köşesi",PrimitiveType.Cube,world.transform,park+Vector3.up*.012f,new Vector3(.65f,.016f,.55f),mats["YY_cream"]);
            var graph=new YanYanaGraphAuthor(box,"Hafif engeli geçiş yolundan taşı");var clickBox=graph.Add(new OnPointerClick());graph.Bind(clickBox.target,box);var boxApproach=start+Vector3.forward*.65f;var approaching=graph.Branch(clickBox.trigger,CanExplore(graph));graph.Do(approaching.ifTrue,typeof(StoryPlayerMovement),"TrySetDestination",movers["Ada"],new[]{typeof(Vector3)},boxApproach);
            var near=graph.Binary<Less>(graph.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},graph.Get(typeof(Transform),"position",cast["Ada"].transform),boxApproach).result,.70f);
            var dragBox=graph.Add(new OnDrag());graph.Bind(dragBox.target,box);
            var permitted=graph.Branch(dragBox.trigger,And(graph,CanExplore(graph),near));var projected=PointerOnPlane(graph,permitted.ifTrue,dragBox.data,start.y);
            graph.Set(projected.path,typeof(Transform),"position",box.transform,projected.point);
            var drop=graph.Add(new OnEndDrag());graph.Bind(drop.target,box);permitted=graph.Branch(drop.trigger,And(graph,CanExplore(graph),near));
            var distance=graph.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},graph.Get(typeof(Transform),"position",box.transform),park).result;
            var fits=graph.Branch(permitted.ifTrue,graph.Binary<Less>(distance,.50f));var path=graph.Set(fits.ifTrue,typeof(Transform),"position",box.transform,park);path=graph.SetVar(path,"ExitBoxMoved",1,flow);path=graph.SetVar(path,"ExitCleared",1,flow);path=Text(graph,path,lineText,"Efe: Şimdi kapıya kadar yol açık!");graph.Send(path,flow,"CommitCheckpoint");
            graph.Set(fits.ifFalse,typeof(Transform),"position",box.transform,start);
            var reset=main.Event("RestorePhysicalObjects");var moved=main.Branch(reset.trigger,Is(main,main.Var("ExitBoxMoved"),1));main.Set(moved.ifTrue,typeof(Transform),"position",box.transform,park);main.Set(moved.ifFalse,typeof(Transform),"position",box.transform,start);graph.Dirty();
        }
    }
}
