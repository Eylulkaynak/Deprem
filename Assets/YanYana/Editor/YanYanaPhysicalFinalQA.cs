using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static int finalRoute;
        static double finalStarted,lastFinalAction;
        [MenuItem("Tools/Yan Yana/QA/Physical Four Reunions")]
        static void FourReunions()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            finalRoute=1;File.WriteAllText("ClientExports/YanYana/Reports/physical-four-reunions.txt","Four isolated decision-state fixtures. Selection priority, actual walking and pointer completion are exercised; this is not four full human playthroughs.\n");
            StartReunionFixture();EditorApplication.update-=ReunionTick;EditorApplication.update+=ReunionTick;
        }
        static void StartReunionFixture()
        {
            Fixture(7,Find("Street_Aid").transform.position+Vector3.forward*1.4f);Find("Senin yolun · neden ve sonuç").SetActive(false);
            foreach(int index in new[]{1,2,3,4})Find("Buluşma yolu "+index).SetActive(false);
            var vars=Variables.Object(Flow);vars.Set("FamilyPlan",finalRoute==1||finalRoute==2?1:0);vars.Set("FacadeReported",finalRoute==1?1:0);vars.Set("FireAssisted",0);vars.Set("NeighborTogether",finalRoute==1||finalRoute==3?1:0);vars.Set("FinalStep",0);vars.Set("Ending",0);vars.Set("Reunited",0);vars.Set("InfoVerified",1);
            finalStarted=lastFinalAction=EditorApplication.timeSinceStartup;CustomEvent.Trigger(Flow,"BeginReunion");
        }
        static void ReunionTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=ReunionTick;return;}
            double now=EditorApplication.timeSinceStartup;var vars=Variables.Object(Flow);
            if(now-finalStarted>65){File.AppendAllText("ClientExports/YanYana/Reports/physical-four-reunions.txt","FAIL route="+finalRoute+" timeout phase="+vars.Get("Phase")+" step="+vars.Get("FinalStep")+" busy="+vars.Get("Busy")+"\n");EditorApplication.update-=ReunionTick;return;}
            if((int)vars.Get("Phase")==8)
            {
                int actual=(int)vars.Get("Ending");File.AppendAllText("ClientExports/YanYana/Reports/physical-four-reunions.txt",(actual==finalRoute?"PASS":"FAIL")+" expected="+finalRoute+" actual="+actual+" reunited="+vars.Get("Reunited")+" seconds="+(now-finalStarted).ToString("F2")+"\n");
                if(finalRoute==4){EditorApplication.update-=ReunionTick;return;}finalRoute++;StartReunionFixture();return;
            }
            if((bool)vars.Get("Busy")||now-lastFinalAction<.65)return;lastFinalAction=now;FinalStep();EditorApplication.QueuePlayerLoopUpdate();
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Reload")]
        static void ReloadPhysical(){CustomEvent.Trigger(Flow,"CommitCheckpoint");PlayerPrefs.SetInt("Deprem.YanYana.v1.autoContinue",1);PlayerPrefs.Save();UnityEngine.SceneManagement.SceneManager.LoadScene(YanYanaAdventureBuilder.ScenePath);}
        [MenuItem("Tools/Yan Yana/QA/Physical Pause")]
        static void PausePhysical()=>Find("Pause").GetComponent<Button>().onClick.Invoke();
        [MenuItem("Tools/Yan Yana/QA/Physical Resume")]
        static void ResumePhysical()=>Find("Resume").GetComponent<Button>().onClick.Invoke();
        [MenuItem("Tools/Yan Yana/QA/Physical Replay Map")]
        static void ReplayMap()=>CustomEvent.Trigger(Flow,"ReplayPhysical","map");
    }
}
