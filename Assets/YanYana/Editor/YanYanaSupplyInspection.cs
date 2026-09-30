using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using TMPro;
using UnityEditor;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static GameObject SupplyModel(string name,Transform parent,Vector3 position,float height)
        {
            // Keep the FBX root's axis/unit conversion inside a separate placement root.
            var root=Group(name,parent,position);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Art/Models/"+name+".fbx");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset,root.transform);var renderers=visual.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(materials[i]&&mats.TryGetValue(materials[i].name.Replace(" (Instance)",""),out var own))materials[i]=own;
                renderer.sharedMaterials=materials;
            }
            visual.transform.localScale*=height/bounds.size.y;bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            visual.transform.position+=root.transform.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            return root;
        }
        // The family compares packages before packing; no food is consumed in this activity.
        static void CreateSupplyInspection()
        {
            var visibility=new YanYanaGraphAuthor(flow,"Ambalaj yakın planında aynı tezgahtaki radyo kadrajdan çıkar");var frame=visibility.Add(new Unity.VisualScripting.LateUpdate());
            visibility.Active(frame.trigger,physicalItems["RadioMechanism"],Is(visibility,Or(visibility,Is(visibility,visibility.Var("Workspace",flow),"supplyWater"),Is(visibility,visibility.Var("Workspace",flow),"supplyFood")),false));visibility.Dirty();
            foreach(string key in new[]{"Water","Food"})
            {
                string workspace="supply"+key;
                InitialPhysical("SupplyViewed"+key);InitialPhysical("SupplyChoice"+key,-1);
                var center=anchors["Anchor_RadioWork"].position+new Vector3(0,.08f,-.12f);
                var root=Group("Ambalaj incelemesi · "+key,interactions.transform,center);root.SetActive(false);
                Shape("Temiz karşılaştırma yüzeyi",PrimitiveType.Cube,root.transform,new Vector3(0,-.025f,0),new Vector3(.91f,.03f,.97f),mats["YY_cream"]);
                var place=center+new Vector3(0,.035f,-.34f);
                Shape("Seçtiğin malzemenin yeri",PrimitiveType.Cylinder,root.transform,root.transform.InverseTransformPoint(place)-Vector3.up*.016f,new Vector3(.31f,.008f,.27f),mats["YY_teal"]);
                var title=WorldText("ÇANTAYA",root.transform,new Vector3(0,.050f,-.37f),.032f,Ink);title.transform.rotation=Quaternion.Euler(90,0,0);
                var today=WorldText("BUGÜN: 09 / 2026",root.transform,new Vector3(0,.040f,.40f),.030f,Ink);today.transform.rotation=Quaternion.Euler(90,0,0);
                for(int choice=0;choice<2;choice++)
                {
                    bool intact=choice==1;var rest=center+new Vector3(choice==0?-.22f:.22f,.035f,.11f);
                    var candidate=Group("İncelenen "+key+" "+choice,root.transform,root.transform.InverseTransformPoint(rest));
                    SupplyModel(intact?key:key+"_Opened",candidate.transform,Vector3.zero,key=="Water"?.34f:.25f);
                    var label=WorldText(intact?"09 / 2028":"09 / 2025",root.transform,new Vector3(choice==0?-.22f:.22f,.045f,-.14f),.030f,Ink);label.transform.rotation=Quaternion.Euler(90,0,0);
                    var hit=candidate.AddComponent<BoxCollider>();hit.center=Vector3.up*.19f;hit.size=new Vector3(.27f,.48f,.27f);
                    var g=new YanYanaGraphAuthor(candidate,"Ambalajın durumuyla malzeme seç; sonuç daha sonra görünür");g.Initial("Held",false);g.Initial("Finger",-999);
                    var down=g.Add(new OnPointerDown());g.Bind(down.target,candidate);var can=g.Branch(down.trigger,And(g,AtWork(g,workspace),Is(g,g.Var("Dragging",flow),false)));
                    var p=g.SetVar(can.ifTrue,"Held",true);p=g.SetVar(p,"Finger",g.Get(typeof(PointerEventData),"pointerId",down.data));p=g.SetVar(p,"Dragging",true,flow);
                    var drag=g.Add(new OnDrag());g.Bind(drag.target,candidate);can=g.Branch(drag.trigger,And(g,AtWork(g,workspace),And(g,g.Var("Held"),Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",drag.data)))));
                    var point=PointerOnPlane(g,can.ifTrue,drag.data,rest.y);g.Set(point.path,typeof(Transform),"position",candidate.transform,point.point);
                    var end=g.Add(new OnEndDrag());g.Bind(end.target,candidate);can=g.Branch(end.trigger,And(g,AtWork(g,workspace),And(g,g.Var("Held"),Is(g,g.Var("Finger"),g.Get(typeof(PointerEventData),"pointerId",end.data)))));
                    p=g.SetVar(can.ifTrue,"Held",false);p=g.SetVar(p,"Dragging",false,flow);
                    var fit=g.Branch(p,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",candidate.transform),place).result,.15f));
                    p=g.SetVar(fit.ifTrue,"Found."+key,1,flow);p=g.SetVar(p,"SupplyChoice"+key,choice,flow);p=g.SetVar(p,key+"Ready",intact?1:0,flow);p=g.SetVar(p,"Workspace","",flow);p=g.Active(p,root,false);p=g.Active(p,physicalItems[key],false);p=g.Send(p,flow,"RestorePacking");p=g.Send(p,flow,"CommitCheckpoint");g.Send(p,flow,"Explore");g.Set(fit.ifFalse,typeof(Transform),"position",candidate.transform,rest);
                    var reset=main.Event("RestorePhysicalObjects");p=main.Set(reset.trigger,typeof(Transform),"position",candidate.transform,rest);p=main.SetVar(p,"Held",false,candidate);g.Dirty();
                }
                var dial=Shape("Ambalajı çevir · "+key,PrimitiveType.Cylinder,root.transform,new Vector3(0,.025f,.03f),new Vector3(.14f,.018f,.14f),mats["YY_coral"]);
                Shape("Döndürme tutacağı",PrimitiveType.Cube,dial.transform,new Vector3(0,.75f,0),new Vector3(.72f,.40f,.20f),mats["YY_cream"]);dial.AddComponent<BoxCollider>();
                var dg=new YanYanaGraphAuthor(dial,"Ambalajların arkasını çevirerek incele");dg.Initial("Angle",0f);var turn=dg.Add(new OnDrag());dg.Bind(turn.target,dial);var allowed=dg.Branch(turn.trigger,AtWork(dg,workspace));
                var delta=dg.Get(typeof(Vector2),"x",dg.Get(typeof(PointerEventData),"delta",turn.data));var q=dg.SetVar(allowed.ifTrue,"Angle",Sum(dg,dg.Var("Angle"),delta));
                q=dg.Set(q,typeof(Transform),"localEulerAngles",dial.transform,V3(dg,0f,dg.Var("Angle"),0f));
                foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.StartsWith("İncelenen "+key+" "))q=dg.Set(q,typeof(Transform),"eulerAngles",t,V3(dg,0f,dg.Var("Angle"),0f));
                q=dg.SetVar(q,"SupplyViewed"+key,1,flow);Text(dg,q,lineText,key=="Water"?"Derya: Kapağı ve tarihi kontrol et. Kapalı şişeyi seçelim.":"Derya: Paketi ve tarihi kontrol et. Yırtık paketi kullanmayalım.");dg.Dirty();
                var cameraView=WorkCamera(workspace,center+new Vector3(0,.10f,0),.88f,false);cameraView.transform.position=center+new Vector3(0,1.9f,-1.4f);cameraView.transform.LookAt(center+Vector3.up*.10f);
                var open=main.Event("OpenSupply",arguments:1);var match=main.Branch(open.trigger,Is(main,open.argumentPorts[0],key));q=main.Send(match.ifTrue,flow,"CapturePhysicalSnapshot",workspace);q=main.Active(q,root,true);main.Send(q,flow,"OpenWork",workspace,key=="Water"?"Su şişelerini incele ve seç":"Yiyecek paketlerini incele ve seç","Derya: Malzemeleri düzenli kontrol edelim. Beraber karşılaştırabiliriz.");
                var leave=main.Event("LeaveWorkspace",arguments:1);match=main.Branch(leave.trigger,Is(main,leave.argumentPorts[0],workspace));main.Active(match.ifTrue,root,false);
                var restore=main.Event("RestorePhysicalObjects");main.Active(restore.trigger,root,And(main,Is(main,main.Var("Workspace"),workspace),Is(main,main.Var("Phase"),0)));
            }
        }
    }
}
