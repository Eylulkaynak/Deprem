using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Story;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static double nativeSprayUntil;
        static int nativeSprayIndex;
        static FirefighterExtinguishManager nativeManager;
        static int nativeHeldFrames;
        static void Fixture(int phase,Vector3 position)
        {
            var vars=Variables.Object(Flow);vars.Set("Phase",phase);vars.Set("Busy",false);vars.Set("Paused",false);vars.Set("Workspace","");vars.Set("Role","Ada");Time.timeScale=1;
            Find("Yan Yana · başlangıç").SetActive(false);Find("Ada").GetComponent<StoryPlayerMovement>().SetStoryInputLocked(false);Find("Ada").GetComponent<StoryPlayerMovement>().Warp(position);
            Find("Efe").GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(position+Vector3.right*.7f);Find("Efe").GetComponent<StorySiblingFollower>().SetFollowing(true);
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Yakından incele")))camera.gameObject.SetActive(false);
            foreach(var manager in UnityEngine.Object.FindObjectsByType<FirefighterExtinguishManager>(FindObjectsSortMode.None))manager.gameObject.SetActive(false);Find("FireHelp").SetActive(false);
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Fire Fixture")]
        static void FireFixture(){Fixture(4,Find("Street_Idil").transform.position+Vector3.forward*.8f);Click(Find("Idil"));}
        [MenuItem("Tools/Yan Yana/QA/Physical Hose Test")]
        static void HoseTest()
        {
            var root=Find("İdil’in hortum bağlantısı");Drag(Find("Hortumun kavrama ucu"),root.transform.position+new Vector3(0,.02f,.026f));
            if((int)Variables.Object(Flow).Get("HoseConnected")!=1)throw new InvalidOperationException("Hose coupling did not latch through drag handlers.");
            var valve=Find("Su vanası");Drag(valve,valve.transform.position+Vector3.forward*.25f);
            File.WriteAllText("ClientExports/YanYana/Reports/physical-hose-test.txt","Isolated fire fixture: coupling drag latched; valve rotation ready="+Variables.Object(Flow).Get("HoseReady"));
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Native Spray")]
        static void NativeSpray()
        {
            nativeManager=UnityEngine.Object.FindObjectsByType<FirefighterExtinguishManager>(FindObjectsSortMode.None).First(m=>m.isActiveAndEnabled);
            nativeSprayIndex=0;nativeSprayUntil=EditorApplication.timeSinceStartup+7.4;
            nativeHeldFrames=0;EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.update-=NativeSprayTick;EditorApplication.update+=NativeSprayTick;QueueSpray(EventType.MouseDown);
        }
        static void QueueSpray(EventType type)
        {
            if(!nativeManager)return;
            var so=new SerializedObject(nativeManager);var fire=so.FindProperty("fires").GetArrayElementAtIndex(nativeSprayIndex).FindPropertyRelative("hitCollider").objectReferenceValue as Collider;
            var point=Camera.main.WorldToScreenPoint(fire.bounds.center);var mouse=new Vector2(point.x,Screen.height-point.y);
            EditorGUIUtility.QueueGameViewInputEvent(new Event{type=type,button=0,mousePosition=mouse,displayIndex=0});EditorApplication.QueuePlayerLoopUpdate();
        }
        static void NativeSprayTick()
        {
            if(!EditorApplication.isPlaying||!nativeManager){EditorApplication.update-=NativeSprayTick;return;}
            if(Input.GetMouseButton(0))nativeHeldFrames++;
            if(EditorApplication.timeSinceStartup>=nativeSprayUntil)
            {
                QueueSpray(EventType.MouseUp);nativeSprayIndex++;
                if(nativeSprayIndex>=3)
                {
                    File.WriteAllText("ClientExports/YanYana/Reports/physical-native-spray.txt","Unity Editor queued Game View mouse input, no ApplyWater calls. Success="+nativeManager.IsSuccessful+" stage="+Variables.Object(Flow).Get("FireStage")+" heldFrames="+nativeHeldFrames+" aim="+nativeManager.CurrentAimPoint);
                    EditorApplication.update-=NativeSprayTick;return;
                }
                nativeSprayUntil=EditorApplication.timeSinceStartup+7.4;QueueSpray(EventType.MouseDown);
            }
            else QueueSpray(Input.GetMouseButton(0)?EventType.MouseDrag:EventType.MouseDown);
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Aid Fixture")]
        static void AidFixture(){Fixture(6,Find("Street_Aid").transform.position+Vector3.forward*.9f);Click(Find("Bora"));}
        [MenuItem("Tools/Yan Yana/QA/Physical Aid Routing Test")]
        static void AidTest()
        {
            var at=Find("Street_Aid").transform.position;var places=new[]{at+new Vector3(-3,1.75f,.05f),at+new Vector3(0,1.75f,-1.4f),at+new Vector3(3,1.75f,.05f)};
            var token=Find("Taşınan ihtiyaç işareti 0");Drag(token,places[1]);bool wrong=(int)Variables.Object(Flow).Get("VisitorHelped0")==0;
            for(int i=0;i<3;i++)Drag(Find("Taşınan ihtiyaç işareti "+i),places[i]);
            bool all=Enumerable.Range(0,3).All(i=>(int)Variables.Object(Flow).Get("VisitorHelped"+i)==1);
            File.WriteAllText("ClientExports/YanYana/Reports/physical-aid-routing.txt","Isolated aid fixture; actual pointer handlers. Wrong destination rejected="+wrong+"; three correct routes="+all);
            if(!wrong||!all)throw new InvalidOperationException("Aid routing failed.");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Relief Test")]
        static void ReliefTest()
        {
            Drag(Find("Efe’ye verilecek su"),Find("Dinlenme su hedefi").transform.position);
            Drag(Find("Efe’ye verilecek yiyecek"),Find("Efe’nin yiyecek tabağı").transform.position);
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Broadcast Test")]
        static void BroadcastTest()
        {
            var vars=Variables.Object(Flow);bool already=(int)vars.Get("BroadcastDial")==120;
            Click(Find("Resmî duyuruyu dinle"));bool rejected=(int)vars.Get("BroadcastHeard")==0;
            if(!already){var dial=Find("Yardım alıcısının frekans düğmesi");Drag(dial,dial.transform.position+new Vector3(.10f,0,.173205f));Click(Find("Resmî duyuruyu dinle"));}
            bool heard=(int)vars.Get("BroadcastHeard")==1;File.AppendAllText("ClientExports/YanYana/Reports/physical-broadcast.txt","Production pointer handlers; source="+vars.Get("BroadcastSource")+" pre-tuned="+already+" unclear rejected="+rejected+" heard="+heard+"\n");if(!heard||(!already&&!rejected))throw new InvalidOperationException("Broadcast consequence failed.");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Identity Test")]
        static void IdentityTest()
        {
            Click(Find("Aile bilgisine başvur"));var at=Find("Görevlinin aile doğrulama masası").transform.position;
            Drag(Find("Bilgi parçası 0 1"),at+new Vector3(-.27f,.06f,.03f));bool wrong=(int)Variables.Object(Flow).Get("IdentityPeople")==0;
            Drag(Find("Bilgi parçası 0 0"),at+new Vector3(-.27f,.06f,.03f));Drag(Find("Bilgi parçası 1 0"),at+new Vector3(.27f,.06f,.03f));
            bool right=(int)Variables.Object(Flow).Get("InfoVerified")==1;File.WriteAllText("ClientExports/YanYana/Reports/physical-identity.txt","Actual pointer handlers: incorrect portrait rejected="+wrong+"; both matching clues verified="+right);if(!wrong||!right)throw new InvalidOperationException("Family identity test failed.");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Final Step")]
        static void FinalStep()
        {
            int step=(int)Variables.Object(Flow).Get("FinalStep"),ending=(int)Variables.Object(Flow).Get("Ending");
            if(ending<1||ending>4)return;
            if(step==0)Click(Find("Final yol işareti "+ending));else if(step==1)Click(Find("Aileye kavuşma hedefi "+ending));else if(step==2)Drag(Find("Ada"),Find("Ada").transform.position+Vector3.up*2.2f);Inspect();
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Radio")]
        static void Radio()=>Click(Find("Ayarlanabilir radyo"));
        [MenuItem("Tools/Yan Yana/QA/Physical Radio Test")]
        static void RadioTest(){var dial=Find("Frekans düğmesi");Drag(dial,dial.transform.position+new Vector3(.1f,0,.173205f));if((int)Variables.Object(Flow).Get("RadioReady")!=1)throw new InvalidOperationException("Radio tuning at120 degrees failed.");}
        [MenuItem("Tools/Yan Yana/QA/Physical Map")]
        static void Map()=>Click(Find("Ailecek denenen resimli mahalle planı"));
        [MenuItem("Tools/Yan Yana/QA/Physical Map Test")]
        static void MapTest(){var origin=Find("Ailecek denenen resimli mahalle planı").transform.position;foreach(int cell in new[]{3,6,7,10,11})Drag(Find("Haritadaki aile taşı"),origin+new Vector3(cell%3*.22f,.065f,cell/3*.22f));if((int)Variables.Object(Flow).Get("FamilyPlan")!=1)throw new InvalidOperationException("Family map route failed.");}
    }
}
