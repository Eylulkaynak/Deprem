// Scene-authoring only. The player uses native graph units and authored objects.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static GameObject aidBroadcast;
        static void CreateAidBroadcast()
        {
            InitialPhysical("BroadcastDial",25);InitialPhysical("BroadcastHeard");InitialPhysical("BroadcastSource");
            Vector3 at=aidCenter+new Vector3(1.3f,.90f,.65f);
            aidBroadcast=Group("Resmî bilgiyi duyma ve doğrulama",interactions.transform,at);aidBroadcast.SetActive(false);
            Shape("Görevlinin yayın masası",PrimitiveType.Cube,aidBroadcast.transform,new Vector3(0,-.055f,0),new Vector3(.98f,.08f,.70f),mats["YY_cream"]);
            var receiver=Model("Radio",aidBroadcast.transform,new Vector3(-.12f,.018f,.02f),1.3f,0);receiver.transform.localRotation=Quaternion.Euler(90,180,0);
            var dial=Shape("Yardım alıcısının frekans düğmesi",PrimitiveType.Cylinder,aidBroadcast.transform,new Vector3(.24f,.14f,.04f),new Vector3(.14f,.028f,.14f),mats["YY_coral"]);
            Shape("Alıcı düğmesinin çizgisi",PrimitiveType.Cube,dial.transform,new Vector3(0,1.1f,.27f),new Vector3(.09f,.1f,.35f),mats["YY_cream"]);var hit=dial.AddComponent<BoxCollider>();hit.size=new Vector3(1.7f,3,1.7f);
            var indicator=Shape("Resmî yayın bulundu ışığı",PrimitiveType.Sphere,aidBroadcast.transform,new Vector3(.25f,.14f,-.15f),new Vector3(.055f,.020f,.055f),mats["YY_mustard"]);
            var listen=Shape("Resmî duyuruyu dinle",PrimitiveType.Cube,aidBroadcast.transform,new Vector3(-.01f,.08f,-.23f),new Vector3(.25f,.035f,.12f),mats["YY_teal"]);listen.AddComponent<BoxCollider>();
            for(int i=0;i<3;i++)Shape("Yayın dalgası",PrimitiveType.Cube,listen.transform,new Vector3(-.22f+i*.22f,.7f,0),new Vector3(.08f,.16f,.30f+i*.12f),mats["YY_cream"]);
            WorkCamera("broadcast",at+new Vector3(0,.05f,0),.90f);
            var g=new YanYanaGraphAuthor(dial,"Kontrol edilmemiş radyonun yerine resmî alıcıyı ayarla");var drag=g.Add(new OnDrag());g.Bind(drag.target,dial);var can=g.Branch(drag.trigger,AtWork(g,"broadcast"));var pointer=PointerOnPlane(g,can.ifTrue,drag.data,at.y+.14f);
            var angle=g.Call(typeof(Mathf),"Atan2",null,new[]{typeof(float),typeof(float)},g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"x",pointer.point),dial.transform.position.x),g.Binary<ScalarSubtract>(g.Get(typeof(Vector3),"z",pointer.point),dial.transform.position.z)).result;
            var p=g.SetVar(pointer.path,"BroadcastDial",g.Call(typeof(Mathf),"RoundToInt",null,OneFloat,g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},Sum(g,g.Binary<ScalarMultiply>(angle,Mathf.Rad2Deg),90f),0f,180f).result).result,flow);g.Send(p,flow,"RestoreBroadcast");g.Dirty();
            var dg=new YanYanaGraphAuthor(listen,"Yalnız net duyulan resmî bilgiyi kullan");var click=dg.Add(new OnPointerClick());dg.Bind(click.target,listen);can=dg.Branch(click.trigger,AtWork(dg,"broadcast"));var good=dg.Branch(can.ifTrue,And(dg,dg.Binary<GreaterOrEqual>(dg.Var("BroadcastDial",flow),112),dg.Binary<LessOrEqual>(dg.Var("BroadcastDial",flow),128)));
            p=Text(dg,good.ifFalse,lineText,"Bora: Duyuru net değil. Frekansı ayarlayıp yeniden dinleyelim.");dg.Send(good.ifTrue,flow,"HearOfficialBroadcast");dg.Dirty();
            var enter=main.Event("OpenAidBroadcast");p=main.Active(enter.trigger,aidBroadcast,true);p=main.SetVar(p,"Workspace","broadcast");p=main.Active(p,workCameras["broadcast"].gameObject,true);p=Text(main,p,goalText,"Resmî duyuruyu netleştir ve dinle");p=Text(main,p,gestureText,"Düğmeyi döndür · yayın tuşuna dokun");
            var carried=main.Branch(p,And(main,Is(main,main.Var("RadioReady"),1),PackedForTravel(main,"Radio")));
            p=main.SetVar(carried.ifTrue,"BroadcastDial",120);p=main.SetVar(p,"BroadcastSource",1);p=Text(main,p,lineText,"Ada: Denediğimiz radyo yanımızda. Resmî duyuruyu birlikte dinleyelim.");p=main.Send(p,flow,"RestoreBroadcast");main.Send(p,flow,"FinishAidCameraBlend");
            p=main.SetVar(carried.ifFalse,"BroadcastSource",2);p=Text(main,p,lineText,"Bora: Görevli alıcısını kullanabiliriz. Önce yayını netleştirelim.");p=main.Send(p,flow,"RestoreBroadcast");main.Send(p,flow,"FinishAidCameraBlend");
            var restore=main.Event("RestoreBroadcast");p=main.Set(restore.trigger,typeof(Transform),"eulerAngles",dial.transform,V3(main,0,main.Var("BroadcastDial"),0));main.Active(p,indicator,And(main,main.Binary<GreaterOrEqual>(main.Var("BroadcastDial"),112),main.Binary<LessOrEqual>(main.Var("BroadcastDial"),128)));
            var reset=main.Event("RestorePhysicalObjects");main.Send(reset.trigger,flow,"RestoreBroadcast");
            var heard=main.Event("HearOfficialBroadcast",true);p=main.SetVar(heard.trigger,"Busy",true);p=main.SetVar(p,"BroadcastHeard",1);p=Text(main,p,lineText,"Radyo: Aile bilgileri yardım masasından doğrulanıyor. Görevliye başvurun.");p=main.Wait(p,2.2f);p=main.SetVar(p,"AidStage",2);p=main.Send(p,flow,"CommitCheckpoint");main.Send(p,flow,"AdvanceAid");
        }
    }
}
