// Editor-only integration harness. Every decision uses production pointer handlers.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static IEnumerator<object> recordedRoute;
        static bool recordedRouteRunning;
        static string RouteReport="ClientExports/YanYana/Reports/physical-recorded-prepared-route.txt";
        static int routeExpected=2;
        static bool routePreparation=true,routeNeighbor,routeAssistance;
        static int State(string key)=>(int)Variables.Object(Flow).Get(key);
        static bool ReadyIn(string work)=>(string)Variables.Object(Flow).Get("Workspace")==work&&!(bool)Variables.Object(Flow).Get("Busy")&&!(bool)Variables.Object(Flow).Get("Paused")&&!Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>().IsBlending;
        static IEnumerable<object> AwaitRoute(Func<bool> condition,string description,double timeout=35)
        {
            double start=EditorApplication.timeSinceStartup;
            while(!condition()){if(EditorApplication.timeSinceStartup-start>timeout)throw new InvalidOperationException("Timeout: "+description+"; phase="+State("Phase")+" workspace="+Variables.Object(Flow).Get("Workspace"));yield return null;}
            File.AppendAllText(RouteReport,"PASS "+description+"\n");
        }
        static IEnumerable<object> TimedDrag(GameObject obj,Vector3 destination,float seconds=.65f)
        {
            var a=(Vector2)Camera.main.WorldToScreenPoint(obj.transform.position);var b=(Vector2)Camera.main.WorldToScreenPoint(destination);
            var data=new PointerEventData(EventSystem.current){position=a,pressPosition=a,pointerId=-1,button=PointerEventData.InputButton.Left,pointerDrag=obj,pointerPress=obj,pointerCurrentRaycast=new RaycastResult{gameObject=obj,worldPosition=obj.transform.position}};
            ExecuteEvents.Execute(obj,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(obj,data,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(obj,data,ExecuteEvents.beginDragHandler);data.dragging=true;
            double start=EditorApplication.timeSinceStartup;
            while(EditorApplication.timeSinceStartup-start<seconds)
            {
                var next=Vector2.Lerp(a,b,Mathf.Clamp01((float)(EditorApplication.timeSinceStartup-start)/seconds));data.delta=next-data.position;data.position=next;ExecuteEvents.Execute(obj,data,ExecuteEvents.dragHandler);yield return null;
            }
            data.delta=b-data.position;data.position=b;ExecuteEvents.Execute(obj,data,ExecuteEvents.dragHandler);ExecuteEvents.Execute(obj,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(obj,data,ExecuteEvents.endDragHandler);data.dragging=false;
        }
        [MenuItem("Tools/Yan Yana/QA/Record Full Prepared Route")]
        static void RecordPreparedRoute()=>ConfigureRecordedRoute(2,true,false,false);
        [MenuItem("Tools/Yan Yana/QA/Record Full Neighbor Route")]
        static void RecordNeighborRoute()=>ConfigureRecordedRoute(3,false,true,false);
        [MenuItem("Tools/Yan Yana/QA/Record Full Official Help Route")]
        static void RecordOfficialRoute()=>ConfigureRecordedRoute(4,false,false,false);
        [MenuItem("Tools/Yan Yana/QA/Record Full Alternative Route")]
        static void RecordAlternativeRoute()=>ConfigureRecordedRoute(1,false,false,true);
        static void ConfigureRecordedRoute(int ending,bool preparation,bool neighbor,bool assistance)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            if(physicalCheckRunning||recordedRouteRunning)throw new InvalidOperationException("Finish or stop the active integration run before starting another.");recordedRouteRunning=true;
            routeExpected=ending;routePreparation=preparation;routeNeighbor=neighbor;routeAssistance=assistance;RouteReport="ClientExports/YanYana/Reports/physical-recorded-route-"+ending+".txt";
            File.WriteAllText(RouteReport,"Technical full route, not a child session. Normal-time walking, timed EventSystem drags, no decision/inventory/health assignment or actor warps. Walking-only destinations use the existing player movement component.\n");
            recordedRoute=PreparedRoute().GetEnumerator();EditorApplication.update-=RecordedRouteTick;EditorApplication.update+=RecordedRouteTick;
        }
        static void RecordedRouteTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=RecordedRouteTick;recordedRouteRunning=false;return;}
            try{if(!recordedRoute.MoveNext()){EditorApplication.update-=RecordedRouteTick;recordedRouteRunning=false;YanYanaDeliveryTools.StopRecording();Inspect();}}
            catch(Exception e){EditorApplication.update-=RecordedRouteTick;recordedRouteRunning=false;YanYanaDeliveryTools.StopRecording();File.AppendAllText(RouteReport,"FAIL "+e+"\n");Inspect();Debug.LogException(e);}
        }
        static IEnumerable<object> PreparedRoute()
        {
            var previous=Flow;New();yield return null;foreach(var f in AwaitRoute(()=>Flow!=previous&&ReadyIn(""),"New journey"))yield return f;YanYanaDeliveryTools.StartRecording();
            foreach(var f in IntroCarrySequence(!routePreparation,false))yield return f;
            foreach(var f in AwaitRoute(()=>State("IntroDone")==1&&ReadyIn(""),"Physical opening completed without preparation flags"))yield return f;
            if(routePreparation)
            {
            Map();foreach(var f in AwaitRoute(()=>ReadyIn("map"),"Walk to family map"))yield return f;
            var map=Find("Ailecek denenen resimli mahalle planı").transform.position;foreach(int cell in new[]{3,6,7,10,11})foreach(var f in TimedDrag(Find("Haritadaki aile taşı"),map+new Vector3(cell%3*.22f,.065f,cell/3*.22f)))yield return f;
            foreach(var f in AwaitRoute(()=>State("FamilyPlan")==1,"Family plan learned"))yield return f;
            Back();foreach(var f in AwaitRoute(()=>ReadyIn(""),"Leave map"))yield return f;
            foreach(string key in collectibles)
            {
                Click(Find("Odada bulunacak · "+key));
                if(key=="Water"||key=="Food")
                {
                    foreach(var f in AwaitRoute(()=>ReadyIn("supply"+key),"Approach and inspect "+key))yield return f;
                    var surface=Find("Ambalaj incelemesi · "+key).transform.position;
                    foreach(var f in TimedDrag(Find("Ambalajı çevir · "+key),surface+new Vector3(.18f,.025f,.03f)))yield return f;
                    foreach(var f in TimedDrag(Find("İncelenen "+key+" 1"),surface+new Vector3(0,.035f,-.34f)))yield return f;
                    foreach(var f in AwaitRoute(()=>ReadyIn(""),"Leave supply comparison"))yield return f;
                }
                foreach(var f in AwaitRoute(()=>State("Found."+key)==1,"Collect "+key))yield return f;
            }
            Fener();foreach(var f in AwaitRoute(()=>ReadyIn("flashlight"),"Approach flashlight"))yield return f;
            var c=Find("Anchor_FlashlightWork").transform.position+Vector3.up*.04f;
            foreach(var f in TimedDrag(Find("Kaydırılabilir pil kapağı"),c+new Vector3(.21f,.068f,-.04f)))yield return f;
            foreach(var f in TimedDrag(Find("AA pil 1"),c+new Vector3(-.031f,.035f,-.028f)))yield return f;
            foreach(var f in TimedDrag(Find("AA pil 2"),c+new Vector3(.031f,.035f,-.028f)))yield return f;
            foreach(var f in TimedDrag(Find("Kaydırılabilir pil kapağı"),c+new Vector3(0,.068f,-.018f)))yield return f;Click(Find("Gerçek açma anahtarı"));
            foreach(var f in AwaitRoute(()=>State("FlashlightReady")==1,"Flashlight powered"))yield return f;
            Back();foreach(var f in AwaitRoute(()=>ReadyIn(""),"Leave flashlight"))yield return f;
            Radio();foreach(var f in AwaitRoute(()=>ReadyIn("radio"),"Approach radio"))yield return f;var dial=Find("Frekans düğmesi");foreach(var f in TimedDrag(dial,dial.transform.position+new Vector3(.10f,0,.173205f)))yield return f;
            foreach(var f in AwaitRoute(()=>State("RadioReady")==1,"Radio tuned"))yield return f;Back();foreach(var f in AwaitRoute(()=>ReadyIn(""),"Leave radio"))yield return f;
            Bag();foreach(var f in AwaitRoute(()=>ReadyIn("bag"),"Approach backpack"))yield return f;
            var origin=Find("Anchor_BagWork").transform.position+new Vector3(-.2375f,.083f,-.13f);
            foreach(var item in new[]{("Radio",0,0,3,2),("FirstAid",3,0,2,2),("Blanket",0,2,2,2),("Water",2,2,1,3),("Flashlight",3,2,1,2),("Food",0,4,2,1),("ComfortFox",4,2,1,2),("FamilyCard",0,5,1,1),("Whistle",1,5,1,1)})
            {
                foreach(var f in TimedDrag(Find("Yerleşim · "+item.Item1),origin+new Vector3((item.Item2+item.Item4*.5f)*.095f,0,(item.Item3+item.Item5*.5f)*.095f)))yield return f;
                foreach(var f in AwaitRoute(()=>State("Pack."+item.Item1+".X")==item.Item2,"Pack "+item.Item1))yield return f;
            }
            Click(Find("Çantayı kapatma tokası"));foreach(var f in CompleteBackpackFitting())yield return f;foreach(var f in AwaitRoute(()=>ReadyIn("")&&State("BagCarried")==1,"Close, fit and physically carry packed bag"))yield return f;
            AdultSafety();foreach(var f in AwaitRoute(()=>State("ShelfSecured")==1&&State("WardrobeSecured")==1,"Two adult tasks queued safely"))yield return f;
            var box=Find("Çıkış önündeki hafif oyuncak kutusu");Click(box);foreach(var f in AwaitRoute(()=>Vector3.Distance(Find("Ada").transform.position,Find("Anchor_ExitBox").transform.position+Vector3.forward*.65f)<.5f,"Approach light box"))yield return f;foreach(var f in TimedDrag(box,Find("Anchor_ExitBox").transform.position+Vector3.right*.9f))yield return f;
            }
            BeginBridge();foreach(var f in AwaitRoute(()=>ReadyIn("bridge"),"Walk to Efe's toy"))yield return f;
            var bridge=Find("Anchor_CoverAda").transform.position+Vector3.up*(Find("Anchor_Comfort").transform.position.y+.035f);
            for(int i=0;i<3;i++)foreach(var f in TimedDrag(Find("Köprü parçası "+i),bridge+new Vector3(-.2f+i*.2f,0,0)))yield return f;
            foreach(var f in AwaitRoute(()=>State("Phase")==1&&!((bool)Variables.Object(Flow).Get("Busy")),"Earthquake begins"))yield return f;
            foreach(var f in PhysicalCoverSequence())yield return f;
            Click(Find("Efe"));foreach(var f in AwaitRoute(()=>ReadyIn("sibling"),"Approach Efe and listen"))yield return f;
            if(routePreparation){foreach(var f in TimedDrag(Find("Efe’ye uzanan el"),(Vector3)Variables.Object(Flow).Get("SiblingTarget")))yield return f;}else Find("SiblingContinue").GetComponent<Button>().onClick.Invoke();
            foreach(var f in AwaitRoute(()=>ReadyIn("")&&State("SiblingChecked")==1,"Sibling communication choice completed"))yield return f;
            if(!routePreparation)Click(Find("Erişilebilir acil aydınlatma"));Click(Find("Kapalı kapı"));foreach(var f in AwaitRoute(()=>State("Phase")==3&&State("LightFound")==1,routePreparation?"Own flashlight used, safe exit":"Emergency light used, safe exit"))yield return f;
            WalkLanding();foreach(var f in AwaitRoute(()=>State("Phase")==31,"Aftershock at landing"))yield return f;foreach(var f in TimedDrag(Find("Ada"),Find("Ada").transform.position+Vector3.down*2,.5f))yield return f;
            foreach(var f in AwaitRoute(()=>State("AftershockDone")==1&&ReadyIn(""),"Stop and protect during aftershock"))yield return f;
            Neighbor();foreach(var f in AwaitRoute(()=>State("NeighborAsked")==1,"Ask Yusuf"))yield return f;
            if(routeNeighbor){Find("NeighborWalk").GetComponent<Button>().onClick.Invoke();foreach(var f in TimedDrag(Find("Yusuf’un önündeki hafif boş kutu"),Find("Street_Yusuf").transform.position+new Vector3(.85f,0,-1)))yield return f;foreach(var f in AwaitRoute(()=>State("NeighborTogether")==1,"Clear light obstacle, Yusuf walks with children"))yield return f;}else NeighborTeam();
            WalkIdil();foreach(var f in AwaitRoute(()=>State("Phase")==4,"Walk through courtyard"))yield return f;TalkIdil();foreach(var f in AwaitRoute(()=>ReadyIn("hose"),"Children safe, Idil controls hose"))yield return f;
            var hose=Find("İdil’in hortum bağlantısı").transform.position;foreach(var f in TimedDrag(Find("Hortumun kavrama ucu"),hose+new Vector3(0,.02f,.026f)))yield return f;var valve=Find("Su vanası");foreach(var f in TimedDrag(valve,valve.transform.position+Vector3.forward*.25f))yield return f;
            if(routeAssistance){foreach(var f in AwaitRoute(()=>ReadyIn("fire1"),"Idil ready for team support"))yield return f;Find("FireHelp").GetComponent<Button>().onClick.Invoke();}
            else for(int i=1;i<=3;i++){int stage=i;foreach(var f in AwaitRoute(()=>ReadyIn("fire"+stage),"Idil walks to fire "+stage))yield return f;PointerSpray();foreach(var f in AwaitRoute(()=>State("FireStage")>stage,"Fire group "+stage+" extinguished",100))yield return f;}
            foreach(var f in AwaitRoute(()=>State("Phase")==6&&ReadyIn(""),"Control returns to Ada"))yield return f;WalkBora();TalkBora();foreach(var f in AwaitRoute(()=>ReadyIn("aid"),"Bora takes over"))yield return f;
            var aid=Find("Street_Aid").transform.position;var places=new[]{aid+new Vector3(-3,1.75f,.05f),aid+new Vector3(0,1.75f,-1.4f),aid+new Vector3(3,1.75f,.05f)};
            for(int i=0;i<3;i++)foreach(var f in TimedDrag(Find("Taşınan ihtiyaç işareti "+i),places[i]))yield return f;
            foreach(var f in AwaitRoute(()=>ReadyIn("relief"),"Neighbors reach help stations"))yield return f;
            foreach(var f in TimedDrag(Find("Efe’ye verilecek su"),Find("Dinlenme su hedefi").transform.position))yield return f;
            foreach(var f in TimedDrag(Find("Efe’ye verilecek yiyecek"),Find("Efe’nin yiyecek tabağı").transform.position))yield return f;
            foreach(var f in AwaitRoute(()=>State("ReliefWaterGiven")==1&&State("ReliefFoodGiven")==1,"Water and food delivered separately"))yield return f;
            foreach(var f in AwaitRoute(()=>ReadyIn("broadcast"),routePreparation?"Prepared radio is available":"Official receiver provides missing radio alternative"))yield return f;BroadcastTest();foreach(var f in AwaitRoute(()=>ReadyIn("family"),"Official broadcast heard"))yield return f;
            Click(Find("Aile bilgisine başvur"));var table=Find("Görevlinin aile doğrulama masası").transform.position;
            if(routePreparation)foreach(var f in AwaitRoute(()=>ReadyIn("family")&&State("EfeContributed")==1,"Supported Efe walks and places learned family marker"))yield return f;
            foreach(var f in TimedDrag(Find("Bilgi parçası 0 0"),table+new Vector3(-.27f,.06f,.03f)))yield return f;
            if(!routePreparation)foreach(var f in TimedDrag(Find("Bilgi parçası 1 0"),table+new Vector3(.27f,.06f,.03f)))yield return f;
            foreach(var f in AwaitRoute(()=>State("Ending")==routeExpected&&ReadyIn(""),"Expected ending "+routeExpected+" selected"))yield return f;FinalStep();foreach(var f in AwaitRoute(()=>State("FinalStep")==1&&ReadyIn(""),"Walk to family marker"))yield return f;FinalStep();foreach(var f in AwaitRoute(()=>State("FinalStep")==2&&ReadyIn(""),"Reach family"))yield return f;
            foreach(string who in routeNeighbor?new[]{"Ada","Efe","Derya","Emre","Yusuf"}:new[]{"Ada","Efe","Derya","Emre"})
            {
                var animator=Find(who).GetComponentInChildren<Animator>();var point=Camera.main.WorldToViewportPoint(animator.GetBoneTransform(HumanBodyBones.Head).position+Vector3.up*.1f);
                if(point.z<=0||point.x<.08f||point.x>.92f||point.y<.27f||point.y>.84f)throw new InvalidOperationException("Reunion participant outside readable frame: "+who+" "+point);
                File.AppendAllText(RouteReport,"PASS reunion participant "+who+" viewport="+point+"\n");
            }
            FinalStep();foreach(var f in AwaitRoute(()=>State("Phase")==8,"Reunion completed"))yield return f;
            double show=EditorApplication.timeSinceStartup;while(EditorApplication.timeSinceStartup-show<3)yield return null;
            File.AppendAllText(RouteReport,"COMPLETE Ending="+State("Ending")+" FamilyPlan="+State("FamilyPlan")+" FlashlightReady="+State("FlashlightReady")+" RadioReady="+State("RadioReady")+" BroadcastSource="+State("BroadcastSource")+" Reunited="+State("Reunited")+". Duration is not evidence of target-age playtime.\n");
        }
    }
}
