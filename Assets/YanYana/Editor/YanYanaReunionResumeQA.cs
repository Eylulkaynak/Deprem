// Editor integration: real reunion handlers and the production pause save.
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical Reunion Save Resume")]
        static void ReunionResumeQA()=>StartPhysicalCheck(ReunionResumeSequence(),"physical-reunion-resume");
        static void ReloadReunionSavedOnly()
        {
            // Select automatic Continue without committing the current state.
            PlayerPrefs.SetInt("Deprem.YanYana.v1.autoContinue",1);PlayerPrefs.Save();
            SceneManager.LoadScene(YanYanaAdventureBuilder.ScenePath);
        }
        static IEnumerable<object> ReunionResumeSequence()
        {
            var previous=Flow;New();yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=previous&&ReadyIn(""),"Fresh reunion session"))yield return f;
            finalRoute=4;StartReunionFixture();
            foreach(var f in WaitPhysical(()=>State("Ending")==4&&ReadyIn(""),"Stage official reunion"))yield return f;
            FinalStep();foreach(var f in WaitPhysical(()=>State("FinalStep")==1&&ReadyIn(""),"First family marker"))yield return f;
            FinalStep();foreach(var f in WaitPhysical(()=>State("FinalStep")==2&&ReadyIn(""),"Children gather beside family"))yield return f;
            if(PlayerPrefs.GetInt("Deprem.YanYana.v1.physical.FinalStep",-1)!=2)throw new InvalidOperationException("Reaching the family did not automatically save the completed step.");
            float separation=Vector3.Distance(Find("Ada").transform.position,Find("Efe").transform.position);
            if(separation>1.25f)throw new InvalidOperationException("Efe remains behind the family group: "+separation);
            File.AppendAllText(physicalCheckReport,"PASS arrival autosaved without an extra checkpoint call; children separation="+separation+"\n");
            previous=Flow;ReloadReunionSavedOnly();yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=previous&&State("Phase")==7&&State("FinalStep")==2&&ReadyIn(""),"Resume at the family gesture"))yield return f;
            if(Find("Efe’nin yanına git").GetComponent<TMP_Text>().text!="Aile işaretini birlikte yap")throw new InvalidOperationException("Restored goal does not match the pending gesture.");
            File.AppendAllText(physicalCheckReport,"PASS saved gesture restored with the correct goal.\n");
            FinalStep();foreach(var f in WaitPhysical(()=>State("FinalStep")==3&&State("Phase")==7,"Begin greeting"))yield return f;
            PausePhysical();yield return null;
            if(PlayerPrefs.GetInt("Deprem.YanYana.v1.physical.FinalStep",-1)!=3||!(bool)Variables.Object(Flow).Get("Paused"))throw new InvalidOperationException("Production pause did not save the unfinished greeting.");
            previous=Flow;ReloadReunionSavedOnly();yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=previous&&State("Phase")==8&&State("Reunited")==1,"Saved greeting finishes after resume"))yield return f;
            if(State("Ending")!=4)throw new InvalidOperationException("Resume changed the ending.");
            File.AppendAllText(physicalCheckReport,"PASS paused greeting restarted and reached the same ending without another gesture.\n");Capture();
        }
    }
}
